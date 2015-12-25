using GalaSoft.MvvmLight;
using System.Windows;
using System.Windows.Media;

namespace GreatSnooper.Model
{
    public class MessageSetting : ObservableObject
    {
        #region Members
        private FontFamily _fontFamily;
        private SolidColorBrush _nickColor;
        private SolidColorBrush _messageColor;
        private double _size;
        private FontWeight _bold;
        private FontStyle _italic;
        private TextDecorationCollection _textDecorations;
        private bool? _underline;
        private bool? _strikethrough;
        #endregion

        #region Properties
        public FontFamily FontFamily
        {
            get { return _fontFamily; }
            set
            {
                if (_fontFamily != value)
                {
                    _fontFamily = value;
                    RaisePropertyChanged("FontFamily");
                }
            }
        }
        public SolidColorBrush NickColor
        {
            get { return _nickColor; }
            set
            {
                if (_nickColor != value)
                {
                    _nickColor = value;
                    _nickColor.Freeze();
                    RaisePropertyChanged("NickColor");
                }
            }
        }
        public SolidColorBrush MessageColor
        {
            get { return _messageColor; }
            set
            {
                if (_messageColor != value)
                {
                    _messageColor = value;
                    _messageColor.Freeze();
                    RaisePropertyChanged("MessageColor");
                }
            }
        }
        public double Size
        {
            get { return _size; }
            set
            {
                if (_size != value)
                {
                    _size = value;
                    RaisePropertyChanged("Size");
                }
            }
        }
        public FontWeight Bold
        {
            get { return _bold; }
            set
            {
                if (_bold != value)
                {
                    _bold = value;
                    RaisePropertyChanged("Bold");
                }
            }
        }
        public FontStyle Italic
        {
            get { return _italic; }
            set
            {
                if (_italic != value)
                {
                    _italic = value;
                    RaisePropertyChanged("Italic");
                }
            }
        }
        public Message.MessageTypes Type { get; private set; }
        public TextDecorationCollection Textdecorations
        {
            get
            {
                if (_textDecorations == null)
                    GenerateTextDecorations();
                return _textDecorations;
            }
            private set
            {
                if (_textDecorations != value)
                {
                    _textDecorations = value;
                    _textDecorations.Freeze();
                }
            }
        }
        public bool? Underline
        {
            get { return _underline; }
            set
            {
                if (_underline != value)
                {
                    _underline = value;
                    GenerateTextDecorations();
                    RaisePropertyChanged("Textdecorations");
                }
            }
        }
        public bool? Strikethrough
        {
            get { return _strikethrough; }
            set
            {
                if (_strikethrough != value)
                {
                    _strikethrough = value;
                    GenerateTextDecorations();
                    RaisePropertyChanged("Textdecorations");
                }
            }
        }
        public bool OneColorOnly { get; private set; }
        #endregion

        public MessageSetting(Color nickColor, Color messageColor, double size, string bold, string italic, string strikethrough, string underline, string fontfamily, Message.MessageTypes type)
        {
            this._fontFamily = new FontFamily(fontfamily);
            this._nickColor = new SolidColorBrush(nickColor);
            this._nickColor.Freeze();
            this._messageColor = new SolidColorBrush(messageColor);
            this._messageColor.Freeze();
            this._size = size;
            this._bold = bold == "1" ? FontWeights.Bold : FontWeights.Normal;
            this._italic = italic == "1" ? FontStyles.Italic : FontStyles.Normal;
            this._strikethrough = strikethrough == "1";
            this._underline = underline == "1";
            this.Type = type;
        }

        public MessageSetting(Color nickColor, double size, string bold, string italic, string strikethrough, string underline, string fontfamily, Message.MessageTypes type)
        {
            this._fontFamily = new FontFamily(fontfamily);
            this._nickColor = new SolidColorBrush(nickColor);
            this._nickColor.Freeze();
            this._messageColor = new SolidColorBrush(nickColor);
            this._messageColor.Freeze();
            this._size = size;
            this._bold = bold == "1" ? FontWeights.Bold : FontWeights.Normal;
            this._italic = italic == "1" ? FontStyles.Italic : FontStyles.Normal;
            this._strikethrough = strikethrough == "1";
            this._underline = underline == "1";
            this.Type = type;
            this.OneColorOnly = true;
        }

        public MessageSetting(MessageSetting messageSetting)
        {
            this._fontFamily = messageSetting._fontFamily;
            this._nickColor = messageSetting._nickColor;
            this._messageColor = messageSetting._messageColor;
            this._size = messageSetting._size;
            this._bold = messageSetting._bold;
            this._italic = messageSetting._italic;
            this._strikethrough = messageSetting._strikethrough;
            this._underline = messageSetting._underline;
            this.Type = messageSetting.Type;
            this.OneColorOnly = messageSetting.OneColorOnly;
        }


        private void GenerateTextDecorations()
        {
            var decorations = new TextDecorationCollection();
            if (Underline.HasValue && Underline.Value)
                decorations.Add(TextDecorations.Underline);
            if (Strikethrough.HasValue && Strikethrough.Value)
                decorations.Add(TextDecorations.Strikethrough);
            this.Textdecorations = decorations;
        }
    }
}
