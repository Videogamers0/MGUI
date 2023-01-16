using MGUI.Core.UI;
using MGUI.Shared.Helpers;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Samples.FF7_Samples
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

    public class Party
    {
        private List<PartyMember> _Members { get; }
        public IReadOnlyList<PartyMember> Members => _Members;

        public PartyMember AddMember(string Name, Texture2D Portrait, int XP = 0, int MaxHP = 100, int MaxMP = 10)
        {
            PartyMember NewMember = new(this, Name, Portrait, XP, MaxHP, MaxMP);
            _Members.Add(NewMember);
            return NewMember;
        }

        public Party()
        {
            this._Members = new();
        }

    }

    public class PartyMember : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public virtual void NotifyPropertyChanged(string szPropertyName) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(szPropertyName)); }
        /// <summary>Notify Property Changed for the given <paramref name="szPropertyName"/></summary>
        public void NPC(string szPropertyName) { NotifyPropertyChanged(szPropertyName); }

        public Party Party { get; }
        public string Name { get; }
        public Texture2D Portrait { get; }

        #region XP / Level
        private int _XP;
        public int XP
        {
            get => _XP;
            set
            {
                int Value = Math.Max(0, value);
                if (_XP != Value)
                {
                    int Previous = XP;
                    _XP = Value;
                    Level = ExperienceTable.GetLevel(XP);
                    XPChanged?.Invoke(this, new(Previous, XP));
                    NPC(nameof(XP));
                }
            }
        }

        public event EventHandler<EventArgs<int>> XPChanged;

        private int _Level;
        public int Level
        {
            get => _Level;
            private set
            {
                if (_Level != value)
                {
                    int Previous = Level;
                    _Level = value;
                    CurrentLevelXP = ExperienceTable.GetXP(Level);
                    NextLevelXP = ExperienceTable.GetXP(Math.Min(ExperienceTable.MaxLevel, Level + 1));
                    LevelChanged?.Invoke(this, new(Previous, Level));
                    NPC(nameof(Level));
                    NPC(nameof(CurrentLevelXP));
                    NPC(nameof(NextLevelXP));
                }
            }
        }

        public event EventHandler<EventArgs<int>> LevelChanged;

        public int CurrentLevelXP { get; private set; }
        public int NextLevelXP { get; private set; }
        #endregion XP / Level

        #region HP / MP
        private int _MaxHP;
        public int MaxHP
        {
            get => _MaxHP;
            set
            {
                if (_MaxHP != value)
                {
                    int Previous = MaxHP;
                    _MaxHP = value;
                    MaxHPChanged?.Invoke(this, new(Previous, MaxHP));
                    NPC(nameof(MaxHP));
                }
            }
        }

        public event EventHandler<EventArgs<int>> MaxHPChanged;

        private int _CurrentHP;
        public int CurrentHP
        {
            get => _CurrentHP;
            set
            {
                if (_CurrentHP != value)
                {
                    int Previous = CurrentHP;
                    _CurrentHP = value;
                    HPChanged?.Invoke(this, new(Previous, CurrentHP));
                    NPC(nameof(CurrentHP));
                }
            }
        }

        public event EventHandler<EventArgs<int>> HPChanged;

        private int _MaxMP;
        public int MaxMP
        {
            get => _MaxMP;
            set
            {
                if (_MaxMP != value)
                {
                    int Previous = MaxMP;
                    _MaxMP = value;
                    MaxMPChanged?.Invoke(this, new(Previous, MaxMP));
                    NPC(nameof(MaxMP));
                }
            }
        }

        public event EventHandler<EventArgs<int>> MaxMPChanged;

        private int _CurrentMP;
        public int CurrentMP
        {
            get => _CurrentMP;
            set
            {
                if (_CurrentMP != value)
                {
                    int Previous = CurrentMP;
                    _CurrentMP = value;
                    MPChanged?.Invoke(this, new(Previous, CurrentMP));
                    NPC(nameof(CurrentMP));
                }
            }
        }

        public event EventHandler<EventArgs<int>> MPChanged;
        #endregion HP / MP

        public PartyMember(Party Party, string Name, Texture2D Portrait, int XP = 0, int MaxHP = 100, int MaxMP = 10)
        {
            this.Party = Party;
            this.Name = Name;
            this.Portrait = Portrait;
            this.XP = XP;
            this.MaxHP = MaxHP;
            this.MaxMP = MaxMP;
            this.CurrentHP = MaxHP;
            this.CurrentMP = MaxMP;
        }
    }
}
