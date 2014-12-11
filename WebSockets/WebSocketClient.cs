using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
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
            if (this.onSocketClosed.IsNotNull())
                this.onSocketClosed();

            if (this.Socket.IsNotNull())
            {
                this.Socket.Close();
                this.Socket = null;
            }
        }

        public byte[] Recieve(long size = 2048)
        {
            if ((int)size != size)
            {
                return null;
            }

            byte[] buffer = new byte[size];
            var num = this.Socket.Receive(buffer, (int)size, SocketFlags.None);

            return buffer.Take(num).ToArray();
        }

        public bool Write(byte[] msg)
        {
            try
            {
                if (this.Socket.IsNotNull())
                {
                    this.Socket.Send(msg);
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

        ConcurrentQueue<byte> incomingData = new ConcurrentQueue<byte>();

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
            Thread recieveThread = new Thread(BeginRecieveing);
            recieveThread.Start();

            Thread processThread = new Thread(ProcessPacketThread);
            processThread.Start();
        }

        int count = 0;
        int processed = 0;
        
        public void BeginRecieveing()
        {
            while (true)
            {
                byte[] data = new byte[4096];
                SocketError err = SocketError.Success;
                int recieved = this.Socket.Receive(data, 0, 2048, SocketFlags.None, out err);
                if (err == SocketError.Success)
                {
                    for (int i = 0; i < recieved; i++)
                        incomingData.Enqueue(data[i]);
                    count += recieved;
                }
                else
                {
                    this.Close();
                    this.Parent.Log("Socket Closed (" + err.ToString() + ")" + ": " + count + ":" + processed);
                    return;
                }
            }
        }

        public void ProcessPacketThread()
        {
            while (true)
            {
                if (onMessageRecieved != null)
                {
                    try
                    {
                        var msg = ProcessPacket();
                        if (msg.Opcode == 1)
                        {
                            this.onMessageRecieved(msg);
                        }
                        else if (msg.Opcode == 8)
                        {
                            this.Parent.Log("Closed Connection");
                            this.Close();
                            return;
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }
                    catch (Exception e)
                    {
                        this.Parent.Log(e.Message);
                        return;
                    }
                }
            }
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

                if (msg.Length > UInt16.MaxValue)
                {
                    //UNTESTED
                    s[1, 8] = (byte)255;

                    data.Add(s.ToByte());

                    var __a = new ByteAsBits(new BitArray(new int[] { msg.Length }).ToBools()).Reverse;

                    var b = __a[0, 8];
                    var c = __a[8, 16];
                    var d = __a[16, 24];
                    var e = __a[24, 32];

                    data.Add(b.GetBits().ToByte());
                    data.Add(c.GetBits().ToByte());
                    data.Add(d.GetBits().ToByte());
                    data.Add(e.GetBits().ToByte());

                }
                else if (msg.Length > 126)
                {
                    s[1, 8] = (byte)254;

                    data.Add(s.ToByte());

                    var __a = new ByteAsBits(new BitArray(new int[] { msg.Length }).ToBools()).Reverse;

                    var b = __a[0, 8];
                    var c = __a[8, 16];
                    var d = __a[16, 24];
                    var e = __a[24, 32];

                    data.Add(d.GetBits().ToByte());
                    data.Add(e.GetBits().ToByte());
                }
                else
                {
                    s[1, 8] = (byte)msg.Length;
                    data.Add(s.ToByte());
                }
            }

            //Add payload
            data.AddRange(WebSocketClient.Encoder.GetBytes(msg));

            return this.Write(data.ToArray());
        }

        public byte GetByte()
        {
            byte data = 0;

            while (!this.incomingData.TryDequeue(out data))
            {
                Thread.Yield();

                if (this.Socket.IsNull())
                    throw new Exception();
            }

            processed++;

            return data;
        }

        public WebSocketMessage ProcessPacket()
        {
            WebSocketMessage msg = new WebSocketMessage();

            int offset = 0;

            byte data = this.GetByte();

            msg.Finished = data.GetBits()[0];
            msg.Reserved1 = data.GetBits()[1];
            msg.Reserved2 = data.GetBits()[2];
            msg.Reserved3 = data.GetBits()[3];

            if (!msg.Finished)
                throw new Exception();

            msg.Opcode = data.GetBits()[4, 8];

            data = this.GetByte();

            msg.Mask = data.GetBits()[0];
            msg.PayloadLength = data.GetBits()[1, 8];

            if (msg.PayloadLength == 126)
            {
                var a = this.GetByte().GetBits();
                var b = this.GetByte().GetBits();

                ushort bb = (ushort)((a.ToByte() << 8) | b.ToByte());

                msg.PayloadLength = bb;
            }
            else if (msg.PayloadLength == 127)
            {
                var a = this.GetByte().GetBits();
                var b = this.GetByte().GetBits();
                var c = this.GetByte().GetBits();
                var d = this.GetByte().GetBits();
                var e = this.GetByte().GetBits();
                var f = this.GetByte().GetBits();
                var g = this.GetByte().GetBits();
                var h = this.GetByte().GetBits();

                ulong cc = (ulong)(
                    (a.ToByte() << 56)|
                    (b.ToByte() << 48)|
                    (c.ToByte() << 40)|
                    (d.ToByte() << 32)|
                    (e.ToByte() << 24)|
                    (f.ToByte() << 16)|
                    (g.ToByte() << 8) | 
                    h.ToByte());
                msg.PayloadLength = (long)cc;
            }

            if (msg.Mask)
            {
                msg.MaskingKey = new byte[4];
                msg.MaskingKey[0] = this.GetByte();
                msg.MaskingKey[1] = this.GetByte();
                msg.MaskingKey[2] = this.GetByte();
                msg.MaskingKey[3] = this.GetByte();
            }

            msg.Data = new List<byte>();

            for (int i = offset; i < msg.PayloadLength; i++)
                msg.Data.Add(this.GetByte());

            /*if (msg.Data.Count < msg.PayloadLength)
            {
                var missingData = this.Recieve(msg.PayloadLength - msg.Data.Count);
                if (missingData.IsNotNull())
                    msg.Data.AddRange(missingData);
                else
                    return null;
            }*/

            //De XOR payload
            for (int i = 0; i < msg.Data.Count; i++)
                msg.Data[i] ^= msg.MaskingKey[i % 4];
            
            return msg;
        }
    }
}
