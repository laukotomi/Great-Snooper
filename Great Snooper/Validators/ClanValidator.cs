namespace GreatSnooper.Validators
{
    using System.Text.RegularExpressions;

    public class ClanValidator : AbstractValidator
    {
        private static Regex clanRegex = new Regex(@"^[a-z0-9]*$", RegexOptions.IgnoreCase);

        public override string Validate(ref string text)
        {
            if (!clanRegex.IsMatch(text))
            {
                return Localizations.GSLocalization.Instance.ClanHasBadChar;
            }

            return string.Empty;
        }
    }
}