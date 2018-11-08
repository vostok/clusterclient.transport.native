using System;
using System.Net;

namespace Vostok.Clusterclient.Transport.Native
{
    /// <summary>
    /// A class that represents <see cref="NativeTransport"/> settings..
    /// </summary>
    public class NativeTransportSettings
    {
        /// <summary>
        ///     Gets or sets a <see cref="IWebProxy" /> instance which will be used to send requests.
        /// </summary>
        public IWebProxy Proxy { get; set; }

        /// <summary>
        ///     Gets or sets the maximum response body size in bytes. This parameter doesn't affect content streaming.
        /// </summary>
        public long? MaxResponseBodySize { get; set; }

        /// <summary>
        ///     Gets or sets the delegate that decide use response streaming or not.
        /// </summary>
        public Predicate<long?> UseResponseStreaming { get; set; } = _ => false;

        /// <summary>
        ///     How much time client should wait for internal handler return after request cancellation.
        /// </summary>
        public TimeSpan RequestAbortTimeout { get; set; } = TimeSpan.FromMilliseconds(250);

        /// <summary>
        ///     Gets or sets a value that indicates whether the transport should follow HTTP redirection responses.
        /// </summary>
        public bool AllowAutoRedirect { get; set; }
        
        internal Func<int, byte[]> BufferFactory { get; set; } = i => new byte[i];
    }
}