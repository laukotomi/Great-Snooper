using MahApps.Metro.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;

namespace MySnooper
{
    public partial class Login : MetroWindow
    {
        private enum TUSStates { OK, TUSError, ConnectionError, UserError }

        // Program datas
        private List<string> ServerList;
        private Dictionary<string, string> Leagues;
        private List<Dictionary<string, string>> News;
        private string LatestVersion;
        private bool FailFlag;
        private Regex NickRegex, NickRegexTUS;
        private Regex NickRegex2, NickRegex2TUS;
        private Regex ClanRegex, ClanRegexTUS;
        private Regex DateRegex;
        private BackgroundWorker loadSettings;


        // User datas
        private string ServerAddress;
        private int ServerPort;
        private CountryClass NickCountry;
        private string NickName;
        private string NickClan;
        private int NickRank;

        // IRC Thread
        private IRCCommunicator WormNetC;
        private Thread IrcThread;
        private bool LoggedIn = false;

        // TUS communicator
        private TUSStates TUSState;
        BackgroundWorker TUSLoginWorker;
        private string TUSPassword;
        private string TUSNickStr;

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

            InitializeComponent();

            NickRegex = new Regex(@"^[a-z`]", RegexOptions.IgnoreCase);
            NickRegex2 = new Regex(@"^[a-z`][a-z0-9`\-]*$", RegexOptions.IgnoreCase);
            ClanRegex = new Regex(@"^[a-z0-9]*$", RegexOptions.IgnoreCase);
            NickRegexTUS = new Regex(@"^[^a-z`]+", RegexOptions.IgnoreCase);
            NickRegex2TUS = new Regex(@"[^a-z0-9`\-]", RegexOptions.IgnoreCase);
            ClanRegexTUS = new Regex(@"[^a-z0-9]", RegexOptions.IgnoreCase);
            DateRegex = new Regex(@"[^0-9]");

            ServerList = new List<string>();
            Leagues = new Dictionary<string, string>();
            News = new List<Dictionary<string, string>>();
            FailFlag = false;

            TUSLoginWorker = new BackgroundWorker();
            TUSLoginWorker.WorkerSupportsCancellation = true;
            TUSLoginWorker.DoWork += TUSLoginWorker_DoWork;
            TUSLoginWorker.RunWorkerCompleted += TUSLoginWorker_RunWorkerCompleted;

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


            if (Properties.Settings.Default.WaExe.Length == 0)
            {
                try
                {
                    string WALoc = (string)(Registry.GetValue(@"HKEY_CURRENT_USER\Software\Team17SoftwareLTD\WormsArmageddon", "PATH", null));
                    if (WALoc != null && File.Exists(WALoc + @"\WA.exe"))
                    {
                        Properties.Settings.Default.WaExe = WALoc + @"\WA.exe";
                        Properties.Settings.Default.Save();
                    }
                }
                catch (Exception e)
                {
                    ErrorLog.log(e);
                }

                if (Properties.Settings.Default.WaExe.Length == 0 && !Properties.Settings.Default.WAExeAsked)
                {
                    Properties.Settings.Default.WAExeAsked = true;
                    Properties.Settings.Default.Save();

                    MessageBox.Show(this, "Ooops, seems like the program can't find your WA.exe! Please set the location of your WA.exe!", "WA.exe needs to be located", MessageBoxButton.OK, MessageBoxImage.Information);
                    OpenFileDialog dlg = new OpenFileDialog();
                    dlg.Filter = "Worms Armageddon Exe|*.exe";

                    // Display OpenFileDialog by calling ShowDialog method 
                    Nullable<bool> result = dlg.ShowDialog();

                    // Get the selected file name
                    if (result == true)
                    {
                        // Set the WA.exe
                        Properties.Settings.Default.WaExe = dlg.FileName;
                        Properties.Settings.Default.Save();
                    }
                }
            }

            loadSettings = new BackgroundWorker();
            loadSettings.WorkerSupportsCancellation = true;
            loadSettings.DoWork += LoadSettings;
            loadSettings.RunWorkerCompleted += SettingsLoaded;
            loadSettings.RunWorkerAsync();
        }

        private void LoadSettings(object sender, DoWorkEventArgs e)
        {
            WormNetCharTable.GenerateThings();

            string settingsPath = Directory.GetParent(Directory.GetParent(System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath).FullName).FullName;

            using (WebClient webClient = new WebClient() { Proxy = null })
            {
                webClient.DownloadFile("http://mediacreator.hu/SnooperSettings.xml", settingsPath + @"\Settings.xml");
            }

            if (File.Exists(settingsPath + @"\Settings.xml")) {                
                using (XmlReader xml = XmlReader.Create(settingsPath + @"\Settings.xml"))
                {
                    xml.ReadToFollowing("servers");
                    using (XmlReader inner = xml.ReadSubtree())
                    {
                        while (inner.ReadToFollowing("server"))
                        {
                            inner.MoveToFirstAttribute();
                            ServerList.Add(inner.Value);
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
                            Leagues.Add(inner.Value, name);
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

                            News.Add(newsthings);
                        }
                    }

                    xml.ReadToFollowing("version");
                    xml.MoveToFirstAttribute();
                    LatestVersion = xml.Value;
                }
            }
            else
                ServerList.Add("wormnet1.team17.com");

            if (loadSettings.CancellationPending)
                e.Cancel = true;

            // Delete old logs
            if (Directory.Exists(settingsPath + @"\Logs"))
            {
                string date = DateRegex.Replace(DateTime.Now.ToString("d"), "-");
                if (date != Properties.Settings.Default.TimeLogsDeleted)
                {
                    Properties.Settings.Default.TimeLogsDeleted = date;
                    Properties.Settings.Default.Save();

                    string[] dirs = Directory.GetDirectories(settingsPath + @"\Logs");
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
        }


        private void SettingsLoaded(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                this.Close();
                return;
            }

            if (e.Error != null)
            {
                ErrorLog.log(e.Error);
                MessageBox.Show(this, "Failed to load the common settings!" + Environment.NewLine + "Probably you don't have internet connection or the program needs to be updated.", "Fail", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (Math.Sign(App.getVersion().CompareTo(LatestVersion)) == -1) // we need update only if it is newer than this version
            {

                MessageBoxResult result = MessageBox.Show(this, "There is a new update available for the Great Snooper!" + Environment.NewLine + "Would you like to download it now?", "Update", MessageBoxButton.YesNo, MessageBoxImage.Information);
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
                        MessageBox.Show(this, "Failed to start auto updater! Please restart Great Snooper with administrator rights! (Right click on the icon and Run as Administrator)", "Fail", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                //this.Close();
                //return;
            }


            for (int i = 0; i < ServerList.Count; i++)
            {
                Server.Items.Add(ServerList[i]);
            }

            if (Properties.Settings.Default.ServerAddress.Length > 0)
            {
                if (!Server.Items.Contains(Properties.Settings.Default.ServerAddress))
                    Server.Items.Add(Properties.Settings.Default.ServerAddress);
                Server.SelectedItem = Properties.Settings.Default.ServerAddress;
            }
            else if (Server.Items.Count > 0) Server.SelectedIndex = 0;


            if (!FailFlag && Properties.Settings.Default.AutoLogIn && !Properties.Settings.Default.ShowLoginScreen)
            {
                LogIn(null, null);
            }
            else
            {
                Container.IsEnabled = true;
                LoadingRing.IsActive = false;
            }
        }


        private void LogIn(object sender, RoutedEventArgs e)
        {
            if (e != null)
                e.Handled = true;

            ServerAddress = Server.Text.Trim().ToLower();
            if (ServerAddress.Length == 0)
            {
                MakeErrorTooltip(Server, "Please choose a server!");
                return;
            }

            int colon;
            if ((colon = ServerAddress.IndexOf(':')) != -1)
            {
                string portstr = ServerAddress.Substring(colon + 1);
                if (!int.TryParse(portstr, out ServerPort))
                    ServerPort = 6667;
            }
            else ServerPort = 6667;


            switch (LoginTypeChooser.SelectedIndex)
            {
                // Simple login
                case 0:
                    NickName = Nick.Text.Trim();
                    NickClan = Clan.Text.Trim();

                    if (NickName.Length == 0)
                    {
                        MakeErrorTooltip(Nick, "Please enter your nickname!");
                    }
                    else if (!NickRegex.IsMatch(NickName))
                    {
                        MakeErrorTooltip(Nick, "Your nickname should begin with a character\nof the English aplhabet or with ` character!");
                    }
                    else if (!NickRegex2.IsMatch(NickName))
                    {
                        MakeErrorTooltip(Nick, "Your nickname contains one or more\nforbidden characters! Use characters from\nthe English alphabet, numbers, - or `!");
                    }
                    else if (!ClanRegex.IsMatch(NickClan))
                    {
                        MakeErrorTooltip(Clan, "Your clan can contain only characters\nfrom the English alphabet or numbers");
                    }
                    else
                    {
                        Container.IsEnabled = false;
                        LoadingRing.IsActive = true;

                        NickCountry = Country.SelectedValue as CountryClass;

                        Properties.Settings.Default.LoginType = "simple";
                        Properties.Settings.Default.ServerAddress = ServerAddress;
                        Properties.Settings.Default.AutoLogIn = AutoLogIn.IsChecked.Value;
                        if (AutoLogIn.IsChecked.Value)
                            Properties.Settings.Default.ShowLoginScreen = false;
                        Properties.Settings.Default.UserName = NickName;
                        Properties.Settings.Default.UserClan = NickClan;
                        Properties.Settings.Default.UserCountry = NickCountry.ID;
                        Properties.Settings.Default.UserRank = Rank.SelectedIndex;
                        Properties.Settings.Default.Save();

                        if (NickClan.Length == 0)
                            NickClan = "Username";

                        NickRank = Rank.SelectedIndex;


                        GlobalManager.User = new Client(NickName, NickCountry, NickClan, NickRank, true);
                        // Initialize the WormNet Communicator
                        WormNetC = new IRCCommunicator(ServerAddress, ServerPort);
                        WormNetC.ConnectionState += ConnectionState;
                        // Start WormNet communicator thread
                        IrcThread = new Thread(new ThreadStart(WormNetC.run));
                        IrcThread.Start();
                    }
                    break;

                // TUS login
                case 1:
                    NickName = TUSNick.Text.Trim();
                    TUSPassword = TUSPass.Password.Trim();

                    if (NickName.Length == 0)
                    {
                        MakeErrorTooltip(TUSNick, "Please enter your nickname!");
                    }
                    else if (!NickRegex.IsMatch(NickName))
                    {
                        MakeErrorTooltip(TUSNick, "Your nickname should begin with a character\nof the English aplhabet or with ` character!");
                    }
                    else if (!NickRegex2.IsMatch(NickName))
                    {
                        MakeErrorTooltip(TUSNick, "Your nickname contains one or more\nforbidden characters! Use characters from\nthe English alphabet, numbers, - or `!");
                    }
                    else if (TUSPassword.Length == 0)
                    {
                        MakeErrorTooltip(TUSPass, "Please enter your password!");
                    }
                    else
                    {
                        Container.IsEnabled = false;
                        LoadingRing.IsActive = true;

                        Properties.Settings.Default.LoginType = "tus";
                        Properties.Settings.Default.ServerAddress = ServerAddress;
                        Properties.Settings.Default.AutoLogIn = AutoLogIn.IsChecked.Value;
                        if (AutoLogIn.IsChecked.Value)
                            Properties.Settings.Default.ShowLoginScreen = false;
                        Properties.Settings.Default.TusNick = NickName;
                        Properties.Settings.Default.TusPass = TUSPassword;
                        Properties.Settings.Default.Save();

                        TUSState = TUSStates.TUSError;
                        TUSLoginWorker.RunWorkerAsync();
                    }
                    break;
            }
        }

        private void TUSLoginWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                this.Close();
                return;
            }

            switch (TUSState)
            {
                case TUSStates.TUSError:
                    MessageBox.Show(this, "An error occoured! Please try again!", "Fail", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;

                case TUSStates.OK:
                    // Initialize the WormNet Communicator
                    GlobalManager.User = new Client(NickName, NickCountry, NickClan, NickRank, true) { TusNick = TUSNickStr };
                    WormNetC = new IRCCommunicator(ServerAddress, ServerPort);
                    WormNetC.ConnectionState += ConnectionState;
                    // Start WormNet communicator thread
                    IrcThread = new Thread(new ThreadStart(WormNetC.run));
                    IrcThread.Start();
                    break;

                case TUSStates.UserError:
                    MessageBox.Show(this, "The given username or password was incorrent!", "Wrong username or password", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;

                case TUSStates.ConnectionError:
                    MessageBox.Show(this, "The communication with TUS has failed. Please try again!", "Fail", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
            }

            if (TUSState != TUSStates.OK)
            {
                Container.IsEnabled = true;
                LoadingRing.IsActive = false;
            }
        }

        private void TUSLoginWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                using (WebClient tusRequest = new WebClient() { Proxy = null })
                {
                    string testlogin = tusRequest.DownloadString("http://www.tus-wa.com/testlogin.php?u=" + System.Web.HttpUtility.UrlEncode(NickName) + "&p=" + System.Web.HttpUtility.UrlEncode(TUSPassword));
                    if (testlogin[0] == '1') // 1 sToOMiToO
                    {
                        NickName = testlogin.Substring(2);
                        NickName = NickRegexTUS.Replace(NickName, ""); // Remove bad characters
                        NickName = NickRegex2TUS.Replace(NickName, ""); // Remove bad characters

                        if (TUSLoginWorker.CancellationPending)
                        {
                            e.Cancel = true;
                            return;
                        }
                        Thread.Sleep(2500); // Tus doesn't like more than one request in 2 seconds
                        if (TUSLoginWorker.CancellationPending)
                        {
                            e.Cancel = true;
                            return;
                        }

                        string userlist = tusRequest.DownloadString("http://www.tus-wa.com/userlist.php?league=classic");
                        string[] rows = userlist.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < rows.Length; i++)
                        {
                            if (rows[i].Substring(0, NickName.Length) == NickName)
                            {
                                TUSState = TUSStates.OK;
                                string[] data = rows[i].Split(new char[] { ' ' });

                                TUSNickStr = data[1];
                                NickClan = ClanRegexTUS.Replace(data[5], ""); // Remove bad characters
                                if (NickClan.Length == 0)
                                    NickClan = "Username";

                                if (int.TryParse(data[2].Substring(1), out NickRank))
                                    NickRank--;
                                else
                                    NickRank = 13;

                                NickCountry = CountriesClass.GetCountryByCC(data[3].ToUpper());
                                break;
                            }
                        }
                    }
                    else
                    {
                        TUSState = TUSStates.UserError;
                    }
                }
            }
            catch (Exception ex)
            {
                TUSState = TUSStates.ConnectionError;
                ErrorLog.log(ex);
            }

            if (TUSLoginWorker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
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
            string[] lines = text.Split(new char[] { '\n' });
            for (int i = 0; i < lines.Length; i++)
            {
                tb.Inlines.Add(new Run(lines[i]));
                if (i + 1 < lines.Length)
                    tb.Inlines.Add(new LineBreak());
            }

            tt.IsOpen = true;
            tt.StaysOpen = false;
        }


        private void ConnectionState(IRCConnectionStates state)
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                switch (state)
                {
                    case IRCConnectionStates.OK:
                        MainWindow MW = new MainWindow(WormNetC, IrcThread, ServerAddress, Leagues, News);
                        MW.Show();
                        LoggedIn = true;
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
            if (loadSettings.IsBusy)
            {
                loadSettings.CancelAsync();
                e.Cancel = true;
            }
            else if (TUSLoginWorker.IsBusy)
            {
                TUSLoginWorker.CancelAsync();
                e.Cancel = true;
            }
            else if (!LoggedIn && IrcThread != null && IrcThread.IsAlive)
            {
                WormNetC.CancelAsync();
                e.Cancel = true;
            }
            else if (LoggedIn)
            {
                WormNetC.ConnectionState -= ConnectionState;
            }
        }
    }
}
