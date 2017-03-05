namespace GreatSnooper.Validators
{
    public class NotEmptyValidator : AbstractValidator
    {
        public override string Validate(ref string text)
        {
            text = text.Trim();
            if (text.Length == 0)
            {
                return Localizations.GSLocalization.Instance.EmptyErrorMessage;
            }
            return string.Empty;
        }
    }
}