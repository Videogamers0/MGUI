using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MGUI.Shared.Helpers;
using MGUI.Shared.Input.Keyboard;
using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.Containers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;

namespace MGUI.Core.UI
{
    /// <summary>Represents a single clickable entry in an <see cref="MGMenuBar"/>.<para/>
    /// Contains an optional <see cref="Submenu"/> that opens as a dropdown when the item is clicked.</summary>
    public class MGMenuBarItem : MGSingleContentHost
    {
        public MGMenuBar MenuBar { get; }

        #region Border
        /// <summary>Provides direct access to this element's border.</summary>
        public MGComponent<MGBorder> BorderComponent { get; }
        private MGBorder BorderElement { get; }
        public override MGBorder GetBorder() => BorderElement;

        public IBorderBrush BorderBrush
        {
            get => BorderElement.BorderBrush;
            set => BorderElement.BorderBrush = value;
        }

        public Thickness BorderThickness
        {
            get => BorderElement.BorderThickness;
            set => BorderElement.BorderThickness = value;
        }
        #endregion Border

        #region ContentWrapper
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private MGButton _ContentWrapper;
        /// <summary>The <see cref="MGButton"/> that wraps this item's content and handles click/hover interactions.</summary>
        public MGButton ContentWrapper
        {
            get => _ContentWrapper;
            internal set
            {
                if (_ContentWrapper != value)
                {
                    MGButton Previous = ContentWrapper;
                    _ContentWrapper = value;

                    if (Previous != null)
                    {
                        using (Previous.AllowChangingContentTemporarily())
                            Previous.SetContent(null as MGElement);
                    }

                    if (ContentWrapper != null)
                    {
                        ContentWrapper.CanChangeContent = false;
                        using (ContentWrapper.AllowChangingContentTemporarily())
                            ContentWrapper.SetContent(_ItemContent);
                    }

                    using (AllowChangingContentTemporarily())
                        SetContent(ContentWrapper);

                    NPC(nameof(ContentWrapper));
                    OnContentWrapperChanged();
                }
            }
        }

        private void OnContentWrapperChanged()
        {
            if (ContentWrapper == null)
                return;

            ContentWrapper.AddCommandHandler((Btn, e) =>
            {
                if (MenuBar.ActiveItem == this)
                    MenuBar.CloseActiveItem();
                else
                    MenuBar.OpenItem(this);
            });

            ContentWrapper.MouseHandler.Entered += (sender, e) =>
            {
                if (MenuBar.IsMenuActive && MenuBar.ActiveItem != this)
                    MenuBar.OpenItem(this);
            };
        }

        private MGElement _ItemContent;
        #endregion ContentWrapper

        #region Submenu
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private MGContextMenu _Submenu;
        /// <summary>The dropdown <see cref="MGContextMenu"/> that opens when this item is clicked.<para/>
        /// Can be null if the item has no dropdown.</summary>
        public MGContextMenu Submenu
        {
            get => _Submenu;
            set
            {
                if (_Submenu != value)
                {
                    if (_Submenu != null)
                    {
                        _Submenu.ItemSelected -= Submenu_ItemSelected;
                        _Submenu.ItemToggled -= Submenu_ItemToggled;
                        _Submenu.ItemRadioSelected -= Submenu_ItemRadioSelected;
                        _Submenu.ContextMenuOpened -= Submenu_Opened;
                        _Submenu.ContextMenuClosed -= Submenu_Closed;
                    }

                    _Submenu = value;

                    if (_Submenu != null)
                    {
                        _Submenu.ItemSelected += Submenu_ItemSelected;
                        _Submenu.ItemToggled += Submenu_ItemToggled;
                        _Submenu.ItemRadioSelected += Submenu_ItemRadioSelected;
                        _Submenu.ContextMenuOpened += Submenu_Opened;
                        _Submenu.ContextMenuClosed += Submenu_Closed;
                    }

                    NPC(nameof(Submenu));
                }
            }
        }

        private void Submenu_Opened(object sender, EventArgs e)
        {
            if (ContentWrapper != null)
                ContentWrapper.SpoofIsHoveredWhileDrawingBackground = true;
        }

        private void Submenu_Closed(object sender, EventArgs e)
        {
            if (ContentWrapper != null)
                ContentWrapper.SpoofIsHoveredWhileDrawingBackground = false;
            MenuBar.OnSubmenuClosed(this);
        }

        private void Submenu_ItemSelected(object sender, MGContextMenuButton e) => MenuBar.InvokeItemSelected(e);
        private void Submenu_ItemToggled(object sender, MGContextMenuToggle e) => MenuBar.InvokeItemToggled(e);
        private void Submenu_ItemRadioSelected(object sender, MGContextMenuRadio e) => MenuBar.InvokeItemRadioSelected(e);
        #endregion Submenu

        /// <summary>Opens the <see cref="Submenu"/> positioned directly below this item in screen space.</summary>
        internal void OpenSubmenu()
        {
            if (Submenu == null || Submenu.IsContextMenuOpen)
                return;
            Rectangle ScreenBounds = ConvertCoordinateSpace(CoordinateSpace.Layout, CoordinateSpace.Screen, LayoutBounds);
            Point AnchorPoint = new(ScreenBounds.Left, ScreenBounds.Bottom);
            Submenu.TryOpenContextMenu(AnchorPoint);
        }

        /// <summary>Closes the <see cref="Submenu"/> if it is currently open.</summary>
        internal void CloseSubmenu()
        {
            if (Submenu?.IsContextMenuOpen == true)
                Submenu.TryCloseContextMenu();
        }

        public MGMenuBarItem(MGMenuBar MenuBar, MGElement Content)
            : base(MenuBar.SelfOrParentWindow, MGElementType.MenuBarItem)
        {
            using (BeginInitializing())
            {
                this.MenuBar = MenuBar;
                _ItemContent = Content;

                BorderElement = new(SelfOrParentWindow, new Thickness(0), MGUniformBorderBrush.Black);
                BorderComponent = MGComponentBase.Create(BorderElement);
                AddComponent(BorderComponent);
                BorderElement.OnBorderBrushChanged += (sender, e) => NPC(nameof(BorderBrush));
                BorderElement.OnBorderThicknessChanged += (sender, e) => NPC(nameof(BorderThickness));

                CanChangeContent = false;

                ContentWrapper = MenuBar.ButtonWrapperTemplate(SelfOrParentWindow);
            }
        }
    }

    /// <summary>A horizontal menu bar that displays a row of top-level <see cref="MGMenuBarItem"/>s,
    /// each of which can open a dropdown <see cref="MGContextMenu"/> when clicked.<para/>
    /// Supports hierarchical menus (unlimited levels), separators, toggle items, radio groups, disabled items, and icon + text.</summary>
    public class MGMenuBar : MGSingleContentHost
    {
        /// <inheritdoc/>
        public override bool CanHandleKeyboardInput => IsMenuActive;
        #region Border
        /// <summary>Provides direct access to this element's border.</summary>
        public MGComponent<MGBorder> BorderComponent { get; }
        private MGBorder BorderElement { get; }
        public override MGBorder GetBorder() => BorderElement;

        public IBorderBrush BorderBrush
        {
            get => BorderElement.BorderBrush;
            set => BorderElement.BorderBrush = value;
        }

        public Thickness BorderThickness
        {
            get => BorderElement.BorderThickness;
            set => BorderElement.BorderThickness = value;
        }
        #endregion Border

        #region Internal layout
        /// <summary>The horizontal <see cref="MGStackPanel"/> that contains all top-level <see cref="MGMenuBarItem"/>s.</summary>
        public MGStackPanel ItemsPanel { get; }
        #endregion Internal layout

        #region Items
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ObservableCollection<MGMenuBarItem> _Items { get; }
        public IList<MGMenuBarItem> Items => _Items;

        /// <summary>Adds a new top-level menu item with the given text label.</summary>
        /// <param name="Text">The label text displayed in the bar.</param>
        /// <param name="Configure">Optional callback invoked after creation to configure the item (e.g. attach a <see cref="MGMenuBarItem.Submenu"/>).</param>
        public MGMenuBarItem AddItem(string Text, Action<MGMenuBarItem> Configure = null)
            => AddItem(new MGTextBlock(SelfOrParentWindow, Text, null, GetTheme().FontSettings.ContextMenuFontSize), Configure);

        /// <summary>Adds a new top-level menu item with a custom content element.</summary>
        public MGMenuBarItem AddItem(MGElement Content, Action<MGMenuBarItem> Configure = null)
        {
            MGMenuBarItem Item = new(this, Content);
            Configure?.Invoke(Item);
            _Items.Add(Item);
            return Item;
        }
        #endregion Items

        #region ButtonWrapperTemplate
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Func<MGWindow, MGButton> _ButtonWrapperTemplate;
        /// <summary>Factory used to create the <see cref="MGButton"/> wrapper for each <see cref="MGMenuBarItem"/>.<para/>
        /// Default value: <see cref="CreateDefaultBarButton"/></summary>
        public Func<MGWindow, MGButton> ButtonWrapperTemplate
        {
            get => _ButtonWrapperTemplate;
            set
            {
                if (_ButtonWrapperTemplate != value)
                {
                    _ButtonWrapperTemplate = value;
                    NPC(nameof(ButtonWrapperTemplate));
                }
            }
        }

        /// <summary>Creates the default button style for a top-level <see cref="MGMenuBarItem"/>.</summary>
        public MGButton CreateDefaultBarButton(MGWindow Window)
        {
            MGButton Button = new(Window ?? this.SelfOrParentWindow, new Thickness(0), MGUniformBorderBrush.Black);
            Button.Padding = new Thickness(8, 3, 8, 3);
            Button.Margin = new Thickness(0);
            Button.HorizontalContentAlignment = HorizontalAlignment.Center;
            Button.VerticalContentAlignment = VerticalAlignment.Center;
            Button.BackgroundBrush = GetTheme().GetBackgroundBrush(MGElementType.MenuBarItem);
            return Button;
        }
        #endregion ButtonWrapperTemplate

        #region Active Item / Menu State
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _IsMenuActive;
        /// <summary>True when at least one top-level item's dropdown is open.</summary>
        public bool IsMenuActive
        {
            get => _IsMenuActive;
            private set
            {
                if (_IsMenuActive != value)
                {
                    _IsMenuActive = value;
                    NPC(nameof(IsMenuActive));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private MGMenuBarItem _ActiveItem;
        /// <summary>The <see cref="MGMenuBarItem"/> whose dropdown is currently open, or null.</summary>
        public MGMenuBarItem ActiveItem
        {
            get => _ActiveItem;
            private set
            {
                if (_ActiveItem != value)
                {
                    _ActiveItem = value;
                    NPC(nameof(ActiveItem));
                }
            }
        }

        /// <summary>Opens the dropdown of the given <paramref name="Item"/>,
        /// closing the previously active item's dropdown if needed.</summary>
        internal void OpenItem(MGMenuBarItem Item)
        {
            if (Item == null || Item.Submenu == null)
                return;

            if (ActiveItem != null && ActiveItem != Item)
                ActiveItem.CloseSubmenu();

            ActiveItem = Item;
            IsMenuActive = true;
            Item.OpenSubmenu();

            // Claim keyboard focus so arrow/escape keys reach this handler
            GetDesktop().QueuedFocusedKeyboardHandler = this;
        }

        /// <summary>Closes the dropdown of the current active item.</summary>
        internal void CloseActiveItem()
        {
            if (ActiveItem != null)
            {
                MGMenuBarItem Prev = ActiveItem;
                ActiveItem = null;
                IsMenuActive = false;
                Prev.CloseSubmenu();

                // Release keyboard focus
                if (GetDesktop().FocusedKeyboardHandler == this)
                    GetDesktop().QueuedFocusedKeyboardHandler = null;
            }
        }

        /// <summary>Called by <see cref="MGMenuBarItem"/> when its submenu closes for any reason.</summary>
        internal void OnSubmenuClosed(MGMenuBarItem Item)
        {
            if (ActiveItem == Item)
            {
                ActiveItem = null;
                IsMenuActive = false;
            }
        }
        #endregion Active Item / Menu State

        #region Events
        /// <summary>Invoked when a <see cref="MGContextMenuButton"/> in any dropdown is clicked.</summary>
        public event EventHandler<MGContextMenuButton> ItemSelected;
        /// <summary>Invoked when a <see cref="MGContextMenuToggle"/> in any dropdown is toggled.</summary>
        public event EventHandler<MGContextMenuToggle> ItemToggled;
        /// <summary>Invoked when a <see cref="MGContextMenuRadio"/> in any dropdown is selected.</summary>
        public event EventHandler<MGContextMenuRadio> ItemRadioSelected;

        internal void InvokeItemSelected(MGContextMenuButton e) => ItemSelected?.Invoke(this, e);
        internal void InvokeItemToggled(MGContextMenuToggle e) => ItemToggled?.Invoke(this, e);
        internal void InvokeItemRadioSelected(MGContextMenuRadio e) => ItemRadioSelected?.Invoke(this, e);
        #endregion Events

        /// <param name="Window">The parent <see cref="MGWindow"/>.</param>
        public MGMenuBar(MGWindow Window)
            : base(Window, MGElementType.MenuBar)
        {
            using (BeginInitializing())
            {
                BorderElement = new(Window, new Thickness(0, 0, 0, 1), MGUniformBorderBrush.Black);
                BorderComponent = MGComponentBase.Create(BorderElement);
                AddComponent(BorderComponent);
                BorderElement.OnBorderBrushChanged += (sender, e) => NPC(nameof(BorderBrush));
                BorderElement.OnBorderThicknessChanged += (sender, e) => NPC(nameof(BorderThickness));

                ButtonWrapperTemplate = CreateDefaultBarButton;

                ItemsPanel = new(Window, Orientation.Horizontal);
                ItemsPanel.Spacing = 0;
                ItemsPanel.ManagedParent = this;
                ItemsPanel.CanChangeContent = false;

                using (AllowChangingContentTemporarily())
                    SetContent(ItemsPanel);
                CanChangeContent = false;

                HorizontalAlignment = HorizontalAlignment.Stretch;
                VerticalAlignment = VerticalAlignment.Top;
                MinHeight = 22;
                Padding = new Thickness(2, 1, 2, 1);

                // Keyboard navigation: ← / → moves between top-level items, Escape closes the menu
                KeyboardHandler.Pressed += (sender, e) =>
                {
                    if (!IsMenuActive)
                        return;

                    if (e.Key == Keys.Left)
                    {
                        int Idx = _Items.IndexOf(ActiveItem);
                        if (Idx > 0)
                            OpenItem(_Items[Idx - 1]);
                        e.SetHandledBy(this, false);
                    }
                    else if (e.Key == Keys.Right)
                    {
                        int Idx = _Items.IndexOf(ActiveItem);
                        if (Idx >= 0 && Idx < _Items.Count - 1)
                            OpenItem(_Items[Idx + 1]);
                        e.SetHandledBy(this, false);
                    }
                    else if (e.Key == Keys.Escape)
                    {
                        CloseActiveItem();
                        e.SetHandledBy(this, false);
                    }
                };

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
                                foreach (MGMenuBarItem Item in e.NewItems)
                                {
                                    ItemsPanel.TryInsertChild(Index, Item);
                                    Index++;
                                }
                            }
                        }

                        if (e.Action is NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Reset)
                        {
                            if (e.OldItems != null)
                            {
                                foreach (MGMenuBarItem Item in e.OldItems)
                                    ItemsPanel.TryRemoveChild(Item);
                            }
                        }

                        if (e.Action is NotifyCollectionChangedAction.Replace or NotifyCollectionChangedAction.Move)
                            throw new NotImplementedException();
                    }
                };
            }
        }
    }
}
