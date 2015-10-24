using GreatSnooper.Classes;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

namespace GreatSnooper.UserControls
{
    public partial class NewsBody : Grid
    {
        public NewsBody(Dictionary<string, string> data)
        {
            InitializeComponent();

            this.Tag = data;

            string bg;
            if (data.TryGetValue("background", out bg))
                this.RTB.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bg));
            string tc;
            if (data.TryGetValue("textcolor", out tc))
                this.RTB.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(tc));
            string fs = "13";
            if (data.TryGetValue("fontsize", out fs))
                this.RTB.FontSize = double.Parse(fs);
            this.RTB.Document = BBParser.Parse(data["bbcode"]);

        }
    }
}
