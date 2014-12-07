using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSockets
{
    public class ByteAsBits
    {
        List<bool> data;

        public ByteAsBits(List<bool> data)
        {
            this.data = data;
        }

        public ByteAsBits()
        {
            this.data = new List<bool>();
            for (int i = 0; i < 8; i++)
                this.data.Add(false);
        }

        public bool this[int key]
        {
            get
            {
                return data[key];
            }
            set
            {
                data[key] = value;
            }
        }

        public byte this[int start, int end]
        {
            get
            {
                List<bool> bits = new List<bool>();

                for (int i = start; i < end; i++)
                    bits.Add(this.data[i]);

                bits.Reverse();

                BitArray array = new BitArray(end - start);
                for (int i = 0; i < array.Length; i++)
                    array.Set(i, bits[i]);
                return array.ToByte();
            }
            set
            {
                var b = value.GetBits();
                var length = end - start;

                for (int i = start, j = 0; i < end; i++, j++)
                {
                    this.data[i] = b[j + (8 - length)];
                }
            }
        }

        public ByteAsBits Reverse
        {
            get
            {
                List<bool> temp = new List<bool>();
                foreach (var b in this.data)
                    temp.Add(b);
                temp.Reverse();
                return new ByteAsBits(temp);
            }
        }

        public BitArray BitArray
        {
            get
            {
                BitArray array = new BitArray(data.Count);
                for (int i = 0; i < data.Count; i++)
                    array.Set(i, data[i]);
                return array;
            }
        }

        public byte ToByte()
        {
            return this.Reverse.BitArray.ToByte();
        }
    }
}
