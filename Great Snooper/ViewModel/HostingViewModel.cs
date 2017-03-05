namespace GreatSnooper.ViewModel
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using System.Windows.Threading;
    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.Command;
    using GreatSnooper.Helpers;
    using GreatSnooper.ServiceInterfaces;
    using GreatSnooper.Services;
    using GreatSnooper.ViewModelInterfaces;

    class HostingViewModel : ViewModelBase, IHostingViewModel
    {
        private static Regex s_passRegex = new Regex(@"^[a-z]*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private enum HosterErrors
        {
            NoError,
            WormNatError,
            WormNatInitError,
            FailedToGetLocalIP,
            CreateGameFailed,
            NoGameID,
            FailedToStartTheGame,
            Unkown,
            WormNatClientError
        }

        private readonly DI _di;
        private readonly Dispatcher _dispatcher;
        private Process _gameProcess;
        private bool _loading;
        private ChannelViewModel _channel;
        private IMetroDialogService _dialogService;

        public ICommand CloseCommand
        {
            get
            {
                return new RelayCommand(Close);
            }
        }

        public ICommand CreateGameCommand
        {
            get
            {
                return new RelayCommand(CreateGame);
            }
        }

        public bool? ExitSnooper
        {
            get;
            set;
        }

        public string GameName
        {
            get;
            set;
        }

        public string GamePassword
        {
            get;
            set;
        }

        public bool? InfoToChannel
        {
            get;
            set;
        }

        public bool Loading
        {
            get
            {
                return _loading;
            }
            private set
            {
                if (_loading != value)
                {
                    _loading = value;
                    RaisePropertyChanged("Loading");
                }
            }
        }

        public int SelectedWaExe
        {
            get;
            set;
        }

        public bool? UsingWormNat2
        {
            get;
            set;
        }

        public HostingViewModel(DI di)
        {
            _di = di;
            _dispatcher = Dispatcher.CurrentDispatcher;

            GameName = Properties.Settings.Default.HostGameName;
            UsingWormNat2 = Properties.Settings.Default.HostUseWormnat;
            InfoToChannel = Properties.Settings.Default.HostInfoToChannel;
            SelectedWaExe = Properties.Settings.Default.SelectedWaExe;
            GamePassword = string.Empty;
            ExitSnooper = false;
        }

        public void Init(IMetroDialogService dialogService, ChannelViewModel channel)
        {
            _dialogService = dialogService;
            _channel = channel;
        }

        private void Close()
        {
            _dialogService.CloseRequest();
        }

        private void CreateGame()
        {
            if (!s_passRegex.IsMatch(GamePassword))
            {
                _dialogService.ShowDialog(Localizations.GSLocalization.Instance.InvalidValueText, Localizations.GSLocalization.Instance.GamePassBadText);
                return;
            }

            this.Loading = true;

            Task.Factory.StartNew<string>(() =>
            {
                IWormNetCharTable wormNetCharTable = _di.Resolve<IWormNetCharTable>();

                // Save settings
                string validGameName = wormNetCharTable.EncodeGame(GameName.Trim());
                Properties.Settings.Default.HostGameName = validGameName;
                Properties.Settings.Default.HostUseWormnat = UsingWormNat2.HasValue && UsingWormNat2.Value;
                Properties.Settings.Default.HostInfoToChannel = InfoToChannel.HasValue && InfoToChannel.Value;
                Properties.Settings.Default.SelectedWaExe = this.SelectedWaExe;
                Properties.Settings.Default.Save();

                string encodedGameName = wormNetCharTable.EncodeGameUrl(validGameName);
                string highPriority = Properties.Settings.Default.WAHighPriority ? "1" : "0";
                string waExe = (this.SelectedWaExe == 0)
                    ? Properties.Settings.Default.WaExe
                    : Properties.Settings.Default.WaExe2;
                string wormnat = (UsingWormNat2.HasValue && UsingWormNat2.Value) ? "1" : "0";
                string hexcc = string.Format("6487{0}{1}",
                    wormNetCharTable.GetByteForChar(_channel.Server.User.Country.CountryCode[1]).ToString("X"),
                    wormNetCharTable.GetByteForChar(_channel.Server.User.Country.CountryCode[0]).ToString("X"));

                string arguments = string.Format("\"{0}\" \"{1}\" \"{2}\" \"{3}\" \"{4}\" \"{5}\" \"{6}\" \"{7}\" \"{8}\" \"{9}\" \"{10}\" \"{11}\"",
                                                 _channel.Server.ServerAddress,
                                                 waExe,
                                                 _channel.Server.User.Name,
                                                 encodedGameName,
                                                 GamePassword,
                                                 _channel.Name.Substring(1),
                                                 _channel.Scheme,
                                                 _channel.Server.User.Country.ID.ToString(),
                                                 hexcc,
                                                 wormnat,
                                                 highPriority,
                                                 GlobalManager.SettingsPath);

                string success = TryHostGame(arguments);

                using (_gameProcess.StandardInput)
                {
                    _gameProcess.StandardInput.WriteLine("1");
                }

                return success;
            })
            .ContinueWith((t) =>
            {
                this.Loading = false;
                HosterErrors result;

                if (t.IsFaulted || Enum.TryParse(t.Result, out result) == false || result == HosterErrors.Unkown)
                {
                    _dialogService.ShowDialog(Localizations.GSLocalization.Instance.ErrorText, Localizations.GSLocalization.Instance.HosterUnknownFail);
                    return;
                }
                switch (result)
                {
                    case HosterErrors.CreateGameFailed:
                        _dialogService.ShowDialog(Localizations.GSLocalization.Instance.ErrorText, Localizations.GSLocalization.Instance.HosterCreateGameFail);
                        return;

                    case HosterErrors.FailedToStartTheGame:
                        _dialogService.ShowDialog(Localizations.GSLocalization.Instance.ErrorText, Localizations.GSLocalization.Instance.HosterStartGameFail);
                        return;

                    case HosterErrors.NoGameID:
                        _dialogService.ShowDialog(Localizations.GSLocalization.Instance.ErrorText, Localizations.GSLocalization.Instance.HosterNoGameIDError);
                        return;

                    case HosterErrors.FailedToGetLocalIP:
                        _dialogService.ShowDialog(Localizations.GSLocalization.Instance.ErrorText, Localizations.GSLocalization.Instance.HosterFailedToGetLocalIP);
                        return;

                    case HosterErrors.WormNatClientError:
                    case HosterErrors.WormNatError:
                    case HosterErrors.WormNatInitError:
                        _dialogService.ShowDialog(Localizations.GSLocalization.Instance.ErrorText, Localizations.GSLocalization.Instance.HosterWormNatError);
                        return;
                }

                this._dispatcher.BeginInvoke(new Action(() =>
                {
                    if (Properties.Settings.Default.HostInfoToChannel)
                    {
                        _channel.SendActionMessage("is hosting a game: " + Properties.Settings.Default.HostGameName);
                    }

                    MainViewModel mvm = _di.Resolve<MainViewModel>();
                    if (this.ExitSnooper.HasValue && this.ExitSnooper.Value)
                    {
                        _gameProcess.Dispose();
                        _gameProcess = null;
                        mvm.CloseCommand.Execute(null);
                        return;
                    }

                    mvm.GameProcess = _gameProcess;
                    mvm.StartedGameType = MainViewModel.StartedGameTypes.Host;

                    if (Properties.Settings.Default.MarkAway)
                    {
                        mvm.SetAway();
                    }
                }));

                this.CloseCommand.Execute(null);
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private string TryHostGame(string arguments)
        {
            _gameProcess = new Process();
            _gameProcess.StartInfo.UseShellExecute = false;
            _gameProcess.StartInfo.CreateNoWindow = true;
            _gameProcess.StartInfo.RedirectStandardOutput = true;
            _gameProcess.StartInfo.RedirectStandardInput = true;
            _gameProcess.StartInfo.FileName = Path.GetFullPath("Hoster.exe");
            _gameProcess.StartInfo.Arguments = arguments;

            Debug.WriteLine("HOSTER: " + arguments);
            if (_gameProcess.Start())
            {
                using (_gameProcess.StandardOutput)
                {
                    return _gameProcess.StandardOutput.ReadLine();
                }
            }
            else
            {
                _gameProcess.Dispose();
                _gameProcess = null;
            }
            return string.Empty;
        }
    }
}
