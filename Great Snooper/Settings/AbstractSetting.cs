namespace GreatSnooper.Settings
{
    using GalaSoft.MvvmLight;

    public abstract class AbstractSetting : ObservableObject
    {
        public string SettingName { get; private set; }

        public string Text { get; private set; }

        public AbstractSetting(string settingName, string text)
        {
            this.SettingName = settingName;
            this.Text = text;
        }
    }
}