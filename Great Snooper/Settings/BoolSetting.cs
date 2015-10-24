using GreatSnooper.Helpers;

namespace GreatSnooper.Settings
{
    public class BoolSetting : AbstractSetting
    {
        private bool? _value;

        public bool? Value
        {
            get { return _value; }
            set
            {
                _value = value;

                if (value.HasValue)
                    SettingsHelper.Save(settingName, value.Value);
            }
        }

        public BoolSetting(string settingName, string text)
            : base(settingName, text)
        {
            this._value = SettingsHelper.Load<bool>(settingName);
        }
    }
}
