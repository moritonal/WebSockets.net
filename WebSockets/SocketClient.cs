using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WebSockets
{
    public class SocketClient
    {
        public TcpClient tcpClient;
        public WebSocketServer Parent;
        public WebSocketHandshake handshake;
        private Stream networkStream;
        public Action onSocketClosed = null;

        public Socket Socket
        {
            get
            {
                return this.tcpClient.Client;
            }
            set
            {
                this.tcpClient.Client = value;
            }
        }

        public SocketClient(TcpClient client, WebSocketServer parent)
        {
            this.tcpClient = client;
            this.Stream = this.tcpClient.GetStream();
            this.Parent = parent;
        }

        public Stream Stream
        {
            get
            {
                return this.networkStream;
            }
            set
            {
                this.networkStream = value;
            }
        }

        public static Encoding Encoder
        {
            get
            {
                return Encoding.UTF8;
            }
        }

        public virtual void Close()
        {
            if (this.onSocketClosed.IsNotNull())
                this.onSocketClosed();

            if (this.Socket.IsNotNull())
            {
                this.Socket.Close();
                this.Socket = null;
            }
        }

        public byte[] Recieve(SocketFlags flags)
        {
            byte[] buffer = new byte[2048];
            var i = this.Socket.Receive(buffer, flags);
            return buffer.Take(i).ToArray();
        }

        public byte[] Recieve(long size = 2048)
        {
            byte[] buffer = new byte[size];
            var i = this.Stream.Read(buffer, 0, (int)size);
            return buffer.Take(i).ToArray();
        }

        public bool Write(byte[] msg)
        {
            try
            {
                if (this.Socket.IsNotNull())
                {
                    this.Stream.Write(msg, 0, msg.Length);
                    return true;
                }
                else
                    return false;
            }
            catch (SocketException)
            {
                return false;
            }
        }

        public bool Write(string str)
        {
            if (str != null)
                return this.Write(SocketClient.Encoder.GetBytes(str));
            return false;
        }

        public string Protocol
        {
            get
            {
                if (handshake.IsNotNull())
                    return handshake.Protocol;
                return null;
            }
        }

        public static implicit operator TcpClient(SocketClient client)
        {
            return client.tcpClient;
        }

        public void ReadHeader()
        {
            var buffer = this.Recieve();
            if (buffer.Length > 0)
            {
                handshake = new WebSocketHandshake(SocketClient.Encoder.GetString(buffer));
            }
        }

        public virtual void PerformHandshake()
        {

        }

        public virtual void Start()
        {

        }
    }
}
