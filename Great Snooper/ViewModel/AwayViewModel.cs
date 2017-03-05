namespace GreatSnooper.ViewModel
{
    using System;
    using System.Windows.Input;
    using System.Windows.Threading;
    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.Command;
    using GreatSnooper.ServiceInterfaces;
    using GreatSnooper.Services;

    class AwayViewModel : ViewModelBase
    {
        private readonly DI _di;
        private readonly Dispatcher _dispatcher;
        private bool _isAway;
        private IMetroDialogService _dialogService;

        public AwayViewModel(DI di)
        {
            _di = di;
            _dispatcher = Dispatcher.CurrentDispatcher;
        }

        public void Init(IMetroDialogService dialogService, string awayText)
        {
            _dialogService = dialogService;
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
                return (_isAway)
                    ? Localizations.GSLocalization.Instance.AwayButtonBack
                    : Localizations.GSLocalization.Instance.AwayButtonAway;
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
            _dialogService.CloseRequest();
        }

        private void SetAway()
        {
            if (IsAway)
            {
                MainViewModel mvm = _di.Resolve<MainViewModel>();
                _dispatcher.BeginInvoke(new Action(() =>
                {
                    mvm.SetBack();
                }));
                this.Close();
            }
            else
            {
                IWormNetCharTable wormNetCharTable = _di.Resolve<IWormNetCharTable>();
                string text = wormNetCharTable.Encode(AwayText.Trim());
                if (text.Length > 0)
                {
                    Properties.Settings.Default.AwayMessage = text;
                    Properties.Settings.Default.Save();

                    MainViewModel mvm = _di.Resolve<MainViewModel>();
                    _dispatcher.BeginInvoke(new Action(() =>
                    {
                        mvm.SetAway(text);
                    }));
                    this.Close();
                }
            }
        }
    }
}