using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.Containers;
using MGUI.Core.UI.Containers.Grids;
using MGUI.Core.UI.XAML;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MGUI.Shared.Helpers;
using MGUI.Core.UI.Brushes.Border_Brushes;
using MonoGame.Extended;
using Thickness = MonoGame.Extended.Thickness;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using RowDefinition = MGUI.Core.UI.Containers.Grids.RowDefinition;
using ColumnDefinition = MGUI.Core.UI.Containers.Grids.ColumnDefinition;

namespace MGUI.Core.UI
{
    /// <typeparam name="TItemType">The type that the ItemsSource will be bound to.</typeparam>
    public class MGListView<TItemType> : MGSingleContentHost
    {
        #region Items Source
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ObservableCollection<MGListViewItem<TItemType>> _InternalRowItems;
        private ObservableCollection<MGListViewItem<TItemType>> InternalRowItems
        {
            get => _InternalRowItems;
            set
            {
                if (_InternalRowItems != value)
                {
                    if (InternalRowItems != null)
                        InternalRowItems.CollectionChanged += RowItems_CollectionChanged;
                    _InternalRowItems = value;
                    if (InternalRowItems != null)
                        InternalRowItems.CollectionChanged += RowItems_CollectionChanged;

                    foreach (MGListViewColumn<TItemType> LVIColumn in _Columns)
                    {
                        LVIColumn.RefreshColumnContent();
                    }
                }
            }
        }
        public IReadOnlyList<MGListViewItem<TItemType>> RowItems => InternalRowItems;

        //TODO invoke this method whenever Content that was auto-created via a column's CellTemplate gets deleted.
        //  (Such as in MGListViewColumn.RefreshColumnContent or in RowItems_CollectionChanged)
        //  This isn't completely necessary but may help avoid memory leaks from old DataBindings that are subscribed to
        //  PropertyChanged events even though the target object isn't in use anymore.
        private void HandleTemplatedContentRemoved(IEnumerable<MGElement> Items)
        {
            if (Items != null)
            {
                foreach (MGElement Item in Items)
                    Item.RemoveDataBindings(true);
            }
        }

        private void RowItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            using (DataGrid.AllowChangingContentTemporarily())
            {
                if (e.Action is NotifyCollectionChangedAction.Reset)
                {
                    _ = DataGrid.TryRemoveAll();
                }
                else if (e.Action is NotifyCollectionChangedAction.Add && e.NewItems != null)
                {
                    foreach (MGListViewItem<TItemType> Item in e.NewItems)
                    {
                        Item.RefreshRowContent();
                    }
                }
                else if (e.Action is NotifyCollectionChangedAction.Remove && e.OldItems != null)
                {
                    foreach (MGListViewItem<TItemType> Item in e.OldItems)
                    {
                        DataGrid.RemoveRow(Item.DataRow);
                    }
                }
                else if (e.Action is NotifyCollectionChangedAction.Replace)
                {
                    List<MGListViewItem<TItemType>> Old = e.OldItems.Cast<MGListViewItem<TItemType>>().ToList();
                    List<MGListViewItem<TItemType>> New = e.NewItems.Cast<MGListViewItem<TItemType>>().ToList();
                    for (int i = 0; i < Old.Count; i++)
                    {
                        New[i].RefreshRowContent();
                    }
                }
                else if (e.Action is NotifyCollectionChangedAction.Move)
                {
                    throw new NotImplementedException();
                }
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

                    using (DataGrid.AllowChangingContentTemporarily())
                    {
                        _ = DataGrid.TryRemoveAll();

                        //  Make exactly 1 row per item in the collection
                        int RowCount = ItemsSource?.Count ?? 0;
                        while (DataGrid.Rows.Count < RowCount)
                            DataGrid.AddRow(RowLength);
                        while (DataGrid.Rows.Count > RowCount)
                            DataGrid.RemoveRow(DataGrid.Rows[^1]);
                    }

                    if (ItemsSource == null)
                        InternalRowItems = null;
                    else
                    {
                        IEnumerable<MGListViewItem<TItemType>> Values = ItemsSource.Select((x, Index) => new MGListViewItem<TItemType>(this, x, DataGrid.Rows[Index]));
                        this.InternalRowItems = new ObservableCollection<MGListViewItem<TItemType>>(Values);
                    }

                    NPC(nameof(ItemsSource));
                }
            }
        }

        private void ItemsSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action is NotifyCollectionChangedAction.Reset)
            {
                InternalRowItems.Clear();
            }
            else if (e.Action is NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                int CurrentIndex = e.NewStartingIndex;
                foreach (TItemType Item in e.NewItems)
                {
                    RowDefinition NewRow = DataGrid.InsertRow(CurrentIndex, RowLength);
                    MGListViewItem<TItemType> NewRowItem = new(this, Item, NewRow);
                    InternalRowItems.Insert(CurrentIndex, NewRowItem);
                    CurrentIndex++;
                }
            }
            else if (e.Action is NotifyCollectionChangedAction.Remove && e.OldItems != null)
            {
                int CurrentIndex = e.OldStartingIndex;
                foreach (TItemType Item in e.OldItems)
                {
                    InternalRowItems.RemoveAt(CurrentIndex);
                }
            }
            else if (e.Action is NotifyCollectionChangedAction.Replace)
            {
                List<TItemType> Old = e.OldItems.Cast<TItemType>().ToList();
                List<TItemType> New = e.NewItems.Cast<TItemType>().ToList();
                for (int i = 0; i < Old.Count; i++)
                {
                    MGListViewItem<TItemType> OldRowItem = InternalRowItems[i];
                    MGListViewItem<TItemType> NewRowItem = new(this, New[i], OldRowItem.DataRow);
                    InternalRowItems[e.OldStartingIndex + i] = NewRowItem;
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

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly List<MGListViewColumn<TItemType>> _Columns;
        public IReadOnlyList<MGListViewColumn<TItemType>> Columns => _Columns;

        public MGListViewColumn<TItemType> AddColumn(ListViewColumnWidth Width, MGElement Header, Func<TItemType, MGElement> ItemTemplate)
        {
            MGListViewColumn<TItemType> Column = new(this, Width, Header, ItemTemplate);
            _Columns.Add(Column);
            return Column;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int? _RowHeight;
        /// <summary>The height, in pixels, of each row in <see cref="DataGrid"/>. Does not affect the height of the header row.<para/>
        /// If null, row heights will be dynamically sized to their content, which may incur significant performance hit for large collections.<para/>
        /// Default value: null</summary>
        public int? RowHeight
        {
            get => _RowHeight;
            set
            {
                if (_RowHeight != value)
                {
                    _RowHeight = value;
                    RowLength = RowHeight.HasValue ? GridLength.CreatePixelLength(RowHeight.Value) : GridLength.Auto;
                    NPC(nameof(RowHeight));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private GridLength _RowLength;
        internal GridLength RowLength
        {
            get => _RowLength;
            set
            {
                if (_RowLength != value)
                {
                    _RowLength = value;

                    if (DataGrid != null)
                    {
                        foreach (RowDefinition Row in DataGrid.Rows)
                        {
                            Row.Length = RowLength;
                        }
                    }

                    NPC(nameof(RowLength));
                }
            }
        }

        public MGDockPanel DockPanelElement { get; }
        /// <summary>The <see cref="MGGrid"/> that contains the column headers</summary>
        public MGGrid HeaderGrid { get; }
        public MGScrollViewer ScrollViewer { get; }
        /// <summary>The <see cref="MGGrid"/> that contains the rows of data, based on <see cref="ItemsSource"/></summary>
        public MGGrid DataGrid { get; }

        /// <summary>Default value: <see cref="GridSelectionMode.None"/></summary>
        public GridSelectionMode SelectionMode
        {
            get => DataGrid.SelectionMode;
            set
            {
                if (SelectionMode != value)
                {
                    DataGrid.SelectionMode = value;
                    NPC(nameof(SelectionMode));
                }
            }
        }

        /// <summary>To get the underlying <see cref="MGElement"/>s in the selection,<br/>
        /// enumerate <see cref="GridSelection"/>, and for each <see cref="GridCell"/>, call <see cref="GridSelection.Grid"/>'s <see cref="MGGrid.GetCellContent(GridCell)"/></summary>
        public GridSelection? SelectedData => DataGrid.CurrentSelection;

        public event EventHandler<GridSelection?> SelectionChanged;

        public MGListView(MGWindow Window)
            : this(Window, 8, 3) { }

        public MGListView(MGWindow Window, int Spacing, int GridLineMargin)
            : base(Window, MGElementType.ListView)
        {
            using (BeginInitializing())
            {
                IFillBrush GridLineBrush = MGSolidFillBrush.Black;

                this.HeaderGrid = new(Window);
                HeaderGrid.AddRow(GridLength.Auto);
                HeaderGrid.GridLinesVisibility = GridLinesVisibility.All;
                HeaderGrid.RowSpacing = Spacing;
                HeaderGrid.ColumnSpacing = Spacing;
                HeaderGrid.GridLineMargin = GridLineMargin;
                HeaderGrid.HorizontalGridLineBrush = GridLineBrush;
                HeaderGrid.VerticalGridLineBrush = GridLineBrush;
                HeaderGrid.BackgroundBrush = GetTheme().TitleBackground.GetValue(true);
                HeaderGrid.DefaultTextForeground.SetAll(Color.White);
                HeaderGrid.CanChangeContent = false;

                this.DataGrid = new(Window);
                DataGrid.GridLinesVisibility = GridLinesVisibility.AllVertical | GridLinesVisibility.InnerHorizontal | GridLinesVisibility.BottomEdge; // Don't draw TopEdge gridline since the header grid already has a BottomEdge gridline
                DataGrid.Padding = new(0, GridLineMargin, 0, 0); // Normally the top would already be padded if we were drawing a TopEdge gridline. But since we're not, manually pad it
                DataGrid.RowSpacing = Spacing;
                DataGrid.ColumnSpacing = Spacing;
                DataGrid.GridLineMargin = GridLineMargin;
                DataGrid.HorizontalGridLineBrush = GridLineBrush;
                DataGrid.VerticalGridLineBrush = GridLineBrush;
                DataGrid.CanChangeContent = false;

                DataGrid.SelectionChanged += (sender, e) => { SelectionChanged?.Invoke(this, e); };

                //  Create a content-less element that will be placed to the right of the HeaderGrid, whose width will always match the width of the vertical scrollbar.
                //  If the DataGrid needs to reserve width on the right edge for a vertical scrollbar, this width will also be reserved in the HeaderGrid
                int TopRightCornerBorderThickness = Math.Max(0, Spacing - GridLineMargin * 2);
                MGBorder TopRightCornerPlaceholder = new(Window, new Thickness(0, TopRightCornerBorderThickness, TopRightCornerBorderThickness, TopRightCornerBorderThickness), MGUniformBorderBrush.Black); //null as IBorderBrush);
                TopRightCornerPlaceholder.BackgroundBrush = HeaderGrid.BackgroundBrush;

                this.ScrollViewer = new(Window, ScrollBarVisibility.Auto, ScrollBarVisibility.Disabled);
                ScrollViewer.SetContent(DataGrid);
                ScrollViewer.CanChangeContent = false;
                ScrollViewer.VerticalScrollBarBoundsChanged += (sender, e) => {
                    TopRightCornerPlaceholder.PreferredWidth = e?.Width ?? 0;
                };

                MGDockPanel HeaderGridWrapper = new(Window);
                HeaderGridWrapper.TryAddChild(TopRightCornerPlaceholder, Dock.Right);
                HeaderGridWrapper.TryAddChild(HeaderGrid, Dock.Left);
                HeaderGridWrapper.CanChangeContent = false;

                this.DockPanelElement = new(Window);
                DockPanelElement.TryAddChild(HeaderGridWrapper, Dock.Top);
                DockPanelElement.TryAddChild(ScrollViewer, Dock.Bottom);
                //DockPanelElement.VerticalAlignment = VerticalAlignment.Top;
                DockPanelElement.CanChangeContent = false;
                SetContent(DockPanelElement);
                this.CanChangeContent = false;

                HeaderGrid.ManagedParent = this;
                DataGrid.ManagedParent = this;
                ScrollViewer.ManagedParent = this;
                DockPanelElement.ManagedParent = this;

                this.VerticalAlignment = VerticalAlignment.Top;

                this.RowHeight = null;
                this.RowLength = GridLength.Auto;

                this._Columns = new();

                SelectionMode = GridSelectionMode.None;
            }
        }

        public override void DrawBackground(ElementDrawArgs DA, Rectangle LayoutBounds)
        {
            base.DrawBackground(DA, HeaderGrid.LayoutBounds);
            base.DrawBackground(DA, DataGrid.LayoutBounds);
        }

        //  This method is invoked via reflection in MGUI.Core.UI.XAML.Lists.ListView.ApplyDerivedSettings.
        //  Do not modify the method signature.
        internal void LoadSettings(ListView Settings, bool IncludeContent)
        {
            Settings.HeaderGrid.ApplySettings(this, HeaderGrid, false);
            Settings.ScrollViewer.ApplySettings(this, ScrollViewer, false);
            Settings.DataGrid.ApplySettings(this, DataGrid, false);

            foreach (ListViewColumn ColumnDefinition in Settings.Columns)
            {
                ListViewColumnWidth Width = ColumnDefinition.Width.ToWidth();
                MGElement Header = ColumnDefinition.Header.ToElement<MGElement>(SelfOrParentWindow, this);

                Func<TItemType, MGElement> CellTemplate;
                if (ColumnDefinition.CellTemplate != null)
                    CellTemplate = (Item) => ColumnDefinition.CellTemplate.GetContent(SelfOrParentWindow, this, Item);
                else
                    CellTemplate = (Item) => new MGTextBlock(SelfOrParentWindow, Item.ToString());

                AddColumn(Width, Header, CellTemplate);
            }

            if (Settings.RowHeight.HasValue)
                RowHeight = Settings.RowHeight.Value;

            if (Settings.SelectionMode.HasValue)
                SelectionMode = Settings.SelectionMode.Value;
        }
    }
    
    public class ListViewColumnWidth : ViewModelBase
    {
        public GridLength Length { get; private set; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsAbsoluteWidth => Length.IsPixelLength;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsWeightedWidth => Length.IsWeightedLength;

        /// <summary>Getter is only valid if <see cref="IsAbsoluteWidth"/> is true</summary>
        public int WidthPixels
        {
            get => Length.Pixels;
            set
            {
                Length = GridLength.CreatePixelLength(value);
                WidthChanged?.Invoke(this, EventArgs.Empty);
                NPC(nameof(WidthPixels));
                NPC(nameof(WidthWeight));
                NPC(nameof(Length));
            }
        }

        /// <summary>Getter is only valid if <see cref="IsWeightedWidth"/> is true</summary>
        public double WidthWeight
        {
            get => Length.Weight;
            set
            {
                Length = GridLength.CreateWeightedLength(value);
                WidthChanged?.Invoke(this, EventArgs.Empty);
                NPC(nameof(WidthWeight));
                NPC(nameof(WidthPixels));
                NPC(nameof(Length));
            }
        }

        public event EventHandler<EventArgs> WidthChanged;

        public ListViewColumnWidth(int WidthPixels)
        {
            this.WidthPixels = WidthPixels;
        }

        public ListViewColumnWidth(double WidthWeight)
        {
            this.WidthWeight = WidthWeight;
        }
    }

    public class MGListViewItem<TItemType>
    {
        public MGListView<TItemType> ListView { get; }

        public MGGrid DataGrid => ListView.DataGrid;
        public RowDefinition DataRow { get; }

        /// <summary>The data object used as a parameter to generate the content of each cell in the <see cref="DataRow"/>.<para/>
        /// See also: <see cref="MGListViewColumn{TItemType}.CellTemplate"/></summary>
        public TItemType Data { get; }

        public Dictionary<MGListViewColumn<TItemType>, MGElement> GetRowContents()
        {
            Dictionary<MGListViewColumn<TItemType>, MGElement> Cells = new();
            IReadOnlyDictionary<ColumnDefinition, IReadOnlyList<MGElement>> RowContentByColumn = DataGrid.GetRowContent(DataRow);
            foreach (MGListViewColumn<TItemType> Column in ListView.Columns)
            {
                if (RowContentByColumn.TryGetValue(Column.DataColumn, out var Elements) && Elements.Count > 0)
                {
                    //  Sanity check
                    if (Elements.Count > 1)
                    {
                        throw new InvalidOperationException($"Each cell in a ListView's internal Grid should have at most 1 child element. " +
                            $"The cell at RowIndex={DataRow.Index},ColumnIndex={Column.DataColumn.Index} contains {Elements.Count} children.");
                    }
                    Cells.Add(Column, Elements.First());
                }
            }
            return Cells;
        }

        internal MGListViewItem(MGListView<TItemType> ListView, TItemType Data, RowDefinition Row)
        {
            this.ListView = ListView ?? throw new ArgumentNullException(nameof(ListView));
            this.Data = Data ?? throw new ArgumentNullException(nameof(Data));
            this.DataRow = Row ?? throw new ArgumentNullException(nameof(Row));
        }

        internal void RefreshRowContent()
        {
            using (DataGrid.AllowChangingContentTemporarily())
            {
                DataGrid.ClearRowContent(DataRow);
                foreach (MGListViewColumn<TItemType> Column in ListView.Columns)
                {
                    if (Column.CellTemplate != null)
                    {
                        MGElement CellContent = Column.CellTemplate(Data);
                        DataGrid.TryAddChild(DataRow, Column.DataColumn, CellContent);
                    }
                }
            }
        }
    }

    public class MGListViewColumn<TItemType> : ViewModelBase
    {
        public MGListView<TItemType> ListView { get; }
        public ListViewColumnWidth Width { get; }

        public MGGrid HeaderGrid => ListView.HeaderGrid;
        public ColumnDefinition HeaderColumn { get; }
        public MGGrid DataGrid => ListView.DataGrid;
        public ColumnDefinition DataColumn { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private MGElement _Header;
        /// <summary>The content to display in the header row of the list view.</summary>
        public MGElement Header
        {
            get => _Header;
            set
            {
                if (_Header != value)
                {
                    _Header = value;

                    RowDefinition HeaderRow = HeaderGrid.Rows[0];
                    using (HeaderGrid.AllowChangingContentTemporarily())
                    {
                        HeaderGrid.ClearCellContent(HeaderRow, HeaderColumn);
                        if (Header != null)
                        {
                            HeaderGrid.TryAddChild(HeaderRow, HeaderColumn, Header);
                        }
                    }

                    NPC(nameof(Header));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Func<TItemType, MGElement> _CellTemplate;
        /// <summary>This function is invoked to instantiate the content of each cell in this column</summary>
        public Func<TItemType, MGElement> CellTemplate
        {
            get => _CellTemplate;
            set
            {
                if (_CellTemplate != value)
                {
                    _CellTemplate = value;
                    RefreshColumnContent();
                    NPC(nameof(CellTemplate));
                }
            }
        }

        internal void RefreshColumnContent()
        {
            using (DataGrid.AllowChangingContentTemporarily())
            {
                DataGrid.ClearColumnContent(DataColumn);
                if (ListView.RowItems != null && CellTemplate != null)
                {
                    for (int i = 0; i < ListView.RowItems.Count; i++)
                    {
                        MGListViewItem<TItemType> RowItem = ListView.RowItems[i];
                        MGElement CellContent = CellTemplate(RowItem.Data);
                        DataGrid.TryAddChild(RowItem.DataRow, DataColumn, CellContent);
                    }
                }
            }
        }

        internal MGListViewColumn(MGListView<TItemType> ListView, ListViewColumnWidth Width, MGElement Header, Func<TItemType, MGElement> ItemTemplate)
        {
            this.ListView = ListView;
            this.Width = Width;

            Width.WidthChanged += (sender, e) =>
            {
                HeaderColumn.Length = Width.Length;
                DataColumn.Length = Width.Length;
            };

            this.HeaderColumn = HeaderGrid.AddColumn(Width.Length);
            this.DataColumn = DataGrid.AddColumn(Width.Length);

            this.Header = Header;

            this.CellTemplate = ItemTemplate;
        }
    }
}
