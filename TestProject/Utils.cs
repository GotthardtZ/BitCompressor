using System;
using System.IO;

namespace TestProject
{
    internal class Utils
    {
        public static bool FilesAreEqual(string file1, string file2)
        {
            byte[] content1 = File.ReadAllBytes(file1);
            byte[] content2 = File.ReadAllBytes(file2);
            return((ReadOnlySpan<byte>)content1).SequenceEqual(content2);
        }
    }
}
