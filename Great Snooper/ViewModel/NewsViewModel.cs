using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GreatSnooper.Helpers;
using GreatSnooper.UserControls;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace GreatSnooper.ViewModel
{
    class NewsViewModel : ViewModelBase
    {
        #region Members
        private int _selectedNewsIndex = -1;
        private Dictionary<string, bool> newsSeen;
        #endregion

        #region Properties
        public int SelectedNewsIndex
        {
            get { return _selectedNewsIndex; }
            set
            {
                if (_selectedNewsIndex != value)
                {
                    _selectedNewsIndex = value;

                    if (_selectedNewsIndex != -1)
                    {
                        var data = (Dictionary<string, string>)News[value].Tag;
                        string id;
                        if (data.TryGetValue("id", out id) && !newsSeen.ContainsKey(id))
                        {
                            newsSeen.Add(id, true);
                            SettingsHelper.Save("NewsSeen", newsSeen.Keys);
                        }
                    }
                }
            }
        }
        public List<NewsBody> News { get; private set; }
        #endregion

        public NewsViewModel(List<Dictionary<string, string>> news, Dictionary<string, bool> newsSeen)
        {
            this.newsSeen = newsSeen;
            this.News = new List<NewsBody>();

            foreach (Dictionary<string, string> item in news)
            {
                try
                {
                    if (item["show"] != "1" && !GlobalManager.DebugMode)
                        continue;

                    News.Add(new NewsBody(item));
                }
                catch (Exception) { }
            }

            this.SelectedNewsIndex = 0; // To save newsSeen
        }

        #region NextNewsCommand
        public ICommand NextNewsCommand
        {
            get { return new RelayCommand(NextNews); }
        }

        private void NextNews()
        {
            if (News.Count > 0 && SelectedNewsIndex != -1)
            {
                if (SelectedNewsIndex + 1 < News.Count)
                    SelectedNewsIndex++;
                else
                    SelectedNewsIndex = 0;
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
            if (News.Count > 0 && SelectedNewsIndex != -1)
            {
                if (SelectedNewsIndex > 0)
                    SelectedNewsIndex--;
                else
                    SelectedNewsIndex = News.Count - 1;
                RaisePropertyChanged("SelectedNewsIndex");
            }
        }
        #endregion
    }
}
