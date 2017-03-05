namespace GreatSnooper.Model
{
    using System;
    using System.Windows.Media.Imaging;

    using GreatSnooper.Helpers;

    public class Country : IComparable
    {
        private static int counter = 0;

        public Country(string name, string countryCode)
        {
            this.ID = counter++;
            this.CountryCode = countryCode;
            this.Name = name;

            try
            {
                this.Flag = new BitmapImage();
                this.Flag.DecodePixelWidth = 22;
                this.Flag.DecodePixelHeight = 18;
                this.Flag.CacheOption = BitmapCacheOption.OnLoad;
                this.Flag.BeginInit();
                this.Flag.UriSource = new Uri("pack://application:,,,/Resources/flags/flag" + this.ID.ToString("D3") + ".PNG");
                this.Flag.EndInit();
                this.Flag.Freeze();
            }
            catch (Exception ex)
            {
                ErrorLog.Log(ex);
            }
        }

        public string CountryCode
        {
            get;
            private set;
        }

        public BitmapImage Flag
        {
            get;
            private set;
        }

        public int ID
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        public int CompareTo(object obj)
        {
            var o = obj as Country;
            return this.Name.CompareTo(o.Name);
        }
    }
}