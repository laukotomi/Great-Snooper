using System;
using System.Text.RegularExpressions;

namespace MySnooper
{
    public abstract class MyValidator
    {
        public abstract string Validate(ref string text);
    }

    public class NickNameValidator : MyValidator
    {
        private static Regex nickRegex = nickRegex = new Regex(@"^[a-z`]", RegexOptions.IgnoreCase);
        private static Regex nickRegex2 = new Regex(@"^[a-z`][a-z0-9`\-]*$", RegexOptions.IgnoreCase);

        public override string Validate(ref string text)
        {
            if (!nickRegex.IsMatch(text))
            {
                return "Your nickname should begin with a character" + Environment.NewLine + "of the English aplhabet or with ` character!";
            }
            else if (!nickRegex2.IsMatch(text))
            {
                return "Your nickname contains one or more" + Environment.NewLine + "forbidden characters! Use characters from" + Environment.NewLine + "the English alphabet, numbers, - or `!";
            }

            return string.Empty;
        }
    }

    public class GSVersionValidator : MyValidator
    {
        private readonly Regex gsVersionRegex = new Regex(@"^v[1-9]\.[0-9]\.?[0-9]?$");

        public override string Validate(ref string text)
        {
            text = WormNetCharTable.RemoveNonWormNetChars(text.Trim());
            string[] words = text.ToLower().Split(new char[] { ' ' });

            if (words.Length == 3 && words[0] == "great" && words[1] == "snooper" && gsVersionRegex.IsMatch(words[2]) && words[2] != "v" + App.GetVersion())
                return "No trolling please!";
            return string.Empty;
        }
    }

    public class IRCTextValidator : MyValidator
    {
        public override string Validate(ref string text)
        {
            text = WormNetCharTable.RemoveNonWormNetChars(text.Trim());
            return string.Empty;
        }
    }

    public class NotEmptyValidator : MyValidator
    {
        public override string Validate(ref string text)
        {
            text = text.Trim();
            if (text.Length == 0)
                return "This text cannot be empty!";
            return string.Empty;
        }
    }
}
