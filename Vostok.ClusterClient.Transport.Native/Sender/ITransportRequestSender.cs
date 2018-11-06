using System.Threading;
using System.Threading.Tasks;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Transport.Native.Client;

namespace Vostok.Clusterclient.Transport.Native.Sender
{
    internal interface ITransportRequestSender
    {
        Task<Response> SendAsync(IHttpClient client, Request request, CancellationToken cancellationToken);
    }
}