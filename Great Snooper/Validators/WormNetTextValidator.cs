namespace GreatSnooper.Validators
{
    using GreatSnooper.Helpers;

    public class WormNetTextValidator : AbstractValidator
    {
        public override string Validate(ref string text)
        {
            text = WormNetCharTable.RemoveNonWormNetChars(text.Trim());
            return string.Empty;
        }
    }
}