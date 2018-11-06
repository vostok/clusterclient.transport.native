using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Transport.Native.Client
{
    internal class SystemNetHttpClient : HttpClient, IHttpClient
    {
        public HttpMessageHandler Handler { get; }

        public SystemNetHttpClient(HttpMessageHandler handler)
            : base(handler)
        {
            Handler = handler;
        }

        public SystemNetHttpClient(HttpMessageHandler handler, bool disposeHandler)
            : base(handler, disposeHandler)
        {
            Handler = handler;
        }
    }
    
    internal interface IHttpClientProvider : IDisposable
    {
        IHttpClient GetClient(TimeSpan? connectionTimeout);
    }
    
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
            return new SystemNetHttpClient(HttpClientHandlerFactory.Build(settings, log), true);
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

    internal static class HttpClientHandlerFactory
    {
        private static readonly object sync = new object();
        private static volatile Func<HttpClientHandler> clientFactory;

        private static void EnsureInitialized(ILog log)
        {
            if (clientFactory != null)
                return;
            lock (sync)
            {
                if (clientFactory != null)
                    return;
                clientFactory = BuildHandler(log);
            }
        }

        public static HttpClientHandler Build(NativeTransportSettings settings, ILog log)
        {
            EnsureInitialized(log);
            var client = clientFactory();
            client.AllowAutoRedirect = settings.AllowAutoRedirect;
            client.AutomaticDecompression = DecompressionMethods.None;
            client.MaxResponseHeadersLength = int.MaxValue;
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                client.ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true;
            
           
            return client;
        }

        private static Func<HttpClientHandler> BuildHandler(ILog log)
        {
            try
            {
                var handlerType = typeof(HttpClientHandler);
                var ctor = handlerType.GetConstructor(BindingFlags.NonPublic, null, new [] {typeof(bool)}, null);
                if (ctor == null)
                    return () => new HttpClientHandler();
                return Expression.Lambda<Func<HttpClientHandler>>(Expression.New(ctor, Expression.Constant(false))).Compile();
            }
            catch (Exception)
            {
                return () => new HttpClientHandler();
            }
        }
    }
}