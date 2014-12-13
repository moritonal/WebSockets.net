using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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
    public class WebSocketClient : SocketClient
    {
        static private string guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        static SHA1 sha1 = SHA1CryptoServiceProvider.Create();
        
        public Action<WebSocketMessage> onMessageRecieved = null;

        ConcurrentQueue<byte> incomingData = new ConcurrentQueue<byte>();
        CounterResetEvent isDataAvaliable = new CounterResetEvent();
        byte[] buffer = new byte[2048];

        public WebSocketClient(TcpClient client, WebSocketServer server)
            : base(client, server)
        {
            this.Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            this.Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 8192);
            this.Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, 8192);
        }

        public override void PerformHandshake()
        {
            var key = handshake["sec-websocket-key"];

            StringBuilder response = new StringBuilder();
            response.AppendLine("HTTP/1.1 101 Switching Protocols");
            response.AppendLine("Upgrade: websocket");
            response.AppendLine("Connection: Upgrade");
            response.AppendLine("Sec-WebSocket-Accept: " + this.AcceptKey(key));
            response.AppendLine();

            //Finish handshake
            this.Write(response.ToString());

            base.PerformHandshake();
        }

        public override void Start()
        {
            Thread recieveThread = new Thread(BeginRecieveing);
            recieveThread.Start();

            Thread processThread = new Thread(ProcessPacketThread);
            processThread.Start();
            
            Valid = true;
        }

        void RecievedBuffer(IAsyncResult res)
        {
            try
            {
                var recieved = this.Stream.EndRead(res);
                for (int i = 0; i < recieved; i++)
                {
                    incomingData.Enqueue(buffer[i]);
                    isDataAvaliable.Increment();
                }
               
                this.BeginRecieveing();
            }
            catch (IOException)
            {
                this.Close();
            }
            catch (ObjectDisposedException)
            {
                this.Close();
            }
            catch (NullReferenceException)
            {
                this.Close();
            }
            catch (SocketException)
            {
                this.Close();
            }
        }
        
        public void BeginRecieveing()
        {
            this.Stream.BeginRead(buffer, 0, buffer.Length, this.RecievedBuffer, null);
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
                        
                        switch (msg.Opcode)
                        {
                            case 1:
                                this.onMessageRecieved(msg);
                                break;
                            case 8:
                                throw new Exception("Process Failed (Connection Closed)");
                            case 10: //Pong
                                continue;
                            default:
                                throw new Exception("OpCode " + msg.Opcode + " not recognised");
                        }
                    }
                    catch (Exception e)
                    {
                        this.Parent.Log("Exception: " + e.Message);
                        this.Close();
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

        public byte GetByte()
        {
            isDataAvaliable.Decrement();

            byte data = 0;
            if (this.incomingData.TryDequeue(out data))
                return data;
            else
                throw new Exception("Couldn't read from queue");
        }

        public override void Close()
        {
            base.Close();

            isDataAvaliable.Break();
        }

        public bool SendPacket(string msg, byte opCode = 1)
        {
            List<byte> data = new List<byte>();

            var s = new ByteAsBits();
            {
                s[0] = true;
                s[1] = false;
                s[2] = false;
                s[3] = false;
                s[4, 8] = opCode;
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
                throw new Exception("Message wasn't finished");

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

            //DeXOR payload
            for (int i = 0; i < msg.Data.Count; i++)
                msg.Data[i] ^= msg.MaskingKey[i % 4];
            
            return msg;
        }

        public bool Valid
        {
            get;
            private set;
        }
    }
}
