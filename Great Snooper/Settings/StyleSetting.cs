using GalaSoft.MvvmLight.Command;
using GreatSnooper.Helpers;
using GreatSnooper.Model;
using GreatSnooper.Services;
using GreatSnooper.Windows;
using System.Windows.Input;

namespace GreatSnooper.Settings
{
    public class StyleSetting : AbstractSetting
    {
        private IMetroDialogService dialogService;
        public MessageSetting Style { get; private set; }

        #region StyleCommand
        public ICommand StyleCommand
        {
            get { return new RelayCommand(OpenFontChooser); }
        }

        private void OpenFontChooser()
        {
            var window = new FontChooser(this);
            window.Owner = dialogService.GetView();
            window.ShowDialog();
        }
        #endregion

        public StyleSetting(string settingName, string text, MessageSetting style, IMetroDialogService dialogService)
            : base(settingName, text)
        {
            this.dialogService = dialogService;
            this.Style = style;
        }

        public void Save()
        {
            SettingsHelper.Save(this.settingName, MessageSettings.ObjToSetting(Style));
        }
    }
}
