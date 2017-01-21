using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GreatSnooper.Classes;
using GreatSnooper.Helpers;
using GreatSnooper.IRCTasks;
using GreatSnooper.Model;
using GreatSnooper.Services;
using GreatSnooper.Validators;
using GreatSnooper.Windows;

namespace GreatSnooper.ViewModel
{
    public partial class MainViewModel : ViewModelBase, IDisposable
    {
        #region Consts
        private const UInt32 FLASHW_STOP = 0; //Stop flashing. The system restores the window to its original state.
        private const UInt32 FLASHW_CAPTION = 1; //Flash the window caption.
        private const UInt32 FLASHW_TRAY = 2; //Flash the taskbar button.
        private const UInt32 FLASHW_ALL = 3; //Flash both the window caption and taskbar button.
        private const UInt32 FLASHW_TIMER = 4; //Flash continuously, until the FLASHW_STOP flag is set.
        private const UInt32 FLASHW_TIMERNOFG = 12; //Flash continuously until the window comes to the foreground.
        #endregion

        #region Enums
        public enum StartedGameTypes { Join, Host };
        #endregion

        #region Members
        private bool _isAway;
        private bool _volumeChanging;
        private bool _isWindowFlashing;
        public bool _isFilterFocused;
        private bool _isEnergySaveMode;
        private string _filterText = Localizations.GSLocalization.Instance.FilterText;

        private int procId = Process.GetCurrentProcess().Id;
        private readonly DispatcherTimer filterTimer = new DispatcherTimer();
        private Task loadSettingsTask;
        private Task loadGamesTask;
        private Task<string[]> loadTUSAccountsTask;
        private TimeSpan tusAccountsLoadTime = new TimeSpan(0, 0, 20);
        private TimeSpan gamesLoadTime = new TimeSpan(0, 0, 10);
        private readonly List<League> leagues = new List<League>();
        private readonly List<News> newsList = new List<News>();
        private readonly DispatcherTimer secondTimer = new DispatcherTimer(DispatcherPriority.Input);

        private readonly Regex GameRegex = new Regex(@"^<GAME\s(\S*)\s(\S+)\s(\S+)\s(\S+)\s1\s(\S+)\s(\S+)\s([^>]+)>$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly byte[] gameRecvBuffer = new byte[10240];
        private readonly StringBuilder gameRecvSB = new StringBuilder(10240);

        private readonly Notificator notificator;
        private IntPtr lobbyWindow = IntPtr.Zero;
        private IntPtr gameWindow = IntPtr.Zero;
        private bool isHidden;
        private bool shouldLeaveEnergySaveMode;
        public volatile bool closing;

        private readonly MainWindow view;
        private readonly ChannelTabControlViewModel _channelTabControl1;
        private readonly ChannelTabControlViewModel _channelTabControl2;
        private readonly ObservableCollection<ChannelViewModel> _gameListAndUserListChannels =
            new ObservableCollection<ChannelViewModel>();
        private readonly List<AbstractChannelViewModel> _allChannels =
            new List<AbstractChannelViewModel>();
        public List<AbstractChannelViewModel> AllChannels
        {
            get { return _allChannels; }
        }
        #endregion

        #region Properties
        public static MainViewModel Instance { get; private set; }
        public AbstractCommunicator[] Servers { get; private set; }
        public Dispatcher Dispatcher { get; private set; }
        public IMetroDialogService DialogService { get; private set; }
        public ITaskbarIconService TaskbarIconService { get; private set; }
        public Process GameProcess { get; set; }
        public StartedGameTypes StartedGameType { get; set; }
        public bool ExitSnooperAfterGameStart { get; set; }
        public LeagueSearcher LeagueSearcher { get; private set; }

        public bool IsAway
        {
            get { return _isAway; }
            private set
            {
                if (_isAway != value)
                {
                    _isAway = value;
                    RaisePropertyChanged("IsAway");
                    RaisePropertyChanged("AwayTooltip");
                }
            }
        }

        public string AwayTooltip
        {
            get
            {
                return (_isAway)
                    ? string.Format(Localizations.GSLocalization.Instance.AwayManagerTooltipAway, AwayText)
                    : Localizations.GSLocalization.Instance.AwayManagerTooltip;
            }
        }
        public string AwayText { get; private set; }
        public string FilterText
        {
            get { return _filterText; }
            set
            {
                if (_filterText != value)
                {
                    _filterText = value;
                    filterTimer.Stop();
                    if (this.SelectedGLChannel != null)
                    {
                        if (_filterText.Trim().Length > 0 && _filterText != Localizations.GSLocalization.Instance.FilterText)
                            filterTimer.Start();
                        else
                            this.SelectedGLChannel.UserListDG.SetUserListDGView();
                    }
                }
            }
        }
        public bool ChatModeEnabled
        {
            get { return Properties.Settings.Default.ChatMode; }
            private set
            {
                if (Properties.Settings.Default.ChatMode != value)
                {
                    Properties.Settings.Default.ChatMode = value;
                    Properties.Settings.Default.Save();
                    RaisePropertyChanged("ChatModeEnabled");
                }
            }
        }
        public bool NotificatorEnabled
        {
            get { return Notificator.Instance.IsEnabled; }
        }
        public bool SoundMuted
        {
            get { return Properties.Settings.Default.MuteState; }
            private set
            {
                if (Properties.Settings.Default.MuteState != value)
                {
                    Properties.Settings.Default.MuteState = value;
                    Properties.Settings.Default.Save();
                    RaisePropertyChanged("SoundMuted");
                }
            }
        }
        public string WelcomeText
        {
            get { return string.Format(Localizations.GSLocalization.Instance.WelcomeText, GlobalManager.User.Name); }
        }
        public bool VolumeChanging
        {
            get { return _volumeChanging; }
            set
            {
                if (_volumeChanging != value)
                {
                    _volumeChanging = value;
                    if (value == false)
                        ChangeVolume();
                }
            }
        }
        public double Volume
        {
            get { return Properties.Settings.Default.Volume; }
            set
            {
                Properties.Settings.Default.Volume = Convert.ToInt32(value);
                Properties.Settings.Default.Save();

                if (!VolumeChanging)
                    ChangeVolume();
            }
        }
        public bool IsWindowFlashing
        {
            get { return _isWindowFlashing; }
            set
            {
                if (_isWindowFlashing != value)
                {
                    _isWindowFlashing = value;

                    var h = new WindowInteropHelper(this.DialogService.GetView());

                    FLASHWINFO info = new FLASHWINFO
                    {
                        hwnd = h.Handle,
                        dwFlags = (value) ? FLASHW_ALL | FLASHW_TIMER : FLASHW_STOP,
                        uCount = uint.MaxValue,
                        dwTimeout = 0
                    };

                    info.cbSize = Convert.ToUInt32(Marshal.SizeOf(info));
                    NativeMethods.FlashWindowEx(ref info);
                }
            }
        }
        public GridLength LeftColumnWidth
        {
            get { return new GridLength(Properties.Settings.Default.LeftColumnWidth, GridUnitType.Star); }
            set { Properties.Settings.Default.LeftColumnWidth = value.Value; }
        }
        public GridLength RightColumnWidth
        {
            get { return new GridLength(Properties.Settings.Default.RightColumnWidth, GridUnitType.Star); }
            set { Properties.Settings.Default.RightColumnWidth = value.Value; }
        }
        public GridLength TopRowHeight
        {
            get { return new GridLength(Properties.Settings.Default.TopRowHeight, GridUnitType.Star); }
            set { Properties.Settings.Default.TopRowHeight = value.Value; }
        }
        public GridLength BottomRowHeight
        {
            get { return new GridLength(Properties.Settings.Default.BottomRowHeight, GridUnitType.Star); }
            set { Properties.Settings.Default.BottomRowHeight = value.Value; }
        }

        private int _selectedTabIndex2 = -1;
        public int SelectedTabIndex2
        {
            get { return this._selectedTabIndex2; }
            private set
            {
                if (this._selectedTabIndex2 != value)
                {
                    this._selectedTabIndex2 = value;
                    this.RaisePropertyChanged("SelectedTabIndex2");
                }
            }
        }

        public AbstractChannelViewModel SelectedChannel { get; set; }

        public ChannelViewModel SelectedGLChannel { get; private set; }
        public bool IsWindowActive
        {
            get
            {
                var activatedHandle = NativeMethods.GetForegroundWindow();
                if (activatedHandle == IntPtr.Zero)
                    return false;       // No window is currently activated

                int activeProcId;
                NativeMethods.GetWindowThreadProcessId(activatedHandle, out activeProcId);
                return activeProcId == this.procId;
            }
        }
        public bool GameListRefresh { get; set; }
        public bool TusRefresh { get; set; }
        public bool IsEnergySaveMode
        {
            get { return _isEnergySaveMode; }
            private set
            {
                if (_isEnergySaveMode != value)
                {
                    _isEnergySaveMode = value;
                    RaisePropertyChanged("IsEnergySaveMode");
                }
            }
        }
        public Dictionary<string, SolidColorBrush> InstantColors { get; private set; }
        public WormNetCommunicator WormNet
        {
            get { return (WormNetCommunicator)this.Servers[0]; }
        }
        public GameSurgeCommunicator GameSurge
        {
            get { return (GameSurgeCommunicator)this.Servers[1]; }
        }
        public bool ShowWAExe1
        {
            get { return Properties.Settings.Default.WaExe.Length != 0 && File.Exists(Properties.Settings.Default.WaExe); }
        }
        public bool ShowWAExe2
        {
            get { return Properties.Settings.Default.WaExe2.Length != 0 && File.Exists(Properties.Settings.Default.WaExe2); }
        }
        public bool BatLogo
        {
            get { return Properties.Settings.Default.BatLogo; }
            set
            {
                if (Properties.Settings.Default.BatLogo != value)
                {
                    Properties.Settings.Default.BatLogo = value;
                    Properties.Settings.Default.Save();
                    RaisePropertyChanged("BatLogo");
                }
            }
        }
        public bool IsFilterFocused
        {
            get { return _isFilterFocused; }
            private set
            {
                if (_isFilterFocused != value)
                {
                    _isFilterFocused = value;
                    RaisePropertyChanged("IsFilterFocused");
                    _isFilterFocused = false;
                    RaisePropertyChanged("IsFilterFocused");
                }
            }
        }
        public bool GameListForce { get; set; }
        #endregion

        #region Constructor
        public MainViewModel(IMetroDialogService dialogService, ITaskbarIconService taskbarIconService, WormNetCommunicator wormNetC)
        {
            Instance = this;
            GlobalManager.MainWindowInit();
            Properties.Settings.Default.PropertyChanged += SettingsChanged;
            this._gameListAndUserListChannels.CollectionChanged += GameListAndUserListChannels_CollectionChanged;
            this.view = (MainWindow)dialogService.GetView();
            this._channelTabControl1 = this.view.ChannelTabcontrol1.ViewModel;
            this._channelTabControl2 = this.view.ChannelTabcontrol2.ViewModel;
            this._channelTabControl1.Channels.CollectionChanged += Channels_CollectionChanged;
            this._channelTabControl2.Channels.CollectionChanged += Channels_CollectionChanged;
            this._channelTabControl1.PropertyChanged += ChannelChanged;
            this._channelTabControl2.PropertyChanged += ChannelChanged;

            this.AwayText = string.Empty;
            this.DialogService = dialogService;
            this.TaskbarIconService = taskbarIconService;
            this.Dispatcher = Dispatcher.CurrentDispatcher;

            this.Servers = new AbstractCommunicator[2];
            this.Servers[0] = wormNetC;
            wormNetC.ConnectionState += ConnectionState;
            wormNetC.MVM = this;
            this.Servers[1] = new GameSurgeCommunicator("irc.gamesurge.net", 6667);
            this.Servers[1].ConnectionState += ConnectionState;
            this.Servers[1].MVM = this;

            this.InstantColors = new Dictionary<string, SolidColorBrush>(GlobalManager.CIStringComparer);
            this.notificator = Notificator.Instance;
            this.notificator.IsEnabledChanged += notificator_IsEnabledChanged;
            this.LeagueSearcher = LeagueSearcher.Instance;

            secondTimer.Interval = new TimeSpan(0, 0, 1);
            secondTimer.Tick += secondTimer_Tick;
            secondTimer.Start();
            GameListForce = true;

            filterTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            filterTimer.Tick += filterTimer_Tick;

            wormNetC.GetChannelList(this);
        }

        private void ChannelChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedChannelIndex")
            {
                ChannelTabControlViewModel vm = (ChannelTabControlViewModel)sender;
                if (vm.SelectedChannel != null)
                {
                    this.SelectedChannel = vm.SelectedChannel;
                    if (vm == this._channelTabControl1)
                    {
                        ChannelViewModel channel = vm.SelectedChannel as ChannelViewModel;
                        if (channel != null)
                        {
                            SelectedGLChannel = channel;
                            GameListForce = true;
                            this.SelectedTabIndex2 = this._gameListAndUserListChannels.IndexOf(channel);
                        }
                    }
                }
            }
        }
        #endregion

        public void HandleTask(IRCTask task)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    task.DoTask(this);
                }
                catch (Exception ex)
                {
                    ErrorLog.Log(ex);
                }
            }));
        }

        #region Load Games + League searcher spam
        void secondTimer_Tick(object sender, EventArgs e)
        {
            if (this.shouldLeaveEnergySaveMode && IsEnergySaveMode)
                this.LeaveEnergySaveMode();

            // Game list refresh and channel scheme
            if (!closing && this.SelectedGLChannel != null && this.SelectedGLChannel.Joined && this.SelectedGLChannel.Server is WormNetCommunicator)
            {
                if (Properties.Settings.Default.LoadChannelScheme && string.IsNullOrEmpty(this.SelectedGLChannel.Scheme) && (this.SelectedGLChannel.ChannelSchemeTask == null || this.SelectedGLChannel.ChannelSchemeTask.IsCompleted) && (this.GameProcess == null || this.IsGameWindowOn() == false))
                    this.SelectedGLChannel.TryGetChannelScheme();
                else if (Properties.Settings.Default.LoadGames && this.SelectedGLChannel.CanHost)
                {
                    if ((GameListForce || DateTime.Now - this.SelectedGLChannel.GameListUpdatedTime >= gamesLoadTime) && DateTime.Now >= this.SelectedGLChannel.GameListUpdatedTime.AddSeconds(3) && (this.loadGamesTask == null || this.loadGamesTask.IsCompleted) && (Properties.Settings.Default.LoadOnlyIfWindowActive == false || this.IsWindowActive))
                    {
                        LoadGames(this.SelectedGLChannel);
                        GameListForce = false;
                    }
                }
            }

            // Game things
            if (GameProcess != null)
                HandleGameProcess();

            // Leagues search (spamming)
            if (this.LeagueSearcher.IsEnabled && this.LeagueSearcher.SpamLeft != -1)
            {
                if (!this.LeagueSearcher.ChannelToSearch.Joined || this.LeagueSearcher.SpamLeft == 0) // reset
                    this.LeagueSearcher.ChangeSearching(null);
                else
                {
                    this.LeagueSearcher.Counter++;
                    if (this.LeagueSearcher.Counter >= 90)
                        this.LeagueSearcher.DoSearch();
                }
            }

            // TUS accounts
            if (!this.closing && Properties.Settings.Default.LoadTUSAccounts && DateTime.Now - TusAccounts.tusAccountsLoaded >= this.tusAccountsLoadTime && (Properties.Settings.Default.LoadOnlyIfWindowActive == false || this.IsWindowActive))
                this.LoadTusAccounts();
        }
        #endregion

        #region Channel things
        #region Channel collection changed
        private void Channels_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                var temp = (AbstractChannelViewModel)e.NewItems[0];
                ChannelViewModel chvm = temp as ChannelViewModel;
                if (chvm != null)
                {
                    this._gameListAndUserListChannels.Add(chvm);
                }
                this._allChannels.Add(temp);

                if (this._channelTabControl2.Channels.Count > 0)
                {
                    this.ShowSecondaryTab = true;
                }
            }
            else
            {
                var temp = (AbstractChannelViewModel)e.OldItems[0];
                ChannelViewModel chvm = temp as ChannelViewModel;
                if (chvm != null)
                {
                    this._gameListAndUserListChannels.Remove(chvm);
                }

                this._allChannels.Remove(temp);
                if (temp == this.SelectedChannel)
                    this.SelectedChannel = null;

                if (this._channelTabControl2.Channels.Count == 0)
                {
                    this.ShowSecondaryTab = false;
                }
            }
        }

        private void GameListAndUserListChannels_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                ChannelViewModel chvm = (ChannelViewModel)e.NewItems[0];
                this.view.GameList.Items.Insert(e.NewStartingIndex, chvm.GetGameListLayout());
                this.view.UserList.Items.Insert(e.NewStartingIndex, chvm.GetUserListLayout());
            }
            else
            {
                this.view.GameList.Items.RemoveAt(e.OldStartingIndex);
                this.view.UserList.Items.RemoveAt(e.OldStartingIndex);
            }
        }
        #endregion

        #endregion

        #region TusAccounts
        private void LoadTusAccounts()
        {
            if ((this.loadTUSAccountsTask == null || this.loadTUSAccountsTask.IsCompleted) && (this.GameProcess == null || this.IsGameWindowOn() == false || GlobalManager.User.TusAccount != null))
            {
                loadTUSAccountsTask = Task.Factory.StartNew<string[]>(() =>
                {
                    try
                    {
                        string userlist = string.Empty;

                        using (var tusRequest = new WebDownload())
                        {
                            if (GlobalManager.User.TusAccount != null)
                                userlist = tusRequest.DownloadString("http://www.tus-wa.com/userlist.php?league=classic&update=" + System.Web.HttpUtility.UrlEncode(GlobalManager.User.TusAccount.TusNick));
                            else
                                userlist = tusRequest.DownloadString("http://www.tus-wa.com/userlist.php?league=classic");
                        }

                        return userlist.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    }
                    catch (Exception ex)
                    {
                        ErrorLog.Log(ex);
                    }
                    return null;
                });
                loadTUSAccountsTask.ContinueWith((t) =>
                {
                    if (closing)
                    {
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            this.CloseCommand.Execute(null);
                        }));
                        return;
                    }

                    if (t.Result == null)
                        return;

                    TusAccounts.SetTusAccounts(t.Result, this.WormNet);
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }
        #endregion

        #region ConnectionState
        private void ConnectionState(object sender, AbstractCommunicator.ConnectionStates oldState)
        {
            this.Dispatcher.BeginInvoke(new Action(delegate()
            {
                var server = (AbstractCommunicator)sender;

                if (closing)
                {
                    if (server.State == AbstractCommunicator.ConnectionStates.Disconnected)
                        this.CloseCommand.Execute(null);
                    else if (server.State != AbstractCommunicator.ConnectionStates.Disconnecting)
                        server.CancelAsync();
                }
                else if (server.State == AbstractCommunicator.ConnectionStates.Connected)
                {
                    if (oldState == AbstractCommunicator.ConnectionStates.ReConnecting || server is WormNetCommunicator)
                    {
                        foreach (var chvm in server.Channels)
                        {
                            chvm.Value.SetLoading(false);
                            if (chvm.Value.Joined)
                            {
                                chvm.Value.AddMessage(GlobalManager.SystemUser, Localizations.GSLocalization.Instance.ReconnectMessage, MessageSettings.SystemMessage);

                                if (chvm.Value.GetType() != typeof(PMChannelViewModel))
                                {
                                    server.JoinChannel(this, chvm.Value.Name);
                                    if (Properties.Settings.Default.UseWhoMessages)
                                        server.GetChannelClients(this, chvm.Value.Name);
                                }
                            }
                        }
                    }
                    else
                    {
                        var gameSurge = (GameSurgeCommunicator)server;
                        foreach (string channel in gameSurge.JoinChannelList)
                        {
                            AbstractChannelViewModel temp;
                            if (gameSurge.Channels.TryGetValue(channel, out temp) && temp is ChannelViewModel)
                            {
                                var chvm = (ChannelViewModel)temp;
                                if (chvm.Joined == false)
                                    chvm.JoinCommand.Execute(null);
                            }
                        }
                        gameSurge.JoinChannelList.Clear();

                        foreach (var channel in gameSurge.Channels)
                            channel.Value.Disabled = false;
                    }
                }
                /*
                else if (state == IRCCommunicator.ConnectionStates.AuthOK && gameSurgeIsConnected)
                {
                    sender.JoinChannel("#worms");
                }
                else if (state == IRCCommunicator.ConnectionStates.AuthBad)
                {
                    sender.ChannelList["#worms"].Part();
                    gameSurgeIsConnected = false;
                    sender.CancelAsync();
                }
                */
                else if (server.State == AbstractCommunicator.ConnectionStates.Disconnected)
                {
                    if (server is GameSurgeCommunicator)
                    {
                        if (server.ErrorState == AbstractCommunicator.ErrorStates.UsernameInUse)
                        {
                            foreach (var chvm in server.Channels)
                                chvm.Value.SetLoading(false);

                            if (oldState != AbstractCommunicator.ConnectionStates.ReConnecting)
                                this.DialogService.ShowDialog(Localizations.GSLocalization.Instance.ErrorText, Localizations.GSLocalization.Instance.GSNickInUseText);
                        }
                        else if (server.ErrorState == AbstractCommunicator.ErrorStates.None)
                        {
                            foreach (var item in server.Channels)
                            {
                                item.Value.SetLoading(false);
                                if (item.Value is ChannelViewModel)
                                    ((ChannelViewModel)item.Value).LeaveChannelCommand.Execute(null);
                                else
                                    item.Value.Disabled = true;
                            }
                        }
                        else
                            server.Reconnect();
                    }
                    else
                        server.Reconnect();
                }
                else if (server.State == AbstractCommunicator.ConnectionStates.Connecting || server.State == AbstractCommunicator.ConnectionStates.Disconnecting || server.State == AbstractCommunicator.ConnectionStates.ReConnecting)
                {
                    foreach (var chvm in server.Channels)
                        chvm.Value.SetLoading();
                }
            }
            ));
        }
        #endregion

        #region CloseActualChannelCommand
        public ICommand CloseActualChannelCommand
        {
            get { return new RelayCommand(CloseActualChannel); }
        }

        private void CloseActualChannel()
        {
            if (this.SelectedChannel != null && this.SelectedChannel is PMChannelViewModel)
                this.CloseChannel(this.SelectedChannel);
        }
        #endregion

        #region Closing
        #region Closing request
        internal void ClosingRequest(object sender, CancelEventArgs e)
        {
            if (closing == false && Properties.Settings.Default.CloseToTray)
            {
                e.Cancel = true;
                HideWindow();
                if (Properties.Settings.Default.TrayNotifications)
                    this.ShowTrayMessage(Localizations.GSLocalization.Instance.GSRunningTaskbar);

                return;
            }

            this.closing = true;

            if (loadSettingsTask != null && !loadSettingsTask.IsCompleted)
                e.Cancel = true;

            if (loadTUSAccountsTask != null && !loadTUSAccountsTask.IsCompleted)
                e.Cancel = true;

            if (loadGamesTask != null && !loadGamesTask.IsCompleted)
                e.Cancel = true;

            foreach (var server in this.Servers)
            {
                if (server.State != AbstractCommunicator.ConnectionStates.Disconnected)
                    e.Cancel = true;
                server.CancelAsync();
                foreach (var chvm in server.Channels)
                {
                    if (chvm.Value is ChannelViewModel && ((ChannelViewModel)chvm.Value).ChannelSchemeTask != null && !((ChannelViewModel)chvm.Value).ChannelSchemeTask.IsCompleted)
                        e.Cancel = true;
                }
            }

            if (e.Cancel)
                return;

            GlobalManager.TusAccounts.Clear();

            foreach (var server in this.Servers)
            {
                foreach (var item in server.Channels)
                {
                    if (item.Value.Joined)
                        item.Value.Log(item.Value.Messages.Count, true);
                }
            }

            // Save window width, state
            var window = this.DialogService.GetView();
            Properties.Settings.Default.WindowHeight = window.Height;
            Properties.Settings.Default.WindowWidth = window.Width;
            Properties.Settings.Default.Save();

            this.Dispose();
        }
        #endregion

        #region CloseCommand
        public ICommand CloseCommand
        {
            get { return new RelayCommand(Close); }
        }

        private void Close()
        {
            this.closing = true;
            DialogService.CloseRequest();
        }
        #endregion

        #region LogoutCommand
        public ICommand LogoutCommand
        {
            get { return new RelayCommand(Logout); }
        }

        private void Logout()
        {
            new Login().Show();
            this.CloseCommand.Execute(null);
        }
        #endregion

        #region IDisposable
        bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            disposed = true;

            if (disposing)
            {
                if (TaskbarIconService != null)
                {
                    TaskbarIconService.Dispose();
                    TaskbarIconService = null;
                }

                foreach (var server in this.Servers)
                {
                    server.ConnectionState -= ConnectionState;
                    server.Dispose();
                }

                if (loadSettingsTask != null)
                {
                    loadSettingsTask.Dispose();
                    loadSettingsTask = null;
                }

                if (loadGamesTask != null)
                {
                    loadGamesTask.Dispose();
                    loadGamesTask = null;
                }

                if (loadTUSAccountsTask != null)
                {
                    loadTUSAccountsTask.Dispose();
                    loadTUSAccountsTask = null;
                }

                if (GameProcess != null)
                    this.FreeGameProcess();

                Properties.Settings.Default.PropertyChanged -= SettingsChanged;
                this.notificator.IsEnabledChanged -= notificator_IsEnabledChanged;
                this.notificator.Dispose();
            }
        }

        ~MainViewModel()
        {
            Dispose(false);
        }
        #endregion
        #endregion

        #region Top header things
        #region StartWAExe1Command
        public ICommand StartWAExe1Command
        {
            get { return new RelayCommand(StartWAExe1); }
        }

        private void StartWAExe1()
        {
            StartGame(Properties.Settings.Default.WaExe);
        }
        #endregion

        #region StartWAExe2Command
        public ICommand StartWAExe2Command
        {
            get { return new RelayCommand(StartWAExe2); }
        }

        private void StartWAExe2()
        {
            StartGame(Properties.Settings.Default.WaExe2);
        }

        private void StartGame(string path, string args = null)
        {
            if (!File.Exists(path))
            {
                this.DialogService.ShowDialog(Localizations.GSLocalization.Instance.ErrorText, Localizations.GSLocalization.Instance.WAExeNotExistsText);
                return;
            }

            this.GameProcess = new Process();
            this.GameProcess.StartInfo.UseShellExecute = false;
            this.GameProcess.StartInfo.FileName = path;
            if (args != null)
                this.GameProcess.StartInfo.Arguments = args;
            if (this.GameProcess.Start())
            {
                if (Properties.Settings.Default.WAHighPriority)
                    this.GameProcess.PriorityClass = ProcessPriorityClass.High;
                if (Properties.Settings.Default.MarkAway)
                    this.SetAway();
            }
        }
        #endregion

        #region AwayManagerCommand
        public ICommand AwayManagerCommand
        {
            get { return new RelayCommand(AwayManager); }
        }

        private void AwayManager()
        {
            var window = new AwayManager(this, this.AwayText);
            window.Owner = DialogService.GetView();
            window.ShowDialog();
        }
        #endregion

        #region ChatModeCommand
        public ICommand ChatModeCommand
        {
            get { return new RelayCommand(ChatMode); }
        }

        private void ChatMode()
        {
            this.ChatModeEnabled = !this.ChatModeEnabled;
        }
        #endregion

        #region LeagueSearcherCommand
        public ICommand LeagueSearcherCommand
        {
            get { return new RelayCommand(OpenLeagueSearcher); }
        }

        private void OpenLeagueSearcher()
        {
            if (this.SelectedGLChannel == null || this.SelectedGLChannel.Joined == false)
            {
                this.DialogService.ShowDialog(Localizations.GSLocalization.Instance.ErrorText, Localizations.GSLocalization.Instance.ChannelOfflineText);
                return;
            }
            var window = new LeagueSearcherWindow(this.leagues, this.SelectedGLChannel);
            window.Owner = DialogService.GetView();
            window.ShowDialog();
        }
        #endregion

        #region NotificatorCommand
        public ICommand NotificatorCommand
        {
            get { return new RelayCommand(OpenNotificator); }
        }

        private void OpenNotificator()
        {
            var window = new NotificatorWindow();
            window.Owner = DialogService.GetView();
            window.ShowDialog();
        }
        #endregion

        #region SoundCommand
        public ICommand SoundCommand
        {
            get { return new RelayCommand(Sound); }
        }

        private void Sound()
        {
            this.SoundMuted = !this.SoundMuted;
        }
        #endregion

        #region SettingsCommand
        public ICommand SettingsCommand
        {
            get { return new RelayCommand(OpenSettings); }
        }

        private void OpenSettings()
        {
            var window = new SettingsWindow();
            window.Owner = DialogService.GetView();
            window.ShowDialog();
        }
        #endregion

        #region BanListCommand
        public ICommand BanListCommand
        {
            get { return new RelayCommand(BanList); }
        }

        private void BanList()
        {
            var window = new ListEditor(GlobalManager.BanList, Localizations.GSLocalization.Instance.IgnoreListTitle, new Action<string>(AddOrRemoveBan), new Action<string>(AddOrRemoveBan), Validator.NickNameValidator);
            window.Owner = this.DialogService.GetView();
            window.ShowDialog();
        }
        #endregion

        #region OpenNewsCommand
        public ICommand OpenNewsCommand
        {
            get { return new RelayCommand(OpenNews); }
        }

        private void OpenNews()
        {
            var window = new NewsWindow(newsList);
            window.Owner = DialogService.GetView();
            window.ShowDialog();
        }
        #endregion

        #region OpenLinkCommand
        public RelayCommand<string> OpenLinkCommand
        {
            get { return new RelayCommand<string>(OpenLink); }
        }

        private void OpenLink(string o)
        {
            try
            {
                Process.Start(o);
            }
            catch (Exception ex)
            {
                ErrorLog.Log(ex);
            }
        }
        #endregion

        #region Change volume
        private void ChangeVolume()
        {
            // Calculate the volume that's being set. BTW: this is a trackbar!
            uint NewVolume = Convert.ToUInt32((ushort.MaxValue / 100) * Properties.Settings.Default.Volume);
            // Set the same volume for both the left and the right channels
            uint NewVolumeAllChannels = ((NewVolume & 0x0000ffff) | ((uint)NewVolume << 16));
            // Set the volume
            NativeMethods.waveOutSetVolume(IntPtr.Zero, NewVolumeAllChannels);
            Sounds.PlaySoundByName("PMBeep");
        }
        #endregion
        #endregion

        #region Tray icon
        #region ActivationCommand
        public ICommand ActivationCommand
        {
            get { return new RelayCommand(ActivateWindow); }
        }

        private void ActivateWindow()
        {
            if (this.isHidden)
            {
                this.DialogService.GetView().Show();
                this.isHidden = false;
            }

            if (IsEnergySaveMode)
                this.LeaveEnergySaveMode();

            this.IsWindowFlashing = false;
            DialogService.ActivationRequest();
        }
        #endregion

        #region MessageLogsCommand
        public ICommand MessageLogsCommand
        {
            get { return new RelayCommand(MessageLogs); }
        }

        private void MessageLogs()
        {
            string logpath = GlobalManager.SettingsPath + @"\Logs";
            if (!Directory.Exists(logpath))
                Directory.CreateDirectory(logpath);

            try
            {
                Process.Start(logpath);
            }
            catch (Exception ex)
            {
                ErrorLog.Log(ex);
            }
        }
        #endregion

        #endregion

        #region Userlist things
        #region AddOrRemoveUserCommand for Conversations
        public RelayCommand<User> AddOrRemoveUserCommand
        {
            get { return new RelayCommand<User>(AddOrRemoveUser); }
        }

        private void AddOrRemoveUser(User u)
        {
            if (this.SelectedChannel != null)
            {
                var chvm = this.SelectedChannel as PMChannelViewModel;
                if (chvm != null)
                {
                    if (chvm.IsUserInConversation(u))
                        chvm.RemoveUserFromConversation(u);
                    else
                        chvm.AddUserToConversation(u);

                    if (chvm.ChannelTabVM != null)
                        chvm.ChannelTabVM.ActivateSelectedChannel();
                }
            }
        }
        #endregion

        #region AddOrRemoveBanCommand
        public RelayCommand<string> AddOrRemoveBanCommand
        {
            get { return new RelayCommand<string>(AddOrRemoveBan); }
        }

        private void AddOrRemoveBan(string userName)
        {
            foreach (var server in this.Servers)
            {
                User u;
                if (server.Users.TryGetValue(userName, out u))
                {
                    u.IsBanned = !u.IsBanned;
                    if (u.IsBanned)
                        GlobalManager.BanList.Add(u.Name);
                    else
                        GlobalManager.BanList.Remove(u.Name);
                    SettingsHelper.Save("BanList", GlobalManager.BanList);

                    // Reload channel messages where this user was active
                    if (!Properties.Settings.Default.ShowBannedMessages)
                    {
                        foreach (var chvm in u.Channels)
                        {
                            if (chvm.Joined)
                            {
                                chvm.LoadMessages(GlobalManager.MaxMessagesDisplayed, true);

                                // Refresh sorting
                                chvm.Users.Remove(u);
                                chvm.Users.Add(u);
                            }
                        }
                    }
                    else
                    {
                        foreach (var chvm in u.Channels)
                        {
                            if (chvm.Joined)
                            {
                                chvm.Users.Remove(u);
                                chvm.Users.Add(u);
                            }
                        }
                    }
                }
            }
        }
        #endregion
        #endregion

        #region Away set / Back
        public void SetAway(string awayText = null)
        {
            this.AwayText = string.IsNullOrEmpty(awayText) ? Properties.Settings.Default.AwayText : awayText;
            this.IsAway = true;
            foreach (var server in Servers)
            {
                foreach (var chvm in server.Channels)
                {
                    if (chvm.Value is PMChannelViewModel)
                        ((PMChannelViewModel)chvm.Value).AwayMsgSent = false;
                }
            }
        }

        public void SetBack()
        {
            this.AwayText = string.Empty;
            this.IsAway = false;
        }

        public ICommand AwayShortkeyCommand
        {
            get { return new RelayCommand(AwayShortkey); }
        }

        private void AwayShortkey()
        {
            if (this.IsAway)
                this.SetBack();
            else
                this.SetAway();
        }
        #endregion

        #region ShowHistoryCommand
        public ICommand ShowHistoryCommand
        {
            get { return new RelayCommand<AbstractChannelViewModel>(ShowHistory); }
        }

        private void ShowHistory(AbstractChannelViewModel channel = null)
        {
            if (channel == null)
                channel = this.SelectedChannel;

            if (channel != null)
            {
                AbstractChannelViewModel temp;
                if (!channel.Server.Channels.TryGetValue("Log: " + channel.Name, out temp))
                    temp = new LogChannelViewModel(this, channel.Server, channel.Name);
                //this.SelectChannel(temp);
            }
        }
        #endregion

        #region ShowUserHistoryCommand
        public RelayCommand<User> ShowUserHistoryCommand
        {
            get { return new RelayCommand<User>(ShowUserHistory); }
        }

        private void ShowUserHistory(User user)
        {
            if (this.SelectedGLChannel != null)
            {
                AbstractChannelViewModel temp;
                if (!this.SelectedGLChannel.Server.Channels.TryGetValue("Log: " + user.Name, out temp))
                    temp = new LogChannelViewModel(this, this.SelectedGLChannel.Server, user.Name);
                //this.SelectChannel(temp);
            }
        }
        #endregion

        #region Notificator
        internal void NotificatorFound(Message msg, AbstractChannelViewModel chvm)
        {
            chvm.Highlight();
            this.FlashWindow();
            if (Properties.Settings.Default.TrayNotifications)
                this.ShowTrayMessage("(" + chvm.Name + ") " + msg.Sender.Name + ": " + msg.Text, chvm);
            if (Properties.Settings.Default.NotificatorSoundEnabled)
                Sounds.PlaySoundByName("NotificatorSound");
        }

        internal void NotificatorFound(string msg, AbstractChannelViewModel chvm)
        {
            this.FlashWindow();
            if (Properties.Settings.Default.TrayNotifications)
                this.ShowTrayMessage(msg, chvm);
            if (Properties.Settings.Default.NotificatorSoundEnabled)
                Sounds.PlaySoundByName("NotificatorSound");
        }

        void notificator_IsEnabledChanged()
        {
            RaisePropertyChanged("NotificatorEnabled");
        }
        #endregion

        internal void ShowTrayMessage(string message, AbstractChannelViewModel chvm = null)
        {
            if (this.GameProcess == null && !IsGameWindowOn())
                this.TaskbarIconService.ShowMessage(message, chvm);
        }

        public void SelectChannel(AbstractChannelViewModel chvm)
        {
            if (chvm.ChannelTabVM != null)
                chvm.ChannelTabVM.SelectedChannel = chvm;
        }

        public void CreateChannel(AbstractChannelViewModel chvm)
        {
            this._channelTabControl1.Channels.Add(chvm);
        }

        private bool _showSecondaryTab;

        public bool ShowSecondaryTab
        {
            get { return this._showSecondaryTab; }
            set
            {
                if (this._showSecondaryTab != value)
                {
                    this._showSecondaryTab = value;
                    if (value)
                    {
                        Grid.SetColumnSpan(this._channelTabControl1.View, 1);
                        this._channelTabControl2.View.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        Grid.SetColumnSpan(this._channelTabControl1.View, 3);
                        this._channelTabControl2.View.Visibility = Visibility.Collapsed;

                    }
                    this.RaisePropertyChanged("ShowSecondaryTab");
                }
            }
        }

    }
}
