using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Shared.Helpers;
using MGUI.Shared.Input.Mouse;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI.Containers.Grids
{
    public readonly record struct GridCellIndex(int Row, int Column);

    public readonly record struct StaticGridSelection(MGUniformGrid Grid, GridCellIndex Cell, GridSelectionMode SelectionMode)
        : IEnumerable<GridCellIndex>
    {
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<GridCellIndex> GetEnumerator() => GetCells().GetEnumerator();

        public MGUniformGrid Grid { get; init; } = Grid ?? throw new ArgumentNullException(nameof(Grid));

        private IEnumerable<GridCellIndex> GetCells()
        {
            switch (SelectionMode)
            {
                case GridSelectionMode.None:
                    yield break;
                case GridSelectionMode.Row:
                    if (Grid.IsValidRowIndex(Cell.Row))
                    {
                        for (int ColumnIndex = 0; ColumnIndex < Grid.Columns; ColumnIndex++)
                            yield return new GridCellIndex(Cell.Row, ColumnIndex);
                    }
                    yield break;
                case GridSelectionMode.Column:
                    if (Grid.IsValidColumnIndex(Cell.Column))
                    {
                        for (int RowIndex = 0; RowIndex < Grid.Rows; RowIndex++)
                            yield return new GridCellIndex(RowIndex, Cell.Column);
                    }
                    yield break;
                case GridSelectionMode.Cell:
                    if (Grid.IsValidCellIndex(Cell))
                        yield return Cell;
                    yield break;
                default:
                    throw new NotImplementedException($"Unrecognized {nameof(GridSelectionMode)}: {SelectionMode}");
            }
        }
    }

    /// <summary>Represents a 2d grid of cells, where each cell has a known, uniform size (such as a player's inventory)</summary>
    public class MGUniformGrid : MGMultiContentHost
    {
        #region Dimensions
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _Rows;
        /// <summary>Determines how many rows the grid contains.</summary>
        public int Rows
        {
            get => _Rows;
            set
            {
                if (_Rows != value)
                {
                    int PreviousRows = Rows;
                    _Rows = value;
                    if (Rows < PreviousRows)
                    {
                        for (int i = PreviousRows - 1; i >= Rows; i--)
                            _ = ClearRowContent(i);
                    }
                    LayoutChanged(this, true);
                    NPC(nameof(Rows));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _Columns;
        ///<summary>Determines how many columns the grid contains.</summary>
        public int Columns
        {
            get => _Columns;
            set
            {
                if (_Columns != value)
                {
                    int PreviousColumns = Columns;
                    _Columns = value;
                    if (Columns < PreviousColumns)
                    {
                        for (int i = PreviousColumns - 1; i >= Columns; i--)
                            _ = ClearColumnContent(i);
                    }
                    LayoutChanged(this, true);
                    NPC(nameof(Columns));
                }
            }
        }

        public bool IsValidRowIndex(int Index) => Index >= 0 && Index < Rows;
        public bool IsValidColumnIndex(int Index) => Index >= 0 && Index < Columns;
        public bool IsValidCellIndex(GridCellIndex Index) => IsValidRowIndex(Index.Row) && IsValidColumnIndex(Index.Column);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Size _CellSize;
        /// <summary>The dimensions of each cell.<para/>
        /// See also:<br/><see cref="HeaderRowHeight"/><br/><see cref="HeaderColumnWidth"/></summary>
        public Size CellSize
        {
            get => _CellSize;
            set
            {
                if (_CellSize != value)
                {
                    _CellSize = value;
                    LayoutChanged(this, true);
                    NPC(nameof(CellSize));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int? _HeaderRowHeight;
        /// <summary>If not null, this value overrides the height of the first row, rather than using <see cref="CellSize"/>.Height</summary>
        public int? HeaderRowHeight
        {
            get => _HeaderRowHeight;
            set
            {
                if (_HeaderRowHeight != value)
                {
                    _HeaderRowHeight = value;
                    LayoutChanged(this, true);
                    NPC(nameof(HeaderRowHeight));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int? _HeaderColumnWidth;
        /// <summary>If not null, this value overrides the width of the first column, rather than using <see cref="CellSize"/>.Width</summary>
        public int? HeaderColumnWidth
        {
            get => _HeaderColumnWidth;
            set
            {
                if (_HeaderColumnWidth != value)
                {
                    _HeaderColumnWidth = value;
                    LayoutChanged(this, true);
                    NPC(nameof(HeaderColumnWidth));
                }
            }
        }

        private Dictionary<GridCellIndex, Rectangle> _CellBounds = new();
        /// <summary>Warning - the <see cref="Rectangle"/>s in this dictionary do not account for <see cref="MGElement.Origin"/><para/>
        /// See also: <see cref="MGElement.ConvertCoordinateSpace(CoordinateSpace, CoordinateSpace, Point)"/></summary>
        public IReadOnlyDictionary<GridCellIndex, Rectangle> CellBounds => _CellBounds;

        private Dictionary<GridCellIndex, Rectangle> GetCellBounds(Rectangle LayoutBounds, bool IncludeGridLineMargin)
        {
            Dictionary<GridCellIndex, Rectangle> CellBounds = new();

            int CurrentX = LayoutBounds.Left + Padding.Left;
            if (GridLinesVisibility.HasFlag(GridLinesVisibility.LeftEdge))
                CurrentX += Math.Max(0, ColumnSpacing - GridLineMargin);

            for (int ColumnIndex = 0; ColumnIndex < this.Columns; ColumnIndex++)
            {
                int ColumnWidth = ColumnIndex == 0 && HeaderColumnWidth.HasValue ? HeaderColumnWidth.Value : CellSize.Width;

                int CurrentY = LayoutBounds.Top + Padding.Top;
                if (GridLinesVisibility.HasFlag(GridLinesVisibility.TopEdge))
                    CurrentY += Math.Max(0, RowSpacing - GridLineMargin);

                for (int RowIndex = 0; RowIndex < this.Rows; RowIndex++)
                {
                    int RowHeight = RowIndex == 0 && HeaderRowHeight.HasValue ? HeaderRowHeight.Value : CellSize.Height;

                    GridCellIndex Cell = new(RowIndex, ColumnIndex);
                    Rectangle PaddedBounds = new(CurrentX, CurrentY, ColumnWidth, RowHeight);
                    Rectangle ActualBounds = IncludeGridLineMargin ? PaddedBounds : PaddedBounds.GetExpanded(Math.Max(0, GridLineMargin));
                    CellBounds.Add(Cell, ActualBounds);

                    CurrentY += RowHeight + RowSpacing;
                }

                CurrentX += ColumnWidth + ColumnSpacing;
            }

            return CellBounds;
        }
        #endregion Dimensions

        #region Elements
        /// <summary>All content in this <see cref="MGUniformGrid"/>, indexed by the <see cref="GridCellIndex"/> it resides in.</summary>
        private readonly Dictionary<GridCellIndex, List<MGElement>> ChildrenByRC = new();
        /// <summary>A lookup table that returns the <see cref="GridCellIndex"/> that a given <see cref="MGElement"/> resides in.</summary>
        private readonly Dictionary<MGElement, GridCellIndex> ChildCellLookup = new();

        /// <summary>Retrieves the <see cref="GridCellIndex"/> that the given <paramref name="Element"/> belongs to.</summary>
        public bool TryGetCell(MGElement Element, out GridCellIndex Cell) => ChildCellLookup.TryGetValue(Element, out Cell);

        public IReadOnlyDictionary<GridCellIndex, IReadOnlyList<MGElement>> GetRowContent(int Row)
        {
            Dictionary<GridCellIndex, List<MGElement>> RowContent = new();
            for (int Column = 0; Column < Columns; Column++)
            {
                GridCellIndex Cell = new(Row, Column);
                if (ChildrenByRC.TryGetValue(Cell, out List<MGElement> Elements))
                    RowContent.Add(Cell, Elements);
            }
            return RowContent.ToDictionary(x => x.Key, x => x.Value as IReadOnlyList<MGElement>);
        }

        public IReadOnlyDictionary<GridCellIndex, IReadOnlyList<MGElement>> GetColumnContent(int Column)
        {
            Dictionary<GridCellIndex, List<MGElement>> ColumnContent = new();
            for (int Row = 0; Row < Rows; Row++)
            {
                GridCellIndex Cell = new(Row, Column);
                if (ChildrenByRC.TryGetValue(Cell, out List<MGElement> Elements))
                    ColumnContent.Add(Cell, Elements);
            }
            return ColumnContent.ToDictionary(x => x.Key, x => x.Value as IReadOnlyList<MGElement>);
        }

        public IReadOnlyList<MGElement> GetCellContent(int Row, int Column) => GetCellContent(new GridCellIndex(Row, Column));
        public IReadOnlyList<MGElement> GetCellContent(GridCellIndex Cell)
        {
            if (ChildrenByRC.TryGetValue(Cell, out List<MGElement> Elements))
                return Elements;
            else
                return new List<MGElement>();
        }

        public bool TryAddChild(int Row, int Column, MGElement Item) => TryAddChild(new GridCellIndex(Row, Column), Item);
        /// <returns>True if the given <paramref name="Item"/> was successfully added.<br/>
        /// False otherwise, such as if <see cref="MGContentHost.CanChangeContent"/> is false.</returns>
        public bool TryAddChild(GridCellIndex Cell, MGElement Item)
        {
            if (!CanChangeContent)
                return false;
            if (Item == null)
                throw new ArgumentNullException(nameof(Item));
            if (_Children.Contains(Item))
                throw new InvalidOperationException($"{nameof(MGUniformGrid)} does not support adding the same {nameof(MGElement)} multiple times.");
            if (!IsValidCellIndex(Cell))
                throw new ArgumentOutOfRangeException($"Cell: {Cell.Row},{Cell.Column}. Row must be >= 0 and < {nameof(MGUniformGrid)}.{nameof(Rows)}, Column must be >= 0 and < {nameof(MGUniformGrid)}.{nameof(MGUniformGrid.Columns)}");

            if (!ChildrenByRC.TryGetValue(Cell, out var CellContent))
            {
                CellContent = new();
                ChildrenByRC.Add(Cell, CellContent);
            }

            CellContent.Add(Item);
            ChildCellLookup[Item] = Cell;

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
                GridCellIndex Cell = ChildCellLookup[Item];
                ChildrenByRC[Cell].Remove(Item);
                ChildCellLookup.Remove(Item);
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
            return true;
        }

        public List<MGElement> ClearCellContent(int Row, int Column) => ClearCellContent(new GridCellIndex(Row, Column));
        public List<MGElement> ClearCellContent(GridCellIndex Cell)
        {
            List<MGElement> Removed = new();
            if (!CanChangeContent)
                return Removed;

            IReadOnlyList<MGElement> CellContent = GetCellContent(Cell);
            foreach (MGElement Element in CellContent)
            {
                if (_Children.Remove(Element))
                {
                    Removed.Add(Element);
                    ChildCellLookup.Remove(Element);
                }
            }

            if (Removed.Any())
            {
                if (Removed.Count == CellContent.Count)
                    ChildrenByRC.Remove(Cell);
                else
                {
#if DEBUG
                    throw new Exception($"{nameof(MGGrid)}.{nameof(ClearCellContent)}: failed to remove all elements in {Cell.Row},{Cell.Column}");
#else
                    ChildrenByRC[Cell].RemoveAll(x => Removed.Contains(x));
#endif
                }
            }

            return Removed;
        }

        public List<MGElement> ClearRowContent(int Row)
        {
            List<MGElement> Removed = new();
            for (int Column = 0; Column < Columns; Column++)
            {
                GridCellIndex Cell = new(Row, Column);
                Removed.AddRange(ClearCellContent(Cell));
            }
            return Removed;
        }

        public List<MGElement> ClearColumnContent(int Column)
        {
            List<MGElement> Removed = new();
            for (int Row = 0; Row < Rows; Row++)
            {
                GridCellIndex Cell = new(Row, Column);
                Removed.AddRange(ClearCellContent(Cell));
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
                    NPC(nameof(CurrentSelection));
                    NPC(nameof(HasSelection));
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

        private StaticGridSelection? SelectionAtStartOfMousePress = null;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private StaticGridSelection? _CurrentSelection;
        public StaticGridSelection? CurrentSelection
        {
            get => _CurrentSelection;
            set
            {
                if (_CurrentSelection != value)
                {
                    _CurrentSelection = value;
                    NPC(nameof(CurrentSelection));
                    SelectionChanged?.Invoke(this, CurrentSelection);
                }
            }
        }

        public event EventHandler<StaticGridSelection?> SelectionChanged;
        public bool HasSelection => CurrentSelection.HasValue;

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

            GridCellIndex? Cell = null;
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
                    GridCellIndex PreviousCell = SelectionAtStartOfMousePress.Value.Cell;
                    GridCellIndex CurrentCell = CurrentSelection.Value.Cell;
                    GridCellIndex ClickedCell = Cell.Value;

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
                    CurrentSelection = new StaticGridSelection(this, Cell.Value, SelectionMode);
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

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private VisualStateFillBrush _CellBackground;
        public VisualStateFillBrush CellBackground
        {
            get => _CellBackground;
            set
            {
                if (_CellBackground != value)
                {
                    _CellBackground = value;
                    NPC(nameof(CellBackground));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _DrawEmptyCells;
        /// <summary>If true, the <see cref="CellBackground"/> will be drawn on cells which do not contain any child content.<para/>
        /// Default value: true.</summary>
        public bool DrawEmptyCells
        {
            get => _DrawEmptyCells;
            set
            {
                if (_DrawEmptyCells != value)
                {
                    _DrawEmptyCells = value;
                    NPC(nameof(DrawEmptyCells));
                }
            }
        }

        public class RenderCellArgs : EventArgs
        {
            public ElementDrawArgs DrawArgs { get; }
            public GridCellIndex CellIndex { get; }
            public Rectangle CellBounds { get; }

            public RenderCellArgs(ElementDrawArgs DrawArgs, GridCellIndex CellIndex, Rectangle CellBounds)
                : base()
            {
                this.DrawArgs = DrawArgs;
                this.CellIndex = CellIndex;
                this.CellBounds = CellBounds;
            }
        }
        /// <summary>Invoked on each cell whenever the cell is being drawn..<para/>
        /// This event is invoked even on cells that have no content.<br/>
        /// You may wish to check if <see cref="GetCellContent(GridCellIndex)"/> is an empty list to skip custom rendering to empty cells.</summary>
        public event EventHandler<RenderCellArgs> OnRenderCell;

        public MGUniformGrid(MGWindow Window, int Rows, int Columns, Size CellSize)
            : base(Window, MGElementType.UniformGrid)
        {
            using (BeginInitializing())
            {
                this.Rows = Rows;
                this.Columns = Columns;
                this.CellSize = CellSize;
                this.HeaderRowHeight = null;
                this.HeaderColumnWidth = null;

                this.RowSpacing = 0;
                this.ColumnSpacing = 0;

                _CellBounds = new();

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
                        foreach (GridCellIndex Cell in CurrentSelection.Value)
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

                OnLayoutBoundsChanged += (sender, e) => { _CellBounds = GetCellBounds(e.NewValue, true); };

                this.CellBackground = new(null);
                this.DrawEmptyCells = true;
            }
        }

        public override void UpdateSelf(ElementUpdateArgs UA)
        {
            if (UA.IsHitTestVisible)
            {
                SelectionMouseHandler.ManualUpdate();
            }

            base.UpdateSelf(UA);
        }

        public override Thickness MeasureSelfOverride(Size AvailableSize, out Thickness SharedSize)
        {
            SharedSize = 0;

            if (Rows <= 0 || Columns <= 0)
                return new(0);

            int TotalColumnSpacingWidth = (Columns - 1) * ColumnSpacing;
            if (GridLinesVisibility.HasFlag(GridLinesVisibility.LeftEdge))
                TotalColumnSpacingWidth += Math.Max(0, ColumnSpacing - GridLineMargin);
            if (GridLinesVisibility.HasFlag(GridLinesVisibility.RightEdge))
                TotalColumnSpacingWidth += Math.Max(0, ColumnSpacing - GridLineMargin);

            int TotalWidth = TotalColumnSpacingWidth + CellSize.Width * Columns;
            if (HeaderColumnWidth.HasValue && Columns > 0)
                TotalWidth += HeaderColumnWidth.Value - CellSize.Width;

            int TotalRowSpacingHeight = (Rows - 1) * RowSpacing;
            if (GridLinesVisibility.HasFlag(GridLinesVisibility.TopEdge))
                TotalRowSpacingHeight += Math.Max(0, RowSpacing - GridLineMargin);
            if (GridLinesVisibility.HasFlag(GridLinesVisibility.BottomEdge))
                TotalRowSpacingHeight += Math.Max(0, RowSpacing - GridLineMargin);

            int TotalHeight = TotalRowSpacingHeight + CellSize.Height * Rows;
            if (HeaderRowHeight.HasValue && Rows > 0)
                TotalHeight += HeaderRowHeight.Value - CellSize.Height;

            return new(TotalWidth, TotalHeight, 0, 0);
        }

        protected override Thickness UpdateContentMeasurement(Size AvailableSize) => new(0);

        protected override void UpdateContentLayout(Rectangle Bounds)
        {
            _CellBounds = GetCellBounds(LayoutBounds, true);
            foreach (var KVP in _CellBounds)
            {
                Rectangle CellBounds = KVP.Value;
                foreach (MGElement Element in GetCellContent(KVP.Key))
                {
                    Rectangle ElementBounds = Rectangle.Intersect(LayoutBounds, CellBounds);
                    Element.UpdateLayout(ElementBounds);
                }
            }
        }

        public override void DrawSelf(ElementDrawArgs DA, Rectangle LayoutBounds)
        {
            //  Draw the selection background
            if (HasSelection && SelectionBackground != null)
            {
                Rectangle? ScissorBounds = DA.DT.GD.RasterizerState.ScissorTestEnable ? DA.DT.GD.ScissorRectangle : null;
                foreach (GridCellIndex Cell in CurrentSelection.Value)
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

            for (int C = 0; C < Columns; C++)
            {
                for (int R = 0; R < Rows; R++)
                {
                    GridCellIndex Index = new(R, C);
                    Rectangle Bounds = _CellBounds[Index];

                    if (DrawEmptyCells || GetCellContent(Index).Count > 0)
                    {
                        CellBackground.GetUnderlay(VisualState.Primary)?.Draw(DA, this, Bounds);
                        CellBackground.GetFillOverlay(VisualState.Secondary)?.Draw(DA, this, Bounds);
                    }

                    OnRenderCell?.Invoke(this, new RenderCellArgs(DA, Index, Bounds));
                }
            }

            base.DrawSelf(DA, LayoutBounds);
        }


        private void DrawHorizontalGridLines(ElementDrawArgs DA, Rectangle LayoutBounds)
        {
            if (DA.Opacity <= 0 || DA.Opacity.IsAlmostZero() || HorizontalGridLineBrush == null || Rows == 0 || Columns == 0)
                return;

            int Left = _CellBounds[new(0, 0)].Left - Math.Max(0, ColumnSpacing - GridLineMargin);
            int Right = _CellBounds[new(0, Columns - 1)].Right + Math.Max(0, ColumnSpacing - GridLineMargin);
            int Top = _CellBounds[new(0, 0)].Top - Math.Max(0, RowSpacing - GridLineMargin);
            int Bottom = _CellBounds[new(Rows - 1, 0)].Bottom + Math.Max(0, RowSpacing - GridLineMargin);

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
                for (int i = 0; i < Rows; i++)
                {
                    if (i != Rows - 1)
                    {
                        GridCellIndex Cell = new(i, 0);
                        Rectangle CellBounds = _CellBounds[Cell];

                        Rectangle GridLineBounds = new(Left, CellBounds.Bottom + GridLineMargin, Right - Left, FilledHeight);
                        HorizontalGridLineBrush.Draw(DA, this, GridLineBounds);
                    }
                }
            }
        }

        private void DrawVerticalGridLines(ElementDrawArgs DA, Rectangle LayoutBounds)
        {
            if (DA.Opacity <= 0 || DA.Opacity.IsAlmostZero() || VerticalGridLineBrush == null || Rows == 0 || Columns == 0)
                return;

            int Left = _CellBounds[new(0, 0)].Left - Math.Max(0, ColumnSpacing - GridLineMargin);
            int Right = _CellBounds[new(0, Columns - 1)].Right + Math.Max(0, ColumnSpacing - GridLineMargin);
            int Top = _CellBounds[new(0, 0)].Top - Math.Max(0, RowSpacing - GridLineMargin);
            int Bottom = _CellBounds[new(Rows - 1, 0)].Bottom + Math.Max(0, RowSpacing - GridLineMargin);

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
                for (int i = 0; i < Columns; i++)
                {
                    if (i != Columns - 1)
                    {
                        GridCellIndex Cell = new(0, i);
                        Rectangle CellBounds = _CellBounds[Cell];

                        Rectangle GridLineBounds = new(CellBounds.Right + GridLineMargin, Top, FilledWidth, Bottom - Top);
                        VerticalGridLineBrush.Draw(DA, this, GridLineBounds);
                    }
                }
            }
        }
    }
}
