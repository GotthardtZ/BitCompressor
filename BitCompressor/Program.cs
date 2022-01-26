using ArthmeticCoder;
using System;
using System.IO;

namespace BitCompressor
{
    public class Program
    {
        public static string version = "v7";

        static void HowToUse()
        {
            Console.WriteLine("BitCompressor " + version);
            Console.WriteLine();
            Console.WriteLine("Encodes a file bit by bit using an arithmetic encoder");
            Console.WriteLine();
            Console.WriteLine("Usage: BitCompressor.exe [e|d] input output");
            Console.WriteLine(" - e: to endode");
            Console.WriteLine(" - d: to dedode");
        }

        public static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                HowToUse();
                return;
            }
            string command = args[0];
            string inputFilename = args[1];
            string outputFilename = args[2];
            try
            {
                if (command == "e")
                {
                    byte[] input = File.ReadAllBytes(inputFilename);
                    byte[] output = Encode(input);
                    File.WriteAllBytes(outputFilename, output);
                    Console.WriteLine($"{inputFilename}\t{input.Length}\t{output.Length}");
                    Console.WriteLine();
                }
                else if (command == "d")
                {
                    byte[] input = File.ReadAllBytes(inputFilename);
                    byte[] output = Decode(input);
                    File.WriteAllBytes(outputFilename, output);
                    Console.WriteLine($"{inputFilename}\t{input.Length}\t{output.Length}");
                    Console.WriteLine();
                }
                else
                {
                    HowToUse();
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        const double M = uint.MaxValue + 1.0d; // 2^32
        static byte[] Encode(byte[] input)
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter bw = new BinaryWriter(ms);
            Encoder encoder = new Encoder(bw);
            uint filesize = (uint)input.Length;
            bw.Write(filesize);

            Stat[] stats0 = new Stat[255];
            Stat[] stats1 = new Stat[255 * 256];
            Stat[] stats2 = new Stat[255 * 256 * 256];

            uint c1 = 0;
            uint c2 = 0;
            for (int i = 0; i < filesize; i++)
            {
                byte b = input[i];
                uint c0 = 1;
                for (int j = 7; j >= 0; j--)
                {
                    uint context0 = (c0 - 1);
                    uint context1 = (c0 - 1) << 8 | c1;
                    uint context2 = (c0 - 1) << 16 | c1 << 8 | c2;
                    var p0 = stats0[context0].p;
                    var p1 = stats1[context1].p;
                    var p2 = stats2[context2].p;

                    var px =
                        stats2[context2].IsMature ? p2 :
                        stats1[context1].IsMature ? p1 : p0;

                    uint p = (uint)(px * M);
                    if (p == 0) p = 1;

                    uint bit = (uint)(b >> j) & 1;
                    encoder.Encode(bit, p);

                    stats0[context0].Update(bit);
                    stats1[context1].Update(bit);
                    stats2[context2].Update(bit);

                    c0 <<= 1;
                    c0 += bit;
                }
                c2 = c1;
                c1 = b; //c0 works, too
            }
            encoder.Flush();
            ms.Flush();
            return ms.ToArray();
        }

        static byte[] Decode(byte[] input)
        {
            using MemoryStream reader = new MemoryStream(input);
            using MemoryStream writer = new MemoryStream();
            using BinaryReader br = new BinaryReader(reader);
            uint origFileSize = br.ReadUInt32();
            Decoder decoder = new Decoder(br);

            Stat[] stats0 = new Stat[255];
            Stat[] stats1 = new Stat[255 * 256];
            Stat[] stats2 = new Stat[255 * 256 * 256];

            byte b = 0;
            uint c1 = 0;
            uint c2 = 0;
            for (int i = 0; i < origFileSize; i++)
            {
                uint c0 = 1;
                for (int j = 7; j >= 0; j--)
                {
                    uint context = (c0 - 1) << 16 | c1 << 8 | c2;
                    uint context0 = (c0 - 1);
                    uint context1 = (c0 - 1) << 8 | c1;
                    uint context2 = (c0 - 1) << 16 | c1 << 8 | c2;
                    var p0 = stats0[context0].p;
                    var p1 = stats1[context1].p;
                    var p2 = stats2[context2].p;

                    var px =
                        stats2[context2].IsMature ? p2 :
                        stats1[context1].IsMature ? p1 : p0;

                    uint p = (uint)(px * M);
                    if (p == 0) p = 1;

                    uint bit = decoder.Decode(p);
                    b = (byte)((b << 1) | bit);

                    stats0[context0].Update(bit);
                    stats1[context1].Update(bit);
                    stats2[context2].Update(bit);

                    c0 <<= 1;
                    c0 += bit;
                }
                c2 = c1;
                c1 = b; //c0 works, too
                writer.WriteByte(b);
            }
            return writer.ToArray();
        }

    }
}
