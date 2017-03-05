namespace GreatSnooper.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Input;
    using System.Windows.Threading;

    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.Command;

    using GreatSnooper.Classes;
    using GreatSnooper.Helpers;
    using GreatSnooper.Model;
    using GreatSnooper.Services;

    class LeagueSearcherViewModel : ViewModelBase
    {
        private ChannelViewModel channel;
        private Dispatcher dispatcher;
        private bool _isSearching;

        public LeagueSearcherViewModel(List<League> leagues, ChannelViewModel channel)
        {
            this.channel = channel;
            this.dispatcher = Dispatcher.CurrentDispatcher;

            var lookingForThese = new HashSet<string>(
                Properties.Settings.Default.SearchForThese.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                GlobalManager.CIStringComparer);

            this.LeaguesToSearch = new List<LeagueToSearch>();
            foreach (var item in leagues)
            {
                bool? isChecked = lookingForThese.Contains(item.ShortName);
                LeaguesToSearch.Add(new LeagueToSearch(item, isChecked));
            }
            this._isSearching = LeagueSearcher.Instance.IsEnabled;

            if (GlobalManager.SpamAllowed)
            {
                this.IsSpamming = Properties.Settings.Default.SpammingChecked;
            }
        }

        public ICommand CloseCommand
        {
            get
            {
                return new RelayCommand(Close);
            }
        }

        public IMetroDialogService DialogService
        {
            get;
            set;
        }

        public bool IsSearching
        {
            get
            {
                return _isSearching;
            }
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

        public bool IsSpamAllowed
        {
            get
            {
                return IsSearching == false && GlobalManager.SpamAllowed;
            }
        }

        public bool? IsSpamming
        {
            get;
            set;
        }

        public List<LeagueToSearch> LeaguesToSearch
        {
            get;
            private set;
        }

        public string StartStopButtonText
        {
            get
            {
                return (_isSearching)
                       ? Localizations.GSLocalization.Instance.StopSearchingText
                       : Localizations.GSLocalization.Instance.StartSearchingText;
            }
        }

        public ICommand StartStopCommand
        {
            get
            {
                return new RelayCommand(StartStop);
            }
        }

        private void Close()
        {
            this.DialogService.CloseRequest();
        }

        private void StartStop()
        {
            if (!this.IsSearching)
            {
                List<string> selectedLeagues = this.LeaguesToSearch
                                               .Where(x => x.IsSearching.HasValue && x.IsSearching.Value)
                                               .Select(x => x.League.ShortName)
                                               .ToList();

                if (selectedLeagues.Count == 0)
                {
                    this.DialogService.ShowDialog(Localizations.GSLocalization.Instance.MissingValueText, Localizations.GSLocalization.Instance.NoLeaguesSelectedError);
                    return;
                }

                if (GlobalManager.SpamAllowed)
                {
                    Properties.Settings.Default.SpammingChecked = IsSpamming.HasValue && IsSpamming.Value;
                }
                SettingsHelper.Save("SearchForThese", selectedLeagues);

                this.dispatcher.BeginInvoke(new Action(() =>
                {
                    LeagueSearcher.Instance.ChangeSearching(
                        this.channel,
                        IsSpamming.HasValue && IsSpamming.Value);
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
    }
}
