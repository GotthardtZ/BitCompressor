using ArthmeticCoder;
using System;
using System.IO;

namespace BitCompressor
{
    public class Program
    {
        public static string version = "v3";

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

            Stat[] stats = new Stat[255];
            for (int i = 0; i < stats.Length; i++)
                stats[i] = new Stat();

            for (int i = 0; i < filesize; i++)
            {
                byte b = input[i];
                uint c0 = 1;
                for (int j = 7; j >= 0; j--)
                {
                    var p1 = stats[c0 - 1].p;
                    uint p = (uint)(p1 * M);
                    if (p == 0) p = 1;

                    uint bit = (uint)(b >> j) & 1;
                    encoder.Encode(bit, p);

                    stats[c0 - 1].Update(bit);
                    c0 <<= 1;
                    c0 += bit;
                }
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

            Stat[] stats = new Stat[255];
            for (int i = 0; i < stats.Length; i++)
                stats[i] = new Stat();

            byte b = 0;
            for (int i = 0; i < origFileSize; i++)
            {
                uint c0 = 1;
                for (int j = 7; j >= 0; j--)
                {
                    var p1 = stats[c0 - 1].p;
                    uint p = (uint)(p1 * M);
                    if (p == 0) p = 1;

                    uint bit = decoder.Decode(p);
                    b = (byte)((b << 1) | bit);

                    stats[c0 - 1].Update(bit);
                    c0 <<= 1;
                    c0 += bit;
                }
                writer.WriteByte(b);
            }
            return writer.ToArray();
        }

    }
}
