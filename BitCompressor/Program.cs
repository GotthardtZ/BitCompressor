using ArthmeticCoder;
using System;
using System.IO;

namespace BitCompressor
{
    public class Program
    {
        public static string version = "v1";

        static void HowToUse()
        {
            Console.WriteLine("BitCompressor " + version);
            Console.WriteLine();
            Console.WriteLine("Encodes a file bit by bit using an arithmetic encoder with a static bit probability");
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

        static double GetBitProbability(byte[] bytes)
        {
            ulong n0 = 0;
            ulong n1 = 0;
            for (uint i = 0; i < bytes.Length; i++)
            {
                for (int j = 7; j >= 0; j--)
                {
                    uint bit = (uint)(bytes[i] >> j) & 1;
                    n0 += bit == 0 ? 1u : 0u;
                    n1 += bit == 1 ? 1u : 0u;
                }
            }
            return (n1 + 0.5d) / (n0 + n1 + 1.0d);
        }

        const double M = uint.MaxValue + 1.0d; // 2^32
        static byte[] Encode(byte[] input)
        {
            double bitProbability = GetBitProbability(input);
            Console.WriteLine("Probability of bit=1 is " + bitProbability);
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter bw = new BinaryWriter(ms);
            Encoder encoder = new Encoder(bw);
            uint filesize = (uint)input.Length;
            bw.Write(filesize);

            uint p = (uint)(bitProbability * M);
            if (p == 0) p = 1;
            bw.Write(p);

            for (int i = 0; i < filesize; i++)
            {
                byte b = input[i];
                for (int j = 7; j >= 0; j--)
                {
                    uint bit = (uint)(b >> j) & 1;
                    encoder.Encode(bit, p);
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
            uint p = br.ReadUInt32();
            Console.WriteLine("Probability of bit=1 is " + (p/M));
            Decoder decoder = new Decoder(br);

            byte b = 0;
            for (int i = 0; i < origFileSize; i++)
            {
                for (int j = 7; j >= 0; j--)
                {
                    uint bit = decoder.Decode(p);
                    b = (byte)((b << 1) | bit);
                }
                writer.WriteByte(b);
            }
            return writer.ToArray();
        }

    }
}
