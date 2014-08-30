using MahApps.Metro.Controls;
using System.Windows.Controls;

namespace MySnooper
{
    public partial class MainWindow : MetroWindow
    {
        private IRCManipulator WormNetM;


        // Highlight (If a message contained your name in the messages of one of the channels)
        private void Highlight(Channel ch)
        {
            if (Channels.SelectedItem == null || !ch.BeepSoundPlay)
                return;

            Channel msgch = (Channel)((TabItem)Channels.SelectedItem).Tag;

            if (ch.BeepSoundPlay && (msgch != ch || !IsWindowFocused))
            {
                ch.NewMessages = true;
                ch.BeepSoundPlay = false;
                this.FlashWindow();
                if (Properties.Settings.Default.HBeepEnabled && SoundEnabled && HighlightBeep != null)
                {
                    try
                    {
                        HighlightBeep.Play();
                    }
                    catch (System.Exception e)
                    {
                        ErrorLog.log(e);
                    }
                }
            }
        }
    }
}
