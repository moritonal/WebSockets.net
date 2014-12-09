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
    public class WebSocketMessage
    {
        public bool Finished;
        public bool Reserved1;
        public bool Reserved2;
        public bool Reserved3;
        public byte Opcode;
        public bool Mask;
        public long PayloadLength;
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
                try
                {
                    return JObject.Parse(this.DataAsString);
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}
