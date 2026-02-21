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

        /// <returns>True if the opening should proceed; false if a subscriber cancelled it.</returns>
        internal bool InvokeContextMenuOpening()
        {
            if (ContextMenuOpening != null)
            {
                var args = new System.ComponentModel.CancelEventArgs();
                ContextMenuOpening.Invoke(this, args);
                if (args.Cancel) return false;
            }

            // Rebuild items via factory (if set) after the cancel-check so we
            // don't mutate items for a menu that ends up not opening.
            if (ItemsFactory != null)
            {
                ClearItems();
                ItemsFactory(this);
            }

            return true;
        }
        internal void InvokeContextMenuOpened()
        {
            NPC(nameof(IsContextMenuOpen));
            ContextMenuOpened?.Invoke(this, EventArgs.Empty);
        }
        internal void InvokeContextMenuClosing() => ContextMenuClosing?.Invoke(this, EventArgs.Empty);
        internal void InvokeContextMenuClosed()
        {
            // Invalidate the entire layout subtree so the next open starts with fresh
            // measurement caches. Two conditions make this necessary:
            //   1. Closing the menu does not set IsLayoutValid = false by default.
            //   2. InvalidateLayout() / LayoutChanged() do not propagate downward into children.
            // Without this, TryGetCachedMeasurement() in UpdateLayout can return stale values
            // from the previous open cycle — e.g. items overlapping the vertical scrollbar when
            // ScrollBarVisibility.Auto is used (see root-cause analysis in TODO-ContextMenu-Improvements.md).
            InvalidateLayoutTree();
            NPC(nameof(IsContextMenuOpen));
            ContextMenuClosed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Fired just before this <see cref="MGContextMenu"/> is shown.<br/>
        /// Set <see cref="System.ComponentModel.CancelEventArgs.Cancel"/> to <see langword="true"/> to prevent the menu from opening.<para/>
        /// <b>Dynamic items:</b> Use <see cref="ItemsFactory"/> instead of subscribing here to add/clear items.
        /// <see cref="ItemsFactory"/> is called automatically on every open and avoids the risks below.<para/>
        /// <b>Double-subscription warning:</b> This is a standard C# event — subscribing N times means the handler
        /// runs N times per open. If the same object re-subscribes on every tab-rebuild or layout pass, items (or
        /// other side-effects) will accumulate. Always pair a subscription with an unsubscription, or use
        /// <see cref="ItemsFactory"/> which replaces the handler pattern entirely.<para/>
        /// <b>Recommended patterns:</b>
        /// <list type="bullet">
        ///   <item>Static items: declare them once in XAML or in the constructor — no handler needed.</item>
        ///   <item>Dynamic items: set <see cref="ItemsFactory"/> once.</item>
        ///   <item>Conditional logic (not item-building): subscribe here, but ensure you unsubscribe when
        ///         the hosting element is disposed / rebuilt.</item>
        /// </list></summary>
        public event EventHandler<System.ComponentModel.CancelEventArgs> ContextMenuOpening;
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

        /// <summary>Removes all items from this menu, correctly unregistering internal event handlers
        /// and removing elements from the visual tree.
        /// <para/>Prefer this over calling <c>Items.Clear()</c> directly, which does not clean up properly
        /// due to <see cref="ObservableCollection{T}"/>'s Reset action not carrying <c>OldItems</c>.</summary>
        public void ClearItems()
        {
            for (int i = _Items.Count - 1; i >= 0; i--)
                _Items.RemoveAt(i);
        }

        /// <summary>Optional factory called every time this menu is about to open (just after
        /// <see cref="ContextMenuOpening"/> fires and has not been cancelled).
        /// When set, <see cref="ClearItems"/> is called automatically before the factory runs,
        /// so the item list is always freshly built.
        /// <para/>This is the recommended approach for <em>dynamic</em> menus whose items change
        /// between invocations — it replaces the fragile pattern of subscribing to
        /// <see cref="ContextMenuOpening"/> and calling <c>Clear()+AddButton()</c> inside the handler.
        /// <para/>Example:
        /// <code>
        /// menu.ItemsFactory = m =>
        /// {
        ///     m.AddButton("Close",  _ => Close());
        ///     m.AddButton("Reload", _ => Reload());
        /// };
        /// </code></summary>
        public Action<MGContextMenu> ItemsFactory { get; set; }
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
                if (!Menu.InvokeContextMenuOpening())
                    return false;
                _ActiveContextMenu = Menu;

                Menu.Scale = Scale;

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
                MGContextMenu Current = ActiveContextMenu;
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
            Host = ParentContextMenu;
            HeaderSize = ParentContextMenu.HeaderSize;
            ButtonWrapperTemplate = ParentContextMenu.ButtonWrapperTemplate;
            StaysOpenOnItemSelected = ParentContextMenu.StaysOpenOnItemSelected;
            StaysOpenOnItemToggled = ParentContextMenu.StaysOpenOnItemToggled;
            AutoCloseThreshold = null;
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
                Host = Desktop;

                IsDraggable = false;
                AllowsClickThrough = false;
                IsCloseButtonVisible = false;

                ItemsPanel = new(this, Orientation.Vertical);
                ItemsPanel.Spacing = 2;
                ItemsPanel.ManagedParent = this;
                MGScrollViewer SV = new(this, ScrollBarVisibility.Auto, ScrollBarVisibility.Disabled);
                ScrollViewerElement = SV;
                SV.Padding = new(0);
                SV.SetContent(ItemsPanel);
                SV.ManagedParent = this;
                SetContent(SV);

                SV.CanChangeContent = false;
                ItemsPanel.CanChangeContent = false;
                CanChangeContent = false;

                this.TitleText = TitleText;
                IsTitleBarVisible = !string.IsNullOrEmpty(TitleText);
                TitleBarTextBlockElement.TextAlignment = HorizontalAlignment.Center;

                Padding = new(1);
                BorderBrush = MGUniformBorderBrush.Gray;
                BorderThickness = new(1);

                IsUserResizable = false;

                MinWidth = 150;
                MaxWidth = 600;
                MinHeight = 50;
                MaxHeight = 600;

                HeaderSize = new Size(14, 14);

                StaysOpenOnItemSelected = false;
                StaysOpenOnItemToggled = true;
                AutoCloseThreshold = DefaultAutoCloseThreshold;
                CanContextMenuOpen = true;

                CanChangeContent = false;

                _Items = new();
                _Items.CollectionChanged += (sender, e) =>
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

                // Note: the original SV.InvalidateLayout() workaround that lived here (and then in
                // InvokeContextMenuOpening) has been superseded by InvalidateLayoutTree() called in
                // InvokeContextMenuClosed(). Invalidating the whole subtree on close is the proper fix:
                // it ensures every descendant's measurement cache is cleared between open/close cycles,
                // preventing stale values from the previous cycle causing items to overlap the scrollbar.

                ButtonWrapperTemplate = CreateDefaultDropdownButton;

                MouseHandler.MovedOutside += (sender, e) =>
                {
                    if (IsContextMenuOpen && !IsSubmenu && !IsHoveringSubmenu(5) && AutoCloseThreshold.HasValue)
                    {
                        Point LayoutSpacePosition = ConvertCoordinateSpace(CoordinateSpace.Screen, CoordinateSpace.Layout, e.CurrentPosition);
                        if (((RectangleF)LayoutBounds).SquaredDistanceTo(LayoutSpacePosition) >= AutoCloseThreshold.Value * AutoCloseThreshold.Value)
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
                        Point PreviousPosition = LayoutBounds.TopLeft();
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
            foreach (MGContextMenuItem Item in Items)
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
