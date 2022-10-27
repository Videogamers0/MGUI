using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MGUI.Shared.Helpers;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI
{
    public enum ContextMenuItemType
    {
        Button,
        Toggle,
        Separator
    }

    public class MGSimpleContextMenuItem
    {
        public string CommandId { get; set; }
        public string Text { get; set; }

        /// <summary>Can be null. Only used if <see cref="IsToggle"/> is false.</summary>
        public MGImage Icon { get; set; }
        /// <summary>If true, an instance of <see cref="MGContextMenuToggle"/> will be created to represent this item. Otherwise, creates an instance of <see cref="MGContextMenuButton"/></summary>
        public bool IsToggle { get; set; }
        /// <summary>Only used if <see cref="IsToggle"/> is true.</summary>
        public bool IsChecked { get; set; }
        /// <summary>Can be null. Represents a nested submenu that will appear when hovering over this item.</summary>
        public List<MGSimpleContextMenuItem> Submenu { get; set; }

        public Func<bool> ComputeIsVisible { get; set; }

        public MGContextMenuItem GeneratedItem { get; set; }

        public MGSimpleContextMenuItem(string CommandId, string Text)
            : this(CommandId, Text, false, false, null, null, null) { }

        public MGSimpleContextMenuItem(string CommandId, string Text, Func<bool> ComputeIsVisible)
            : this(CommandId, Text, false, false, null, ComputeIsVisible, null) { }

        public MGSimpleContextMenuItem(string CommandId, string Text, bool IsToggle, bool IsChecked, MGImage Icon, Func<bool> ComputeIsVisible)
            : this(CommandId, Text, IsToggle, IsChecked, Icon, ComputeIsVisible, null) { }

        public MGSimpleContextMenuItem(string CommandId, string Text, bool IsToggle, bool IsChecked, MGImage Icon, Func<bool> ComputeIsVisible, params MGSimpleContextMenuItem[] Submenu)
        {
            this.CommandId = CommandId;
            this.Text = Text;
            this.IsToggle = IsToggle;
            this.IsChecked = IsChecked;
            this.Icon = Icon;
            this.ComputeIsVisible = ComputeIsVisible;
            this.Submenu = Submenu?.ToList();
        }
    }

    public abstract class MGContextMenuItem : MGSingleContentHost
    {
        public MGContextMenu Menu { get; }
        public string CommandId { get; set; }

        public ContextMenuItemType MenuItemType { get; }
        public bool IsButton => MenuItemType == ContextMenuItemType.Button;
        public bool IsToggle => MenuItemType == ContextMenuItemType.Toggle;
        public bool IsSeparator => MenuItemType == ContextMenuItemType.Separator;

        /// <summary>True if this <see cref="MGContextMenuItem"/> can accept and handle mouse input.</summary>
        public bool HandlesInput => !IsSeparator;

        /// <summary>Can be null. This <see cref="Func{TResult}"/> will be evaluated each time the parent <see cref="Menu"/> is opened to refresh this element's <see cref="MGElement.Visibility"/>.<para/>
        /// True = <see cref="Visibility.Visible"/><br/>False = <see cref="Visibility.Collapsed"/></summary>
        public Func<bool> ComputeIsVisible { get; set; }

        protected MGContextMenuItem(MGContextMenu Menu, ContextMenuItemType MenuItemType)
            : base(Menu, MGElementType.ContextMenuItem)
        {
            this.Menu = Menu;
            this.MenuItemType = MenuItemType;
            this.CanChangeContent = false;

            Menu.ContextMenuOpening += (sender, e) =>
            {
                if (ComputeIsVisible != null)
                {
                    bool IsVisible = ComputeIsVisible();
                    this.Visibility = IsVisible ? Visibility.Visible : Visibility.Collapsed;
                }
            };
        }
    }

    public abstract class MGWrappedContextMenuItem : MGContextMenuItem
    {
        protected MGContentPresenter HeaderPresenter { get; }

        private MGButton _ContentWrapper;
        /// <summary>The wrapper element that contains the <see cref="MenuItemContent"/>.<para/>
        /// This element is automatically created via <see cref="MGContextMenu.ButtonWrapperTemplate"/></summary>
        public MGButton ContentWrapper
        {
            get => _ContentWrapper;
            internal set
            {
                if (_ContentWrapper != value)
                {
                    MGButton Previous = ContentWrapper;
                    _ContentWrapper = value;
                    NPC(nameof(ContentWrapper));

                    //  Clear previous wrapper's content
                    if (Previous != null)
                    {
                        using (Previous.AllowChangingContentTemporarily())
                        {
                            Previous.SetContent(null as MGElement);
                        }
                    }

                    //  Set new wrapper's content
                    if (ContentWrapper != null)
                    {
                        ContentWrapper.CanChangeContent = false;
                        using (ContentWrapper.AllowChangingContentTemporarily())
                        {
                            ContentWrapper.SetContent(Container);
                        }
                    }

                    //  Set this ContextMenuItem's content
                    using (AllowChangingContentTemporarily())
                    {
                        SetContent(ContentWrapper);
                    }

                    OnContentWrapperChanged();
                }
            }
        }

        protected virtual void OnContentWrapperChanged() { }

        private MGElement _MenuItemContent;
        /// <summary>The content to display inside this <see cref="MGContextMenuItem"/>.<para/>
        /// This content is automatically wrapped inside of an <see cref="MGSingleContentHost"/> that is created via <see cref="MGContextMenu.ButtonWrapperTemplate"/>.<para/>
        /// See also: <see cref="ContentWrapper"/></summary>
        public MGElement MenuItemContent
        {
            get => _MenuItemContent;
            set
            {
                if (_MenuItemContent != value)
                {
                    MGElement Previous = MenuItemContent;
                    _MenuItemContent = value;
                    NPC(nameof(MenuItemContent));

                    using (Container.AllowChangingContentTemporarily())
                    {
                        if (Previous != null)
                            Container.TryRemoveChild(Previous);
                        if (MenuItemContent != null)
                            Container.TryAddChild(MenuItemContent);
                    }
                }
            }
        }

        protected MGStackPanel Container { get; }

        /// <summary>The width of the dropdown arrow that appears on the right-edge of a <see cref="MGContextMenuItem"/> that has a nested <see cref="Submenu"/></summary>
        public const int SubmenuArrowWidth = 5;
        /// <summary>The height of the dropdown arrow that appears on the right-edge of a <see cref="MGContextMenuItem"/> that has a nested <see cref="Submenu"/></summary>
        public const int SubmenuArrowHeight = 8;

        /// <summary>The default empty width between the right edge of an <see cref="MGContextMenuItem"/> and the left edge of the dropdown arrow that appears when the <see cref="MGContextMenuItem"/> has a nested <see cref="Submenu"/></summary>
        public static int DefaultSubmenuArrowRightMargin = 8;

        /// <summary>Provides direct access to the dropdown arrow that appears on the right-edge of this <see cref="MGContextMenuItem"/> if there is a nested <see cref="Submenu"/>.</summary>
        public MGComponent<MGRectangle> SubmenuArrowComponent { get; }
        private MGRectangle SubmenuArrowElement { get; }

        private MGContextMenu _Submenu;
        /// <summary>If not null, represents a nested <see cref="MGContextMenu"/> that will open when hovering over this <see cref="MGContextMenuItem"/>.</summary>
        public MGContextMenu Submenu
        {
            get => _Submenu;
            set
            {
                if (_Submenu != value)
                {
                    if (value != null && value.Host != this.Menu)
                        throw new InvalidOperationException($"Cannot set {nameof(MGContextMenuItem)}.{nameof(Submenu)} to an {nameof(MGContextMenu)} whose parent is not the same as this.{nameof(Menu)}.");

                    if (Submenu != null)
                    {
                        Submenu.ContextMenuOpened -= Submenu_Opened;
                        Submenu.ContextMenuClosed -= Submenu_Closed;
                    }

                    _Submenu = value;
                    NPC(nameof(Submenu));

                    if (Submenu != null)
                    {
                        Submenu.ContextMenuOpened += Submenu_Opened;
                        Submenu.ContextMenuClosed += Submenu_Closed;
                    }

                    ContentWrapper.SpoofIsHoveredWhileDrawingBackground = Submenu?.IsContextMenuOpen == true;

                    SubmenuArrowElement.Visibility = Submenu == null ? Visibility.Collapsed : Visibility.Visible;
                }
            }
        }

        private void Submenu_Opened(object sender, EventArgs e) => ContentWrapper.SpoofIsHoveredWhileDrawingBackground = true;
        private void Submenu_Closed(object sender, EventArgs e) => ContentWrapper.SpoofIsHoveredWhileDrawingBackground = false;

        protected MGWrappedContextMenuItem(MGContextMenu Menu, ContextMenuItemType ItemType, MGButton ContentWrapper, MGElement MenuItemContent)
            : base(Menu, ItemType)
        {
            Container = new(Menu, Orientation.Horizontal);
            Container.Spacing = 5;
            Container.ComponentParent = this;
            Container.CanChangeContent = false;

            HeaderPresenter = new(Menu);
            HeaderPresenter.VerticalAlignment = VerticalAlignment.Center;
            HeaderPresenter.CanChangeContent = false;
            HeaderPresenter.PreferredWidth = Menu.HeaderSize.Width;
            HeaderPresenter.PreferredHeight = Menu.HeaderSize.Height;
            HeaderPresenter.Margin = new(0);
            HeaderPresenter.BackgroundBrush = new(GetTheme(), null);
            HeaderPresenter.ComponentParent = this;

            Menu.HeaderSizeChanged += (sender, e) =>
            {
                HeaderPresenter.PreferredWidth = e.NewValue.Width;
                HeaderPresenter.PreferredHeight = e.NewValue.Height;
            };

            using (Container.AllowChangingContentTemporarily())
            {
                Container.TryAddChild(HeaderPresenter);
            }

            this.MenuItemContent = MenuItemContent;
            this.ContentWrapper = ContentWrapper;

            SubmenuArrowElement = new(Menu, SubmenuArrowWidth, SubmenuArrowHeight, Color.Transparent, 0, Color.Transparent);
            SubmenuArrowElement.Margin = new(0, 5, DefaultSubmenuArrowRightMargin, 5);
            SubmenuArrowElement.Visibility = Visibility.Collapsed;
            SubmenuArrowComponent = new(SubmenuArrowElement, true, true, false, false, false, false, false,
                (AvailableBounds, ComponentSize) => ApplyAlignment(AvailableBounds, HorizontalAlignment.Right, VerticalAlignment.Center, ComponentSize.Size));
            AddComponent(SubmenuArrowComponent);

            Container.OnEndDraw += (sender, e) =>
            {
                Rectangle ArrowElementFullBounds = SubmenuArrowElement.LayoutBounds;
                Rectangle ArrowPartBounds = ApplyAlignment(ArrowElementFullBounds, HorizontalAlignment.Center, VerticalAlignment.Center, new Size(SubmenuArrowWidth, SubmenuArrowHeight));
                List<Vector2> ArrowVertices = new() {
                    ArrowPartBounds.TopLeft().ToVector2(), new(ArrowPartBounds.Right, ArrowPartBounds.Center.Y), ArrowPartBounds.BottomLeft().ToVector2()
                };
                e.DA.DT.FillPolygon(e.DA.Offset.ToVector2(), ArrowVertices, Color.Black * e.DA.Opacity);
            };

            this.Submenu = null;

            void OpenSubmenu()
            {
                if (Submenu?.IsContextMenuOpen == false)
                {
                    Submenu.TryOpenContextMenu(LayoutBounds.TranslatedCopy(AsIMouseHandlerHost().GetOffset().ToPoint()).GetExpanded(new Thickness(2, 0, 2, 0)));
                }
            }

            void CloseSubmenuIfNotHovered()
            {
                if (Submenu?.IsContextMenuOpen == true && !Menu.IsHoveringSubmenu(5))
                {
                    Submenu.TryCloseContextMenu();
                }
            }

            MouseHandler.Entered += (sender, e) => { OpenSubmenu(); };
            MouseHandler.MovedInside += (sender, e) => { OpenSubmenu(); };
            MouseHandler.Scrolled += (sender, e) => { CloseSubmenuIfNotHovered(); };
            MouseHandler.MovedOutside += (sender, e) => { CloseSubmenuIfNotHovered(); };
        }
    }

    /// <summary>Instantiated via <see cref="MGContextMenu.AddButton(MGElement, Action{MGContextMenuButton})"/></summary>
    public class MGContextMenuButton : MGWrappedContextMenuItem
    {
        private MGImage _Icon;
        public MGImage Icon
        {
            get => _Icon;
            set
            {
                if (_Icon != value)
                {
                    _Icon = value;
                    NPC(nameof(Icon));

                    using (HeaderPresenter.AllowChangingContentTemporarily())
                    {
                        HeaderPresenter.SetContent(Icon);
                    }
                }
            }
        }

        /// <summary>Invoked when this <see cref="MGContextMenuButton"/> is left-clicked.</summary>
        public event EventHandler<EventArgs> OnSelected;

        protected override void OnContentWrapperChanged()
        {
            ContentWrapper?.AddCommandHandler((Button, e) =>
            {
                OnSelected?.Invoke(this, e);
            });
        }

        public Action<MGContextMenuButton> Action { get; set; }

        internal MGContextMenuButton(MGContextMenu Menu, MGElement Content, Action<MGContextMenuButton> Action = null)
            : base(Menu, ContextMenuItemType.Button, Menu.ButtonWrapperTemplate(), Content)
        {
            Menu.ButtonWrapperTemplateChanged += (sender, e) =>
            {
                this.ContentWrapper = Menu.ButtonWrapperTemplate();
            };

            OnSelected += (sender, e) => 
            { 
                this.Action?.Invoke(this); 
            };

            this.Action = Action;
        }
    }

    /// <summary>Instantiated via <see cref="MGContextMenu.AddCheckBox(string, bool)"/></summary>
    public class MGContextMenuToggle : MGWrappedContextMenuItem
    {
        private bool _IsChecked;
        public bool IsChecked
        {
            get => _IsChecked;
            set
            {
                if (_IsChecked != value)
                {
                    _IsChecked = value;
                    NPC(nameof(IsChecked));
                    OnToggled?.Invoke(this, IsChecked);
                }
            }
        }

        public event EventHandler<bool> OnToggled;

        protected override void OnContentWrapperChanged()
        {
            ContentWrapper?.AddCommandHandler((Button, e) =>
            {
                this.IsChecked = !this.IsChecked;
            });
        }

        internal MGContextMenuToggle(MGContextMenu Menu, MGElement HeaderContent, bool IsChecked)
            : base(Menu, ContextMenuItemType.Toggle, Menu.ButtonWrapperTemplate(), HeaderContent)
        {
            Menu.ButtonWrapperTemplateChanged += (sender, e) =>
            {
                this.ContentWrapper = Menu.ButtonWrapperTemplate();
            };

            HeaderPresenter.OnEndDraw += (sender, e) =>
            {
                if (this.IsChecked)
                {
                    e.DA.DT.FillRectangle(e.DA.Offset.ToVector2(), HeaderPresenter.LayoutBounds, Color.White * 0.3f * e.DA.Opacity);
                    MGCheckBox.DrawCheckMark(HeaderPresenter.LayoutBounds, e.DA.DT, e.DA.Opacity, e.DA.Offset, Color.Black);
                }
            };

            this.IsChecked = IsChecked;
        }
    }

    /// <summary>Instantiated via <see cref="MGContextMenu.AddSeparator(int)"/></summary>
    public class MGContextMenuSeparator : MGContextMenuItem
    {
        private MGComponent<MGSeparator> SeparatorComponent { get; }
        public MGSeparator SeparatorElement { get; }

        public int Height
        {
            get => SeparatorElement.Size;
            set => SeparatorElement.Size = value;
        }

        internal MGContextMenuSeparator(MGContextMenu Menu, int Height)
            : base(Menu, ContextMenuItemType.Separator)
        {
            this.SeparatorElement = new(Menu, Orientation.Horizontal, Height);
            this.SeparatorElement.Margin = new(0);

            this.SeparatorComponent = new(SeparatorElement, false, false, true, true, false, false, true,
                (AvailableBounds, ComponentSize) => ApplyAlignment(AvailableBounds, HorizontalContentAlignment, VerticalContentAlignment, ComponentSize.Size));
            AddComponent(SeparatorComponent);

            this.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            this.VerticalContentAlignment = VerticalAlignment.Stretch;
        }
    }
}
