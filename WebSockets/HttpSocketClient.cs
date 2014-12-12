using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WebSockets
{
    public class HttpSocketClient : SocketClient
    {
        public HttpSocketClient(TcpClient client, WebSocketServer server)
            : base(client, server)
        {

        }
    }
}