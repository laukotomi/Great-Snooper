using GreatSnooper.Model;
using GreatSnooper.ViewModel;
using MahApps.Metro.Controls;
using System.Collections.Generic;

namespace GreatSnooper.Windows
{
    public partial class NewsWindow : MetroWindow
    {
        private NewsViewModel vm;

        public NewsWindow(List<News> news)
        {
            this.vm = new NewsViewModel(news);
            this.DataContext = vm;
            InitializeComponent(); 
        }
    }
}
