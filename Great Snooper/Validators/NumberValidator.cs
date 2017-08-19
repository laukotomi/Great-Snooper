namespace GreatSnooper.Validators
{
    public class NumberValidator : AbstractValidator
    {
        public override string Validate(ref string text)
        {
            int temp;
            text = text.Trim();
            if (!int.TryParse(text, out temp))
            {
                return Localizations.GSLocalization.Instance.RequiredNumberErrorMessage;
            }
            return string.Empty;
        }
    }
}