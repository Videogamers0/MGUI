using Microsoft.Xna.Framework;
using MGUI.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoGame.Extended;
using MGUI.Core.UI.Containers;
using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.XAML;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;
using Thickness = MonoGame.Extended.Thickness;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Size = MonoGame.Extended.Size;
using MGUI.Core.UI.Data_Binding;

namespace MGUI.Core.UI
{
    public record class TemplatedElement<TDataType, TElementType>(TDataType SourceData, TElementType Element);

    /// <typeparam name="TItemType">The type that the ItemsSource will be bound to. Usually this would be: <see cref="string"/> for simple text-choices</typeparam>
    public class MGComboBox<TItemType> : MGSingleContentHost
    {
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

        #region Dropdown Arrow
        /// <summary>Provides direct access to the dropdown part of this combobox.</summary>
        public MGComponent<MGContentPresenter> DropdownArrowComponent { get; }
        public MGContentPresenter DropdownArrowElement { get; }

        /// <summary>The width of the <see cref="MGContentPresenter"/> that hosts the dropdown arrow. This should be >= <see cref="DropdownArrowWidth"/></summary>
        public const int DropdownArrowPaddedWidth = 16;
        /// <summary>The width of the dropdown arrow</summary>
        public const int DropdownArrowWidth = 10;
        /// <summary>The height of the <see cref="MGContentPresenter"/> that hosts the dropdown arrow. This should be >= <see cref="DropdownArrowHeight"/></summary>
        public const int DropdownArrowPaddedHeight = 16;
        /// <summary>The height of the dropdown arrow</summary>
        public const int DropdownArrowHeight = 6;

        /// <summary>The default empty width between the right edge of the <see cref="MGSingleContentHost.Content"/> and the left edge of the dropdown arrow</summary>
        public static int DefaultDropdownArrowLeftMargin { get; set; } = 7;
        /// <summary>The default empty width between the right edge of the dropdown arrow and the right edge of this <see cref="MGComboBox{TItemType}"/></summary>
        public static int DefaultDropdownArrowRightMargin { get; set; } = 5;

        /// <summary>The default empty width between the right edge of the <see cref="MGSingleContentHost.Content"/> and the left edge of the dropdown arrow<para/>
        /// See also: <see cref="DefaultDropdownArrowLeftMargin"/></summary>
        public int DropdownArrowLeftMargin
        {
            get => DropdownArrowElement.Margin.Left;
            set
            {
                if (DropdownArrowElement.Margin.Left != value)
                {
                    DropdownArrowElement.Margin = DropdownArrowElement.Margin.ChangeLeft(value);
                    NPC(nameof(DropdownArrowLeftMargin));
                }
            }
        }

        /// <summary>The default empty width between the right edge of the dropdown arrow and the right edge of this <see cref="MGComboBox{TItemType}"/><para/>
        /// See also: <see cref="DefaultDropdownArrowRightMargin"/></summary>
        public int DropdownArrowRightMargin
        {
            get => DropdownArrowElement.Margin.Right;
            set
            {
                if (DropdownArrowElement.Margin.Right != value)
                {
                    DropdownArrowElement.Margin = DropdownArrowElement.Margin.ChangeRight(value);
                    NPC(nameof(DropdownArrowRightMargin));
                }
            }
        }
        #endregion Dropdown Arrow

        #region Templated Items
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ObservableCollection<TemplatedElement<TItemType, MGButton>> _TemplatedItems;
        private ObservableCollection<TemplatedElement<TItemType, MGButton>> TemplatedItems
        {
            get => _TemplatedItems;
            set
            {
                if (_TemplatedItems != value)
                {
                    if (TemplatedItems != null)
                    {
                        HandleTemplatedContentRemoved(TemplatedItems.Select(x => x.Element));
                        TemplatedItems.CollectionChanged -= TemplatedItems_CollectionChanged;
                    }
                    _TemplatedItems = value;
                    if (TemplatedItems != null)
                        TemplatedItems.CollectionChanged += TemplatedItems_CollectionChanged;

                    DropdownContentChanged();
                    HoveredItem = null;
                }
            }
        }

        private static void HandleTemplatedContentRemoved(IEnumerable<MGButton> Items)
        {
            if (Items != null)
            {
                foreach (var Item in Items)
                    Item.RemoveDataBindings(true);
            }
        }

        private void TemplatedItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            DropdownContentChanged();

            if (e.Action is NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Replace && e.OldItems != null)
            {
                HandleTemplatedContentRemoved(e.OldItems.Cast<TemplatedElement<TItemType, MGButton>>().Select(x => x.Element));
            }
        }

        #region Selected Item
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private TemplatedElement<TItemType, MGButton> _SelectedTemplatedItem;
        private TemplatedElement<TItemType, MGButton> SelectedTemplatedItem
        {
            get => _SelectedTemplatedItem;
            set
            {
                if (_SelectedTemplatedItem != value)
                {
                    TemplatedElement<TItemType, MGButton> PreviousSelection = SelectedTemplatedItem;
                    if (SelectedTemplatedItem != null)
                        SelectedTemplatedItem.Element.IsSelected = false;
                    _SelectedTemplatedItem = value;
                    UpdateSelectedContent();
                    if (SelectedTemplatedItem != null)
                        SelectedTemplatedItem.Element.IsSelected = true;

                    NPC(nameof(SelectedTemplatedItem));
                    NPC(nameof(SelectedItem));
                    NPC(nameof(SelectedIndex));

                    SelectedItemChanged?.Invoke(this, new(
                        PreviousSelection == null ? default(TItemType) : PreviousSelection.SourceData,
                        SelectedTemplatedItem == null ? default(TItemType) : SelectedTemplatedItem.SourceData
                    ));
                }
            }
        }

        private readonly EqualityComparer<TItemType> Comparer = EqualityComparer<TItemType>.Default;
        /// <summary>Warning - If <see cref="ItemsSource"/> contains several items with the same hash code, this property's setter function will always select the first matching value.<para/>
        /// See also: <see cref="SelectedIndex"/></summary>
        public TItemType SelectedItem
        {
            get => SelectedTemplatedItem == null ? default(TItemType) : SelectedTemplatedItem.SourceData;
            set => SelectedTemplatedItem = TemplatedItems.FirstOrDefault(x => Comparer.Equals(value, x.SourceData));
        }

        /// <summary>The index of the currently-selected item.<para/>
        /// See also: <see cref="SelectedItem"/></summary>
        public int SelectedIndex
        {
            get => SelectedTemplatedItem == null ? -1 : TemplatedItems.IndexOf(SelectedTemplatedItem);
            set => SelectedTemplatedItem = TemplatedItems[value];
        }

        public event EventHandler<EventArgs<TItemType>> SelectedItemChanged;

        private void UpdateSelectedContent()
        {
            Content?.RemoveDataBindings(true);
            MGElement SelectedContent = SelectedTemplatedItem == null || SelectedItemTemplate == null ? null : SelectedItemTemplate(SelectedTemplatedItem.SourceData);
            ManagedSetContent(SelectedContent);
        }
        #endregion Selected Item

        #region Hovered Item
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private TemplatedElement<TItemType, MGButton> _HoveredItem;
        /// <summary>The item within the dropdown that is currently hovered by the mouse, if any.</summary>
        public TemplatedElement<TItemType, MGButton> HoveredItem
        {
            get => _HoveredItem;
            private set
            {
                if (_HoveredItem != value)
                {
                    TemplatedElement<TItemType, MGButton> Previous = HoveredItem;
                    _HoveredItem = value;
                    NPC(nameof(HoveredItem));
                    HoveredItemChanged?.Invoke(this, new(Previous, HoveredItem));
                }
            }
        }

        public event EventHandler<EventArgs<TemplatedElement<TItemType, MGButton>>> HoveredItemChanged;

        private void UpdateHoveredDropdownItem()
        {
            if (IsDropdownOpen && TemplatedItems != null && DropdownStackPanel.IsHovered)
                HoveredItem = TemplatedItems.FirstOrDefault(x => x.Element.VisualState.IsHovered);
            else
                HoveredItem = null;
        }
        #endregion Hovered Item
        #endregion Templated Items

        #region Items Source
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ObservableCollection<TItemType> _ItemsSource;
        public ObservableCollection<TItemType> ItemsSource
        {
            get => _ItemsSource;
            private set
            {
                if (_ItemsSource != value)
                {
                    if (ItemsSource != null)
                        ItemsSource.CollectionChanged -= ItemsSource_CollectionChanged;
                    _ItemsSource = value;
                    if (ItemsSource != null)
                        ItemsSource.CollectionChanged += ItemsSource_CollectionChanged;

                    if (ItemsSource == null || DropdownItemTemplate == null)
                        TemplatedItems = null;
                    else
                    {
                        IEnumerable<TemplatedElement<TItemType, MGButton>> Values = ItemsSource.Select(x => new TemplatedElement<TItemType, MGButton>(x, DropdownItemTemplate(x)));
                        this.TemplatedItems = new ObservableCollection<TemplatedElement<TItemType, MGButton>>(Values);
                    }

                    NPC(nameof(ItemsSource));
                }
            }
        }

        private void ItemsSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (TemplatedItems != null && DropdownItemTemplate != null)
            {
                if (e.Action is NotifyCollectionChangedAction.Reset)
                {
                    HandleTemplatedContentRemoved(TemplatedItems.Select(x => x.Element));
                    TemplatedItems.Clear();
                }
                else if (e.Action is NotifyCollectionChangedAction.Add && e.NewItems != null)
                {
                    int CurrentIndex = e.NewStartingIndex;
                    foreach (TItemType Item in e.NewItems)
                    {
                        TemplatedElement<TItemType, MGButton> TemplatedItem = new(Item, DropdownItemTemplate(Item));
                        TemplatedItems.Insert(CurrentIndex, TemplatedItem);
                        CurrentIndex++;
                    }
                }
                else if (e.Action is NotifyCollectionChangedAction.Remove && e.OldItems != null)
                {
                    int CurrentIndex = e.OldStartingIndex;
                    foreach (var Item in e.OldItems)
                    {
                        TemplatedItems.RemoveAt(CurrentIndex);
                        CurrentIndex++;
                    }
                }
                else if (e.Action is NotifyCollectionChangedAction.Replace)
                {
                    List<TItemType> Old = e.OldItems.Cast<TItemType>().ToList();
                    List<TItemType> New = e.NewItems.Cast<TItemType>().ToList();
                    for (int i = 0; i < Old.Count; i++)
                    {
                        TItemType NewItem = New[i]; 
                        TemplatedElement<TItemType, MGButton> TemplatedItem = new(NewItem, DropdownItemTemplate(NewItem));
                        TemplatedItems[e.OldStartingIndex + i] = TemplatedItem;
                    }
                }
                else if (e.Action is NotifyCollectionChangedAction.Move)
                {
                    throw new NotImplementedException();
                }
            }
        }

        /// <param name="Value"><see cref="ItemsSource"/> will be set to a copy of this <see cref="ICollection{T}"/>.<br/>
        /// If you want <see cref="ItemsSource"/> to dynamically update as the collection changes, pass in an <see cref="ObservableCollection{T}"/></param>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="IsDropdownOpen"/>=true.</exception>
        public void SetItemsSource(ICollection<TItemType> Value)
        {
            if (Value is ObservableCollection<TItemType> Observable)
                this.ItemsSource = Observable;
            else
                this.ItemsSource = new ObservableCollection<TItemType>(Value.ToList());
        }
        #endregion Items Source

        #region Item Template
        /// <summary>The default amount of padding in each item within the dropdown.</summary>
        public static Thickness DefaultDropdownItemPadding { get; set; } = new(8, 5, 8, 5);

        /// <summary>See also: <see cref="ApplyDefaultDropdownButtonSettings(MGButton)"/></summary>
        public MGButton CreateDefaultDropdownButton()
        {
            MGButton Button = new(Dropdown, new(0), null);
            ApplyDefaultDropdownButtonSettings(Button);
            return Button;
        }

        public void ApplyDefaultDropdownButtonSettings(MGButton Target)
        {
            Target.Padding = DefaultDropdownItemPadding;
            Target.Margin = 0;
            Target.BackgroundBrush = GetTheme().ComboBoxDropdownItemBackground.GetValue(true);
            Target.HorizontalAlignment = HorizontalAlignment.Stretch;
            Target.HorizontalContentAlignment = HorizontalAlignment.Left;
            Target.VerticalAlignment = VerticalAlignment.Stretch;
            Target.VerticalContentAlignment = VerticalAlignment.Center;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Func<TItemType, MGButton> _DropdownItemTemplate;
        /// <summary>The template to use for items inside the dropdown.<br/>
        /// Highly recommend to use an <see cref="MGElement"/> with Padding, such as '8,5,8,5'. See also: <see cref="DefaultDropdownItemPadding"/><para/>
        /// Default value: <see cref="CreateDefaultDropdownButton"/>, which is then populated with an <see cref="MGTextBlock"/> whose Text is given by the <typeparamref name="TItemType"/>'s <see cref="object.ToString"/><para/>
        /// <code>
        /// DropdownItemTemplate = item =>
        /// {
        ///     <see cref="MGButton"/> Button = <see cref="CreateDefaultDropdownButton"/>;
        ///     Button.SetContent(item.ToString());
        ///     return Button;
        /// }
        /// </code></summary>
        public Func<TItemType, MGButton> DropdownItemTemplate
        {
            get => _DropdownItemTemplate;
            set
            {
                if (_DropdownItemTemplate != value)
                {
                    _DropdownItemTemplate = value;

                    if (ItemsSource == null || DropdownItemTemplate == null)
                        TemplatedItems = null;
                    else
                    {
                        IEnumerable<TemplatedElement<TItemType, MGButton>> Values = ItemsSource.Select(x => new TemplatedElement<TItemType, MGButton>(x, DropdownItemTemplate(x)));
                        this.TemplatedItems = new ObservableCollection<TemplatedElement<TItemType, MGButton>>(Values);
                    }

                    NPC(nameof(DropdownItemTemplate));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Func<TItemType, MGElement> _SelectedItemTemplate;
        /// <summary>The template to use for the selected item<para/>
        /// Default value:<para/>
        /// <code>
        /// SelectedItemTemplate = item => new <see cref="MGTextBlock"/>(Window, item.ToString()) { WrapText = false, VerticalAlignment = <see cref="VerticalAlignment.Center"/> };
        /// </code></summary>
        public Func<TItemType, MGElement> SelectedItemTemplate
        {
            get => _SelectedItemTemplate;
            set
            {
                if (_SelectedItemTemplate != value)
                {
                    _SelectedItemTemplate = value;
                    UpdateSelectedContent();
                    NPC(nameof(SelectedItemTemplate));
                }
            }
        }
        #endregion Item Template

        #region Dropdown
        /// <summary>When the dropdown is opened, it will attempt to use the <see cref="MGElement.ActualWidth"/> of this <see cref="MGComboBox{TItemType}"/>,<br/>
        /// but the width will be clamped to the range [<see cref="MinDropdownWidth"/>, <see cref="MaxDropdownWidth"/>]</summary>
        public int MinDropdownWidth { get; set; } = 180;
        /// <summary>When the dropdown is opened, it will attempt to use the <see cref="MGElement.ActualWidth"/> of this <see cref="MGComboBox{TItemType}"/>,<br/>
        /// but the width will be clamped to the range [<see cref="MinDropdownWidth"/>, <see cref="MaxDropdownWidth"/>]</summary>
        public int MaxDropdownWidth { get; set; } = 1000;
        public int MinDropdownHeight { get; set; } = 100;
        public int MaxDropdownHeight { get; set; } = 360;

        /// <summary>The floating <see cref="MGWindow"/> used to display the content inside of the dropdown when <see cref="IsDropdownOpen"/> is true.<para/>
        /// Warning - be careful when editing properties on this object. Some changes could break the combobox's functionality,<br/>
        /// such as setting <see cref="MGWindow.IsTitleBarVisible"/> and <see cref="MGWindow.IsCloseButtonVisible"/> to true, and then clicking the close button.<para/>
        /// See also: <see cref="DropdownScrollViewer"/>, <see cref="DropdownStackPanel"/></summary>
        public MGWindow Dropdown { get; }
        /// <summary>The <see cref="MGScrollViewer"/> that the <see cref="Dropdown"/>'s Content is wrapped in.<para/>
        /// See also: <see cref="Dropdown"/>, <see cref="DropdownStackPanel"/></summary>
        public MGScrollViewer DropdownScrollViewer { get; }
        /// <summary>The <see cref="MGStackPanel"/> that the <see cref="ItemsSource"/>'s rows are added to.<para/>
        /// See also: <see cref="Dropdown"/>, <see cref="DropdownScrollViewer"/></summary>
        public MGStackPanel DropdownStackPanel { get; }
        private MGDockPanel DropdownDockPanel { get; }

        private bool IsDropdownContentValid;
        private void DropdownContentChanged()
        {
            IsDropdownContentValid = false;
        }

        private void UpdateDropdownContent()
        {
            IsDropdownContentValid = true;

            using (DropdownStackPanel.AllowChangingContentTemporarily())
            {
                foreach (MGElement Element in DropdownStackPanel.Children.ToList())
                    DropdownStackPanel.TryRemoveChild(Element);
                if (TemplatedItems != null)
                {
                    foreach (TemplatedElement<TItemType, MGButton> UIItem in TemplatedItems)
                        DropdownStackPanel.TryAddChild(UIItem.Element);
                }
            }

            int AvailableHeightScreenSpace = GetDesktop().ValidScreenBounds.Bottom - Dropdown.Top;
            int ActualAvailableHeight = (int)(AvailableHeightScreenSpace / Dropdown.Scale);
            int ActualMaxHeight = Math.Clamp(MaxDropdownHeight, 0, ActualAvailableHeight);
            int ActualMinHeight = Math.Clamp(MinDropdownHeight, 0, ActualMaxHeight);
            int ActualMinWidth = Math.Clamp(Math.Max(this.ActualWidth, MinDropdownWidth), 0, MaxDropdownWidth);
            Dropdown.ApplySizeToContent(SizeToContent.WidthAndHeight, ActualMinWidth, ActualMinHeight, MaxDropdownWidth, ActualMaxHeight);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _IsDropdownOpen;
        public bool IsDropdownOpen
        {
            get => _IsDropdownOpen;
            set
            {
                if (_IsDropdownOpen != value)
                {
                    CancelEventArgs e = new CancelEventArgs(false);
                    DropdownOpening?.Invoke(this, e);
                    if (e.Cancel)
                        return;

                    _IsDropdownOpen = value;

                    if (IsDropdownOpen)
                    {
                        Point TopLeft = ConvertCoordinateSpace(CoordinateSpace.Layout, CoordinateSpace.Screen, LayoutBounds.BottomLeft());
                        Dropdown.Left = TopLeft.X;
                        Dropdown.Top = TopLeft.Y;
                        UpdateDropdownContent();
                        ParentWindow.AddNestedWindow(Dropdown);
                    }
                    else
                        ParentWindow.RemoveNestedWindow(Dropdown);

                    HoveredItem = null;

                    NPC(nameof(IsDropdownOpen));
                    DropdownOpened?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler<CancelEventArgs> DropdownOpening;
        public event EventHandler<EventArgs> DropdownOpened;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Color _DropdownArrowColor;
        /// <summary>The color of the inverted triangle on the right-side of this <see cref="MGComboBox{TItemType}"/>.<para/>
        /// Default value: <see cref="MGTheme.DropdownArrowColor"/><para/>
        /// See also:<br/><see cref="MGWindow.Theme"/><br/><see cref="MGDesktop.Theme"/></summary>
        public Color DropdownArrowColor
        {
            get => _DropdownArrowColor;
            set
            {
                if (_DropdownArrowColor != value)
                {
                    _DropdownArrowColor = value;
                    NPC(nameof(DropdownArrowColor));
                }
            }
        }

        private void ManagedSetContent(MGElement Content)
        {
            using (AllowChangingContentTemporarily())
            {
                SetContent(Content);
            }
        }

        private MGContentPresenter DropdownHeaderPresenter { get; }
        private MGContentPresenter DropdownFooterPresenter { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private MGElement _DropdownHeader;
        /// <summary>Optional content that is displayed at the top of the <see cref="Dropdown"/> window.<para/>
        /// Recommended to use a bottom margin or padding to visually separate the <see cref="DropdownHeader"/> from the items list.<para/>
        /// See also: <see cref="DropdownFooter"/></summary>
        public MGElement DropdownHeader
        {
            get => _DropdownHeader;
            set
            {
                if (_DropdownHeader != value)
                {
                    _DropdownHeader = value;
                    using (DropdownHeaderPresenter.AllowChangingContentTemporarily())
                    {
                        DropdownHeaderPresenter.SetContent(DropdownHeader);
                    }
                    NPC(nameof(DropdownHeader));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private MGElement _DropdownFooter;
        /// <summary>Optional content that is displayed at the bottom of the <see cref="Dropdown"/> window.<para/>
        /// Recommended to use a top margin or padding to visually separate the <see cref="DropdownFooter"/> from the items list.<para/>
        /// See also: <see cref="DropdownHeader"/></summary>
        public MGElement DropdownFooter
        {
            get => _DropdownFooter;
            set
            {
                if (_DropdownFooter != value)
                {
                    _DropdownFooter = value;
                    using (DropdownFooterPresenter.AllowChangingContentTemporarily())
                    {
                        DropdownFooterPresenter.SetContent(DropdownFooter);
                    }
                    NPC(nameof(DropdownFooter));
                }
            }
        }
        #endregion Dropdown

        public MGComboBox(MGWindow Window)
            : this(Window, new(1), MGUniformBorderBrush.Black) { }

        public MGComboBox(MGWindow Window, Thickness BorderThickness, IFillBrush BorderBrush)
            : this(Window, BorderThickness, new MGUniformBorderBrush(BorderBrush)) { }

        public MGComboBox(MGWindow Window, Thickness BorderThickness, IBorderBrush BorderBrush)
            : base(Window, MGElementType.ComboBox)
        {
            using (BeginInitializing())
            {
                this.BorderElement = new(Window, BorderThickness, BorderBrush);
                this.BorderComponent = MGComponentBase.Create(BorderElement);
                AddComponent(BorderComponent);
                BorderElement.OnBorderBrushChanged += (sender, e) => { NPC(nameof(BorderBrush)); };
                BorderElement.OnBorderThicknessChanged += (sender, e) => { NPC(nameof(BorderThickness)); };

                this.DropdownArrowElement = new(Window) { PreferredWidth = DropdownArrowPaddedWidth, PreferredHeight = DropdownArrowPaddedHeight };
                this.DropdownArrowComponent = new(DropdownArrowElement, false, true, false, true, true, false, false,
                    (AvailableBounds, ComponentSize) => ApplyAlignment(AvailableBounds, HorizontalAlignment.Right, VerticalAlignment.Center, ComponentSize.Size));
                AddComponent(DropdownArrowComponent);

                this.DropdownArrowElement.Margin = new(DefaultDropdownArrowLeftMargin, 0, DefaultDropdownArrowRightMargin, 0);
                this.DropdownArrowColor = GetTheme().DropdownArrowColor;

                DropdownArrowElement.OnEndingDraw += (sender, e) =>
                {
                    Rectangle ArrowElementFullBounds = DropdownArrowElement.LayoutBounds;
                    Rectangle ArrowPartBounds = ApplyAlignment(ArrowElementFullBounds, HorizontalAlignment.Center, VerticalAlignment.Center, new Size(DropdownArrowWidth, DropdownArrowHeight));
                    List<Vector2> ArrowVertices = new() {
                        ArrowPartBounds.TopLeft().ToVector2(), ArrowPartBounds.TopRight().ToVector2(), new(ArrowPartBounds.Center.X, ArrowPartBounds.Bottom)
                    };
                    e.DA.DT.FillPolygon(e.DA.Offset.ToVector2(), ArrowVertices, this.DropdownArrowColor * e.DA.Opacity);
                };

                this.HorizontalContentAlignment = HorizontalAlignment.Center;
                this.VerticalContentAlignment = VerticalAlignment.Center;
                this.Padding = new(4, 2, 4, 2);

                this.MinHeight = 26;

                this.CanChangeContent = false;

                SelfOrParentWindow.ScaleChanged += (sender, e) =>
                {
                    Dropdown.Scale = e.NewValue;
                };

                Dropdown = new(Window, 0, 0, 100, 300, SelfOrParentWindow.Theme)
                {
                    ManagedParent = this,
                    IsUserResizable = false,
                    IsTitleBarVisible = false,
                    Scale = SelfOrParentWindow.Scale
                };
                Dropdown.WindowMouseHandler.LMBReleasedInside += (sender, e) =>
                {
                    if (HoveredItem != null)
                    {
                        e.SetHandledBy(Dropdown, false);
                        this.SelectedTemplatedItem = HoveredItem;
                        IsDropdownOpen = false;
                    }
                };
                Dropdown.WindowMouseHandler.ReleasedOutside += (sender, e) =>
                {
                    if (IsDropdownOpen)
                    {
                        Point LayoutSpacePosition = ConvertCoordinateSpace(CoordinateSpace.Screen, CoordinateSpace.Layout, e.Position);
                        if (!Dropdown.RenderBounds.ContainsInclusive(LayoutSpacePosition))
                        {
                            IsDropdownOpen = false;
                            e.SetHandledBy(Dropdown, false);
                        }
                    }
                };

                Dropdown.BorderThickness = new(1);
                Dropdown.BorderBrush = MGUniformBorderBrush.Gray;
                Dropdown.BackgroundBrush = GetTheme().ComboBoxDropdownBackground.GetValue(true);
                Dropdown.Padding = new(0);

                //  Create the placeholders for the Header and Footer
                DropdownHeaderPresenter = new(Dropdown);
                DropdownHeaderPresenter.CanChangeContent = false;
                DropdownFooterPresenter = new(Dropdown);
                DropdownFooterPresenter.CanChangeContent = false;

                //  Create the StackPanel and ScrollViewer that host the items list
                DropdownStackPanel = new(Dropdown, Orientation.Vertical);
                DropdownStackPanel.Spacing = 0;
                DropdownStackPanel.CanChangeContent = false;
                DropdownStackPanel.ManagedParent = Dropdown;
                DropdownScrollViewer = new(Dropdown, ScrollBarVisibility.Auto, ScrollBarVisibility.Disabled);
                DropdownScrollViewer.Padding = new(0);
                DropdownScrollViewer.SetContent(DropdownStackPanel);
                DropdownScrollViewer.CanChangeContent = false;
                DropdownScrollViewer.ManagedParent = Dropdown;

                //  Set the dropdown's content
                DropdownDockPanel = new(Dropdown, true);
                DropdownDockPanel.TryAddChild(DropdownHeaderPresenter, Dock.Top);
                DropdownDockPanel.TryAddChild(DropdownFooterPresenter, Dock.Bottom);
                DropdownDockPanel.TryAddChild(DropdownScrollViewer, Dock.Top);
                DropdownDockPanel.CanChangeContent = false;
                Dropdown.SetContent(DropdownDockPanel);
                Dropdown.CanChangeContent = false;

                Dropdown.HoveredElementChanged += (sender, e) => { UpdateHoveredDropdownItem(); };

                DropdownItemTemplate = item =>
                {
                    MGButton Button = CreateDefaultDropdownButton();
                    Button.SetContent(item.ToString());
                    return Button;
                };
                SelectedItemTemplate = item => new MGTextBlock(Window, item.ToString()) { WrapText = false, VerticalAlignment = VerticalAlignment.Center };

                MouseHandler.LMBReleasedInside += (sender, e) =>
                {
                    IsDropdownOpen = !IsDropdownOpen;
                    e.SetHandledBy(this, false);
                };
            }
        }

        public override void UpdateSelf(ElementUpdateArgs UA)
        {
            base.UpdateSelf(UA);

            if (IsDropdownOpen)
            {
                if (!IsDropdownContentValid)
                    UpdateDropdownContent();

                //  Example scenario: ComboBox is inside a vertically-scrolling ScrollViewer
                //  ComboBox is visible. User clicks it to open dropdown, then they scroll the ScrollViewer up until the ComboBox is no longer visible
                //  Should we auto-hide (or close) the dropdown?
                //Dropdown.Visibility = RecentDrawWasClipped ? Visibility.Collapsed : Visibility.Visible;

                Point TopLeft = ConvertCoordinateSpace(CoordinateSpace.Layout, CoordinateSpace.Screen, LayoutBounds.BottomLeft());
                Dropdown.Left = TopLeft.X;
                Dropdown.Top = TopLeft.Y;
                Dropdown.ValidateWindowSizeAndPosition();
            }
        }

        //  This method is invoked via reflection in MGUI.Core.UI.XAML.Controls.ComboBox.ApplyDerivedSettings.
        //  Do not modify the method signature.
        internal void LoadSettings(ComboBox Settings, bool IncludeContent)
        {
            Settings.Border.ApplySettings(this, BorderComponent.Element, false);
            Settings.DropdownArrow.ApplySettings(this, DropdownArrowComponent.Element, IncludeContent);

            if (Settings.DropdownArrowColor.HasValue)
                DropdownArrowColor = Settings.DropdownArrowColor.Value.ToXNAColor();

            if (Settings.MinDropdownWidth.HasValue)
                MinDropdownWidth = Settings.MinDropdownWidth.Value;
            if (Settings.MaxDropdownWidth.HasValue)
                MaxDropdownWidth = Settings.MaxDropdownWidth.Value;
            if (Settings.MinDropdownHeight.HasValue)
                MinDropdownHeight = Settings.MinDropdownHeight.Value;
            if (Settings.MaxDropdownHeight.HasValue)
                MaxDropdownHeight = Settings.MaxDropdownHeight.Value;

            Settings.Dropdown?.ApplySettings(Dropdown.Parent, Dropdown, false);
            Settings.DropdownScrollViewer?.ApplySettings(DropdownScrollViewer.Parent, DropdownScrollViewer, false);
            Settings.DropdownStackPanel?.ApplySettings(DropdownStackPanel.Parent, DropdownStackPanel, false);

            if (Settings.DropdownHeader != null)
                DropdownHeader = Settings.DropdownHeader.ToElement<MGElement>(Dropdown, DropdownHeaderPresenter);
            if (Settings.DropdownFooter != null)
                DropdownFooter = Settings.DropdownFooter.ToElement<MGElement>(Dropdown, DropdownFooterPresenter);

            if (Settings.Items?.Any() == true)
            {
                List<TItemType> TempItems = new();
                Type TargetType = typeof(TItemType);
                foreach (object Item in Settings.Items)
                {
                    if (TargetType.IsAssignableFrom(Item.GetType()))
                    {
                        TItemType Value = (TItemType)Item;
                        TempItems.Add(Value);
                    }
                }

                if (TempItems.Any())
                {
                    SetItemsSource(TempItems);
                }
            }

            if (Settings.DropdownItemTemplate != null)
            {
                this.DropdownItemTemplate = (Item) =>
                {
                    MGElement Content = Settings.DropdownItemTemplate.GetContent(Dropdown, this, Item, x =>
                    {
                        if (x is MGButton ButtonContent)
                            ApplyDefaultDropdownButtonSettings(ButtonContent);
                    });

                    if (Content is MGButton ButtonContent)
                    {
                        return ButtonContent;
                    }
                    else
                    {
                        MGButton Button = CreateDefaultDropdownButton();
                        Button.SetContent(Content);
                        return Button;
                    }
                };
            }

            if (Settings.SelectedItemTemplate != null)
            {
                this.SelectedItemTemplate = (Item) => Settings.SelectedItemTemplate.GetContent(SelfOrParentWindow, this, Item);
            }
            //  Use the same template for the selected item if only a DropdownItemTemplate is specified
            else if (Settings.DropdownItemTemplate != null)
            {
                this.SelectedItemTemplate = (Item) => Settings.DropdownItemTemplate.GetContent(SelfOrParentWindow, this, Item);
            }

            if (Settings.SelectedIndex.HasValue)
                this.SelectedIndex = Settings.SelectedIndex.Value;
        }
    }
}
