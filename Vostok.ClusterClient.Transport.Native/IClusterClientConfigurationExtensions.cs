using System;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core;

namespace Vostok.Clusterclient.Transport.Native
{
    [PublicAPI]
    public static class IClusterClientConfigurationExtensions
    {
        /// <summary>
        /// Initialiazes configuration transport with a <see cref="NativeTransport"/> with given settings.
        /// </summary>
        [Obsolete("Don't use this ITransport implementation on .NET Core 2.1 or later. Use SocketsTransport instead.")]
        public static void SetupNativeTransport(this IClusterClientConfiguration self, NativeTransportSettings settings)
        {
            self.Transport = new NativeTransport(settings, self.Log);
        }

        /// <summary>
        /// Initialiazes configuration transport with a <see cref="NativeTransport"/> with default settings.
        /// </summary>
        [Obsolete("Don't use this ITransport implementation on .NET Core 2.1 or later. Use SocketsTransport instead.")]
        public static void SetupNativeTransport(this IClusterClientConfiguration self)
        {
            self.Transport = new NativeTransport(self.Log);
        }
    }
}