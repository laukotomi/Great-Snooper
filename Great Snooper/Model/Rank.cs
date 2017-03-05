namespace GreatSnooper.Model
{
    using System;
    using System.Windows.Media.Imaging;

    using GreatSnooper.Helpers;

    public class Rank : IComparable
    {
        private static int counter = 0;

        public Rank(string name)
        {
            this.ID = counter++;
            this.Name = name;

            try
            {
                this.Picture = new BitmapImage();
                this.Picture.DecodePixelWidth = 48;
                this.Picture.DecodePixelHeight = 17;
                this.Picture.CacheOption = BitmapCacheOption.OnLoad;
                this.Picture.BeginInit();
                this.Picture.UriSource = new Uri("pack://application:,,,/Resources/ranks/rank" + this.ID.ToString() + ".png");
                this.Picture.EndInit();
                this.Picture.Freeze();
            }
            catch (Exception e)
            {
                ErrorLog.Log(e);
            }
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

        public BitmapImage Picture
        {
            get;
            private set;
        }

        public int CompareTo(object obj)
        {
            var o = (Rank)obj;
            return this.ID.CompareTo(o.ID);
        }
    }
}