namespace GreatSnooper.Settings
{
    using GreatSnooper.Helpers;
    using GreatSnooper.Services;
    using GreatSnooper.Validators;

    public class NumberSetting : AbstractSetting
    {
        private IMetroDialogService dialogService;
        private AbstractValidator validator;
        private int _value;

        public NumberSetting(string settingName, string text, AbstractValidator validator, IMetroDialogService dialogService)
            : base(settingName, text)
        {
            this.dialogService = dialogService;
            this.validator = validator;
            this._value = SettingsHelper.Load<int>(settingName);
        }

        public string Value
        {
            get
            {
                return _value == default(int) ? string.Empty : _value.ToString();
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _value = default(int);
                    SettingsHelper.Save(this.settingName, _value);
                    return;
                }

                if (this.validator != null)
                {
                    string error = this.validator.Validate(ref value);
                    if (error != string.Empty)
                    {
                        this.dialogService.ShowDialog(Localizations.GSLocalization.Instance.InvalidValueText, error);
                        return;
                    }
                }

                _value = int.Parse(value);
                SettingsHelper.Save(this.settingName, _value);
            }
        }
    }
}