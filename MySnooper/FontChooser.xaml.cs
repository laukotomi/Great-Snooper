using MahApps.Metro.Controls;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Xceed.Wpf.Toolkit;


namespace MySnooper
{
    public delegate void SaveSettingsDelegate();

    public partial class FontChooser : MetroWindow
    {
        private string TheName;
        private MessageSetting ActSetting;
        public event SaveSettingsDelegate SaveSettings;
        private SortedDictionary<string, FontFamily> FontFamilies = new SortedDictionary<string,FontFamily>();


        public FontChooser() { } // Never used, but visual stdio throws an error if not exists
        public FontChooser(string TheName, string title)
        {
            InitializeComponent();

            Title = title;

            IEnumerator<FontFamily> iterator = Fonts.SystemFontFamilies.GetEnumerator();
            while (iterator.MoveNext())
                FontFamilies.Add(iterator.Current.ToString(), iterator.Current);

            // Bind ChannelList to the UI
            Binding b = new Binding();
            b.Source = FontFamilies;
            b.Mode = BindingMode.OneWay;
            //b.IsAsync = true;
            TheFontfamily.SetBinding(ListView.ItemsSourceProperty, b);

            this.TheName = TheName;

            for (double i = 1; i <= 20; i++)
                TheSize.Items.Add(i);

            switch (TheName)
            {
                case "UserMessage":
                    ActSetting = MessageSettings.SettingToObj(Properties.Settings.Default.UserMessage);
                    break;
                case "ChannelMessage":
                    ActSetting = MessageSettings.SettingToObj(Properties.Settings.Default.ChannelMessage);
                    break;
                case "JoinMessage":
                    ActSetting = MessageSettings.SettingToObj(Properties.Settings.Default.JoinMessage);
                    break;
                case "PartMessage":
                    ActSetting = MessageSettings.SettingToObj(Properties.Settings.Default.PartMessage);
                    break;
                case "QuitMessage":
                    ActSetting = MessageSettings.SettingToObj(Properties.Settings.Default.QuitMessage);
                    break;
                case "ActionMessage":
                    ActSetting = MessageSettings.SettingToObj(Properties.Settings.Default.ActionMessage);
                    break;
                case "NoticeMessage":
                    ActSetting = MessageSettings.SettingToObj(Properties.Settings.Default.NoticeMessage);
                    break;
                case "OfflineMessage":
                    ActSetting = MessageSettings.SettingToObj(Properties.Settings.Default.OfflineMessage);
                    break;
                case "BuddyJoinMessage":
                    ActSetting = MessageSettings.SettingToObj(Properties.Settings.Default.BuddyJoinedMessage);
                    break;
                case "MessageTimeChange":
                    ActSetting = MessageSettings.SettingToObj(Properties.Settings.Default.MessageTimeStyle);
                    break;
                case "HyperlinkStyleChange":
                    ActSetting = MessageSettings.SettingToObj(Properties.Settings.Default.HyperLinkStyle);
                    break;
                case "LeagueFound":
                    ActSetting = MessageSettings.SettingToObj(Properties.Settings.Default.LeagueFoundMessage);
                    break;

            }

            TheFontfamily.SelectedItem = new KeyValuePair<string, FontFamily>(ActSetting.fontfamily.ToString(), ActSetting.fontfamily);
            TheFontfamily.ScrollIntoView(TheFontfamily.SelectedItem);
            TheSize.SelectedItem = ActSetting.size;
            TheColor.SelectedColor = ActSetting.color.Color;
            TheBold.IsChecked = ActSetting.bold == FontWeights.Bold;
            TheItalic.IsChecked = ActSetting.italic == FontStyles.Italic;
            TheStrikethrough.IsChecked = ActSetting.strikethrough;
            TheUnderline.IsChecked = ActSetting.underline;
        }

        private void FontFamilyChanged(object sender, SelectionChangedEventArgs e)
        {
            var obj = sender as ListView;
            var data = (KeyValuePair<string, FontFamily>)obj.SelectedItem;
            Example.FontFamily = data.Value;
        }

        private void FontSizeChanged(object sender, SelectionChangedEventArgs e)
        {
            var obj = sender as ComboBox;
            Example.FontSize = double.Parse(obj.SelectedItem.ToString());
        }

        private void ColorChanged(object sender, RoutedPropertyChangedEventArgs<Color> e)
        {
            var obj = sender as ColorPicker;
            Example.Foreground = new SolidColorBrush(obj.SelectedColor);
        }

        private void BoldChanged(object sender, RoutedEventArgs e)
        {
            var obj = sender as CheckBox;
            Example.FontWeight = obj.IsChecked.Value ? FontWeights.Bold : FontWeights.Normal;
        }

        private void ItalicChanged(object sender, RoutedEventArgs e)
        {
            var obj = sender as CheckBox;
            Example.FontStyle = obj.IsChecked.Value ? FontStyles.Italic : FontStyles.Normal;
        }

        private void StrikethroughChanged(object sender, RoutedEventArgs e)
        {
            var obj = sender as CheckBox;
            Example.TextDecorations = GetTextDecorations();
        }

        private void UnderlineChanged(object sender, RoutedEventArgs e)
        {
            var obj = sender as CheckBox;
            Example.TextDecorations = GetTextDecorations();
        }

        private TextDecorationCollection GetTextDecorations()
        {
            TextDecorationCollection coll = new TextDecorationCollection();
            if (TheUnderline.IsChecked.Value)
                coll.Add(TextDecorations.Underline);
            if (TheStrikethrough.IsChecked.Value)
                coll.Add(TextDecorations.Strikethrough);
            return coll;
        }

        private void SaveClick(object sender, RoutedEventArgs e)
        {
            var setting = new MessageSetting(TheColor.SelectedColor, Example.FontSize, TheBold.IsChecked.Value, TheItalic.IsChecked.Value, TheStrikethrough.IsChecked.Value, TheUnderline.IsChecked.Value, Example.FontFamily.ToString());

            switch (TheName)
            {
                case "UserMessage":
                    Properties.Settings.Default.UserMessage = MessageSettings.ObjToSetting(setting);
                    MessageSettings.Settings[MessageTypes.User] = setting;
                    break;
                case "ChannelMessage":
                    Properties.Settings.Default.ChannelMessage = MessageSettings.ObjToSetting(setting);
                    MessageSettings.Settings[MessageTypes.Channel] = setting;
                    break;
                case "JoinMessage":
                    Properties.Settings.Default.JoinMessage = MessageSettings.ObjToSetting(setting);
                    MessageSettings.Settings[MessageTypes.Join] = setting;
                    break;
                case "PartMessage":
                    Properties.Settings.Default.PartMessage = MessageSettings.ObjToSetting(setting);
                    MessageSettings.Settings[MessageTypes.Part] = setting;
                    break;
                case "QuitMessage":
                    Properties.Settings.Default.QuitMessage = MessageSettings.ObjToSetting(setting);
                    MessageSettings.Settings[MessageTypes.Quit] = setting;
                    break;
                case "ActionMessage":
                    Properties.Settings.Default.ActionMessage = MessageSettings.ObjToSetting(setting);
                    MessageSettings.Settings[MessageTypes.Action] = setting;
                    break;
                case "NoticeMessage":
                    Properties.Settings.Default.NoticeMessage = MessageSettings.ObjToSetting(setting);
                    MessageSettings.Settings[MessageTypes.Notice] = setting;
                    break;
                case "OfflineMessage":
                    Properties.Settings.Default.OfflineMessage = MessageSettings.ObjToSetting(setting);
                    MessageSettings.Settings[MessageTypes.Offline] = setting;
                    break;
                case "BuddyJoinMessage":
                    Properties.Settings.Default.BuddyJoinedMessage = MessageSettings.ObjToSetting(setting);
                    MessageSettings.Settings[MessageTypes.BuddyJoined] = setting;
                    break;
                case "MessageTimeChange":
                    Properties.Settings.Default.MessageTimeStyle = MessageSettings.ObjToSetting(setting);
                    MessageSettings.MessageTime = setting;
                    break;
                case "HyperlinkStyleChange":
                    Properties.Settings.Default.HyperLinkStyle = MessageSettings.ObjToSetting(setting);
                    MessageSettings.Hyperlink = setting;
                    break;
                case "LeagueFound":
                    Properties.Settings.Default.LeagueFoundMessage = MessageSettings.ObjToSetting(setting);
                    MessageSettings.LeagueFound = setting;
                    break;
            }

            Properties.Settings.Default.Save();
            SaveSettings();
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
