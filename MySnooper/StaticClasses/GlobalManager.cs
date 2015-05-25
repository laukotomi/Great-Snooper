using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Windows.Markup;

namespace MySnooper
{
    public class GlobalManager
    {
        public const int MaxMessagesInMemory = 1000;
        public const int MaxMessagesDisplayed = 100;
        public const int NumOfOldMessagesToBeLoaded = 50;


        private static ParserContext _XamlContext;

        // This method ensures that the initialization will be made from the appropriate thread
        public static void Initialize()
        {
            DefaultGroup = new UserGroup(UserGroups.SystemGroupID);
            SettingsPath = Directory.GetParent(Directory.GetParent(System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath).FullName).FullName;
            DebugMode = false;
            UITasks = new ConcurrentQueue<UITask>();
            SpamAllowed = false;
            SystemClient = new Client("System", null);
        }

        public static void MainWindowInit()
        {
            BanList = new Dictionary<string, string>();
            BanList.DeSerialize(Properties.Settings.Default.BanList);
            AutoJoinList = new Dictionary<string, string>();
            AutoJoinList.DeSerialize(Properties.Settings.Default.AutoJoinChannels);
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

        public static Client SystemClient { get; private set; }

        public static bool DebugMode { get; set; }

        public static string SettingsPath { get; private set; }

        public static ConcurrentQueue<UITask> UITasks { get; private set; }

        public static UserGroup DefaultGroup { get; private set; }

        public static bool SpamAllowed { get; set; }

        public static Dictionary<string, string> BanList { get; private set; }

        public static Dictionary<string, string> AutoJoinList { get; private set; }
    }
}
