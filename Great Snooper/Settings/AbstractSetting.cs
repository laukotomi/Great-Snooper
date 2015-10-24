
using GalaSoft.MvvmLight;

namespace GreatSnooper.Settings
{
    public abstract class AbstractSetting : ObservableObject
    {
        protected string settingName;
        public string Text { get; private set; }

        public AbstractSetting(string settingName, string text)
        {
            this.settingName = settingName;
            this.Text = text;
        }
    }
}
