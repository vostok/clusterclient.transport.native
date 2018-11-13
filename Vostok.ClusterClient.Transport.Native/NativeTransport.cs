using System;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Transport;
using Vostok.Clusterclient.Transport.Native.Client;
using Vostok.Clusterclient.Transport.Native.Messages;
using Vostok.Clusterclient.Transport.Native.Pool;
using Vostok.Clusterclient.Transport.Native.ResponseReading;
using Vostok.Clusterclient.Transport.Native.Sender;
using Vostok.Commons.Time;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Transport.Native
{
    /// <summary>
    /// <para>A legacy ClusterClient transport for .NET Core 2.0. Internally uses <c>WinHttpHandler</c> on Windows and <c>CurlHandler</c> on Unix-like OS.</para>
    /// </summary>
    [Obsolete("Don't use this ITransport implementation on .NET Core 2.1 or later. Use SocketsTransport instead.")]
    public class NativeTransport : ITransport, IDisposable
    {
        private const int DefaultBufferSize = 16 * 1024;
        private readonly NativeTransportSettings settings;
        private readonly ILog log;
        private readonly IHttpClientProvider httpClientProvider;
        private readonly ResponseReader reader;
        private readonly TransportRequestSender sender;
        private readonly HttpClientProvider clientProvider;

        /// <inheritdoc cref="NativeTransport" />
        public NativeTransport(NativeTransportSettings settings, ILog log)
        {
            this.settings = settings;
            this.log = log ?? throw new ArgumentNullException(nameof(log));

            httpClientProvider = new HttpClientProvider(settings, log);
            reader = new ResponseReader(settings, new Pool<byte[]>(() => new byte[16384]), log);
            this.log = log;

            sender = CreateSender(settings, log);
            clientProvider = new HttpClientProvider(this.settings, log);
        }

        /// <inheritdoc />
        public TransportCapabilities Capabilities { get; }
            = TransportCapabilities.RequestStreaming | TransportCapabilities.ResponseStreaming;

        /// <inheritdoc />
        public async Task<Response> SendAsync(Request request, TimeSpan? connectionTimeout, TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return Responses.Canceled;

            if (timeout.TotalMilliseconds < 1)
            {
                LogRequestTimeout(request, timeout);
                return Responses.Timeout;
            }

            using (var timeoutCancellation = new CancellationTokenSource())
            using (var requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                var client = clientProvider.GetClient(connectionTimeout);
                var timeoutTask = Task.Delay(timeout, timeoutCancellation.Token);
                var senderTask = sender.SendAsync(client, request, requestCancellation.Token);
                var completedTask = await Task.WhenAny(timeoutTask, senderTask).ConfigureAwait(false);
                if (completedTask is Task<Response> taskWithResponse)
                {
                    timeoutCancellation.Cancel();
                    return taskWithResponse.GetAwaiter().GetResult();
                }

                // completedTask is timeout Task
                requestCancellation.Cancel();
                LogRequestTimeout(request, timeout);

                // wait for cancellation & dispose resources associated with Response object
                // ReSharper disable once MethodSupportsCancellation
                var senderTaskContinuation = senderTask.ContinueWith(
                    t =>
                    {
                        if (t.IsCompleted)
                            t.GetAwaiter().GetResult().Dispose();
                    });

                using (var abortCancellation = new CancellationTokenSource())
                {
                    var abortWaitingDelay = Task.Delay(settings.RequestAbortTimeout, abortCancellation.Token);

                    await Task.WhenAny(senderTaskContinuation, abortWaitingDelay).ConfigureAwait(false);

                    abortCancellation.Cancel();
                }

                if (!senderTask.IsCompleted)
                    LogFailedToWaitForRequestAbort();

                return Responses.Timeout;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            httpClientProvider?.Dispose();
        }

        private static TransportRequestSender CreateSender(NativeTransportSettings settings, ILog log)
        {
            var pool = new Pool<byte[]>(() => new byte[DefaultBufferSize]);

            var requestFactory = new HttpRequestMessageFactory(pool, log);
            var responseReader = new ResponseReader(settings, pool, log);

            return new TransportRequestSender(requestFactory, responseReader, log);
        }

        #region Logging

        private void LogRequestTimeout(Request request, TimeSpan timeout)
        {
            log.Error("Request timed out. Target = {Target}. Timeout = {Timeout:0.000} sec.", request.Url.Authority, timeout.TotalSeconds);
        }

        private void LogUnknownException(Request request, Exception error)
        {
            log.Error(error, "Unknown error in sending request to {Target}. ", request.Url.Authority);
        }

        private void LogWin32Error(Request request, Win32Exception error)
        {
            log.Error(error, "WinAPI error with code {ErrorCode} while sending request to {Target}.", error.NativeErrorCode, request.Url.Authority);
        }

        private void LogFailedToWaitForRequestAbort()
        {
            log.Warn(
                "Timed out request was aborted but did not complete in {RequestAbortTimeout}.",
                settings.RequestAbortTimeout.ToPrettyString());
        }

        #endregion
    }
}