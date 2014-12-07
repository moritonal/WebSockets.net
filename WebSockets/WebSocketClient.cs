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
    class WebSocketHandshake
    {
        public string request;
        public Dictionary<string, string> headers = new Dictionary<string, string>();

        public WebSocketHandshake(string str)
        {
            var _headers = str.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList().Select(x => x.Split(':'));
            var _getHeader = _headers.Where(x => x[0].StartsWith("GET"));
            var getHeader = _getHeader.FirstOrDefault().First().Split(' ');

            request = getHeader[1];

            foreach (var header in _headers.Where(x => x.Count() > 1))
            {
                headers[header[0].Trim()] = header[1].Trim();
            }
        }
    }

    class WebSocketClient
    {
        static private string guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        static SHA1 sha1 = SHA1CryptoServiceProvider.Create();

        public static Encoding Encoder
        {
            get
            {
                return Encoding.UTF8;
            }
        }

        private TcpClient tcpClient;
        public WebSocketServer Parent;
        public Action<WebSocketMessage> onMessageRecieved;
        private byte[] globalBuffer = new byte[2048];

        public Socket Socket
        {
            get
            {
                return this.tcpClient.Client;
            }
        }

        public WebSocketClient(TcpClient client, WebSocketServer server)
        {
            this.Parent = server;
            this.tcpClient = client;
        }

        ~WebSocketClient()
        {
            this.tcpClient.Close();
        }

        public void Init()
        {
            InitHandshake();

            BeginRecievePacket();
        }

        private void InitHandshake()
        {
            var buffer = this.Recieve();

            WebSocketHandshake handshake = new WebSocketHandshake(WebSocketClient.Encoder.GetString(buffer));

            var key = handshake.headers["Sec-WebSocket-Key"];

            var response = "HTTP/1.1 101 Switching Protocols" + Environment.NewLine
                         + "Upgrade: websocket" + Environment.NewLine
                         + "Connection: Upgrade" + Environment.NewLine
                         + "Sec-WebSocket-Accept: " + this.AcceptKey(key) + Environment.NewLine + Environment.NewLine;

            //Finish handshake
            this.Write(WebSocketClient.Encoder.GetBytes(response));
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

        private string AcceptKey(string key)
        {
            string longKey = key + guid;
            byte[] hashBytes = ComputeHash(longKey);
            return Convert.ToBase64String(hashBytes);
        }

        private byte[] ComputeHash(string str)
        {
            return sha1.ComputeHash(System.Text.Encoding.ASCII.GetBytes(str));
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

            msg.FIN = data[0].GetBits()[0];
            msg.RSV1 = data[0].GetBits()[1];
            msg.RSV2 = data[0].GetBits()[2];
            msg.RSV3 = data[0].GetBits()[3];

            msg.Opcode = data[0].GetBits()[4, 8];

            msg.Mask = data[1].GetBits()[0];
            msg.payloadLength = data[1].GetBits()[1, 8];

            if (msg.Mask)
            {
                msg.maskingKey = new byte[4];
                msg.maskingKey[0] = data[2];
                msg.maskingKey[1] = data[3];
                msg.maskingKey[2] = data[4];
                msg.maskingKey[3] = data[5];
            }

            msg.data = new List<byte>();

            //Parse payload
            for (int i = 0; i < msg.payloadLength; i++)
                msg.data.Add(data[6 + i]);

            //De XOR payload
            for (int i = 0; i < msg.payloadLength; i++)
                msg.data[i] ^= msg.maskingKey[i % 4];
            
            return msg;
        }
    }
}
