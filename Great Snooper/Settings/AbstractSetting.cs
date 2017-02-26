namespace GreatSnooper.Settings
{
    using GalaSoft.MvvmLight;

    public abstract class AbstractSetting : ObservableObject
    {
        protected string settingName;

        public AbstractSetting(string settingName, string text)
        {
            this.settingName = settingName;
            this.Text = text;
        }

        public string Text
        {
            get;
            private set;
        }
    }
}