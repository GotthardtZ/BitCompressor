using System.IO;
using Xunit;

namespace TestProject
{
    public class UnitTest
    {
        [Theory]

        [InlineData("BenchmarkFiles/Calgary/bib")]
        [InlineData("BenchmarkFiles/Calgary/book1")]
        [InlineData("BenchmarkFiles/Calgary/book2")]
        [InlineData("BenchmarkFiles/Calgary/geo")]
        [InlineData("BenchmarkFiles/Calgary/news")]
        [InlineData("BenchmarkFiles/Calgary/obj1")]
        [InlineData("BenchmarkFiles/Calgary/obj2")]
        [InlineData("BenchmarkFiles/Calgary/paper1")]
        [InlineData("BenchmarkFiles/Calgary/paper2")]
        [InlineData("BenchmarkFiles/Calgary/pic")]
        [InlineData("BenchmarkFiles/Calgary/progc")]
        [InlineData("BenchmarkFiles/Calgary/progl")]
        [InlineData("BenchmarkFiles/Calgary/progp")]
        [InlineData("BenchmarkFiles/Calgary/trans")]

        [InlineData("BenchmarkFiles/Cantenbury/alice29.txt")]
        [InlineData("BenchmarkFiles/Cantenbury/asyoulik.txt")]
        [InlineData("BenchmarkFiles/Cantenbury/cp.html")]
        [InlineData("BenchmarkFiles/Cantenbury/fields.c")]
        [InlineData("BenchmarkFiles/Cantenbury/grammar.lsp")]
        [InlineData("BenchmarkFiles/Cantenbury/kennedy.xls")]
        [InlineData("BenchmarkFiles/Cantenbury/lcet10.txt")]
        [InlineData("BenchmarkFiles/Cantenbury/plrabn12.txt")]
        [InlineData("BenchmarkFiles/Cantenbury/ptt5")]
        [InlineData("BenchmarkFiles/Cantenbury/sum")]
        [InlineData("BenchmarkFiles/Cantenbury/xargs.1")]

        public void RunTest(string filePath)
        {
            string orig = filePath;
            string encoded = filePath + ".bin";
            string decoded = filePath + ".bin.decompressed";

            var sw = System.Diagnostics.Stopwatch.StartNew();
            BitCompressor.Program.Main(new string[] { "e", orig, encoded });
            var compressionTime = sw.ElapsedMilliseconds;

            sw.Restart();
            BitCompressor.Program.Main(new string[] { "d", encoded, decoded });
            var decompressionTime = sw.ElapsedMilliseconds;

            Assert.True(Utils.FilesAreEqual(orig, decoded));

            var compressedSize = new FileInfo(encoded).Length;
            File.Delete(encoded);
            File.Delete(decoded);

            Utils.WriteResultsToFile(BitCompressor.Program.version, filePath, compressedSize, compressionTime, decompressionTime);
        }
    }
}
