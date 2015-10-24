using System.Text.RegularExpressions;

namespace GreatSnooper.Validators
{
    public class NickNameValidator : AbstractValidator
    {
        private static Regex nickRegex = nickRegex = new Regex(@"^[a-z`]", RegexOptions.IgnoreCase);
        private static Regex nickRegex2 = new Regex(@"^[a-z`][a-z0-9`\-]*$", RegexOptions.IgnoreCase);

        public override string Validate(ref string text)
        {
            if (!nickRegex.IsMatch(text))
                return Localizations.GSLocalization.Instance.NickStartsBad;
            else if (!nickRegex2.IsMatch(text))
                return Localizations.GSLocalization.Instance.NickHasBadChar;

            return string.Empty;
        }
    }
}
