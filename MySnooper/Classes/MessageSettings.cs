using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;


namespace MySnooper
{
    public static class MessageSettings
    {
        public static MessageSetting ChannelMessage { get; private set; }
        public static MessageSetting JoinMessage { get; private set; }
        public static MessageSetting QuitMessage { get; private set; }
        public static MessageSetting PartMessage { get; private set; }
        public static MessageSetting OfflineMessage { get; private set; }
        public static MessageSetting ActionMessage { get; private set; }
        public static MessageSetting UserMessage { get; private set; }
        public static MessageSetting NoticeMessage { get; private set; }
        public static MessageSetting BuddyJoinedMessage { get; private set; }
        public static MessageSetting MessageTimeStyle { get; private set; }
        public static MessageSetting HyperLinkStyle { get; private set; }
        public static MessageSetting LeagueFoundMessage { get; private set; }

        static MessageSettings()
        {
            ChannelMessage = SettingToObj(Properties.Settings.Default.ChannelMessage, MessageTypes.Channel);
            JoinMessage = SettingToObj(Properties.Settings.Default.JoinMessage, MessageTypes.Join);
            QuitMessage = SettingToObj(Properties.Settings.Default.QuitMessage, MessageTypes.Quit);
            PartMessage = SettingToObj(Properties.Settings.Default.PartMessage, MessageTypes.Part);
            OfflineMessage = SettingToObj(Properties.Settings.Default.OfflineMessage, MessageTypes.Offline);
            ActionMessage = SettingToObj(Properties.Settings.Default.ActionMessage, MessageTypes.Action);
            UserMessage = SettingToObj(Properties.Settings.Default.UserMessage, MessageTypes.User);
            NoticeMessage = SettingToObj(Properties.Settings.Default.NoticeMessage, MessageTypes.Notice);
            BuddyJoinedMessage = SettingToObj(Properties.Settings.Default.BuddyJoinedMessage, MessageTypes.BuddyJoined);
            MessageTimeStyle = SettingToObj(Properties.Settings.Default.MessageTimeStyle, MessageTypes.Time);
            HyperLinkStyle = SettingToObj(Properties.Settings.Default.HyperLinkStyle, MessageTypes.Hyperlink);
            LeagueFoundMessage = SettingToObj(Properties.Settings.Default.LeagueFoundMessage, MessageTypes.League);
        }
 
        public static MessageSetting SettingToObj(string setting, MessageTypes type)
        {
            var things = setting.Split('|');

            var color = Color.FromRgb(
                byte.Parse(things[0].Substring(0, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(things[0].Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(things[0].Substring(4, 2), System.Globalization.NumberStyles.HexNumber)
            );

            return new MessageSetting(color, double.Parse(things[1]), things[2], things[3], things[4], things[5], things[6], type);
        }


        public static string ObjToSetting(MessageSetting obj)
        {
            var sb = new System.Text.StringBuilder();
            // Color
            sb.Append(string.Format("{0:X2}{1:X2}{2:X2}", obj.Color.Color.R, obj.Color.Color.G, obj.Color.Color.B));
            sb.Append('|');
            // Size
            sb.Append(obj.Size);
            sb.Append('|');
            // Bold
            sb.Append(obj.Bold == FontWeights.Bold ? 1 : 0);
            sb.Append('|');
            // Italic
            sb.Append(obj.Italic == FontStyles.Italic ? 1 : 0);
            sb.Append('|');
            // Strikethrough
            sb.Append(obj.Strikethrough ? 1 : 0);
            sb.Append('|');
            // Underline
            sb.Append(obj.Underline ? 1 : 0);
            sb.Append('|');
            // Font family
            sb.Append(obj.Fontfamily.ToString());

            return sb.ToString();
        }

        public static void LoadSettingsFor(System.Windows.Documents.TextElement element, MessageSetting setting)
        {
            element.FontFamily = setting.Fontfamily;
            element.FontWeight = setting.Bold;
            element.Foreground = setting.Color;
            element.FontStyle = setting.Italic;
            element.FontSize = setting.Size;
            element.FontWeight = setting.Bold;
            if (element is Inline)
            {
                Inline inline = (Inline)element;
                inline.TextDecorations = setting.Textdecorations;
            }
            else if (element is Paragraph)
            {
                Paragraph p = (Paragraph)element;
                p.TextDecorations = setting.Textdecorations;
            }
        }
    }
}
