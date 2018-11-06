using System.Net.Sockets;
using Vostok.Clusterclient.Core.Model;

namespace Vostok.Clusterclient.Transport.Native
{
    internal class SendContext
    {
        public Socket Socket;
        public Response Response;
    }
}