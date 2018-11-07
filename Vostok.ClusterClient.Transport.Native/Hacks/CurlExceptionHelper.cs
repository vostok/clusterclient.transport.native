using System;
using System.Net.Http;

namespace Vostok.Clusterclient.Transport.Native.Hacks
{
    internal static class CurlExceptionHelper
    {
        private static readonly Type curlException = typeof(HttpClient).Assembly.GetType("System.Net.Http.CurlException");

        public static bool IsCurlException(Exception e, out CurlCode code)
        {
            code = (CurlCode) (e?.HResult ?? 0);
            return e != null && e.GetType() == curlException;
        }

        public static bool IsConnectionFailure(CurlCode code)
        {
            switch (code)
            {
                case CurlCode.CURLE_COULDNT_RESOLVE_PROXY:
                case CurlCode.CURLE_COULDNT_RESOLVE_HOST:
                case CurlCode.CURLE_COULDNT_CONNECT:
                    return true;
            }

            return false;
        }
    }
}