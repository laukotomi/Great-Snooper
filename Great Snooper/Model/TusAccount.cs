namespace GreatSnooper.Model
{
    using GreatSnooper.Helpers;

    public class TusAccount
    {
        public TusAccount(string[] data)
        {
            this.TusNick = data[1];
            int rank;
            if (int.TryParse(data[2].Substring(1), out rank))
            {
                this.Rank = Ranks.GetRankByInt(rank - 1);
            }
            else
            {
                this.Rank = Ranks.Unknown;
            }
            Country = Countries.GetCountryByCC(data[3].ToUpper());
            this.TusLink = data[4];
            this.Clan = data[5];
            this.Active = true;
        }

        public bool Active
        {
            get;
            set;
        }

        public string Clan
        {
            get;
            private set;
        }

        public Country Country
        {
            get;
            private set;
        }

        public Rank Rank
        {
            get;
            set;
        }

        public string TusLink
        {
            get;
            private set;
        }

        public string TusNick
        {
            get;
            private set;
        }

        public User User
        {
            get;
            set;
        }
    }
}