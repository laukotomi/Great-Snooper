using System.Windows;
using System.Windows.Media;


namespace MySnooper
{
    public class MessageSetting
    {
        public FontFamily fontfamily { get; private set; }
        public SolidColorBrush color { get; private set; }
        public double size { get; private set; }
        public FontWeight bold { get; private set; }
        public FontStyle italic { get; private set; }
        public TextDecorationCollection textdecorations
        {
            get
            {
                TextDecorationCollection coll = new TextDecorationCollection();
                if (underline)
                    coll.Add(TextDecorations.Underline);
                if (strikethrough)
                    coll.Add(TextDecorations.Strikethrough);
                return coll;
            }
            private set
            {

            }
        }
        public bool underline { get; private set; }
        public bool strikethrough { get; private set; }


        public MessageSetting(Color color, double size, string bold, string italic, string strikethrough, string underline, string fontfamily)
        {
            this.fontfamily = new FontFamily(fontfamily);
            this.color = new SolidColorBrush(color);
            this.size = size;
            this.bold = bold == "1" ? FontWeights.Bold : FontWeights.Normal;
            this.italic = italic == "1" ? FontStyles.Italic : FontStyles.Normal;
            this.strikethrough = strikethrough == "1";
            this.underline = underline == "1";
        }

        public MessageSetting(Color color, double size, bool bold, bool italic, bool strikethrough, bool underline, string fontfamily)
        {
            this.fontfamily = new FontFamily(fontfamily);
            this.color = new SolidColorBrush(color);
            this.size = size;
            this.bold = bold ? FontWeights.Bold : FontWeights.Normal;
            this.italic = italic ? FontStyles.Italic : FontStyles.Normal;
            this.strikethrough = strikethrough;
            this.underline = underline;
        }
    }
}
