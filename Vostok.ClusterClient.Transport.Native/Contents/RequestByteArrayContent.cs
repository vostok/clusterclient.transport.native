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
        private readonly CancellationToken cancellationToken;

        public RequestByteArrayContent(
            Request request,
            SendContext context,
            ILog log,
            CancellationToken cancellationToken)
            : base(request, context, log)
        {
            content = request.Content ?? throw new ArgumentNullException(nameof(request.Content), "Bug in code: content is null.");
            this.cancellationToken = cancellationToken;

            Headers.ContentLength = content.Length;
        }

        protected override Task SerializeAsync(Stream stream, TransportContext context)
            => stream.WriteAsync(content.Buffer, content.Offset, content.Length, cancellationToken);

        protected override Task<Stream> CreateContentReadStreamAsync()
            => Task.FromResult<Stream>(new MemoryStream(content.Buffer, content.Offset, content.Length));

        protected override bool TryComputeLength(out long length)
        {
            length = content.Length;
            return true;
        }
    }
}