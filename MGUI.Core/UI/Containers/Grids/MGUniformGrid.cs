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

    /// <summary>Represents a 2d grid of cells, where each cell has a known, uniform size.<br/>
    /// Nested content cannot be added to the cells. Instead, content is directly rendered by the caller via events and delegates such as <see cref="RenderCell"/>.<para/>
    /// Typically used for things like a player's inventory, or a table filled with static content like skill icons</summary>
    public class MGUniformGrid : MGElement
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
                    _Rows = value;
                    LayoutChanged(this, true);
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
                    _Columns = value;
                    LayoutChanged(this, true);
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
                }
            }
        }

        private Dictionary<GridCellIndex, Rectangle> _CellBounds { get; }
        public IReadOnlyDictionary<GridCellIndex, Rectangle> CellBounds => _CellBounds;
        #endregion Dimensions

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
                }
            }
        }

        public bool CanDeselectByClickingSelectedCell { get; set; } = true;

        private StaticGridSelection? PreviousSelection = null;
        public StaticGridSelection? CurrentSelection = null;
        public bool HasSelection => CurrentSelection.HasValue;

        private void UpdateSelection(Point MousePosition, bool AllowDeselect)
        {
            AllowDeselect = AllowDeselect && CanDeselectByClickingSelectedCell;

            Rectangle Viewport = LayoutBounds; // Does this also need to be translated by this.BoundsOffset?
            if (TryFindParentOfType(out MGScrollViewer SV, false))
            {
                //TODO test this logic for ScrollViewers that are nested inside of another ScrollViewer
                //  It might be: this.BoundsOffset + SV.BoundsOffset; Idk
                Viewport = SV.ContentViewport.GetTranslated(BoundsOffset - SV.BoundsOffset);
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
                if (HasSelection && PreviousSelection.HasValue)
                {
                    GridCellIndex PreviousCell = PreviousSelection.Value.Cell;
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

        public IFillBrush SelectionBackground { get; set; } = MGSolidFillBrush.Yellow * 0.5f;
        public IFillBrush SelectionOverlay { get; set; } = new MGSolidFillBrush(Color.Yellow * 0.25f);
        #endregion Selection

        #region GridLines
        /// <summary>Default value: <see cref="GridLineIntersection.HorizontalThenVertical"/></summary>
        public GridLineIntersection GridLineIntersectionHandling { get; set; }

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
                }
            }
        }

        public IFillBrush HorizontalGridLineBrush { get; set; }
        public IFillBrush VerticalGridLineBrush { get; set; }
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
                }
            }
        }

        public VisualStateFillBrush CellBackground { get; set; }

        /// <summary>An action to invoke on each cell whenever the cell is being drawn</summary>
        public Action<ElementDrawArgs, GridCellIndex, Rectangle> RenderCell { get; set; }

        //TODO
        //events such as: CellEntered, CellExited, CellLeftCLicked, CellRightClicked, CellDragStart etc

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
                        PreviousSelection = CurrentSelection;
                        Point Position = e.AdjustedPosition(this).ToPoint();
                        UpdateSelection(Position, false);
                    }
                };
                SelectionMouseHandler.LMBReleasedInside += (sender, e) =>
                {
                    if (!ParentWindow.HasModalWindow)
                    {
                        Point Position = e.AdjustedPosition(this).ToPoint();
                        UpdateSelection(Position, true);
                    }
                };

                OnEndDraw += (sender, e) =>
                {
                    //  Draw the selection overlay
                    if (HasSelection && SelectionOverlay != null)
                    {
                        Rectangle? ScissorBounds = e.DA.DT.GD.RasterizerState.ScissorTestEnable ? e.DA.DT.GD.ScissorRectangle : null;
                        foreach (GridCellIndex Cell in CurrentSelection.Value)
                        {
                            if (_CellBounds.TryGetValue(Cell, out Rectangle Bounds))
                            {
                                if (ScissorBounds.HasValue && Bounds.GetTranslated(e.DA.Offset).Intersects(ScissorBounds.Value))
                                    SelectionOverlay.Draw(e.DA, this, Bounds);
                            }
                        }
                    }
                };

                SelectionMode = GridSelectionMode.None;
                CurrentSelection = null;

                OnLayoutBoundsChanged += (sender, e) =>
                {
                    //  Compute the bounds of each cell
                    _CellBounds.Clear();

                    int CurrentX = e.NewValue.Left + Padding.Left;
                    if (GridLinesVisibility.HasFlag(GridLinesVisibility.LeftEdge))
                        CurrentX += Math.Max(0, ColumnSpacing - GridLineMargin);

                    for (int ColumnIndex = 0; ColumnIndex < this.Columns; ColumnIndex++)
                    {
                        int ColumnWidth = ColumnIndex == 0 && HeaderColumnWidth.HasValue ? HeaderColumnWidth.Value : CellSize.Width;

                        int CurrentY = e.NewValue.Top + Padding.Top;
                        if (GridLinesVisibility.HasFlag(GridLinesVisibility.TopEdge))
                            CurrentY += Math.Max(0, RowSpacing - GridLineMargin);

                        for (int RowIndex = 0; RowIndex < this.Rows; RowIndex++)
                        {
                            int RowHeight = RowIndex == 0 && HeaderRowHeight.HasValue ? HeaderRowHeight.Value : CellSize.Height;

                            GridCellIndex Cell = new(RowIndex, ColumnIndex);
                            Rectangle CellBounds = new(CurrentX, CurrentY, ColumnWidth, RowHeight);
                            _CellBounds.Add(Cell, CellBounds);

                            CurrentY += RowHeight + RowSpacing;
                        }

                        CurrentX += ColumnWidth + ColumnSpacing;
                    }
                };

                this.CellBackground = new(null);
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

            if (RenderCell != null)
            {
                for (int C = 0; C < Columns; C++)
                {
                    for (int R = 0; R < Rows; R++)
                    {
                        GridCellIndex Index = new(R, C);
                        Rectangle Bounds = _CellBounds[Index];
                        RenderCell(DA, Index, Bounds);
                    }
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
