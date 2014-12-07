using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

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
        private TcpClient client;
        WebSocketServer server;
        static private string guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        public WebSocketClient(TcpClient client, WebSocketServer server)
        {
            this.server = server;
            this.client = client;

            var buffer = this.Recieve();
            WebSocketHandshake handshake = new WebSocketHandshake(this.Encoder.GetString(buffer));

            var key = handshake.headers["Sec-WebSocket-Key"];
            var newKey = this.AcceptKey(key);

            var response = "HTTP/1.1 101 Switching Protocols" + Environment.NewLine
                         + "Upgrade: websocket" + Environment.NewLine
                         + "Connection: Upgrade" + Environment.NewLine
                         + "Sec-WebSocket-Accept: " + newKey + Environment.NewLine + Environment.NewLine;

            this.Write(this.Encoder.GetBytes(response));

            var data = this.RecievePacket();

            this.SendPacket(String.Join("", this.Encoder.GetString(data.data.ToArray())));

            //this.client.Close();
        }
        public byte[] Recieve()
        {
            byte[] buffer = new byte[2048];
            var num = client.Client.Receive(buffer);

            List<byte> data = new List<byte>();

            for (int i = 0; i < num; i++)
                data.Add(buffer[i]);

            return data.ToArray();
        }

        public void Write(byte[] msg)
        {
            client.Client.Send(msg);
        }

        Encoding Encoder
        {
            get
            {
                return Encoding.UTF8;
            }
        }

        private string AcceptKey(string key)
        {
            string longKey = key + guid;
            byte[] hashBytes = ComputeHash(longKey);
            return Convert.ToBase64String(hashBytes);
        }

        static SHA1 sha1 = SHA1CryptoServiceProvider.Create();

        private byte[] ComputeHash(string str)
        {
            return sha1.ComputeHash(System.Text.Encoding.ASCII.GetBytes(str));
        }

        public struct Msg
        {
            public bool FIN;
            public bool RSV1;
            public bool RSV2;
            public bool RSV3;
            public byte Opcode;
            public bool Mask;
            public byte payloadLength;
            public byte[] maskingKey;
            public List<byte> data;
        }

        public void SendPacket(string msg)
        {
            List<byte> data = new List<byte>();

            var s = new ByteAsBits();
            s[0] = true;
            s[1] = false;
            s[2] = false;
            s[3] = false;
            s[4, 8] = 1;

            data.Add(s.ToByte());

            s = new ByteAsBits();

            s[0] = false; //Mask
            s[1, 8] = (byte)msg.Length;

            data.Add(s.ToByte());

            var bb = this.Encoder.GetBytes(msg);

            for (int i = 0; i < msg.Length; i++)
                data.Add(bb[i]);

            /*BitWriter writer = new BitWriter(4 + 4 + 7 + 1 + (msg.Length * 8));
            writer.addBit(true); //FIN
            writer.addBit(false); //RSV1
            writer.addBit(false); //RSV2
            writer.addBit(false); //RSV3
            writer.addByte(1, 4, true); //Opcode
            writer.addByte((byte)msg.Length, 7); //Payload Length
            writer.addBit(false); //Mask

            var bb = this.Encoder.GetBytes(msg);

            for (int i = 0; i < msg.Length; i++)
                writer.addByte(bb[i], 8);*/

            var result = new BitReader(data.ToArray()).ToString();
            var _ = "";
            for (int i = 0; i < result.Length; i++)
                _ += result[i] + ((i + 1) % 4 == 0 ? " " : "");
            this.server.toWrite("Out: " + _ + " - " + msg);

            this.Write(data.ToArray());
        }

        public Msg RecievePacket()
        {
            var data = this.Recieve();

            Msg msg = new Msg();

            BitReader reader = new BitReader(data);

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

            for (int i = 0; i < msg.payloadLength; i++)
            {
                msg.data.Add(data[6 + i]);
            }

            for (int i = 0; i < msg.payloadLength; i++)
            {
                msg.data[i] ^= msg.maskingKey[i % 4];
            }

            var str = this.Encoder.GetString(msg.data.ToArray());

            var _ = "";
            for (int i = 0; i < reader.ToString().Length; i++)
                _ += reader.ToString()[i] + ((i + 1) % 4 == 0 ? " " : "");
            this.server.toWrite("In:  " + _ + " - " + str);

            return msg;
        }
    }

    class BitWriter
    {
        BitArray data;
        int index = 0;

        public BitWriter(int size)
        {
            data = new BitArray(size);
        }

        public void addBit(bool value)
        {
            data.Set(index++, value);
        }

        public void addByte(byte b, int length, bool reverse = false)
        {
            BitArray bb = new BitArray(new byte[] { b });
            BitArray temp = new BitArray(length);
            for (int i = 0; i < length; i++)
                temp.Set(i, bb.Get(i));

            var list = temp.OfType<bool>().ToList();

            if (reverse)
                list.Reverse();

            for (int i = 0; i < length; i++)
                data.Set(index++, list[i]);
        }

        public byte[] getBytes()
        {
            byte[] b = new byte[data.Count];
            data.CopyTo(b, 0);
            return b;
        }

        public override string ToString()
        {
            return String.Join("", data.OfType<bool>().Select(x => x ? "1" : "0").ToArray());
        }
    }

    class BitReader
    {
        BitArray data;
        int index = 0;

        public override string ToString()
        {
            return String.Join("",data.OfType<bool>().Select(x=>x?"1":"0").ToArray());
        }

        public BitReader(byte[] data)
        {
            this.data = new BitArray(data);
        }

        public bool getBit()
        {
            return data.Get(index++);
        }

        public byte getByte(int length = 4, bool bigEnd = false)
        {
            List<bool> bits = new List<bool>();

            for (int i = 0; i < length; i++)
                bits.Add(this.getBit());

            if (bigEnd)
                bits.Reverse();

            BitArray a = new BitArray(bits.ToArray());
            byte[] b = new byte[1];
            a.CopyTo(b, 0);

            return b[0];
        }
    }
}
