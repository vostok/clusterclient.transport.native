using System;

namespace Vostok.Clusterclient.Transport.Native.Hacks
{
    // copy-paste of System.Net.Http.Headers.HttpHeaderType
    [Flags]
    internal enum HttpHeaderType : byte
    {
        General = 1,
        Request = 2,
        Response = 4,
        Content = 8,
        Custom = 16, // 0x10
        All = Custom | Content | Response | Request | General, // 0x1F
        None = 0
    }
}