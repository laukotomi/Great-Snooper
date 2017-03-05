namespace GreatSnooper.Classes
{
    using System;
    using System.Collections.Generic;

    internal delegate void NotificatorIsEnabledChangedDelegate();

    class Notificator
    {
        public volatile bool _isEnabled;
        public bool _searchInGameNamesEnabled;
        public bool _searchInHosterNamesEnabled;
        public bool _searchInJoinMessagesEnabled;
        public bool _searchInMessagesEnabled;
        public bool _searchInSenderNamesEnabled;

        private static Notificator instance;

        private bool isDisposed;

        private Notificator()
        {
            this._isEnabled = Properties.Settings.Default.NotificatorStartWithSnooper;
            this.GameNames = this.LoadList(Properties.Settings.Default.NotificatorInGameNames, out this._searchInGameNamesEnabled);
            this.HosterNames = this.LoadList(Properties.Settings.Default.NotificatorInHosterNames, out this._searchInHosterNamesEnabled);
            this.JoinMessages = this.LoadList(Properties.Settings.Default.NotificatorInJoinMessages, out this._searchInJoinMessagesEnabled);
            this.InMessages = this.LoadList(Properties.Settings.Default.NotificatorInMessages, out this._searchInMessagesEnabled);
            this.SenderNames = this.LoadList(Properties.Settings.Default.NotificatorInSenderNames, out this._searchInSenderNamesEnabled);

            Properties.Settings.Default.PropertyChanged += this.Default_PropertyChanged;
        }

        public event NotificatorIsEnabledChangedDelegate IsEnabledChanged;

        public static Notificator Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Notificator();
                }
                return instance;
            }
        }

        public List<NotificatorEntry> GameNames
        {
            get;
            private set;
        }

        public List<NotificatorEntry> HosterNames
        {
            get;
            private set;
        }

        public List<NotificatorEntry> InMessages
        {
            get;
            private set;
        }

        public bool IsEnabled
        {
            get
            {
                return this._isEnabled;
            }
            set
            {
                if (this._isEnabled != value)
                {
                    this._isEnabled = value;
                    if (this.IsEnabledChanged != null)
                    {
                        this.IsEnabledChanged();
                    }
                }
            }
        }

        public List<NotificatorEntry> JoinMessages
        {
            get;
            private set;
        }

        public bool SearchInGameNamesEnabled
        {
            get
            {
                return this.IsEnabled && this._searchInGameNamesEnabled;
            }
        }

        public bool SearchInHosterNamesEnabled
        {
            get
            {
                return this.IsEnabled && this._searchInHosterNamesEnabled;
            }
        }

        public bool SearchInJoinMessagesEnabled
        {
            get
            {
                return this.IsEnabled && this._searchInJoinMessagesEnabled;
            }
        }

        public bool SearchInMessagesEnabled
        {
            get
            {
                return this.IsEnabled && this._searchInMessagesEnabled;
            }
        }

        public bool SearchInSenderNamesEnabled
        {
            get
            {
                return this.IsEnabled && this._searchInSenderNamesEnabled;
            }
        }

        public List<NotificatorEntry> SenderNames
        {
            get;
            private set;
        }

        public void Dispose()
        {
            if (!this.isDisposed)
            {
                this.isDisposed = true;
                Properties.Settings.Default.PropertyChanged -= this.Default_PropertyChanged;
            }
        }

        private void Default_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
            case "NotificatorInGameNames":
                this.GameNames = this.LoadList(Properties.Settings.Default.NotificatorInGameNames, out this._searchInGameNamesEnabled);
                break;

            case "NotificatorInHosterNames":
                this.HosterNames = this.LoadList(Properties.Settings.Default.NotificatorInHosterNames, out this._searchInHosterNamesEnabled);
                break;

            case "NotificatorInJoinMessages":
                this.JoinMessages = this.LoadList(Properties.Settings.Default.NotificatorInJoinMessages, out this._searchInJoinMessagesEnabled);
                break;

            case "NotificatorInMessages":
                this.InMessages = this.LoadList(Properties.Settings.Default.NotificatorInMessages, out this._searchInMessagesEnabled);
                break;

            case "NotificatorInSenderNames":
                this.SenderNames = this.LoadList(Properties.Settings.Default.NotificatorInSenderNames, out this._searchInSenderNamesEnabled);
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
    }
}