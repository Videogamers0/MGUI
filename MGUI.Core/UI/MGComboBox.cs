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
        private MGContentPresenter DropdownArrowElement { get; }

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
            set => DropdownArrowElement.Margin = DropdownArrowElement.Margin.ChangeLeft(value);
        }

        /// <summary>The default empty width between the right edge of the dropdown arrow and the right edge of this <see cref="MGComboBox{TItemType}"/><para/>
        /// See also: <see cref="DefaultDropdownArrowRightMargin"/></summary>
        public int DropdownArrowRightMargin
        {
            get => DropdownArrowElement.Margin.Right;
            set => DropdownArrowElement.Margin = DropdownArrowElement.Margin.ChangeRight(value);
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
                        TemplatedItems.CollectionChanged -= TemplatedItems_CollectionChanged;
                    _TemplatedItems = value;
                    if (TemplatedItems != null)
                        TemplatedItems.CollectionChanged += TemplatedItems_CollectionChanged;

                    DropdownContentChanged();
                    HoveredItem = null;
                }
            }
        }

        private void TemplatedItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            DropdownContentChanged();
        }

        #region Selected Item
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private TemplatedElement<TItemType, MGButton> _SelectedItem;
        public TemplatedElement<TItemType, MGButton> SelectedItem
        {
            get => _SelectedItem;
            set
            {
                if (_SelectedItem != value)
                {
                    if (SelectedItem != null)
                        SelectedItem.Element.IsSelected = false;
                    _SelectedItem = value;
                    UpdateSelectedContent();
                    if (SelectedItem != null)
                        SelectedItem.Element.IsSelected = true;
                }
            }
        }

        private void UpdateSelectedContent()
        {
            MGElement SelectedContent = SelectedItem == null || SelectedItemTemplate == null ? null : SelectedItemTemplate(SelectedItem.SourceData);
            ManagedSetContent(SelectedContent);
        }
        #endregion Selected Item

        #region Hovered Item
        /// <summary>The item within the dropdown that is currently hovered by the mouse, if any.</summary>
        public TemplatedElement<TItemType, MGButton> HoveredItem { get; private set; }

        private void UpdateHoveredDropdownItem()
        {
            if (IsDropdownOpen && TemplatedItems != null)
                HoveredItem = TemplatedItems.FirstOrDefault(x => x.Element.IsHovered);
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
                }
            }
        }

        private void ItemsSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (TemplatedItems != null && DropdownItemTemplate != null)
            {
                if (e.Action is NotifyCollectionChangedAction.Reset)
                {
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
        public static Thickness DefaultDropdownItemPadding = new(8, 5, 8, 5);

        public MGButton CreateDefaultDropdownButton()
        {
            MGButton Button = new(Dropdown, new(0), null);
            Button.Padding = DefaultDropdownItemPadding;
            Button.Margin = 0;
            Button.BackgroundBrush = GetTheme().ComboBoxDropdownItemBackground.GetValue(true);
            Button.HorizontalAlignment = HorizontalAlignment.Stretch;
            Button.HorizontalContentAlignment = HorizontalAlignment.Left;
            Button.VerticalAlignment = VerticalAlignment.Stretch;
            Button.VerticalContentAlignment = VerticalAlignment.Center;
            return Button;
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

        private MGWindow Dropdown { get; }
        private MGScrollViewer DropdownSV { get; }
        private MGStackPanel DropdownSP { get; }

        private bool IsDropdownContentValid;
        private void DropdownContentChanged()
        {
            IsDropdownContentValid = false;
        }

        private void UpdateDropdownContent()
        {
            IsDropdownContentValid = true;

            using (DropdownSP.AllowChangingContentTemporarily())
            {
                foreach (MGElement Element in DropdownSP.Children.ToList())
                    DropdownSP.TryRemoveChild(Element);
                if (TemplatedItems != null)
                {
                    foreach (TemplatedElement<TItemType, MGButton> UIItem in TemplatedItems)
                        DropdownSP.TryAddChild(UIItem.Element);
                }
            }

            int AvailableHeightScreenSpace = GetDesktop().ValidScreenBounds.Bottom - Dropdown.Top;
            int ActualAvailableHeight = (int)(AvailableHeightScreenSpace / Dropdown.Scale);
            int ActualMinHeight = Math.Clamp(MinDropdownHeight, 0, ActualAvailableHeight);
            int ActualMaxHeight = Math.Clamp(MaxDropdownHeight, 0, ActualAvailableHeight);
            Dropdown.ApplySizeToContent(SizeToContent.WidthAndHeight, Math.Max(this.ActualWidth, MinDropdownWidth), ActualMinHeight, MaxDropdownWidth, ActualMaxHeight);
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

                    DropdownOpened?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler<CancelEventArgs> DropdownOpening;
        public event EventHandler<EventArgs> DropdownOpened;

        /// <summary>The color of the inverted triangle on the right-side of this <see cref="MGComboBox{TItemType}"/>.<para/>
        /// Default value: <see cref="MGTheme.DropdownArrowColor"/><para/>
        /// See also:<br/><see cref="MGWindow.Theme"/><br/><see cref="MGDesktop.Theme"/></summary>
        public Color DropdownArrowColor { get; set; }

        private void ManagedSetContent(MGElement Content)
        {
            using (AllowChangingContentTemporarily())
            {
                SetContent(Content);
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
                        e.SetHandled(Dropdown, false);
                        this.SelectedItem = HoveredItem;
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
                            e.SetHandled(Dropdown, false);
                        }
                    }
                };
                Dropdown.WindowMouseHandler.MovedInside += (sender, e) => { UpdateHoveredDropdownItem(); };
                Dropdown.WindowMouseHandler.Scrolled += (sender, e) => { UpdateHoveredDropdownItem(); };

                Dropdown.BorderThickness = new(1);
                Dropdown.BorderBrush = MGUniformBorderBrush.Gray;
                Dropdown.BackgroundBrush = GetTheme().ComboBoxDropdownBackground.GetValue(true);
                Dropdown.Padding = new(0);

                DropdownSP = new(Dropdown, Orientation.Vertical);
                DropdownSP.Spacing = 0;
                DropdownSP.CanChangeContent = false;
                DropdownSP.ManagedParent = Dropdown;
                DropdownSV = new(Dropdown, ScrollBarVisibility.Auto, ScrollBarVisibility.Disabled);
                DropdownSV.Padding = new(0);
                DropdownSV.SetContent(DropdownSP);
                DropdownSV.CanChangeContent = false;
                DropdownSV.ManagedParent = Dropdown;
                Dropdown.SetContent(DropdownSV);
                Dropdown.CanChangeContent = false;

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
                    e.SetHandled(this, false);
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
        internal void LoadSettings(ComboBox Settings)
        {
            Settings.Border.ApplySettings(this, BorderComponent.Element);
            Settings.DropdownArrow.ApplySettings(this, DropdownArrowComponent.Element);

            if (Settings.DropdownArrowColor.HasValue)
                DropdownArrowColor = Settings.DropdownArrowColor.Value.ToXNAColor();

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
        }
    }
}
