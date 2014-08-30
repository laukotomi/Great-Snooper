using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;


namespace MySnooper
{
    public static class MessageSettings
    {
        public static Dictionary<MessageTypes, MessageSetting> Settings = new Dictionary<MessageTypes, MessageSetting>()
        {
            { MessageTypes.Channel, SettingToObj(Properties.Settings.Default.ChannelMessage) },
            { MessageTypes.Join, SettingToObj(Properties.Settings.Default.JoinMessage) },
            { MessageTypes.Quit, SettingToObj(Properties.Settings.Default.QuitMessage) },
            { MessageTypes.Part, SettingToObj(Properties.Settings.Default.PartMessage) },
            { MessageTypes.Offline, SettingToObj(Properties.Settings.Default.OfflineMessage) },
            { MessageTypes.Action, SettingToObj(Properties.Settings.Default.ActionMessage) },
            { MessageTypes.User, SettingToObj(Properties.Settings.Default.UserMessage) },
            { MessageTypes.Notice, SettingToObj(Properties.Settings.Default.NoticeMessage) },
            { MessageTypes.BuddyJoined, SettingToObj(Properties.Settings.Default.BuddyJoinedMessage) }
        };

        public static MessageSetting MessageTime;
        public static MessageSetting Hyperlink;
        public static MessageSetting LeagueFound;

        static MessageSettings()
        {
            MessageTime = SettingToObj(Properties.Settings.Default.MessageTimeStyle);
            Hyperlink = SettingToObj(Properties.Settings.Default.HyperLinkStyle);
            LeagueFound = SettingToObj(Properties.Settings.Default.LeagueFoundMessage);
        }
 
        public static MessageSetting SettingToObj(string setting)
        {
            var things = setting.Split('|');

            var color = Color.FromRgb(
                byte.Parse(things[0].Substring(0, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(things[0].Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(things[0].Substring(4, 2), System.Globalization.NumberStyles.HexNumber)
            );

            return new MessageSetting(color, double.Parse(things[1]), things[2], things[3], things[4], things[5], things[6]);
        }


        public static string ObjToSetting(MessageSetting obj)
        {
            var sb = new System.Text.StringBuilder();
            // Color
            sb.Append(string.Format("{0:X2}{1:X2}{2:X2}", obj.color.Color.R, obj.color.Color.G, obj.color.Color.B));
            sb.Append('|');
            // Size
            sb.Append(obj.size);
            sb.Append('|');
            // Bold
            sb.Append(obj.bold == FontWeights.Bold ? 1 : 0);
            sb.Append('|');
            // Italic
            sb.Append(obj.italic == FontStyles.Italic ? 1 : 0);
            sb.Append('|');
            // Strikethrough
            sb.Append(obj.strikethrough ? 1 : 0);
            sb.Append('|');
            // Underline
            sb.Append(obj.underline ? 1 : 0);
            sb.Append('|');
            // Font family
            sb.Append(obj.fontfamily.ToString());

            return sb.ToString();
        }

        public static void LoadSettingsFor(System.Windows.Documents.TextElement element, MessageSetting setting)
        {
            element.FontFamily = setting.fontfamily;
            element.FontWeight = setting.bold;
            element.Foreground = setting.color;
            element.FontStyle = setting.italic;
            element.FontSize = setting.size;
            element.FontWeight = setting.bold;
        }
    }
}
