using GreatSnooper.Helpers;
using System;
using System.Windows.Media.Imaging;

namespace GreatSnooper.Model
{
    public class Rank : IComparable
    {
        #region Static
        private static int counter = 0;
        #endregion

        #region Properties
        public int ID { get; private set; }
        public string Name { get; private set; }
        public BitmapImage Picture { get; private set; }
        #endregion

        public Rank(string name)
        {
            ID = counter++;
            this.Name = name;

            try
            {
                Picture = new BitmapImage();
                Picture.DecodePixelWidth = 48;
                Picture.DecodePixelHeight = 17;
                Picture.CacheOption = BitmapCacheOption.OnLoad;
                Picture.BeginInit();
                Picture.UriSource = new Uri("pack://application:,,,/Resources/ranks/rank" + ID.ToString() + ".png");
                Picture.EndInit();
                Picture.Freeze();
            }
            catch (Exception e)
            {
                ErrorLog.Log(e);
            }
        }

        #region IComparable
        public int CompareTo(object obj)
        {
            var o = (Rank)obj;
            return ID.CompareTo(o.ID);
        }
        #endregion
    }
}
