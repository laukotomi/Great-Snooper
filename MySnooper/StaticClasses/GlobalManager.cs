using System.Collections.Concurrent;
using System.IO;
using System.Windows.Markup;

namespace MySnooper
{
    public class GlobalManager
    {
        private static ParserContext _XamlContext;

        // This method ensures that the initialization will be made from the appropriate thread
        public static void Initialize()
        {
            MaxMessagesInMemory = 1000;
            MaxMessagesDisplayed = 100;
            NumOfOldMessagesToBeLoaded = 50;
            SettingsPath = Directory.GetParent(Directory.GetParent(System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath).FullName).FullName;
            DebugMode = false;
            SystemClient = new Client("System");
            UITasks = new ConcurrentQueue<UITask>();
            DefaultGroup = new UserGroup(int.MaxValue);
        }

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

        public static Client SystemClient { get; private set; }

        public static bool DebugMode { get; set; }

        public static string SettingsPath { get; private set; }

        public static ConcurrentQueue<UITask> UITasks { get; private set; }

        public static UserGroup DefaultGroup { get; private set; }
    }
}
