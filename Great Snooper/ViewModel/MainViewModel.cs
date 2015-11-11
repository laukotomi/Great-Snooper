using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GreatSnooper.Classes;
using GreatSnooper.EventArguments;
using GreatSnooper.Helpers;
using GreatSnooper.Model;
using GreatSnooper.Services;
using GreatSnooper.Validators;
using GreatSnooper.Windows;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;

namespace GreatSnooper.ViewModel
{
    public class MainViewModel : ViewModelBase, IDisposable
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
        private int _selectedChannelIndex = -1;
        private bool _isEnergySaveMode;
        private AbstractChannelViewModel _selectedChannel;
        private string _filterText = Localizations.GSLocalization.Instance.FilterText;
        private WindowState _tempWindowState = WindowState.Maximized;

        private AbstractCommunicator[] servers;
        private volatile bool closing;
        private int procId = Process.GetCurrentProcess().Id;
        private Timer tusTimer;
        private readonly List<int> visitedChannels = new List<int>();
        private readonly DispatcherTimer filterTimer = new DispatcherTimer();
        private Task<string> channelSchemeTask;
        private Task loadSettingsTask;
        private Task loadGamesTask;
        private readonly List<League> leagues = new List<League>();
        private readonly List<Dictionary<string, string>> newsList = new List<Dictionary<string, string>>();
        private readonly Dictionary<string, bool> newsSeen = new Dictionary<string, bool>();
        private readonly DispatcherTimer secondTimer = new DispatcherTimer(DispatcherPriority.Input);
        private int gameListCounter = 0;

        private readonly Regex GameRegex = new Regex(@"^<GAME\s(\S*)\s(\S+)\s(\S+)\s(\S+)\s1\s(\S+)\s(\S+)\s([^>]+)>$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly byte[] gameRecvBuffer = new byte[10240];
        private readonly StringBuilder gameRecvSB = new StringBuilder(10240);
        private readonly Regex channelSchemeRegex = new Regex(@"^<SCHEME=([^>]+)>$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly Notificator notificator;
        private IntPtr lobbyWindow = IntPtr.Zero;
        private IntPtr gameWindow = IntPtr.Zero;
        private bool isHidden;
        #endregion

        #region Properties
        public Dispatcher Dispatcher { get; private set; }
        public IMetroDialogService DialogService { get; private set; }
        public ITaskbarIconService TaskbarIconService { get; private set; }
        public Process GameProcess { get; set; }
        public StartedGameTypes StartedGameType { get; set; }
        public bool ExitSnooperAfterGameStart { get; set; }
        public LeagueSearcher LeagueSearcher { get; private set; }
        public WindowState TempWindowState
        {
            get { return _tempWindowState; }
            set
            {
                if (_tempWindowState != value)
                    _tempWindowState = value;
            }
        }

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
                    if (_filterText.Length > 1 && _filterText != Localizations.GSLocalization.Instance.FilterText)
                        filterTimer.Start();
                    else
                        this.SelectedGLChannel.UserListDG.SetUserListDGView();
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

                    foreach (var chvm in this.Channels)
                    {
                        if (chvm is ChannelViewModel)
                        {
                            if (chvm.Joined)
                                chvm.LoadMessages(GlobalManager.MaxMessagesDisplayed, true);
                        }
                        else
                            break;
                    }
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
        public SortedObservableCollection<AbstractChannelViewModel> Channels { get; private set; }
        public int SelectedChannelIndex
        {
            get { return _selectedChannelIndex; }
            set
            {
                if (_selectedChannelIndex != value)
                {
                    _selectedChannelIndex = value;

                    if (FilterText != Localizations.GSLocalization.Instance.FilterText)
                    {
                        FilterText = Localizations.GSLocalization.Instance.FilterText;
                        RaisePropertyChanged("FilterText");
                    }

                    var pmChannel = (_selectedChannel != null && _selectedChannel is PMChannelViewModel)
                        ? (PMChannelViewModel)_selectedChannel : null;

                    if (value == -1)
                    {
                        _selectedChannel = null;
                    }
                    else
                    {
                        visitedChannels.Remove(value);
                        visitedChannels.Add(value);
                        
                        _selectedChannel = Channels[value];
                        if (_selectedChannel.IsHighlighted)
                            _selectedChannel.IsHighlighted = false;
                        if (_selectedChannel is ChannelViewModel)
                        {
                            SelectedGLChannel = (ChannelViewModel)_selectedChannel;
                            GameListForce = true;
                            RaisePropertyChanged("SelectedTabIndex2");
                        }
                        else
                        {
                            ((PMChannelViewModel)_selectedChannel).GenerateHeader();
                        }
                        if (_selectedChannel.Joined)
                        {
                            this.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                this.DialogService.GetView().UpdateLayout();
                                _selectedChannel.IsTBFocused = true;
                            }));
                        }
                    }

                    if (pmChannel != null)
                        pmChannel.GenerateHeader();
                }
            }
        }
        public int SelectedTabIndex2
        {
            get { return _selectedChannelIndex; }
        }
        public AbstractChannelViewModel SelectedChannel
        {
            get { return _selectedChannel; }
        }
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
            get { return (WormNetCommunicator)this.servers[0]; }
        }
        public GameSurgeCommunicator GameSurge
        {
            get { return (GameSurgeCommunicator)this.servers[1]; }
        }
        public bool ShowWAExe1
        {
            get { return Properties.Settings.Default.WaExe.Length != 0; }
        }
        public bool ShowWAExe2
        {
            get { return Properties.Settings.Default.WaExe2.Length != 0; }
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
            GlobalManager.MainWindowInit();
            Properties.Settings.Default.PropertyChanged += SettingsChanged;

            this.AwayText = string.Empty;
            this.DialogService = dialogService;
            this.TaskbarIconService = taskbarIconService;
            this.Dispatcher = Dispatcher.CurrentDispatcher;

            this.servers = new AbstractCommunicator[2];
            this.servers[0] = wormNetC;
            wormNetC.ConnectionState += ConnectionState;
            wormNetC.MVM = this;
            this.servers[1] = new GameSurgeCommunicator("irc.gamesurge.net", 6667);
            this.servers[1].ConnectionState += ConnectionState;
            this.servers[1].MVM = this;

            this.Channels = new SortedObservableCollection<AbstractChannelViewModel>();
            this.Channels.CollectionChanged += Channels_CollectionChanged;
            this.InstantColors = new Dictionary<string, SolidColorBrush>(StringComparer.OrdinalIgnoreCase);
            this.notificator = Notificator.Instance;
            this.notificator.IsEnabledChanged += notificator_IsEnabledChanged;
            this.LeagueSearcher = LeagueSearcher.Instance;

            secondTimer.Interval = new TimeSpan(0, 0, 1);
            secondTimer.Tick += secondTimer_Tick;
            secondTimer.Start();
            GameListForce = true;

            tusTimer = new Timer(LoadTusAccounts);
            if (GlobalManager.User.TusAccount != null) // tus login filled it already
                tusTimer.Change(20000, Timeout.Infinite);
            else
                tusTimer.Change(2000, Timeout.Infinite);

            filterTimer.Interval = new TimeSpan(0, 0, 0, 0, 300);
            filterTimer.Tick += filterTimer_Tick;

            // Unserialize newsseen
            string[] list = Properties.Settings.Default.NewsSeen.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < list.Length; i++)
                newsSeen.Add(list[i], false);

            wormNetC.GetChannelList(this);
        }
        #endregion

        #region Load Games + League searcher spam
        void secondTimer_Tick(object sender, EventArgs e)
        {
            // Game list refresh
            if (this.SelectedGLChannel != null && this.SelectedGLChannel.Joined && this.SelectedGLChannel.CanHost)
            {
                gameListCounter++;

                if (!closing && (GameListForce || gameListCounter >= 10) && DateTime.Now >= this.SelectedGLChannel.GameListUpdatedTime.AddSeconds(3) && (loadGamesTask == null || loadGamesTask.IsCompleted))
                {
                    LoadGames();

                    GameListForce = false;
                    gameListCounter = 0;
                }
            }
            else if (gameListCounter != 0)
                gameListCounter = 0;

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
        }

        private void HandleGameProcess()
        {
            // gameProcess = hoster.exe (HOST)
            // gameProcess = wa.exe (JOIN)
            if (GameProcess.HasExited)
            {
                SetBack(); 
                this.FreeGameProcess();
                return;
            }

            gameWindow = NativeMethods.FindWindow(null, "Worms2D");
            if (StartedGameType == StartedGameTypes.Join && ExitSnooperAfterGameStart && gameWindow != IntPtr.Zero)
            {
                this.CloseCommand.Execute(null);
                return;
            }

            lobbyWindow = NativeMethods.FindWindow(null, "Worms Armageddon");
            if (Properties.Settings.Default.EnergySaveModeGame && lobbyWindow != IntPtr.Zero)
            {
                if (NativeMethods.GetPlacement(lobbyWindow).showCmd == ShowWindowCommands.Normal)
                {
                    if (!IsEnergySaveMode)
                        this.EnterEnergySaveMode();
                }
                else if (IsEnergySaveMode)
                    LeaveEnergySaveMode();
            }
        }

        public void FreeGameProcess()
        {
            GameProcess.Dispose();
            GameProcess = null;
            lobbyWindow = IntPtr.Zero;
            gameWindow = IntPtr.Zero;
            ExitSnooperAfterGameStart = false;
            if (Properties.Settings.Default.EnergySaveModeGame && IsEnergySaveMode)
                LeaveEnergySaveMode();
        }

        private void LoadGames()
        {
            ChannelViewModel chvm = this.SelectedGLChannel;
            loadGamesTask = Task.Factory.StartNew(() =>
            {
                try
                {
                    HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create("http://" + this.WormNet.ServerAddress + ":80/wormageddonweb/GameList.asp?Channel=" + chvm.Name.Substring(1));
                    myHttpWebRequest.UserAgent = "T17Client/1.2";
                    myHttpWebRequest.Proxy = null;
                    myHttpWebRequest.AllowAutoRedirect = false;
                    myHttpWebRequest.Timeout = GlobalManager.WebRequestTimeout;
                    using (WebResponse myHttpWebResponse = myHttpWebRequest.GetResponse())
                    using (System.IO.Stream stream = myHttpWebResponse.GetResponseStream())
                    {
                        int bytes;
                        gameRecvSB.Clear();
                        while ((bytes = stream.Read(gameRecvBuffer, 0, gameRecvBuffer.Length)) > 0)
                        {
                            for (int j = 0; j < bytes; j++)
                                gameRecvSB.Append(WormNetCharTable.Decode[gameRecvBuffer[j]]);
                        }

                        gameRecvSB.Replace("\n", "");
                    }
                }
                catch (Exception ex)
                {
                    ErrorLog.Log(ex);
                    throw;
                }
            })
            .ContinueWith((t) =>
            {
                if (closing)
                {
                    this.CloseCommand.Execute(null);
                    return;
                }

                if (t.IsFaulted || !chvm.Joined) // we already left the channel
                    return;

                try
                {
                    // <GAMELISTSTART><GAME GameName Hoster HosterAddress CountryID 1 PasswordNeeded GameID HEXCC><BR><GAME GameName Hoster HosterAddress CountryID 1 PasswordNeeded GameID HEXCC><BR><GAMELISTEND>
                    //string start = "<GAMELISTSTART>"; 15 chars
                    //string end = "<GAMELISTEND>"; 13 chars
                    if (gameRecvSB.Length > 28)
                    {
                        string[] games = gameRecvSB.ToString(15, gameRecvSB.Length - 15 - 13).Split(new string[] { "<BR>" }, StringSplitOptions.RemoveEmptyEntries);

                        // Set all the games we have in !isAlive state (we will know if the game is not active anymore)
                        foreach (var game in chvm.Games)
                            game.IsAlive = false;

                        for (int i = 0; i < games.Length; i++)
                        {
                            // <GAME GameName Hoster HosterAddress CountryID 1 PasswordNeeded GameID HEXCC><BR>
                            Match m = GameRegex.Match(games[i].Trim());
                            if (m.Success)
                            {
                                string name = m.Groups[1].Value.Replace('\b', ' ').Replace("#039", "\x12");

                                // Encode the name to decode it with GameDecode
                                int bytes = 0;
                                byte b;
                                for (int j = 0; j < name.Length; j++)
                                {
                                    if (WormNetCharTable.Encode.TryGetValue(name[j], out b))
                                        gameRecvBuffer[bytes++] = b;
                                }
                                gameRecvSB.Clear();
                                for (int j = 0; j < bytes; j++)
                                    gameRecvSB.Append(WormNetCharTable.DecodeGame[gameRecvBuffer[j]]);
                                name = gameRecvSB.ToString();

                                string hoster = m.Groups[2].Value;
                                string address = m.Groups[3].Value;

                                int countryID;
                                if (!int.TryParse(m.Groups[4].Value, out countryID))
                                    continue;

                                bool password = m.Groups[5].Value == "1";

                                uint gameID;
                                if (!uint.TryParse(m.Groups[6].Value, out gameID))
                                    continue;

                                string hexCC = m.Groups[7].Value;


                                // Get the country of the hoster
                                Country country;
                                if (hexCC.Length < 9)
                                    country = Countries.GetCountryByID(countryID);
                                else
                                {
                                    string hexstr = uint.Parse(hexCC).ToString("X");
                                    if (hexstr.Length == 8 && hexstr.Substring(0, 4) == "6487")
                                    {
                                        char c1 = WormNetCharTable.Decode[byte.Parse(hexstr.Substring(6), System.Globalization.NumberStyles.HexNumber)];
                                        char c2 = WormNetCharTable.Decode[byte.Parse(hexstr.Substring(4, 2), System.Globalization.NumberStyles.HexNumber)];
                                        country = Countries.GetCountryByCC(c1.ToString() + c2.ToString());
                                    }
                                    else
                                        country = Countries.DefaultCountry;
                                }

                                // Add the game to the list or set its isAlive state true if it is already in the list
                                Game game = chvm.Games.Where(x => x.ID == gameID).FirstOrDefault();
                                if (game != null)
                                    game.IsAlive = true;
                                else
                                {
                                    chvm.Games.Add(new Game(gameID, name, address, country, hoster, password));
                                    if (this.notificator.SearchInGameNamesEnabled && this.notificator.GameNamesRegex.IsMatch(name)
                                        || this.notificator.SearchInHosterNamesEnabled && this.notificator.HosterNamesRegex.IsMatch(hoster))
                                    {
                                        NotificatorFound(string.Format(Localizations.GSLocalization.Instance.NotificatorGameText, hoster, name));
                                    }
                                }
                            }
                        }

                        // Delete inactive games from the list
                        for (int i = 0; i < chvm.Games.Count; i++)
                        {
                            if (!chvm.Games[i].IsAlive)
                            {
                                chvm.Games.RemoveAt(i);
                                i--;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorLog.Log(ex);
                }

                chvm.GameListUpdatedTime = DateTime.Now;
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
        #endregion

        #region EnergySave mode
        private void EnterEnergySaveMode()
        {
            this.IsEnergySaveMode = true;
        }

        private void LeaveEnergySaveMode()
        {
            if (Properties.Settings.Default.EnergySaveModeWin && (this.isHidden || TempWindowState == WindowState.Minimized))
                return;

            this.IsEnergySaveMode = false;
            this.DialogService.GetView().WindowState = TempWindowState;

            for (int i = 0; i < this.servers.Length; i++)
            {
                if (this.servers[i].State == AbstractCommunicator.ConnectionStates.Connected)
                {
                    foreach (var item in this.servers[i].Channels)
                    {
                        if (item.Value.Joined && item.Value.NewMessagesCount != 0)
                            item.Value.LoadNewMessages();
                    }
                }
            }
        }
        #endregion

        #region News, update
        internal void ContentRendered(object sender, EventArgs e)
        {
            string latestVersion = string.Empty;

            loadSettingsTask = Task.Factory.StartNew(() =>
            {
                string settingsXMLPath = GlobalManager.SettingsPath + @"\Settings.xml";

                try
                {
                    string settingsXMLPathTemp = GlobalManager.SettingsPath + @"\SettingsTemp.xml";

                    using (WebDownload webClient = new WebDownload() { Proxy = null })
                    {
                        webClient.DownloadFile("http://mediacreator.hu/SnooperSettings.xml", settingsXMLPathTemp);
                    }

                    // If downloading will fail then leagues won't be loaded. If they would, it could be hacked easily.
                    GlobalManager.SpamAllowed = true;

                    if (File.Exists(settingsXMLPath))
                        File.Delete(settingsXMLPath);

                    File.Move(settingsXMLPathTemp, settingsXMLPath);
                }
                catch (Exception ex)
                {
                    ErrorLog.Log(ex);
                }

                if (this.closing)
                    return;

                if (File.Exists(settingsXMLPath))
                {
                    HashSet<string> serverList = new HashSet<string>(
                        Properties.Settings.Default.ServerAddresses.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        , StringComparer.OrdinalIgnoreCase);
                    bool updateServers = false;

                    using (XmlReader xml = XmlReader.Create(settingsXMLPath))
                    {
                        xml.ReadToFollowing("servers");
                        using (XmlReader inner = xml.ReadSubtree())
                        {
                            while (inner.ReadToFollowing("server"))
                            {
                                inner.MoveToFirstAttribute();
                                string server = inner.Value;
                                if (!serverList.Contains(server))
                                {
                                    serverList.Add(server);
                                    updateServers = true;
                                }
                            }
                        }

                        xml.ReadToFollowing("leagues");
                        using (XmlReader inner = xml.ReadSubtree())
                        {
                            while (inner.ReadToFollowing("league"))
                            {
                                inner.MoveToFirstAttribute();
                                string name = inner.Value;
                                inner.MoveToNextAttribute();
                                leagues.Add(new League(name, inner.Value));
                            }
                        }

                        xml.ReadToFollowing("news");
                        using (XmlReader inner = xml.ReadSubtree())
                        {
                            while (inner.ReadToFollowing("bbnews"))
                            {
                                Dictionary<string, string> newsThings = new Dictionary<string, string>();
                                inner.MoveToFirstAttribute();
                                newsThings.Add(inner.Name, inner.Value);
                                while (inner.MoveToNextAttribute())
                                    newsThings.Add(inner.Name, inner.Value);

                                newsList.Add(newsThings);
                            }
                        }

                        xml.ReadToFollowing("version");
                        xml.MoveToFirstAttribute();
                        latestVersion = xml.Value;
                    }

                    if (updateServers)
                        SettingsHelper.Save("ServerAddresses", serverList);
                }

                if (this.closing)
                    return;
            })
            .ContinueWith((t) =>
            {
                if (this.closing)
                {
                    this.CloseCommand.Execute(null);
                    return;
                }

                if (!GlobalManager.SpamAllowed)
                {
                    if (t.IsFaulted)
                        ErrorLog.Log(t.Exception);
                    this.DialogService.ShowDialog(Localizations.GSLocalization.Instance.ErrorText, Localizations.GSLocalization.Instance.CommonSettingFailText);
                }
                else if (t.IsFaulted)
                {
                    ErrorLog.Log(t.Exception);
                    return;
                }
                else if (Math.Sign(App.GetVersion().CompareTo(latestVersion)) == -1) // we need update only if it is newer than this version
                {
                    this.DialogService.ShowDialog(Localizations.GSLocalization.Instance.InformationText, Localizations.GSLocalization.Instance.NewVersionText,
                        MahApps.Metro.Controls.Dialogs.MessageDialogStyle.AffirmativeAndNegative, GlobalManager.YesNoDialogSetting, (tt) =>
                        {
                            if (tt.Result == MessageDialogResult.Affirmative)
                            {
                                try
                                {
                                    Process p = new Process();
                                    if (Environment.OSVersion.Version.Major >= 6) // Vista or higher (to get admin rights).. on xp this causes fail!
                                    {
                                        p.StartInfo.UseShellExecute = true;
                                        p.StartInfo.Verb = "runas";
                                    }
                                    p.StartInfo.FileName = "Updater2.exe";
                                    p.Start();
                                    this.CloseCommand.Execute(null);
                                    return;
                                }
                                catch (Exception ex)
                                {
                                    ErrorLog.Log(ex);
                                    this.DialogService.ShowDialog(Localizations.GSLocalization.Instance.ErrorText, Localizations.GSLocalization.Instance.UpdaterFailText);
                                }
                            }
                            else
                                ProcessNews();
                        });
                }
                else
                    ProcessNews();
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void ProcessNews()
        {
            bool open = false;
            bool first = true;
            foreach (Dictionary<string, string> item in newsList)
            {
                try
                {
                    if (item["show"] == "1")
                    {
                        if (first)
                        {
                            first = false;
                            if (!newsSeen.ContainsKey(item["id"]))
                                open = true;
                        }

                        if (newsSeen.ContainsKey(item["id"]))
                            newsSeen[item["id"]] = true;
                    }
                }
                catch (Exception) { }
            }

            List<string> toRemove = new List<string>();
            foreach (var item in newsSeen)
            {
                if (!item.Value)
                    toRemove.Add(item.Key);
            }
            for (int i = 0; i < toRemove.Count; i++)
                newsSeen.Remove(toRemove[i]);

            if (open)
                OpenNewsCommand.Execute(null);
        }
        #endregion

        #region Channel things
        #region Channel collection changed
        void Channels_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                var mw = (MainWindow)this.DialogService.GetView();
                var chvm = (AbstractChannelViewModel)e.NewItems[0];
                mw.ChannelsTabControl.Items.Insert(e.NewStartingIndex, chvm.GetLayout());
                if (chvm is ChannelViewModel)
                {
                    var temp = (ChannelViewModel)chvm;
                    mw.GameList.Items.Insert(e.NewStartingIndex, temp.GetGameListLayout());
                    mw.UserList.Items.Insert(e.NewStartingIndex, temp.GetUserListLayout());
                }

                if (Channels.Count == 1)
                    this.SelectedChannelIndex = 0;
            }
            else
            {
                var mw = (MainWindow)this.DialogService.GetView();
                var chvm = (AbstractChannelViewModel)e.OldItems[0];
                mw.ChannelsTabControl.Items.RemoveAt(e.OldStartingIndex);
                if (chvm is ChannelViewModel)
                {
                    mw.GameList.Items.RemoveAt(e.OldStartingIndex);
                    mw.UserList.Items.RemoveAt(e.OldStartingIndex);
                }
            }
        }
        #endregion

        #region CloseChannelCommand (right click)
        public RelayCommand<PMChannelViewModel> CloseChannelCommand
        {
            get { return new RelayCommand<PMChannelViewModel>(CloseChannel); }
        }

        private void CloseChannel(PMChannelViewModel chvm)
        {
            this.CloseChannelTab(chvm);
        }
        #endregion

        #region HideChannelCommand (right click)
        public RelayCommand<ChannelViewModel> HideChannelCommand
        {
            get { return new RelayCommand<ChannelViewModel>(HideChannel); }
        }

        private void HideChannel(ChannelViewModel chvm)
        {
            this.CloseChannelTab(chvm);
            GlobalManager.HiddenChannels.Add(chvm.Name);
            SettingsHelper.Save("HiddenChannels", GlobalManager.HiddenChannels);
        }
        #endregion

        public void GetChannelScheme(ChannelViewModel chvm, Action onSuccess)
        {
            // Can not dispose channelSchemeTask because there can be parallel requests if user selects at least 2 channels to autojoin
            channelSchemeTask = Task.Factory.StartNew<string>(() =>
            {
                try
                {
                    HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create("http://" + this.WormNet.ServerAddress + "/wormageddonweb/RequestChannelScheme.asp?Channel=" + chvm.Name.Substring(1));
                    myHttpWebRequest.UserAgent = "T17Client/1.2";
                    myHttpWebRequest.Proxy = null;
                    myHttpWebRequest.AllowAutoRedirect = false;
                    myHttpWebRequest.Timeout = GlobalManager.WebRequestTimeout;
                    using (WebResponse myHttpWebResponse = myHttpWebRequest.GetResponse())
                    using (Stream stream = myHttpWebResponse.GetResponseStream())
                    {
                        int bytes;
                        var sb = new StringBuilder();
                        byte[] schemeRecvBuffer = new byte[100];
                        while ((bytes = stream.Read(schemeRecvBuffer, 0, schemeRecvBuffer.Length)) > 0)
                        {
                            for (int j = 0; j < bytes; j++)
                                sb.Append(WormNetCharTable.Decode[schemeRecvBuffer[j]]);
                        }

                        // <SCHEME=Pf,Be>
                        Match m = channelSchemeRegex.Match(sb.ToString());
                        if (m.Success)
                            return m.Groups[1].Value;
                    }
                }
                catch (Exception ex)
                {
                    ErrorLog.Log(ex);
                }

                return string.Empty;
            });
            channelSchemeTask.ContinueWith((t) =>
            {
                if (this.closing)
                {
                    this.CloseCommand.Execute(null);
                    return;
                }

                if (t.Result.Length == 0)
                    this.DialogService.ShowDialog(Localizations.GSLocalization.Instance.ErrorText, Localizations.GSLocalization.Instance.ChannelSchemeText);
                else
                    chvm.Scheme = t.Result;
                onSuccess();
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public void SelectChannel(int i)
        {
            this.SelectedChannelIndex = i;
            RaisePropertyChanged("SelectedChannelIndex");
        }

        public void SelectChannel(AbstractChannelViewModel chvm)
        {
            for (int i = 0; i < this.Channels.Count; i++)
            {
                if (this.Channels[i] == chvm)
                {
                    this.SelectChannel(i);
                    return;
                }
            }
        }

        public void CloseChannelTab(AbstractChannelViewModel chvm)
        {
            int index = (this.SelectedChannel == chvm)
                ? this.SelectedChannelIndex
                : this.Channels.IndexOf(chvm);

            if (this.SelectedChannel == chvm && visitedChannels.Count > 2) // Channel was selected
            {
                int lastindex = visitedChannels[visitedChannels.Count - 2];
                this.SelectChannel(lastindex);
            }

            visitedChannels.Remove(index);
            for (int i = 0; i < visitedChannels.Count; i++)
            {
                if (visitedChannels[i] > index)
                    visitedChannels[i]--;
            }

            if (chvm is ChannelViewModel)
            {
                if (chvm.Joined)
                    ((ChannelViewModel)chvm).LeaveChannelCommand.Execute(null);
            }
            else
            {
                chvm.ClearUsers();
                chvm.Server.Channels.Remove(chvm.Name);
            }

            if (chvm.Server is GameSurgeCommunicator && chvm.Server.Channels.Any(x => x.Value.Joined) == false)
                chvm.Server.CancelAsync();

            this.Channels.Remove(chvm);
        }
        #endregion

        #region GameList
        #region RefreshGameListCommand
        public ICommand RefreshGameListCommand
        {
            get { return new RelayCommand(RefreshGameList); }
        }

        private void RefreshGameList()
        {
            this.GameListForce = true;
        }
        #endregion
        #endregion

        #region Settings changed
        private Regex groupSoundRegex = new Regex(@"^Group(\d+)Sound$");
        private void SettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            Match m = groupSoundRegex.Match(e.PropertyName);
            if (m.Success)
            {
                UserGroups.Groups["Group" + m.Groups[1].Value].Sound = null;
                return;
            }
            switch (e.PropertyName)
            {
                case "ShowWormsChannel":
                    if (Properties.Settings.Default.ShowWormsChannel)
                        new ChannelViewModel(this, this.GameSurge, "#worms", "A place for hardcore wormers");
                    else
                    {
                        var chvm = (ChannelViewModel)this.GameSurge.Channels["#worms"];
                        CloseChannelTab(chvm);
                    }
                    break;

                case "WaExe":
                    RaisePropertyChanged("ShowWAExe1");
                    break;

                case "WaExe2":
                    RaisePropertyChanged("ShowWAExe2");
                    break;

                case "BatLogo":
                    RaisePropertyChanged("BatLogo");
                    break;

                case "HiddenChannels":
                    GlobalManager.HiddenChannels = new HashSet<string>(
                        Properties.Settings.Default.HiddenChannels.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                        StringComparer.OrdinalIgnoreCase); 
                    foreach (var server in this.servers)
                    {
                        if (server.State == AbstractCommunicator.ConnectionStates.Connected)
                        {
                            foreach (var chvm in server.Channels)
                            {
                                if (this.Channels.Any(x => x.Name.Equals(chvm.Key, StringComparison.OrdinalIgnoreCase)) == false && GlobalManager.HiddenChannels.Contains(chvm.Key) == false)
                                    this.Channels.Add(chvm.Value);
                            }
                        }
                    }
                    break;
            }
        }
        #endregion

        #region Filter
        #region Filtering
        void filterTimer_Tick(object sender, EventArgs e)
        {
            filterTimer.Stop();

            if (!this.SelectedGLChannel.Joined)
                return;

            List<string> words = new List<string>();
            string[] filtersTemp = FilterText.Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < filtersTemp.Length; i++)
            {
                string temp = filtersTemp[i].Trim();
                if (temp.Length >= 2)
                    words.Add(temp);
            }

            if (words.Count == 0)
                this.SelectedGLChannel.UserListDG.SetUserListDGView();
            else
            {
                var view = CollectionViewSource.GetDefaultView(this.SelectedGLChannel.Users);
                if (view != null)
                {
                    view.Filter = x =>
                    {
                        User u = (User)x;
                        if (!Properties.Settings.Default.ShowBannedUsers && u.IsBanned)
                            return false;

                        foreach (string word in words)
                        {
                            if (
                                u.Name.IndexOf(word, StringComparison.OrdinalIgnoreCase) != -1
                                || u.Clan.StartsWith(word, StringComparison.OrdinalIgnoreCase)
                                || u.TusAccount != null && (
                                    u.TusAccount.TusNick.IndexOf(word, StringComparison.OrdinalIgnoreCase) != -1
                                    || u.TusAccount.Clan.StartsWith(word, StringComparison.OrdinalIgnoreCase)
                                    )
                                || u.Country.Name.StartsWith(word, StringComparison.OrdinalIgnoreCase)
                                || u.Rank.Name.StartsWith(word, StringComparison.OrdinalIgnoreCase)
                                || Properties.Settings.Default.ShowInfoColumn && u.ClientName.IndexOf(word, StringComparison.OrdinalIgnoreCase) != -1
                            )
                                return true;
                        }
                        return false;
                    };
                }
            }
        }
        #endregion

        #region FilterCommand
        public ICommand FilterCommand
        {
            get { return new RelayCommand(Filter); }
        }

        private void Filter()
        {
            IsFilterFocused = true;
        }
        #endregion

        #region FilterFocusCommand
        public ICommand FilterFocusCommand
        {
            get { return new RelayCommand(FilterFocus); }
        }

        private void FilterFocus()
        {
            if (FilterText.Trim() == Localizations.GSLocalization.Instance.FilterText)
            {
                FilterText = string.Empty;
                RaisePropertyChanged("FilterText");
            }
        }
        #endregion

        #region FilterLeftCommand
        public ICommand FilterLeftCommand
        {
            get { return new RelayCommand(FilterLeft); }
        }

        private void FilterLeft()
        {
            if (FilterText.Trim() == string.Empty)
            {
                FilterText = Localizations.GSLocalization.Instance.FilterText;
                RaisePropertyChanged("FilterText");
            }
        }
        #endregion
        #endregion

        #region TusAccounts
        private void LoadTusAccounts(object state)
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

                if (closing)
                    return;

                string[] rows = userlist.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                this.Dispatcher.Invoke(new Action(() =>
                {
                    if (closing)
                        return;
                    TusAccounts.SetTusAccounts(rows, this.WormNet);
                }));
            }
            catch (Exception ex)
            {
                ErrorLog.Log(ex);
            }
            tusTimer.Change(20000, Timeout.Infinite);
        }
        #endregion

        #region ConnectionState
        private void ConnectionState(object sender, AbstractCommunicator.ConnectionStates oldState)
        {
            this.Dispatcher.Invoke(new Action(delegate()
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
                            if (oldState != AbstractCommunicator.ConnectionStates.ReConnecting)
                                this.DialogService.ShowDialog(Localizations.GSLocalization.Instance.ErrorText, Localizations.GSLocalization.Instance.GSNickInUseText);
                        }
                        else if (server.ErrorState == AbstractCommunicator.ErrorStates.None)
                        {
                            foreach (var item in server.Channels)
                            {
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

        #region Window things
        #region WindowActivatedCommand
        public ICommand WindowActivatedCommand
        {
            get { return new RelayCommand(WindowActivated); }
        }

        private void WindowActivated()
        {
            this.IsWindowFlashing = false;
            if (this.SelectedChannel != null)
            {
                if (this.SelectedChannel.IsHighlighted)
                {
                    this.SelectedChannel.IsHighlighted = false;
                    if (this.SelectedChannel is PMChannelViewModel)
                        ((PMChannelViewModel)this.SelectedChannel).GenerateHeader();
                }
                if (this.SelectedChannel.Joined)
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _selectedChannel.IsTBFocused = true;
                    }));
                }
            }
        }
        #endregion

        #region WindowStateChangedCommand
        public ICommand WindowStateChangedCommand
        {
            get { return new RelayCommand(WindowStateChanged); }
        }

        private void WindowStateChanged()
        {
            if (this.DialogService.GetView().WindowState == WindowState.Minimized)
            {
                if (Properties.Settings.Default.EnergySaveModeWin && !IsEnergySaveMode)
                    EnterEnergySaveMode();
            }
            else if (IsEnergySaveMode)
                LeaveEnergySaveMode();
        }
        #endregion

        #region ColumnsWidthChangedCommand
        public ICommand ColumnsWidthChangedCommand
        {
            get { return new RelayCommand(ColumnsWidthChanged); }
        }

        private void ColumnsWidthChanged()
        {
            this.LeftColumnWidth = ((MainWindow)this.DialogService.GetView()).LeftColumn.Width;
            this.RightColumnWidth = ((MainWindow)this.DialogService.GetView()).RightColumn.Width;
        }
        #endregion

        #region RowsHeightChangedCommand
        public ICommand RowsHeightChangedCommand
        {
            get { return new RelayCommand(RowsHeightChanged); }
        }

        private void RowsHeightChanged()
        {
            this.TopRowHeight = ((MainWindow)this.DialogService.GetView()).TopRow.Height;
            this.BottomRowHeight = ((MainWindow)this.DialogService.GetView()).BottomRow.Height;
        }
        #endregion

        #region WindowClosingCommand
        public RelayCommand<CancelEventArgs> WindowClosingCommand
        {
            get { return new RelayCommand<CancelEventArgs>(WindowClosing); }
        }

        private void WindowClosing(CancelEventArgs e)
        {
            if (closing == false && Properties.Settings.Default.CloseToTray)
            {
                e.Cancel = true;
                HideWindow();
                if (Properties.Settings.Default.TrayNotifications)
                    this.TaskbarIconService.ShowMessage(Localizations.GSLocalization.Instance.GSRunningTaskbar);
            }
        }

        private void HideWindow()
        {
            if (Properties.Settings.Default.EnergySaveModeWin && !IsEnergySaveMode)
                EnterEnergySaveMode();

            this.DialogService.GetView().Hide();
            this.isHidden = true;
        }
        #endregion

        internal void FlashWindow()
        {
            if (Properties.Settings.Default.TrayFlashing && this.IsWindowActive == false && this.IsWindowFlashing == false)
                this.IsWindowFlashing = true;
        }
        #endregion

        #region Channel Shortkeys
        #region NextChannelCommand
        public ICommand NextChannelCommand
        {
            get { return new RelayCommand(NextChannel); }
        }

        private void NextChannel()
        {
            if (Channels.Count > 0)
            {
                if (SelectedChannelIndex + 1 < Channels.Count)
                    this.SelectChannel(this.SelectedChannelIndex + 1);
                else
                    this.SelectChannel(0);
            }
        }
        #endregion

        #region PrevChannelCommand
        public ICommand PrevChannelCommand
        {
            get { return new RelayCommand(PrevChannel); }
        }

        private void PrevChannel()
        {
            if (Channels.Count > 0)
            {
                if (SelectedChannelIndex > 0)
                    this.SelectChannel(this.SelectedChannelIndex - 1);
                else
                    this.SelectChannel(Channels.Count - 1);
            }
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
                this.CloseChannelTab((PMChannelViewModel)this.SelectedChannel);
        }
        #endregion
        #endregion

        #region Closing
        #region Closing request
        internal void ClosingRequest(object sender, CancelEventArgs e)
        {
            this.closing = true;

            if (channelSchemeTask != null && !channelSchemeTask.IsCompleted)
                e.Cancel = true;

            if (loadSettingsTask != null && !loadSettingsTask.IsCompleted)
                e.Cancel = true;

            foreach (var server in this.servers)
            {
                if (server.State != AbstractCommunicator.ConnectionStates.Disconnected)
                {
                    server.CancelAsync();
                    e.Cancel = true;
                }
            }

            if (e.Cancel)
                return;

            foreach (var server in this.servers)
            {
                foreach (var item in server.Channels)
                {
                    if (item.Value.Joined)
                        item.Value.Log(item.Value.Messages.Count, true);
                }
            }
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
            DialogService.CloseRequest();
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

                foreach (var server in this.servers)
                {
                    server.ConnectionState -= ConnectionState;
                    server.Dispose();
                }

                if (channelSchemeTask != null)
                {
                    channelSchemeTask.Dispose();
                    channelSchemeTask = null;
                }

                if (loadSettingsTask != null)
                {
                    loadSettingsTask.Dispose();
                    loadSettingsTask = null;
                }

                if (tusTimer != null)
                {
                    tusTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    tusTimer.Dispose();
                    tusTimer = null;
                }

                if (GameProcess != null)
                {
                    this.FreeGameProcess();
                }

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
            if (this.SelectedGLChannel.Joined == false)
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
            var window = new NewsWindow(newsList, newsSeen);
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
            catch (Exception e)
            {
                ErrorLog.Log(e);
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
            DialogService.ActivationRequest();

            if (IsEnergySaveMode)
                LeaveEnergySaveMode(); 
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
            var chvm = (PMChannelViewModel)this.SelectedChannel;
            if (chvm.IsUserInConversation(u))
                chvm.RemoveUserFromConversation(u);
            else
                chvm.AddUserToConversation(u);

            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.DialogService.GetView().UpdateLayout();
                _selectedChannel.IsTBFocused = true;
            }));
        }
        #endregion

        #region AddOrRemoveBanCommand
        public RelayCommand<string> AddOrRemoveBanCommand
        {
            get { return new RelayCommand<string>(AddOrRemoveBan); }
        }

        private void AddOrRemoveBan(string userName)
        {
            foreach (var server in this.servers)
            {
                if (server.State == AbstractCommunicator.ConnectionStates.Connected)
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
        }
        #endregion
        #endregion

        #region Away set / Back
        public void SetAway(string awayText = null)
        {
            this.AwayText = (awayText != null && awayText.Length > 0) ? awayText : Properties.Settings.Default.AwayText;
            this.IsAway = true;
        }

        public void SetBack()
        {
            this.AwayText = string.Empty;
            this.IsAway = false;
        }
        #endregion

        #region Notificator
        internal void NotificatorFound(Message msg, AbstractChannelViewModel chvm)
        {
            this.FlashWindow();
            if (Properties.Settings.Default.TrayNotifications)
                this.TaskbarIconService.ShowMessage("(" + chvm.Name + ") " + msg.Sender.Name + ": " + msg.Text);
            if (Properties.Settings.Default.NotificatorSoundEnabled)
                Sounds.PlaySoundByName("NotificatorSound");
        }

        internal void NotificatorFound(string msg)
        {
            this.FlashWindow();
            if (Properties.Settings.Default.TrayNotifications)
                this.TaskbarIconService.ShowMessage(msg);
            if (Properties.Settings.Default.NotificatorSoundEnabled)
                Sounds.PlaySoundByName("NotificatorSound");
        }

        void notificator_IsEnabledChanged()
        {
            RaisePropertyChanged("NotificatorEnabled");
        }
        #endregion
    }
}
