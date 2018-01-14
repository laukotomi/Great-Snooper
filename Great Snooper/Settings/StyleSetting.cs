namespace GreatSnooper.Settings
{
    using System.Windows.Input;

    using GalaSoft.MvvmLight.Command;

    using GreatSnooper.Helpers;
    using GreatSnooper.Model;
    using GreatSnooper.Services;
    using GreatSnooper.Windows;

    public class StyleSetting : AbstractSetting
    {
        private IMetroDialogService dialogService;

        public StyleSetting(string settingName, string text, MessageSetting style, IMetroDialogService dialogService)
        : base(settingName, text)
        {
            this.dialogService = dialogService;
            this.Style = style;
        }

        public MessageSetting Style
        {
            get;
            private set;
        }

        public ICommand StyleCommand
        {
            get
            {
                return new RelayCommand(OpenFontChooser);
            }
        }

        public void Save()
        {
            SettingsHelper.Save(this.SettingName, MessageSettings.ObjToSetting(Style));
        }

        private void OpenFontChooser()
        {
            var window = new FontChooser(this);
            window.Owner = dialogService.GetView();
            window.ShowDialog();
        }
    }
}