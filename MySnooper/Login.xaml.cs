using MahApps.Metro.Controls;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MySnooper
{
    public partial class Login : MetroWindow, IDisposable
    {
        private enum TUSStates { OK, TUSError, ConnectionError, UserError }

        // Program data
        public SortedObservableCollection<string> ServerList { get; set; }

        //private Dictionary<string, string> leagues;
        //private List<Dictionary<string, string>> news;
        //private string latestVersion;
        //private bool failFlag;
        private Regex nickRegex, nickRegexTUS;
        private Regex nickRegex2, nickRegex2TUS;
        private Regex clanRegex, clanRegexTUS;
        //private BackgroundWorker loadSettings;


        // User data
        private string serverAddress;
        private int serverPort;
        private CountryClass nickCountry;
        private string nickName;
        private string nickClan;
        private int nickRank;

        // IRC Thread
        private IRCCommunicator wormNetC;
        private Thread ircThread;
        private bool loggedIn = false;

        // TUS communicator
        private TUSStates tusState;
        BackgroundWorker tusLoginWorker;
        private string tusPassword;
        private string tusNickStr;

        public static RoutedCommand DoubleClickCommand = new RoutedCommand();

        public Login()
        {
            if (!Properties.Settings.Default.SettingsUpgraded)
            {
                try
                {
                    Properties.Settings.Default.Upgrade();
                }
                catch (Exception) { }

                Properties.Settings.Default.SettingsUpgraded = true;
                Properties.Settings.Default.Save();
            }

            // Reducing Timeline frame rate
            Timeline.DesiredFrameRateProperty.OverrideMetadata(
                            typeof(Timeline),
                            new FrameworkPropertyMetadata { DefaultValue = 25 }
            );

            InitializeComponent();
            DataContext = this;

            nickRegex = new Regex(@"^[a-z`]", RegexOptions.IgnoreCase);
            nickRegex2 = new Regex(@"^[a-z`][a-z0-9`\-]*$", RegexOptions.IgnoreCase);

            ServerList = new SortedObservableCollection<string>();
            ServerList.DeSerialize(Properties.Settings.Default.ServerAddresses);

            Server.SelectedItem = Properties.Settings.Default.ServerAddress;

            //leagues = new Dictionary<string, string>();
            //news = new List<Dictionary<string, string>>();
            //failFlag = false;

            switch (Properties.Settings.Default.LoginType)
            {
                case "simple":
                    LoginTypeChooser.SelectedIndex = 0;
                    break;
                default:
                    LoginTypeChooser.SelectedIndex = 1;
                    break;
            }

            AutoLogIn.IsChecked = Properties.Settings.Default.AutoLogIn;
            Nick.Text = Properties.Settings.Default.UserName;

            Country.ItemsSource = CountriesClass.Countries;
            if (Properties.Settings.Default.UserCountry != -1)
            {
                CountryClass country = CountriesClass.GetCountryByID(Properties.Settings.Default.UserCountry);
                Country.SelectedItem = country;
            }
            else
            {
                CultureInfo ci = CultureInfo.InstalledUICulture;
                
                CountryClass country;
                if (ci != null)
                    country = CountriesClass.GetCountryByCC(ci.TwoLetterISOLanguageName.ToUpper());
                else
                    country = CountriesClass.GetCountryByID(49);

                Country.SelectedItem = country;
            }

            Rank.ItemsSource = RanksClass.Ranks;
            Rank.SelectedIndex = Properties.Settings.Default.UserRank;
            Clan.Text = Properties.Settings.Default.UserClan;

            TUSNick.Text = Properties.Settings.Default.TusNick;
            TUSPass.Password = Properties.Settings.Default.TusPass;

            /*
            loadSettings = new BackgroundWorker();
            loadSettings.WorkerSupportsCancellation = true;
            loadSettings.DoWork += LoadSettings;
            loadSettings.RunWorkerCompleted += SettingsLoaded;
            loadSettings.RunWorkerAsync();
            */
        }

        private void LoginWindow_ContentRendered(object sender, EventArgs e)
        {            
            if (Properties.Settings.Default.WaExe.Length == 0)
            {
                try
                {
                    object WALoc = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Team17SoftwareLTD\WormsArmageddon", "PATH", null);
                    if (WALoc != null)
                    {
                        string WAPath = WALoc.ToString() + @"\WA.exe";
                        if (File.Exists(WAPath)) {
                            Properties.Settings.Default.WaExe = WAPath;
                            Properties.Settings.Default.Save();
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorLog.log(ex);
                }

                if (Properties.Settings.Default.WaExe.Length == 0 && !Properties.Settings.Default.WAExeAsked)
                {
                    Properties.Settings.Default.WAExeAsked = true;
                    Properties.Settings.Default.Save();

                    MessageBoxResult res = MessageBox.Show(this, "Ooops, it seems like Great Snooper can not find your WA.exe! You can not host or join a game without that file. Would you like to locate your WA.exe now? You can do it later in the settings too.", "WA.exe needs to be located", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (res == MessageBoxResult.Yes)
                    {
                        OpenFileDialog dlg = new OpenFileDialog();
                        dlg.Filter = "Worms Armageddon Exe|*.exe";

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
                }
            }

            myNotifyIcon.ShowBalloonTip(null, "Welcome to Great Snooper!", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
        }

        /*
        private void LoadSettings(object sender, DoWorkEventArgs e)
        {
            WormNetCharTable.GenerateThings();

            string SettingsXML = GlobalManager.SettingsPath + @"\Settings.xml";

            using (WebClient webClient = new WebClient() { Proxy = null })
            {
                webClient.DownloadFile("http://mediacreator.hu/SnooperSettings.xml", SettingsXML);
            }

            if (loadSettings.CancellationPending)
            {
                e.Cancel = true;
                return;
            }

            if (File.Exists(SettingsXML))
            {
                using (XmlReader xml = XmlReader.Create(SettingsXML))
                {
                    xml.ReadToFollowing("servers");
                    using (XmlReader inner = xml.ReadSubtree())
                    {
                        while (inner.ReadToFollowing("server"))
                        {
                            inner.MoveToFirstAttribute();
                            serverList.Add(inner.Value);
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
                            leagues.Add(inner.Value, name);
                        }
                    }

                    xml.ReadToFollowing("news");
                    using (XmlReader inner = xml.ReadSubtree())
                    {
                        while (inner.ReadToFollowing("bbnews"))
                        {
                            Dictionary<string, string> newsthings = new Dictionary<string, string>();
                            inner.MoveToFirstAttribute();
                            newsthings.Add(inner.Name, inner.Value);
                            while (inner.MoveToNextAttribute())
                                newsthings.Add(inner.Name, inner.Value);

                            news.Add(newsthings);
                        }
                    }

                    xml.ReadToFollowing("version");
                    xml.MoveToFirstAttribute();
                    latestVersion = xml.Value;
                }
            }
            else
                serverList.Add("wormnet1.team17.com");

            if (loadSettings.CancellationPending)
            {
                e.Cancel = true;
                return;
            }

            // Delete old logs
            string logsDirectory = GlobalManager.SettingsPath + @"\Logs";
            if (Properties.Settings.Default.DeleteLogs && Directory.Exists(logsDirectory))
            {
                Regex DateRegex = new Regex(@"[^0-9]");
                string date = DateRegex.Replace(DateTime.Now.ToString("d"), "-");
                if (date != Properties.Settings.Default.TimeLogsDeleted)
                {
                    Properties.Settings.Default.TimeLogsDeleted = date;
                    Properties.Settings.Default.Save();

                    string[] dirs = Directory.GetDirectories(logsDirectory);
                    DateTime old = DateTime.Now - new TimeSpan(30, 0, 0, 0);
                    for (int i = 0; i < dirs.Length; i++)
                    {
                        string[] files = Directory.GetFiles(dirs[i]);
                        for (int j = 0; j < files.Length; j++)
                        {
                            FileInfo info = new FileInfo(files[j]);
                            if (info.LastWriteTime < old)
                                File.Delete(files[j]);
                        }

                        if (Directory.GetFiles(dirs[i]).Length == 0)
                            Directory.Delete(dirs[i]);
                    }
                }
            }

            if (loadSettings.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
        }
        */

        /*
        private void SettingsLoaded(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                this.Close();
                return;
            }

            if (e.Error != null)
            {
                failFlag = true;
                ErrorLog.log(e.Error);
                MessageBox.Show(this, "Failed to load the common settings!" + Environment.NewLine + "Probably you don't have internet connection or the program needs to be updated.", "Fail", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (Math.Sign(App.getVersion().CompareTo(latestVersion)) == -1) // we need update only if it is newer than this version
            {
                MessageBoxResult result = MessageBox.Show(this, "There is a new update available for Great Snooper!" + Environment.NewLine + "Would you like to download it now?", "Update", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        System.Diagnostics.Process p = new System.Diagnostics.Process();
                        if (Environment.OSVersion.Version.Major >= 6) // Vista or higher (to get admin rights).. on xp this causes fail!
                        {
                            p.StartInfo.UseShellExecute = true;
                            p.StartInfo.Verb = "runas";
                        }
                        p.StartInfo.FileName = "Updater.exe";
                        p.Start();
                        this.Close();
                        return;
                    }
                    catch (Exception ex)
                    {
                        ErrorLog.log(ex);
                        MessageBox.Show(this, "Failed to start the updater! Please restart Great Snooper with administrator rights! (Right click on the icon and Run as Administrator)", "Fail", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            for (int i = 0; i < serverList.Count; i++)
            {
                Server.Items.Add(serverList[i]);
            }

            if (Properties.Settings.Default.ServerAddress.Length > 0)
            {
                if (!Server.Items.Contains(Properties.Settings.Default.ServerAddress))
                    Server.Items.Add(Properties.Settings.Default.ServerAddress);
                Server.SelectedItem = Properties.Settings.Default.ServerAddress;
            }
            else if (Server.Items.Count > 0) Server.SelectedIndex = 0;


            if (!failFlag && Properties.Settings.Default.AutoLogIn)
            {
                LogIn(null, null);
            }
            else
            {
                Container.IsEnabled = true;
                LoadingRing.IsActive = false;
            }
        }
        */
        private void LogInClicked(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            LogIn();
        }

        private void LogIn()
        {
            serverAddress = Server.Text.Trim().ToLower();
            if (serverAddress.Length == 0)
            {
                MakeErrorTooltip(Server, "Please choose a server!");
                return;
            }

            int colon;
            if ((colon = serverAddress.IndexOf(':')) != -1)
            {
                string portstr = serverAddress.Substring(colon + 1);
                if (!int.TryParse(portstr, out serverPort))
                    serverPort = 6667;
                else
                    serverAddress = serverAddress.Substring(0, colon);
            }
            else serverPort = 6667;


            switch (LoginTypeChooser.SelectedIndex)
            {
                // Simple login
                case 0:
                    if (clanRegex == null)
                        clanRegex = new Regex(@"^[a-z0-9]*$", RegexOptions.IgnoreCase);

                    nickName = Nick.Text.Trim();
                    nickClan = Clan.Text.Trim();

                    if (nickName.Length == 0)
                    {
                        MakeErrorTooltip(Nick, "Please enter your nickname!");
                    }
                    else if (!nickRegex.IsMatch(nickName))
                    {
                        MakeErrorTooltip(Nick, "Your nickname should begin with a character" + Environment.NewLine + "of the English aplhabet or with ` character!");
                    }
                    else if (!nickRegex2.IsMatch(nickName))
                    {
                        MakeErrorTooltip(Nick, "Your nickname contains one or more" + Environment.NewLine + "forbidden characters! Use characters from" + Environment.NewLine + "the English alphabet, numbers, - or `!");
                    }
                    else if (!clanRegex.IsMatch(nickClan))
                    {
                        MakeErrorTooltip(Clan, "Your clan can contain only characters" + Environment.NewLine + "from the English alphabet or numbers");
                    }
                    else
                    {
                        Container.IsEnabled = false;
                        LoadingRing.IsActive = true;

                        nickCountry = Country.SelectedValue as CountryClass;
                        nickRank = Rank.SelectedIndex;

                        Properties.Settings.Default.LoginType = "simple";
                        Properties.Settings.Default.ServerAddress = Server.Text.Trim().ToLower();
                        Properties.Settings.Default.AutoLogIn = AutoLogIn.IsChecked.Value;
                        Properties.Settings.Default.UserName = nickName;
                        Properties.Settings.Default.UserClan = nickClan;
                        Properties.Settings.Default.UserCountry = nickCountry.ID;
                        Properties.Settings.Default.UserRank = nickRank;
                        Properties.Settings.Default.Save();

                        GlobalManager.User = new Client(nickName, nickCountry, nickClan, nickRank, true);

                        // Initialize the WormNet Communicator
                        wormNetC = new IRCCommunicator(serverAddress, serverPort);
                        wormNetC.ConnectionState += ConnectionState;

                        // Start WormNet communicator thread
                        ircThread = new Thread(new ThreadStart(wormNetC.run));
                        ircThread.Start();
                    }
                    break;

                // TUS login
                case 1:
                    nickName = TUSNick.Text.Trim();
                    tusPassword = TUSPass.Password.Trim();

                    if (nickName.Length == 0)
                    {
                        MakeErrorTooltip(TUSNick, "Please enter your nickname!");
                    }
                    else if (!nickRegex.IsMatch(nickName))
                    {
                        MakeErrorTooltip(TUSNick, "Your nickname should begin with a character" + Environment.NewLine + "of the English aplhabet or with ` character!");
                    }
                    else if (!nickRegex2.IsMatch(nickName))
                    {
                        MakeErrorTooltip(TUSNick, "Your nickname contains one or more" + Environment.NewLine + "forbidden characters! Use characters from" + Environment.NewLine + "the English alphabet, numbers, - or `!");
                    }
                    else if (tusPassword.Length == 0)
                    {
                        MakeErrorTooltip(TUSPass, "Please enter your password!");
                    }
                    else
                    {
                        Container.IsEnabled = false;
                        LoadingRing.IsActive = true;

                        Properties.Settings.Default.LoginType = "tus";
                        Properties.Settings.Default.ServerAddress = serverAddress;
                        Properties.Settings.Default.AutoLogIn = AutoLogIn.IsChecked.Value;
                        Properties.Settings.Default.TusNick = nickName;
                        Properties.Settings.Default.TusPass = tusPassword;
                        Properties.Settings.Default.Save();

                        tusState = TUSStates.TUSError;

                        if (tusLoginWorker == null)
                        {
                            tusLoginWorker = new BackgroundWorker();
                            tusLoginWorker.WorkerSupportsCancellation = true;
                            tusLoginWorker.DoWork += TUSLoginWorker_DoWork;
                            tusLoginWorker.RunWorkerCompleted += TUSLoginWorker_RunWorkerCompleted;
                        }
                        tusLoginWorker.RunWorkerAsync();
                    }
                    break;
            }
        }

        private void TUSLoginWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                using (WebClient tusRequest = new WebClient() { Proxy = null })
                {
                    string testlogin = tusRequest.DownloadString("http://www.tus-wa.com/testlogin.php?u=" + System.Web.HttpUtility.UrlEncode(nickName) + "&p=" + System.Web.HttpUtility.UrlEncode(tusPassword));
                    if (testlogin[0] == '1') // 1 sToOMiToO
                    {
                        if (tusLoginWorker.CancellationPending)
                        {
                            e.Cancel = true;
                            return;
                        }

                        string tempNick = nickName;
                        nickName = testlogin.Substring(2);

                        if (nickRegexTUS == null)
                            nickRegexTUS = new Regex(@"^[^a-z`]+", RegexOptions.IgnoreCase);
                        if (nickRegex2TUS == null)
                            nickRegex2TUS = new Regex(@"[^a-z0-9`\-]", RegexOptions.IgnoreCase);

                        nickName = nickRegexTUS.Replace(nickName, ""); // Remove bad characters
                        nickName = nickRegex2TUS.Replace(nickName, ""); // Remove bad characters

                        for (int j = 0; j < 10; j++)
                        {
                            string userlist = tusRequest.DownloadString("http://www.tus-wa.com/userlist.php?update=" + System.Web.HttpUtility.UrlEncode(tempNick) + "&league=classic");

                            if (tusLoginWorker.CancellationPending)
                            {
                                e.Cancel = true;
                                return;
                            }

                            string[] rows = userlist.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            for (int i = 0; i < rows.Length; i++)
                            {
                                if (rows[i].Substring(0, nickName.Length) == nickName)
                                {
                                    tusState = TUSStates.OK;
                                    string[] data = rows[i].Split(new char[] { ' ' });

                                    tusNickStr = data[1];

                                    if (clanRegexTUS == null)
                                        clanRegexTUS = new Regex(@"[^a-z0-9]", RegexOptions.IgnoreCase);

                                    nickClan = clanRegexTUS.Replace(data[5], ""); // Remove bad characters
                                    if (nickClan.Length == 0)
                                        nickClan = "Username";

                                    if (int.TryParse(data[2].Substring(1), out nickRank))
                                        nickRank--;
                                    else
                                        nickRank = 13;

                                    nickCountry = CountriesClass.GetCountryByCC(data[3].ToUpper());
                                    break;
                                }
                            }

                            if (tusState == TUSStates.OK)
                                break;

                            Thread.Sleep(2500);

                            if (tusLoginWorker.CancellationPending)
                            {
                                e.Cancel = true;
                                return;
                            }
                        }
                    }
                    else
                    {
                        tusState = TUSStates.UserError;
                    }
                }
            }
            catch (Exception ex)
            {
                tusState = TUSStates.ConnectionError;
                ErrorLog.log(ex);
            }

            if (tusLoginWorker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
        }


        private void TUSLoginWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                this.Close();
                return;
            }

            switch (tusState)
            {
                case TUSStates.TUSError:
                    MessageBox.Show(this, "An error occoured! Please try again!", "Fail", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;

                case TUSStates.OK:
                    GlobalManager.User = new Client(nickName, nickCountry, nickClan, nickRank, true) { TusNick = tusNickStr };

                    // Initialize the WormNet Communicator
                    wormNetC = new IRCCommunicator(serverAddress, serverPort);
                    wormNetC.ConnectionState += ConnectionState;

                    // Start WormNet communicator thread
                    ircThread = new Thread(new ThreadStart(wormNetC.run));
                    ircThread.Start();
                    return;

                case TUSStates.UserError:
                    MessageBox.Show(this, "The given username or password was incorrent!", "Wrong username or password", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;

                case TUSStates.ConnectionError:
                    MessageBox.Show(this, "The communication with TUS has failed. Please try again!", "Fail", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
            }

            Container.IsEnabled = true;
            LoadingRing.IsActive = false;
        }


        private void ConnectionState(IRCConnectionStates state)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                switch (state)
                {
                    case IRCConnectionStates.OK:
                        MainWindow MW = new MainWindow(wormNetC, ircThread, serverAddress);
                        MW.Show();
                        loggedIn = true;
                        this.Close();
                        return;

                    case IRCConnectionStates.UsernameInUse:
                        MessageBox.Show(this,
                            "This nickname is already in use! Please choose an other one!" + Environment.NewLine + Environment.NewLine +
                            "Note: if you lost your internet connection, you may need to wait 1 or 2 minutes until the server releases your broken nickname."
                            , "Nickname is alredy in use", MessageBoxButton.OK, MessageBoxImage.Information
                        );
                        break;

                    case IRCConnectionStates.Error:
                        MessageBox.Show(this,
                            "Could not connect to the server! Check if the server address is ok or try again later (probably maintenance time)!"
                            , "Connection error", MessageBoxButton.OK, MessageBoxImage.Error
                        );
                        break;

                    case IRCConnectionStates.Cancelled:
                        this.Close();
                        return;
                }

                Container.IsEnabled = true;
                LoadingRing.IsActive = false;
            }
            ));
        }
        private void MakeErrorTooltip(Control item, string text)
        {
            ToolTip tt;
            if (item.ToolTip != null)
                tt = (ToolTip)item.ToolTip;
            else
            {
                tt = new ToolTip();
                tt.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                tt.PlacementTarget = item;
            }

            TextBlock tb;
            if (tt.Content != null)
                tb = (TextBlock)tt.Content;
            else
            {
                tb = new TextBlock();
                tb.Foreground = new SolidColorBrush(Colors.Red);
                tt.Content = tb;
            }

            tb.Inlines.Clear();
            string[] lines = text.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                tb.Inlines.Add(new Run(lines[i]));
                if (i + 1 < lines.Length)
                    tb.Inlines.Add(new LineBreak());
            }

            tt.IsOpen = true;
            tt.StaysOpen = false;
        }

        private void ServerHelp(object sender, RoutedEventArgs e)
        {
            ToolTip tt = ((ToolTip)((Hyperlink)sender).ToolTip);
            tt.IsOpen = true;
            tt.StaysOpen = false;
            tt.Closed += ToolTipClosed;
            e.Handled = true;
        }

        private void ToolTipClosed(object sender, RoutedEventArgs e)
        {
            ToolTip tt = (ToolTip)sender;
            tt.Closed -= ToolTipClosed;
            tt.StaysOpen = true;
            tt.IsOpen = false;
            e.Handled = true;
        }


        private void TusLoginHelp(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            MessageBox.Show(this,
                "TUS is The Ultimate Site. It is a website for Worms: Armageddon, which has a lot of things for the game, such as: leagues, cups, tournaments, schemes, maps, forum, etc." + Environment.NewLine + Environment.NewLine +
                "It also supports snoopers. If you login with your tus account into the snooper, then people will know your TUS nick name even if your snooper nick is different and you will be marked as online on the site, so people who are only surfing the site will also know, that you are online." + Environment.NewLine + Environment.NewLine +
                "You can also set your snooper nick and password on TUS site in your Account Settings" + Environment.NewLine + Environment.NewLine +
                "More info: http://www.tus-wa.com/forums/announcements/bringing-back-wn-ranks-and-registered-usernames-4819/", "What is TUS login?", MessageBoxButton.OK, MessageBoxImage.Information
            );
        }

        private void WindowClosing(object sender, CancelEventArgs e)
        {
            /*
            if (loadSettings.IsBusy)
            {
                loadSettings.CancelAsync();
                e.Cancel = true;
                return;
            }
            */
            
            if (tusLoginWorker != null && tusLoginWorker.IsBusy)
            {
                tusLoginWorker.CancelAsync();
                e.Cancel = true;
                return;
            }
            
            if (!loggedIn && ircThread != null && ircThread.IsAlive)
            {
                wormNetC.CancelAsync();
                e.Cancel = true;
                return;
            }

            if (loggedIn)
            {
                wormNetC.ConnectionState -= ConnectionState;
            }

            Properties.Settings.Default.ServerAddresses = ServerList.Serialize();
            Properties.Settings.Default.Save();
            myNotifyIcon.Dispose();
            myNotifyIcon = null;

            if (tusLoginWorker != null)
            {
                tusLoginWorker.Dispose();
                tusLoginWorker = null;
            }
        }

        private void LogInWithEnter(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                e.Handled = true; 
                LogIn();
            }
        }

        private void ServerListEditClicked(object sender, RoutedEventArgs e)
        {
            SortedObservableCollection<string> serverList = new SortedObservableCollection<string>();
            foreach (var server in this.ServerList)
                serverList.Add(server);

            ListEditor window = new ListEditor(serverList, "Server list", ListEditor.ListModes.Normal);
            window.Closing += ServerListWindowClosed;
            window.ItemRemoved += RemoveServer;
            window.ItemAdded += AddServer;
            window.Owner = this;
            window.ShowDialog();
            e.Handled = true;
        }

        private void AddServer(string item)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                this.ServerList.Add(item);
            }
            ));
        }

        private void RemoveServer(string item)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                this.ServerList.Remove(item);
            }
            ));
        }

        private void ServerListWindowClosed(object sender, CancelEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                var obj = sender as ListEditor;
                obj.Closing -= ServerListWindowClosed;
                obj.ItemRemoved -= RemoveServer;
                obj.ItemAdded -= AddServer;
            }
            ));
        }

        private void ExitClicked(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            this.Close();
        }

        private void SettingsClicked(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            UserSettings window = new UserSettings();
            window.Owner = this;
            window.ShowDialog();
        }

        private void CanExecuteCustomCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void NotifyIconDoubleClick(object sender, ExecutedRoutedEventArgs e)
        {
            this.Activate();
        }

        public void Dispose()
        {
            if (tusLoginWorker != null)
            {
                tusLoginWorker.Dispose();
                tusLoginWorker = null;
            }

            if (myNotifyIcon != null)
            {
                myNotifyIcon.Dispose();
                myNotifyIcon = null;
            }
        }
    }
}
