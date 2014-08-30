using System;
using System.Windows.Media.Imaging;

namespace MySnooper
{
    public class CountryClass : IComparable
    {
        // A static variable to give ID to the countries (they will be loaded in appropriate order)
        private static int counter = 0;

        // Variables
        private string _Name;

        // Properties
        public int ID { get; private set; }
        public string CountryCode { get; private set; }
        public string Name
        {
            get { return _Name; }
            private set
            {
                _Name = value;
                LowerName = value.ToLower();
            }
        }
        public string LowerName { get; private set; }
        public BitmapImage Flag { get; private set; }


        // Constructor
        public CountryClass(string Name, string CountryCode)
        {
            ID = counter++;
            this.CountryCode = CountryCode;
            this.Name = Name;

            try
            {
                string flagfile = System.IO.Path.GetFullPath(@"Flags\Flag" + ID.ToString("D3") + ".PNG");
                Flag = new BitmapImage();
                Flag.DecodePixelWidth = 22;
                Flag.DecodePixelHeight = 18;
                Flag.CacheOption = BitmapCacheOption.OnLoad;
                Flag.BeginInit();
                Flag.UriSource = new Uri(flagfile);
                Flag.EndInit();
            }
            catch (Exception e)
            {
                ErrorLog.log(e);
            }
        }

        // IComparable interface
        public int CompareTo(object obj)
        {
            var obj2 = obj as CountryClass;
            return Name.CompareTo(obj2.Name);
        }

        public override bool Equals(System.Object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to Point return false.
            CountryClass cc = obj as CountryClass;
            if ((System.Object)cc == null)
            {
                return false;
            }

            // Return true if the fields match:
            return ID == cc.ID;
        }

        public bool Equals(CountryClass cc)
        {
            // If parameter is null return false:
            if ((object)cc == null)
            {
                return false;
            }

            // Return true if the fields match:
            return ID == cc.ID;
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

        public static bool operator ==(CountryClass a, CountryClass b)
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

        public static bool operator !=(CountryClass a, CountryClass b)
        {
            return !(a == b);
        }
    }
}
