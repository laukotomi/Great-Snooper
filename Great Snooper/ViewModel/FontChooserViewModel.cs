using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GreatSnooper.Helpers;
using GreatSnooper.Model;
using GreatSnooper.Services;
using GreatSnooper.Settings;
using MahApps.Metro.Controls.Dialogs;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media;

namespace GreatSnooper.ViewModel
{
    class FontChooserViewModel : ViewModelBase
    {
        #region Members
        public bool settingsChanged;
        #endregion

        #region Properties
        public StyleSetting Style { get; private set; }
        public SortedDictionary<string, FontFamily> FontFamilies {
            get
            {
                var fontFamilies = new SortedDictionary<string, FontFamily>();
                IEnumerator<FontFamily> iterator = Fonts.SystemFontFamilies.GetEnumerator();
                while (iterator.MoveNext())
                    fontFamilies.Add(iterator.Current.ToString(), iterator.Current);
                return fontFamilies;
            }
        }
        public SortedDictionary<string, FontFamily> FallBackList {
            get
            {
                var fallBackList = new SortedDictionary<string, FontFamily>();
                fallBackList.Add("Loading font families", Style.Style.FontFamily);
                return fallBackList;
            }
        }
        public KeyValuePair<string, FontFamily> SelectedFontFamily
        {
            get { return new KeyValuePair<string, FontFamily>(MessageSetting.FontFamily.ToString(), MessageSetting.FontFamily); }
            set { MessageSetting.FontFamily = value.Value; }
        }
        public List<double> FontSizes { get; private set; }
        public IMetroDialogService DialogService { get; set; }
        public MessageSetting MessageSetting { get; private set; }
        #endregion

        public FontChooserViewModel(StyleSetting style)
        {
            this.Style = style;
            this.FontSizes = new List<double>();
            for (double i = 1; i <= 20; i++)
                this.FontSizes.Add(i);
            this.MessageSetting = new MessageSetting(style.Style);
            this.MessageSetting.PropertyChanged += MessageSetting_PropertyChanged;
        }

        internal void ClosingRequest(object sender, CancelEventArgs e)
        {
            if (this.settingsChanged)
            {
                e.Cancel = true;
                this.CloseCommand.Execute(null);
            }
        }

        void MessageSetting_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.settingsChanged = true;
        }

        #region CloseCommand
        public ICommand CloseCommand
        {
            get { return new RelayCommand(Close); }
        }

        private void Close()
        {
            if (this.settingsChanged == false)
            {
                DialogService.CloseRequest();
                return;
            }

            DialogService.ShowDialog(Localizations.GSLocalization.Instance.QuestionText, "Are you sure you would like to close this window and lose your changes?", MahApps.Metro.Controls.Dialogs.MessageDialogStyle.AffirmativeAndNegative, GlobalManager.YesNoDialogSetting, (t) =>
            {
                if (t.Result == MessageDialogResult.Affirmative)
                {
                    this.settingsChanged = false;
                    DialogService.CloseRequest();
                }
            });
        }
        #endregion

        #region SaveCommand
        public ICommand SaveCommand
        {
            get { return new RelayCommand(Save); }
        }

        private void Save()
        {
            Style.Style.Bold = MessageSetting.Bold;
            Style.Style.FontFamily = MessageSetting.FontFamily;
            Style.Style.Italic = MessageSetting.Italic;
            Style.Style.MessageColor = MessageSetting.MessageColor;
            Style.Style.NickColor = MessageSetting.NickColor;
            Style.Style.Size = MessageSetting.Size;
            Style.Style.Strikethrough = MessageSetting.Strikethrough;
            Style.Style.Underline = MessageSetting.Underline;
            Style.Save();
            this.settingsChanged = false;
            DialogService.CloseRequest();
        }
        #endregion
    }
}
