using System.Diagnostics;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Vostok.Clusterclient.Transport.Tests.Shared.Functional;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Console;
using ITransport = Vostok.Clusterclient.Core.Transport.ITransport;

namespace Vostok.Clusterclient.Transport.Native.Tests
{
    internal class Config : ITransportTestConfig
    {
        public ILog CreateLog() => new ConsoleLog();

        public ITransport CreateTransport(TestTransportSettings settings, ILog log)
        {
            var transportSettings = new NativeTransportSettings
            {
                Proxy = settings.Proxy,
                MaxResponseBodySize = settings.MaxResponseBodySize,
                MaxConnectionsPerEndpoint = settings.MaxConnectionsPerEndpoint,
                UseResponseStreaming = settings.UseResponseStreaming,
                AllowAutoRedirect = settings.AllowAutoRedirect,
                BufferFactory = settings.BufferFactory
            };
            
            return new NativeTransport(transportSettings, log);
        }
        
        public TestTransportSettings CreateDefaultSettings() => new TestTransportSettings
        {
            MaxConnectionsPerEndpoint = 10 * 1000,
            BufferFactory = size => new byte[size],
            UseResponseStreaming = _ => false
        };
    }
    
    internal class AllowAutoRedirectTests : AllowAutoRedirectTests<Config>
    {
    }

    internal class ClientTimeoutTests : ClientTimeoutTests<Config>
    {
    }

    internal class ConnectionFailureTests : ConnectionFailureTests<Config>
    {
    }

    internal class ConnectionTimeoutTests : ConnectionTimeoutTests<Config>
    {
        public override void Should_timeout_on_connection_to_a_blackhole_by_connect_timeout()
        {
            while (!Debugger.IsAttached) ;
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                base.Should_timeout_on_connection_to_a_blackhole_by_connect_timeout();
        }
    }

    internal class ContentReceivingTests : ContentReceivingTests<Config>
    {
    }

    internal class ContentSendingTests : ContentSendingTests<Config>
    {
    }

    internal class HeaderReceivingTests : HeaderReceivingTests<Config>
    {
    }
    
    internal class HeaderSendingTests : HeaderSendingTests<Config>
    {
        public override void SetUp()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Assert.Pass("Test ignored on Windows");
            
            base.SetUp();
        }
    }
    
    internal class MaxConnectionsPerEndpointTests : MaxConnectionsPerEndpointTests<Config>
    {
    }

    internal class MethodSendingTests : MethodSendingTests<Config>
    {
    }

    internal class ProxyTests : ProxyTests<Config>
    {
    }

    internal class QuerySendingTests : QuerySendingTests<Config>
    {
    }

    internal class RequestCancellationTests : RequestCancellationTests<Config>
    {
    }

    internal class StatusCodeReceivingTests : StatusCodeReceivingTests<Config>
    {
    }

    internal class ContentStreamingTests : ContentStreamingTests<Config>
    {
    }

    internal class NetworkErrorsHandlingTests : NetworkErrorsHandlingTests<Config>
    {
    }
}