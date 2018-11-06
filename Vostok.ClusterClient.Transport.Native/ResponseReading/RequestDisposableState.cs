using System;
using System.Net.Http;
using System.Threading;

namespace Vostok.Clusterclient.Transport.Native.ResponseReading
{
    internal class RequestDisposableState : IDisposable
    {
        private int disposeBarrier;

        public HttpRequestMessage RequestMessage { get; set; }
        public HttpResponseMessage ResponseMessage { get; set; }

        public void PreventNextDispose()
        {
            Interlocked.Exchange(ref disposeBarrier, 1);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposeBarrier, 0) > 0)
                return;

            DisposeRequest();
            DisposeResponse();
        }

        private void DisposeRequest()
        {
            if (RequestMessage != null)
            {
                try
                {
                    RequestMessage.Dispose();
                }
                catch
                {
                }
                finally
                {
                    RequestMessage = null;
                }
            }
        }

        private void DisposeResponse()
        {
            if (ResponseMessage != null)
            {
                try
                {
                    ResponseMessage.Dispose();
                }
                catch
                {
                }
                finally
                {
                    ResponseMessage = null;
                }
            }
        }
    }
}