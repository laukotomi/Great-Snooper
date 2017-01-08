using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GreatSnooper.Services
{
    static class RegexService
    {
        private static string GetRegexStr(string str)
        {
            return Regex.Escape(str)
                .Replace(@"\.", @".")
                .Replace(@"\\.", @"\.")
                .Replace(@"\*", @".*")
                .Replace(@"\\.*", @"\*")
                .Replace(@"\+", @".+")
                .Replace(@"\\.+", @"\+")
                .Replace(@"\\\\", @"\\");
        }

        public static Regex GenerateRegex(string word)
        {
            return new Regex(@"(" + GetRegexStr(word) + @")", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
    }
}
