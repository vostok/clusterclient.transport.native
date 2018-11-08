using System;
using System.Collections.Concurrent;
using System.Threading;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Transport.Native.Client
{
    internal class HttpClientProvider : IHttpClientProvider
    {
        private readonly NativeTransportSettings settings;
        private readonly ILog log;
        private ConcurrentDictionary<TimeSpan, Lazy<IHttpClient>> clients;

        public HttpClientProvider(NativeTransportSettings settings, ILog log)
        {
            this.settings = settings;
            this.log = log;
            clients = new ConcurrentDictionary<TimeSpan, Lazy<IHttpClient>>();
        }

        public IHttpClient GetClient(TimeSpan? connectionTimeout)
            => clients
                .GetOrAdd(
                    connectionTimeout ?? Timeout.InfiniteTimeSpan,
                    t => new Lazy<IHttpClient>(() => CreateClient(t)))
                .Value;
        

        private IHttpClient CreateClient(TimeSpan? connectionTimeout)
        {
            return new SystemNetHttpClient(HttpClientHandlerFactory.Build(settings, connectionTimeout, log), true);
        }

        public void Dispose()
        {
            foreach (var kvp in clients)
            {
                var client = kvp.Value.Value;

                client.CancelPendingRequests();
                client.Dispose();
            }
        }
    }
}