using MahApps.Metro.Controls;
using System;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace MySnooper
{
    public partial class MainWindow : MetroWindow
    {
        /*
         * Sound On / Off variables
         */
        private bool SoundEnabled = true;
        private Image SoundOnOffImage;
        private ImageSource SoundEnabledImage;
        private BitmapImage SoundDisabledImage;

        /*
         * Chat Mode On / Off variables
         */
        private Image ChatModeImage;
        private ImageSource ChatModeOffImage;
        private BitmapImage ChatModeOnImage;

        /*
         * Chat Mode On / Off variables
         */
        private string AwayOnOffDefaultTooltip;
        private Button AwayOnOffButton;
        private Image AwayOnOffImage;
        private ImageSource AwayOffImage;
        private BitmapImage AwayOnImage;

        /*
         * League Searcher sources
         */
        private Image LeagueSearcherImage;
        private ImageSource LeagueSearcherOff;
        private BitmapImage LeagueSearcherOn;

        /*
         * League Searcher sources
         */
        private Image NotificatorImage;
        private ImageSource NotificatorOff;
        private BitmapImage NotificatorOn;



        /*
         * Open log folder
         */
        private void OpenLogs(object sender, RoutedEventArgs e)
        {
            string logpath = GlobalManager.SettingsPath + @"\Logs";
            if (!Directory.Exists(logpath))
                Directory.CreateDirectory(logpath);

            System.Diagnostics.Process.Start(logpath);
        }

        /*
         * Sound On / Off loaded
         */
        private void SoundOnOffLoaded(object sender, RoutedEventArgs e)
        {
            SoundOnOffImage = (Image)((Button)sender).Content;
            SoundEnabledImage = SoundOnOffImage.Source;

            SoundDisabledImage = new BitmapImage();
            SoundDisabledImage.DecodePixelHeight = Convert.ToInt32(SoundOnOffImage.Height);
            SoundDisabledImage.DecodePixelWidth = Convert.ToInt32(SoundOnOffImage.Width);
            SoundDisabledImage.CacheOption = BitmapCacheOption.OnLoad;
            SoundDisabledImage.BeginInit();
            SoundDisabledImage.UriSource = new Uri("pack://application:,,,/Resources/soundoff.png");
            SoundDisabledImage.EndInit();
            SoundDisabledImage.Freeze();

            e.Handled = true;
        }


        /*
         * Sound On / Off changed
         */
        private void SoundOnOffClick(object sender, RoutedEventArgs e)
        {
            if (SoundEnabled)
            {
                SoundEnabled = false;
                SoundOnOffImage.Source = SoundDisabledImage;
            }
            else
            {
                SoundEnabled = true;
                SoundOnOffImage.Source = SoundEnabledImage;
            }

            e.Handled = true;
        }

        /*
         * Chat Mode On / Off loaded
         */
        private void ChatModeOnOffLoaded(object sender, RoutedEventArgs e)
        {
            ChatModeImage = (Image)((Button)sender).Content;
            ChatModeOffImage = ChatModeImage.Source;

            ChatModeOnImage = new BitmapImage();
            ChatModeOnImage.DecodePixelHeight = Convert.ToInt32(ChatModeImage.Height);
            ChatModeOnImage.DecodePixelWidth = Convert.ToInt32(ChatModeImage.Width);
            ChatModeOnImage.CacheOption = BitmapCacheOption.OnLoad;
            ChatModeOnImage.BeginInit();
            ChatModeOnImage.UriSource = new Uri("pack://application:,,,/Resources/chatmodeon.png");
            ChatModeOnImage.EndInit();
            ChatModeOnImage.Freeze();

            if (Properties.Settings.Default.ChatMode)
                ChatModeImage.Source = ChatModeOnImage;

            e.Handled = true;
        }

        /*
         * Chat Mode On / Off changed
         */
        private void ChatModeOnOffClick(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.ChatMode)
            {
                Properties.Settings.Default.ChatMode = false;
                ChatModeImage.Source = ChatModeOffImage;
            }
            else
            {
                Properties.Settings.Default.ChatMode = true;
                ChatModeImage.Source = ChatModeOnImage;
            }
            Properties.Settings.Default.Save();

            for (int i = 0; i < servers.Count; i++)
            {
                foreach (var item in servers[i].ChannelList)
                {
                    if (!item.Value.IsPrivMsgChannel && item.Value.Joined)
                        LoadMessages(item.Value, GlobalManager.MaxMessagesDisplayed, true);
                }
            }
            e.Handled = true;
        }


        /*
         * League Searcher image loaded
         */
        private void LeagueSearcherLoaded(object sender, RoutedEventArgs e)
        {
            LeagueSearcherImage = (Image)((Button)sender).Content;
            LeagueSearcherOff = LeagueSearcherImage.Source;

            LeagueSearcherOn = new BitmapImage();
            LeagueSearcherOn.DecodePixelHeight = Convert.ToInt32(LeagueSearcherImage.Height);
            LeagueSearcherOn.DecodePixelWidth = Convert.ToInt32(LeagueSearcherImage.Width);
            LeagueSearcherOn.CacheOption = BitmapCacheOption.OnLoad;
            LeagueSearcherOn.BeginInit();
            LeagueSearcherOn.UriSource = new Uri("pack://application:,,,/Resources/searching.png");
            LeagueSearcherOn.EndInit();
            LeagueSearcherOn.Freeze();

            e.Handled = true;
        }


        /*
         * Away On / Off loaded
         */
        private void AwayOnOffLoaded(object sender, RoutedEventArgs e)
        {
            AwayOnOffButton = (Button)sender;
            AwayOnOffImage = (Image)AwayOnOffButton.Content;
            AwayOffImage = AwayOnOffImage.Source;
            AwayOnOffDefaultTooltip = AwayOnOffButton.ToolTip.ToString();

            AwayOnImage = new BitmapImage();
            AwayOnImage.DecodePixelHeight = Convert.ToInt32(AwayOnOffImage.Height);
            AwayOnImage.DecodePixelWidth = Convert.ToInt32(AwayOnOffImage.Width);
            AwayOnImage.CacheOption = BitmapCacheOption.OnLoad;
            AwayOnImage.BeginInit();
            AwayOnImage.UriSource = new Uri("pack://application:,,,/Resources/away.png");
            AwayOnImage.EndInit();
            AwayOnImage.Freeze();

            e.Handled = true;
        }

        private void NotificatorOnOffLoaded(object sender, RoutedEventArgs e)
        {
            NotificatorImage = (Image)((Button)sender).Content;
            NotificatorOff = NotificatorImage.Source;

            NotificatorOn = new BitmapImage();
            NotificatorOn.DecodePixelHeight = Convert.ToInt32(NotificatorImage.Height);
            NotificatorOn.DecodePixelWidth = Convert.ToInt32(NotificatorImage.Width);
            NotificatorOn.CacheOption = BitmapCacheOption.OnLoad;
            NotificatorOn.BeginInit();
            NotificatorOn.UriSource = new Uri("pack://application:,,,/Resources/notificatoron.png");
            NotificatorOn.EndInit();
            NotificatorOn.Freeze();

            e.Handled = true;
        }

        private bool SliderThumb = false;
        private bool CanChangeVolume = false;

        private void VolumeSliderLoaded(object sender, RoutedEventArgs e)
        {
            CanChangeVolume = true;
            Slider slider = (Slider)sender;
            slider.Value = Properties.Settings.Default.Volume;
        }

        private void VolumeChanged(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            Slider slider = (Slider)sender;
            ChangeVolume(slider.Value);
        }

        private void ChangeVolume(double value)
        {
            if (!CanChangeVolume)
                return;

            Properties.Settings.Default.Volume = Convert.ToInt32(value);
            Properties.Settings.Default.Save();

            // Calculate the volume that's being set. BTW: this is a trackbar!
            uint NewVolume = (uint)((ushort.MaxValue / 100) * value);
            // Set the same volume for both the left and the right channels
            uint NewVolumeAllChannels = ((NewVolume & 0x0000ffff) | ((uint)NewVolume << 16));
            // Set the volume
            waveOutSetVolume(IntPtr.Zero, NewVolumeAllChannels);

            SliderThumb = false;

            SoundPlayer sp;
            if (soundPlayers.TryGetValue("PMBeep", out sp))
            {
                try
                {
                    sp.Play();
                }
                catch (Exception ex)
                {
                    ErrorLog.Log(ex);
                }
            }
        }

        private void SliderChangeWithThumb(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            SliderThumb = true;
        }

        private void SliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!SliderThumb)
            {
                Slider slider = (Slider)sender;
                ChangeVolume(slider.Value);
            }
        }
    }
}
