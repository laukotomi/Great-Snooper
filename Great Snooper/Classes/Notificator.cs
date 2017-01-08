using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GreatSnooper.Classes
{
    internal delegate void NotificatorIsEnabledChangedDelegate();

    class Notificator
    {
        #region Static
        private static Notificator instance;
        #endregion

        #region Members
        public bool _searchInMessagesEnabled;
        public bool _searchInSenderNamesEnabled;
        public bool _searchInGameNamesEnabled;
        public bool _searchInHosterNamesEnabled;
        public bool _searchInJoinMessagesEnabled;
        public volatile bool _isEnabled;
        #endregion

        #region Properties
        public static Notificator Instance
        {
            get
            {
                if (instance == null)
                    instance = new Notificator();
                return instance;
            }
        }


        public List<NotificatorEntry> InMessages { get; private set; }

        public List<NotificatorEntry> GameNames { get; private set; }

        public List<NotificatorEntry> HosterNames { get; private set; }

        public List<NotificatorEntry> JoinMessages { get; private set; }

        public List<NotificatorEntry> SenderNames { get; private set; }

        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    if (this.IsEnabledChanged != null)
                        this.IsEnabledChanged();
                }
            }
        }
        public bool SearchInMessagesEnabled
        {
            get { return this.IsEnabled && _searchInMessagesEnabled; }
        }
        public bool SearchInSenderNamesEnabled
        {
            get { return this.IsEnabled && _searchInSenderNamesEnabled; }
        }
        public bool SearchInGameNamesEnabled
        {
            get { return this.IsEnabled && _searchInGameNamesEnabled; }
        }
        public bool SearchInHosterNamesEnabled
        {
            get { return this.IsEnabled && _searchInHosterNamesEnabled; }
        }
        public bool SearchInJoinMessagesEnabled
        {
            get { return this.IsEnabled && _searchInJoinMessagesEnabled; }
        }
        #endregion

        #region Events
        public event NotificatorIsEnabledChangedDelegate IsEnabledChanged;
        #endregion

        private Notificator()
        {
            this._isEnabled = Properties.Settings.Default.NotificatorStartWithSnooper;
            this.GameNames = LoadList(Properties.Settings.Default.NotificatorInGameNames, out this._searchInGameNamesEnabled);
            this.HosterNames = LoadList(Properties.Settings.Default.NotificatorInHosterNames, out this._searchInHosterNamesEnabled);
            this.JoinMessages = LoadList(Properties.Settings.Default.NotificatorInJoinMessages, out this._searchInJoinMessagesEnabled);
            this.InMessages = LoadList(Properties.Settings.Default.NotificatorInMessages, out this._searchInMessagesEnabled);
            this.SenderNames = LoadList(Properties.Settings.Default.NotificatorInSenderNames, out this._searchInSenderNamesEnabled);

            Properties.Settings.Default.PropertyChanged += Default_PropertyChanged;
        }

        private void Default_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "NotificatorInGameNames":
                    this.GameNames = LoadList(Properties.Settings.Default.NotificatorInGameNames, out this._searchInGameNamesEnabled);
                    break;

                case "NotificatorInHosterNames":
                    this.HosterNames = LoadList(Properties.Settings.Default.NotificatorInHosterNames, out this._searchInHosterNamesEnabled);
                    break;

                case "NotificatorInJoinMessages":
                    this.JoinMessages = LoadList(Properties.Settings.Default.NotificatorInJoinMessages, out this._searchInJoinMessagesEnabled);
                    break;

                case "NotificatorInMessages":
                    this.InMessages = LoadList(Properties.Settings.Default.NotificatorInMessages, out this._searchInMessagesEnabled);
                    break;

                case "NotificatorInSenderNames":
                    this.SenderNames = LoadList(Properties.Settings.Default.NotificatorInSenderNames, out this._searchInSenderNamesEnabled);
                    break;
            }
        }

        private List<NotificatorEntry> LoadList(string value, out bool enabled)
        {
            List<NotificatorEntry> list = new List<NotificatorEntry>();
            HashSet<string> temp = new HashSet<string>();

            var words = value.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string word in words)
            {
                string entryWord = word.Trim();
                if (!entryWord.StartsWith("#") && !temp.Contains(entryWord))
                {
                    temp.Add(entryWord);
                    list.Add(new NotificatorEntry(entryWord));
                }
            }

            enabled = list.Count != 0;
            return list;
        }

        #region Dispose
        private bool isDisposed;
        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                Properties.Settings.Default.PropertyChanged -= Default_PropertyChanged;
            }
        }
        #endregion
    }
}
