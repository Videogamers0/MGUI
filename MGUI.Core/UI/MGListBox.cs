using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.Containers;
using MGUI.Core.UI.XAML;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thickness = MonoGame.Extended.Thickness;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using MGUI.Shared.Helpers;

namespace MGUI.Core.UI
{
    public enum ListBoxSelectionMode
    {
        /// <summary>Items cannot be selected</summary>
        None,
        /// <summary>A single item may be selected at a time, by left-clicking it</summary>
        Single,
        /// <summary>A set of consecutive items may be selected at once. Left-click to replace selection with a single item.<br/>
        /// Shift+Left-click to select all consecutive items between the current selection source and the clicked item.</summary>
        Contiguous,
        /// <summary>Any number of items may be selected at once. Left-click to replace selection with a single item.<br/>
        /// Ctrl+Left-click to toggle the selection state of the clicked item.</summary>
        Multiple
    }

    /// <typeparam name="TItemType">The type that the ItemsSource will be bound to.</typeparam>
    public class MGListBox<TItemType> : MGElement
    {
        #region Outer Border
        private MGComponent<MGBorder> OuterBorderComponent { get; }
        /// <summary><see cref="MGListBox{TItemType}"/>es contain 3 borders:<para/>
        /// 1. <see cref="OuterBorder"/>: Wrapped around the entire <see cref="MGListBox{TItemType}"/><br/>
        /// 2. <see cref="InnerBorder"/>: Wrapped around the <see cref="ItemsPanel"/>, but not the <see cref="TitleComponent"/><br/>
        /// 3. <see cref="TitleBorder"/>: Wrapped around the <see cref="TitleComponent"/></summary>
        public MGBorder OuterBorder { get; }
        public override MGBorder GetBorder() => OuterBorder;

        public IBorderBrush OuterBorderBrush
        {
            get => OuterBorder.BorderBrush;
            set
            {
                if (OuterBorderBrush != value)
                {
                    OuterBorder.BorderBrush = value;
                    NPC(nameof(OuterBorderBrush));
                }
            }
        }

        public Thickness OuterBorderThickness
        {
            get => OuterBorder.BorderThickness;
            set
            {
                if (!OuterBorderThickness.Equals(value))
                {
                    OuterBorder.BorderThickness = value;
                    NPC(nameof(OuterBorderThickness));
                }
            }
        }
        #endregion Outer Border

        #region Inner Border
        private MGComponent<MGBorder> InnerBorderComponent { get; }
        /// <summary><see cref="MGListBox{TItemType}"/>es contain 3 borders:<para/>
        /// 1. <see cref="OuterBorder"/>: Wrapped around the entire <see cref="MGListBox{TItemType}"/><br/>
        /// 2. <see cref="InnerBorder"/>: Wrapped around the <see cref="ItemsPanel"/>, but not the <see cref="TitleComponent"/><br/>
        /// 3. <see cref="TitleBorder"/>: Wrapped around the <see cref="TitleComponent"/></summary>
        public MGBorder InnerBorder { get; }

        public IBorderBrush InnerBorderBrush
        {
            get => InnerBorder.BorderBrush;
            set
            {
                if (InnerBorderBrush != value)
                {
                    InnerBorder.BorderBrush = value;
                    NPC(nameof(InnerBorderBrush));
                }
            }
        }

        public Thickness InnerBorderThickness
        {
            get => InnerBorder.BorderThickness;
            set
            {
                if (!InnerBorderThickness.Equals(value))
                {
                    InnerBorder.BorderThickness = value;
                    NPC(nameof(InnerBorderThickness));
                }
            }
        }
        #endregion Inner Border

        #region Title
        private MGComponent<MGBorder> TitleComponent { get; }
        /// <summary><see cref="MGListBox{TItemType}"/>es contain 3 borders:<para/>
        /// 1. <see cref="OuterBorder"/>: Wrapped around the entire <see cref="MGListBox{TItemType}"/><br/>
        /// 2. <see cref="InnerBorder"/>: Wrapped around the <see cref="ItemsPanel"/>, but not the <see cref="TitleComponent"/><br/>
        /// 3. <see cref="TitleBorder"/>: Wrapped around the <see cref="TitleComponent"/></summary>
        public MGBorder TitleBorder { get; }
        public MGContentPresenter TitlePresenter { get; }

        public IBorderBrush TitleBorderBrush
        {
            get => TitleBorder.BorderBrush;
            set
            {
                if (TitleBorder.BorderBrush != value)
                {
                    TitleBorder.BorderBrush = value;
                    NPC(nameof(TitleBorderBrush));
                }
            }
        }

        public Thickness TitleBorderThickness
        {
            get => TitleBorder.BorderThickness;
            set
            {
                if (!TitleBorder.BorderThickness.Equals(value))
                {
                    TitleBorder.BorderThickness = value;
                    NPC(nameof(TitleBorderThickness));
                }
            }
        }

        public bool IsTitleVisible
        {
            get => TitleBorder.Visibility == Visibility.Visible;
            set
            {
                if (IsTitleVisible != value)
                {
                    TitleBorder.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                    NPC(nameof(IsTitleVisible));
                }
            }
        }
        #endregion Title

        #region Items Source
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ObservableCollection<MGListBoxItem<TItemType>> _InternalItems;
        private ObservableCollection<MGListBoxItem<TItemType>> InternalItems
        {
            get => _InternalItems;
            set
            {
                if (_InternalItems != value)
                {
                    if (InternalItems != null)
                    {
                        HandleTemplatedContentRemoved(InternalItems.Select(x => x.Content));
                        InternalItems.CollectionChanged -= ListBoxItems_CollectionChanged;
                    }
                    _InternalItems = value;
                    if (InternalItems != null)
                        InternalItems.CollectionChanged += ListBoxItems_CollectionChanged;

                    using (ItemsPanel.AllowChangingContentTemporarily())
                    {
                        //  Clear all ListBoxItems
                        _ = ItemsPanel.TryRemoveAll();

                        //  Add the new ListBoxItems to the ItemsPanel
                        if (InternalItems != null)
                        {
                            foreach (MGListBoxItem<TItemType> LBI in InternalItems)
                                _ = ItemsPanel.TryAddChild(LBI.ContentPresenter);
                        }
                    }

                    ClearSelection();
                    RefreshRowBackgrounds();

                    NPC(nameof(ListBoxItems));
                }
            }
        }
        public IReadOnlyList<MGListBoxItem<TItemType>> ListBoxItems => InternalItems;

        private static void HandleTemplatedContentRemoved(IEnumerable<MGElement> Items)
        {
            if (Items != null)
            {
                foreach (var Item in Items)
                    Item.RemoveDataBindings(true);
            }
        }

        private void ListBoxItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            using (ItemsPanel.AllowChangingContentTemporarily())
            {
                HashSet<MGListBoxItem<TItemType>> Removed = new();

                if (e.Action is NotifyCollectionChangedAction.Reset)
                {
                    _ = ItemsPanel.TryRemoveAll();
                    ClearSelection();
                }
                else if (e.Action is NotifyCollectionChangedAction.Add && e.NewItems != null)
                {
                    int Index = e.NewStartingIndex;
                    foreach (MGListBoxItem<TItemType> Item in e.NewItems)
                    {
                        ItemsPanel.TryInsertChild(Index, Item.ContentPresenter);
                        Index++;
                    }

                    RefreshRowBackgrounds();
                }
                else if (e.Action is NotifyCollectionChangedAction.Remove && e.OldItems != null)
                {
                    foreach (MGListBoxItem<TItemType> Item in e.OldItems)
                    {
                        if (ItemsPanel.TryRemoveChild(Item.ContentPresenter))
                        {
                            Removed.Add(Item);
                        }
                    }

                    RefreshRowBackgrounds();
                }
                else if (e.Action is NotifyCollectionChangedAction.Replace)
                {
                    List<MGListBoxItem<TItemType>> Old = e.OldItems.Cast<MGListBoxItem<TItemType>>().ToList();
                    List<MGListBoxItem<TItemType>> New = e.NewItems.Cast<MGListBoxItem<TItemType>>().ToList();
                    for (int i = 0; i < Old.Count; i++)
                    {
                        if (ItemsPanel.TryReplaceChild(Old[i].ContentPresenter, New[i].ContentPresenter))
                        {
                            Removed.Add(Old[i]);

                            if (AlternatingRowBackgrounds?.Any() == true)
                                New[i].ContentPresenter.BackgroundBrush.NormalValue = Old[i].ContentPresenter.BackgroundBrush.NormalValue;
                        }
                    }
                }
                else if (e.Action is NotifyCollectionChangedAction.Move)
                {
                    throw new NotImplementedException();
                }

                //  Ensure none of the removed items are Selected
                if (SelectedItems != null)
                {
                    List<MGListBoxItem<TItemType>> NewSelectedItems = SelectedItems.Where(x => !Removed.Contains(x)).ToList();
                    if (NewSelectedItems.Count != SelectedItems.Count || !NewSelectedItems.SequenceEqual(SelectedItems))
                        SelectedItems = NewSelectedItems.AsReadOnly();
                }
                if (SelectionSourceItem != null && Removed.Contains(SelectionSourceItem))
                    SelectionSourceItem = null;

                HandleTemplatedContentRemoved(Removed.Select(x => x.Content));
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ObservableCollection<TItemType> _ItemsSource;
        /// <summary>To set this value, use <see cref="SetItemsSource(ICollection{TItemType})"/></summary>
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

                    if (ItemsSource == null)
                        InternalItems = null;
                    else
                    {
                        IEnumerable<MGListBoxItem<TItemType>> Values = ItemsSource.Select((x, Index) => new MGListBoxItem<TItemType>(this, x));
                        this.InternalItems = new ObservableCollection<MGListBoxItem<TItemType>>(Values);
                    }

                    NPC(nameof(ItemsSource));
                }
            }
        }

        private void ItemsSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action is NotifyCollectionChangedAction.Reset)
            {
                HandleTemplatedContentRemoved(InternalItems.Select(x => x.Content));
                InternalItems.Clear();
            }
            else if (e.Action is NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                int CurrentIndex = e.NewStartingIndex;
                foreach (TItemType Item in e.NewItems)
                {
                    MGListBoxItem<TItemType> NewRowItem = new(this, Item);
                    InternalItems.Insert(CurrentIndex, NewRowItem);
                    CurrentIndex++;
                }
            }
            else if (e.Action is NotifyCollectionChangedAction.Remove && e.OldItems != null)
            {
                int CurrentIndex = e.OldStartingIndex;
                foreach (TItemType Item in e.OldItems)
                {
                    InternalItems.RemoveAt(CurrentIndex);
                    CurrentIndex++;
                }
            }
            else if (e.Action is NotifyCollectionChangedAction.Replace)
            {
                List<TItemType> Old = e.OldItems.Cast<TItemType>().ToList();
                List<TItemType> New = e.NewItems.Cast<TItemType>().ToList();
                for (int i = 0; i < Old.Count; i++)
                {
                    MGListBoxItem<TItemType> OldRowItem = InternalItems[i];
                    MGListBoxItem<TItemType> NewRowItem = new(this, New[i]);
                    InternalItems[e.OldStartingIndex + i] = NewRowItem;
                }
            }
            else if (e.Action is NotifyCollectionChangedAction.Move)
            {
                throw new NotImplementedException();
            }
        }

        /// <param name="Value"><see cref="ItemsSource"/> will be set to a copy of this <see cref="ICollection{T}"/> unless the collection is an <see cref="ObservableCollection{T}"/>.<br/>
        /// If you want <see cref="ItemsSource"/> to dynamically update as the collection changes, pass in an <see cref="ObservableCollection{T}"/></param>
        public void SetItemsSource(ICollection<TItemType> Value)
        {
            if (Value is ObservableCollection<TItemType> Observable)
                this.ItemsSource = Observable;
            else
                this.ItemsSource = new ObservableCollection<TItemType>(Value.ToList());
        }
        #endregion Items Source

        #region Selection
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _CanDeselectByClickingSelectedItem;
        /// <summary>Only relevant if <see cref="SelectionMode"/> is not <see cref="ListBoxSelectionMode.None"/><para/>
        /// If true, allows deselecting a currently-selected item by left-clicking it.<para/>
        /// Note: If <see cref="SelectionMode"/> is <see cref="ListBoxSelectionMode.Multiple"/>, and the Control key is held when clicking an item,<br/>
        /// the user is always able to deselect regardless of this setting.<para/>
        /// Default value: true</summary>
        public bool CanDeselectByClickingSelectedItem
        {
            get => _CanDeselectByClickingSelectedItem;
            set
            {
                if (_CanDeselectByClickingSelectedItem != value)
                {
                    _CanDeselectByClickingSelectedItem = value;
                    NPC(nameof(CanDeselectByClickingSelectedItem));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ListBoxSelectionMode _SelectionMode;
        public ListBoxSelectionMode SelectionMode
        {
            get => _SelectionMode;
            set
            {
                if (_SelectionMode != value)
                {
                    _SelectionMode = value;
                    ClearSelection();
                    NPC(nameof(SelectionMode));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private MGListBoxItem<TItemType> _SelectionSourceItem;
        /// <summary>Only relevant if <see cref="SelectionMode"/> is <see cref="ListBoxSelectionMode.Contiguous"/>.<para/>
        /// Represents the starting item of the contiguous selection of items.</summary>
        public MGListBoxItem<TItemType> SelectionSourceItem
        {
            get => _SelectionSourceItem;
            private set
            {
                if (_SelectionSourceItem != value)
                {
                    _SelectionSourceItem = value;
                    NPC(nameof(SelectionSourceItem));
                }
            }
        }

        private readonly EqualityComparer<TItemType> EqualityComparer = EqualityComparer<TItemType>.Default;

        /// <summary>Returns the <see cref="MGListBoxItem{TItemType}.Data"/> of the first item in <see cref="SelectedItems"/>, or default(<typeparamref name="TItemType"/>) if no items are selected.<para/>
        /// Setting this value overwrites the <see cref="SelectedItems"/> with the first <see cref="MGListBoxItem{TItemType}"/> containing the matching data.<br/>
        /// If no matching item is found, clears the selection entirely.</summary>
        public TItemType SelectedValue
        {
            get => SelectedItems.Count == 0 ? default(TItemType) : SelectedItems.First().Data;
            set => SelectItem(value, true);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ReadOnlyCollection<MGListBoxItem<TItemType>> _SelectedItems;
        /// <summary>The currently-selected items. This collection is never null: Uses an empty list if setting to null.</summary>
        public ReadOnlyCollection<MGListBoxItem<TItemType>> SelectedItems
        {
            get => _SelectedItems;
            set
            {
                if (_SelectedItems != value)
                {
                    if (SelectedItems != null)
                    {
                        foreach (MGListBoxItem<TItemType> Item in SelectedItems)
                            Item.ContentPresenter.IsSelected = false;
                    }

                    _SelectedItems = value ?? new List<MGListBoxItem<TItemType>>().AsReadOnly();

                    if (SelectedItems.Any(x => x == null))
                        throw new ArgumentNullException($"{nameof(MGListBoxItem<object>)}.{nameof(SelectedItems)} cannnot contain null items.");

                    foreach (MGListBoxItem<TItemType> Item in SelectedItems)
                        Item.ContentPresenter.IsSelected = true;

                    NPC(nameof(SelectedItems));
                    NPC(nameof(SelectedValue));
                    SelectionChanged?.Invoke(this, SelectedItems);
                }
            }
        }

        public event EventHandler<ReadOnlyCollection<MGListBoxItem<TItemType>>> SelectionChanged;

        /// <summary>Note: This method clears the selection if <see cref="SelectionMode"/> is <see cref="ListBoxSelectionMode.None"/></summary>
        /// <param name="Item">The item to select. Will search for a <see cref="MGListBoxItem{TItemType}"/> whose <see cref="MGListBoxItem{TItemType}.Data"/> matches this value.</param>
        /// <param name="DeselectAllIfNotFound">If true, and if no corresponding <see cref="MGListBoxItem{TItemType}"/> is found that matches the given <paramref name="Item"/>, the selection will be cleared.</param>
        public void SelectItem(TItemType Item, bool DeselectAllIfNotFound)
        {
            if (SelectionMode is ListBoxSelectionMode.None)
            {
                ClearSelection();
                return;
            }

            if (SelectedItems?.Count == 1 && EqualityComparer.Equals(SelectedItems.First().Data, Item))
                return;

            //  Find ListBoxItem that wraps the Item data
            foreach (MGListBoxItem<TItemType> LBI in ListBoxItems)
            {
                if (EqualityComparer.Equals(LBI.Data, Item))
                {
                    //  Select it
                    this.SelectedItems = new List<MGListBoxItem<TItemType>>() { LBI }.AsReadOnly();
                    return;
                }
            }

            if (DeselectAllIfNotFound)
                ClearSelection();
        }

        public void ClearSelection()
        {
            SelectionSourceItem = null;
            if (SelectedItems?.Count != 0)
                SelectedItems = new List<MGListBoxItem<TItemType>>().AsReadOnly();
        }
        #endregion Selection

        public MGScrollViewer ScrollViewer { get; }
        public MGStackPanel ItemsPanel { get; }

        /// <summary>Sets the <see cref="TitleBorderBrush"/> to the given <paramref name="Brush"/> using the given <paramref name="BorderThickness"/>, except with a bottom thickness of 0 to avoid doubled thickness between the title and content.<br/>
        /// Sets the <see cref="InnerBorderBrush"/> to the given <paramref name="Brush"/> using the given <paramref name="BorderThickness"/></summary>
        public void SetTitleAndContentBorder(IFillBrush Brush, int BorderThickness)
        {
            TitleBorderBrush = Brush?.AsUniformBorderBrush();
            TitleBorderThickness = new(BorderThickness, BorderThickness, BorderThickness, 0);

            InnerBorderBrush = Brush?.AsUniformBorderBrush();
            InnerBorderThickness = new(BorderThickness);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private MGElement _Header;
        /// <summary>Content to display inside the <see cref="TitlePresenter"/>. Only relevant if <see cref="IsTitleVisible"/> is true.</summary>
        public MGElement Header
        {
            get => _Header;
            set
            {
                if (_Header != value)
                {
                    _Header = value;
                    using (TitlePresenter.AllowChangingContentTemporarily())
                    {
                        TitlePresenter.SetContent(Header);
                    }
                    NPC(nameof(Header));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Func<TItemType, MGElement> _ItemTemplate;
        /// <summary>This function is invoked to instantiate the <see cref="MGListBoxItem{TItemType}.Content"/> of each <see cref="MGListBoxItem{TItemType}"/> in this <see cref="MGListBox{TItemType}"/></summary>
        public Func<TItemType, MGElement> ItemTemplate
        {
            get => _ItemTemplate;
            set
            {
                if (_ItemTemplate != value)
                {
                    _ItemTemplate = value;
                    NPC(nameof(ItemTemplate));
                    ItemTemplateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler<EventArgs> ItemTemplateChanged;

        public readonly MGUniformBorderBrush DefaultItemBorderBrush = new MGSolidFillBrush(Color.Black * 0.35f).AsUniformBorderBrush();
        public readonly Thickness DefaultItemBorderThickness = new(0, 1);

        public void ApplyDefaultItemContainerStyle(MGBorder Item)
        {
            Item.BorderBrush = DefaultItemBorderBrush;
            Item.BorderThickness = DefaultItemBorderThickness;
            Item.Padding = new(6, 4);
            Item.BackgroundBrush = GetTheme().ListBoxItemBackground.GetValue(true);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Action<MGBorder> _ItemContainerStyle;
        /// <summary>An action that will be invoked on every <see cref="MGBorder"/> that wraps each <see cref="MGListBoxItem{TItemType}"/>'s content.<para/>
        /// See also: <see cref="MGListBoxItem{TItemType}.ContentPresenter"/><para/>
        /// Default value: <see cref="ApplyDefaultItemContainerStyle(MGBorder)"/></summary>
        public Action<MGBorder> ItemContainerStyle
        {
            get => _ItemContainerStyle;
            set
            {
                if (_ItemContainerStyle != value)
                {
                    _ItemContainerStyle = value;
                    NPC(nameof(ItemContainerStyle));
                    ItemContainerStyleChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler<EventArgs> ItemContainerStyleChanged;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ReadOnlyCollection<IFillBrush> _AlternatingRowBackgrounds;
        /// <summary>If not null/empty, the row items will cycle through these <see cref="IFillBrush"/> for their backgrounds</summary>
        public ReadOnlyCollection<IFillBrush> AlternatingRowBackgrounds
        {
            get => _AlternatingRowBackgrounds;
            set
            {
                if (_AlternatingRowBackgrounds != value)
                {
                    _AlternatingRowBackgrounds = value;
                    RefreshRowBackgrounds();
                    NPC(nameof(AlternatingRowBackgrounds));
                }
            }
        }

        private void RefreshRowBackgrounds()
        {
            if (InternalItems != null)
            {
                for (int i = 0; i < InternalItems.Count; i++)
                {
                    IFillBrush Brush = AlternatingRowBackgrounds?.Any() == true ? AlternatingRowBackgrounds[i % AlternatingRowBackgrounds.Count] : null;
                    InternalItems[i].ContentPresenter.BackgroundBrush.NormalValue = Brush;
                }
            }
        }

        public MGListBox(MGWindow ParentWindow)
            : base(ParentWindow, MGElementType.ListBox)
        {
            using (BeginInitializing())
            {
                //  Create the outer border
                this.OuterBorder = new(ParentWindow, 0, MGSolidFillBrush.Black);
                this.OuterBorderComponent = MGComponentBase.Create(OuterBorder);
                AddComponent(OuterBorderComponent);
                OuterBorder.OnBorderBrushChanged += (sender, e) => { NPC(nameof(OuterBorderBrush)); };
                OuterBorder.OnBorderThicknessChanged += (sender, e) => { NPC(nameof(OuterBorderThickness)); };

                //  Create the title bar
                this.TitleBorder = new(ParentWindow);
                TitleBorder.Padding = new(6, 3);
                TitleBorder.BackgroundBrush = GetTheme().TitleBackground.GetValue(true);
                TitleBorder.DefaultTextForeground.SetAll(Color.White);
                TitleBorder.OnBorderBrushChanged += (sender, e) => { NPC(nameof(TitleBorderBrush)); };
                TitleBorder.OnBorderThicknessChanged += (sender, e) => { NPC(nameof(TitleBorderThickness)); };
                this.TitlePresenter = new(ParentWindow);
                TitlePresenter.VerticalAlignment = VerticalAlignment.Center;
                TitleBorder.SetContent(TitlePresenter);
                TitleBorder.CanChangeContent = false;
                TitlePresenter.CanChangeContent = false;
                this.TitleComponent = new(TitleBorder, true, false, false, true, false, false, false,
                    (AvailableBounds, ComponentSize) => ApplyAlignment(AvailableBounds, HorizontalAlignment.Stretch, VerticalAlignment.Top, ComponentSize.Size));
                AddComponent(TitleComponent);

                //  Create the inner border
                this.InnerBorder = new(ParentWindow);
                this.InnerBorderComponent = new(InnerBorder, true, false, true, true, false, false, false,
                    (AvailableBounds, ComponentSize) => ApplyAlignment(AvailableBounds, HorizontalAlignment.Stretch, VerticalAlignment.Stretch, ComponentSize.Size));
                AddComponent(InnerBorderComponent);
                InnerBorder.OnBorderBrushChanged += (sender, e) => { NPC(nameof(InnerBorderBrush)); };
                InnerBorder.OnBorderThicknessChanged += (sender, e) => { NPC(nameof(InnerBorderThickness)); };

                //  Create the scrollviewer and itemspanel
                this.ItemsPanel = new(ParentWindow, Orientation.Vertical);
                ItemsPanel.VerticalAlignment = VerticalAlignment.Top;
                ItemsPanel.CanChangeContent = false;
                this.ScrollViewer = new(ParentWindow);
                ScrollViewer.Padding = new(0, 0);
                ScrollViewer.SetContent(ItemsPanel);
                ScrollViewer.CanChangeContent = false;
                InnerBorder.SetContent(ScrollViewer);
                InnerBorder.CanChangeContent = false;

                SetTitleAndContentBorder(MGSolidFillBrush.Black, 1);

                MinHeight = 30;

                AlternatingRowBackgrounds = GetTheme().ListBoxItemAlternatingRowBackgrounds.Select(x => x.GetValue(true)).ToList().AsReadOnly();

                ItemsPanel.BorderThickness = DefaultItemBorderThickness;
                ItemsPanel.BorderBrush = DefaultItemBorderBrush;

                this.ItemContainerStyle = ApplyDefaultItemContainerStyle;
                this.ItemTemplate = (item) => new MGTextBlock(ParentWindow, item.ToString()) { Padding = new(1,0) };

                this.SelectedItems = new List<MGListBoxItem<TItemType>>().AsReadOnly();
                this.SelectionMode = ListBoxSelectionMode.Single;
                this.CanDeselectByClickingSelectedItem = true;

                MouseHandler.LMBReleasedInside += (sender, e) =>
                {
                    MGListBoxItem<TItemType> PressedItem = InternalItems?.FirstOrDefault(x => x.ContentPresenter.IsHovered);
                    if (PressedItem != null)
                    {
                        bool IsPressedItemAlreadySelected = SelectedItems?.Contains(PressedItem) == true;
                        bool IsShiftDown = InputTracker.Keyboard.IsShiftDown;
                        bool IsControlDown = InputTracker.Keyboard.IsControlDown;

                        void SelectSingle()
                        {
                            if (IsPressedItemAlreadySelected && CanDeselectByClickingSelectedItem && SelectedItems.Count <= 1)
                            {
                                ClearSelection();
                            }
                            else
                            {
                                SelectionSourceItem = PressedItem;
                                SelectedItems = new List<MGListBoxItem<TItemType>>() { PressedItem }.AsReadOnly();
                            }
                        }

                        void SelectContiguous()
                        {
                            if (!IsShiftDown || SelectionSourceItem == null || !InternalItems.Contains(SelectionSourceItem))
                            {
                                SelectSingle();
                            }
                            else
                            {
                                int SourceIndex = InternalItems.IndexOf(SelectionSourceItem);
                                int PressedIndex = InternalItems.IndexOf(PressedItem);

                                int StartIndex = Math.Min(SourceIndex, PressedIndex);
                                int EndIndex = Math.Max(SourceIndex, PressedIndex);

                                SelectedItems = InternalItems.Skip(StartIndex).Take(EndIndex - StartIndex + 1).ToList().AsReadOnly();
                            }
                        }

                        switch (this.SelectionMode)
                        {
                            case ListBoxSelectionMode.None:
                                break;
                            case ListBoxSelectionMode.Single:
                                SelectSingle();
                                break;
                            case ListBoxSelectionMode.Contiguous:
                                SelectContiguous();
                                break;
                            case ListBoxSelectionMode.Multiple:
                                if (IsControlDown)
                                {
                                    if (IsPressedItemAlreadySelected)
                                        SelectedItems = SelectedItems.Where(x => x != PressedItem).ToList().AsReadOnly();
                                    else
                                        SelectedItems = SelectedItems.Append(PressedItem).ToList().AsReadOnly();
                                }
                                else
                                    SelectContiguous();
                                break;
                            default: throw new NotImplementedException($"Unrecognized {nameof(ListBoxSelectionMode)}: {nameof(SelectionMode)}");
                        }
                    }
                };
            }
        }

        public override void DrawSelf(ElementDrawArgs DA, Rectangle LayoutBounds)
        {
            base.DrawSelf(DA, LayoutBounds);
        }

        //  This method is invoked via reflection in MGUI.Core.UI.XAML.Lists.ListBox.ApplyDerivedSettings.
        //  Do not modify the method signature.
        internal void LoadSettings(ListBox Settings, bool IncludeContent)
        {
            MGDesktop Desktop = GetDesktop();

            Settings.OuterBorder.ApplySettings(this, OuterBorder, false);
            Settings.InnerBorder.ApplySettings(this, InnerBorder, false);
            Settings.TitleBorder.ApplySettings(this, TitleBorder, false);
            Settings.TitlePresenter.ApplySettings(this, TitlePresenter, false);
            Settings.ScrollViewer.ApplySettings(this, ScrollViewer, false);
            Settings.ItemsPanel.ApplySettings(this, ItemsPanel, false);

            if (Settings.Header != null)
                Header = Settings.Header.ToElement<MGElement>(SelfOrParentWindow, this);

            if (Settings.IsTitleVisible.HasValue)
            {
                IsTitleVisible = Settings.IsTitleVisible.Value;
                if (!IsTitleVisible)
                {
                    TitleBorderThickness = new(0);
                    InnerBorderThickness = new(0);
                }
            }

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

            if (Settings.CanDeselectByClickingSelectedItem.HasValue)
                CanDeselectByClickingSelectedItem = Settings.CanDeselectByClickingSelectedItem.Value;
            if (Settings.SelectionMode.HasValue)
                SelectionMode = Settings.SelectionMode.Value;

            if (Settings.AlternatingRowBackgrounds != null && Settings.AlternatingRowBackgrounds.Any())
                AlternatingRowBackgrounds = Settings.AlternatingRowBackgrounds.Select(x => x.ToFillBrush(Desktop)).ToList().AsReadOnly();
            else
                AlternatingRowBackgrounds = new List<IFillBrush>().AsReadOnly();

            if (Settings.ItemContainerStyle != null)
            {
                this.ItemContainerStyle = (Border) => { Settings.ItemContainerStyle.ApplySettings(this, Border, false); };
            }

            if (Settings.ItemTemplate != null)
            {
                this.ItemTemplate = (Item) => Settings.ItemTemplate.GetContent(SelfOrParentWindow, this, Item);
            }
        }
    }

    public class MGListBoxItem<TItemType> : ViewModelBase
    {
        public MGListBox<TItemType> ListBox { get; }

        /// <summary>The data object used as a parameter to generate the content of this item.<para/>
        /// See also: <see cref="MGListBox{TItemType}.ItemTemplate"/></summary>
        public TItemType Data { get; }

        /// <summary>The wrapper element that hosts this item's content</summary>
        public MGBorder ContentPresenter { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private MGElement _Content;
        /// <summary>This <see cref="MGElement"/> is automatically generated via <see cref="MGListBox{TItemType}.ItemTemplate"/> using this.<see cref="Data"/> as the parameter.<para/>
        /// See also: <see cref="ContentPresenter"/></summary>
        public MGElement Content
        {
            get => _Content;
            private set
            {
                if (_Content != value)
                {
                    _Content = value;
                    using (ContentPresenter.AllowChangingContentTemporarily())
                    {
                        ContentPresenter.SetContent(Content);
                    }
                    NPC(nameof(Content));
                }
            }
        }

        internal MGListBoxItem(MGListBox<TItemType> ListBox, TItemType Data)
        {
            this.ListBox = ListBox ?? throw new ArgumentNullException(nameof(ListBox));
            this.Data = Data ?? throw new ArgumentNullException(nameof(Data));
            this.ContentPresenter = new(ListBox.SelfOrParentWindow);

            ListBox.ItemTemplateChanged += (sender, e) =>
            {
                Content?.RemoveDataBindings(true);
                Content = ListBox.ItemTemplate?.Invoke(this.Data);
            };
            this.Content = ListBox.ItemTemplate?.Invoke(this.Data);
            ContentPresenter.CanChangeContent = false;

            ListBox.ItemContainerStyleChanged += (sender, e) => { ListBox.ItemContainerStyle?.Invoke(this.ContentPresenter); };
            ListBox.ItemContainerStyle?.Invoke(this.ContentPresenter);
        }
    }
}
