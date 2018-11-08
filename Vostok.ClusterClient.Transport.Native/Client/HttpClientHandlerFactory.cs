using System;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Transport.Native.Client
{
    internal static class HttpClientHandlerFactory
    {
        private static readonly object Sync = new object();
        private static volatile Func<HttpClientHandler> clientFactory;

        private static void EnsureInitialized(ILog log)
        {
            if (clientFactory != null)
                return;
            lock (Sync)
            {
                if (clientFactory != null)
                    return;
                clientFactory = BuildHandler(log);
            }
        }

        public static HttpClientHandler Build(NativeTransportSettings settings, TimeSpan? connectionTimeout, ILog log)
        {
            EnsureInitialized(log);
            var client = clientFactory();
            client.AllowAutoRedirect = settings.AllowAutoRedirect;
            client.AutomaticDecompression = DecompressionMethods.None;
            client.MaxResponseHeadersLength = int.MaxValue;
            client.Proxy = settings.Proxy;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                client.ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true;
                WinHttpHandlerTuner.Tune(client, connectionTimeout, log);
            }
            
           
            return client;
        }

        private static Func<HttpClientHandler> BuildHandler(ILog log)
        {
            try
            {
                var handlerType = typeof(HttpClientHandler);
                var ctor = handlerType.GetConstructor(BindingFlags.NonPublic|BindingFlags.Instance, null, new [] {typeof(bool)}, null);
                if (ctor == null)
                    return () => new HttpClientHandler();
                return Expression.Lambda<Func<HttpClientHandler>>(Expression.New(ctor, Expression.Constant(false))).Compile();
            }
            catch (Exception e)
            {
                log.ForContext(typeof(HttpClientHandlerFactory)).Warn(e);
                return () => new HttpClientHandler();
            }
        }
    }
}