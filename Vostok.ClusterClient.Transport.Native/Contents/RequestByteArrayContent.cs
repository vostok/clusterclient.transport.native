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
    internal class RequestByteArrayContent : ClusterClientHttpContent
    {
        private readonly Content content;
        private readonly IPool<byte[]> pool;
        private readonly CancellationToken cancellationToken;

        public RequestByteArrayContent(
            Request request,
            SendContext context,
            IPool<byte[]> pool,
            ILog log,
            CancellationToken cancellationToken)
            : base(request, context, log)
        {
            content = request.Content ?? throw new ArgumentNullException(nameof(request.Content), "Bug in code: content is null.");
            this.pool = pool;
            this.cancellationToken = cancellationToken;

            Headers.ContentLength = content.Length;
        }

        protected override async Task SerializeAsync(Stream stream, TransportContext context)
        {
            // (epeshk): avoid storing large buffers in Socket private fields.
            if (content.Buffer.Length < 84000)
            {
                await stream.WriteAsync(content.Buffer, content.Offset, content.Length, cancellationToken).ConfigureAwait(false);
                return;
            }

            using (pool.AcquireHandle(out var buffer))
            {
                var index = content.Offset;
                var end = content.Offset + content.Length;
                while (index < end)
                {
                    var size = Math.Min(buffer.Length, end - index);
                    Buffer.BlockCopy(content.Buffer, index, buffer, 0, size);
                    await stream.WriteAsync(buffer, 0, size, cancellationToken).ConfigureAwait(false);
                    index += size;
                }
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = content.Length;
            return true;
        }
    }
}