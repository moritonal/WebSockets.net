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
        public Action<string> onLog;

        public WebSocketServer(int port)
        {
            listener = new TcpListener(new IPEndPoint(IPAddress.Any, port));
            listener.Start();

            listener.BeginAcceptTcpClient(this.TcpClientJoined, null);
        }

        public void Log(string msg)
        {
            if (this.onLog != null)
                onLog(msg);
        }

        ~WebSocketServer()
        {
            listener.Stop();
        }

        void TcpClientJoined(IAsyncResult res)
        {
            lock (clients)
            {
                var client = listener.EndAcceptTcpClient(res);
                clients.Add(new WebSocketClient(client, this));

                var webSocketClient = clients.Last();
                if (this.onClientJoined != null)
                    this.onClientJoined(webSocketClient);

                webSocketClient.Init();

                listener.BeginAcceptTcpClient(this.TcpClientJoined, null);
            }
        }
    }
}
