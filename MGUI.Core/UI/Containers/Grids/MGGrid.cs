using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MGUI.Shared.Helpers;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MGUI.Shared.Input.Mouse;

namespace MGUI.Core.UI.Containers.Grids
{
    [Flags]
    public enum GridLinesVisibility : byte
    {
        None = 0b_0000_0000,

        /// <summary>Indicates that horizontal gridlines should be rendered. Does not include horizontal gridlines at the top or bottom edge of the grid's border</summary>
        InnerHorizontal = 0b_0000_0001,
        /// <summary>Indicates that a horizontal gridline should be rendered at the top edge of the grid's border</summary>
        TopEdge = 0b_0000_0010,
        /// <summary>Indicates that a horizontal gridline should be rendered at the bottom edge of the grid's border</summary>
        BottomEdge = 0b_0000_0100,

        /// <summary>Indicates that vertical gridlines should be rendered. Does not include vertical gridlines at the left or right edge of the grid's border</summary>
        InnerVertical = 0b_0000_1000,
        /// <summary>Indicates that a vertical gridline should be rendered at the left edge of the grid's border</summary>
        LeftEdge = 0b_0001_0000,
        /// <summary>Indicates that a vertical gridline should be rendered at the right edge of the grid's border</summary>
        RightEdge = 0b_0010_0000,

        All = InnerHorizontal | TopEdge | BottomEdge | InnerVertical | LeftEdge | RightEdge,

        AllHorizontal = InnerHorizontal | TopEdge | BottomEdge,
        AllVertical = InnerVertical | LeftEdge | RightEdge,
    }

    public enum GridLineIntersection : byte
    {
#if NEVER // Not Implemented
        /// <summary>Indicates that the intersections of the horizontal and vertical gridlines should not be filled with either gridline brush</summary>
        None,
        /// <summary>Indicates that the intersections of the horizontal and vertical gridlines should only be filled with the horizontal gridline brush</summary>
        HorizontalOnly,
        /// <summary>Indicates that the intersections of the horizontal and vertical gridlines should only be filled with the vertical gridline brush</summary>
        VerticalOnly,
#endif
        /// <summary>Indicates that horizontal gridlines should be rendered first, then vertical gridlines.<br/>
        /// This will result in the vertical gridline brush being drawn overtop of the horizontal gridline brush at their intersection points.</summary>
        HorizontalThenVertical,
        /// <summary>Indicates that vertical gridlines should be rendered first, then horizontal gridlines.<br/>
        /// This will result in the horizontal gridline brush being drawn overtop of the vertical gridline brush at their intersection points.</summary>
        VerticalThenHorizontal
    }

    public enum GridSelectionMode
    {
        /// <summary>Indicates that nothing in an <see cref="MGGrid"/> can be selected via left-clicking.</summary>
        None,
        /// <summary>Indicates that an entire row of content in an <see cref="MGGrid"/> can be selected by left-clicking on any cell in the <see cref="RowDefinition"/></summary>
        Row,
        /// <summary>Indicates that an entire column of content in an <see cref="MGGrid"/> can be selected by left-clicking on any cell in the <see cref="ColumnDefinition"/></summary>
        Column,
        /// <summary>Indicates that a cell in an <see cref="MGGrid"/> can be selected by left-clicking on it</summary>
        Cell
    }

    public readonly record struct GridCell(RowDefinition Row, ColumnDefinition Column)
    {
        public RowDefinition Row { get; init; } = Row ?? throw new ArgumentNullException(nameof(Row));
        public ColumnDefinition Column { get; init; } = Column ?? throw new ArgumentNullException(nameof(Column));
    }

    public readonly record struct GridSelection(MGGrid Grid, GridCell Cell, GridSelectionMode SelectionMode)
        : IEnumerable<GridCell>
    {
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<GridCell> GetEnumerator() => GetCells().GetEnumerator();

        public MGGrid Grid { get; init; } = Grid ?? throw new ArgumentNullException(nameof(Grid));

        private IEnumerable<GridCell> GetCells()
        {
            switch (SelectionMode)
            {
                case GridSelectionMode.None:
                    yield break;
                case GridSelectionMode.Row:
                    if (Grid.Rows.Contains(Cell.Row))
                    {
                        foreach (ColumnDefinition Column in Grid.Columns)
                            yield return new GridCell(Cell.Row, Column);
                    }
                    yield break;
                case GridSelectionMode.Column:
                    if (Grid.Columns.Contains(Cell.Column))
                    {
                        foreach (RowDefinition Row in Grid.Rows)
                            yield return new GridCell(Row, Cell.Column);
                    }
                    yield break;
                case GridSelectionMode.Cell:
                    if (Grid.Rows.Contains(Cell.Row) && Grid.Columns.Contains(Cell.Column))
                        yield return Cell;
                    yield break;
                default:
                    throw new NotImplementedException($"Unrecognized {nameof(GridSelectionMode)}: {SelectionMode}");
            }
        }
    }

    public readonly record struct GridSpan(int RowSpan = 1, int ColumnSpan = 1, bool AffectsMeasure = true)
    {
        public static readonly GridSpan Default = new(1, 1, true);

        public int RowSpan { get; init; } = RowSpan > 0 ? RowSpan : throw new ArgumentOutOfRangeException(nameof(RowSpan));
        public int ColumnSpan { get; init; } = ColumnSpan > 0 ? ColumnSpan : throw new ArgumentOutOfRangeException(nameof(ColumnSpan));

        public GridSpan() : this(Default.RowSpan, Default.ColumnSpan) { }
    }

    public class MGGrid : MGMultiContentHost
    {
        #region Rows / Columns
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ObservableCollection<ColumnDefinition> _Columns { get; }
        public IReadOnlyList<ColumnDefinition> Columns => _Columns;
        public int GetColumnIndex(ColumnDefinition Column) => _Columns.IndexOf(Column);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ObservableCollection<RowDefinition> _Rows { get; }
        public IReadOnlyList<RowDefinition> Rows => _Rows;
        public int GetRowIndex(RowDefinition Row) => _Rows.IndexOf(Row);

        private bool SuppressDimensionChanged = false;
        /// <summary>Converts all weighted row/column lengths to integral values that are clamped between the Min/Max size of the row/column.<para/>
        /// For example, if the columns are:<para/>
        /// <code>
        /// Index       Length      MinWidth        MaxWidth        ActualWidth
        /// 0           <b>1*</b>          20              50              50
        /// 1           25px        null            null            25
        /// 2           <b>1.2*</b>        30              100             80
        /// 
        /// Grid total width: 155
        /// </code>
        /// Normalizing the weights would convert the columns to:<para/>
        /// <code>
        /// Index       Length      MinWidth        MaxWidth        ActualWidth
        /// 0           <b>50*</b>         20              50              50
        /// 1           25px        null            null            25
        /// 2           <b>80*</b>         30              100             80
        /// 
        /// Grid total width: 155
        /// </code><para/>
        /// This method is typically only used when simplifying the row/column definitions before applying resizing logic, such as in <see cref="MGGridSplitter"/>.</summary>
        public void NormalizeWeightedLengths()
        {
            bool WasSuppressingDimensionChanged = SuppressDimensionChanged;
            try
            {
                SuppressDimensionChanged = true;

                foreach (ColumnDefinition Column in _Columns)
                {
                    if (Column.Length.IsWeightedLength)
                    {
                        int NormalizedWeight = Math.Clamp(Column.Width, Column.MinWidth ?? 0, Column.MaxWidth ?? int.MaxValue);
                        Column.Length = GridLength.CreateWeightedLength(NormalizedWeight);
                    }
                }

                foreach (RowDefinition Row in _Rows)
                {
                    if (Row.Length.IsWeightedLength)
                    {
                        int NormalizedWeight = Math.Clamp(Row.Height, Row.MinHeight ?? 0, Row.MaxHeight ?? int.MaxValue);
                        Row.Length = GridLength.CreateWeightedLength(NormalizedWeight);
                    }
                }
            }
            finally { SuppressDimensionChanged = WasSuppressingDimensionChanged; }
        }

        /// <param name="Lengths">The lengths of each <see cref="ColumnDefinition"/> to create.<para/>
        /// Consider using <see cref="GridLength.ParseMultiple(string)"/> for a convenient/concise way to generate the values</param>
        /// <returns>The created <see cref="ColumnDefinition"/>s</returns>
        public List<ColumnDefinition> AddColumns(IEnumerable<GridLength> Lengths) => Lengths.Select(x => AddColumn(x)).ToList();

        /// <param name="Lengths">The lengths of each <see cref="ColumnDefinition"/> to create.<para/>
        /// Consider using <see cref="ConstrainedGridLength.ParseMultiple(string)"/> for a convenient/concise way to generate the values</param>
        /// <returns>The created <see cref="ColumnDefinition"/>s</returns>
        public List<ColumnDefinition> AddColumns(IEnumerable<ConstrainedGridLength> Lengths) => Lengths.Select(x => AddColumn(x)).ToList();

        /// <summary>See also: <see cref="AddColumns(IEnumerable{GridLength})"/>, <see cref="GridLength.ParseMultiple(string)"/></summary>
        /// <param name="Length">Warning - <see cref="GridUnitType.Weighted"/> is treated as <see cref="GridUnitType.Auto"/> 
        /// when the column is inside of an <see cref="MGScrollViewer"/> with a horizontal scrollbar</param>
        public ColumnDefinition AddColumn(GridLength Length) => AddColumn(new ConstrainedGridLength(Length, null, null));

        /// <summary>See also: <see cref="AddColumns(IEnumerable{ConstrainedGridLength})"/>, <see cref="ConstrainedGridLength.ParseMultiple(string)"/></summary>
        /// <param name="Length">Warning - <see cref="GridUnitType.Weighted"/> is treated as <see cref="GridUnitType.Auto"/> 
        /// when the column is inside of an <see cref="MGScrollViewer"/> with a horizontal scrollbar</param>
        public ColumnDefinition AddColumn(ConstrainedGridLength Length)
        {
            ColumnDefinition Column = new(this, Length.Length, Length.MinSize, Length.MaxSize);
            _Columns.Add(Column);
            return Column;
        }

        public ColumnDefinition InsertColumn(int Index, GridLength Length) => InsertColumn(Index, new ConstrainedGridLength(Length, null, null));
        public ColumnDefinition InsertColumn(int Index, ConstrainedGridLength Length)
        {
            ColumnDefinition Column = new(this, Length.Length, Length.MinSize, Length.MaxSize);
            _Columns.Insert(Index, Column);
            return Column;
        }

        public void RemoveColumn(ColumnDefinition Column)
        {
            if (!CanChangeContent)
                return;

            ClearColumnContent(Column);
            _Columns.Remove(Column);
        }

        /// <param name="Lengths">The lengths of each <see cref="RowDefinition"/> to create.<para/>
        /// Consider using <see cref="GridLength.ParseMultiple(string)"/> for a convenient/concise way to generate the values</param>
        /// <returns>The created <see cref="RowDefinition"/>s</returns>
        public List<RowDefinition> AddRows(IEnumerable<GridLength> Lengths) => Lengths.Select(x => AddRow(x)).ToList();

        /// <param name="Lengths">The lengths of each <see cref="RowDefinition"/> to create.<para/>
        /// Consider using <see cref="ConstrainedGridLength.ParseMultiple(string)"/> for a convenient/concise way to generate the values</param>
        /// <returns>The created <see cref="RowDefinition"/>s</returns>
        public List<RowDefinition> AddRows(IEnumerable<ConstrainedGridLength> Lengths) => Lengths.Select(x => AddRow(x)).ToList();

        /// <summary>See also: <see cref="AddRows(IEnumerable{GridLength})"/>, <see cref="GridLength.ParseMultiple(string)"/></summary>
        /// <param name="Length">Warning - <see cref="GridUnitType.Weighted"/> is treated as <see cref="GridUnitType.Auto"/> 
        /// when the row is inside of an <see cref="MGScrollViewer"/> with a vertical scrollbar</param>
        public RowDefinition AddRow(GridLength Length) => AddRow(new ConstrainedGridLength(Length, null, null));

        /// <summary>See also: <see cref="AddRows(IEnumerable{ConstrainedGridLength})"/>, <see cref="ConstrainedGridLength.ParseMultiple(string)"/></summary>
        /// <param name="Length">Warning - <see cref="GridUnitType.Weighted"/> is treated as <see cref="GridUnitType.Auto"/> 
        /// when the row is inside of an <see cref="MGScrollViewer"/> with a vertical scrollbar</param>
        public RowDefinition AddRow(ConstrainedGridLength Length)
        {
            RowDefinition Row = new(this, Length.Length, Length.MinSize, Length.MaxSize);
            _Rows.Add(Row);
            return Row;
        }

        public RowDefinition InsertRow(int Index, GridLength Length)
        {
            RowDefinition Row = new(this, Length);
            _Rows.Insert(Index, Row);
            return Row;
        }

        public void RemoveRow(RowDefinition Row)
        {
            if (!CanChangeContent)
                return;

            ClearRowContent(Row);
            _Rows.Remove(Row);
        }

        private Dictionary<GridCell, Rectangle> _CellBounds = new();
        /// <summary>Warning - the <see cref="Rectangle"/>s in this dictionary do not account for <see cref="MGElement.Origin"/>.<para/>
        /// See also: <see cref="MGElement.ConvertCoordinateSpace(CoordinateSpace, CoordinateSpace, Point)"/></summary>
        public IReadOnlyDictionary<GridCell, Rectangle> CellBounds => _CellBounds;

        private Dictionary<GridCell, Rectangle> GetCellBounds(bool IncludeGridLineMargin)
        {
            Dictionary<GridCell, Rectangle> CellBounds = new();

            foreach (RowDefinition Row in _Rows)
            {
                foreach (ColumnDefinition Column in _Columns)
                {
                    GridCell Cell = new(Row, Column);
                    Rectangle PaddedBounds = new(Column.Left, Row.Top, Column.Width, Row.Height);
                    Rectangle ActualBounds = IncludeGridLineMargin ? PaddedBounds : PaddedBounds.GetExpanded(Math.Max(0, GridLineMargin));
                    CellBounds.Add(Cell, ActualBounds);
                }
            }

            return CellBounds;
        }
        #endregion Rows / Columns

        #region Elements
        /// <summary>All content in this <see cref="MGGrid"/>, indexed first by the <see cref="RowDefinition"/> it resides in, then by the <see cref="ColumnDefinition"/> it resides in.</summary>
        private readonly Dictionary<RowDefinition, Dictionary<ColumnDefinition, List<MGElement>>> ChildrenByRC = new();
        /// <summary>A lookup table that returns the <see cref="RowDefinition"/> and <see cref="ColumnDefinition"/> that a given <see cref="MGElement"/> resides in.</summary>
        private readonly Dictionary<MGElement, GridCell> ChildCellLookup = new();
        /// <summary>A lookup table that returns the <see cref="GridSpan"/> settings associated with a given <see cref="MGElement"/></summary>
        private readonly Dictionary<MGElement, GridSpan> ChildSpanLookup = new();

        /// <summary>Retrieves the <see cref="GridCell"/> that the given <paramref name="Element"/> belongs to.</summary>
        public bool TryGetCell(MGElement Element, out GridCell Cell) => ChildCellLookup.TryGetValue(Element, out Cell);

        public IReadOnlyDictionary<ColumnDefinition, IReadOnlyList<MGElement>> GetRowContent(RowDefinition Row)
        {
            if (ChildrenByRC.TryGetValue(Row, out var RowContent))
                return RowContent.ToDictionary(x => x.Key, x => x.Value as IReadOnlyList<MGElement>);
            else
                return new Dictionary<ColumnDefinition, IReadOnlyList<MGElement>>();
        }

        public IReadOnlyDictionary<RowDefinition, IReadOnlyList<MGElement>> GetColumnContent(ColumnDefinition Column)
        {
            Dictionary<RowDefinition, IReadOnlyList<MGElement>> ColumnContent = new();
            foreach (var KVP in ChildrenByRC)
            {
                if (KVP.Value.TryGetValue(Column, out var CellContent) && CellContent.Any())
                {
                    ColumnContent.Add(KVP.Key, CellContent);
                }
            }
            return ColumnContent;
        }

        public IReadOnlyList<MGElement> GetCellContent(RowDefinition Row, ColumnDefinition Column) => GetCellContent(new GridCell(Row, Column));
        public IReadOnlyList<MGElement> GetCellContent(GridCell Cell)
        {
            IReadOnlyDictionary<ColumnDefinition, IReadOnlyList<MGElement>> RowContent = GetRowContent(Cell.Row);
            if (RowContent.TryGetValue(Cell.Column, out IReadOnlyList<MGElement> CellContent))
                return CellContent;
            else
                return new List<MGElement>();
        }

        private IReadOnlyList<MGElement> GetMeasurableCellContent(RowDefinition Row, ColumnDefinition Column) => GetMeasurableCellContent(new GridCell(Row, Column));
        private IReadOnlyList<MGElement> GetMeasurableCellContent(GridCell Cell) => GetCellContent(Cell).Where(x => ChildSpanLookup[x].AffectsMeasure).ToList();

        public bool TryAddChild(int RowIndex, int ColumnIndex, MGElement Item) => TryAddChild(RowIndex, ColumnIndex, GridSpan.Default, Item);
        public bool TryAddChild(int RowIndex, int ColumnIndex, GridSpan Span, MGElement Item)
        {
            if (RowIndex >= 0 && RowIndex < _Rows.Count && ColumnIndex >= 0 && ColumnIndex < _Columns.Count)
            {
                RowDefinition Row = _Rows[RowIndex];
                ColumnDefinition Column = _Columns[ColumnIndex];
                return TryAddChild(Row, Column, Span, Item);
            }
            else
                return false;
        }

        public bool TryAddChild(RowDefinition Row, ColumnDefinition Column, MGElement Item) => TryAddChild(Row, Column, GridSpan.Default, Item);
        public bool TryAddChild(RowDefinition Row, ColumnDefinition Column, GridSpan Span, MGElement Item) => TryAddChild(new GridCell(Row, Column), Span, Item);
        public bool TryAddChild(GridCell Cell, MGElement Item) => TryAddChild(Cell, GridSpan.Default, Item);

        /// <returns>True if the given <paramref name="Item"/> was successfully added.<br/>
        /// False otherwise, such as if <see cref="MGContentHost.CanChangeContent"/> is false.</returns>
        public bool TryAddChild(GridCell Cell, GridSpan Span, MGElement Item)
        {
            if (!CanChangeContent)
                return false;
            if (Item == null)
                throw new ArgumentNullException(nameof(Item));
            if (_Children.Contains(Item))
                throw new InvalidOperationException($"{nameof(MGGrid)} does not support adding the same {nameof(MGElement)} multiple times.");

            if (!ChildrenByRC.TryGetValue(Cell.Row, out var RowContent))
            {
                RowContent = new Dictionary<ColumnDefinition, List<MGElement>>();
                ChildrenByRC.Add(Cell.Row, RowContent);
            }

            if (!RowContent.TryGetValue(Cell.Column, out var CellContent))
            {
                CellContent = new List<MGElement>();
                RowContent.Add(Cell.Column, CellContent);
            }

            CellContent.Add(Item);
            ChildCellLookup[Item] = Cell;
            ChildSpanLookup[Item] = Span;

            _Children.Add(Item);
            return true;
        }

        /// <returns>True if the given <paramref name="Item"/> was found in <see cref="MGMultiContentHost.Children"/> and was successfully removed.<br/>
        /// False otherwise, such as if <see cref="MGContentHost.CanChangeContent"/> is false.</returns>
        public bool TryRemoveChild(MGElement Item)
        {
            if (!CanChangeContent)
                return false;

            if (_Children.Remove(Item))
            {
                GridCell Cell = ChildCellLookup[Item];
                ChildrenByRC[Cell.Row][Cell.Column].Remove(Item);
                ChildCellLookup.Remove(Item);
                ChildSpanLookup.Remove(Item);
                return true;
            }
            else
                return false;
        }

        /// <summary>Removes all elements from every row/column of this grid</summary>
        public bool TryRemoveAll()
        {
            if (!CanChangeContent)
                return false;

            _Children.ClearOneByOne();
            ChildrenByRC.Clear();
            ChildCellLookup.Clear();
            ChildSpanLookup.Clear();
            return true;
        }

        public List<MGElement> ClearCellContent(int RowIndex, int ColumnIndex)
        {
            if (RowIndex >= 0 && RowIndex < _Rows.Count && ColumnIndex >= 0 && ColumnIndex < _Columns.Count)
            {
                RowDefinition Row = _Rows[RowIndex];
                ColumnDefinition Column = _Columns[ColumnIndex];
                return ClearCellContent(Row, Column);
            }
            else
                return new List<MGElement>();
        }

        public List<MGElement> ClearCellContent(RowDefinition Row, ColumnDefinition Column)
        {
            if (Row == null)
                throw new ArgumentNullException(nameof(Row));
            if (Column == null)
                throw new ArgumentNullException(nameof(Column));

            List<MGElement> Removed = new();
            if (!CanChangeContent)
                return Removed;

            IReadOnlyList<MGElement> CellContent = GetCellContent(Row, Column);
            foreach (MGElement Element in CellContent)
            {
                if (_Children.Remove(Element))
                {
                    Removed.Add(Element);
                    ChildCellLookup.Remove(Element);
                    ChildSpanLookup.Remove(Element);
                }
            }

            if (Removed.Any())
            {
                if (Removed.Count == CellContent.Count)
                    ChildrenByRC[Row].Remove(Column);
                else
                {
#if DEBUG
                    throw new Exception($"{nameof(MGGrid)}.{nameof(ClearCellContent)}: failed to remove all elements in {Row},{Column}");
#else
                    ChildrenByRow[Row][Column].RemoveAll(x => Removed.Contains(x));
#endif
                }
            }

            return Removed;
        }

        public List<MGElement> ClearColumnContent(int ColumnIndex)
        {
            if (ColumnIndex >= 0 && ColumnIndex < _Columns.Count)
            {
                ColumnDefinition Column = _Columns[ColumnIndex];
                return ClearColumnContent(Column);
            }
            else
                return new List<MGElement>();
        }

        public List<MGElement> ClearColumnContent(ColumnDefinition Column)
        {
            if (Column == null)
                throw new ArgumentNullException(nameof(Column));

            List<MGElement> Removed = new();
            if (!CanChangeContent)
                return Removed;

            foreach (RowDefinition Row in Rows)
            {
                if (ChildrenByRC.TryGetValue(Row, out Dictionary<ColumnDefinition, List<MGElement>> RowContent) && RowContent.TryGetValue(Column, out List<MGElement> CellContent))
                {
                    foreach (MGElement Element in CellContent)
                    {
                        if (_Children.Remove(Element))
                        {
                            Removed.Add(Element);
                            ChildCellLookup.Remove(Element);
                            ChildSpanLookup.Remove(Element);
                        }
#if DEBUG
                        else
                        {
                            throw new Exception($"{nameof(MGGrid)}.{nameof(ClearColumnContent)}: failed to remove all elements in {Row},{Column}");
                        }
#endif
                    }

                    ChildrenByRC[Row].Remove(Column);
                }
            }

            return Removed;
        }

        public List<MGElement> ClearRowContent(int RowIndex)
        {
            if (RowIndex >= 0 && RowIndex < _Rows.Count)
            {
                RowDefinition Row = _Rows[RowIndex];
                return ClearRowContent(Row);
            }
            else
                return new List<MGElement>();
        }

        public List<MGElement> ClearRowContent(RowDefinition Row)
        {
            if (Row == null)
                throw new ArgumentNullException(nameof(Row));

            List<MGElement> Removed = new();
            if (!CanChangeContent)
                return Removed;

            if (ChildrenByRC.TryGetValue(Row, out Dictionary<ColumnDefinition, List<MGElement>> RowContent))
            {
                foreach (var KVP in RowContent)
                {
                    ColumnDefinition Column = KVP.Key;
                    foreach (MGElement Element in KVP.Value)
                    {
                        if (_Children.Remove(Element))
                        {
                            Removed.Add(Element);
                            ChildCellLookup.Remove(Element);
                            ChildSpanLookup.Remove(Element);
                        }
#if DEBUG
                        else
                        {
                            throw new Exception($"{nameof(MGGrid)}.{nameof(ClearRowContent)}: failed to remove all elements in {Row},{Column}");
                        }
#endif
                    }
                }

                ChildrenByRC.Remove(Row);
            }

            return Removed;
        }
        #endregion Elements

        #region Selection
        private MouseHandler SelectionMouseHandler { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private GridSelectionMode _SelectionMode;
        public GridSelectionMode SelectionMode
        {
            get => _SelectionMode;
            set
            {
                if (_SelectionMode != value)
                {
                    _SelectionMode = value;
                    CurrentSelection = HasSelection ? CurrentSelection.Value with { SelectionMode = SelectionMode } : CurrentSelection;
                    NPC(nameof(SelectionMode));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _CanDeselectByClickingSelectedCell = true;
        public bool CanDeselectByClickingSelectedCell
        {
            get => _CanDeselectByClickingSelectedCell;
            set
            {
                if (_CanDeselectByClickingSelectedCell != value)
                {
                    _CanDeselectByClickingSelectedCell = value;
                    NPC(nameof(CanDeselectByClickingSelectedCell));
                }
            }
        }

        private GridSelection? SelectionAtStartOfMousePress = null;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private GridSelection? _CurrentSelection;
        public GridSelection? CurrentSelection
        {
            get => _CurrentSelection;
            set
            {
                if (_CurrentSelection != value)
                {
                    _CurrentSelection = value;
                    NPC(nameof(CurrentSelection));
                    NPC(nameof(HasSelection));
                    SelectionChanged?.Invoke(this, CurrentSelection);
                }
            }
        }

        public event EventHandler<GridSelection?> SelectionChanged;
        public bool HasSelection => CurrentSelection.HasValue;

        /// <param name="MousePosition">The current mouse position in <see cref="CoordinateSpace.Layout"/><para/>
        /// See also: <see cref="MGElement.ConvertCoordinateSpace(CoordinateSpace, CoordinateSpace, Point)"/></param>
        private void UpdateSelection(Point MousePosition, bool AllowDeselect)
        {
            AllowDeselect = AllowDeselect && CanDeselectByClickingSelectedCell;

            Rectangle Viewport = LayoutBounds; // Does this also need to be translated by this.Origin?
            if (TryFindParentOfType(out MGScrollViewer SV, false))
            {
                //TODO test this logic for ScrollViewers that are nested inside of another ScrollViewer
                //  It might be: this.Origin + SV.Origin; Idk
                Viewport = SV.ContentViewport.GetTranslated(Origin - SV.Origin);
            }

            GridCell? Cell = null;
            foreach (var KVP in _CellBounds)
            {
                if (KVP.Value.ContainsInclusive(MousePosition) && Viewport.ContainsInclusive(MousePosition))
                {
                    Cell = KVP.Key;
                    break;
                }
            }

            if (Cell.HasValue)
            {
                bool ClickedExistingSelection = false;
                if (HasSelection && SelectionAtStartOfMousePress.HasValue)
                {
                    GridCell PreviousCell = SelectionAtStartOfMousePress.Value.Cell;
                    GridCell CurrentCell = CurrentSelection.Value.Cell;
                    GridCell ClickedCell = Cell.Value;

                    ClickedExistingSelection = SelectionMode switch
                    {
                        GridSelectionMode.None => false,
                        GridSelectionMode.Row => PreviousCell.Row == ClickedCell.Row && CurrentCell.Row == ClickedCell.Row,
                        GridSelectionMode.Column => PreviousCell.Column == ClickedCell.Column && CurrentCell.Column == ClickedCell.Column,
                        GridSelectionMode.Cell => PreviousCell == ClickedCell && CurrentCell == ClickedCell,
                        _ => throw new NotImplementedException($"Unrecognized {nameof(GridSelectionMode)}: {SelectionMode}")
                    };
                }

                if (AllowDeselect && ClickedExistingSelection)
                    CurrentSelection = null;
                else
                    CurrentSelection = new GridSelection(this, Cell.Value, SelectionMode);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IFillBrush _SelectionBackground = MGSolidFillBrush.Yellow * 0.5f;
        public IFillBrush SelectionBackground
        {
            get => _SelectionBackground;
            set
            {
                if (_SelectionBackground != value)
                {
                    _SelectionBackground = value;
                    NPC(nameof(SelectionBackground));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IFillBrush _SelectionOverlay = new MGSolidFillBrush(Color.Yellow * 0.25f);
        public IFillBrush SelectionOverlay
        {
            get => _SelectionOverlay;
            set
            {
                if (_SelectionOverlay != value)
                {
                    _SelectionOverlay = value;
                    NPC(nameof(SelectionOverlay));
                }
            }
        }
        #endregion Selection

        #region GridLines
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private GridLineIntersection _GridLineIntersectionHandling;
        /// <summary>Default value: <see cref="GridLineIntersection.HorizontalThenVertical"/></summary>
        public GridLineIntersection GridLineIntersectionHandling
        {
            get => _GridLineIntersectionHandling;
            set
            {
                if (_GridLineIntersectionHandling != value)
                {
                    _GridLineIntersectionHandling = value;
                    NPC(nameof(GridLineIntersectionHandling));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private GridLinesVisibility _GridLinesVisibility;
        /// <summary>Default value: <see cref="GridLinesVisibility.None"/></summary>
        public GridLinesVisibility GridLinesVisibility
        {
            get => _GridLinesVisibility;
            set
            {
                if (_GridLinesVisibility != value)
                {
                    _GridLinesVisibility = value;
                    CheckIfOuterPaddingChanged();
                    NPC(nameof(GridLinesVisibility));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _GridLineMargin;
        /// <summary>The amount of empty space around each gridline.<br/>
        /// For example: If horizontal gridlines are visible, and <see cref="RowSpacing"/> = 6, and <see cref="GridLineMargin"/> = 1, 
        /// then the horizontal gridline brush would fill the center 4 pixels of the 6 pixels between each row.<para/>
        /// This value should be less than half the value of <see cref="RowSpacing"/>/<see cref="ColumnSpacing"/> or else the corresponding gridline brush won't have any space to fill.</summary>
        public int GridLineMargin
        {
            get => _GridLineMargin;
            set
            {
                if (_GridLineMargin != value)
                {
                    _GridLineMargin = value;
                    CheckIfOuterPaddingChanged();
                    NPC(nameof(GridLineMargin));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IFillBrush _HorizontalGridLineBrush;
        public IFillBrush HorizontalGridLineBrush
        {
            get => _HorizontalGridLineBrush;
            set
            {
                if (_HorizontalGridLineBrush != value)
                {
                    _HorizontalGridLineBrush = value;
                    NPC(nameof(HorizontalGridLineBrush));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IFillBrush _VerticalGridLineBrush;
        public IFillBrush VerticalGridLineBrush
        {
            get => _VerticalGridLineBrush;
            set
            {
                if (_VerticalGridLineBrush != value)
                {
                    _VerticalGridLineBrush = value;
                    NPC(nameof(VerticalGridLineBrush));
                }
            }
        }
        #endregion GridLines

        private void CheckIfOuterPaddingChanged()
        {
            if (RowSpacing > 0 && GridLineMargin < RowSpacing &&
                (GridLinesVisibility.HasFlag(GridLinesVisibility.TopEdge) || GridLinesVisibility.HasFlag(GridLinesVisibility.BottomEdge)))
            {
                LayoutChanged(this, true);
            }
            else if (ColumnSpacing > 0 && GridLineMargin < ColumnSpacing &&
                (GridLinesVisibility.HasFlag(GridLinesVisibility.LeftEdge) || GridLinesVisibility.HasFlag(GridLinesVisibility.RightEdge)))
            {
                LayoutChanged(this, true);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _RowSpacing;
        /// <summary>The amount of padding, in pixels, between each consecutive row.<para/>
        /// Default value: 0<para/>
        /// If horizontal gridlines are visible, this value should be at least more than twice as large as <see cref="GridLineMargin"/></summary>
        public int RowSpacing
        {
            get => _RowSpacing;
            set
            {
                if (_RowSpacing != value)
                {
                    _RowSpacing = value;
                    LayoutChanged(this, true);
                    NPC(nameof(RowSpacing));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _ColumnSpacing;
        /// <summary>The amount of padding, in pixels, between each consecutive column.<para/>
        /// Default value: 0<para/>
        /// If vertical gridlines are visible, this value should be at least more than twice as large as <see cref="GridLineMargin"/></summary>
        public int ColumnSpacing
        {
            get => _ColumnSpacing;
            set
            {
                if (_ColumnSpacing != value)
                {
                    _ColumnSpacing = value;
                    LayoutChanged(this, true);
                    NPC(nameof(ColumnSpacing));
                }
            }
        }

        public MGGrid(MGWindow Window)
            : base(Window, MGElementType.Grid)
        {
            using (BeginInitializing())
            {
                _Columns = new();
                _Columns.CollectionChanged += (sender, e) =>
                {
                    if (e.Action is NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Replace or NotifyCollectionChangedAction.Reset)
                    {
                        LayoutChanged(this, true);
                    }

                    if (e.Action is NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Replace && e.NewItems != null)
                    {
                        foreach (ColumnDefinition Item in e.NewItems)
                            Item.DimensionsChanged += RowColumn_DimensionsChanged;
                    }

                    if (e.Action is NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Replace or NotifyCollectionChangedAction.Reset && e.OldItems != null)
                    {
                        foreach (ColumnDefinition Item in e.OldItems)
                            Item.DimensionsChanged -= RowColumn_DimensionsChanged;
                    }
                };

                _Rows = new();
                _Rows.CollectionChanged += (sender, e) =>
                {
                    if (e.Action is NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Replace or NotifyCollectionChangedAction.Reset)
                    {
                        LayoutChanged(this, true);
                    }

                    if (e.Action is NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Replace && e.NewItems != null)
                    {
                        foreach (RowDefinition Item in e.NewItems)
                            Item.DimensionsChanged += RowColumn_DimensionsChanged;
                    }

                    if (e.Action is NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Replace or NotifyCollectionChangedAction.Reset && e.OldItems != null)
                    {
                        foreach (RowDefinition Item in e.OldItems)
                            Item.DimensionsChanged -= RowColumn_DimensionsChanged;
                    }
                };

                RowSpacing = 0;
                ColumnSpacing = 0;

                GridLineIntersectionHandling = GridLineIntersection.HorizontalThenVertical;

                SelectionMouseHandler = InputTracker.Mouse.CreateHandler(this, null, false, true);
                SelectionMouseHandler.LMBPressedInside += (sender, e) =>
                {
                    if (!ParentWindow.HasModalWindow)
                    {
                        SelectionAtStartOfMousePress = CurrentSelection;
                        Point Position = ConvertCoordinateSpace(CoordinateSpace.Screen, CoordinateSpace.Layout, e.Position);
                        UpdateSelection(Position, false);
                    }
                };
                SelectionMouseHandler.LMBReleasedInside += (sender, e) =>
                {
                    if (!ParentWindow.HasModalWindow)
                    {
                        Point Position = ConvertCoordinateSpace(CoordinateSpace.Screen, CoordinateSpace.Layout, e.Position);
                        UpdateSelection(Position, true);
                    }
                };

                OnEndingDraw += (sender, e) =>
                {
                    //  Draw the selection overlay
                    if (HasSelection && SelectionOverlay != null)
                    {
                        Rectangle? ScissorBounds = e.DA.DT.GD.RasterizerState.ScissorTestEnable ? e.DA.DT.GD.ScissorRectangle : null;
                        foreach (GridCell Cell in CurrentSelection.Value)
                        {
                            if (_CellBounds.TryGetValue(Cell, out Rectangle Bounds))
                            {
                                Rectangle ScreenSpaceBounds = ConvertCoordinateSpace(CoordinateSpace.UnscaledScreen, CoordinateSpace.Screen, Bounds.GetTranslated(e.DA.Offset));
                                if (ScissorBounds.HasValue && ScreenSpaceBounds.Intersects(ScissorBounds.Value))
                                    SelectionOverlay.Draw(e.DA, this, Bounds);
                            }
                        }
                    }
                };

                SelectionMode = GridSelectionMode.None;
                CurrentSelection = null;

                Window.OnWindowPositionChanged += (sender, e) =>
                {
                    Point Offset = new(e.NewValue.Left - e.PreviousValue.Left, e.NewValue.Top - e.PreviousValue.Top);
                    foreach (ColumnDefinition Column in Columns)
                        Column.Left += Offset.X;
                    foreach (RowDefinition Row in Rows)
                        Row.Top += Offset.Y;
                    foreach (var KVP in _CellBounds.ToList())
                        _CellBounds[KVP.Key] = KVP.Value.GetTranslated(Offset);
                };
            }
        }

        private void RowColumn_DimensionsChanged(object sender, EventArgs e)
        {
            if (!SuppressDimensionChanged)
                LayoutChanged(this, true);
        }

        public override void UpdateSelf(ElementUpdateArgs UA)
        {
            if (UA.IsHitTestVisible)
            {
                SelectionMouseHandler.ManualUpdate();
            }

            base.UpdateSelf(UA);
        }

        private readonly record struct GridDimensions(Dictionary<ColumnDefinition, int> ColumnWidths, Dictionary<RowDefinition, int> RowHeights, int TotalWidth, int TotalHeight);

        /// <param name="IsMeasuring">True if measuring the grid's content (will result in * lengths being treated as Auto, so that this method can compute the minimally-required dimensions to show the content).<br/>
        /// False if the bounds of this element have already been allocated (will allow * lengths to stretch all available space)</param>
        private GridDimensions ComputeDimensions(Size AvailableSize, bool IsMeasuring)
        {
            Dictionary<ColumnDefinition, int> ColumnWidths = new();
            Dictionary<RowDefinition, int> RowHeights = new();

            //  If Width or Height is arbitrarily large, this element is being measured within a ScrollViewer.
            //  Which means we can't just request all the AvailableSize, or we'd end up with infinitely-sized content inside the ScrollViewer.
            bool IsPseudoInfiniteWidth = AvailableSize.Width >= 1000000;
            bool IsPseduoInfiniteHeight = AvailableSize.Height >= 1000000;

            double TotalColumnWeight = Columns.Select(x => x.Length).Where(x => x.IsWeightedLength).Sum(x => x.Weight);
            double RemainingColumnWeight = TotalColumnWeight;
            double TotalRowWeight = Rows.Select(x => x.Length).Where(x => x.IsWeightedLength).Sum(x => x.Weight);
            double RemainingRowWeight = TotalRowWeight;

            int TotalColumnSpacingWidth = (Columns.Count - 1) * ColumnSpacing;
            if (GridLinesVisibility.HasFlag(GridLinesVisibility.LeftEdge))
                TotalColumnSpacingWidth += Math.Max(0, ColumnSpacing - GridLineMargin);
            if (GridLinesVisibility.HasFlag(GridLinesVisibility.RightEdge))
                TotalColumnSpacingWidth += Math.Max(0, ColumnSpacing - GridLineMargin);

            int TotalRowSpacingHeight = (Rows.Count - 1) * RowSpacing;
            if (GridLinesVisibility.HasFlag(GridLinesVisibility.TopEdge))
                TotalRowSpacingHeight += Math.Max(0, RowSpacing - GridLineMargin);
            if (GridLinesVisibility.HasFlag(GridLinesVisibility.BottomEdge))
                TotalRowSpacingHeight += Math.Max(0, RowSpacing - GridLineMargin);

            //  Fill in the trivial column measurements where we know exactly how wide they are
            int TotalWidth = TotalColumnSpacingWidth;
            int RemainingColumnWidth = Math.Max(0, AvailableSize.Width - TotalColumnSpacingWidth);
            foreach (ColumnDefinition Column in Columns)
            {
                int? ColumnWidth = null;
                if (Column.Length.IsPixelLength)
                    ColumnWidth = Column.Length.Pixels;
                else if (Column.MinWidth.HasValue && Column.MaxWidth.HasValue && Column.MinWidth == Column.MaxWidth)
                    ColumnWidth = Column.MinWidth.Value;

                if (ColumnWidth.HasValue)
                {
                    int Width = GeneralUtils.Min(RemainingColumnWidth, ColumnWidth.Value, Column.MaxWidth ?? int.MaxValue);
                    ColumnWidths.Add(Column, Width);
                    TotalWidth += Width;
                    RemainingColumnWidth -= Width;
                }
            }

            //  Fill in the trival row measurements where we know exactly how tall they are
            int TotalHeight = TotalRowSpacingHeight;
            int RemainingRowHeight = Math.Max(0, AvailableSize.Height - TotalRowSpacingHeight);
            foreach (RowDefinition Row in Rows)
            {
                int? RowHeight = null;
                if (Row.Length.IsPixelLength)
                    RowHeight = Row.Length.Pixels;
                else if (Row.MinHeight.HasValue && Row.MaxHeight.HasValue && Row.MinHeight == Row.MaxHeight)
                    RowHeight = Row.MinHeight.Value;

                if (RowHeight.HasValue)
                {
                    int Height = GeneralUtils.Min(RemainingRowHeight, RowHeight.Value, Row.MaxHeight ?? int.MaxValue);
                    RowHeights.Add(Row, Height);
                    TotalHeight += Height;
                    RemainingRowHeight -= Height;
                }
            }

            //  Measure every remaining column, starting with Auto-length columns
            IEnumerable<ColumnDefinition> RemainingColumns = Columns
                .Where(x => !ColumnWidths.ContainsKey(x))
                .OrderBy(x => x.Length.IsAutoLength || IsPseudoInfiniteWidth ? 0 : 1)
                .ThenBy(x => x.MaxWidth ?? int.MaxValue) // Try to handle columns with a MaxWidth first because if the column's width gets truncated, it might free up more width for the next weighted column to use
                .ThenByDescending(x => x.MinWidth.HasValue); // Try to handle columns with a MinWidth first because if the column's width gets increased upwards to the MinWidth, it consumes more space than usual, leaving less space for the next weighted column to use
            foreach (ColumnDefinition Column in RemainingColumns)
            {
                int ColumnWidth;
                if (RemainingColumnWidth <= 0)
                    ColumnWidth = 0;
                else if (Column.MinWidth.HasValue && RemainingColumnWidth <= Column.MinWidth.Value)
                    ColumnWidth = Column.MinWidth.Value;
                else
                {
                    bool IsWeightedWidth = Column.Length.IsWeightedLength && !IsPseudoInfiniteWidth; // If measured inside a scrollviewer, * lengths are treated as Auto
                    if (IsWeightedWidth && !IsMeasuring)
                    {
                        double ColumnWeight = Column.Length.Weight;
#if DEBUG
                        if (ColumnWeight > RemainingColumnWeight)
                            throw new InvalidOperationException("Column weight should not exceed remaining weight");
#endif
                        ColumnWidth = Math.Clamp((int)Math.Round(RemainingColumnWidth * (ColumnWeight / RemainingColumnWeight), MidpointRounding.ToEven), Column.MinWidth ?? 0, Column.MaxWidth ?? int.MaxValue);
                        RemainingColumnWeight -= ColumnWeight;
                    }
                    else
                    {
                        //  Note: If we're measuring the content (pre-processing phase), rather than allocating space to the content, we need to know the minimum dimensions required to show the content.
                        //  So weighted columns are treated like Auto-sized columns (with their width capped at what the weighted width would be)
                        int WeightedWidth = int.MaxValue;
                        if (IsWeightedWidth)
                        {
                            double ColumnWeight = Column.Length.Weight;
                            WeightedWidth = Math.Clamp((int)Math.Round(RemainingColumnWidth * (ColumnWeight / RemainingColumnWeight), MidpointRounding.ToEven), Column.MinWidth ?? 0, Column.MaxWidth ?? int.MaxValue);
                            RemainingColumnWeight -= ColumnWeight;
                        }

                        //  Measure every element in this column
                        List<int> ColumnChildWidths = new();
                        int CellAvailableWidth = GeneralUtils.Min(RemainingColumnWidth, WeightedWidth, Column.MaxWidth ?? int.MaxValue);
                        foreach (RowDefinition Row in Rows)
                        {
                            GridCell Cell = new(Row, Column);
                            if (!RowHeights.TryGetValue(Row, out int RowHeight))
                                RowHeight = RemainingRowHeight; // Lazy 'solution' because I'm too dumb to come up with the actual correct logic that avoids circular dependencies...

                            Size CellAvailableSize = new(CellAvailableWidth, RowHeight);
                            foreach (MGElement Element in GetMeasurableCellContent(Cell))
                            {
                                Element.UpdateMeasurement(CellAvailableSize, out _, out Thickness ElementSize, out _, out _);
                                ColumnChildWidths.Add(ElementSize.Size.Width);
                            }
                        }

                        //  Take the maximum width of the row items in this column
                        ColumnWidth = Math.Max(Column.MinWidth ?? 0, ColumnChildWidths.DefaultIfEmpty(0).Max());
                    }
                }

                ColumnWidth = GeneralUtils.Min(RemainingColumnWidth, ColumnWidth, Column.MaxWidth ?? int.MaxValue);
                ColumnWidths.Add(Column, ColumnWidth);
                TotalWidth += ColumnWidth;
                RemainingColumnWidth -= ColumnWidth;
            }

            //  Measure every remaining row, starting with Auto-length rows
            IEnumerable<RowDefinition> RemainingRows = Rows
                .Where(x => !RowHeights.ContainsKey(x))
                .OrderBy(x => x.Length.IsAutoLength || IsPseduoInfiniteHeight ? 0 : 1)
                .ThenBy(x => x.MaxHeight ?? int.MaxValue) // Try to handle rows with a MaxHeight first because if the row's height gets truncated, it might free up more height for the next weighted row to use
                .ThenByDescending(x => x.MinHeight.HasValue); // Try to handle rows with a MinHeight first because if the row's height gets increased upwards to the MinHeight, it consumes more space than usual, leaving less space for the next weighted row to use
            foreach (RowDefinition Row in RemainingRows)
            {
                int RowHeight;
                if (RemainingRowHeight <= 0)
                    RowHeight = 0;
                else if (Row.MinHeight.HasValue && RemainingRowHeight <= Row.MinHeight.Value)
                    RowHeight = Row.MinHeight.Value;
                else
                {
                    bool IsWeightedHeight = Row.Length.IsWeightedLength && !IsPseduoInfiniteHeight; // If measured inside a scrollviewer, * lengths are treated as Auto
                    if (IsWeightedHeight && !IsMeasuring)
                    {
                        double RowWeight = Row.Length.Weight;
#if DEBUG
                        if (RowWeight > RemainingRowWeight)
                            throw new InvalidOperationException("Row weight should not exceed remaining weight");
#endif
                        RowHeight = Math.Clamp((int)Math.Round(RemainingRowHeight * (RowWeight / RemainingRowWeight), MidpointRounding.ToEven), Row.MinHeight ?? 0, Row.MaxHeight ?? int.MaxValue);
                        RemainingRowWeight -= RowWeight;
                    }
                    else
                    {
                        //  Note: If we're measuring the content (pre-processing phase), rather than allocating space to the content, we need to know the minimum dimensions required to show the content.
                        //  So weighted rows are treated like Auto-sized rows (with their height capped at what the weighted height would be)
                        int WeightedHeight = int.MaxValue;
                        if (IsWeightedHeight)
                        {
                            double RowWeight = Row.Length.Weight;
                            WeightedHeight = Math.Clamp((int)Math.Round(RemainingRowHeight * (RowWeight / RemainingRowWeight), MidpointRounding.ToEven), Row.MinHeight ?? 0, Row.MaxHeight ?? int.MaxValue);
                            RemainingRowWeight -= RowWeight;
                        }

                        //  Measure every element in this row
                        List<int> RowChildHeights = new();
                        int CellAvailableHeight = GeneralUtils.Min(RemainingRowHeight, WeightedHeight, Row.MaxHeight ?? int.MaxValue);
                        for (int ColumnIndex = 0; ColumnIndex < _Columns.Count; ColumnIndex++)
                        {
                            ColumnDefinition Column = _Columns[ColumnIndex];
                            GridCell Cell = new(Row, Column);
                            int ColumnWidth = ColumnWidths[Column];

                            Size CellAvailableSize = new(ColumnWidth, CellAvailableHeight);
                            foreach (MGElement Element in GetMeasurableCellContent(Cell))
                            {
                                int AvailableWidth = CellAvailableSize.Width;

                                //  Measure the element using the total width of the spanned columns
                                int ColumnSpan = ChildSpanLookup[Element].ColumnSpan;
                                if (ColumnSpan != 1)
                                {
                                    AvailableWidth = Enumerable.Range(ColumnIndex, ColumnSpan).Sum(x => ColumnWidths[_Columns[x]]) + (ColumnSpan - 1) * ColumnSpacing;
                                }

                                Element.UpdateMeasurement(new Size(AvailableWidth, CellAvailableSize.Height), out _, out Thickness ElementSize, out _, out _);
                                RowChildHeights.Add(ElementSize.Size.Height);
                            }
                        }

                        //  Take the maximum height of the items in this row
                        RowHeight = Math.Max(Row.MinHeight ?? 0, RowChildHeights.DefaultIfEmpty(0).Max());
                    }
                }

                RowHeight = GeneralUtils.Min(RemainingRowHeight, RowHeight, Row.MaxHeight ?? int.MaxValue);
                RowHeights.Add(Row, RowHeight);
                TotalHeight += RowHeight;
                RemainingRowHeight -= RowHeight;
            }

            return new GridDimensions(ColumnWidths, RowHeights, TotalWidth, TotalHeight);
        }

        protected override Thickness UpdateContentMeasurement(Size AvailableSize)
        {
            if (!HasContent)
                return UpdateContentMeasurementBaseImplementation(AvailableSize);

            GridDimensions Dimensions = ComputeDimensions(AvailableSize, true);
            return new Thickness(Dimensions.TotalWidth, Dimensions.TotalHeight, 0, 0);
        }

        protected override void UpdateContentLayout(Rectangle Bounds)
        {
            Size AvailableSize = new(Bounds.Width, Bounds.Height);

            GridDimensions Dimensions = ComputeDimensions(AvailableSize, false);
            Dictionary<ColumnDefinition, int> ColumnWidths = Dimensions.ColumnWidths;
            Dictionary<RowDefinition, int> RowHeights = Dimensions.RowHeights;
            Size TotalContentSize = new(Dimensions.TotalWidth, Dimensions.TotalHeight);

            //  Account for content alignment
            int ConsumedWidth = HorizontalContentAlignment == HorizontalAlignment.Stretch ? AvailableSize.Width : Math.Min(AvailableSize.Width, TotalContentSize.Width);
            Size ConsumedContentSize = new(ConsumedWidth, TotalContentSize.Height);
            Rectangle AlignedBounds = ApplyAlignment(Bounds, HorizontalContentAlignment, VerticalContentAlignment, ConsumedContentSize);

            int CurrentX = AlignedBounds.Left;
            if (GridLinesVisibility.HasFlag(GridLinesVisibility.LeftEdge))
                CurrentX += Math.Max(0, ColumnSpacing - GridLineMargin);

            //  Set the bounds of each cell
            foreach (ColumnDefinition Column in Columns)
            {
                int ColumnWidth = ColumnWidths[Column];
                Column.Left = CurrentX;
                Column.Width = ColumnWidth;

                int CurrentY = AlignedBounds.Top;
                if (GridLinesVisibility.HasFlag(GridLinesVisibility.TopEdge))
                    CurrentY += Math.Max(0, RowSpacing - GridLineMargin);

                foreach (RowDefinition Row in Rows)
                {
                    int RowHeight = RowHeights[Row];
                    Row.Top = CurrentY;
                    Row.Height = RowHeight;

                    CurrentY += RowHeight + RowSpacing;
                }

                CurrentX += ColumnWidth + ColumnSpacing;
            }

            _CellBounds = GetCellBounds(true);

            //  Allocate space for each child
            for (int ColumnIndex = 0; ColumnIndex < Columns.Count; ColumnIndex++)
            {
                ColumnDefinition Column = Columns[ColumnIndex];
                for (int RowIndex = 0; RowIndex < Rows.Count; RowIndex++)
                {
                    RowDefinition Row = Rows[RowIndex];

                    GridCell Cell = new(Row, Column);
                    foreach (MGElement Child in GetCellContent(Cell))
                    {
                        GridSpan Span = ChildSpanLookup[Child];

                        //  Get the bounds of each cell this element spans
                        List<Rectangle> SpannedCellBounds = new();
                        for (int ColumnOffset = 0; ColumnOffset < Span.ColumnSpan; ColumnOffset++)
                        {
                            for (int RowOffset = 0; RowOffset < Span.RowSpan; RowOffset++)
                            {
                                GridCell SpannedCell = new(_Rows[RowIndex + RowOffset], _Columns[ColumnIndex + ColumnOffset]);
                                SpannedCellBounds.Add(_CellBounds[SpannedCell]);
                            }
                        }

                        Rectangle ElementBounds = Rectangle.Intersect(Bounds, RectangleUtils.Union(SpannedCellBounds));
                        Child.UpdateLayout(ElementBounds);
                    }
                }
            }
        }

        public override void DrawSelf(ElementDrawArgs DA, Rectangle LayoutBounds)
        {
            //  Draw the selection background
            if (HasSelection && SelectionBackground != null)
            {
                Rectangle? ScissorBounds = DA.DT.GD.RasterizerState.ScissorTestEnable ? DA.DT.GD.ScissorRectangle : null;
                foreach (GridCell Cell in CurrentSelection.Value)
                {
                    if (_CellBounds.TryGetValue(Cell, out Rectangle Bounds))
                    {
                        if (ScissorBounds.HasValue && Bounds.GetTranslated(DA.Offset).Intersects(ScissorBounds.Value))
                            SelectionBackground.Draw(DA, this, Bounds);
                    }
                }
            }

            bool HasHorizontalGridLines = (GridLinesVisibility & GridLinesVisibility.AllHorizontal) != 0;
            bool HasVerticalGridLines = (GridLinesVisibility & GridLinesVisibility.AllVertical) != 0;

            if (HasHorizontalGridLines && HasVerticalGridLines)
            {
                if (GridLineIntersectionHandling == GridLineIntersection.HorizontalThenVertical)
                {
                    DrawHorizontalGridLines(DA, LayoutBounds);
                    DrawVerticalGridLines(DA, LayoutBounds);
                }
                else if (GridLineIntersectionHandling == GridLineIntersection.VerticalThenHorizontal)
                {
                    DrawVerticalGridLines(DA, LayoutBounds);
                    DrawHorizontalGridLines(DA, LayoutBounds);
                }
                else
                {
                    throw new NotImplementedException($"Unrecognized {nameof(GridLineIntersection)}: {GridLineIntersectionHandling}");
                }
            }
            else if (HasHorizontalGridLines)
                DrawHorizontalGridLines(DA, LayoutBounds);
            else if (HasVerticalGridLines)
                DrawVerticalGridLines(DA, LayoutBounds);
        }

        /// <summary>Attempts to retrieve the bottom-right <see cref="GridCell"/> whose layout has already been computed at least once.<para/>
        /// Usually this is just (_Rows[^1], _Columns[^1]) but might not be in cases where a new row/column was just added and the layout for it is yet to be calculated.</summary>
        private GridCell GetLastCellWithKnownBounds()
        {
            ColumnDefinition LastKnownColumn = _Columns.FirstOrDefault();
            for (int i = _Columns.Count - 1; i >= 0; i--)
            {
                ColumnDefinition CD = _Columns[i];
                if (CD.Left != default || CD.Width != default)
                {
                    LastKnownColumn = CD;
                    break;
                }
            }

            RowDefinition LastKnownRow = _Rows.FirstOrDefault();
            for (int i = _Rows.Count - 1; i >= 0; i--)
            {
                RowDefinition RD = _Rows[i];
                if (RD.Top != default || RD.Height != default)
                {
                    LastKnownRow = RD;
                    break;
                }
            }

            return new(LastKnownRow, LastKnownColumn);
        }

        private void DrawHorizontalGridLines(ElementDrawArgs DA, Rectangle LayoutBounds)
        {
            if (DA.Opacity <= 0 || DA.Opacity.IsAlmostZero() || HorizontalGridLineBrush == null || Rows.Count == 0 || Columns.Count == 0)
                return;

            _CellBounds.TryGetValue(new GridCell(_Rows[0], _Columns[0]), out Rectangle TopLeftCellBounds);
            _CellBounds.TryGetValue(GetLastCellWithKnownBounds(), out Rectangle BottomRightCellBounds);

            int Left = TopLeftCellBounds.Left - Math.Max(0, ColumnSpacing - GridLineMargin);
            int Right = BottomRightCellBounds.Right + Math.Max(0, ColumnSpacing - GridLineMargin);
            int Top = TopLeftCellBounds.Top - Math.Max(0, RowSpacing - GridLineMargin);
            int Bottom = BottomRightCellBounds.Bottom + Math.Max(0, RowSpacing - GridLineMargin);

            int FilledHeight = RowSpacing - GridLineMargin * 2;

            if (GridLinesVisibility.HasFlag(GridLinesVisibility.TopEdge))
            {
                Rectangle TopGridLineBounds = new(Left, Top, Right - Left, FilledHeight);
                HorizontalGridLineBrush.Draw(DA, this, TopGridLineBounds);
            }

            if (GridLinesVisibility.HasFlag(GridLinesVisibility.BottomEdge))
            {
                Rectangle BottomGridLineBounds = new(Left, Bottom - FilledHeight, Right - Left, FilledHeight);
                HorizontalGridLineBrush.Draw(DA, this, BottomGridLineBounds);
            }

            if (GridLinesVisibility.HasFlag(GridLinesVisibility.InnerHorizontal))
            {
                for (int i = 0; i < Rows.Count; i++)
                {
                    RowDefinition Row = Rows[i];

                    if (i != Rows.Count - 1)
                    {
                        Rectangle GridLineBounds = new(Left, Row.Top + Row.Height + GridLineMargin, Right - Left, FilledHeight);
                        HorizontalGridLineBrush.Draw(DA, this, GridLineBounds);
                    }
                }
            }
        }

        private void DrawVerticalGridLines(ElementDrawArgs DA, Rectangle LayoutBounds)
        {
            if (DA.Opacity <= 0 || DA.Opacity.IsAlmostZero() || VerticalGridLineBrush == null || Columns.Count == 0 || Columns.Count == 0)
                return;

            _CellBounds.TryGetValue(new GridCell(_Rows[0], _Columns[0]), out Rectangle TopLeftCellBounds);
            _CellBounds.TryGetValue(GetLastCellWithKnownBounds(), out Rectangle BottomRightCellBounds);

            int Left = TopLeftCellBounds.Left - Math.Max(0, ColumnSpacing - GridLineMargin);
            int Right = BottomRightCellBounds.Right + Math.Max(0, ColumnSpacing - GridLineMargin);
            int Top = TopLeftCellBounds.Top - Math.Max(0, RowSpacing - GridLineMargin);
            int Bottom = BottomRightCellBounds.Bottom + Math.Max(0, RowSpacing - GridLineMargin);

            int FilledWidth = ColumnSpacing - GridLineMargin * 2;

            if (GridLinesVisibility.HasFlag(GridLinesVisibility.LeftEdge))
            {
                Rectangle LeftGridLineBounds = new(Left, Top, FilledWidth, Bottom - Top);
                VerticalGridLineBrush.Draw(DA, this, LeftGridLineBounds);
            }

            if (GridLinesVisibility.HasFlag(GridLinesVisibility.RightEdge))
            {
                Rectangle RightGridLineBounds = new(Right - FilledWidth, Top, FilledWidth, Bottom - Top);
                VerticalGridLineBrush.Draw(DA, this, RightGridLineBounds);
            }

            if (GridLinesVisibility.HasFlag(GridLinesVisibility.InnerVertical))
            {
                for (int i = 0; i < Columns.Count; i++)
                {
                    ColumnDefinition Column = Columns[i];

                    if (i != Columns.Count - 1)
                    {
                        Rectangle GridLineBounds = new(Column.Left + Column.Width + GridLineMargin, Top, FilledWidth, Bottom - Top);
                        VerticalGridLineBrush.Draw(DA, this, GridLineBounds);
                    }
                }
            }
        }
    }
}
