
namespace LasUtility.Tests
{
    internal static class Utils
    {
        internal static bool FileCompare(string sFile1, string sFile2)
        {
            try
            {
                using var reader1 = File.ReadLines(sFile1).GetEnumerator();
                using var reader2 = File.ReadLines(sFile2).GetEnumerator();

                while (reader1.MoveNext() && reader2.MoveNext())
                {
                    if (NormalizeLineEndings(reader1.Current) != NormalizeLineEndings(reader2.Current))
                    {
                        return false;
                    }
                }

                // Ensuring both files are completely read
                return !reader1.MoveNext() && !reader2.MoveNext();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error comparing files: {ex.Message}");
                return false;
            }
        }

        private static string NormalizeLineEndings(string text) => text.Replace("\r\n", "\n");

    }
}
