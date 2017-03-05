namespace GreatSnooper.Services
{
    using System.Text.RegularExpressions;

    static class RegexService
    {
        public static Regex GenerateRegex(string word)
        {
            return new Regex(@"(" + GetRegexStr(word) + @")", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        private static string GetRegexStr(string str)
        {
            str = Regex.Escape(str)
                  .Replace(@"\.", @".")
                  .Replace(@"\\.", @"\.")
                  .Replace(@"\*", @".*?")
                  .Replace(@"\\.*?", @"\*")
                  .Replace(@"\+", @".+")
                  .Replace(@"\\.+", @"\+")
                  .Replace(@"\\\\", @"\\");
            return str;
        }
    }
}