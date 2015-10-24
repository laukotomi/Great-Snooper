using GreatSnooper.Model;
using GreatSnooper.Services;
using GreatSnooper.ViewModel;
using MahApps.Metro.Controls;
using System.Collections.Generic;
using System.Windows;

namespace GreatSnooper.Windows
{
    public partial class LeagueSearcherWindow : MetroWindow
    {
        private LeagueSearcherViewModel vm;

        public LeagueSearcherWindow(List<League> leagues, ChannelViewModel channel)
        {
            this.vm = new LeagueSearcherViewModel(leagues, channel);
            this.vm.DialogService = new MetroDialogService(this);
            this.DataContext = this.vm;
            InitializeComponent();
        }
    }
}
