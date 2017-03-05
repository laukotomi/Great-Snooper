namespace GreatSnooper.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Data;
    using System.Windows.Input;

    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.Command;

    using GreatSnooper.Model;

    public partial class MainViewModel : ViewModelBase, IDisposable
    {
        public ICommand FilterCommand
        {
            get
            {
                return new RelayCommand(Filter);
            }
        }

        public ICommand FilterFocusCommand
        {
            get
            {
                return new RelayCommand(FilterFocus);
            }
        }

        public ICommand FilterLeftCommand
        {
            get
            {
                return new RelayCommand(FilterLeft);
            }
        }

        private void Filter()
        {
            IsFilterFocused = true;
        }

        private void FilterFocus()
        {
            if (FilterText.Trim() == Localizations.GSLocalization.Instance.FilterText)
            {
                FilterText = string.Empty;
                RaisePropertyChanged("FilterText");
            }
        }

        private void FilterLeft()
        {
            if (FilterText.Trim() == string.Empty)
            {
                FilterText = Localizations.GSLocalization.Instance.FilterText;
                RaisePropertyChanged("FilterText");
            }
        }

        private void filterTimer_Tick(object sender, EventArgs e)
        {
            filterTimer.Stop();

            if (this.SelectedGLChannel == null || !this.SelectedGLChannel.Joined)
            {
                return;
            }

            List<string> words = new List<string>();
            string[] filtersTemp = FilterText.Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < filtersTemp.Length; i++)
            {
                string temp = filtersTemp[i].Trim();
                if (temp.Length >= 1)
                {
                    words.Add(temp);
                }
            }

            if (words.Count == 0)
            {
                this.SelectedGLChannel.UserListDG.SetUserListDGView();
            }
            else
            {
                var view = CollectionViewSource.GetDefaultView(this.SelectedGLChannel.Users);
                if (view != null)
                {
                    view.Filter = x =>
                    {
                        User u = (User)x;
                        if (!Properties.Settings.Default.ShowBannedUsers && u.IsBanned)
                        {
                            return false;
                        }

                        foreach (string word in words)
                        {
                            if (word.Length == 1)
                            {
                                if (u.Name.StartsWith(word, StringComparison.OrdinalIgnoreCase)
                                || u.TusAccount != null && u.TusAccount.TusNick.StartsWith(word, StringComparison.OrdinalIgnoreCase))
                                {
                                    return true;
                                }
                            }
                            else if (
                                u.Name.IndexOf(word, StringComparison.OrdinalIgnoreCase) != -1
                                || u.Clan.StartsWith(word, StringComparison.OrdinalIgnoreCase)
                                || u.TusAccount != null && (
                                    u.TusAccount.TusNick.IndexOf(word, StringComparison.OrdinalIgnoreCase) != -1
                                    || u.TusAccount.Clan.StartsWith(word, StringComparison.OrdinalIgnoreCase))
                                || u.Country != null && u.Country.Name.StartsWith(word, StringComparison.OrdinalIgnoreCase)
                                || u.Rank != null && u.Rank.Name.StartsWith(word, StringComparison.OrdinalIgnoreCase)
                                || Properties.Settings.Default.ShowInfoColumn && u.ClientName != null && u.ClientName.IndexOf(word, StringComparison.OrdinalIgnoreCase) != -1)
                            {
                                return true;
                            }
                        }
                        return false;
                    };
                }
            }
        }
    }
}
