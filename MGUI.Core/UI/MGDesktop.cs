using Microsoft.Xna.Framework;
using MGUI.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoGame.Extended;
using MGUI.Shared.Input.Mouse;
using MGUI.Shared.Input.Keyboard;
using MGUI.Shared.Input;
using MGUI.Shared.Text;
using MGUI.Shared.Rendering;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;
using System.IO;

namespace MGUI.Core.UI
{
    public readonly record struct NamedTextureRegion(string TextureName, string RegionName, Rectangle? SourceRect, Color? Color);

    /// <summary>Represents a Rectanglular screen bounds that you can add or remove <see cref="MGWindow"/>s to/from, 
    /// and handles mutual exclusion with things like input handling or ensuring there is only 1 <see cref="MGToolTip"/> or <see cref="MGContextMenu"/> on the user interface at a time.</summary>
    public class MGDesktop : IMouseHandlerHost, IKeyboardHandlerHost, IContextMenuHost
    {
        public MainRenderer Renderer { get; }
        public InputTracker InputTracker => Renderer.Input;
        public FontManager FontManager => Renderer.FontManager;

        /// <summary>A <see cref="MouseHandler"/> that is updated at the start of <see cref="Update()"/>, before any <see cref="MGWindow"/>s in <see cref="Windows"/> are updated.<br/>
        /// Objects that subscribe to this handler's mouse events will be the very first to receive and handle the event.<para/>
        /// Highly recommended to avoid using this unless absolutely necessary, and if you do use it, you probably shouldn't call e.SetHandled(...) so other elements can still receive the input.</summary>
        public MouseHandler HighPriorityMouseHandler { get; }
        /// <summary>A <see cref="KeyboardHandler"/> that is updated at the start of <see cref="Update()"/>, before any <see cref="MGWindow"/>s in <see cref="Windows"/> are updated.<br/>
        /// Objects that subscribe to this handler's keyboard events will be the very first to receive and handle the event.<para/>
        /// Highly recommended to avoid using this unless absolutely necessary, and if you do use it, you probably shouldn't call e.SetHandled(...) so other elements can still receive the input.</summary>
        public KeyboardHandler HighPriorityKeyboardHandler { get; }

        bool IMouseViewport.IsInside(Vector2 Position) => ValidScreenBounds.ContainsInclusive(Position);
        Vector2 IMouseViewport.GetOffset() => Vector2.Zero;

        #region ToolTip
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private MGToolTip _ActiveToolTip;
        public MGToolTip ActiveToolTip
        {
            get => _ActiveToolTip;
            private set
            {
                if (_ActiveToolTip != value)
                {
                    bool Cancellable = true;
                    if (ActiveToolTip != null && value != null && ActiveToolTip.Host == value.Host)
                        Cancellable = false;

                    if (Cancellable)
                    {
                        CancelEventArgs<MGToolTip> e = new(value);
                        ToolTipOpening?.Invoke(this, e);
                        if (e.Cancel)
                            return;
                    }

                    if (ActiveToolTip != null)
                    {
                        ToolTipClosed?.Invoke(this, ActiveToolTip);
                        ActiveToolTip.Host.ToolTipChanged -= Host_ToolTipChanged;
                    }

                    _ActiveToolTip = value;

                    if (ActiveToolTip != null)
                    {
                        ToolTipOpened?.Invoke(this, ActiveToolTip);
                        ActiveToolTip.Host.ToolTipChanged += Host_ToolTipChanged;
                    }
                }
            }
        }

        private void Host_ToolTipChanged(object sender, EventArgs<MGToolTip> e)
        {
            ActiveToolTip = e.NewValue;
        }

        public event EventHandler<MGToolTip> ToolTipClosed;
        public event EventHandler<CancelEventArgs<MGToolTip>> ToolTipOpening;
        public event EventHandler<MGToolTip> ToolTipOpened;

        internal MGToolTip QueuedToolTip { get; set; } = null;

        public static TimeSpan DefaultToolTipShowDelay = TimeSpan.FromSeconds(0.40);

        /// <summary>The amount of time that the mouse must hover a particular <see cref="MGElement"/> before its <see cref="MGElement.ToolTip"/> can be shown.<para/>
        /// Default value: <see cref="DefaultToolTipShowDelay"/></summary>
        public TimeSpan ToolTipShowDelay { get; set; }
        #endregion ToolTip

        #region Context Menu
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private MGContextMenu _ActiveContextMenu;
        /// <summary>The currently open <see cref="MGContextMenu"/>.<para/>
        /// To set this value, use <see cref="TryCloseActiveContextMenu"/> or <see cref="TryOpenContextMenu(MGContextMenu, Point)"/></summary>
        public MGContextMenu ActiveContextMenu { get => _ActiveContextMenu; }

        /// <returns>True if there was no <see cref="ActiveContextMenu"/> or it was successfully closed.<br/>
        /// False if the action was cancelled such as via <see cref="ContextMenuOpening"/>'s <see cref="ContextMenuOpeningClosingEventArgs"/>.Cancel.</returns>
        public bool TryCloseActiveContextMenu()
        {
            if (ActiveContextMenu != null)
            {
                //  Close nested menus
                if (!ActiveContextMenu.TryCloseActiveContextMenu())
                    return false;

                MGContextMenu Previous = ActiveContextMenu;

                if (ContextMenuClosing != null)
                {
                    ContextMenuOpeningClosingEventArgs ClosingArgs = new(ActiveContextMenu, null);
                    ContextMenuClosing.Invoke(this, ClosingArgs);
                    if (ClosingArgs.Cancel)
                        return false;
                }

                ActiveContextMenu.InvokeContextMenuClosing();

                _ActiveContextMenu = null;

                Previous.InvokeContextMenuClosed();
                ContextMenuClosed?.Invoke(this, Previous);

                return true;
            }
            else
                return true;
        }

        /// <returns>True if the <paramref name="Menu"/> was already opened, or was successfully opened.<br/>
        /// False if the action was cancelled (such as via <see cref="ContextMenuOpeningClosingEventArgs"/>.Cancel while trying to close the current menu, or while trying to open the new menu), or because <see cref="MGContextMenu.CanContextMenuOpen"/> is false.</returns>
        public bool TryOpenContextMenu(MGContextMenu Menu, Rectangle Anchor)
        {
            if (!TryCloseActiveContextMenu())
                return false;

            if (Menu == null || !Menu.CanContextMenuOpen)
                return false;

            Rectangle ValidBounds = ValidScreenBounds;
            if (Menu.IsContextMenuOpen)
            {
                Size MenuSizeScreenSpace = new((int)(Menu.RenderBounds.Width * Menu.Scale), (int)(Menu.RenderBounds.Height * Menu.Scale));
                Point NewPosition = MGContextMenu.FitMenuToViewport(Anchor, MenuSizeScreenSpace, ValidBounds).TopLeft();
                Menu.Left = NewPosition.X;
                Menu.Top = NewPosition.Y;
                Menu.ValidateWindowSizeAndPosition();
                return true;
            }
            else
            {
                if (ContextMenuOpening != null)
                {
                    ContextMenuOpeningClosingEventArgs OpeningArgs = new(ActiveContextMenu, Menu);
                    ContextMenuOpening.Invoke(this, OpeningArgs);
                    if (OpeningArgs.Cancel)
                        return false;
                }

                Menu.InvokeContextMenuOpening();

                _ActiveContextMenu = Menu;

                int MinWidth = 100;
                int MinHeight = 40;
                int MaxWidth = 1000;
                int MaxHeight = 800;

                Size MenuSizeUnscaledScreenSpace = Menu.ComputeContentSize(MinWidth, MinHeight, MaxWidth, MaxHeight);
                Size MenuSizeScreenSpace = new((int)(MenuSizeUnscaledScreenSpace.Width * Menu.Scale), (int)(MenuSizeUnscaledScreenSpace.Height * Menu.Scale));

                Point Position = MGContextMenu.FitMenuToViewport(Anchor, MenuSizeScreenSpace, ValidBounds).TopLeft();
                Menu.TopLeft = Position;
                _ = Menu.ApplySizeToContent(SizeToContent.WidthAndHeight, MinWidth, MinHeight, MaxWidth, MaxHeight, true);

                ActiveContextMenu.InvokeContextMenuOpened();
                ContextMenuOpened?.Invoke(this, Menu);

                return true;
            }
        }

        /// <returns>True if the <paramref name="Menu"/> was already opened, or was successfully opened.<br/>
        /// False if the action was cancelled (such as via <see cref="ContextMenuOpeningClosingEventArgs"/>.Cancel while trying to close the current menu, or while trying to open the new menu), or because <see cref="MGContextMenu.CanContextMenuOpen"/> is false.</returns>
        public bool TryOpenContextMenu(MGContextMenu Menu, Point Position) => TryOpenContextMenu(Menu, new Rectangle(Position.X, Position.Y, 1, 1));

        /// <summary>Invoked just before an <see cref="MGContextMenu"/> is opened. Allows cancellation.</summary>
        public event EventHandler<ContextMenuOpeningClosingEventArgs> ContextMenuOpening;
        /// <summary>Invoked immediately after an <see cref="MGContextMenu"/> is opened.</summary>
        public event EventHandler<MGContextMenu> ContextMenuOpened;
        /// <summary>Invoked just before an <see cref="MGContextMenu"/> is closed. Allows cancellation.</summary>
        public event EventHandler<ContextMenuOpeningClosingEventArgs> ContextMenuClosing;
        /// <summary>Invoked immediately after an <see cref="MGContextMenu"/> is closed.</summary>
        public event EventHandler<MGContextMenu> ContextMenuClosed;
        #endregion Context Menu

        #region Windows
        /// <summary>The last element represents the <see cref="MGWindow"/> that will be drawn last (I.E., rendered overtop of everything else), and 
        /// updated first (I.E., has the first chance to handle inputs)</summary>
        public List<MGWindow> Windows { get; }

        /// <summary>Moves the given <paramref name="Window"/> to the end of <see cref="Windows"/> list. It will be rendered overtop of all other <see cref="Windows"/> and have first chance at receiving/handling input.</summary>
        /// <returns>True if the <paramref name="Window"/> was brought to the front. False if it was not a valid element in <see cref="Windows"/>.</returns>
        public bool BringToFront(MGWindow Window)
        {
            if (!Windows.Contains(Window))
            {
                return false;
            }
            else
            {
                if (Windows.IndexOf(Window) != Windows.Count - 1)
                {
                    Windows.Remove(Window);
                    Windows.Add(Window);
                }
                return true;
            }
        }

        /// <summary>Moves the given <paramref name="Window"/> to the start of <see cref="Windows"/> list. It will be rendered underneath of all other <see cref="Windows"/></summary>
        /// <returns>True if the <paramref name="Window"/> was moved to the back. False if it was not a valid element in <see cref="Windows"/>.</returns>
        public bool BringToBack(MGWindow Window)
        {
            if (!Windows.Contains(Window))
            {
                return false;
            }
            else
            {
                if (Windows.IndexOf(Window) != 0)
                {
                    Windows.Remove(Window);
                    Windows.Insert(0, Window);
                }
                return true;
            }
        }
        #endregion Windows

        internal MGElement QueuedFocusedKeyboardHandler { get; set; } = null;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private MGElement _FocusedKeyboardHandler;
        /// <summary>The <see cref="MGElement"/> that should handle Keyboard inputs, if any.<para/>
        /// Only <see cref="MGElement"/>'s where <see cref="MGElement.CanHandleKeyboardInput"/> is true can be set as the <see cref="FocusedKeyboardHandler"/></summary>
        public MGElement FocusedKeyboardHandler
        {
            get => _FocusedKeyboardHandler;
            private set
            {
                if (_FocusedKeyboardHandler != value)
                {
                    if (value != null && !value.CanHandleKeyboardInput)
                        throw new InvalidOperationException($"{nameof(MGWindow)}.{nameof(FocusedKeyboardHandler)} cannot be set to an value with {nameof(MGElement)}.{nameof(MGElement.CanHandleKeyboardInput)}=false.");

                    MGElement Previous = FocusedKeyboardHandler;
                    if (Previous is MGTextBox PreviousTextBox)
                        PreviousTextBox.ReadonlyChanged -= TextBox_ReadonlyChanged;

                    _FocusedKeyboardHandler = value;

                    if (FocusedKeyboardHandler is MGTextBox CurrentTextBox)
                        CurrentTextBox.ReadonlyChanged += TextBox_ReadonlyChanged;

                    FocusedKeyboardHandlerChanged?.Invoke(this, new(Previous, FocusedKeyboardHandler));
                }
            }
        }

        private void TextBox_ReadonlyChanged(object sender, bool IsReadonly)
        {
            if (sender is MGTextBox TextBox && IsReadonly && FocusedKeyboardHandler == TextBox)
                FocusedKeyboardHandler = null;
        }

        public event EventHandler<EventArgs<MGElement>> FocusedKeyboardHandlerChanged;

        /// <summary>Represents the screen space that can be occupied with <see cref="MGElement"/>s.<para/>
        /// For example, an <see cref="MGContextMenu"/> will attempt to position itself such that it is not rendered outside of these bounds.</summary>
        public Rectangle ValidScreenBounds => Renderer.Host.GetBounds();

        public MGTheme Theme { get; }

        #region Named Textures / Regions
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Dictionary<string, Texture2D> _NamedTextures { get; }
        /// <summary>This dictionary is commonly used by <see cref="MGImage"/> to reference textures by a string key value.<para/>
        /// See also:<br/><see cref="AddNamedTexture(string, Texture2D)"/><br/><see cref="RemoveNamedTexture(string)"/><br/><see cref="NamedRegions"/></summary>
        public IReadOnlyDictionary<string, Texture2D> NamedTextures => _NamedTextures;

        public void AddNamedTexture(string Name, Texture2D Texture) => _NamedTextures.Add(Name, Texture);
        public void RemoveNamedTexture(string Name)
        {
            _NamedTextures.Remove(Name);
            List<NamedTextureRegion> DerivedRegions = _NamedRegions.Values.Where(x => x.TextureName == Name).ToList();
            foreach (NamedTextureRegion Region in DerivedRegions)
                RemoveNamedRegion(Region.RegionName);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Dictionary<string, NamedTextureRegion> _NamedRegions { get; }
        /// <summary>This dictionary is commonly used by <see cref="MGImage"/> to reference textures and various other texture settings by a string key value.<para/>
        /// See also:<br/><see cref="AddNamedRegion(NamedTextureRegion)"/><br/><see cref="RemoveNamedRegion(string)"/><br/><see cref="NamedTextures"/></summary>
        public IReadOnlyDictionary<string, NamedTextureRegion> NamedRegions => _NamedRegions;

        /// <param name="Region">Recommended to avoid <see cref="NamedTextureRegion.RegionName"/>s containing <see cref="Text.FTTokenizer.CloseTagChar"/>,<br/>
        /// since the parsing logic in <see cref="Text.FTTokenizer.Tokenize(string, bool)"/> isn't robust enough to handle such cases unambiguously.</param>
        public void AddNamedRegion(NamedTextureRegion Region)
        {
            if (!_NamedTextures.ContainsKey(Region.TextureName))
                throw new InvalidOperationException($"{nameof(MGDesktop)}.{nameof(NamedTextures)} does not contain an entry for {nameof(NamedTextureRegion.TextureName)}={Region.TextureName}");
            _NamedRegions.Add(Region.RegionName, Region);
        }
        public void RemoveNamedRegion(string RegionName) => _NamedRegions.Remove(RegionName);

        public bool TryDrawNamedRegion(DrawTransaction DT, string RegionName, Point Position, int? Width, int? Height, float Opacity = 1.0f)
        {
            if (_NamedRegions.TryGetValue(RegionName, out NamedTextureRegion Region) && _NamedTextures.TryGetValue(Region.TextureName, out Texture2D Texture))
            {
                int ActualWidth = Width ?? Texture.Width;
                int ActualHeight = Height ?? Texture.Height;
                Rectangle Destination = new(Position.X, Position.Y, ActualWidth, ActualHeight);

                DT.DrawTextureTo(Texture, Region.SourceRect, Destination, (Region.Color ?? Color.White) * Opacity);

                return true;
            }
            else
                return false;
        }
        #endregion Named Textures / Regions

        public MGDesktop(MainRenderer Renderer)
        {
            this.Renderer = Renderer;
            this.Windows = new();
            this.Theme = new(Renderer.FontManager.DefaultFontFamily);

            this._NamedTextures = new();
            this._NamedRegions = new();

            #region Sample Icons
            Texture2D AngryMeteor_MilitaryIconsSet = Renderer.Content.Load<Texture2D>(Path.Combine("Icons", "AngryMeteor_MilitaryIconsSet"));
            _NamedTextures.Add("AngryMeteor", AngryMeteor_MilitaryIconsSet);

            int TextureTopMargin = 6;
            int TextureSpacing = 1;
            int TextureIconSize = 16;
            List<(string Name, int Row, int Column)> Icons = new()
            {
                ("ArrowRightGreen", 0, 0),
                ("ArrowDownGreen", 0, 1),
                ("ArrowLeftGreen", 1, 0),
                ("ArrowUpGreen", 1, 1),

                ("GoldBullion", 1, 4),
                ("SilverBullion", 1, 5),
                ("BronzeBullion", 1, 6),

                ("SkullOpen", 1, 7),
                ("SkullClosed", 1, 8),
                ("SkullAndCrossbones", 4, 3),

                ("Wrench", 1, 10),
                ("Gear", 8, 3),

                ("Backpack", 1, 11),

                ("Diamond", 2, 9),
                ("Emerald", 2, 10),
                ("Ruby", 2, 11),

                ("GoldMedal", 2, 4),
                ("SilverMedal", 2, 5),
                ("BronzeMedal", 2, 6),

                ("Delete", 5, 3),
                ("CheckMarkGreen", 5, 4),

                ("Computer", 4, 10),
                ("Save", 4, 11)
            };
            foreach (var (Name, Row, Column) in Icons)
            {
                Rectangle SourceRect = new(Column * (TextureIconSize + TextureSpacing), TextureTopMargin + Row * (TextureIconSize + TextureSpacing), TextureIconSize, TextureIconSize);
                AddNamedRegion(new("AngryMeteor", Name, SourceRect, null));
            }
            #endregion Sample Icons

            this.ToolTipShowDelay = DefaultToolTipShowDelay;

            this.HighPriorityMouseHandler = InputTracker.Mouse.CreateHandler(this, null);
            this.HighPriorityKeyboardHandler = InputTracker.Keyboard.CreateHandler(this, null);

            HighPriorityMouseHandler.PressedInside += (sender, e) => { QueuedFocusedKeyboardHandler = null; };
            HighPriorityMouseHandler.PressedOutside += (sender, e) => { QueuedFocusedKeyboardHandler = null; };
        }

        public void Update()
        {
            UpdateBaseArgs BA = Renderer.UpdateArgs;

            HighPriorityMouseHandler.ManualUpdate();
            HighPriorityKeyboardHandler.ManualUpdate();

            QueuedToolTip = null;

            ElementUpdateArgs UA = new(BA, true, false, true, Point.Zero, ValidScreenBounds);

            ActiveContextMenu?.Update(UA);
            ActiveToolTip?.Update(UA with { IsHitTestVisible = ActiveToolTip.ParentWindow.IsHitTestVisible });

            foreach (MGWindow Window in Windows.Reverse<MGWindow>())
            {
                Window.Update(UA);
            }

            this.ActiveToolTip = QueuedToolTip;
            this.FocusedKeyboardHandler = QueuedFocusedKeyboardHandler;
        }

        public void Draw(DrawTransaction DT, float Opacity = 1.0f)
        {
            DrawBaseArgs BA = new(Renderer.UpdateArgs.TotalElapsed, DT, Opacity);
            ElementDrawArgs DA = new(BA, new VisualState(PrimaryVisualState.Normal, SecondaryVisualState.None), Point.Zero);

            Rectangle ScreenBounds = ValidScreenBounds;
            if (!BA.DT.CurrentSettings.RasterizerState.ScissorTestEnable || ScreenBounds.Intersects(BA.DT.GD.ScissorRectangle))
            {
                using (BA.DT.SetClipTargetTemporary(ScreenBounds, true))
                {
                    foreach (MGWindow Window in Windows)
                    {
                        Window.Draw(DA);
                    }

                    //  The ToolTip only takes priority if it is a ToolTip belonging to the current ContextMenu
                    if (ActiveToolTip != null && ActiveContextMenu != null && ActiveToolTip.ParentWindow == ActiveContextMenu)
                    {
                        ActiveContextMenu?.Draw(DA);
                        ActiveToolTip?.DrawAtMousePosition(DA);
                    }
                    else
                    {
                        ActiveToolTip?.DrawAtMousePosition(DA);
                        ActiveContextMenu?.Draw(DA);
                    }
                }
            }
        }

        /// <param name="InitialDrawSettings">If null, uses <see cref="DrawSettings.Default"/></param>
        public void Draw(float Opacity = 1.0f, DrawSettings InitialDrawSettings = null)
        {
            using (DrawTransaction DT = new(Renderer, InitialDrawSettings ?? DrawSettings.Default, false))
            {
                Draw(DT, Opacity);
            }
        }
    }
}
