namespace GreatSnooper.Model
{
    using System;
    using System.Collections.Generic;
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
        private string _clientName;
        private UserGroup _group = GlobalManager.DefaultGroup;
        private bool _isBanned = false;
        private string _name;
        private Status _onlineStatus = Status.Unknown;
        private TusAccount _tusAccount;
        private bool? _usingGreatSnooper;
        private bool? _usingGreatSnooper2;

        public User(IRCCommunicator server, string name, string clan = "")
        {
            this.Server = server;
            this._name = name;
            this._clan = clan;
            this.ChannelCollection = new ChannelCollection();
            this.AddToChannel = new List<ChannelViewModel>();
            this.Messages = new List<Message>();
            UserGroup group;
            if (UserGroups.Users.TryGetValue(name, out group))
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

        public List<Message> Messages { get; private set; }

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
        public List<ChannelViewModel> AddToChannel { get; private set; }


        public string Clan
        {
            get
            {
                if (TusAccount != null)
                {
                    return TusAccount.Clan;
                }
                else
                {
                    return _clan;
                }
            }
            set
            {
                if (_clan != value)
                {
                    _clan = value;
                    RaisePropertyChanged("Clan");
                }
            }
        }

        public string ClientName
        {
            get
            {
                return this._clientName;
            }
            set
            {
                if (this._clientName != value)
                {
                    this._clientName = value;
                    this._usingGreatSnooper = null;
                    this._canConversation = null;
                }
            }
        }

        public Country Country
        {
            get
            {
                if (TusAccount != null)
                {
                    return TusAccount.Country;
                }
                else
                {
                    return _country;
                }
            }
            set
            {
                if (_country != value)
                {
                    _country = value;
                    RaisePropertyChanged("Country");
                }
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

                    // Refresh sorting
                    foreach (ChannelViewModel chvm in ChannelCollection.Channels)
                    {
                        if (chvm.Joined)
                        {
                            chvm.Users.Remove(this);
                            chvm.Users.Add(this);
                        }
                    }

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
                    _onlineStatus = value;
                    if (value != Status.Online)
                    {
                        // Reset client info
                        TusAccount = null;
                        ClientName = null;
                    }
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
                else
                {
                    return _rank;
                }
            }
            set
            {
                if (_rank != value)
                {
                    _rank = value;
                    RaisePropertyChanged("Rank");
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
                    RaisePropertyChanged("Clan");
                    RaisePropertyChanged("Rank");
                    RaisePropertyChanged("Country");
                    RaisePropertyChanged("TusAccount");
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