using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;


namespace WebSockets
{
    class WebSocketServer
    {
        public Action<WebSocketClient> onClientJoined;
        private TcpListener listener;
        private List<WebSocketClient> clients = new List<WebSocketClient>();

        public Action<string> toWrite;

        void TcpClientJoined(IAsyncResult res)
        {
            var client = listener.EndAcceptTcpClient(res);
            clients.Add(new WebSocketClient(client, this));
            listener.BeginAcceptTcpClient(this.TcpClientJoined, null);
        }

        public WebSocketServer()
        {
            Thread thread = new Thread((object obj) =>
            {
                listener = new TcpListener(new IPEndPoint(IPAddress.Any, 9090));
                listener.Start();

                listener.BeginAcceptTcpClient(this.TcpClientJoined, null);
            });

            thread.Start();
        }
    }
}
