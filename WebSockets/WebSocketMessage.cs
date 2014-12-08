using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WebSockets
{
    public struct WebSocketMessage
    {
        public bool Finished;
        public bool Reserved1;
        public bool Reserved2;
        public bool Reserved3;
        public byte Opcode;
        public bool Mask;
        public byte PayloadLength;
        public byte[] MaskingKey;
        public List<byte> Data;

        public string DataAsString
        {
            get
            {
                return WebSocketClient.Encoder.GetString(this.Data.ToArray());
            }
        }

        public JObject DataAsJson
        {
            get
            {
                return JObject.Parse(this.DataAsString);
            }
        }
    }
}
