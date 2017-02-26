namespace GreatSnooper.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class News
    {
        public News(int id, bool show, string background, string foreground, double fontsize, string bbcode)
        {
            this.ID = id;
            this.Show = show;
            this.Background = background;
            this.Foreground = foreground;
            this.FontSize = fontsize;
            this.BBCode = bbcode;
        }

        public string Background
        {
            get;
            private set;
        }

        public string BBCode
        {
            get;
            private set;
        }

        public double FontSize
        {
            get;
            private set;
        }

        public string Foreground
        {
            get;
            private set;
        }

        public int ID
        {
            get;
            private set;
        }

        public bool Show
        {
            get;
            private set;
        }
    }
}