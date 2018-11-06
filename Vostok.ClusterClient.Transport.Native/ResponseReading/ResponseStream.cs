using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Vostok.Clusterclient.Transport.Native.ResponseReading
{
    internal class ResponseStream : Stream
    {
        private readonly Stream stream;
        private readonly RequestDisposableState state;

        public ResponseStream(Stream stream, RequestDisposableState state)
        {
            this.stream = stream;
            this.state = state;
        }

        public override bool CanRead { get; } = true;
        public override bool CanSeek { get; } = false;
        public override bool CanWrite { get; } = false;
        public override long Length => stream.Length;
        public override long Position
        {
            get => stream.Position;
            set => throw new NotSupportedException();
        }

        public override void Flush() => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count) => stream.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => stream.ReadAsync(buffer, offset, count, cancellationToken);

        protected override void Dispose(bool disposing)
        {
            stream.Dispose();
            state.Dispose();
            base.Dispose(disposing);
        }
    }
}