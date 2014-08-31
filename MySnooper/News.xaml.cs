using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MySnooper
{
    /// <summary>
    /// Interaction logic for News.xaml
    /// </summary>
    public partial class News : MetroWindow
    {
        public static RoutedCommand NextNewsCommand = new RoutedCommand();
        public static RoutedCommand PrevNewsCommand = new RoutedCommand();

        private Dictionary<string, bool> NewsSeen;

        public News(List<Dictionary<string, string>> news, Dictionary<string, bool> NewsSeen)
        {
            InitializeComponent();

            this.NewsSeen = NewsSeen;
            NewsFlipView.Items.Clear();
            foreach (Dictionary<string, string> item in news)
            {
                try
                {
                    if (item["show"] != "1" && !GlobalManager.DebugMode)
                        continue;

                    Grid g = new Grid();
                    g.Tag = item;
                    RichTextBox rtb = new RichTextBox();
                    rtb.IsReadOnly = true;
                    rtb.Focusable = false;
                    rtb.IsDocumentEnabled = true;
                    rtb.BorderThickness = new Thickness(0);
                    rtb.FontFamily = new FontFamily("Segoe UI, Arial");
                    string bg;
                    if (item.TryGetValue("background", out bg))
                        rtb.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bg));
                    string tc;
                    if (item.TryGetValue("textcolor", out tc))
                        rtb.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(tc));
                    string fs;
                    if (item.TryGetValue("fontsize", out fs))
                        rtb.FontSize = 13;//double.Parse(fs);
                    rtb.Document = BBParser.Parse(item["bbcode"]);
                    g.Children.Add(rtb);
                    NewsFlipView.Items.Add(g);
                }
                catch (Exception) { }
            }
        }

        public void NextNews(object sender, ExecutedRoutedEventArgs e)
        {
            if (NewsFlipView.Items.Count > 0)
            {
                if (NewsFlipView.SelectedIndex + 1 < NewsFlipView.Items.Count)
                    NewsFlipView.SelectedIndex = NewsFlipView.SelectedIndex + 1;
                else
                    NewsFlipView.SelectedIndex = 0;
            }
            e.Handled = true;
        }

        public void PrevNews(object sender, ExecutedRoutedEventArgs e)
        {
            if (NewsFlipView.Items.Count > 0)
            {
                if (NewsFlipView.SelectedIndex - 1 > -1)
                    NewsFlipView.SelectedIndex = NewsFlipView.SelectedIndex - 1;
                else
                    NewsFlipView.SelectedIndex = NewsFlipView.Items.Count - 1;
            }
            e.Handled = true;
        }

        public void CanExecuteCustomCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
            e.Handled = true;
        }

        private void NewsFlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NewsFlipView.SelectedItem != null)
            {
                Dictionary<string, string> NewsData = (Dictionary<string, string>)((Grid)NewsFlipView.SelectedItem).Tag;
                string id;
                if (NewsData.TryGetValue("id", out id) && !NewsSeen.ContainsKey(id))
                {
                    NewsSeen.Add(id, true);
                }
            }
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            int i = 0;
            foreach (var item in NewsSeen)
            {
                sb.Append(item.Key);
                if (i + 1 < NewsSeen.Count)
                    sb.Append(',');
                i++;
            }
            Properties.Settings.Default.NewsSeen = sb.ToString();
            Properties.Settings.Default.Save();
        }
    }
}
