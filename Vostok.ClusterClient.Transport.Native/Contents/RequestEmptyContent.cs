using System.IO;
using System.Net;
using System.Threading.Tasks;
using Vostok.Clusterclient.Core.Model;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Transport.Native.Contents
{
    internal class RequestEmptyContent : ClusterClientHttpContent
    {
        public RequestEmptyContent(Request request, SendContext sendContext, ILog log)
            : base(request, sendContext, log)
        {
            Headers.ContentLength = 0;
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return true;
        }

        protected override Task SerializeAsync(Stream stream, TransportContext context) => Task.CompletedTask;
    }
}