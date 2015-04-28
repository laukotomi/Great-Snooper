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
        private bool soundEnabled = false;
        private Image soundOnOffImage;
        private ImageSource soundEnabledImage;
        private BitmapImage soundDisabledImage;

        /*
         * Chat Mode On / Off variables
         */
        private Image chatModeImage;
        private ImageSource chatModeOffImage;
        private BitmapImage chatModeOnImage;

        /*
         * Chat Mode On / Off variables
         */
        private string awayOnOffDefaultTooltip;
        private Button awayOnOffButton;
        private Image awayOnOffImage;
        private ImageSource awayOffImage;
        private BitmapImage awayOnImage;

        /*
         * League Searcher sources
         */
        private Image leagueSearcherImage;
        private ImageSource leagueSearcherOff;
        private BitmapImage leagueSearcherOn;

        /*
         * League Searcher sources
         */
        private Image notificatorImage;
        private ImageSource notificatorOff;
        private BitmapImage notificatorOn;



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
            soundOnOffImage = (Image)((Button)sender).Content;
            soundEnabledImage = soundOnOffImage.Source;

            soundDisabledImage = new BitmapImage();
            soundDisabledImage.DecodePixelHeight = Convert.ToInt32(soundOnOffImage.Height);
            soundDisabledImage.DecodePixelWidth = Convert.ToInt32(soundOnOffImage.Width);
            soundDisabledImage.CacheOption = BitmapCacheOption.OnLoad;
            soundDisabledImage.BeginInit();
            soundDisabledImage.UriSource = new Uri("pack://application:,,,/Resources/soundoff.png");
            soundDisabledImage.EndInit();
            soundDisabledImage.Freeze();

            if (Properties.Settings.Default.MuteState)
                soundOnOffImage.Source = soundDisabledImage;

            e.Handled = true;
        }


        /*
         * Sound On / Off changed
         */
        private void SoundOnOffClick(object sender, RoutedEventArgs e)
        {
            if (soundEnabled)
                soundOnOffImage.Source = soundDisabledImage;
            else
                soundOnOffImage.Source = soundEnabledImage;

            soundEnabled = !soundEnabled;
            Properties.Settings.Default.MuteState = !Properties.Settings.Default.MuteState;
            Properties.Settings.Default.Save();

            e.Handled = true;
        }

        /*
         * Chat Mode On / Off loaded
         */
        private void ChatModeOnOffLoaded(object sender, RoutedEventArgs e)
        {
            chatModeImage = (Image)((Button)sender).Content;
            chatModeOffImage = chatModeImage.Source;

            chatModeOnImage = new BitmapImage();
            chatModeOnImage.DecodePixelHeight = Convert.ToInt32(chatModeImage.Height);
            chatModeOnImage.DecodePixelWidth = Convert.ToInt32(chatModeImage.Width);
            chatModeOnImage.CacheOption = BitmapCacheOption.OnLoad;
            chatModeOnImage.BeginInit();
            chatModeOnImage.UriSource = new Uri("pack://application:,,,/Resources/chatmodeon.png");
            chatModeOnImage.EndInit();
            chatModeOnImage.Freeze();

            if (Properties.Settings.Default.ChatMode)
                chatModeImage.Source = chatModeOnImage;

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
                chatModeImage.Source = chatModeOffImage;
            }
            else
            {
                Properties.Settings.Default.ChatMode = true;
                chatModeImage.Source = chatModeOnImage;
            }
            Properties.Settings.Default.Save();

            for (int i = 0; i < Servers.Count; i++)
            {
                foreach (var item in Servers[i].ChannelList)
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
            leagueSearcherImage = (Image)((Button)sender).Content;
            leagueSearcherOff = leagueSearcherImage.Source;

            leagueSearcherOn = new BitmapImage();
            leagueSearcherOn.DecodePixelHeight = Convert.ToInt32(leagueSearcherImage.Height);
            leagueSearcherOn.DecodePixelWidth = Convert.ToInt32(leagueSearcherImage.Width);
            leagueSearcherOn.CacheOption = BitmapCacheOption.OnLoad;
            leagueSearcherOn.BeginInit();
            leagueSearcherOn.UriSource = new Uri("pack://application:,,,/Resources/searching.png");
            leagueSearcherOn.EndInit();
            leagueSearcherOn.Freeze();

            e.Handled = true;
        }


        /*
         * Away On / Off loaded
         */
        private void AwayOnOffLoaded(object sender, RoutedEventArgs e)
        {
            awayOnOffButton = (Button)sender;
            awayOnOffImage = (Image)awayOnOffButton.Content;
            awayOffImage = awayOnOffImage.Source;
            awayOnOffDefaultTooltip = awayOnOffButton.ToolTip.ToString();

            awayOnImage = new BitmapImage();
            awayOnImage.DecodePixelHeight = Convert.ToInt32(awayOnOffImage.Height);
            awayOnImage.DecodePixelWidth = Convert.ToInt32(awayOnOffImage.Width);
            awayOnImage.CacheOption = BitmapCacheOption.OnLoad;
            awayOnImage.BeginInit();
            awayOnImage.UriSource = new Uri("pack://application:,,,/Resources/away.png");
            awayOnImage.EndInit();
            awayOnImage.Freeze();

            e.Handled = true;
        }

        private void NotificatorOnOffLoaded(object sender, RoutedEventArgs e)
        {
            notificatorImage = (Image)((Button)sender).Content;
            notificatorOff = notificatorImage.Source;

            notificatorOn = new BitmapImage();
            notificatorOn.DecodePixelHeight = Convert.ToInt32(notificatorImage.Height);
            notificatorOn.DecodePixelWidth = Convert.ToInt32(notificatorImage.Width);
            notificatorOn.CacheOption = BitmapCacheOption.OnLoad;
            notificatorOn.BeginInit();
            notificatorOn.UriSource = new Uri("pack://application:,,,/Resources/notificatoron.png");
            notificatorOn.EndInit();
            notificatorOn.Freeze();

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
            if (!CanChangeVolume) // To prevent that the default value in the xaml code overwrite the value stored in settings
                return;

            Properties.Settings.Default.Volume = Convert.ToInt32(value);
            Properties.Settings.Default.Save();

            // Calculate the volume that's being set. BTW: this is a trackbar!
            uint NewVolume = (uint)((ushort.MaxValue / 100) * value);
            // Set the same volume for both the left and the right channels
            uint NewVolumeAllChannels = ((NewVolume & 0x0000ffff) | ((uint)NewVolume << 16));
            // Set the volume
            NativeMethods.waveOutSetVolume(IntPtr.Zero, NewVolumeAllChannels);

            SliderThumb = false;

            this.PlaySound("PMBeep");
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

        private void OpenNews(object sender, RoutedEventArgs e)
        {
            OpenNewsWindow();
            e.Handled = true;
        }
    }
}
