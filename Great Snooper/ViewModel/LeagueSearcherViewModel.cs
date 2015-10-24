using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GreatSnooper.Classes;
using GreatSnooper.Helpers;
using GreatSnooper.Model;
using GreatSnooper.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;

namespace GreatSnooper.ViewModel
{
    class LeagueSearcherViewModel : ViewModelBase
    {
        #region Members
        private bool _isSearching;
        private Dispatcher dispatcher;
        private ChannelViewModel channel;
        #endregion

        #region Properties
        public bool IsSearching
        {
            get { return _isSearching; }
            private set
            {
                if (_isSearching != value)
                {
                    _isSearching = value;
                    RaisePropertyChanged("IsSearching");
                    RaisePropertyChanged("IsSpamAllowed");
                    RaisePropertyChanged("StartStopButtonText");
                }
            }
        }
        public List<LeagueToSearch> LeaguesToSearch { get; private set; }
        public bool? IsSpamming { get; set; }
        public bool IsSpamAllowed
        {
            get { return IsSearching == false && GlobalManager.SpamAllowed; }
        }
        public IMetroDialogService DialogService { get; set; }
        public string StartStopButtonText
        {
            get
            {
                return (_isSearching)
                    ? Localizations.GSLocalization.Instance.StopSearchingText
                    : Localizations.GSLocalization.Instance.StartSearchingText;
            }
        }
        #endregion

        public LeagueSearcherViewModel(List<League> leagues, ChannelViewModel channel)
        {
            this.channel = channel;
            this.dispatcher = Dispatcher.CurrentDispatcher;

            var lookingForThese = new HashSet<string>(
                Properties.Settings.Default.SearchForThese.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                StringComparer.OrdinalIgnoreCase
            );

            this.LeaguesToSearch = new List<LeagueToSearch>();
            foreach (var item in leagues)
            {
                bool? isChecked = lookingForThese.Contains(item.ShortName);
                LeaguesToSearch.Add(new LeagueToSearch(item, isChecked));
            }
            this._isSearching = LeagueSearcher.Instance.IsEnabled;

            if (GlobalManager.SpamAllowed)
                this.IsSpamming = Properties.Settings.Default.SpammingChecked;
        }

        #region StartStopCommand
        public ICommand StartStopCommand
        {
            get { return new RelayCommand(StartStop); }
        }

        private void StartStop()
        {
            if (!this.IsSearching)
            {
                var selectedLeagues = this.LeaguesToSearch.Where(x => x.IsSearching.HasValue && x.IsSearching.Value).Select(x => x.League.ShortName).ToList();
                if (selectedLeagues.Count == 0)
                {
                    this.DialogService.ShowDialog(Localizations.GSLocalization.Instance.MissingValueText, Localizations.GSLocalization.Instance.NoLeaguesSelectedError);
                    return;
                }

                Properties.Settings.Default.SearchForThese = string.Join(",", selectedLeagues);
                if (GlobalManager.SpamAllowed)
                    Properties.Settings.Default.SpammingChecked = IsSpamming.HasValue && IsSpamming.Value;
                Properties.Settings.Default.Save();

                this.dispatcher.BeginInvoke(new Action(() =>
                {
                    LeagueSearcher.Instance.ChangeSearching(
                        this.channel,
                        IsSpamming.HasValue && IsSpamming.Value
                    );
                }));

                this.CloseCommand.Execute(null);
            }
            else
            {
                this.IsSearching = false;
                this.dispatcher.BeginInvoke(new Action(() =>
                {
                    LeagueSearcher.Instance.ChangeSearching(null);
                }));
            }
        }
        #endregion

        #region CloseCommand
        public ICommand CloseCommand
        {
            get { return new RelayCommand(Close); }
        }

        private void Close()
        {
            this.DialogService.CloseRequest();
        }
        #endregion
    }
}
