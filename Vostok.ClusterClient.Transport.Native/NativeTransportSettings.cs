using System;
using System.Net;

namespace Vostok.Clusterclient.Transport.Native
{
    public class NativeTransportSettings
    {
        public IWebProxy Proxy { get; set; }

        public long? MaxResponseBodySize { get; set; }

        public Func<int, byte[]> BufferFactory { get; set; } = i => new byte[i];

        public Predicate<long?> UseResponseStreaming { get; set; } = _ => false;
        
        public TimeSpan RequestAbortTimeout { get; set; } = TimeSpan.FromMilliseconds(250);

        public bool AllowAutoRedirect { get; set; } = false;
    }
}