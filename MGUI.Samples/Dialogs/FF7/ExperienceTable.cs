using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Samples.Dialogs.FF7
{
    public static class ExperienceTable
    {
        /// <summary>The total amount of XP required for each Level. First value is Level 1. Last value is Level 99.</summary>
        private static readonly ReadOnlyCollection<int> XPTable = new List<int>()
        {
            0, 6, 33, 94, 202, 372, 616, 949, 1384,
            1934, 2614, 3588, 4610, 5809, 7200, 8797, 10614, 12665, 14965,
            17528, 20368, 24161, 27694, 31555, 35759, 40321, 45255, 50576, 56299,
            62438, 69008, 77066, 84643, 92701, 101255, 110320, 119910, 130040, 140725,
            151980, 163820, 176259, 189312, 202994, 217320, 232305, 247963, 264309, 281358,
            299125, 317625, 336872, 356881, 377667, 399245, 421630, 444836, 468878, 493771,
            519530, 546170, 581467, 610297, 640064, 670784, 702471, 735141, 768808, 803488,
            839195, 875945, 913752, 952632, 992599, 1033669, 1075856, 1119176, 1163643, 1209273,
            1256080, 1304080, 1389359, 1441133, 1494178, 1548509, 1604141, 1661090, 1719371, 1778999,
            1839990, 1902360, 1966123, 2031295, 2097892, 2165929, 2235421, 2306384, 2378833, 2452783
        }.AsReadOnly();

        private static readonly Dictionary<int, int> XPByLevel = new Dictionary<int, int>(XPTable.Select((xp, zeroBasedLevel) => new KeyValuePair<int, int>(zeroBasedLevel + 1, xp)));
        public const int MinLevel = 1;
        public static readonly int MaxLevel = XPTable.Count;

        public static int GetLevel(int XP)
        {
            //  Binary search to find the lowest level that the XP is >= to
            int Min = MinLevel;
            int Max = MaxLevel;
            int CurrentLevel;

            do
            {
                CurrentLevel = (Min + Max) / 2;

                int CurrentLevelXP = GetXP(CurrentLevel);
                if (CurrentLevelXP > XP)
                    Max = CurrentLevel - 1;
                else if (CurrentLevelXP < XP)
                {
                    int NextLevelXP = GetXP(CurrentLevel + 1);
                    if (NextLevelXP > XP)
                        return CurrentLevel;
                    Min = CurrentLevel + 1;
                }
                else
                    return CurrentLevel;
            } while (Min < Max);

            return CurrentLevel;
        }

        public static int GetXP(int Level) => XPByLevel[Level];
    }
}
