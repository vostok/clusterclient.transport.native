using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Transport.Native.Pool;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Transport.Native.Contents
{
    internal class RequestStreamWrapper : Stream
    {
        private readonly Stream stream;
        private readonly long? length;
        private readonly SendContext context;
        private long position;
        private readonly long bytesToSent;

        public RequestStreamWrapper(Stream stream, long? length, SendContext context)
        {
            this.stream = stream;
            this.length = length;
            this.context = context;
            bytesToSent = length ?? long.MaxValue;
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            try
            {
                count = (int) Math.Min(count, bytesToSent - position);
                var bytesRead = await stream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
                
                position += bytesRead;
                return bytesRead;
            }
            catch (StreamAlreadyUsedException)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception error)
            {
                context.Response = new Response(ResponseCode.StreamInputFailure);
                return 0;
            }
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead { get; } = true;
        public override bool CanSeek { get; } = false;
        public override bool CanWrite { get; } = false;
        public override long Length => length ?? throw new NotSupportedException();
        public override long Position { get; set; }
    }
    
    internal class RequestStreamContent : ClusterClientHttpContent
    {
        private readonly CancellationToken cancellationToken;
        private readonly IPool<byte[]> arrayPool;
        private readonly IStreamContent streamContent;

        public RequestStreamContent(
            Request request,
            SendContext sendContext,
            IPool<byte[]> arrayPool,
            ILog log,
            CancellationToken cancellationToken)
            : base(request, sendContext, log)
        {
            streamContent = request.StreamContent ?? throw new ArgumentNullException(nameof(request.StreamContent), "Bug in code: StreamContent is null.");
            this.arrayPool = arrayPool;
            this.cancellationToken = cancellationToken;

            Headers.ContentLength = request.StreamContent.Length;
        }

        protected override async Task SerializeAsync(Stream stream, TransportContext context)
        {
            var bodyStream = streamContent.Stream;
            var bytesToSend = streamContent.Length ?? long.MaxValue;
            var bytesSent = 0L;

            using (arrayPool.AcquireHandle(out var buffer))
            {
                while (bytesSent < bytesToSend)
                {
                    var bytesToRead = (int) Math.Min(buffer.Length, bytesToSend - bytesSent);

                    int bytesRead;

                    try
                    {
                        bytesRead = await bodyStream.ReadAsync(buffer, 0, bytesToRead, cancellationToken).ConfigureAwait(false);
                    }
                    catch (StreamAlreadyUsedException)
                    {
                        throw;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception error)
                    {
                        LogUserStreamFailure(error);
                        SendContext.Response = new Response(ResponseCode.StreamInputFailure);
                        return;
                    }

                    if (bytesRead == 0)
                        break;

                    await stream.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);

                    bytesSent += bytesRead;
                }
            }
        }

        protected override Task<Stream> CreateContentReadStreamAsync()
            => Task.FromResult((Stream) new RequestStreamWrapper(streamContent.Stream, streamContent.Length, SendContext));

        protected override bool TryComputeLength(out long length)
        {
            length = streamContent.Length ?? 0;
            return streamContent.Length != null;
        }

        private void LogUserStreamFailure(Exception error)
        {
            Log.Warn(error, "Failure in reading input stream while sending request body.");
        }
    }
}