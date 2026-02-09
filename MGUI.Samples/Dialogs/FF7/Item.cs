using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Samples.Dialogs.FF7
{
    [Flags]
    public enum StatusAilments
    {
        None = 0,
        Poison = 1 << 0,
        Sleep = 1 << 1,
        Silence = 1 << 2,
        Confusion = 1 << 3,
        Slow = 1 << 4,
        Haste = 1 << 5,
        Stop = 1 << 6,
        Downed = 1 << 7,
        Darkness = 1 << 8,
        Petrify = 1 << 9,
        Frog = 1 << 10,
        All = None | Poison | Sleep | Silence | Confusion | Slow | Haste | Stop | Downed | Darkness | Petrify | Frog
    }

    public readonly record struct Item(string Name, string Description, int HPRecovery, double HPRecoveryPercent, int MPRecovery, double MPRecoveryPercent,
        StatusAilments HealedAilments = StatusAilments.None, StatusAilments AppliedAilments = StatusAilments.None)
    {
        public static readonly Item Potion = new("Potion", "Restores HP by 100", 100, 0, 0, 0);
        public static readonly Item HiPotion = new("Hi-Potion", "Restores HP by 500", 500, 0, 0, 0);
        public static readonly Item XPotion = new("X-Potion", "Fully Restores HP", 0, 1.0, 0, 0);
        public static readonly Item MiniEther = new("Mini-Ether", "Restores MP by 10", 0, 0, 10, 0);
        public static readonly Item Ether = new("Ether", "Restores MP by 100", 0, 0, 100, 0);
        public static readonly Item TurboEther = new("Turbo Ether", "Fully Restores MP", 0, 0, 0, 1.0);
        public static readonly Item Elixir = new("Elixir", "Fully Restores HP/MP", 0, 1.0, 0, 1.0);
        public static readonly Item PhoenixDown = new("Phoenix Down", "Restores life", 0, 0.25, 0, 0, StatusAilments.Downed);
        public static readonly Item PoisonousBrew = new("Poisonous Brew", "Damages HP by 100 for testing purposes", -100, 0.0, 0, 0.0);
        public static readonly Item Antidote = new("Antidote", "Cures [Poison]", 0, 0, 0, 0, StatusAilments.Poison);
        public static readonly Item EyeDrop = new("Eye Drop", "Cures [Darkness]", 0, 0, 0, 0, StatusAilments.Darkness);
        public static readonly Item Soft = new("Soft", "Cures [Petrify]", 0, 0, 0, 0, StatusAilments.Petrify);
        public static readonly Item MaidensKiss = new("Maiden's Kiss", "Cures [Frog]", 0, 0, 0, 0, StatusAilments.Frog);
        public static readonly Item EchoScreen = new("Echo Screen", "Cures [Silence]", 0, 0, 0, 0, StatusAilments.Silence);
        public static readonly Item Remedy = new("Remedy", "Cures abnormal status", 0, 0, 0, 0, StatusAilments.All & ~StatusAilments.Downed);
        public static readonly Item Megalixir = new("Megalixir", "Fully restores all members HP/MP", 0, 1.0, 0, 1.0);
    }

}
