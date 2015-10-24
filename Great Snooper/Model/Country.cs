using GreatSnooper.Helpers;
using System;
using System.Windows.Media.Imaging;

namespace GreatSnooper.Model
{
    public class Country : IComparable
    {
        #region Static
        private static int counter = 0;
        #endregion

        #region Properties
        public int ID { get; private set; }
        public string Name { get; private set; }
        public string CountryCode { get; private set; }
        public BitmapImage Flag { get; private set; }
        #endregion

        public Country(string name, string countryCode)
        {
            ID = counter++;
            this.CountryCode = countryCode;
            this.Name = name;

            try
            {
                Flag = new BitmapImage();
                Flag.DecodePixelWidth = 22;
                Flag.DecodePixelHeight = 18;
                Flag.CacheOption = BitmapCacheOption.OnLoad;
                Flag.BeginInit();
                Flag.UriSource = new Uri("pack://application:,,,/Resources/flags/flag" + ID.ToString("D3") + ".PNG");
                Flag.EndInit();
                Flag.Freeze();
            }
            catch (Exception ex)
            {
                ErrorLog.Log(ex);
            }
        }

        public int CompareTo(object obj)
        {
            var o = obj as Country;
            return Name.CompareTo(o.Name);
        }
    }
}