using System;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Transport.Native.Client;
using Vostok.Clusterclient.Transport.Native.Messages;
using Vostok.Clusterclient.Transport.Native.ResponseReading;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Transport.Native.Sender
{
    internal class TransportRequestSender : ITransportRequestSender
    {
        private readonly IHttpRequestMessageFactory requestFactory;
        private readonly IResponseReader responseReader;
        private readonly ILog log;

        public TransportRequestSender(
            IHttpRequestMessageFactory requestFactory,
            IResponseReader responseReader,
            ILog log)
        {
            this.requestFactory = requestFactory;
            this.responseReader = responseReader;
            this.log = log;
        }

        public async Task<Response> SendAsync(IHttpClient client, Request request, CancellationToken cancellationToken)
        {
            await Task.Yield();

            try
            {
                using (var state = new RequestDisposableState())
                    return await SendInternalAsync(client, state, request, cancellationToken).ConfigureAwait(false);
            }
            catch (StreamAlreadyUsedException)
            {
                throw;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return Responses.Canceled;
            }
            catch (Exception e)
            {
                LogUnknownException(e);
                return Responses.UnknownFailure;
            }
        }

        private static bool IsConnectionFailure(SocketError socketError)
        {
            switch (socketError)
            {
                case SocketError.HostNotFound:
                case SocketError.AddressNotAvailable:
                // seen on linux:
                case SocketError.ConnectionRefused:
                case SocketError.TryAgain:
                case SocketError.NetworkUnreachable:
                // other:
                case SocketError.NetworkDown:
                case SocketError.HostDown:
                case SocketError.HostUnreachable:
                    return true;
                default:
                    return false;
            }
        }

        private async Task<Response> SendInternalAsync(IHttpClient client, RequestDisposableState state, Request request, CancellationToken cancellationToken)
        {
            state.RequestMessage = requestFactory.Create(request, cancellationToken, out var sendContext);

            try
            {
                state.ResponseMessage = await client
                    .SendAsync(state.RequestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return HandleCancellationError(request, cancellationToken);
            }
            catch (Win32Exception error)
            {
                return HandleWin32Error(request, error);
            }

            if (sendContext.Response != null)
                return sendContext.Response;

            var responseCode = (ResponseCode) (int) state.ResponseMessage.StatusCode;

            var headers = HeadersConverter.Create(state.ResponseMessage);

            var responseReadResult = await responseReader
                .ReadResponseBodyAsync(state.ResponseMessage, cancellationToken)
                .ConfigureAwait(false);

            if (responseReadResult.Content != null)
                return new Response(responseCode, responseReadResult.Content, headers);
            if (responseReadResult.ErrorCode != null)
                return new Response(responseReadResult.ErrorCode.Value, null, headers);
            if (responseReadResult.Stream == null)
                return new Response(responseCode, null, headers);

            state.PreventNextDispose();
            return new Response(responseCode, null, headers, new ResponseStream(responseReadResult.Stream, state));
        }
        
        private void LogUnknownException(Exception error)
        {
            log.Warn(error, "Unknown error in sending request.");
        }
        
        private Response HandleCancellationError(Request request, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return new Response(ResponseCode.Canceled);

            // LogRequestTimeout(request, timeout);

            return new Response(ResponseCode.RequestTimeout);
        }
        private Response HandleWin32Error(Request request, Win32Exception error)
        {
            LogWin32Error(request, error);

            return WinHttpErrorsHandler.Handle(error);
        }

        private void LogRequestTimeout(Request request, TimeSpan timeout)
        {
            log.Error($"Request timed out. Target = {request.Url.Authority}. Timeout = {timeout.TotalSeconds:0.000} sec.");
        }

        private void LogUnknownException(Request request, Exception error)
        {
            log.Error($"Unknown error in sending request to {request.Url.Authority}. ", error);
        }

        private void LogWin32Error(Request request, Win32Exception error)
        {
            log.Error($"WinAPI error with code {error.NativeErrorCode} while sending request to {request.Url.Authority}.", error);
        }
    }
}