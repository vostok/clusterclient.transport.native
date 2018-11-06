using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Vostok.Clusterclient.Transport.Native.Client
{
    internal interface IHttpClient : IDisposable
    {
        Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            HttpCompletionOption completionOption,
            CancellationToken cancellationToken);
        
        void CancelPendingRequests();
        
        HttpMessageHandler Handler { get; }
    }
}