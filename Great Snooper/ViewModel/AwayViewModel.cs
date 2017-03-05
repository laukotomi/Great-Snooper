namespace GreatSnooper.ViewModel
{
    using System;
    using System.Windows.Input;
    using System.Windows.Threading;

    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.Command;

    using GreatSnooper.Helpers;
    using GreatSnooper.Services;

    class AwayViewModel : ViewModelBase
    {
        private Dispatcher dispatcher;
        private MainViewModel mvm;
        private bool _isAway;

        public AwayViewModel(MainViewModel mvm, string awayText)
        {
            this.mvm = mvm;
            this.dispatcher = Dispatcher.CurrentDispatcher;

            _isAway = awayText != string.Empty;
            if (_isAway)
            {
                AwayText = awayText;
            }
            else
            {
                AwayText = Properties.Settings.Default.AwayMessage;
            }
        }

        public string AwayButtonText
        {
            get
            {
                return (_isAway) ? Localizations.GSLocalization.Instance.AwayButtonBack : Localizations.GSLocalization.Instance.AwayButtonAway;
            }
        }

        public ICommand AwayCommand
        {
            get
            {
                return new RelayCommand(SetAway);
            }
        }

        public string AwayText
        {
            get;
            set;
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

        public bool IsAway
        {
            get
            {
                return _isAway;
            }
            private set
            {
                if (_isAway != value)
                {
                    _isAway = value;
                    RaisePropertyChanged("AwayText");
                    RaisePropertyChanged("IsAway");
                }
            }
        }

        private void Close()
        {
            this.DialogService.CloseRequest();
        }

        private void SetAway()
        {
            if (IsAway)
            {
                this.dispatcher.BeginInvoke(new Action(() =>
                {
                    this.mvm.SetBack();
                }));
                this.Close();
            }
            else
            {
                string text = WormNetCharTable.RemoveNonWormNetChars(AwayText.Trim());
                if (text.Length > 0)
                {
                    Properties.Settings.Default.AwayMessage = text;
                    Properties.Settings.Default.Save();
                    this.dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.mvm.SetAway(text);
                    }));
                    this.Close();
                }
            }
        }
    }
}