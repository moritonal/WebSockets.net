using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSockets
{
    public struct WebSocketMessage
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
}
