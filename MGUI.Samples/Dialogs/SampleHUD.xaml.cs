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
using MGUI.Core.UI.Containers.Grids;
using MonoGame.Extended;
using MGUI.Shared.Text;
using MGUI.Core.UI.XAML;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Thickness = MonoGame.Extended.Thickness;

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

        public PlayerToolBar ToolBar { get; }
        private MGUniformGrid UIToolBar { get; }

        private static void InitializeResources(ContentManager Content, MGDesktop Desktop)
        {
            MGResources Resources = Desktop.Resources;

            Texture2D AngryMeteor = Resources.Textures["AngryMeteor"].Texture;

            int TextureTopMargin = 6;
            int TextureSpacing = 1;
            int TextureIconSize = 16;

            List<(string Name, int Row, int Column)> Icons = new()
            {
                ("ToolBar_Dagger", 4, 5),
                ("ToolBar_PickaxeShovel", 5, 9),
                ("ToolBar_Backpack", 1, 11),
                ("ToolBar_Medkit", 0, 6)
            };

            foreach (var (Name, Row, Column) in Icons)
            {
                Rectangle SourceRect = new(Column * (TextureIconSize + TextureSpacing), TextureTopMargin + Row * (TextureIconSize + TextureSpacing), TextureIconSize, TextureIconSize);
                Resources.AddTexture(Name, new MGTextureData(AngryMeteor, SourceRect));
            }

            TextureSpacing = 1;
            TextureTopMargin = 166;
            TextureIconSize = 12;

            Icons = new()
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
                Resources.AddTexture(Name, new MGTextureData(AngryMeteor, SourceRect));
            }
        }

        public SampleHUD(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Dialogs)}", $"{nameof(SampleHUD)}.xaml", () => InitializeResources(Content, Desktop))
        {
            UpdateWindowBounds();
            if (Desktop.Renderer.Host is GameRenderHost<Game1> Host)
                Host.Game.Window.ClientSizeChanged += (sender, e) => UpdateWindowBounds();

            MGResources Resources = Desktop.Resources;

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
                new(Desktop, "Deathtouch", TimeSpan.FromMinutes(1.0), Resources.Textures["Buff_Deathtouch"], "+5% insta-kill"),
                new(Desktop, "Bone Plating", TimeSpan.FromMinutes(15.0), Resources.Textures["Buff_BonePlating"], "+25% DEF"),
                new(Desktop, "Precision", TimeSpan.FromMinutes(8.0), Resources.Textures["Buff_Precision"], "+15% ACC"),
                new(Desktop, "Voidguard", TimeSpan.FromMinutes(5.0), Resources.Textures["Buff_Voidguard"], "+25% M.DEF"),
                new(Desktop, "Reflect", TimeSpan.FromMinutes(5.0), Resources.Textures["Buff_Reflect"], "+20% Dmg Reflect"),
                new(Desktop, "Lucky", TimeSpan.FromMinutes(20.0), Resources.Textures["Diamond"], "+25% Rare item drop rate")
            };

            MGListBox<PlayerBuff> BuffsList = Window.GetElementByName<MGListBox<PlayerBuff>>("BuffsList");
            BuffsList.SetItemsSource(ActiveBuffs);

            //  Remove buffs from the list when they expire
            //  (Since the ItemsSource is an ObservableCollection, the UI will automatically update to remove from the list when the bound collection changes)
            void OnBuffExpired(object Sender, PlayerBuff Buff)
            {
                ActiveBuffs.Remove(Buff);
                Buff.OnExpired -= OnBuffExpired;
            }
            foreach (PlayerBuff Buff in ActiveBuffs)
            {
                Buff.OnExpired += OnBuffExpired;
            }

            this.ToolBar = new(Desktop, 10);

            //  Create a few sample items and add them to the toolbar
            Item Dagger = new(Desktop, "Dagger", "A small blade.", Resources.Textures["ToolBar_Dagger"]);
            Item PickaxeShovel = new(Desktop, "Pickaxe and Shovel", "A set of useful tools.", Resources.Textures["ToolBar_PickaxeShovel"]);
            Item Backpack = new(Desktop, "Knapsack", "A small pouch (Can hold up to 6 items).", Resources.Textures["ToolBar_Backpack"]);
            Item Medkit = new(Desktop, "First-Aid kit", "Restores 50 HP", Resources.Textures["ToolBar_Medkit"]);
            Item Diamond = new(Desktop, "Diamond", "A rare gem.", Resources.Textures["Diamond"]);
            ToolBar.Slots[0].Item = new(Dagger, 1);
            ToolBar.Slots[1].Item = new(PickaxeShovel, 1);
            ToolBar.Slots[2].Item = new(Backpack, 1);
            ToolBar.Slots[6].Item = new(Medkit, 2);
            ToolBar.Slots[9].Item = new(Diamond, 5);

            this.UIToolBar = Window.GetElementByName<MGUniformGrid>("UniformGrid_ToolBar");

            UIToolBar.SelectionChanged += (sender, e) =>
            {
                if (!e.HasValue)
                    ToolBar.SelectedSlot = null;
                else
                {
                    int Index = e.Value.Cell.Row * UIToolBar.Columns + e.Value.Cell.Column;
                    ToolBarSlot SelectedSlot = ToolBar.Slots[Index];
                    SelectedSlot.IsSelected = true;
                }
            };

            //  Apply custom drawing logic to each Cell in the ToolBar
            UIToolBar.OnRenderCell += (sender, e) =>
            {
                DrawTransaction DT = e.DrawArgs.DT;

                Rectangle ActualCellBounds = e.CellBounds.GetTranslated(e.DrawArgs.Offset);

                int Index = e.CellIndex.Row * UIToolBar.Columns + e.CellIndex.Column;
                ToolBarSlot Slot = ToolBar.Slots[Index];

                //  Draw the item in this slot
                if (Slot.Item != null)
                {
                    const int SlotBorderSize = 3;
                    const int SlotPadding = 5;
                    Rectangle ItemBounds = ActualCellBounds.GetCompressed(SlotBorderSize + SlotPadding);

                    Resources.TryDrawTexture(DT, Slot.Item.Item.Icon, ItemBounds, e.DrawArgs.Opacity);

                    //  Draw the quantity in the bottom-right corner
                    if (Slot.Item.Quantity > 1)
                    {
                        const string FontFamily = "Arial";
                        const CustomFontStyles FontStyle = CustomFontStyles.Bold;
                        const int FontSize = 11;

                        string Text = Slot.Item.Quantity.ToString();
                        Vector2 TextSize = DT.MeasureText(FontFamily, FontStyle, Text, FontSize);
                        Vector2 Position = ActualCellBounds.GetCompressed(SlotBorderSize).BottomRight().ToVector2().Translate(-TextSize.X - 1, -TextSize.Y + 1);

                        DT.DrawShadowedText(FontFamily, FontStyle, Text, Position, Color.White, new Color(40,40,40) * e.DrawArgs.Opacity, FontSize);
                    }
                }

                //  Draw a yellow border around the selected cell
                if (Slot.IsSelected)
                {
                    DT.StrokeRectangle(e.DrawArgs.Offset.ToVector2(), e.CellBounds, Color.Yellow * 0.8f * e.DrawArgs.Opacity, new Thickness(3));
                }
            };

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
        public string Description { get; }

        public MGTextureData Icon { get; }
        public Texture2D IconTexture => Icon.Texture;
        public Rectangle? IconSourceRect => Icon.SourceRect;

        public TimeSpan TotalDuration { get; }

        private TimeSpan _RemainingDuration;
        public TimeSpan RemainingDuration
        {
            get => _RemainingDuration;
            set
            {
                if (_RemainingDuration != value)
                {
                    TimeSpan Previous = _RemainingDuration;
                    _RemainingDuration = value;
                    NPC(nameof(RemainingDuration));
                    if (Previous > TimeSpan.Zero && RemainingDuration <= TimeSpan.Zero)
                    {
                        OnExpired?.Invoke(this, this);
                    }
                }
            }
        }
        
        /// <summary>Invoked when <see cref="RemainingDuration"/> changes from > 0 to <= 0.<br/>
        /// This event is not repeatedly invoked if the duration continues to tick down to smaller negative values.</summary>
        public event EventHandler<PlayerBuff> OnExpired;

        public PlayerBuff(MGDesktop Desktop, string Name, TimeSpan Duration, MGTextureData Icon, string Description)
        {
            this.Desktop = Desktop;
            this.Name = Name;
            this.TotalDuration = Duration;
            this.RemainingDuration = Duration;
            this.Icon = Icon;
            this.Description = Description;
        }
    }

    public class PlayerToolBar : ViewModelBase
    {
        public MGDesktop Desktop { get; }

        public ReadOnlyCollection<ToolBarSlot> Slots { get; }
        public int NumSlots => Slots.Count;

        private ToolBarSlot _SelectedSlot;
        public ToolBarSlot SelectedSlot
        {
            get => _SelectedSlot;
            set
            {
                if (_SelectedSlot != value)
                {
                    ToolBarSlot Previous = SelectedSlot;
                    _SelectedSlot = value;
                    NPC(nameof(SelectedSlot));
                    Previous?.NPC(nameof(ToolBarSlot.IsSelected));
                    SelectedSlot?.NPC(nameof(ToolBarSlot.IsSelected));
                }
            }
        }

        public PlayerToolBar(MGDesktop Desktop, int Size)
        {
            this.Desktop = Desktop;
            this.Slots = Enumerable.Range(0, Size).Select(x => new ToolBarSlot(this)).ToList().AsReadOnly();
        }
    }

    public class ToolBarSlot : ViewModelBase
    {
        public PlayerToolBar ToolBar { get; }
        public int Index => ToolBar.Slots.IndexOf(this);

        public bool IsSelected
        {
            get => ToolBar.SelectedSlot == this;
            set
            {
                if (value)
                    ToolBar.SelectedSlot = this;
                else if (!value && IsSelected)
                    ToolBar.SelectedSlot = null;
            }
        }

        private PlayerItem _Item;
        public PlayerItem Item
        {
            get => _Item;
            set
            {
                if (_Item != value)
                {
                    _Item = value;
                    NPC(nameof(Item));
                }
            }
        }

        public ToolBarSlot(PlayerToolBar ToolBar)
        {
            this.ToolBar = ToolBar;
        }
    }

    public readonly record struct Item(MGDesktop Desktop, string Name, string Description, MGTextureData Icon)
    {
        public Texture2D IconTexture => Icon.Texture;
        public Rectangle? IconSourceRect => Icon.SourceRect;
    }

    public class PlayerItem : ViewModelBase
    {
        public Item Item { get; }

        private int _Quantity;
        public int Quantity
        {
            get => _Quantity;
            set
            {
                if (_Quantity != value)
                {
                    _Quantity = value;
                    NPC(nameof(Quantity));
                }
            }
        }

        public PlayerItem(Item Item, int Quantity)
        {
            this.Item = Item;
            this.Quantity = Quantity;
        }
    }
}
