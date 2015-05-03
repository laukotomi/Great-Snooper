using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MySnooper
{
    public static class RanksClass
    {
        // The countries object
        public static List<RankClass> Ranks = new List<RankClass>()
        {
            new RankClass("Beginner"),
            new RankClass("Rookie"),
            new RankClass("Novice"),
            new RankClass("Average"),
            new RankClass("Above average"),
            new RankClass("Competent"),
            new RankClass("Veteran"),
            new RankClass("Highly distinguished"),
            new RankClass("Major"),
            new RankClass("Field Marshall"),
            new RankClass("Superstar"),
            new RankClass("Elite"),
            new RankClass("Unknown"),
            new RankClass("Snooper")
        };

        public static RankClass GetRankByInt(int rank)
        {
            if (rank >= 0 && rank <= 13)
                return Ranks[rank];
            return Ranks[12];
        }
    }
}
