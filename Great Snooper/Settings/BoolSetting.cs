namespace GreatSnooper.Settings
{
    using GreatSnooper.Helpers;

    public class BoolSetting : AbstractSetting
    {
        private bool? _value;

        public BoolSetting(string settingName, string text)
        : base(settingName, text)
        {
            this._value = SettingsHelper.Load<bool>(settingName);
        }

        public bool? Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;

                if (value.HasValue)
                {
                    SettingsHelper.Save(settingName, value.Value);
                }
            }
        }
    }
}