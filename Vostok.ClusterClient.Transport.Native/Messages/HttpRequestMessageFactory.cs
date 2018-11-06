using System.Net.Http;
using System.Threading;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Transport.Native.Contents;
using Vostok.Clusterclient.Transport.Native.Pool;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Transport.Native.Messages
{
    internal class HttpRequestMessageFactory : IHttpRequestMessageFactory
    {
        private static readonly HttpMethod Patch = new HttpMethod(RequestMethods.Patch);
        
        private readonly IPool<byte[]> pool;
        private readonly ILog log;

        public HttpRequestMessageFactory(IPool<byte[]> pool, ILog log)
        {
            this.pool = pool;
            this.log = log;
        }

        public HttpRequestMessage Create(Request request, CancellationToken cancellationToken, out SendContext sendContext)
        {
            sendContext = new SendContext();

            var method = TranslateRequestMethod(request.Method);
            var content = CreateContent(request, sendContext, cancellationToken);

            var message = new HttpRequestMessage(method, request.Url)
            {
                Content = content
            };

            HeadersConverter.Fill(request, message, log);

            return message;
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

        private HttpContent CreateContent(Request request, SendContext sendContext, CancellationToken cancellationToken)
        {
            var content = request.Content;
            var streamContent = request.StreamContent;

            if (content != null && content.Length > 0)
                return new RequestByteArrayContent(request, sendContext, pool, log, cancellationToken);
            if (streamContent != null)
                return new RequestStreamContent(request, sendContext, pool, log, cancellationToken);

            // (epeshk): return empty 'content' which extract Socket instance from write stream
            return new RequestEmptyContent(request, sendContext, log);
        }
    }
}