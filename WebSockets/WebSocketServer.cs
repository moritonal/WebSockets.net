using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.IO;

namespace WebSockets
{
    public class WebSocketServer
    {
        private TcpListener listener;

        public ConcurrentDictionary<Guid, SocketClient> Clients = new ConcurrentDictionary<Guid, SocketClient>();

        public Action<string> onLog;
		public Action<WebSocketClient> onClientJoined;
        public Action<HttpSocketClient> onHttpRequest;

        public int Port;

        public WebSocketServer(int Port)
        {
            this.Port = Port;
        }

        ~WebSocketServer()
        {
            listener.Stop();
        }

		public void Init()
		{
            new Task(() =>
            {
                listener = new TcpListener(new IPEndPoint(IPAddress.Any, this.Port));
                listener.Start();

                listener.BeginAcceptTcpClient(this.TcpClientJoined, null);

                Timer t = new Timer(
                    (object obj) =>
                    {
                        Clients.Values.Select(x => x as WebSocketClient).Where(x => x.IsNotNull()).ToList().ForEach(x => x.SendPacket("", 9));
                    },
                    null, 0, 1000);

            }).Start();
		}

        

        void TcpClientJoined(IAsyncResult res)
        {
            lock (Clients)
            {
                var socketClient = new SocketClient(listener.EndAcceptTcpClient(res), this);

                /*SslStream stream = new SslStream(socketClient.tcpClient.GetStream());

                var a = new X509Certificate2(Environment.CurrentDirectory + "\\..\\..\\..\\Certs\\server.pfx", "test");
                stream.AuthenticateAsServer(a, false, System.Security.Authentication.SslProtocols.Tls12, false);

                socketClient.networkStream = stream;
                
                var temp = WebSocketClient.Encoder.GetBytes("hello");
                socketClient.networkStream.Write(temp, 0, temp.Length);*/

                socketClient.networkStream = socketClient.tcpClient.GetStream();

                //Read first header
                socketClient.ReadHeader();

                SocketClient protcolClient = null;

                //Work out correct client
                switch (socketClient.Protocol)
                {
                    case "http":
                        protcolClient = new HttpSocketClient(socketClient, this) { handshake = socketClient.handshake };
                        break;
                    case "ws":
                        protcolClient = new WebSocketClient(socketClient, this) { handshake = socketClient.handshake };
                        break;
                }

                //Handshake
                if (protcolClient.IsNotNull())
                {
                    protcolClient.Handshake();

                    var g = Guid.NewGuid();
                    Clients.TryAdd(g, protcolClient);

                    protcolClient.onSocketClosed = () =>
                    {
                        SocketClient c;
                        this.Clients.TryRemove(g, out c);
                    };

                    switch (protcolClient.Protocol)
                    {
                        case "http":
                            if (this.onHttpRequest != null)
                                this.onHttpRequest(protcolClient as HttpSocketClient);
                            protcolClient.Close();
                            break;
                        case "ws":
                            if (this.onClientJoined != null)
                                this.onClientJoined(protcolClient as WebSocketClient);
                            break;
                    }

                    protcolClient.Start();
                }

                listener.BeginAcceptTcpClient(this.TcpClientJoined, null);
            }
        }

		public void Log(string msg)
		{
			if (this.onLog != null)
				onLog(msg);
		}
    }
}
