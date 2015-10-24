using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;

namespace GreatSnooper.Helpers
{
    public static class Sounds
    {
        private static readonly Dictionary<string, SoundPlayer> soundPlayers = new Dictionary<string, SoundPlayer>();

        public static void Initialize()
        {
            if (File.Exists(Properties.Settings.Default.PMBeep))
                soundPlayers.Add("PMBeep", new SoundPlayer(new FileInfo(Properties.Settings.Default.PMBeep).FullName));
            if (File.Exists(Properties.Settings.Default.HBeep))
                soundPlayers.Add("HBeep", new SoundPlayer(new FileInfo(Properties.Settings.Default.HBeep).FullName));
            if (File.Exists(Properties.Settings.Default.LeagueFoundBeep))
                soundPlayers.Add("LeagueFoundBeep", new SoundPlayer(new FileInfo(Properties.Settings.Default.LeagueFoundBeep).FullName));
            if (File.Exists(Properties.Settings.Default.LeagueFailBeep))
                soundPlayers.Add("LeagueFailBeep", new SoundPlayer(new FileInfo(Properties.Settings.Default.LeagueFailBeep).FullName));
            if (File.Exists(Properties.Settings.Default.NotificatorSound))
                soundPlayers.Add("NotificatorSound", new SoundPlayer(new FileInfo(Properties.Settings.Default.NotificatorSound).FullName));
        }

        public static void PlaySoundByName(string name)
        {
            SoundPlayer sp;
            if (soundPlayers.TryGetValue(name, out sp))
                PlaySound(sp);
        }

        public static void PlaySound(SoundPlayer sp)
        {
            if (Properties.Settings.Default.MuteState == false)
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
    }
}
