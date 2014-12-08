using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace WebSockets
{
    public class WebSocketHandshake
    {
        public string method;
        public string request;
        public string protocol;

        public Dictionary<string, string> headers = new Dictionary<string, string>();

        public WebSocketHandshake(string str)
        {
            if (str != "")
            {
                var _headers = str.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList().Select(x => x.Split(':'));
                var _getHeader = _headers.Where(x => x[0].StartsWith("GET"));
                var getHeader = _getHeader.FirstOrDefault().First().Split(' ');

                method = getHeader[0];
                request = getHeader[1];
                protocol = getHeader[2];

                foreach (var header in _headers.Where(x => x.Count() > 1))
                {
                    headers[header[0].Trim().ToLower()] = String.Join(":", header.Skip(1).ToArray()).Trim();
                }
            }
        }

        public string this[string key]
        {
            get
            {
                if (this.headers.ContainsKey(key))
                    return this.headers[key];
                else
                    return null;
            }
        }
    }

    public class SocketClient
    {
        public TcpClient tcpClient;
        public WebSocketServer Parent;
        public WebSocketHandshake handshake;

        public Socket Socket
        {
            get
            {
                return this.tcpClient.Client;
            }
        }

        public SocketClient(TcpClient client, WebSocketServer parent)
        {
            this.tcpClient = client;
            this.Parent = parent;
        }

        ~SocketClient()
        {
            this.tcpClient.Close();
        }

        public static Encoding Encoder
        {
            get
            {
                return Encoding.UTF8;
            }
        }

        public void Close()
        {
            this.Socket.Close();
        }

        public byte[] Recieve()
        {
            byte[] buffer = new byte[2048];
            var num = this.Socket.Receive(buffer);

            List<byte> data = new List<byte>();

            for (int i = 0; i < num; i++)
                data.Add(buffer[i]);

            return data.ToArray();
        }

        public bool Write(byte[] msg)
        {
            try
            {
                this.Socket.Send(msg);
                return true;
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
                var connectionMode = handshake["connection"];
                if (connectionMode.IsNotNull())
                {
                    if (connectionMode.ToLower() == "keep-alive")
                    {
                        return "http";
                    }
                    else if (connectionMode.ToLower() == "upgrade")
                    {
                        var upgradeMode = handshake["upgrade"];
                        if (upgradeMode.IsNotNull() && upgradeMode.ToLower() == "websocket")
                        {
                            return "ws";
                        }
                    }
                }

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

        public virtual void Handshake()
        {

        }

        public virtual void Start()
        {

        }
    }

    public class HttpSocketClient : SocketClient
    {
        public HttpSocketClient(TcpClient client, WebSocketServer server)
            : base(client, server)
        {

        }
    }

    public class WebSocketClient : SocketClient
    {
        static private string guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        static SHA1 sha1 = SHA1CryptoServiceProvider.Create();
        
        public Action<WebSocketMessage> onMessageRecieved = null;
        private byte[] globalBuffer = new byte[2048];

        public WebSocketClient(TcpClient client, WebSocketServer server)
            : base(client, server)
        {

        }

        public override void Handshake()
        {
            var key = handshake["sec-websocket-key"];

            var response = "HTTP/1.1 101 Switching Protocols" + Environment.NewLine
                         + "Upgrade: websocket" + Environment.NewLine
                         + "Connection: Upgrade" + Environment.NewLine
                         + "Sec-WebSocket-Accept: " + this.AcceptKey(key) + Environment.NewLine + Environment.NewLine;

            //Finish handshake
            this.Write(response);

            base.Handshake();
        }

        public override void Start()
        {
            BeginRecievePacket();
        }

        public void BeginRecievePacket()
        {
            this.Socket.BeginReceive(globalBuffer, 0, 2048, SocketFlags.None, (IAsyncResult res) =>
            {
                try
                {
                    int amount = this.Socket.EndReceive(res);
                    if (amount > 0)
                    {
                        if (onMessageRecieved != null)
                            this.onMessageRecieved(ProcessPacket(this.globalBuffer.Take(amount).ToArray()));
                    }
                    this.BeginRecievePacket();
                }
                catch (SocketException)
                {
                    return;
                }
            }, null);
        }

        private string AcceptKey(string key)
        {
            string longKey = key + guid;
            byte[] hashBytes = ComputeHash(longKey);
            return Convert.ToBase64String(hashBytes);
        }

        private byte[] ComputeHash(string str)
        {
            return sha1.ComputeHash(SocketClient.Encoder.GetBytes(str));
        }

        public bool SendPacket(string msg)
        {
            List<byte> data = new List<byte>();

            var s = new ByteAsBits();
            {
                s[0] = true;
                s[1] = false;
                s[2] = false;
                s[3] = false;
                s[4, 8] = 1;
            }
            data.Add(s.ToByte());

            s = new ByteAsBits();
            {
                s[0] = false; //Mask
                s[1, 8] = (byte)msg.Length;
            }
            data.Add(s.ToByte());

            //Add payload
            data.AddRange(WebSocketClient.Encoder.GetBytes(msg));

            return this.Write(data.ToArray());
        }

        public WebSocketMessage ProcessPacket(byte[] data)
        {
            WebSocketMessage msg = new WebSocketMessage();

            msg.Finished = data[0].GetBits()[0];
            msg.Reserved1 = data[0].GetBits()[1];
            msg.Reserved2 = data[0].GetBits()[2];
            msg.Reserved3 = data[0].GetBits()[3];

            msg.Opcode = data[0].GetBits()[4, 8];

            msg.Mask = data[1].GetBits()[0];
            msg.PayloadLength = data[1].GetBits()[1, 8];

            if (msg.Mask)
            {
                msg.MaskingKey = new byte[4];
                msg.MaskingKey[0] = data[2];
                msg.MaskingKey[1] = data[3];
                msg.MaskingKey[2] = data[4];
                msg.MaskingKey[3] = data[5];
            }

            msg.Data = new List<byte>();

            //Parse payload
            for (int i = 0; i < msg.PayloadLength; i++)
                msg.Data.Add(data[6 + i]);

            //De XOR payload
            for (int i = 0; i < msg.PayloadLength; i++)
                msg.Data[i] ^= msg.MaskingKey[i % 4];
            
            return msg;
        }
    }
}
