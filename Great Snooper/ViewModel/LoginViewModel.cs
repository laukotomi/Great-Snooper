namespace GreatSnooper.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Resources;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Threading;
    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.Command;
    using GreatSnooper.Classes;
    using GreatSnooper.Helpers;
    using GreatSnooper.IRC;
    using GreatSnooper.Model;
    using GreatSnooper.Services;
    using GreatSnooper.Validators;
    using GreatSnooper.Windows;
    using MahApps.Metro;
    using MahApps.Metro.Controls;
    using MahApps.Metro.Controls.Dialogs;
    using Microsoft.Win32;

    public class LoginViewModel : ViewModelBase, IDisposable
    {
        private static bool _firstStart = true;

        private volatile bool closing;
        private Dispatcher dispatcher;
        bool disposed = false;
        private bool firstStart;
        private bool loggedIn;
        private Task<TusResult> tusLoginTask;
        private WormNetCommunicator wormNetC;
        private bool _isClanFocused;
        private bool _isConfigFlyoutOpened;
        private bool _isNickFocused;
        private bool _isServerFocused;
        private bool _isTusNickFocused;
        private bool _isTusPassFocused;
        private bool _loading;
        private AccentColorMenuData _selectedAccent;
        private LanguageData _selectedLanguage;

        public LoginViewModel()
        {
            this.firstStart = _firstStart;
            _firstStart = false;

            dispatcher = Dispatcher.CurrentDispatcher;

            if (!Properties.Settings.Default.IsCultureAccentSet)
            {
                // Load languages
                Languages = new List<LanguageData>();
                CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
                foreach (CultureInfo culture in cultures)
                {
                    try
                    {
                        if (culture.Name.Length > 0)
                        {
                            Localizations.GSLocalization loc = Localizations.GSLocalization.Instance;
                            ResourceSet rs = loc.RM.GetResourceSet(culture, true, false);
                            if (rs != null)
                            {
                                string languageEnName = loc.RM.GetString("LanguageEnName", culture);
                                string languageName = loc.RM.GetString("LanguageName", culture);
                                string countryCode = loc.RM.GetString("CountryCode", culture);
                                string cultureName = loc.RM.GetString("CultureName", culture);
                                if (Languages.Where(x => x.CultureName == cultureName).Any() == false)
                                {
                                    Languages.Add(new LanguageData(languageEnName, languageName, countryCode, cultureName));
                                }
                            }
                        }
                    }
                    catch (CultureNotFoundException) { }
                }

                SelectedLanguage = Languages.Where(x => x.CultureName == Properties.Settings.Default.CultureName).FirstOrDefault();

                // Load accents
                this.AccentColors = ThemeManager.Accents
                                    .Select(a => new AccentColorMenuData(a.Name, a.Resources["AccentColorBrush"] as Brush))
                                    .ToList();

                SelectedAccent = AccentColors.Where(x => x.Name == Properties.Settings.Default.AccentName).FirstOrDefault();

                IsConfigFlyoutOpened = true;
            }
            else
            {
                var theme = ThemeManager.DetectAppStyle(Application.Current);
                var accent = ThemeManager.GetAccent(Properties.Settings.Default.AccentName);
                ThemeManager.ChangeAppStyle(Application.Current, accent, theme.Item1);
            }

            // Servers
            ServerList = SortedObservableCollection<string>.CreateFrom(Properties.Settings.Default.ServerAddresses);
            SelectedServer = Properties.Settings.Default.ServerAddress;

            // LoginType
            switch (Properties.Settings.Default.LoginType)
            {
                case "simple":
                    LoginType = 0;
                    break;
                default:
                    LoginType = 1;
                    break;
            }

            AutoLogin = Properties.Settings.Default.AutoLogIn;
            Nick = Properties.Settings.Default.UserName;

            CountryList = Countries.CountryList;
            if (Properties.Settings.Default.UserCountry != -1)
            {
                SelectedCountry = Countries.GetCountryByID(Properties.Settings.Default.UserCountry);
            }
            else
            {
                CultureInfo ci = CultureInfo.InstalledUICulture;

                Country country;
                if (ci != null)
                {
                    country = Countries.GetCountryByCC(ci.TwoLetterISOLanguageName.ToUpper());
                }
                else
                {
                    country = Countries.DefaultCountry;
                }

                SelectedCountry = country;
            }

            RankList = Ranks.RankList;
            SelectedRank = Properties.Settings.Default.UserRank;
            Clan = Properties.Settings.Default.UserClan;

            TusNick = Properties.Settings.Default.TusNick;
            TusPass = Properties.Settings.Default.TusPass;
            this.UseSnooperRank = Properties.Settings.Default.UseSnooperRank;
        }

        ~LoginViewModel()
        {
            Dispose(false);
        }

        public List<AccentColorMenuData> AccentColors
        {
            get;
            private set;
        }

        public ICommand ActivationCommand
        {
            get
            {
                return new RelayCommand(ActivateWindow);
            }
        }

        public bool? AutoLogin
        {
            get;
            set;
        }

        public string Clan
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

        public MySortedList<Country> CountryList
        {
            get;
            private set;
        }

        public IMetroDialogService DialogService
        {
            get;
            set;
        }

        public bool IsClanFocused
        {
            get
            {
                return _isClanFocused;
            }
            private set
            {
                _isClanFocused = value;
                RaisePropertyChanged("IsClanFocused");
                _isClanFocused = false;
                RaisePropertyChanged("IsClanFocused");
            }
        }

        public bool IsConfigFlyoutOpened
        {
            get
            {
                return _isConfigFlyoutOpened;
            }
            set
            {
                _isConfigFlyoutOpened = value;

                // The flyout was closed, save config
                if (value == false)
                {
                    Properties.Settings.Default.IsCultureAccentSet = true;
                    Properties.Settings.Default.Save();
                }
            }
        }

        public bool IsNickFocused
        {
            get
            {
                return _isNickFocused;
            }
            private set
            {
                _isNickFocused = value;
                RaisePropertyChanged("IsNickFocused");
                _isNickFocused = false;
                RaisePropertyChanged("IsNickFocused");
            }
        }

        public bool IsServerFocused
        {
            get
            {
                return _isServerFocused;
            }
            private set
            {
                _isServerFocused = value;
                RaisePropertyChanged("IsServerFocused");
                _isServerFocused = false;
                RaisePropertyChanged("IsServerFocused");
            }
        }

        public bool IsTusNickFocused
        {
            get
            {
                return _isTusNickFocused;
            }
            private set
            {
                _isTusNickFocused = value;
                RaisePropertyChanged("IsTusNickFocused");
                _isTusNickFocused = false;
                RaisePropertyChanged("IsTusNickFocused");
            }
        }

        public bool IsTusPassFocused
        {
            get
            {
                return _isTusPassFocused;
            }
            private set
            {
                _isTusPassFocused = value;
                RaisePropertyChanged("IsTusPassFocused");
                _isTusPassFocused = false;
                RaisePropertyChanged("IsTusPassFocused");
            }
        }

        public List<LanguageData> Languages
        {
            get;
            private set;
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

        public ICommand LoginCommand
        {
            get
            {
                return new RelayCommand(Login);
            }
        }

        public int LoginType
        {
            get;
            set;
        }

        public ICommand MessageLogsCommand
        {
            get
            {
                return new RelayCommand(MessageLogs);
            }
        }

        public string Nick
        {
            get;
            set;
        }

        public List<Rank> RankList
        {
            get;
            private set;
        }

        public AccentColorMenuData SelectedAccent
        {
            get
            {
                return _selectedAccent;
            }
            set
            {
                if (_selectedAccent != value)
                {
                    bool init = _selectedAccent == null;
                    _selectedAccent = value;

                    try
                    {
                        var theme = ThemeManager.DetectAppStyle(Application.Current);
                        var accent = ThemeManager.GetAccent(value.Name);
                        ThemeManager.ChangeAppStyle(Application.Current, accent, theme.Item1);

                        if (!init)
                        {
                            Properties.Settings.Default.AccentName = accent.Name;
                            Properties.Settings.Default.Save();
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorLog.Log(ex);
                    }
                }
            }
        }

        public Country SelectedCountry
        {
            get;
            set;
        }

        public LanguageData SelectedLanguage
        {
            get
            {
                return _selectedLanguage;
            }
            set
            {
                if (_selectedLanguage != value)
                {
                    bool init = _selectedLanguage == null;
                    _selectedLanguage = value;

                    try
                    {
                        Thread.CurrentThread.CurrentCulture = new CultureInfo(value.CultureName);
                        Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentCulture;

                        if (!init)
                        {
                            Localizations.GSLocalization.Instance.CultureChanged();

                            Properties.Settings.Default.CultureName = _selectedLanguage.CultureName;
                            Properties.Settings.Default.Save();
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorLog.Log(ex);
                    }
                }
            }
        }

        public int SelectedRank
        {
            get;
            set;
        }

        public string SelectedServer
        {
            get;
            set;
        }

        public SortedObservableCollection<string> ServerList
        {
            get;
            private set;
        }

        public ICommand ServerListCommand
        {
            get
            {
                return new RelayCommand(OpenServerList);
            }
        }

        public ICommand SettingsCommand
        {
            get
            {
                return new RelayCommand(OpenSettings);
            }
        }

        public ITaskbarIconService TaskbarIconService
        {
            get;
            set;
        }

        public string TusNick
        {
            get;
            set;
        }

        public string TusPass
        {
            get;
            set;
        }

        public bool? UseSnooperRank
        {
            get;
            set;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal void ClosingRequest(object sender, CancelEventArgs e)
        {
            closing = true;

            if (tusLoginTask != null && !tusLoginTask.IsCompleted)
            {
                e.Cancel = true;
            }

            if (!loggedIn && wormNetC != null && wormNetC.State != IRCCommunicator.ConnectionStates.Disconnected)
            {
                wormNetC.CancelAsync();
                e.Cancel = true;
            }

            if (e.Cancel)
            {
                return;
            }

            this.Dispose();
        }

        internal void ContentRendered(object sender, EventArgs e)
        {
            var o = (MetroWindow)sender;
            o.ContentRendered -= this.ContentRendered;

            if (!Properties.Settings.Default.WAExeAsked && (Properties.Settings.Default.WaExe.Length == 0 || !File.Exists(Properties.Settings.Default.WaExe)))
            {
                Properties.Settings.Default.WAExeAsked = true;
                Properties.Settings.Default.Save();

                this.DialogService.ShowDialog(Localizations.GSLocalization.Instance.ErrorText, Localizations.GSLocalization.Instance.WAExeNotFoundText, MessageDialogStyle.AffirmativeAndNegative, GlobalManager.YesNoDialogSetting, (t) =>
                {
                    if (t.Result == MessageDialogResult.Affirmative)
                    {
                        OpenFileDialog dlg = new OpenFileDialog();
                        dlg.Filter = "Worms Armageddon Exe (*.exe)|*.exe";

                        // Display OpenFileDialog by calling ShowDialog method
                        Nullable<bool> result = dlg.ShowDialog();

                        // Get the selected file name
                        if (result.HasValue && result.Value)
                        {
                            // Set the WA.exe
                            Properties.Settings.Default.WaExe = dlg.FileName;
                            Properties.Settings.Default.Save();
                        }
                    }
                });
            }

            if (Properties.Settings.Default.TrayNotifications)
            {
                TaskbarIconService.ShowMessage(Localizations.GSLocalization.Instance.WelcomeMessage);
            }

            // Auto login
            if (Properties.Settings.Default.AutoLogIn && firstStart)
            {
                this.LoginCommand.Execute(null);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            if (disposing)
            {
                if (tusLoginTask != null)
                {
                    tusLoginTask.Dispose();
                    tusLoginTask = null;
                }

                if (wormNetC != null)
                {
                    wormNetC.ConnectionState -= ConnectionState;
                    if (!loggedIn)
                    {
                        if (TaskbarIconService != null)
                        {
                            TaskbarIconService.Dispose();
                            TaskbarIconService = null;
                        }
                        wormNetC.Dispose();
                        wormNetC = null;
                    }
                }
            }
        }

        private void ActivateWindow()
        {
            DialogService.ActivationRequest();
        }

        private void Close()
        {
            DialogService.CloseRequest();
        }

        private void ConnectionState(object sender, ConnectionStateEventArgs e)
        {
            this.dispatcher.Invoke(new Action(delegate()
            {
                Debug.WriteLine("ConnectionState: " + wormNetC.State.ToString());

                switch (wormNetC.State)
                {
                    case IRCCommunicator.ConnectionStates.Connected:
                        if (!closing)
                        {
                            new MainWindow(wormNetC, TaskbarIconService).Show();
                            loggedIn = true;
                            this.CloseCommand.Execute(null);
                            return;
                        }
                        break;

                    case IRCCommunicator.ConnectionStates.Disconnected:
                        if (!closing)
                        {
                            if (wormNetC.ErrorState == IRCCommunicator.ErrorStates.UsernameInUse)
                            {
                                this.DialogService.ShowDialog(Localizations.GSLocalization.Instance.ErrorText, Localizations.GSLocalization.Instance.NicknameInUseText);
                            }
                            else
                            {
                                this.DialogService.ShowDialog(Localizations.GSLocalization.Instance.ErrorText, Localizations.GSLocalization.Instance.WNCommFailText);
                            }
                        }

                        wormNetC.ConnectionState -= ConnectionState;
                        wormNetC.Dispose();
                        wormNetC = null;
                        this.Loading = false;
                        break;
                }
            }));
        }

        private void Login()
        {
            this.closing = false;
            this.loggedIn = false;

            string server = (this.SelectedServer != null) ? this.SelectedServer.Trim().ToLower() : string.Empty;
            if (server.Length == 0)
            {
                this.DialogService.ShowDialog(Localizations.GSLocalization.Instance.MissingValueText, Localizations.GSLocalization.Instance.ServerMissingText, MessageDialogStyle.Affirmative, GlobalManager.OKDialogSetting, (t) =>
                {
                    this.IsServerFocused = true;
                });
                return;
            }

            // Simple login
            if (this.LoginType == 0)
            {
                string nickName = this.Nick.Trim();

                if (nickName.Length == 0)
                {
                    this.DialogService.ShowDialog(Localizations.GSLocalization.Instance.MissingValueText, Localizations.GSLocalization.Instance.NickEmptyText, MessageDialogStyle.Affirmative, GlobalManager.OKDialogSetting, (t) =>
                    {
                        this.IsNickFocused = true;
                    });
                    return;
                }

                string nickNameError = Validator.NickNameValidator.Validate(ref nickName);

                if (nickNameError != string.Empty)
                {
                    this.DialogService.ShowDialog(Localizations.GSLocalization.Instance.InvalidValueText, nickNameError, MessageDialogStyle.Affirmative, GlobalManager.OKDialogSetting, (t) =>
                    {
                        this.IsNickFocused = true;
                    });
                    return;
                }

                string clan = this.Clan.Trim();
                string clanError = Validator.ClanValidator.Validate(ref clan);
                Regex clanRegex = new Regex(@"^[a-z0-9]*$", RegexOptions.IgnoreCase);
                if (clanError != string.Empty)
                {
                    this.DialogService.ShowDialog(Localizations.GSLocalization.Instance.InvalidValueText, clanError, MessageDialogStyle.Affirmative, GlobalManager.OKDialogSetting, (t) =>
                    {
                        this.IsClanFocused = true;
                    });
                    return;
                }
            }

            // TUS login
            else
            {
                string tusNickName = this.TusNick.Trim();

                if (tusNickName.Length == 0)
                {
                    this.DialogService.ShowDialog(Localizations.GSLocalization.Instance.MissingValueText, Localizations.GSLocalization.Instance.TusNickEmptyText, MessageDialogStyle.Affirmative, GlobalManager.OKDialogSetting, (t) =>
                    {
                        this.IsTusNickFocused = true;
                    });
                    return;
                }

                string tusPass = this.TusPass.Trim();
                if (tusPass.Length == 0)
                {
                    this.DialogService.ShowDialog(Localizations.GSLocalization.Instance.MissingValueText, Localizations.GSLocalization.Instance.TusPasswordEmptyText, MessageDialogStyle.Affirmative, GlobalManager.OKDialogSetting, (t) =>
                    {
                        this.IsTusPassFocused = true;
                    });
                    return;
                }
            }

            this.Loading = true;

            int port = 6667;
            int colon;
            if ((colon = server.IndexOf(':')) != -1)
            {
                string portstr = server.Substring(colon + 1);
                if (!int.TryParse(portstr, out port))
                {
                    port = 6667;
                }
                else
                {
                    server = server.Substring(0, colon);
                }
            }

            Properties.Settings.Default.ServerAddress = SelectedServer;
            Properties.Settings.Default.AutoLogIn = AutoLogin.Value;

            // Simple login
            if (this.LoginType == 0)
            {
                if (Properties.Settings.Default.BatLogo == false &&
                    Properties.Settings.Default.UserName.IndexOf("guuria", StringComparison.OrdinalIgnoreCase) == -1 &&
                    Properties.Settings.Default.UserName.IndexOf("guuuria", StringComparison.OrdinalIgnoreCase) == -1)
                {
                    Properties.Settings.Default.BatLogo = Nick.IndexOf("guuria", StringComparison.OrdinalIgnoreCase) != -1
                                                          || Nick.IndexOf("guuuria", StringComparison.OrdinalIgnoreCase) != -1;
                }
                Properties.Settings.Default.LoginType = "simple";
                Properties.Settings.Default.UserName = Nick;
                Properties.Settings.Default.UserClan = Clan;
                Properties.Settings.Default.UserCountry = SelectedCountry.ID;
                Properties.Settings.Default.UserRank = SelectedRank;
                if (Properties.Settings.Default.ChangeWormsNick)
                {
                    Properties.Settings.Default.WormsNick = Nick;
                }
                Properties.Settings.Default.Save();

                GlobalManager.User = new User(null, Nick, Clan)
                {
                    OnlineStatus = User.Status.Online
                };
                GlobalManager.User.SetUserInfo(SelectedCountry, Ranks.GetRankByInt(SelectedRank), App.GetFullVersion());

                // Initialize the WormNet Communicator
                wormNetC = new WormNetCommunicator(server, port);
                wormNetC.ConnectionState += ConnectionState;
                wormNetC.Connect();
            }

            // TUS login
            else
            {
                Properties.Settings.Default.LoginType = "tus";
                Properties.Settings.Default.TusNick = TusNick;
                Properties.Settings.Default.TusPass = TusPass;
                Properties.Settings.Default.UseSnooperRank = this.UseSnooperRank.HasValue && this.UseSnooperRank.Value;
                Properties.Settings.Default.Save();

                if (GlobalManager.TusAccounts == null)
                {
                    GlobalManager.TusAccounts = new Dictionary<string, TusAccount>(GlobalManager.CIStringComparer);
                }
                else
                {
                    GlobalManager.TusAccounts.Clear();
                }

                tusLoginTask = Task.Factory.StartNew<TusResult>(TusLogin);
                tusLoginTask.ContinueWith((t) =>
                {
                    if (this.closing)
                    {
                        this.Loading = false;
                        return;
                    }

                    Debug.WriteLine("TUS state: " + t.Result.TusState.ToString());

                    switch (t.Result.TusState)
                    {
                        case TusResult.TusStates.OK:
                            TusAccounts.Instance.SetTusAccounts(t.Result.Rows, null);
                            var tusAccount = GlobalManager.TusAccounts[t.Result.Nickname];
                            if (this.UseSnooperRank.HasValue && this.UseSnooperRank.Value)
                            {
                                tusAccount.Rank = Ranks.Snooper;
                            }

                            var clanRegexTUS = new Regex(@"[^a-z0-9]", RegexOptions.IgnoreCase);
                            var clan = clanRegexTUS.Replace(tusAccount.Clan, ""); // Remove bad characters

                            GlobalManager.User = new User(null, t.Result.Nickname, clan)
                            {
                                OnlineStatus = User.Status.Online
                            };
                            GlobalManager.User.TusAccount = tusAccount;

                            if (Properties.Settings.Default.ChangeWormsNick)
                            {
                                Properties.Settings.Default.WormsNick = t.Result.Nickname;
                                Properties.Settings.Default.Save();
                            }

                            // Initialize the WormNet Communicator
                            wormNetC = new WormNetCommunicator(server, port);
                            wormNetC.ConnectionState += ConnectionState;
                            wormNetC.Connect();
                            return;

                        case TusResult.TusStates.TUSError:
                            this.DialogService.ShowDialog(Localizations.GSLocalization.Instance.ErrorText, Localizations.GSLocalization.Instance.TusLoginFailText);
                            break;

                        case TusResult.TusStates.UserError:
                            this.DialogService.ShowDialog(Localizations.GSLocalization.Instance.ErrorText, Localizations.GSLocalization.Instance.TusAuthFailText);
                            break;

                        case TusResult.TusStates.ConnectionError:
                            this.DialogService.ShowDialog(Localizations.GSLocalization.Instance.ErrorText, Localizations.GSLocalization.Instance.TusCommFailText);
                            break;
                    }

                    this.Loading = false;
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        private void MessageLogs()
        {
            string logpath = GlobalManager.SettingsPath + @"\Logs";
            if (!Directory.Exists(logpath))
            {
                Directory.CreateDirectory(logpath);
            }

            try
            {
                Process.Start(logpath);
            }
            catch (Exception ex)
            {
                ErrorLog.Log(ex);
            }
        }

        private void OpenServerList()
        {
            ListEditor window = new ListEditor(this.ServerList, Localizations.GSLocalization.Instance.ServerListEditorTitle, (str) =>
            {
                this.ServerList.Add(str);
                SettingsHelper.Save("ServerAddresses", ServerList);
            }, (str) =>
            {
                this.ServerList.Remove(str);
                SettingsHelper.Save("ServerAddresses", ServerList);
            });
            window.Owner = DialogService.GetView();
            window.ShowDialog();
        }

        private void OpenSettings()
        {
            var window = new SettingsWindow();
            window.Owner = DialogService.GetView();
            window.ShowDialog();
        }

        private TusResult TusLogin()
        {
            try
            {
                using (var tusRequest = new WebDownload())
                {
                    string testlogin = tusRequest.DownloadString("https://www.tus-wa.com/testlogin.php?u=" + HttpUtility.UrlEncode(TusNick) + "&p=" + HttpUtility.UrlEncode(TusPass));
                    if (testlogin[0] == '1') // 1 sToOMiToO
                    {
                        if (this.closing)
                        {
                            return null;
                        }

                        string nickName = testlogin.Substring(2);

                        var nickRegexTUS = new Regex(@"^[^a-z`]+", RegexOptions.IgnoreCase);
                        var nickRegex2TUS = new Regex(@"[^a-z0-9`\-]", RegexOptions.IgnoreCase);

                        nickName = nickRegexTUS.Replace(nickName, ""); // Remove bad characters
                        nickName = nickRegex2TUS.Replace(nickName, ""); // Remove bad characters

                        for (int j = 0; j < 10; j++)
                        {
                            string userlist = tusRequest.DownloadString("https://www.tus-wa.com/userlist.php?update=" + HttpUtility.UrlEncode(TusNick) + "&league=classic");

                            if (this.closing)
                            {
                                return null;
                            }

                            string[] rows = userlist.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var row in rows)
                            {
                                if (row.StartsWith(nickName, StringComparison.OrdinalIgnoreCase))
                                {
                                    return new TusResult(TusResult.TusStates.OK, nickName, rows);
                                }
                            }

                            Thread.Sleep(2500);

                            if (this.closing)
                            {
                                return null;
                            }
                        }

                        return new TusResult(TusResult.TusStates.TUSError);
                    }
                    else
                    {
                        return new TusResult(TusResult.TusStates.UserError);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Log(ex);
                return new TusResult(TusResult.TusStates.ConnectionError);
            }
        }
    }
}
