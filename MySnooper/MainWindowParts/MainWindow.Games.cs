using MahApps.Metro.Controls;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Media;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;


namespace MySnooper
{
    public partial class MainWindow : MetroWindow
    {
        private enum StartedGameTypes { Join, Host };
        private System.Diagnostics.Process gameProcess;
        private StartedGameTypes startedGameType = StartedGameTypes.Join;

        // Joining a game
        //private Game StartedGame;
        //private Channel StartedGameChannel;
        private bool SilentJoined;
        private bool ExitSnooper;

        // Hosting a game
        private Hosting HostingWindow;

        // Check if WA.exe is set correctly
        private bool CheckWAExe()
        {
            if (Properties.Settings.Default.WaExe.Length == 0)
            {
                MessageBox.Show(this, "Please set your WA.exe in the Settings!", "WA.exe missing", MessageBoxButton.OK, MessageBoxImage.Stop);
                return false;
            }

            if (!System.IO.File.Exists(Properties.Settings.Default.WaExe))
            {
                MessageBox.Show(this, "WA.exe doesn't exist!", "WA.exe missing", MessageBoxButton.OK, MessageBoxImage.Stop);
                return false;
            }

            return true;
        }

        // The method that will start the thread which will open the game
        private void JoinGame(Game game, bool silent = false, bool exit = false)
        {
            if (gameProcess != null)
            {
                MessageBox.Show(this, "You are already in a game!", "Fail", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!CheckWAExe())
                return;

            if (SearchHere != null && Properties.Settings.Default.AskLeagueSearcherOff)
            {
                MessageBoxResult res = MessageBox.Show("Would you like to turn off league searcher?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                    ClearSpamming();
            }

            if (Notifications.Count != 0 && Properties.Settings.Default.AskNotificatorOff)
            {
                MessageBoxResult res = MessageBox.Show("Would you like to turn off notificator?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                {
                    Notifications.Clear();
                    notificatorImage.Source = notificatorOff;
                }
            }

            if (gameListChannel != null && !snooperClosing)
            {
                SilentJoined = silent;
                ExitSnooper = exit;

                startedGameType = StartedGameTypes.Join;
                lobbyWindow = IntPtr.Zero;
                gameWindow = IntPtr.Zero;
                gameProcess = new System.Diagnostics.Process();
                gameProcess.StartInfo.UseShellExecute = false;
                gameProcess.StartInfo.FileName = Properties.Settings.Default.WaExe;
                gameProcess.StartInfo.Arguments = "wa://" + game.Address + "?gameid=" + game.ID + "&scheme=" + gameListChannel.Scheme;
                if (gameProcess.Start())
                {
                    if (Properties.Settings.Default.MessageJoinedGame && !SilentJoined)
                        SendMessageToChannel(">is joining a game: " + game.Name, gameListChannel);
                    if (Properties.Settings.Default.MarkAway)
                        SendMessageToChannel("/away", null);
                }
                else
                {
                    gameProcess.Dispose();
                    gameProcess = null;
                }
            }
        }

        IntPtr lobbyWindow = IntPtr.Zero;
        IntPtr gameWindow = IntPtr.Zero;

        private void GameProcess()
        {
            if (lobbyWindow == IntPtr.Zero)
                lobbyWindow = NativeMethods.FindWindow(null, "Worms Armageddon");
            if (gameWindow == IntPtr.Zero)
                gameWindow = NativeMethods.FindWindow(null, "Worms2D");

            if (startedGameType == StartedGameTypes.Host)
            {
                // gameProcess = hoster.exe
                if (gameProcess.HasExited)
                {
                    SendMessageToChannel("/back", null);
                    gameProcess.Dispose();
                    gameProcess = null;
                    return;
                }
            }
            else
            {
                if (ExitSnooper && gameWindow != IntPtr.Zero)
                {
                    snooperClosing = true;
                    this.Close();
                    return;
                }
                // gameProcess = wa.exe
                if (gameProcess.HasExited)
                {
                    SendMessageToChannel("/back", null);
                    gameProcess.Dispose();
                    gameProcess = null;
                    return;
                }
            }

            if (Properties.Settings.Default.EnergySaveMode && lobbyWindow != IntPtr.Zero)
            {
                if (NativeMethods.GetPlacement(lobbyWindow).showCmd == ShowWindowCommands.Normal && !EnergySaveModeOn)
                    EnterEnergySaveMode();
                else if (EnergySaveModeOn)
                    LeaveEnergySaveMode();
            }
        }

        // If we'd like to join a game (double click)
        private void GameDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ListBox lb = (ListBox)sender;
            if (lb.SelectedItem != null)
            {
                JoinGame((Game)lb.SelectedItem);
            }
            lb.SelectedIndex = -1;
            e.Handled = true;
        }

        // If we'd like to join a game (context menu click)
        private void JoinGameClick(object sender, RoutedEventArgs e)
        {
            ListBox lb = (ListBox)((MenuItem)sender).Tag;
            if (lb.SelectedItem != null)
            {
                JoinGame((Game)lb.SelectedItem);
            }
            lb.SelectedIndex = -1;
            e.Handled = true;
        }

        private void JoinAndClose(object sender, RoutedEventArgs e)
        {
            ListBox lb = (ListBox)((MenuItem)sender).Tag;
            if (lb.SelectedItem != null)
            {
                JoinGame((Game)lb.SelectedItem, false, true);
            }
            lb.SelectedIndex = -1;
            e.Handled = true;
        }

        // Silent join
        private void SilentJoin(object sender, RoutedEventArgs e)
        {
            ListBox lb = (ListBox)((MenuItem)sender).Tag;
            if (lb.SelectedItem != null)
            {
                JoinGame((Game)lb.SelectedItem, true);
            }
            lb.SelectedIndex = -1;
            e.Handled = true;
        }

        // Silent join and close
        private void SilentJoinAndClose(object sender, RoutedEventArgs e)
        {
            ListBox lb = (ListBox)((MenuItem)sender).Tag;
            if (lb.SelectedItem != null)
            {
                JoinGame((Game)lb.SelectedItem, true, true);
            }
            lb.SelectedIndex = -1;
            e.Handled = true;
        }

        // When we want to refresh the game list
        // GameListForce is checked in MainWindow.xaml.cs : ClockTick()
        private void RefreshClick(object sender, RoutedEventArgs e)
        {
            GameListForce = true;
            e.Handled = true;
        }

        public void NotificatorFound(string str, Channel ch)
        {
            if (Properties.Settings.Default.TrayFlashing && !IsWindowFocused)
                this.FlashWindow();
            if (Properties.Settings.Default.TrayNotifications)
                myNotifyIcon.ShowBalloonTip(null, str, Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);

            if (Properties.Settings.Default.NotificatorSoundEnabled)
                this.PlaySound("NotificatorSound");
        }
    }
}
