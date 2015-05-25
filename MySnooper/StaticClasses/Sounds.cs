using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;

namespace MySnooper
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

        public static void PlaySound(string index)
        {
            SoundPlayer sp;
            if (Properties.Settings.Default.MuteState == false && soundPlayers.TryGetValue(index, out sp))
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

        public static void ReloadSound(string settingName)
        {
            string value = (string)(Properties.Settings.Default.GetType().GetProperty(settingName).GetValue(Properties.Settings.Default, null));
            if (soundPlayers.ContainsKey(settingName))
                soundPlayers[settingName] = new SoundPlayer(new FileInfo(value).FullName);
            else
                soundPlayers.Add(settingName, new SoundPlayer(new FileInfo(value).FullName));
        }
    }
}
