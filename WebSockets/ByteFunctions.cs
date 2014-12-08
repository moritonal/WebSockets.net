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
            BitArray bb = new BitArray(new byte[] { b });
            var _ = bb.ToBools();
            _.Reverse();
            return new ByteAsBits(_);
        }

        static public List<bool> ToBools(this BitArray b)
        {
            return b.OfType<bool>().ToList();
        }

        static public byte ToByte(this BitArray b)
        {
            byte[] _b = new byte[1];
            b.CopyTo(_b, 0);
            return _b[0];
        }

        static public bool IsNull(this object b)
        {
            return b == null;
        }

        static public bool IsNotNull(this object b)
        {
            return !b.IsNull();
        }
    }
}
