using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace MySnooper
{
    public class Client : IComparable, INotifyPropertyChanged
    {
        public enum Status { Online, Offline, Unknown }

        // Private variables to make properties work well
        private RankClass _rank;
        private CountryClass _country = null;
        private bool _isBanned = false;
        private bool _tusActive = false;
        private string _tusNick = string.Empty;
        private Status _onlineStatus = Status.Unknown;
        private string _name;
        private string _clientApp;
        private UserGroup _group = GlobalManager.DefaultGroup;
        private bool _greatSnooper = false;

        // Properties
        public string LowerName { get; private set; }
        public string Clan { get; set; }
        public string TusLowerNick { get; private set; }
        public string TusLink { get; set; }
        public List<Channel> Channels { get; private set; }
        public List<Channel> PMChannels { get; private set; }
        public List<Channel> AddToChannel { get; private set; }
        public string ClientAppL { get; private set; }

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                LowerName = value.ToLower();
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Name"));
            }
        }

        public string ClientApp
        {
            get { return _clientApp; }
            set
            {
                _clientApp = value;
                ClientAppL = value.ToLower();
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("ClientApp"));
            }
        }

        public RankClass Rank
        {
            get
            {
                return _rank;
            }
            set
            {
                _rank = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Rank"));
            }
        }
        public string TusNick
        {
            get
            {
                return _tusNick;
            }
            set
            {
                _tusNick = value;
                TusLowerNick = value.ToLower();
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
                _onlineStatus = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("OnlineStatus"));
            }
        }


        // These properties may change and then they will notify the UI thread about that
        public CountryClass Country
        {
            get
            {
                return _country;
            }
            set
            {
                _country = value;
                // Notify the UI thread!
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Country"));
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
                _isBanned = value;
                // Notify the UI thread!
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("IsBanned"));
            }
        }

        public bool TusActive
        {
            get
            {
                return _tusActive;
            }
            set
            {
                _tusActive = value;
                // Notify the UI thread!
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("TusActive"));
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
                if (value != null)
                    _group = value;
                else
                    _group = GlobalManager.DefaultGroup;

                // Refresh sorting
                var temp = new List<Channel>(Channels);

                foreach (Channel ch in temp)
                {
                    ch.Clients.Remove(this);
                    ch.Clients.Add(this);
                }

                // Notify the UI thread!
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Group"));
            }
        }

        public bool GreatSnooper {
            get
            {
                return _greatSnooper;
            }
            set
            {
                _greatSnooper = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("GreatSnooper"));
            }
        }


        // Constructor
        public Client(string name, IRCCommunicator server, string clan = "")
        {
            this.Name = name;
            this.LowerName = name.ToLower();
            this.Clan = clan;
            this.Rank = RanksClass.GetRankByInt(0);
            this.Channels = new List<Channel>();
            this.PMChannels = new List<Channel>();
            this.AddToChannel = new List<Channel>();
            UserGroup group = null;
            if (UserGroups.Users.TryGetValue(this.LowerName, out group))
                Group = group;
            if (server != null)
            {
                this.IsBanned = GlobalManager.BanList.ContainsKey(this.LowerName);
                server.Clients.Add(this.LowerName, this);
            }
        }

        // IComparable interface
        public int CompareTo(object obj)
        {
            var obj2 = obj as Client;
            return LowerName.CompareTo(obj2.LowerName);
        }

        public override bool Equals(System.Object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to Point return false.
            Client cl = obj as Client;
            if ((System.Object)cl == null)
            {
                return false;
            }

            // Return true if the fields match:
            return LowerName == cl.LowerName;
        }

        public bool Equals(Client cl)
        {
            // If parameter is null return false:
            if ((object)cl == null)
            {
                return false;
            }

            // Return true if the fields match:
            return LowerName == cl.LowerName;
        }

        public override int GetHashCode()
        {
            return LowerName.GetHashCode();
        }

        public static bool operator ==(Client a, Client b)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            // Return true if the fields match:
            return a.LowerName == b.LowerName;
        }

        public static bool operator !=(Client a, Client b)
        {
            return !(a == b);
        }

        public bool CanConversation()
        {
            if (!GreatSnooper)
                return false;

            // Great snooper v1.4
            string gsVersion = ClientApp.Substring(15);
            if (Math.Sign(gsVersion.CompareTo("1.4")) != -1)
                return true;

            return false;
        }

        // INotifyPropertyChanged interface
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
