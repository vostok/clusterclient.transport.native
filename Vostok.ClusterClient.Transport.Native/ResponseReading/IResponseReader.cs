using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Vostok.Clusterclient.Transport.Native.ResponseReading
{
    internal interface IResponseReader
    {
        Task<ResponseReadResult> ReadResponseBodyAsync(HttpResponseMessage responseMessage, CancellationToken cancellationToken);
    }
}