using MahApps.Metro.Controls;
using System.Media;
using System.Windows.Controls;

namespace MySnooper
{
    public partial class MainWindow : MetroWindow
    {
        // Highlight (If a message contained your name in the messages of one of the channels)
        private void Highlight(Channel ch)
        {
            if (Channels.SelectedItem == null || !ch.BeepSoundPlay)
                return;

            Channel msgch = (Channel)((TabItem)Channels.SelectedItem).DataContext;

            if (ch.BeepSoundPlay && (msgch != ch || !isWindowFocused))
            {
                ch.NewMessages = true;
                ch.BeepSoundPlay = false;
                if (!isWindowFocused)
                    this.FlashWindow();
                myNotifyIcon.ShowBalloonTip(null, "You have been highlighted!", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);

                SoundPlayer sp;
                if (Properties.Settings.Default.HBeepEnabled && SoundEnabled && soundPlayers.TryGetValue("HBeep", out sp))
                {
                    try
                    {
                        sp.Play();
                    }
                    catch (System.Exception e)
                    {
                        ErrorLog.Log(e);
                    }
                }
            }
        }
    }
}
