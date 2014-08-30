using System.Windows.Markup;

namespace MySnooper
{
    public class GlobalManager
    {
        private static ParserContext _XamlContext;

        public static Client User { get; set; }

        public static ParserContext XamlContext
        {
            get
            {
                if (_XamlContext == null)
                {
                    _XamlContext = new ParserContext();
                    _XamlContext.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
                    _XamlContext.XmlnsDictionary.Add("x", "http://schemas.microsoft.com/winfx/2006/xaml");
                    _XamlContext.XmlnsDictionary.Add("local", "clr-namespace:MySnooper.CustomUIThings");
                }
                return _XamlContext;
            }
            private set { _XamlContext = value; }
        }

        public static int MaxMessagesInMemory { get; private set; }

        public static int MaxMessagesDisplayed { get; private set; }

        public static int NumOfOldMessagesToBeLoaded { get; private set; }

        static GlobalManager()
        {
            MaxMessagesInMemory = 1000;
            MaxMessagesDisplayed = 100;
            NumOfOldMessagesToBeLoaded = 50;
        }

        public static Client SystemClient = new Client("System", null, "", 0, false);

        public static bool DebugMode = false;
    }
}
