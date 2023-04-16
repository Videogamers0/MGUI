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

namespace MGUI.Samples.Dialogs.FF7
{
    public class Party
    {
        private List<PartyMember> _Members { get; }
        public IReadOnlyList<PartyMember> Members => _Members;

        public PartyMember AddMember(string Name, MGTextureData Portrait, int XP = 0, int MaxHP = 100, int MaxMP = 10)
        {
            PartyMember NewMember = new(this, Name, Portrait, XP, MaxHP, MaxMP);
            _Members.Add(NewMember);
            return NewMember;
        }

        public Party()
        {
            _Members = new();
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
        public MGTextureData Portrait { get; }

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

        public PartyMember(Party Party, string Name, MGTextureData Portrait, int XP = 0, int MaxHP = 100, int MaxMP = 10)
        {
            this.Party = Party;
            this.Name = Name;
            this.Portrait = Portrait;
            this.XP = XP;
            this.MaxHP = MaxHP;
            this.MaxMP = MaxMP;
            CurrentHP = MaxHP;
            CurrentMP = MaxMP;
        }
    }
}
