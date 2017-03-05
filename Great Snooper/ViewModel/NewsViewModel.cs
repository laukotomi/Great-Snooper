namespace GreatSnooper.ViewModel
{
    using System.Collections.Generic;
    using System.Windows.Input;

    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.Command;

    using GreatSnooper.Helpers;
    using GreatSnooper.Model;
    using GreatSnooper.UserControls;

    class NewsViewModel : ViewModelBase
    {
        public NewsViewModel(List<News> news)
        {
            this.News = new List<NewsBody>();

            foreach (News item in news)
            {
                if (!item.Show && !GlobalManager.DebugMode)
                {
                    continue;
                }

                News.Add(new NewsBody(item));
                if (item.ID > Properties.Settings.Default.LastNewsID)
                {
                    SettingsHelper.Save("LastNewsID", item.ID);
                }
            }
        }

        public List<NewsBody> News
        {
            get;
            private set;
        }

        public ICommand NextNewsCommand
        {
            get
            {
                return new RelayCommand(NextNews);
            }
        }

        public ICommand PrevNewsCommand
        {
            get
            {
                return new RelayCommand(PrevNews);
            }
        }

        public int SelectedNewsIndex
        {
            get;
            set;
        }

        private void NextNews()
        {
            if (News.Count > 0)
            {
                if (SelectedNewsIndex + 1 < News.Count)
                {
                    SelectedNewsIndex++;
                }
                RaisePropertyChanged("SelectedNewsIndex");
            }
        }

        private void PrevNews()
        {
            if (News.Count > 0)
            {
                if (SelectedNewsIndex > 0)
                {
                    SelectedNewsIndex--;
                }
                RaisePropertyChanged("SelectedNewsIndex");
            }
        }
    }
}