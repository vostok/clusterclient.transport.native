using System;
using System.ComponentModel;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Transport.Native.Client;
using Vostok.Clusterclient.Transport.Native.Hacks;
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
            catch (HttpRequestException error) when (CurlExceptionHelper.IsCurlException(error.InnerException, out var code))
            {
                LogCurlError(request, error, code);
                return Responses.UnknownFailure;
            }
            catch (HttpRequestException error) when (error.InnerException is Win32Exception win32Exception)
            {
                LogWin32Error(request, win32Exception);
                return Responses.UnknownFailure;
            }
            catch (Exception e)
            {
                LogUnknownException(request, e);
                return Responses.UnknownFailure;
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
            catch (HttpRequestException error) when (error.InnerException is Win32Exception win32Exception)
            {
                return HandleWin32Error(request, win32Exception);
            }
            catch (HttpRequestException error) when (
                CurlExceptionHelper.IsCurlException(error.InnerException, out var code) &&
                CurlExceptionHelper.IsConnectionFailure(code))
            {
                LogCurlError(request, error, code);
                return Responses.ConnectFailure;
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

        private void LogUnknownException(Request request, Exception error)
        {
            log.Warn(error, "Unknown error in sending request to {Target}.", request.Url.Authority);
        }

        private Response HandleCancellationError(Request request, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return new Response(ResponseCode.Canceled);

            LogRequestTimeout(request);

            return new Response(ResponseCode.RequestTimeout);
        }

        private Response HandleWin32Error(Request request, Win32Exception error)
        {
            LogWin32Error(request, error);

            return WinHttpErrorsHandler.Handle(error);
        }

        private void LogRequestTimeout(Request request)
        {
            log.Error("Request timed out. Target = {Target}.", request.Url.Authority);
        }

        private void LogWin32Error(Request request, Win32Exception error)
        {
            log.Error(error, "WinAPI error with code {ErrorCode} while sending request to {Target}.", error.NativeErrorCode, request.Url.Authority);
        }

        private void LogCurlError(Request request, Exception error, CurlCode code)
        {
            log.Error(error, "CURL error with code {ErrorCode} while sending request to {Target}.", code, request.Url.Authority);
        }
    }
}