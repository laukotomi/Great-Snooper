using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xceed.Wpf.Toolkit;


namespace MySnooper
{
    public delegate void SaveSettingsDelegate(object sender, EventArgs e);

    public partial class FontChooser : MetroWindow
    {
        private readonly string styleName;
        private readonly MessageSetting actSetting;
        private readonly TextBlock tb1;
        private readonly TextBlock tb2;

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
            if (actSetting.OneColorOnly)
            {
                tb1 = new TextBlock() { Text = "Example" };
                ExampleSP.Children.Add(tb1);
            }
            else
            {
                tb1 = new TextBlock() { Text = "Player: ", FontWeight = FontWeights.Bold };
                tb2 = new TextBlock() { Text = "message" };
                ExampleSP.Children.Add(tb1);
                ExampleSP.Children.Add(tb2);
            }

            TheFontfamily.SelectedItem = new KeyValuePair<string, FontFamily>(setting.Fontfamily.ToString(), setting.Fontfamily);
            TheFontfamily.ScrollIntoView(TheFontfamily.SelectedItem);
            TheSize.SelectedItem = setting.Size;
            NickColor.SelectedColor = setting.NickColor.Color;
            if (setting.OneColorOnly == false)
                MessageColor.SelectedColor = setting.MessageColor.Color;
            else
                MessageColor.Visibility = System.Windows.Visibility.Collapsed;
            TheBold.IsChecked = setting.Bold == FontWeights.Bold;
            BoldChanged(null, null);
            TheItalic.IsChecked = setting.Italic == FontStyles.Italic;
            ItalicChanged(null, null);
            TheStrikethrough.IsChecked = setting.Strikethrough;
            StrikethroughChanged(null, null);
            TheUnderline.IsChecked = setting.Underline;
            UnderlineChanged(null, null);
        }

        private void FontFamilyChanged(object sender, SelectionChangedEventArgs e)
        {
            var obj = sender as ListView;
            var data = (KeyValuePair<string, FontFamily>)obj.SelectedItem;
            tb1.FontFamily = data.Value;
            if (actSetting.OneColorOnly == false)
                tb2.FontFamily = data.Value;
        }

        private void FontSizeChanged(object sender, SelectionChangedEventArgs e)
        {
            var obj = sender as ComboBox;
            tb1.FontSize = double.Parse(obj.SelectedItem.ToString());
            if (actSetting.OneColorOnly == false)
                tb2.FontSize = double.Parse(obj.SelectedItem.ToString());
        }

        private void NickColorChanged(object sender, RoutedPropertyChangedEventArgs<Color> e)
        {
            var obj = sender as ColorPicker;
            tb1.Foreground = new SolidColorBrush(obj.SelectedColor);
        }

        private void MessageColorChanged(object sender, RoutedPropertyChangedEventArgs<Color> e)
        {
            var obj = sender as ColorPicker;
            tb2.Foreground = new SolidColorBrush(obj.SelectedColor);
        }

        private void BoldChanged(object sender, RoutedEventArgs e)
        {
            if (actSetting.OneColorOnly)
                tb1.FontWeight = TheBold.IsChecked.Value ? FontWeights.Bold : FontWeights.Normal;
            else
                tb2.FontWeight = TheBold.IsChecked.Value ? FontWeights.Bold : FontWeights.Normal;
        }

        private void ItalicChanged(object sender, RoutedEventArgs e)
        {
            tb1.FontStyle = TheItalic.IsChecked.Value ? FontStyles.Italic : FontStyles.Normal;
            if (actSetting.OneColorOnly == false)
                tb2.FontStyle = TheItalic.IsChecked.Value ? FontStyles.Italic : FontStyles.Normal;
        }

        private void StrikethroughChanged(object sender, RoutedEventArgs e)
        {
            tb1.TextDecorations = GetTextDecorations();
            if (actSetting.OneColorOnly == false)
                tb2.TextDecorations = GetTextDecorations();
        }

        private void UnderlineChanged(object sender, RoutedEventArgs e)
        {
            tb1.TextDecorations = GetTextDecorations();
            if (actSetting.OneColorOnly == false)
                tb2.TextDecorations = GetTextDecorations();
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
            actSetting.Fontfamily = new FontFamily(tb1.FontFamily.ToString()); 
            actSetting.NickColor = new SolidColorBrush(NickColor.SelectedColor);
            if (actSetting.OneColorOnly)
                actSetting.MessageColor = new SolidColorBrush(NickColor.SelectedColor);
            else
                actSetting.MessageColor = new SolidColorBrush(MessageColor.SelectedColor);
            actSetting.Size = tb1.FontSize;
            actSetting.Bold = TheBold.IsChecked.Value ? FontWeights.Bold : FontWeights.Normal;
            actSetting.Italic = TheItalic.IsChecked.Value ? FontStyles.Italic : FontStyles.Normal;
            actSetting.Strikethrough = TheStrikethrough.IsChecked.Value;
            actSetting.Underline = TheUnderline.IsChecked.Value;
            
            Properties.Settings.Default.GetType().GetProperty(styleName).SetValue(Properties.Settings.Default, MessageSettings.ObjToSetting(actSetting), null);
            Properties.Settings.Default.Save();

            if (SaveSetting != null)
                SaveSetting.BeginInvoke(this, null, null, null);

            this.Close();
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
