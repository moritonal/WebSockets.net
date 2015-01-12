using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSockets
{
    static class ByteFunctions
    {
        static public ByteAsBits GetBits(this byte b)
        {
            BitArray bitArray = new BitArray(new byte[] { b });
            var _ = bitArray.ToBools();
            _.Reverse();
            return new ByteAsBits(_);
        }

        static public List<bool> ToBools(this BitArray bitArray)
        {
            return bitArray.OfType<bool>().ToList();
        }

        static public byte ToByte(this BitArray bitArray)
        {
            byte[] _b = new byte[1];
            bitArray.CopyTo(_b, 0);
            return _b[0];
        }

        static public bool IsNull(this object bitArray)
        {
            return bitArray == null;
        }

        static public bool IsNotNull(this object bitArray)
        {
            return !bitArray.IsNull();
        }
    }
}
