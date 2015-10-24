using GreatSnooper.Helpers;
using GreatSnooper.Services;
using GreatSnooper.Validators;

namespace GreatSnooper.Settings
{
    public class StringSetting : AbstractSetting
    {
        private string _value;
        private IMetroDialogService dialogService;
        private AbstractValidator validator;

        public string Value
        {
            get { return _value; }
            set
            {
                string error = this.validator.Validate(ref value);
                if (error != string.Empty)
                {
                    this.dialogService.ShowDialog(Localizations.GSLocalization.Instance.InvalidValueText, error);
                    return;
                }

                SettingsHelper.Save(this.settingName, value);

                _value = value;
            }
        }

        public StringSetting(string settingName, string text, AbstractValidator validator, IMetroDialogService dialogService)
            : base(settingName, text)
        {
            this.dialogService = dialogService;
            this.validator = validator;
            this._value = SettingsHelper.Load<string>(settingName);
        }
    }
}
