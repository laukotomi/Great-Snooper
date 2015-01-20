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
        private string styleName;
        private MessageSetting actSetting;

        public event SaveSettingsDelegate SaveSetting;

        public FontChooser() { } // Never used, but visual stdio throws an error if not exists
        public FontChooser(string styleName, string title, MessageSetting setting)
        {
            InitializeComponent();

            Title = title;

            SortedDictionary<string, FontFamily> fontFamilies = new SortedDictionary<string, FontFamily>();
            IEnumerator<FontFamily> iterator = Fonts.SystemFontFamilies.GetEnumerator();
            while (iterator.MoveNext())
                fontFamilies.Add(iterator.Current.ToString(), iterator.Current);

            TheFontfamily.ItemsSource = fontFamilies;

            this.styleName = styleName;

            for (double i = 1; i <= 20; i++)
                TheSize.Items.Add(i);

            actSetting = setting;

            TheFontfamily.SelectedItem = new KeyValuePair<string, FontFamily>(setting.Fontfamily.ToString(), setting.Fontfamily);
            TheFontfamily.ScrollIntoView(TheFontfamily.SelectedItem);
            TheSize.SelectedItem = setting.Size;
            TheColor.SelectedColor = setting.Color.Color;
            TheBold.IsChecked = setting.Bold == FontWeights.Bold;
            TheItalic.IsChecked = setting.Italic == FontStyles.Italic;
            TheStrikethrough.IsChecked = setting.Strikethrough;
            TheUnderline.IsChecked = setting.Underline;
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
            actSetting.Fontfamily = new FontFamily(Example.FontFamily.ToString()); 
            actSetting.Color = new SolidColorBrush(TheColor.SelectedColor);
            actSetting.Size = Example.FontSize;
            actSetting.Bold = TheBold.IsChecked.Value ? FontWeights.Bold : FontWeights.Normal;
            actSetting.Italic = TheItalic.IsChecked.Value ? FontStyles.Italic : FontStyles.Normal;
            actSetting.Strikethrough = TheStrikethrough.IsChecked.Value;
            actSetting.Underline = TheUnderline.IsChecked.Value;
            
            Properties.Settings.Default.GetType().GetProperty(styleName).SetValue(Properties.Settings.Default, MessageSettings.ObjToSetting(actSetting));
            Properties.Settings.Default.Save();

            if (SaveSetting != null)
                SaveSetting();

            this.Close();
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
