namespace GreatSnooper.Model
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using GalaSoft.MvvmLight;
    using GreatSnooper.Channel;
    using GreatSnooper.Helpers;
    using GreatSnooper.IRC;
    using GreatSnooper.Services;
    using GreatSnooper.ViewModel;

    [DebuggerDisplay("{Name}")]
    public class User : ObservableObject, IComparable
    {
        public Country _country;
        public Rank _rank;

        private bool? _canConversation;
        private string _clan;
        private UserGroup _group = GlobalManager.DefaultGroup;
        private string _name;
        private Status _onlineStatus = Status.Unknown;
        private TusAccount _tusAccount;
        private bool _isBanned;
        private bool? _usingGreatSnooper;
        private bool? _usingGreatSnooper2;

        public User(IRCCommunicator server, string name, string clan = "")
        {
            this.Server = server;
            this._name = name;
            this._clan = clan;
            this._isBanned = GlobalManager.BanList.Contains(Name);
            this.ChannelCollection = new ChannelCollection();
            this.ChannelCollection.CollectionChanged += CheckReferences;
            this.Messages = new ObservableCollection<Message>();
            this.Messages.CollectionChanged += CheckReferences;
            UserGroup group;
            if (UserGroups.Instance.Users.TryGetValue(name, out group))
            {
                this._group = group;
            }
            else
            {
                this._group = GlobalManager.DefaultGroup;
            }
        }

        public IRCCommunicator Server { get; private set; }

        public enum Status
        {
            Online, Offline, Unknown
        }

        public ObservableCollection<Message> Messages { get; private set; }

        public bool CanConversation
        {
            get
            {
                if (this._canConversation.HasValue)
                {
                    return this._canConversation.Value;
                }

                if (!UsingGreatSnooper)
                {
                    this._canConversation = false;
                }
                else
                {
                    // Great snooper v1.4
                    string gsVersion = ClientName.Substring(15);
                    this._canConversation = Math.Sign(gsVersion.CompareTo("1.4")) != -1;
                }
                return this._canConversation.Value;
            }
        }

        public ChannelCollection ChannelCollection { get; private set; }

        public string Clan
        {
            get
            {
                if (TusAccount != null)
                {
                    return TusAccount.Clan;
                }
                return _clan;
            }
        }

        public string ClientName { get; private set; }

        public Country Country
        {
            get
            {
                if (TusAccount != null)
                {
                    return TusAccount.Country;
                }
                return _country;
            }
        }

        public UserGroup Group
        {
            get
            {
                return _group;
            }
            set
            {
                if (_group != value)
                {
                    if (value != null)
                    {
                        _group = value;
                    }
                    else
                    {
                        _group = GlobalManager.DefaultGroup;
                    }

                    RefrestView();
                    RaisePropertyChanged("Group");
                }
            }
        }

        public bool IsBanned
        {
            get
            {
                return _isBanned;
            }
            set
            {
                if (_isBanned != value)
                {
                    _isBanned = value;
                    RaisePropertyChanged("IsBanned");
                }
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    RaisePropertyChanged("Name");
                }
            }
        }

        public Status OnlineStatus
        {
            get
            {
                return _onlineStatus;
            }
            set
            {
                if (_onlineStatus != value)
                {
                    if (_onlineStatus == Status.Online)
                    {
                        // Reset client info
                        if (TusAccount != null)
                        {
                            TusAccount.User = null;
                            TusAccount = null;
                        }
                        ClientName = null;
                        CanShow = false;
                        _country = null;
                        _rank = null;
                        _clan = string.Empty;
                        _canConversation = null;
                        if (Messages.Count == 0)
                        {
                            Server.Users.Remove(Name);
                            Server = null;
                        }
                    }
                    _onlineStatus = value;
                    RaisePropertyChanged("OnlineStatus");
                }
            }
        }

        public Rank Rank
        {
            get
            {
                if (TusAccount != null)
                {
                    return TusAccount.Rank;
                }
                return _rank;
            }
        }

        private bool _canShow;
        public bool CanShow
        {
            get
            {
                return _canShow;
            }
            set
            {
                if (_canShow != value)
                {
                    _canShow = value;
                    RefrestView();
                }
            }
        }

        public TusAccount TusAccount
        {
            get
            {
                return _tusAccount;
            }
            set
            {
                if (_tusAccount != value)
                {
                    _tusAccount = value;
                    RefrestView();
                }
            }
        }

        public bool UsingGreatSnooper
        {
            get
            {
                if (_usingGreatSnooper.HasValue)
                {
                    return _usingGreatSnooper.Value;
                }
                _usingGreatSnooper = ClientName != null && ClientName.StartsWith("Great Snooper", StringComparison.OrdinalIgnoreCase);
                return _usingGreatSnooper.Value;
            }
        }

        public bool UsingGreatSnooper2
        {
            get
            {
                if (_usingGreatSnooper2.HasValue)
                {
                    return _usingGreatSnooper2.Value;
                }

                if (!UsingGreatSnooper)
                {
                    _usingGreatSnooper2 = false;
                }
                else
                {
                    // Great snooper v1.4
                    string gsVersion = ClientName.Substring(15);
                    _usingGreatSnooper2 = Math.Sign(gsVersion.CompareTo("2.0")) != -1;
                }
                return _usingGreatSnooper2.Value;
            }
        }

        public bool UsingGreatSnooperItalic
        {
            get
            {
                return Properties.Settings.Default.ItalicForGSUsers && this.UsingGreatSnooper;
            }
        }

        public void SetUserInfo(Country country, Rank rank, string clientName)
        {
            _country = country;
            _rank = rank;
            ClientName = clientName;
        }

        private void RefrestView()
        {
            // To keep the list sorted..
            foreach (ChannelViewModel chvm in ChannelCollection.Channels)
            {
                if (chvm.Joined)
                {
                    chvm.Users.Remove(this);
                    chvm.Users.Add(this);
                }
            }
        }

        private void CheckReferences(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove &&
                Messages.Count == 0 && ChannelCollection.AllChannels.Count == 0)
            {
                Messages.CollectionChanged -= CheckReferences;
                ChannelCollection.CollectionChanged -= CheckReferences;
                Server.Users.Remove(Name);
            }
        }

        public static bool operator !=(User user1, User user2)
        {
            return !(user1 == user2);
        }

        public static bool operator ==(User user1, User user2)
        {
            if (object.ReferenceEquals(user1, null))
            {
                return object.ReferenceEquals(user2, null);
            }

            return user1.Equals(user2);
        }

        public int CompareTo(object obj)
        {
            var o = obj as User;
            return GlobalManager.CIStringComparer.Compare(this.Name, o.Name);
        }

        public override bool Equals(object obj)
        {
            var item = obj as User;

            if (item == null)
            {
                return false;
            }

            return this.Name.Equals(item.Name);
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }

        public void RaisePropertyChangedPublic(string propertyName)
        {
            this.RaisePropertyChanged(propertyName);
        }

        // To use this object with string.Join(",", List<User>);
        public override string ToString()
        {
            return this.Name;
        }
    }
}