using GreatSnooper.ViewModel;
using MahApps.Metro.Controls;
using System.Collections.Generic;

namespace GreatSnooper.Windows
{
    public partial class NewsWindow : MetroWindow
    {
        private NewsViewModel vm;

        public NewsWindow(List<Dictionary<string, string>> news, Dictionary<string, bool> newsSeen)
        {
            this.vm = new NewsViewModel(news, newsSeen);
            this.DataContext = vm;
            InitializeComponent(); 
        }
    }
}
