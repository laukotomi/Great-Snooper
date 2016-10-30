using GalaSoft.MvvmLight;
using GreatSnooper.Helpers;
using GreatSnooper.Model;
using GreatSnooper.ViewModel;
using System;
using System.Collections.Generic;

namespace GreatSnooper.Classes
{
    public delegate void MessageRegexChangedDelegate(object sender);

    public class LeagueSearcher : ObservableObject
    {
        #region Static
        private static LeagueSearcher instance;
        #endregion

        #region Members
        #endregion

        #region Properties
        public static LeagueSearcher Instance
        {
            get
            {
                if (instance == null)
                    instance = new LeagueSearcher();
                return instance;
            }
        }
        public bool IsEnabled
        {
            get { return ChannelToSearch != null; }
        }
        public ChannelViewModel ChannelToSearch { get; private set; }
        public Dictionary<string, Dictionary<string, DateTime>> SearchData { get; private set; }
        public int SpamLeft { get; set; }
        public int Counter { get; set; }
        public string SearchingText { get; private set; }
        #endregion

        #region Events
        public event MessageRegexChangedDelegate MessageRegexChange;
        #endregion

        private LeagueSearcher()
        {
            SearchData = new Dictionary<string, Dictionary<string, DateTime>>(StringComparer.OrdinalIgnoreCase);
        }

        public void ChangeSearching(ChannelViewModel channel, bool spamming = false)
        {
            this.ChannelToSearch = channel;

            SearchData.Clear();
            if (channel != null)
            {
                string[] leaguesToSearch = Properties.Settings.Default.SearchForThese.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string league in leaguesToSearch)
                    SearchData.Add(league, new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase));
                SearchingText = string.Join(" or ", leaguesToSearch) + " anyone?";
            }

            this.SpamLeft = (spamming) ? 10 : -1;
            this.Counter = 1000;

            RaisePropertyChanged("IsEnabled");
            if (MessageRegexChange != null)
                MessageRegexChange(this);
        }

        public void DoSearch()
        {
            this.ChannelToSearch.SendMessage(this.SearchingText);
            this.Counter = 0;
            this.SpamLeft--;

            if (this.SpamLeft == 0)
            {
                this.ChannelToSearch.AddMessage(GlobalManager.SystemUser, Localizations.GSLocalization.Instance.SpamStopMessage, MessageSettings.SystemMessage);
                this.ChangeSearching(null);

                if (Properties.Settings.Default.LeagueFailBeepEnabled)
                    Sounds.PlaySoundByName("LeagueFailBeep");
            }
        }

        internal bool HandleMatch(System.Text.RegularExpressions.Group leagueGroup, Message msg)
        {
            msg.AddHighlightWord(leagueGroup.Index, leagueGroup.Length, Message.HightLightTypes.LeagueFound);

            Dictionary<string, DateTime> foundValues = this.SearchData[leagueGroup.Value];
            DateTime now = DateTime.Now;
            if (!foundValues.ContainsKey(msg.Sender.Name))
            {
                foundValues.Add(msg.Sender.Name, now);
                return true;
            }
            else if (foundValues[msg.Sender.Name] + new TimeSpan(0, 15, 0) < now)
            {
                foundValues[msg.Sender.Name] = now;
                return true;
            }

            return false;
        }
    }
}
