using System.Net.Http;

namespace Vostok.Clusterclient.Transport.Native.Client
{
    internal class SystemNetHttpClient : HttpClient, IHttpClient
    {
        public SystemNetHttpClient(HttpMessageHandler handler)
            : base(handler)
        {
        }

        public SystemNetHttpClient(HttpMessageHandler handler, bool disposeHandler)
            : base(handler, disposeHandler)
        {
        }
    }
}