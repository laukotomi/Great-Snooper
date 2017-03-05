namespace GreatSnooper.Validators
{
    using System;
    using System.Text.RegularExpressions;

    using GreatSnooper.Helpers;

    public class GSVersionValidator : AbstractValidator
    {
        private readonly Regex gsVersionRegex = new Regex(@"^v[1-9]+\.[0-9]+\.?[0-9]*$", RegexOptions.IgnoreCase);

        public override string Validate(ref string text)
        {
            text = WormNetCharTable.RemoveNonWormNetChars(text.Trim());
            string[] words = text.Split(new char[] { ' ' });

            if (words.Length == 3 &&
                words[0].Equals("great", StringComparison.OrdinalIgnoreCase) &&
                words[1].Equals("snooper", StringComparison.OrdinalIgnoreCase) &&
                gsVersionRegex.IsMatch(words[2]) &&
                words[2].Equals("v" + App.GetVersion(), StringComparison.OrdinalIgnoreCase) == false)
            {
                return Localizations.GSLocalization.Instance.GSVersionTrolling;
            }
            return string.Empty;
        }
    }
}