using MahApps.Metro.Controls;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace MySnooper
{
    public delegate void GameHostedDelegate(object sender, GameHostedEventArgs e);

    public partial class Hosting : MetroWindow
    {
        private string ServerAddress;
        private string ChannelName;
        private string ChannelScheme;
        private string CC;

        private Regex PassRegex;

        public event GameHostedDelegate GameHosted;

        public Hosting() { } // Never used, but visual stdio throws an error if not exists
        public Hosting(string ServerAddress, string ChannelName, string ChannelScheme, string CC)
        {
            this.ServerAddress = ServerAddress;
            this.ChannelName = ChannelName;
            this.ChannelScheme = ChannelScheme;
            this.CC = CC;

            InitializeComponent();

            PassRegex = new Regex(@"^[a-z]*$", RegexOptions.IgnoreCase);

            GameName.Text = Properties.Settings.Default.HostGameName;
            WormNat2.IsChecked = Properties.Settings.Default.HostUseWormnat;
            InfoToChannel.IsChecked = Properties.Settings.Default.HostInfoToChannel;
        }

        private void WormNatHostingHelp(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "About WormNat 2:" + Environment.NewLine + Environment.NewLine +
                "If you host with the game, then it will create the server on your computer and it will wait for players to join on a specified port (usually 17011). But most of the routers and firewall programs don't allow incoming connections, so nobody can join your game. You will need to enable port forwarding on your router and/or configure your firewall if you use one in order to enable players to join your game." + Environment.NewLine + Environment.NewLine +
                "WormNat2 uses a technique, that you can host games without incoming connections so your host will work properly. To make this possible it uses a dedicated proxy server, which actually handles all the connections." + Environment.NewLine + Environment.NewLine +
                "But since there is no support for other proxy servers, we would much rather that you configure your system to use normal way of hosting your games and use WormNat2 only if nothing else works so the proxy server will not be overloaded." + Environment.NewLine + Environment.NewLine +
                "You can find easy setup guides at http://worms2d.info/Hosting" + Environment.NewLine + Environment.NewLine +
                "Enable loading WormKit modules are NOT needed to use WormNat2 hosting feature of the snooper!",
                "About WormNat 2", MessageBoxButton.OK, MessageBoxImage.Information
            );
        }


        private void CreateGame(object sender, RoutedEventArgs e)
        {
            if (!PassRegex.IsMatch(GamePassword.Text))
            {
                MessageBox.Show(this, "Password can contain only characters from the English alphabet!", "Wrong characters", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string WN = "0";
            if (WormNat2.IsChecked.Value)
            {
                WN = "1";

                MessageBox.Show(
                    "Greetings WormNAT2 user!" + Environment.NewLine + Environment.NewLine +
                    "This is a reminder message to remind you that WormNAT2" + Environment.NewLine +
                    "is a free service. Using WormNAT2 tunnels all data" + Environment.NewLine +
                    "through a proxy server hosted by the community, thus" + Environment.NewLine +
                    "consuming bandwidth and other resources. Therefore," + Environment.NewLine +
                    "we''d like to ask you to only use WormNAT2 when you" + Environment.NewLine +
                    "have already tried configuring hosting the proper way." + Environment.NewLine + Environment.NewLine +
                    "Don''t forget that you can find instructions on how" + Environment.NewLine +
                    "to set up hosting here:" + Environment.NewLine + Environment.NewLine +
                    "http://worms2d.info/Hosting", "A friendly reminder", MessageBoxButton.OK, MessageBoxImage.Information);
            }


            // A stringbuilder, because we want to modify the game name
            StringBuilder sb = new StringBuilder(GameName.Text.Trim());

            // Remove illegal characters
            for (int i = 0; i < sb.Length; i++)
            {
                char ch = sb[i];
                if (!WormNetCharTable.EncodeGame.ContainsKey(ch))
                {
                    sb.Remove(i, 1);
                    i -= 1;
                }
            }
            string tmp = sb.ToString().Trim();
            sb.Clear();
            sb.Append(tmp);

            // Save settings
            Properties.Settings.Default.HostGameName = tmp;
            Properties.Settings.Default.HostUseWormnat = WormNat2.IsChecked.Value;
            Properties.Settings.Default.HostInfoToChannel = InfoToChannel.IsChecked.Value;
            Properties.Settings.Default.Save();


            // Encode the Game name text
            for (int i = 0; i < sb.Length; i++)
            {
                char ch = sb[i];
                if (ch == '"' || ch == '&' || ch == '\'' || ch == '<' || ch == '>' || ch == '\\')
                {
                    sb.Remove(i, 1);
                    sb.Insert(i, "%" + WormNetCharTable.EncodeGame[ch].ToString("X"));
                    i += 2;
                }
                else if (ch == '#' || ch == '+' || ch == '%')
                {
                    sb.Remove(i, 1);
                    sb.Insert(i, "%" + WormNetCharTable.EncodeGame[ch].ToString("X"));
                    i += 2;
                }
                else if (ch == ' ')
                {
                    sb.Remove(i, 1);
                    sb.Insert(i, "%A0");
                    i += 2;
                }
                else if (WormNetCharTable.EncodeGame[ch] >= 0x80)
                {
                    sb.Remove(i, 1);
                    sb.Insert(i, "%" + WormNetCharTable.EncodeGame[ch].ToString("X"));
                    i += 2;
                }
            }


            Container.IsEnabled = false;

            if (e != null)
                e.Handled = true;

            GameHostedEventArgs args = new GameHostedEventArgs(ServerAddress + " \"" + Properties.Settings.Default.WaExe + "\" " + GlobalManager.User.Name + " \"" + sb.ToString() + "\" \"" + GamePassword.Text + "\" " + ChannelName + " " + ChannelScheme + " " + GlobalManager.User.Country.ID.ToString() + " " + CC + " " + WN, ExitSnooper.IsChecked.Value);
            GameHosted.BeginInvoke(this, args, null, null);
        }

        public void RestoreHostButton()
        {
            this.Dispatcher.Invoke(new Action(delegate()
            {
                Container.IsEnabled = true;
            }
            ));
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
            e.Handled = true;
        }

        private void CreateGameWithEnter(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CreateGame(null, null);
                e.Handled = true;
            }
        }

        private void MetroWindow_ContentRendered(object sender, EventArgs e)
        {
            GameName.Focus();
        }
    }
}
