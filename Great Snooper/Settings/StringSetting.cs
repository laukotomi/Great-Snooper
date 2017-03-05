namespace GreatSnooper.Settings
{
    using GreatSnooper.Helpers;
    using GreatSnooper.Services;
    using GreatSnooper.Validators;

    public class StringSetting : AbstractSetting
    {
        private IMetroDialogService dialogService;
        private AbstractValidator validator;
        private string _value;

        public StringSetting(string settingName, string text, AbstractValidator validator, IMetroDialogService dialogService)
        : base(settingName, text)
        {
            this.dialogService = dialogService;
            this.validator = validator;
            this._value = SettingsHelper.Load<string>(settingName);
        }

        public string Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (this.validator != null)
                {
                    string error = this.validator.Validate(ref value);
                    if (error != string.Empty)
                    {
                        this.dialogService.ShowDialog(Localizations.GSLocalization.Instance.InvalidValueText, error);
                        return;
                    }
                }

                SettingsHelper.Save(this.settingName, value);

                _value = value;
            }
        }
    }
}