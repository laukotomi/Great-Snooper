using MahApps.Metro.Controls;
using System.Media;
using System.Windows.Controls;

namespace MySnooper
{
    public partial class MainWindow : MetroWindow
    {
        // Highlight (If a message contained your name in the messages of one of the channels)
        public void Highlight(Channel ch)
        {
            if (Channels.SelectedItem == null || !ch.BeepSoundPlay)
                return;

            Channel msgch = (Channel)((TabItem)Channels.SelectedItem).DataContext;

            if (ch.BeepSoundPlay && (msgch != ch || !IsWindowFocused))
            {
                ch.NewMessages = true;
                ch.BeepSoundPlay = false;
                if (Properties.Settings.Default.TrayFlashing && !IsWindowFocused)
                    this.FlashWindow();
                if (Properties.Settings.Default.TrayNotifications)
                    myNotifyIcon.ShowBalloonTip(null, "You have been highlighted!", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);

                if (Properties.Settings.Default.HBeepEnabled)
                    this.PlaySound("HBeep");
            }
        }
    }
}
