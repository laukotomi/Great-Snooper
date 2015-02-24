using System;
using System.Windows.Media.Imaging;

namespace MySnooper
{
    public class Game : IComparable
    {
        // Variables
        public bool IsAlive = true;

        // Properties
        public uint ID { get; private set; }
        public string Address { get; private set; }
        public string Name { get; private set; }
        public CountryClass Country { get; private set; }
        public string Hoster { get; private set; }
        public BitmapImage Locked { get; private set; }


        // Constructor
        public Game(uint ID, string Name, string Address, CountryClass Country, string Hoster, bool Password)
        {
            this.ID = ID;
            this.Address = Address;

            try
            {
                Locked = new BitmapImage();
                Locked.DecodePixelWidth = 16;
                Locked.DecodePixelHeight = 16;
                Locked.CacheOption = BitmapCacheOption.OnLoad;
                Locked.BeginInit();
                if (Password)
                    Locked.UriSource = new Uri("pack://application:,,,/Resources/locked.png");
                else
                    Locked.UriSource = new Uri("pack://application:,,,/Resources/nolock.png");
                Locked.EndInit();
            }
            catch (Exception e)
            {
                ErrorLog.Log(e);
            }

            this.Name = Name;
            this.Country = Country;
            this.Hoster = Hoster;
        }

        // IComparable interface
        public int CompareTo(object obj)
        {
            var obj2 = (Game)obj;
            return obj2.ID.CompareTo(ID);
        }

        public override bool Equals(System.Object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to Point return false.
            Game g = obj as Game;
            if ((System.Object)g == null)
            {
                return false;
            }

            // Return true if the fields match:
            return ID == g.ID;
        }

        public bool Equals(Game g)
        {
            // If parameter is null return false:
            if ((object)g == null)
            {
                return false;
            }

            // Return true if the fields match:
            return ID == g.ID;
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

        public static bool operator ==(Game a, Game b)
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
            return a.ID == b.ID;
        }

        public static bool operator !=(Game a, Game b)
        {
            return !(a == b);
        }
    }
}
