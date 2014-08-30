using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace MySnooper
{
    public class Client : IComparable, INotifyPropertyChanged
    {
        // Private variables to make properties work well
        private CountryClass _Country;
        private bool _IsBanned;
        private bool _IsBuddy;
        private bool _TusActive;
        private string _TusNick;
        private int _OnlineStatus; // 0 = offline, 1 = online, 2 = not known (client is not in the channel where we are)
        private RankClass _Rank;

        // Variables
        public bool ClientGreatSnooper;

        // Properties
        public string Name { get; private set; }
        public string LowerName { get; private set; }
        public string Clan { get; set; }
        public string TusLowerNick { get; private set; }
        public string TusLink { get; set; }
        public bool TusActiveCheck { get; set; } // This variable is used when we check if an user is (still) on tus, because this doesn't notify the UI when it is changed
        public List<Channel> Channels { get; set; }
        public RankClass Rank
        {
            get
            {
                return _Rank;
            }
            set
            {
                _Rank = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Rank"));
            }
        }
        public string TusNick
        {
            get
            {
                return _TusNick;
            }
            set
            {
                _TusNick = value;
                TusLowerNick = value.ToLower();
            }
        }
        public int OnlineStatus
        {
            get
            {
                return _OnlineStatus;
            }
            set
            {
                _OnlineStatus = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("OnlineStatus"));
            }
        }


        // These properties may change and then they will notify the UI thread about that
        public CountryClass Country
        {
            get
            {
                return _Country;
            }
            set
            {
                _Country = value;
                // Notify the UI thread!
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Country"));
            }
        }
        public bool IsBanned {
            get
            {
                return _IsBanned;
            }
            set
            {
                _IsBanned = value;
                // Notify the UI thread!
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("IsBanned"));
            }
        }
        public bool IsBuddy {
            get
            {
                return _IsBuddy;
            }
            set
            {
                _IsBuddy = value;
                // Notify the UI thread!
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("IsBuddy"));
            }
        }
        public bool TusActive {
            get
            {
                return _TusActive;
            }
            set
            {
                _TusActive = value;
                // Notify the UI thread!
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("TusActive"));
            }
        }



        // Constructor
        public Client(string Name, CountryClass Country, string Clan, int Rank, bool ClientGreatSnooper)
        {
            this.Rank = RanksClass.GetRankByInt(Rank);
            this.Name = Name;
            this.LowerName = Name.ToLower();
            this.Clan = Clan;
            this.Country = Country;
            this.ClientGreatSnooper = ClientGreatSnooper;
            this.TusNick = string.Empty;
            this.Channels = new List<Channel>();
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

        // INotifyPropertyChanged interface
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
