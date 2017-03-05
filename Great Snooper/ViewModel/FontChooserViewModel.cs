namespace GreatSnooper.ViewModel
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows.Input;
    using System.Windows.Media;

    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.Command;

    using GreatSnooper.Helpers;
    using GreatSnooper.Model;
    using GreatSnooper.Services;
    using GreatSnooper.Settings;

    using MahApps.Metro.Controls.Dialogs;

    class FontChooserViewModel : ViewModelBase
    {
        public bool settingsChanged;

        public FontChooserViewModel(StyleSetting style)
        {
            this.Style = style;
            this.FontSizes = new List<double>();
            for (double i = 1; i <= 20; i++)
            {
                this.FontSizes.Add(i);
            }
            this.MessageSetting = new MessageSetting(style.Style);
            this.MessageSetting.PropertyChanged += MessageSetting_PropertyChanged;
        }

        public ICommand CloseCommand
        {
            get
            {
                return new RelayCommand(Close);
            }
        }

        public IMetroDialogService DialogService
        {
            get;
            set;
        }

        public SortedDictionary<string, FontFamily> FallBackList
        {
            get
            {
                var fallBackList = new SortedDictionary<string, FontFamily>();
                fallBackList.Add("Loading font families", Style.Style.FontFamily);
                return fallBackList;
            }
        }

        public SortedDictionary<string, FontFamily> FontFamilies
        {
            get
            {
                var fontFamilies = new SortedDictionary<string, FontFamily>();
                IEnumerator<FontFamily> iterator = Fonts.SystemFontFamilies.GetEnumerator();
                while (iterator.MoveNext())
                {
                    fontFamilies.Add(iterator.Current.ToString(), iterator.Current);
                }
                return fontFamilies;
            }
        }

        public List<double> FontSizes
        {
            get;
            private set;
        }

        public MessageSetting MessageSetting
        {
            get;
            private set;
        }

        public ICommand SaveCommand
        {
            get
            {
                return new RelayCommand(Save);
            }
        }

        public KeyValuePair<string, FontFamily> SelectedFontFamily
        {
            get
            {
                return new KeyValuePair<string, FontFamily>(MessageSetting.FontFamily.ToString(), MessageSetting.FontFamily);
            }
            set
            {
                MessageSetting.FontFamily = value.Value;
            }
        }

        public StyleSetting Style
        {
            get;
            private set;
        }

        internal void ClosingRequest(object sender, CancelEventArgs e)
        {
            if (this.settingsChanged)
            {
                e.Cancel = true;
                this.CloseCommand.Execute(null);
            }
        }

        private void Close()
        {
            if (this.settingsChanged == false)
            {
                DialogService.CloseRequest();
                return;
            }

            DialogService.ShowDialog(Localizations.GSLocalization.Instance.QuestionText, Localizations.GSLocalization.Instance.LosingChangesQuestion, MahApps.Metro.Controls.Dialogs.MessageDialogStyle.AffirmativeAndNegative, GlobalManager.YesNoDialogSetting, (t) =>
            {
                if (t.Result == MessageDialogResult.Affirmative)
                {
                    this.settingsChanged = false;
                    DialogService.CloseRequest();
                }
            });
        }

        void MessageSetting_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.settingsChanged = true;
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
    }
}