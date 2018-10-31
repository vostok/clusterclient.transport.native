using System.Linq;
using System.Net.Http;
using Vostok.Clusterclient.Core.Model;

namespace Vostok.Clusterclient.Transport.Native
{
    internal static class SystemNetHttpRequestConverter
    {
        private static readonly byte[] emptyByteArray = {};
        
        private static readonly HttpMethod Patch = new HttpMethod(RequestMethods.Patch);

        public static HttpRequestMessage Convert(Request request)
        {
            var message = new HttpRequestMessage(TranslateRequestMethod(request.Method), request.Url);

            if (request.Content != null)
            {
                message.Content = new ByteArrayContent(request.Content.Buffer, request.Content.Offset, request.Content.Length);
            }

            foreach (var header in request.Headers ?? Enumerable.Empty<Header>())
            {
                TryAddHeader(header, message);
            }

            return message;
        }

        private static void TryAddHeader(Header header, HttpRequestMessage message)
        {
            if (SystemNetHttpHeaderUtilities.IsContentHeader(header.Name))
            {
                if (message.Content == null)
                {
                    message.Content = new ByteArrayContent(emptyByteArray);
                }

                message.Content.Headers.TryAddWithoutValidation(header.Name, header.Value);
                return;
            }

            message.Headers.TryAddWithoutValidation(header.Name, header.Value);
        }
        
        private static HttpMethod TranslateRequestMethod(string httpMethod)
        {
            switch (httpMethod)
            {
                case RequestMethods.Get:
                    return HttpMethod.Get;
                case RequestMethods.Post:
                    return HttpMethod.Post;
                case RequestMethods.Put:
                    return HttpMethod.Put;
                case RequestMethods.Patch:
                    return Patch;
                case RequestMethods.Delete:
                    return HttpMethod.Delete;
                case RequestMethods.Head:
                    return HttpMethod.Head;
                case RequestMethods.Options:
                    return HttpMethod.Options;
                case RequestMethods.Trace:
                    return HttpMethod.Trace;
                default:
                    return new HttpMethod(httpMethod);
            }
        }
    }
}