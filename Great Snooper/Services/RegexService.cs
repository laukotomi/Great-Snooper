using System.Text.RegularExpressions;

namespace GreatSnooper.Services
{
    static class RegexService
    {
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

        public static Regex GenerateRegex(string word)
        {
            return new Regex(@"(" + GetRegexStr(word) + @")", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
    }
}
