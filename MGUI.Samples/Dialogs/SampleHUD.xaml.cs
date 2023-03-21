using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MGUI.Shared.Rendering;
using Microsoft.Xna.Framework;
using System.Runtime.CompilerServices;
using MGUI.Shared.Helpers;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.ObjectModel;

namespace MGUI.Samples.Dialogs
{
    public class SampleHUD : SampleBase
    {
        private const int PixelsPerUnit = 4;

        public int HPBarWidth => MaxHP * PixelsPerUnit;

        private int _MaxHP;
        public int MaxHP
        {
            get => _MaxHP;
            set
            {
                if (_MaxHP != value)
                {
                    _MaxHP = value;
                    NPC(nameof(MaxHP));
                    NPC(nameof(HPBarWidth));
                }
            }
        }

        private int _CurrentHP;
        public int CurrentHP
        {
            get => _CurrentHP;
            set
            {
                if (_CurrentHP != value)
                {
                    _CurrentHP = value;
                    NPC(nameof(CurrentHP));
                }
            }
        }

        public int MPBarWidth => MaxMP * PixelsPerUnit;

        private int _MaxMP;
        public int MaxMP
        {
            get => _MaxMP;
            set
            {
                if (_MaxMP != value)
                {
                    _MaxMP = value;
                    NPC(nameof(MaxMP));
                    NPC(nameof(MPBarWidth));
                }
            }
        }

        private int _CurrentMP;
        public int CurrentMP
        {
            get => _CurrentMP;
            set
            {
                if (_CurrentMP != value)
                {
                    _CurrentMP = value;
                    NPC(nameof(CurrentMP));
                }
            }
        }

        public int StaminaBarWidth => MaxStamina * PixelsPerUnit;

        private int _MaxStamina;
        public int MaxStamina
        {
            get => _MaxStamina;
            set
            {
                if (_MaxStamina != value)
                {
                    _MaxStamina = value;
                    NPC(nameof(MaxStamina));
                    NPC(nameof(StaminaBarWidth));
                }
            }
        }

        private int _CurrentStamina;
        public int CurrentStamina
        {
            get => _CurrentStamina;
            set
            {
                if (_CurrentStamina != value)
                {
                    _CurrentStamina = value;
                    NPC(nameof(CurrentStamina));
                }
            }
        }

        private int _NextLevelXP;
        public int NextLevelXP
        {
            get => _NextLevelXP;
            set
            {
                if (_NextLevelXP != value)
                {
                    _NextLevelXP = value;
                    NPC(nameof(NextLevelXP));
                }
            }
        }

        private int _CurrentXP;
        public int CurrentXP
        {
            get => _CurrentXP;
            set
            {
                if (_CurrentXP != value)
                {
                    _CurrentXP = value;
                    NPC(nameof(CurrentXP));
                }
            }
        }

        public ObservableCollection<PlayerBuff> ActiveBuffs { get; }

        private static void InitializeResources(ContentManager Content, MGDesktop Desktop)
        {
            const int TextureSpacing = 1;
            const int TextureTopMargin = 166;
            const int TextureIconSize = 12;
            List<(string Name, int Row, int Column)> Icons = new()
            {
                ("HUD_HP", 1, 6),
                ("HUD_MP", 1, 3),
                ("HUD_Stamina", 0, 4),
                ("Buff_Deathtouch", 1, 0),
                ("Buff_BonePlating", 1, 5),
                ("Buff_Precision", 2, 5),
                ("Buff_Voidguard", 0, 3),
                ("Buff_Reflect", 2, 6)
            };
            foreach (var (Name, Row, Column) in Icons)
            {
                Rectangle SourceRect = new(Column * (TextureIconSize + TextureSpacing), TextureTopMargin + Row * (TextureIconSize + TextureSpacing), TextureIconSize, TextureIconSize);
                Desktop.AddNamedRegion(new("AngryMeteor", Name, SourceRect, null));
            }
        }

        public SampleHUD(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Dialogs)}", $"{nameof(SampleHUD)}.xaml", null, () => InitializeResources(Content, Desktop))
        {
            UpdateWindowBounds();
            if (Desktop.Renderer.Host is GameRenderHost<Game1> Host)
                Host.Game.Window.ClientSizeChanged += (sender, e) => UpdateWindowBounds();

            MaxHP = 120;
            CurrentHP = 80;
            MaxMP = 90;
            CurrentMP = 40;
            MaxStamina = 100;
            CurrentStamina = 90;
            NextLevelXP = 26252;
            CurrentXP = 7500;

            ActiveBuffs = new()
            {
                new(Desktop, "Deathtouch", TimeSpan.FromMinutes(10.0), Desktop.NamedRegions["Buff_Deathtouch"], "+5% insta-kill"),
                new(Desktop, "Bone Plating", TimeSpan.FromMinutes(15.0), Desktop.NamedRegions["Buff_BonePlating"], "+25% DEF"),
                new(Desktop, "Precision", TimeSpan.FromMinutes(8.0), Desktop.NamedRegions["Buff_Precision"], "+15% ACC"),
                new(Desktop, "Voidguard", TimeSpan.FromMinutes(5.0), Desktop.NamedRegions["Buff_Voidguard"], "+25% M.DEF"),
                new(Desktop, "Reflect", TimeSpan.FromMinutes(5.0), Desktop.NamedRegions["Buff_Reflect"], "+20% Dmg Reflect"),
                new(Desktop, "Lucky", TimeSpan.FromMinutes(20.0), Desktop.NamedRegions["Diamond"], "+25% Rare item drop rate")
            };

            MGListBox<PlayerBuff> BuffsList = Window.GetElementByName<MGListBox<PlayerBuff>>("BuffsList");
            BuffsList.SetItemsSource(ActiveBuffs);

            Window.WindowDataContext = this;
        }

        private void UpdateWindowBounds()
        {
            Window.Left = Desktop.ValidScreenBounds.Left;
            Window.Top = Desktop.ValidScreenBounds.Top;
            Window.WindowWidth = Desktop.ValidScreenBounds.Width;
            Window.WindowHeight = Desktop.ValidScreenBounds.Height;
        }
    }

    public class PlayerBuff : ViewModelBase
    {
        private MGDesktop Desktop { get; }
        public string Name { get; }
        public TimeSpan RemainingDuration { get; }
        public NamedTextureRegion Icon { get; }
        public Rectangle? IconSourceRect => Icon.SourceRect;
        public Texture2D IconTexture => Desktop.NamedTextures[Icon.TextureName];
        public string Description { get; }

        public PlayerBuff(MGDesktop Desktop, string Name, TimeSpan Duration, NamedTextureRegion Icon, string Description)
        {
            this.Desktop = Desktop;
            this.Name = Name;
            this.RemainingDuration = Duration;
            this.Icon = Icon;
            this.Description = Description;
        }
    }
}
