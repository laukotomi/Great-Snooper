using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MySnooper
{
    public class RankClass : IComparable
    {
        // A static variable to give ID to the ranks
        private static int counter = 0;

        // Variables
        private string _Name;

        // Properties
        public int ID { get; private set; }
        public string LowerName { get; private set; }
        public string Name
        {
            get
            {
                return _Name;
            }
            private set
            {
                _Name = value;
                LowerName = value.ToLower();
            }
        }
        public BitmapImage Picture { get; private set; }


        // Constructor
        public RankClass(string Name)
        {
            ID = counter++;
            this.Name = Name;

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

        public int CompareTo(object obj)
        {
            var o = (RankClass)obj;
            return ID.CompareTo(o.ID);
        }
    }
}
