using System;

namespace Vostok.Clusterclient.Transport.Native.Client
{
    internal interface IHttpClientProvider : IDisposable
    {
        IHttpClient GetClient(TimeSpan? connectionTimeout);
    }
}