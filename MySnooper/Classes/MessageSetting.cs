using System.Windows;
using System.Windows.Media;


namespace MySnooper
{
    public class MessageSetting
    {
        public FontFamily Fontfamily { get; set; }
        public SolidColorBrush Color { get; set; }
        public double Size { get; set; }
        public FontWeight Bold { get; set; }
        public FontStyle Italic { get; set; }
        public MessageTypes Type { get; set; }
        public TextDecorationCollection Textdecorations
        {
            get
            {
                TextDecorationCollection coll = new TextDecorationCollection();
                if (Underline)
                    coll.Add(TextDecorations.Underline);
                if (Strikethrough)
                    coll.Add(TextDecorations.Strikethrough);
                return coll;
            }
            private set
            {

            }
        }
        public bool Underline { get; set; }
        public bool Strikethrough { get; set; }


        public MessageSetting(Color color, double size, string bold, string italic, string strikethrough, string underline, string fontfamily, MessageTypes type)
        {
            this.Fontfamily = new FontFamily(fontfamily);
            this.Color = new SolidColorBrush(color);
            this.Size = size;
            this.Bold = bold == "1" ? FontWeights.Bold : FontWeights.Normal;
            this.Italic = italic == "1" ? FontStyles.Italic : FontStyles.Normal;
            this.Strikethrough = strikethrough == "1";
            this.Underline = underline == "1";
            this.Type = type;
        }

        public MessageSetting(Color color, double size, bool bold, bool italic, bool strikethrough, bool underline, string fontfamily, MessageTypes type)
        {
            this.Fontfamily = new FontFamily(fontfamily);
            this.Color = new SolidColorBrush(color);
            this.Size = size;
            this.Bold = bold ? FontWeights.Bold : FontWeights.Normal;
            this.Italic = italic ? FontStyles.Italic : FontStyles.Normal;
            this.Strikethrough = strikethrough;
            this.Underline = underline;
        }
    }
}
