using System.Windows;
using System.Windows.Media;


namespace MySnooper
{
    public class MessageSetting
    {
        public FontFamily Fontfamily { get; set; }
        public SolidColorBrush NickColor { get; set; }
        public SolidColorBrush MessageColor { get; set; }
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
                coll.Freeze();
                return coll;
            }
            private set
            {

            }
        }
        public bool Underline { get; set; }
        public bool Strikethrough { get; set; }
        public bool IsFixedText { get; private set; }
        public bool OneColorOnly { get; private set; }


        public MessageSetting(Color nickColor, Color messageColor, double size, string bold, string italic, string strikethrough, string underline, string fontfamily, MessageTypes type, bool isFixedText)
        {
            this.Fontfamily = new FontFamily(fontfamily);
            this.NickColor = new SolidColorBrush(nickColor);
            this.MessageColor = new SolidColorBrush(messageColor);
            this.NickColor.Freeze();
            this.Size = size;
            this.Bold = bold == "1" ? FontWeights.Bold : FontWeights.Normal;
            this.Italic = italic == "1" ? FontStyles.Italic : FontStyles.Normal;
            this.Strikethrough = strikethrough == "1";
            this.Underline = underline == "1";
            this.Type = type;
            this.IsFixedText = isFixedText;
        }

        public MessageSetting(Color nickColor, double size, string bold, string italic, string strikethrough, string underline, string fontfamily, MessageTypes type, bool isFixedText)
        {
            this.Fontfamily = new FontFamily(fontfamily);
            this.NickColor = new SolidColorBrush(nickColor);
            this.MessageColor = new SolidColorBrush(nickColor);
            this.NickColor.Freeze();
            this.Size = size;
            this.Bold = bold == "1" ? FontWeights.Bold : FontWeights.Normal;
            this.Italic = italic == "1" ? FontStyles.Italic : FontStyles.Normal;
            this.Strikethrough = strikethrough == "1";
            this.Underline = underline == "1";
            this.Type = type;
            this.IsFixedText = isFixedText;
            this.OneColorOnly = true;
        }

        public MessageSetting(Color nickColor, Color messageColor, double size, bool bold, bool italic, bool strikethrough, bool underline, string fontfamily, MessageTypes type)
        {
            this.Fontfamily = new FontFamily(fontfamily);
            this.NickColor = new SolidColorBrush(nickColor);
            this.MessageColor = new SolidColorBrush(messageColor);
            this.Size = size;
            this.Bold = bold ? FontWeights.Bold : FontWeights.Normal;
            this.Italic = italic ? FontStyles.Italic : FontStyles.Normal;
            this.Strikethrough = strikethrough;
            this.Underline = underline;
        }
    }
}
