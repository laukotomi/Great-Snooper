using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GreatSnooper.Helpers;
using GreatSnooper.Model;
using GreatSnooper.UserControls;
using System.Collections.Generic;
using System.Windows.Input;

namespace GreatSnooper.ViewModel
{
    class NewsViewModel : ViewModelBase
    {
        #region Members
        #endregion

        #region Properties
        public List<NewsBody> News { get; private set; }
        public int SelectedNewsIndex { get; set; }
        #endregion

        public NewsViewModel(List<News> news)
        {
            this.News = new List<NewsBody>();

            foreach (News item in news)
            {
                if (!item.Show && !GlobalManager.DebugMode)
                    continue;

                News.Add(new NewsBody(item));
                if (item.ID > Properties.Settings.Default.LastNewsID)
                    SettingsHelper.Save("LastNewsID", item.ID);
            }
        }

        #region NextNewsCommand
        public ICommand NextNewsCommand
        {
            get { return new RelayCommand(NextNews); }
        }

        private void NextNews()
        {
            if (News.Count > 0)
            {
                if (SelectedNewsIndex + 1 < News.Count)
                    SelectedNewsIndex++;
                RaisePropertyChanged("SelectedNewsIndex");
            }
        }
        #endregion

        #region PrevNewsCommand
        public ICommand PrevNewsCommand
        {
            get { return new RelayCommand(PrevNews); }
        }

        private void PrevNews()
        {
            if (News.Count > 0)
            {
                if (SelectedNewsIndex > 0)
                    SelectedNewsIndex--;
                RaisePropertyChanged("SelectedNewsIndex");
            }
        }
        #endregion
    }
}
