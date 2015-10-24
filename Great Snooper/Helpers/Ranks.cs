using GreatSnooper.Model;
using System.Collections.Generic;

namespace GreatSnooper.Helpers
{
    class Ranks
    {
        public static List<Rank> RankList { get; private set; }
        public static Rank DefaultRank { get; private set; }

        public static void Initialize()
        {
            RankList = new List<Rank>();
            RankList.Add(new Rank("Beginner"));
            RankList.Add(new Rank("Rookie"));
            RankList.Add(new Rank("Novice"));
            RankList.Add(new Rank("Average"));
            RankList.Add(new Rank("Above average"));
            RankList.Add(new Rank("Competent"));
            RankList.Add(new Rank("Veteran"));
            RankList.Add(new Rank("Highly distinguished"));
            RankList.Add(new Rank("Major"));
            RankList.Add(new Rank("Field Marshall"));
            RankList.Add(new Rank("Superstar"));
            RankList.Add(new Rank("Elite"));
            RankList.Add(new Rank("Unknown"));
            RankList.Add(new Rank("Snooper"));

            DefaultRank = RankList[12];
        }

        public static Rank GetRankByInt(int rank)
        {
            if (rank >= 0 && rank <= 13)
                return RankList[rank];
            return DefaultRank;
        }
    }
}
