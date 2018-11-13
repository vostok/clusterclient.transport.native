using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Transport.Native.Pool;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Transport.Native.ResponseReading
{
    internal class ResponseReader : IResponseReader
    {
        private readonly IPool<byte[]> pool;
        private readonly ILog log;
        private readonly NativeTransportSettings settings;

        public ResponseReader(NativeTransportSettings settings, IPool<byte[]> pool, ILog log)
        {
            this.pool = pool;
            this.log = log;
            this.settings = settings;
        }

        public async Task<ResponseReadResult> ReadResponseBodyAsync(HttpResponseMessage responseMessage, CancellationToken cancellationToken)
        {
            try
            {
                var contentLength = responseMessage.Content.Headers.ContentLength;

                if (NeedToStreamResponseBody(contentLength))
                {
                    return new ResponseReadResult(await GetResponseWithStreamAsync(responseMessage).ConfigureAwait(false));
                }

                if (contentLength != null)
                {
                    if (contentLength == 0)
                        return new ResponseReadResult(Content.Empty);

                    if (contentLength > settings.MaxResponseBodySize)
                        return new ResponseReadResult(ResponseCode.InsufficientStorage);

                    return await GetResponseWithKnownContentLength(responseMessage, (int) contentLength, cancellationToken).ConfigureAwait(false);
                }

                return await GetResponseWithUnknownContentLength(responseMessage, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                log.Error(e);
                return new ResponseReadResult(ResponseCode.ReceiveFailure);
            }
        }

        private static Task<Stream> GetResponseWithStreamAsync(HttpResponseMessage responseMessage)
            => responseMessage.Content.ReadAsStreamAsync();

        private async Task<ResponseReadResult> GetResponseWithUnknownContentLength(HttpResponseMessage message, CancellationToken cancellationToken)
        {
            using (var stream = await message.Content.ReadAsStreamAsync().ConfigureAwait(false))
            using (var memoryStream = new MemoryStream())
            using (pool.AcquireHandle(out var buffer))
            {
                while (true)
                {
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                    if (bytesRead == 0)
                        break;

                    memoryStream.Write(buffer, 0, bytesRead);

                    if (memoryStream.Length > settings.MaxResponseBodySize)
                        return new ResponseReadResult(ResponseCode.InsufficientStorage);
                }

                var content = new Content(memoryStream.GetBuffer(), 0, (int) memoryStream.Length);
                return new ResponseReadResult(content);
            }
        }

        private async Task<ResponseReadResult> GetResponseWithKnownContentLength(
            HttpResponseMessage responseMessage,
            int contentLength,
            CancellationToken cancellationToken)
        {
            using (var stream = await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false))
            {
                var array = settings.BufferFactory(contentLength);

                var totalBytesRead = 0;

                // Reference to buffer used in ReadAsync will be stored in Socket instance. We want to avoid long-lived buffers in LOH
                // gcroot sample output for buffer used in .ReadAsync:
                // -> System.Net.Sockets.Socket
                // -> System.Net.Sockets.Socket+CachedEventArgs
                // -> System.Net.Sockets.Socket+AwaitableSocketAsyncEventArgs
                // -> System.Byte[]
                if (contentLength < 84000)
                {
                    while (totalBytesRead < contentLength)
                    {
                        var bytesToRead = Math.Min(contentLength - totalBytesRead, 16384);
                        var bytesRead = await stream.ReadAsync(array, totalBytesRead, bytesToRead, cancellationToken).ConfigureAwait(false);
                        if (bytesRead == 0)
                            break;

                        totalBytesRead += bytesRead;
                    }
                }
                else
                {
                    using (pool.AcquireHandle(out var buffer))
                    {
                        while (totalBytesRead < contentLength)
                        {
                            var bytesToRead = Math.Min(contentLength - totalBytesRead, buffer.Length);
                            var bytesRead = await stream.ReadAsync(buffer, 0, bytesToRead, cancellationToken).ConfigureAwait(false);
                            if (bytesRead == 0)
                                break;

                            Buffer.BlockCopy(buffer, 0, array, totalBytesRead, bytesRead);

                            totalBytesRead += bytesRead;
                        }
                    }
                }

                if (totalBytesRead < contentLength)
                    throw new EndOfStreamException($"Response stream ended prematurely. Read only {totalBytesRead} byte(s), but Content-Length specified {contentLength}.");

                return new ResponseReadResult(new Content(array, 0, contentLength));
            }
        }

        private bool NeedToStreamResponseBody(long? length)
        {
            try
            {
                return settings.UseResponseStreaming(length);
            }
            catch (Exception error)
            {
                log.Error(error);
                return false;
            }
        }
    }
}