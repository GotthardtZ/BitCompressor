using System;
using System.IO;
using System.Linq;

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

        private static readonly object _lockObject = new object();
        public static void WriteResultsToFile(string version, string fileName, long compressedSize, double compressionTime, double decompressionTime)
        {
            string previousVersion = "v" + (Convert.ToInt32(version.Substring(1)) - 1);

            lock (_lockObject)
            {
                const string resultFileName = "../../../results.txt";
                var rows = File.ReadAllLines(resultFileName);
                var index = 
                    rows.Select((row, index) => new { row, index }).FirstOrDefault(x => x.row.StartsWith(version + "\t" + fileName))?.index;
                var previousVersionIndex = 
                    rows.Select((row, index) => new { row, index }).FirstOrDefault(x => x.row.StartsWith(previousVersion + "\t" + fileName))?.index;
                
                var previousCompressedSize = previousVersionIndex.HasValue ? Convert.ToInt64(rows[previousVersionIndex.Value].Split('\t')[2]) : compressedSize;

                var delta = compressedSize - previousCompressedSize;
                var result = version + "\t" + fileName + "\t" + compressedSize + "\t" + delta + "\t" + compressionTime + "\t" + decompressionTime;

                if (index.HasValue)
                {
                    rows[index.Value] = result;
                }
                else
                {
                    var sortedList = rows
                        .ToList()
                        .Append(result)
                        .Select(row => new { row, columns = row.Split('\t') })
                        .Where(x => x.columns[0] != "Version") //skip header
                        .OrderBy(x => Convert.ToInt32(x.columns[0].Substring(1))) //sort by version number
                        .ThenBy(x => x.columns[1]) //then by filename
                        .Select(x => x.row)
                        .ToList();
                    sortedList.Insert(0, "Version\tFile\tCompressedSize\tDelta\tCompressionTime\tDecompressionTime"); //re-add header
                    rows = sortedList.ToArray();
                }
                File.WriteAllLines(resultFileName, rows);
            }
        }
    }
}
