
namespace GreatSnooper.Model
{
    class LeagueToSearch
    {
        public League League { get; private set; }
        public bool? IsSearching { get; set; }

        public LeagueToSearch(League league, bool? isSearching)
        {
            this.League = league;
            this.IsSearching = isSearching;
        }
    }
}
