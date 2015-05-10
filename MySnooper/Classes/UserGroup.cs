using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Media;
using System.Windows.Media;

namespace MySnooper
{
    public class UserGroup : INotifyPropertyChanged
    {
        private SolidColorBrush _groupColor;
        private SoundPlayer soundPlayer;

        public int ID { get; private set; }
        public string SettingName { get; private set; }
        public string Name { get; set; }
        public Dictionary<string, string> Users { get; private set; }
        public SolidColorBrush TextColor { get; private set; }
        public bool SoundEnabled { get; private set; }

        public SolidColorBrush GroupColor
        {
            get
            {
                return _groupColor;
            }
            set
            {
                _groupColor = value;
                _groupColor.Freeze();
                SolidColorBrush textColor = new SolidColorBrush(Color.FromRgb(value.Color.R, value.Color.G, value.Color.B));
                textColor.Freeze();
                TextColor = textColor;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("GroupColor"));
            }
        }

        public UserGroup(int id)
        {
            this.ID = id;
            if (id != int.MaxValue)
            {
                this.SettingName = "Group" + id.ToString();
                string value = (string)(Properties.Settings.Default.GetType().GetProperty(this.SettingName).GetValue(Properties.Settings.Default, null));
                string[] values = value.Split(new char[] { '|' });

                this.Name = values[0];
                this.GroupColor = new SolidColorBrush(Color.FromArgb(
                    byte.Parse(values[1].Substring(0, 2), System.Globalization.NumberStyles.HexNumber),
                    byte.Parse(values[1].Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
                    byte.Parse(values[1].Substring(4, 2), System.Globalization.NumberStyles.HexNumber),
                    byte.Parse(values[1].Substring(6, 2), System.Globalization.NumberStyles.HexNumber)
                ));

                string listName = this.SettingName + "List";
                value = (string)(Properties.Settings.Default.GetType().GetProperty(listName).GetValue(Properties.Settings.Default, null));
                this.Users = new Dictionary<string, string>();
                string[] temp = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string user in temp)
                    this.Users.Add(user.ToLower(), user);

                string soundFile = (string)(Properties.Settings.Default.GetType().GetProperty(this.SettingName + "Sound").GetValue(Properties.Settings.Default, null));
                if (File.Exists(soundFile))
                {
                    soundPlayer = new SoundPlayer(new FileInfo(soundFile).FullName);
                    SoundEnabled = (bool)(Properties.Settings.Default.GetType().GetProperty(this.SettingName + "SoundEnabled").GetValue(Properties.Settings.Default, null));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void SaveSettings()
        {
            string setting = Name + "|" + string.Format("{0:X2}{1:X2}{2:X2}{3:X2}", GroupColor.Color.A, GroupColor.Color.R, GroupColor.Color.G, GroupColor.Color.B);
            Properties.Settings.Default.GetType().GetProperty(this.SettingName).SetValue(Properties.Settings.Default, setting, null);
            Properties.Settings.Default.Save();
        }

        public void SaveUsers()
        {
            List<string> temp = new List<string>();
            foreach (var item in this.Users)
                temp.Add(item.Value);
            Properties.Settings.Default.GetType().GetProperty(this.SettingName + "List").SetValue(Properties.Settings.Default, String.Join(",", temp), null);
            Properties.Settings.Default.Save();
        }

        public void PlaySound()
        {
            if (soundPlayer != null)
            {
                try
                {
                    soundPlayer.Play();
                }
                catch (Exception ex)
                {
                    ErrorLog.Log(ex);
                }
            }
        }

        public void ReloadSound()
        {
            if (soundPlayer != null)
                soundPlayer.Dispose();

            string soundFile = (string)(Properties.Settings.Default.GetType().GetProperty(this.SettingName + "Sound").GetValue(Properties.Settings.Default, null));
            if (File.Exists(soundFile))
                soundPlayer = new SoundPlayer(new FileInfo(soundFile).FullName);
            else soundPlayer = null;
        }

        public void ReloadSoundEnabled()
        {
            SoundEnabled = (bool)(Properties.Settings.Default.GetType().GetProperty(this.SettingName + "SoundEnabled").GetValue(Properties.Settings.Default, null));
        }
    }
}
