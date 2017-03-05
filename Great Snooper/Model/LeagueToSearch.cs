namespace GreatSnooper.Model
{
    class LeagueToSearch
    {
        public LeagueToSearch(League league, bool? isSearching)
        {
            this.League = league;
            this.IsSearching = isSearching;
        }

        public bool? IsSearching
        {
            get;
            set;
        }

        public League League
        {
            get;
            private set;
        }
    }
}