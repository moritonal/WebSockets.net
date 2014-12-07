﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;

namespace WebSockets
{
    class WebSocketServer
    {
        public Action<WebSocketClient> onClientJoined;
        private TcpListener listener;

        public ConcurrentDictionary<Guid, WebSocketClient> Clients = new ConcurrentDictionary<Guid, WebSocketClient>();
        public Action<string> onLog;

        public int Port;

        public WebSocketServer(int Port)
        {
            this.Port = Port;
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
            lock (Clients)
            {
                var webSocketClient = new WebSocketClient(listener.EndAcceptTcpClient(res), this);

                Clients.TryAdd(Guid.NewGuid(), webSocketClient);

                if (this.onClientJoined != null)
                    this.onClientJoined(webSocketClient);

                webSocketClient.Init();

                listener.BeginAcceptTcpClient(this.TcpClientJoined, null);
            }
        }

        internal void Init()
        {
            listener = new TcpListener(new IPEndPoint(IPAddress.Any, this.Port));
            listener.Start();

            listener.BeginAcceptTcpClient(this.TcpClientJoined, null);
        }
    }
}