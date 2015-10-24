using GreatSnooper.Helpers;

namespace GreatSnooper.Model
{
    public class TusAccount
    {
        #region Properties
        public string TusNick { get; private set; }
        public string TusLink { get; private set; }
        public string Clan { get; private set; }
        public Rank Rank { get; private set; }
        public Country Country { get; private set; }
        public bool Active { get; set; }
        public User User { get; set; }
        #endregion

        public TusAccount(string[] data)
        {
            this.TusNick = data[1];
            int rank;
            if (int.TryParse(data[2].Substring(1), out rank))
                this.Rank = Ranks.GetRankByInt(rank - 1);
            else
                this.Rank = Ranks.DefaultRank;
            Country = Countries.GetCountryByCC(data[3].ToUpper());
            TusLink = data[4];
            Clan = data[5];
            Active = true;
        }
    }
}
