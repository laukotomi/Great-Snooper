namespace GreatSnooper.Validators
{
    using GreatSnooper.IRC;

    public class WormNetTextValidator : AbstractValidator
    {
        public override string Validate(ref string text)
        {
            text = WormNetCharTable.Instance.RemoveNonWormNetChars(text.Trim());
            return string.Empty;
        }
    }
}