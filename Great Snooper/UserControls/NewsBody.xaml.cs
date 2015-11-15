using GreatSnooper.Classes;
using GreatSnooper.Model;
using System.Windows.Controls;
using System.Windows.Media;

namespace GreatSnooper.UserControls
{
    public partial class NewsBody : Grid
    {
        public NewsBody(News news)
        {
            InitializeComponent();

            this.RTB.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(news.Background));
            this.RTB.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(news.Foreground));
            this.RTB.FontSize = news.FontSize;
            this.RTB.Document = BBParser.Parse(news.BBCode);
        }
    }
}
