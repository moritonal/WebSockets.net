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
using System.Security.Authentication;

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

                socketClient.networkStream = socketClient.tcpClient.GetStream();

                byte[] originalRequest = new byte[2048];// socketClient.Recieve();

                var possibleHandshake = new WebSocketHandshake(SocketClient.Encoder.GetString(originalRequest));

                if (!possibleHandshake.Valid)
                {
                    //If the request wasn't valid, then let's try see if it's a TslStream
                    SslStream sslStream = new SslStream(socketClient.networkStream, false);
                    var a = new X509Certificate2(Environment.CurrentDirectory + "\\..\\..\\..\\Certs\\server.pfx", "test");
                    sslStream.AuthenticateAsServer(a, false, SslProtocols.Tls, false);
                    sslStream.ReadTimeout = 5000;
                    sslStream.WriteTimeout = 5000;
                    socketClient.networkStream = sslStream;

                    originalRequest = socketClient.Recieve();
                    possibleHandshake = new WebSocketHandshake(SocketClient.Encoder.GetString(originalRequest));
                }
                else
                {
                    //socketClient.networkStream = socketClient.tcpClient.GetStream();
                }

                socketClient.handshake = possibleHandshake;

                //Read first header
                //socketClient.ReadHeader();

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
