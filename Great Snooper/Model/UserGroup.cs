using GalaSoft.MvvmLight;
using GreatSnooper.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Windows.Media;

namespace GreatSnooper.Model
{
    public class UserGroup : ObservableObject, IDisposable
    {
        #region Members
        private SolidColorBrush _groupColor;
        private SoundPlayer _soundPlayer;
        private bool _soundEnabled;
        private string _name;
        #endregion

        #region Properties
        public int ID { get; private set; }
        public string Name {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    RaisePropertyChanged("Name");
                }
            }
        }
        public string SettingName { get; private set; }
        public HashSet<string> Users { get; private set; }
        public SolidColorBrush TextColor { get; private set; }
        public bool SoundEnabled
        {
            get { return _soundEnabled; }
            private set
            {
                if (value)
                    _soundEnabled = SettingsHelper.Load<bool>(this.SettingName + "SoundEnabled");
                else
                    _soundEnabled = value;
            }
        }
        public SoundPlayer Sound
        {
            get { return _soundPlayer; }
            set
            {
                if (_soundPlayer != null)
                    _soundPlayer.Dispose();

                string soundFile = SettingsHelper.Load<string>(this.SettingName + "Sound");
                if (File.Exists(soundFile))
                    _soundPlayer = new SoundPlayer(new FileInfo(soundFile).FullName);
                else
                    _soundPlayer = null;

                SoundEnabled = _soundPlayer != null;
            }
        }

        public SolidColorBrush GroupColor
        {
            get { return _groupColor; }
            set
            {
                if (_groupColor != value)
                {
                    SetColors(value);
                    RaisePropertyChanged("GroupColor");
                }
            }
        }
        #endregion

        public UserGroup(int id)
        {
            this.ID = id;

            if (id != UserGroups.SystemGroupID)
            {
                this.SettingName = "Group" + id.ToString();
                this.ReloadData();

                string userList = SettingsHelper.Load<string>(this.SettingName + "List");
                this.Users = new HashSet<string>(
                    userList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                    StringComparer.OrdinalIgnoreCase);

                Sound = null; // Load sound settings
            }
        }

        private void SetColors(SolidColorBrush value)
        {
            _groupColor = value;
            _groupColor.Freeze();

            var textColor = new SolidColorBrush(Color.FromRgb(value.Color.R, value.Color.G, value.Color.B));
            textColor.Freeze();
            TextColor = textColor;
        }

        #region IDisposable
        bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            disposed = true;

            if (disposing)
            {
                if (_soundPlayer != null)
                {
                    _soundPlayer.Dispose();
                    _soundPlayer = null;
                }
            }
        }

        ~UserGroup()
        {
            Dispose(false);
        }

        #endregion

        internal void SaveUsers()
        {
            SettingsHelper.Save(this.SettingName + "List", this.Users);
        }

        internal void ReloadData()
        {
            string value = SettingsHelper.Load<string>(this.SettingName);
            string[] values = value.Split(new char[] { '|' });

            string defaultValue = SettingsHelper.GetDefaultValue<string>(this.SettingName);
            string[] defaultValues = defaultValue.Split(new char[] { '|' });

            // If the name of the group is default then we can show localized group name
            var asd = System.Threading.Thread.CurrentThread.CurrentCulture;
            var das = System.Threading.Thread.CurrentThread.CurrentUICulture;
            if (values[0] == defaultValues[0])
            {
                switch (this.ID)
                {
                    case 0:
                        this._name = Localizations.GSLocalization.Instance.BuddiesGroupText;
                        break;
                    case 1:
                        this._name = Localizations.GSLocalization.Instance.ClanMatesGroupText;
                        break;
                    case 2:
                        this._name = Localizations.GSLocalization.Instance.LeaguePlayersGroupText;
                        break;
                    case 3:
                        this._name = Localizations.GSLocalization.Instance.Other1GroupText;
                        break;
                    case 4:
                        this._name = Localizations.GSLocalization.Instance.Other2GroupText;
                        break;
                    case 5:
                        this._name = Localizations.GSLocalization.Instance.Other3GroupText;
                        break;
                    case 6:
                        this._name = Localizations.GSLocalization.Instance.Other4GroupText;
                        break;
                }
            }
            else
                this._name = values[0];

            SetColors(new SolidColorBrush(Color.FromArgb(
                byte.Parse(values[1].Substring(0, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(values[1].Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(values[1].Substring(4, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(values[1].Substring(6, 2), System.Globalization.NumberStyles.HexNumber)
            )));
        }
    }
}
