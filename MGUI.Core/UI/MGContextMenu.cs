using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MGUI.Shared.Helpers;
using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.Containers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI
{
    /// <summary>For concrete implementations, see also:<para/>
    /// <see cref="MGDesktop"/> (For root-level <see cref="MGContextMenu"/>)<br/>
    /// <see cref="MGContextMenu"/> (For nested <see cref="MGContextMenu"/>s)</summary>
    public interface IContextMenuHost
    {
        public MGContextMenu ActiveContextMenu { get; }
        public bool TryCloseActiveContextMenu();

        public bool TryOpenContextMenu(MGContextMenu Menu, Rectangle Anchor);
        public bool TryOpenContextMenu(MGContextMenu Menu, Point Position)
            => TryOpenContextMenu(Menu, new Rectangle(Position.X, Position.Y, 1, 1));
    }

    public class ContextMenuOpeningClosingEventArgs : CancelEventArgs
    {
        public MGContextMenu CurrentValue { get; }
        public MGContextMenu NewValue { get; }

        public ContextMenuOpeningClosingEventArgs(MGContextMenu CurrentValue, MGContextMenu NewValue)
        {
            this.CurrentValue = CurrentValue;
            this.NewValue = NewValue;
        }
    }

    public class MGContextMenu : MGWindow, IContextMenuHost
    {
        public static Rectangle FitMenuToViewport(Rectangle Anchor, Size Size, Rectangle Viewport)
        {
            int ActualX = Anchor.Right;
            if (ActualX + Size.Width > Viewport.Right)
                ActualX = Math.Max(Viewport.Left, Anchor.Left - Size.Width);

            int ActualY = Anchor.Top;
            if (ActualY + Size.Height > Viewport.Bottom)
                ActualY = Math.Max(Viewport.Top, Viewport.Bottom - Size.Height);

            Rectangle Bounds = new(ActualX, ActualY, Math.Min(Size.Width, Viewport.Width), Math.Min(Size.Height, Viewport.Height));
            return Bounds;
        }

        #region Open / Close
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _CanContextMenuOpen;
        /// <summary>True if this <see cref="MGContextMenu"/> can be shown.</summary>
        public bool CanContextMenuOpen
        {
            get => _CanContextMenuOpen;
            set
            {
                if (_CanContextMenuOpen != value)
                {
                    _CanContextMenuOpen = value;
                    NPC(nameof(CanContextMenuOpen));
                }
            }
        }

        /// <summary>True if this <see cref="MGContextMenu"/> is currently being shown. To open this <see cref="MGContextMenu"/>, use <see cref="Host"/>'s <see cref="IContextMenuHost.TryOpenContextMenu(MGContextMenu, Point)"/>.<para/>
        /// See also: <see cref="MGDesktop.ActiveContextMenu"/>,  <see cref="MGContextMenu.ActiveContextMenu"/></summary>
        public bool IsContextMenuOpen { get => Host.ActiveContextMenu == this; }

        /// <summary>See also: <see cref="IContextMenuHost.TryOpenContextMenu(MGContextMenu, Point)"/><br/>(<see cref="MGContextMenu"/> and <see cref="MGDesktop"/> are implementations of <see cref="IContextMenuHost"/>)</summary>
        /// <returns>True if this <see cref="MGContextMenu"/> wasn't already opened, and now is opened.<br/>
        /// False if unable to open, such as if it was already open, closing the current menu was cancelled, opening this menu was cancelled, or <see cref="CanContextMenuOpen"/> is false.</returns>
        public bool TryOpenContextMenu(Rectangle Anchor) => !IsContextMenuOpen && Host.TryOpenContextMenu(this, Anchor);
        /// <summary>See also: <see cref="IContextMenuHost.TryOpenContextMenu(MGContextMenu, Point)"/><br/>(<see cref="MGContextMenu"/> and <see cref="MGDesktop"/> are implementations of <see cref="IContextMenuHost"/>)</summary>
        /// <returns>True if this <see cref="MGContextMenu"/> wasn't already opened, and now is opened.<br/>
        /// False if unable to open, such as if it was already open, closing the current menu was cancelled, opening this menu was cancelled, or <see cref="CanContextMenuOpen"/> is false.</returns>
        public bool TryOpenContextMenu(Point Position) => !IsContextMenuOpen && Host.TryOpenContextMenu(this, Position);

        /// <summary>See also: <see cref="MGDesktop.TryCloseActiveContextMenu"/></summary>
        /// <returns>True if this <see cref="MGContextMenu"/> was open, and then was successfully closed.<br/>
        /// False if it wasn't already open, or the action was cancelled.</returns>
        public bool TryCloseContextMenu() => IsContextMenuOpen && Host.TryCloseActiveContextMenu();

        internal void InvokeContextMenuOpening() => ContextMenuOpening?.Invoke(this, EventArgs.Empty);
        internal void InvokeContextMenuOpened()
        {
            NPC(nameof(IsContextMenuOpen));
            ContextMenuOpened?.Invoke(this, EventArgs.Empty);
        }
        internal void InvokeContextMenuClosing() => ContextMenuClosing?.Invoke(this, EventArgs.Empty);
        internal void InvokeContextMenuClosed()
        {
            NPC(nameof(IsContextMenuOpen));
            ContextMenuClosed?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler<EventArgs> ContextMenuOpening;
        public event EventHandler<EventArgs> ContextMenuOpened;
        public event EventHandler<EventArgs> ContextMenuClosing;
        public event EventHandler<EventArgs> ContextMenuClosed;
        #endregion Open / Close

        #region Close Conditions
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _StaysOpenOnItemSelected;
        /// <summary>If true, this menu will not be automatically closed when the user clicks on a particular <see cref="MGContextMenuButton"/> to execute.<para/>
        /// Warning - Closing this menu may be cancelled, such as via <see cref="MGDesktop.ContextMenuClosing"/>'s 'Cancel' property.<para/>
        /// Default value: false<para/>See also: <see cref="StaysOpenOnItemToggled"/></summary>
        public bool StaysOpenOnItemSelected
        {
            get => _StaysOpenOnItemSelected;
            set
            {
                if (_StaysOpenOnItemSelected != value)
                {
                    _StaysOpenOnItemSelected = value;
                    NPC(nameof(StaysOpenOnItemSelected));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _StaysOpenOnItemToggled;
        /// <summary>If true, this menu will not be automatically closed when the user clicks on a particular <see cref="MGContextMenuToggle"/> to toggle.<para/>
        /// Warning - Closing this menu may be cancelled, such as via <see cref="MGDesktop.ContextMenuClosing"/>'s 'Cancel' property.<para/>
        /// Default value: true<para/>See also: <see cref="StaysOpenOnItemSelected"/></summary>
        public bool StaysOpenOnItemToggled
        {
            get => _StaysOpenOnItemToggled;
            set
            {
                if (_StaysOpenOnItemToggled != value)
                {
                    _StaysOpenOnItemToggled = value;
                    NPC(nameof(StaysOpenOnItemToggled));
                }
            }
        }

        public static float DefaultAutoCloseThreshold = 75;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private float? _AutoCloseThreshold;
        /// <summary>Only relevant if <see cref="IsSubmenu"/> is false.<br/>
        /// Determines how far away the mouse can move from <see cref="MGElement.LayoutBounds"/> before this menu is automatically closed.<para/>
        /// Warning - Closing this menu may be cancelled, such as via <see cref="MGDesktop.ContextMenuClosing"/>'s 'Cancel' property.<para/>
        /// Default value: <see cref="DefaultAutoCloseThreshold"/></summary>
        public float? AutoCloseThreshold
        {
            get => _AutoCloseThreshold;
            set
            {
                if (_AutoCloseThreshold != value)
                {
                    _AutoCloseThreshold = value;
                    NPC(nameof(AutoCloseThreshold));
                }
            }
        }
        #endregion Close Conditions

        public MGButton CreateDefaultDropdownButton(MGWindow Window)
        {
            MGButton Button = new(Window ?? this, new(0), MGUniformBorderBrush.Gray);

            Button.Padding = new(5, 3, 20, 3);
            Button.Margin = new(0);

            Button.HorizontalContentAlignment = HorizontalAlignment.Left;
            Button.VerticalContentAlignment = VerticalAlignment.Center;
            Button.HorizontalAlignment = HorizontalAlignment.Stretch;
            Button.VerticalAlignment = VerticalAlignment.Stretch;

            Button.BackgroundBrush = GetTheme().ComboBoxDropdownItemBackground.GetValue(true);

            return Button;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Func<MGWindow, MGButton> _ButtonWrapperTemplate;
        /// <summary>Every <see cref="MGContextMenuButton"/> and <see cref="MGContextMenuToggle"/> within <see cref="Items"/> will be automatically wrapped in an <see cref="MGButton"/> created by this function.<para/>
        /// Default value: <see cref="CreateDefaultDropdownButton"/></summary>
        public Func<MGWindow, MGButton> ButtonWrapperTemplate
        {
            get => _ButtonWrapperTemplate;
            set
            {
                if (_ButtonWrapperTemplate != value)
                {
                    _ButtonWrapperTemplate = value;
                    NPC(nameof(ButtonWrapperTemplate));
                    ButtonWrapperTemplateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler<EventArgs> ButtonWrapperTemplateChanged;

        #region Items
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ObservableCollection<MGContextMenuItem> _Items { get; }
        public IList<MGContextMenuItem> Items => _Items;

        /// <param name="Action">The action to invoke if the <see cref="MGContextMenuButton"/> is left-clicked.</param>
        public MGContextMenuButton AddButton(string Text, Action<MGContextMenuButton> Action)
            => AddButton(new MGTextBlock(this, Text, null, GetTheme().FontSettings.ContextMenuFontSize), Action);

        /// <param name="Action">The action to invoke if the <see cref="MGContextMenuButton"/> is left-clicked.</param>
        public MGContextMenuButton AddButton(MGElement Content, Action<MGContextMenuButton> Action)
        {
            MGContextMenuButton Button = new(this, Content, Action);
            _Items.Add(Button);
            return Button;
        }

        public MGContextMenuToggle AddCheckBox(string Text, bool IsChecked)
            => AddToggle(new MGTextBlock(this, Text, null, GetTheme().FontSettings.ContextMenuFontSize), IsChecked);

        public MGContextMenuToggle AddToggle(MGElement Content, bool IsChecked)
        {
            MGContextMenuToggle CheckBox = new(this, Content, IsChecked);
            _Items.Add(CheckBox);
            return CheckBox;
        }

        public MGContextMenuSeparator AddSeparator(int Height = 4)
        {
            MGContextMenuSeparator Separator = new(this, Height);
            _Items.Add(Separator);
            return Separator;
        }
        #endregion

        public MGScrollViewer ScrollViewerElement { get; }
        public MGStackPanel ItemsPanel { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Size _HeaderSize;
        /// <summary>The size of the header items that appear on the left edge of each <see cref="MGContextMenuItem"/>.<para/>
        /// For <see cref="MGContextMenuButton"/>s, this is either blank space or an icon.<br/>
        /// For <see cref="MGContextMenuToggle"/>s, this is a check mark.<para/>
        /// Default value: 14x14</summary>
        public Size HeaderSize
        {
            get => _HeaderSize;
            set
            {
                if (_HeaderSize != value)
                {
                    Size Previous = HeaderSize;
                    _HeaderSize = value;
                    NPC(nameof(HeaderSize));
                    HeaderSizeChanged?.Invoke(this, new(Previous, HeaderSize));
                }
            }
        }

        public event EventHandler<EventArgs<Size>> HeaderSizeChanged;

        #region Nested Menu
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private MGContextMenu _ActiveContextMenu;
        /// <summary>The currently open nested <see cref="MGContextMenu"/>.</summary>
        public MGContextMenu ActiveContextMenu { get => _ActiveContextMenu; }

        /// <returns>True if there was no <see cref="ActiveContextMenu"/> or it was successfully closed, false otherwise.</returns>
        public bool TryCloseActiveContextMenu()
        {
            if (ActiveContextMenu != null)
            {
                if (!ActiveContextMenu.TryCloseActiveContextMenu())
                    return false;

                MGContextMenu Previous = ActiveContextMenu;
                ActiveContextMenu.InvokeContextMenuClosing();
                _ActiveContextMenu = null;
                NPC(nameof(ActiveContextMenu));
                Previous.InvokeContextMenuClosed();
                SubmenuClosed?.Invoke(this, Previous);
                return true;
            }
            else
                return true;
        }

        /// <returns>True if the <paramref name="Menu"/> was already opened, or was successfully opened.<br/>
        /// False if <see cref="MGContextMenu.CanContextMenuOpen"/> is false.</returns>
        public bool TryOpenContextMenu(MGContextMenu Menu, Rectangle Anchor)
        {
            if (!TryCloseActiveContextMenu())
                return false;

            if (Menu == null || !Menu.CanContextMenuOpen)
                return false;

            Rectangle ValidBounds = GetDesktop().ValidScreenBounds;
            if (Menu.IsContextMenuOpen)
            {
                Size MenuSizeScreenSpace = new((int)(Menu.RenderBounds.Width * Menu.Scale), (int)(Menu.RenderBounds.Height * Menu.Scale));
                Point NewPosition = FitMenuToViewport(Anchor, MenuSizeScreenSpace, ValidBounds).TopLeft();
                Menu.Left = NewPosition.X;
                Menu.Top = NewPosition.Y;
                Menu.ValidateWindowSizeAndPosition();
                return true;
            }
            else
            {
                Menu.InvokeContextMenuOpening();
                _ActiveContextMenu = Menu;

                Menu.Scale = this.Scale;

                int MinWidth = 100;
                int MinHeight = 40;
                int MaxWidth = 1000;
                int MaxHeight = 800;

                Size MenuSizeUnscaledScreenSpace = Menu.ComputeContentSize(MinWidth, MinHeight, MaxWidth, MaxHeight);
                Size MenuSizeScreenSpace = new((int)(MenuSizeUnscaledScreenSpace.Width * Menu.Scale), (int)(MenuSizeUnscaledScreenSpace.Height * Menu.Scale));

                Point Position = FitMenuToViewport(Anchor, MenuSizeScreenSpace, ValidBounds).TopLeft();
                Menu.TopLeft = Position;
                _ = Menu.ApplySizeToContent(SizeToContent.WidthAndHeight, MinWidth, MinHeight, MaxWidth, MaxHeight, true);

                NPC(nameof(ActiveContextMenu));
                ActiveContextMenu.InvokeContextMenuOpened();
                SubmenuOpened?.Invoke(this, Menu);
                return true;
            }
        }

        public IEnumerable<MGContextMenu> Submenus
        {
            get
            {
                MGContextMenu Current = this.ActiveContextMenu;
                while (Current != null)
                {
                    yield return Current;
                    Current = Current.ActiveContextMenu;
                }
            }
        }

        /// <summary>True if the current mouse position is hovering any nested submenu in <see cref="Submenus"/>. See also: <see cref="ActiveContextMenu"/></summary>
        public bool IsHoveringSubmenu(int Padding)
        {
            Point CurrentMousePosition = InputTracker.Mouse.CurrentPosition;
            foreach (MGContextMenu Submenu in Submenus)
            {
                Point LayoutSpacePosition = Submenu.ConvertCoordinateSpace(CoordinateSpace.Screen, CoordinateSpace.Layout, CurrentMousePosition);
                Rectangle SubmenuBounds = Submenu.LayoutBounds.GetExpanded(Padding);
                if (SubmenuBounds.ContainsInclusive(LayoutSpacePosition))
                    return true;
            }

            return false;
        }

        internal event EventHandler<MGContextMenu> SubmenuOpened;
        internal event EventHandler<MGContextMenu> SubmenuClosed;
        #endregion Nested Menu

        //TODO
        //functions to remove/replace from _Items (need to implement NotifyCollectionChangedAction.Replace also)

        /// <summary>Invoked when a <see cref="MGContextMenuButton"/> item is clicked.<br/>
        /// The <see cref="MGContextMenuButton"/> may exist within a nested submenu (See also: <see cref="Submenus"/>)</summary>
        public event EventHandler<MGContextMenuButton> ItemSelected;
        /// <summary>Invoked when a <see cref="MGContextMenuToggle"/> item is clicked, after the <see cref="MGContextMenuToggle.IsChecked"/> value changes. <br/>
        /// The <see cref="MGContextMenuButton"/> may exist within a nested submenu (See also: <see cref="Submenus"/>)</summary>
        public event EventHandler<MGContextMenuToggle> ItemToggled;

        public IContextMenuHost Host { get; }
        public bool IsSubmenu => Host is MGContextMenu;

        /// <summary>Consider subscribing to <see cref="ItemSelected"/> and <see cref="ItemToggled"/> to handle user actions.</summary>
        public static MGContextMenu CreateSimpleMenu(MGDesktop Desktop, string Title, Color? TextForeground, params MGSimpleContextMenuItem[] Items)
        {
            MGContextMenu Menu = new(Desktop, Title);
            AddItemsToMenu(Menu, TextForeground, Items?.ToList());
            return Menu;
        }

        /// <summary>Consider subscribing to <see cref="ItemSelected"/> and <see cref="ItemToggled"/> to handle user actions.</summary>
        public static MGContextMenu CreateSimpleMenu(MGWindow Window, string Title, Color? TextForeground, params MGSimpleContextMenuItem[] Items)
        {
            MGContextMenu Menu = new(Window, Title, Window.Theme);
            AddItemsToMenu(Menu, TextForeground, Items?.ToList());
            return Menu;
        }

        private static void AddItemsToMenu(MGContextMenu Menu, Color? TextForeground, List<MGSimpleContextMenuItem> Items)
        {
            if (Menu == null)
                throw new ArgumentNullException(nameof(Menu));

            if (Items != null)
            {
                int FontSize = Menu.GetTheme().FontSettings.ContextMenuFontSize;
                foreach (MGSimpleContextMenuItem Item in Items)
                {
                    MGContextMenuItem GeneratedItem;
                    MGTextBlock Content = new(Menu, Item.Text, TextForeground, FontSize);
                    if (!Item.IsToggle)
                    {
                        MGContextMenuButton ButtonItem = Menu.AddButton(Content, null);
                        ButtonItem.Icon = Item.Icon;
                        GeneratedItem = ButtonItem;

                        if (Item.Submenu?.Any() == true)
                        {
                            MGContextMenu Submenu = new(Menu);
                            AddItemsToMenu(Submenu, TextForeground, Item.Submenu);
                            ButtonItem.Submenu = Submenu;
                        }
                    }
                    else
                    {
                        MGContextMenuToggle ToggleItem = Menu.AddToggle(Content, Item.IsChecked);
                        GeneratedItem = ToggleItem;

                        if (Item.Submenu?.Any() == true)
                        {
                            MGContextMenu Submenu = new(Menu);
                            AddItemsToMenu(Submenu, TextForeground, Item.Submenu);
                            ToggleItem.Submenu = Submenu;
                        }
                    }

                    GeneratedItem.ComputeIsVisible = Item.ComputeIsVisible;
                    GeneratedItem.CommandId = Item.CommandId;
                    Item.GeneratedItem = GeneratedItem;
                }
            }
        }

        /// <summary>Creates a nested <see cref="MGContextMenu"/></summary>
        public MGContextMenu(MGContextMenu ParentContextMenu)
            : this(ParentContextMenu, ParentContextMenu.TitleText, ParentContextMenu.Theme)
        {
            this.Host = ParentContextMenu;
            this.HeaderSize = ParentContextMenu.HeaderSize;
            this.ButtonWrapperTemplate = ParentContextMenu.ButtonWrapperTemplate;
            this.StaysOpenOnItemSelected = ParentContextMenu.StaysOpenOnItemSelected;
            this.StaysOpenOnItemToggled = ParentContextMenu.StaysOpenOnItemToggled;
            this.AutoCloseThreshold = null;
        }

        /// <summary>Creates a root-level <see cref="MGContextMenu"/></summary>
        public MGContextMenu(MGWindow Window, string TitleText = "[b]Choose Option[/b]", MGTheme Theme = null)
            : this(Window.Desktop, Theme ?? Window.Theme, Window, TitleText) { }

        /// <summary>Creates a root-level <see cref="MGContextMenu"/> that does not belong to any <see cref="MGWindow"/>s</summary>
        public MGContextMenu(MGDesktop Desktop, string TitleText = "[b]Choose Option[/b]", MGTheme Theme = null)
            : this(Desktop, Theme, null, TitleText) { }

        protected MGContextMenu(MGDesktop Desktop, MGTheme WindowTheme, MGWindow Window, string TitleText = "[b]Choose Option[/b]")
            : base(Desktop, WindowTheme, Window, MGElementType.ContextMenu, 0, 0, 1, 1)
        {
            using (BeginInitializing())
            {
                this.Host = Desktop;

                this.IsDraggable = false;
                this.AllowsClickThrough = false;
                this.IsCloseButtonVisible = false;

                this.ItemsPanel = new(this, Orientation.Vertical);
                ItemsPanel.Spacing = 2;
                ItemsPanel.ManagedParent = this;
                MGScrollViewer SV = new(this, ScrollBarVisibility.Auto, ScrollBarVisibility.Disabled);
                this.ScrollViewerElement = SV;
                SV.Padding = new(0);
                SV.SetContent(ItemsPanel);
                SV.ManagedParent = this;
                this.SetContent(SV);

                SV.CanChangeContent = false;
                ItemsPanel.CanChangeContent = false;
                this.CanChangeContent = false;

                this.TitleText = TitleText;
                this.IsTitleBarVisible = !string.IsNullOrEmpty(TitleText);
                this.TitleBarTextBlockElement.TextAlignment = HorizontalAlignment.Center;

                this.Padding = new(1);
                this.BorderBrush = MGUniformBorderBrush.Gray;
                this.BorderThickness = new(1);

                this.IsUserResizable = false;

                this.MinWidth = 150;
                this.MaxWidth = 600;
                this.MinHeight = 50;
                this.MaxHeight = 600;

                this.HeaderSize = new Size(14, 14);

                this.StaysOpenOnItemSelected = false;
                this.StaysOpenOnItemToggled = true;
                this.AutoCloseThreshold = DefaultAutoCloseThreshold;
                this.CanContextMenuOpen = true;

                this.CanChangeContent = false;

                this._Items = new();
                this._Items.CollectionChanged += (sender, e) =>
                {
                    using (ItemsPanel.AllowChangingContentTemporarily())
                    {
                        if (e.Action is NotifyCollectionChangedAction.Add)
                        {
                            if (e.NewItems != null)
                            {
                                int Index = e.NewStartingIndex;
                                foreach (MGContextMenuItem Item in e.NewItems)
                                {
                                    if (Item is MGContextMenuButton Button)
                                        Button.OnSelected += MenuItem_ItemSelected;
                                    else if (Item is MGContextMenuToggle Toggle)
                                        Toggle.OnToggled += MenuItem_ItemToggled;
                                    ItemsPanel.TryInsertChild(Index, Item);
                                    Index++;
                                }
                            }
                        }

                        if (e.Action is NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Reset)
                        {
                            if (e.OldItems != null)
                            {
                                foreach (MGContextMenuItem Item in e.OldItems)
                                {
                                    if (Item is MGContextMenuButton Button)
                                        Button.OnSelected -= MenuItem_ItemSelected;
                                    else if (Item is MGContextMenuToggle Toggle)
                                        Toggle.OnToggled -= MenuItem_ItemToggled;
                                    ItemsPanel.TryRemoveChild(Item);
                                }
                            }
                        }

                        if (e.Action is NotifyCollectionChangedAction.Replace or NotifyCollectionChangedAction.Move)
                        {
                            //Too lazy to implement this. Probably won't ever use it.
                            throw new NotImplementedException();
                        }
                    }
                };

                #region Bug Workaround
                //  I have no clue why this is needed, but without it, the layout is incorrect if:
                //      1. This ContextMenu's Vertical ScrollBar is visible (Set this.MaxHeight to a small value to test)
                //      2. The menu is closed and then re-opened
                //  The layout would be correct the first time the menu is opened. Afterwards, the menu items seem to ignore the Vertical ScrollBar's width
                //  Good luck to future me trying to fix this issue for real instead of with a shitty workaround
                //  Can also be 'fixed' by removing MGElement.TryGetRecentSelfMeasurement(...)
                //  Seems to be related to how MGScrollViewer handles ScrollBarVisibility.Auto
                ContextMenuOpening += (sender, e) => { SV.InvalidateLayout(); };
                //  TODO: This may have been due to an issue that was fixed in this commit?
                //  https://github.com/Videogamers0/MGUI/commit/dd4c1d6cefffe5d4474f8e6017687ff14b6c63c1
                //  Should try re-testing the issue now and see if it's still reproducible
                #endregion Bug Workaround

                this.ButtonWrapperTemplate = CreateDefaultDropdownButton;

                MouseHandler.MovedOutside += (sender, e) =>
                {
                    if (IsContextMenuOpen && !IsSubmenu && !IsHoveringSubmenu(5) && AutoCloseThreshold.HasValue)
                    {
                        Point LayoutSpacePosition = this.ConvertCoordinateSpace(CoordinateSpace.Screen, CoordinateSpace.Layout, e.CurrentPosition);
                        if (((RectangleF)this.LayoutBounds).SquaredDistanceTo(LayoutSpacePosition) >= AutoCloseThreshold.Value * AutoCloseThreshold.Value)
                            TryCloseContextMenu();
                    }
                };

                MouseHandler.PressedOutside += (sender, e) =>
                {
                    if (IsContextMenuOpen)
                    {
                        TryCloseContextMenu();
                        e.SetHandledBy(this, false);
                    }
                };

                //  Draw the submenu(s)
                if (ParentWindow != null && ParentWindow.ElementType != MGElementType.ContextMenu)
                {
                    //  If the ContextMenu is inside of another Window, don't draw the submenu until after the parent window is done drawing,
                    //  so that the submenu is overtop of the rest of the parent window's content
                    ParentWindow.OnEndDraw += (sender, e) =>
                    {
                        using (e.DA.DT.SetClipTargetTemporary(null, false))
                        {
                            ActiveContextMenu?.Draw(e.DA.AsZeroOffset());
                        }
                    };
                }
                else
                {
                    //  If the ContextMenu is its own root-level window, draw the submenu immediately after we're done drawing this ContextMenu
                    OnEndDraw += (sender, e) =>
                    {
                        using (e.DA.DT.SetClipTargetTemporary(null, false))
                        {
                            ActiveContextMenu?.Draw(e.DA.AsZeroOffset());
                        }
                    };
                }

                //  Spaghetti logic for cases where you nest a ContextMenu directly inside of a Window,
                //  rather than making this ContextMenu be its own root-level Window content via Element.ContextMenu
                if (ParentWindow != null && ParentWindow.ElementType != MGElementType.ContextMenu)
                {
                    ParentWindow.OnWindowPositionChanged += (sender, e) =>
                    {
                        Point PreviousPosition = this.LayoutBounds.TopLeft();
                        Point Offset = new(e.NewValue.Left - e.PreviousValue.Left, e.NewValue.Top - e.PreviousValue.Top);
                        InvokeWindowPositionChanged(PreviousPosition, PreviousPosition + Offset);
                    };
                }

                OnBeginUpdateContents += (sender, e) =>
                {
                    ActiveContextMenu?.Update(e.UA.AsZeroOffset());
                };

                SubmenuOpened += (sender, e) =>
                {
                    e.ItemSelected += Submenu_ItemSelected;
                    e.ItemToggled += Submenu_ItemToggled;
                };

                SubmenuClosed += (sender, e) =>
                {
                    e.ItemSelected -= Submenu_ItemSelected;
                    e.ItemToggled -= Submenu_ItemToggled;
                };

                ItemSelected += (sender, e) =>
                {
                    if (!StaysOpenOnItemSelected)
                        TryCloseContextMenu();
                };

                ItemToggled += (sender, e) =>
                {
                    if (!StaysOpenOnItemToggled)
                        TryCloseContextMenu();
                };
            }
        }

        private void Submenu_ItemSelected(object sender, MGContextMenuButton e) => ItemSelected?.Invoke(this, e);
        private void Submenu_ItemToggled(object sender, MGContextMenuToggle e) => ItemToggled?.Invoke(this, e);

        private void MenuItem_ItemSelected(object sender, EventArgs e)
        {
            if (sender is MGContextMenuButton Button)
                ItemSelected?.Invoke(this, Button);
        }

        private void MenuItem_ItemToggled(object sender, bool e)
        {
            if (sender is MGContextMenuToggle Toggle)
                ItemToggled?.Invoke(this, Toggle);
        }

        public IEnumerable<TMenuItemType> GetItemsOfType<TMenuItemType>(bool IncludeSubmenus)
            where TMenuItemType : MGContextMenuItem
        {
            foreach (MGContextMenuItem Item in this.Items)
            {
                if (Item is TMenuItemType TypedItem)
                    yield return TypedItem;

                if (IncludeSubmenus && Item is MGWrappedContextMenuItem WrappedItem && WrappedItem.Submenu != null)
                {
                    foreach (TMenuItemType NestedItem in WrappedItem.Submenu.GetItemsOfType<TMenuItemType>(IncludeSubmenus))
                        yield return NestedItem;
                }
            }
        }

        /// <summary>Throws <see cref="InvalidOperationException"/> if no <see cref="MGContextMenuItem"/> was found with the given <paramref name="CommandId"/></summary>
        public TMenuItemType FindItemByCommandId<TMenuItemType>(string CommandId)
            where TMenuItemType : MGContextMenuItem
            => GetItemsOfType<TMenuItemType>(true).First(x => x.CommandId == CommandId);
    }
}
