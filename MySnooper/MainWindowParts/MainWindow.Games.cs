using MahApps.Metro.Controls;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;


namespace MySnooper
{
    public partial class MainWindow : MetroWindow
    {
        // Joining a game
        private BackgroundWorker StartGame;
        private Game StartedGame;
        private Channel StartedChannel;
        private bool SilentJoined;

        // Hosting a game
        private BackgroundWorker HostGame;
        private Hosting HostingWindow;
        private bool ExitSnooper;
        private bool CloseSnooper;

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern IntPtr FindWindow(string lpClassName, string lpWindowName);


        // "Constructor"
        private void Games()
        {
            StartGame = new BackgroundWorker();
            StartGame.WorkerReportsProgress = true;
            StartGame.WorkerSupportsCancellation = true;
            StartGame.DoWork += RunGame;
            StartGame.ProgressChanged += GameStarted;
            StartGame.RunWorkerCompleted += GameClosed;

            HostGame = new BackgroundWorker();
            HostGame.WorkerReportsProgress = true;
            HostGame.WorkerSupportsCancellation = true;
            HostGame.DoWork += HostGameDoWork;
            HostGame.ProgressChanged += HostStarted;
            HostGame.RunWorkerCompleted += HostClosed;
        }

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
        private void JoinGame(Game game, bool silent = false, bool close = false)
        {
            if (StartGame.IsBusy || HostGame.IsBusy)
            {
                MessageBox.Show(this, "You are already in a game!", "Fail", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!CheckWAExe())
                return;

            if (GameListChannel != null && !SnooperClosing)
            {
                StartedGame = game;
                StartedChannel = GameListChannel;
                SilentJoined = silent;
                CloseSnooper = close;

                StartGame.RunWorkerAsync(game);
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

        // Silent join (context menu 2nd option)
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

        // Silent join (context menu 2nd option)
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


        // StartGame.DoWork
        private void RunGame(object sender, DoWorkEventArgs e)
        {
            // Start W:A with the proper GameID and Scheme
            try
            {
                var p = new System.Diagnostics.Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.FileName = Properties.Settings.Default.WaExe;
                p.StartInfo.Arguments = "wa://" + StartedGame.Address + "?gameid=" + StartedGame.ID + "&scheme=" + StartedChannel.Scheme;
                if (p.Start())
                {
                    if (StartGame.CancellationPending)
                    {
                        e.Cancel = true;
                        return;
                    }

                    StartGame.ReportProgress(50);

                    while (!p.HasExited)
                    {
                        if (StartGame.CancellationPending)
                        {
                            e.Cancel = true;
                            return;
                        }

                        if (CloseSnooper)
                        {
                            IntPtr hwnd = FindWindow("Worms2D", null);
                            if (hwnd != IntPtr.Zero)
                            {
                                e.Cancel = true;
                                return;
                            }
                        }

                        System.Threading.Thread.Sleep(250);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLog.log(ex);
            }

            if (StartGame.CancellationPending)
            {
                e.Cancel = true;
            }
        }

        // StartGame.ProgressChanged
        private void GameStarted(object sender, ProgressChangedEventArgs e)
        {
            if (Properties.Settings.Default.MessageJoinedGame && !SilentJoined)
            {
                SendMessageToChannel(">is joining a game: " + StartedGame.Name, StartedChannel);
            }
            if (Properties.Settings.Default.MarkAway)
            {
                AwayText = (Properties.Settings.Default.AwayText.Length == 0) ? "No reason specified." : Properties.Settings.Default.AwayText;
                SendMessageToChannel("/away " + AwayText, null);
            }
        }

        // StartGame.RunWorkerCompleted
        private void GameClosed(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                this.Close();
                return;
            }

            StartedGame = null;
            StartedChannel = null;
            if (Properties.Settings.Default.MarkAway)
            {
                SendMessageToChannel("/back", null);
            }
        }


        // HostGame.DoWork
        // This method is called from the MainWindow.Windows.cs, because the Hoster window will call it.
        private void HostGameDoWork(object sender, DoWorkEventArgs e)
        {
            // Start Hoster.exe
            try
            {
                var p = new System.Diagnostics.Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = System.IO.Path.GetFullPath("Hoster.exe");
                p.StartInfo.Arguments = e.Argument as string;
                if (p.Start())
                {
                    string success = p.StandardOutput.ReadLine();

                    if (success == "1")
                    {
                        if (HostGame.CancellationPending)
                        {
                            e.Cancel = true;
                            return;
                        }
                        HostGame.ReportProgress(1);

                        if (ExitSnooper)
                        {
                            e.Cancel = true;
                            return;
                        }

                        while (!p.HasExited)
                        {
                            if (HostGame.CancellationPending)
                            {
                                e.Cancel = true;
                                return;
                            }
                            System.Threading.Thread.Sleep(250);
                        }
                    }
                    else
                    {
                        if (HostGame.CancellationPending)
                        {
                            e.Cancel = true;
                            return;
                        }

                        HostGame.ReportProgress(0);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLog.log(ex);
            }

            if (HostGame.CancellationPending)
            {
                e.Cancel = true;
            }
        }

        // HostGame.ProgressChanged
        private void HostStarted(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 0)
            {
                HostingWindow.RestoreHostButton();

                MessageBox.Show(this, "You have hosted too many games recently. Please wait some minutes!", "Failed to host a game", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (e.ProgressPercentage == 1)
            {
                HostingWindow.Close();

                if (Properties.Settings.Default.HostInfoToChannel && GameListChannel != null) // GameListChannel is ok, coz it can't be changed since the Hosting window is opened
                    SendMessageToChannel(">is hosting a game: " + Properties.Settings.Default.HostGameName, GameListChannel);

                if (ExitSnooper)
                {
                    return;
                }

                if (Properties.Settings.Default.MarkAway)
                {
                    AwayText = (Properties.Settings.Default.AwayText.Length == 0) ? "No reason specified." : Properties.Settings.Default.AwayText;
                    SendMessageToChannel("/away " + AwayText, null);
                }
            }
        }

        // HostGame.RunWorkerCompleted
        private void HostClosed(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                this.Close();
                return;
            }

            if (Properties.Settings.Default.MarkAway)
            {
                SendMessageToChannel("/back", null);
            }
        }


        // When we want to refresh the game list
        // GameListForce is checked in MainWindow.xaml.cs : ClockTick()
        private void RefreshClick(object sender, RoutedEventArgs e)
        {
            GameListForce = true;
            e.Handled = true;
        }
    }
}
