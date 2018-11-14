using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Vostok.Clusterclient.Core.Model;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Transport.Native.Contents
{
    internal class RequestStreamWrapper : Stream
    {
        private readonly Stream stream;
        private readonly long? length;
        private readonly SendContext context;
        private readonly ILog log;
        private readonly long bytesToSent;
        private long position;

        public RequestStreamWrapper(Stream stream, long? length, SendContext context, ILog log)
        {
            this.stream = stream;
            this.length = length;
            this.context = context;
            this.log = log;
            bytesToSent = length ?? long.MaxValue;
        }

        public override bool CanRead { get; } = true;
        public override bool CanSeek { get; } = false;
        public override bool CanWrite { get; } = false;
        public override long Length => length ?? throw new NotSupportedException();
        public override long Position { get; set; }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Console.WriteLine("RequestStreamWrapper");
            try
            {
                count = (int)Math.Min(count, bytesToSent - position);
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
                LogUserStreamFailure(error);
                context.Response = new Response(ResponseCode.StreamInputFailure);
                return 0;
            }
        }

        public override void Flush() => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

        public override void SetLength(long value) => throw new NotImplementedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();

        private void LogUserStreamFailure(Exception error)
            => log.Warn(error, "Failure in reading input stream while sending request body.");
    }
}