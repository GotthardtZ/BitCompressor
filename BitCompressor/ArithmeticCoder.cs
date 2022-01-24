using System;
using System.IO;

namespace ArthmeticCoder
{
    class Encoder
    {
        private uint x1 = 0;
        private uint x2 = uint.MaxValue; // Range, initially [0, 1), scaled by 2^32
        private uint pending_bits = 0;
        private uint B = 0; // a byte from the archive
        private uint bits_in_B = 0;
        private BinaryWriter _bw;

        public Encoder(BinaryWriter bw)
        {
            _bw = bw;
        }

        // write the archive bit by bit
        private void bit_write(uint bit)
        {
            B = (B << 1) + bit;
            bits_in_B++;
            if (bits_in_B == 8)
            {
                _bw.Write((byte)B);
                B = 0;
                bits_in_B = 0;
            }
        }

        private void bit_write_with_pending(uint bit)
        {
            bit_write(bit);
            for (; pending_bits > 0; pending_bits--)
                bit_write(bit ^ 1);
        }

        private void bit_flush()
        {
            do
            {
                bit_write_with_pending(x1 >> 31); // pad pending byte from x1
                x1 <<= 1;
            } while (bits_in_B != 0);
        }

        public void Encode(uint bit, uint p) //p: 32 bit
        {
            uint xmid = x1 + (uint)(((ulong)(x2 - x1) * p) >> 32);
            if (!(xmid >= x1 && xmid < x2)) throw new Exception();
            if (bit != 0) x2 = xmid; else x1 = xmid + 1;
            while (((x1 ^ x2) >> 31) == 0)
            {  // pass equal leading bits of range
                bit_write_with_pending(x2 >> 31);
                x1 <<= 1;
                x2 = (x2 << 1) | 1;
            }
            while (x1 >= 0x40000000 && x2 < 0xC0000000)
            {
                pending_bits++;
                x1 = (x1 << 1) & 0x7FFFFFFF;
                x2 = (x2 << 1) | 0x80000001;
            }
        }
        public void Flush()
        {
            bit_flush();
        }
    }

    class Decoder
    {
        private uint x1 = 0;
        private uint x2 = uint.MaxValue; // Range, initially [0, 1), scaled by 2^32
        private uint x = 0;
        private uint B = 0; // a byte from the archive
        private uint bits_in_B = 0;
        private BinaryReader _br;

        public Decoder(BinaryReader br)
        {
            _br = br;
            for (int i = 0; i < 32; i++)
                x = (x << 1) | bit_read();
        }

        // read the archive bit by bit
        uint bit_read()
        {
            if (bits_in_B == 0)
            {
                if (_br.BaseStream.Position != _br.BaseStream.Length)
                    B = _br.ReadByte();
                else
                    B = 255; // EOF
                bits_in_B = 8;
            }
            bits_in_B--; //7..0
            return (B >> (int)bits_in_B) & 1;
        }

        // Decompress and return next bit 
        public uint Decode(uint p) //p: 32 bit
        {
            uint xmid = x1 + (uint)(((ulong)(x2 - x1) * p) >> 32);
            if (!(xmid >= x1 && xmid < x2)) throw new Exception();
            uint bit = x <= xmid ? 1u : 0u;
            if (bit != 0) x2 = xmid; else x1 = xmid + 1;
            while (((x1 ^ x2) >> 31) == 0)
            {  // pass equal leading bits of range
                x1 <<= 1;
                x2 = (x2 << 1) + 1;
                x = (x << 1) + bit_read();
            }
            while (x1 >= 0x40000000 && x2 < 0xC0000000)
            {
                x1 = (x1 << 1) & 0x7FFFFFFF;
                x2 = (x2 << 1) | 0x80000001;
                x = (x << 1) ^ 0x80000000;
                x += bit_read();
            }
            return bit;
        }
    }
}