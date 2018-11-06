using System.IO;
using Vostok.Clusterclient.Core.Model;

namespace Vostok.Clusterclient.Transport.Native.ResponseReading
{
    internal readonly struct ResponseReadResult
    {
        public readonly Content Content;
        public readonly Stream Stream;
        public readonly ResponseCode? ErrorCode;

        public ResponseReadResult(Content content)
            : this(content, null, null)
        {
        }

        public ResponseReadResult(Stream stream)
            : this(null, stream, null)
        {
        }

        public ResponseReadResult(ResponseCode errorCode)
            : this(null, null, errorCode)
        {
        }

        private ResponseReadResult(Content content, Stream stream, ResponseCode? code)
        {
            (Content, Stream, ErrorCode) = (content, stream, code);
        }
    }
}