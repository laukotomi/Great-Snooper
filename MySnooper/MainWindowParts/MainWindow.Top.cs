using MahApps.Metro.Controls;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
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
        private bool ChatMode = false;
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
         * Open log folder
         */
        private void OpenLogs(object sender, RoutedEventArgs e)
        {
            string settingsPath = Directory.GetParent(Directory.GetParent(System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath).FullName).FullName;
            if (!Directory.Exists(settingsPath + @"\Logs"))
                Directory.CreateDirectory(settingsPath + @"\Logs");

            string logpath = settingsPath + @"\Logs";

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

            e.Handled = true;
        }

        /*
         * Chat Mode On / Off changed
         */
        private void ChatModeOnOffClick(object sender, RoutedEventArgs e)
        {
            if (ChatMode)
            {
                ChatMode = false;
                ChatModeImage.Source = ChatModeOffImage;
            }
            else
            {
                ChatMode = true;
                ChatModeImage.Source = ChatModeOnImage;
            }

            foreach (var item in WormNetM.ChannelList)
            {
                if (!item.Value.IsPrivMsgChannel)
                    LoadMessages(item.Value, GlobalManager.MaxMessagesDisplayed, true);
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

            e.Handled = true;
        }
    }
}
