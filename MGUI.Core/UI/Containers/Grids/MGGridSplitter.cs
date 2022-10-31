﻿using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Shared.Helpers;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MGUI.Shared.Input.Mouse;
using System.Diagnostics;

namespace MGUI.Core.UI.Containers.Grids
{
    public class MGGridSplitter : MGElement
    {
        #region Focus Visual Style
        private Color _HoveredHighlightColor;
        /// <summary>An overlay color that is drawn overtop of this <see cref="MGGridSplitter"/>'s graphics if the mouse is currently hovering it.<br/>
        /// Recommended to use a transparent color.<para/>
        /// Default Value: <see cref="MGTheme.HoveredColor"/></summary>
        public Color HoveredHighlightColor
        {
            get => _HoveredHighlightColor;
            set
            {
                if (_HoveredHighlightColor != value)
                {
                    _HoveredHighlightColor = value;
                    NPC(nameof(HoveredHighlightColor));
                    HoveredOverlay = new(HoveredHighlightColor);
                    PressedOverlay = new(HoveredHighlightColor.Darken(PressedDarkenIntensity));
                }
            }
        }

        private float _PressedDarkenIntensity;
        /// <summary>A percentage to darken this <see cref="MGGridSplitter"/>'s graphics by when the mouse is currently pressed, but not yet released, overtop of this <see cref="MGElement"/>.<br/>
        /// Use a larger value to apply a more obvious background overlay while this <see cref="MGElement"/> is pressed. Use a smaller value for a more subtle change.<para/>
        /// Default value: <see cref="MGTheme.PressedDarkenModifier"/></summary>
        public float PressedDarkenIntensity
        {
            get => _PressedDarkenIntensity;
            set
            {
                if (_PressedDarkenIntensity != value)
                {
                    _PressedDarkenIntensity = value;
                    NPC(nameof(PressedDarkenIntensity));
                    PressedOverlay = new(HoveredHighlightColor.Darken(PressedDarkenIntensity));
                }
            }
        }

        private MGSolidFillBrush HoveredOverlay { get; set; }
        private MGSolidFillBrush PressedOverlay { get; set; }
        #endregion FocusVisualStyle

        #region Border
        /// <summary>Provides direct access to this element's border.</summary>
        public MGComponent<MGBorder> BorderComponent { get; }
        private MGBorder BorderElement { get; }

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

        private int _Size;
        /// <summary>For a Vertical <see cref="MGGridSplitter"/>, this represents the Width.<br/>
        /// For a Horizontal <see cref="MGGridSplitter"/>, this represents the Height.<para/>
        /// The orientation is automatically determined based on the <see cref="MGElement.LayoutBounds"/>.<br/>
        /// If the bounds are wider than they are tall, it is oriented horizontally and vice versa.<para/>
        /// Default value: 12.<br/>
        /// Recommended value: 6-20<para/>
        /// Note: This size does not include the size of the <see cref="BorderThickness"/>, if any.</summary>
        public int Size
        {
            get => _Size;
            set
            {
                if (_Size != value)
                {
                    _Size = value;
                    NPC(nameof(Size));
                    LayoutChanged(this, true);
                }
            }
        }

        /// <summary>The size to use when drawing the small grip thumb graphics in the center of this element's bounds.<para/>
        /// <see cref="TickSize"/>.Width represents the size of the larger dimension (The height of a vertical <see cref="MGGridSplitter"/>, the width of a horizontal <see cref="MGGridSplitter"/>)<br/>
        /// <see cref="TickSize"/>.Height represents the size of the smaller dimension.<para/>
        /// See also: <see cref="Orientation"/></summary>
        public Size TickSize { get; set; }

        /// <summary>The brush to use when drawing the thumb tick marks in the center of this element's bounds.</summary>
        public IFillBrush Foreground { get; set; }

        public Orientation Orientation => LayoutBounds.Width > LayoutBounds.Height ? Orientation.Horizontal : Orientation.Vertical;

        public bool IsDragging { get; private set; }

        /// <summary>Contains information about the target <see cref="MGGrid"/> that the resizing operations will be applied to during <see cref="MouseHandler.Dragged"/> events.</summary>
        private GridDragData? GridData { get; set; }

        private readonly record struct GridDragData
        {
            public readonly MGGridSplitter GridSplitter;
            /// <summary>The <see cref="GridCell"/> that the <see cref="GridSplitter"/> resides in.</summary>
            public readonly GridCell GridSplitterCell;
            public readonly MGGrid OwnerGrid;

            public readonly IReadOnlyList<ColumnDefinition> ActualColumns;
            /// <summary>Contains copies of the <see cref="OwnerGrid"/>'s <see cref="MGGrid.Columns"/>, representing the state of the columns when this object was created.</summary>
            public readonly IReadOnlyList<ColumnDefinition> OriginalColumns;

            /// <summary>Key = Actual column. Value = Original column</summary>
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private readonly Dictionary<ColumnDefinition, ColumnDefinition> _ActualToOriginalColumn;
            /// <summary>Key = Actual column. Value = Original column</summary>
            public IReadOnlyDictionary<ColumnDefinition, ColumnDefinition> ActualToOriginalColumn => _ActualToOriginalColumn;

            /// <summary>Key = Original column. Value = Actual column</summary>
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private readonly Dictionary<ColumnDefinition, ColumnDefinition> _OriginalToActualColumn;
            /// <summary>Key = Original column. Value = Actual column</summary>
            public IReadOnlyDictionary<ColumnDefinition, ColumnDefinition> OriginalToActualColumn => _OriginalToActualColumn;

            public readonly IReadOnlyList<RowDefinition> ActualRows;
            /// <summary>Contains copies of the <see cref="OwnerGrid"/>'s <see cref="MGGrid.Rows"/>, representing the state of the rows when this object was created.</summary>
            public readonly IReadOnlyList<RowDefinition> OriginalRows;

            /// <summary>Key = Actual column. Value = Original row</summary>
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private readonly Dictionary<RowDefinition, RowDefinition> _ActualToOriginalRow;
            /// <summary>Key = Actual column. Value = Original row</summary>
            public IReadOnlyDictionary<RowDefinition, RowDefinition> ActualToOriginalRow => _ActualToOriginalRow;

            /// <summary>Key = Original column. Value = Actual row</summary>
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private readonly Dictionary<RowDefinition, RowDefinition> _OriginalToActualRow;
            /// <summary>Key = Original column. Value = Actual row</summary>
            public IReadOnlyDictionary<RowDefinition, RowDefinition> OriginalToActualRow => _OriginalToActualRow;

            public bool IsValid => GridSplitter != null && GridSplitterCell.Column != null && GridSplitterCell.Row != null && OwnerGrid != null && 
                (ActualColumns?.Count ?? 0) > 0 && (ActualRows?.Count ?? 0) > 0;

            internal GridDragData(GridDragData InheritFrom)
            {
                this.GridSplitter = InheritFrom.GridSplitter;
                this.GridSplitterCell = InheritFrom.GridSplitterCell;
                this.OwnerGrid = InheritFrom.OwnerGrid;

                this.ActualColumns = InheritFrom.ActualColumns;
                this.OriginalColumns = InheritFrom.OriginalColumns.Select(x => x.GetCopy()).ToList();

                this._ActualToOriginalColumn = new();
                for (int i = 0; i < ActualColumns.Count; i++)
                    _ActualToOriginalColumn.Add(ActualColumns[i], OriginalColumns[i]);

                this._OriginalToActualColumn = new();
                for (int i = 0; i < OriginalColumns.Count; i++)
                    _OriginalToActualColumn.Add(OriginalColumns[i], ActualColumns[i]);

                this.ActualRows = InheritFrom.ActualRows;
                this.OriginalRows = InheritFrom.OriginalRows.Select(x => x.GetCopy()).ToList();

                this._ActualToOriginalRow = new();
                for (int i = 0; i < ActualRows.Count; i++)
                    _ActualToOriginalRow.Add(ActualRows[i], OriginalRows[i]);

                this._OriginalToActualRow = new();
                for (int i = 0; i < OriginalRows.Count; i++)
                    _OriginalToActualRow.Add(OriginalRows[i], ActualRows[i]);
            }

            public GridDragData(MGGridSplitter GridSplitter, MGGrid OwnerGrid, bool NormalizeWeightedLengths)
            {
                this.GridSplitter = GridSplitter;
                OwnerGrid.TryGetCell(GridSplitter, out GridCell Cell);
                this.GridSplitterCell = Cell;
                this.OwnerGrid = OwnerGrid;

                if (NormalizeWeightedLengths)
                    OwnerGrid.NormalizeWeightedLengths();

                this.ActualColumns = OwnerGrid.Columns;
                this.OriginalColumns = ActualColumns.Select(x => x.GetCopy()).ToList();

                this._ActualToOriginalColumn = new();
                for (int i = 0; i < ActualColumns.Count; i++)
                    _ActualToOriginalColumn.Add(ActualColumns[i], OriginalColumns[i]);

                this._OriginalToActualColumn = new();
                for (int i = 0; i < OriginalColumns.Count; i++)
                    _OriginalToActualColumn.Add(OriginalColumns[i], ActualColumns[i]);

                this.ActualRows = OwnerGrid.Rows;
                this.OriginalRows = ActualRows.Select(x => x.GetCopy()).ToList();

                this._ActualToOriginalRow = new();
                for (int i = 0; i < ActualRows.Count; i++)
                    _ActualToOriginalRow.Add(ActualRows[i], OriginalRows[i]);

                this._OriginalToActualRow = new();
                for (int i = 0; i < OriginalRows.Count; i++)
                    _OriginalToActualRow.Add(OriginalRows[i], ActualRows[i]);
            }
        }

        public MGGridSplitter(MGWindow ParentWindow)
            : base(ParentWindow, MGElementType.GridSplitter)
        {
            using (BeginInitializing())
            {
                this.BorderElement = new(ParentWindow, new(0), MGUniformBorderBrush.Black);
                this.BorderComponent = MGComponentBase.Create(BorderElement);
                AddComponent(BorderComponent);

                MGTheme Theme = GetTheme();

                this.HoveredHighlightColor = Theme.HoveredColor;
                this.PressedDarkenIntensity = Theme.PressedDarkenModifier;

                this.VerticalAlignment = VerticalAlignment.Stretch;
                this.HorizontalAlignment = HorizontalAlignment.Stretch;
                this.Size = 12;
                this.TickSize = new(20, 2);

                this.Foreground = GetTheme().GridSplitterForeground.GetValue(true);

                MouseHandler.DragStart += (sender, e) =>
                {
                    if (e.IsLMB && Parent is MGGrid OwnerGrid)
                    {
                        this.GridData = new(this, OwnerGrid, true);
                        IsDragging = true;
                        e.SetHandled(this, false);
                    }
                };

                MouseHandler.DragEnd += (sender, e) =>
                {
                    if (e.IsLMB)
                        IsDragging = false;
                };

                MouseHandler.Dragged += (sender, e) =>
                {
                    if (e.IsLMB)
                        ApplyResizing(e.PositionDelta);
                };
            }
        }

        /// <summary>Represents the order of precedence that resizing operations will be applied to rows/columns in.<para/>
        /// For example, if columns left of a <see cref="MGGridSplitter"/> are reduced in width, it will attempt to reduce weighted columns first.<br/>
        /// If there are no weighted columns or the weighted column couldn't fully be reduced by the desired amount (such as if it had a MinWidth), then next the resize logic will look for pixel-length columns.</summary>
        private static readonly ReadOnlyCollection<GridUnitType> OrderedGridUnitTypes = new List<GridUnitType>() {
            GridUnitType.Weighted, GridUnitType.Pixel, GridUnitType.Auto
        }.AsReadOnly();

        /// <param name="PositionDelta">The change in position from the start of the mouse drag to the current position</param>
        private void ApplyResizing(Point PositionDelta)
        {
            if (GridData?.IsValid != true)
                return;

            GridDragData Data = new(GridData.Value);

            //  Note: By the time we get to this point, the weighted lengths of the grid's rows/columns
            //  have already been normalized via MGGrid.NormalizeWeightedLengths()
            //  so we shouldn't have to worry about column MinWidth/MaxWidth or row MinHeight/MaxHeight messing up the new weights that we calculate

            int GridSplitterIndex;
            int Delta;
            switch (this.Orientation)
            {
                case Orientation.Horizontal:
                    GridSplitterIndex = Data.GridSplitterCell.Row.Index;
                    Delta = PositionDelta.Y; // (Position is in client-space, so positive Y-axis is down)
                    if (Delta < 0) // Dragged splitter up
                    {
                        //  Decrease height of previous row(s) and increase height of next row(s)
                        //TODO

                    }
                    else if (Delta > 0) // Dragged splitter down
                    {
                        //  Decrease height of next row(s) and increase height of previous row(s)
                        //TODO

                    }
                    break;
                case Orientation.Vertical:
                    GridSplitterIndex = Data.GridSplitterCell.Column.Index;
                    Delta = PositionDelta.X;
                    if (Delta != 0)
                    {
                        List<ColumnDefinition> DecreasedColumns;
                        List<ColumnDefinition> IncreasedColumns;

                        //  Prioritize removing/adding width to/from columns based on the column's length type and how close the column is to the gridsplitter's column
                        List<ColumnDefinition> OrderedDecreasedColumns;
                        List<ColumnDefinition> OrderedIncreasedColumns;

                        if (Delta < 0) // Dragged splitter left: Decrease width of column(s) left of the GridSplitter, increase width of column(s) right of the GridSplitter
                        {
                            DecreasedColumns = Data.OriginalColumns.Take(GridSplitterIndex).ToList();
                            OrderedDecreasedColumns = DecreasedColumns
                                .OrderBy(x => OrderedGridUnitTypes.IndexOf(x.Length.UnitType))
                                .ThenByDescending(x => DecreasedColumns.IndexOf(x))
                                .ToList();

                            IncreasedColumns = Data.OriginalColumns.Skip(GridSplitterIndex + 1).ToList();
                            OrderedIncreasedColumns = IncreasedColumns
                                .OrderBy(x => OrderedGridUnitTypes.IndexOf(x.Length.UnitType))
                                .ThenBy(x => IncreasedColumns.IndexOf(x))
                                .ToList();
                        }
                        else // Dragged splitter right: Decrease width of column(s) right of the GridSplitter, increase width of column(s) left of the GridSplitter
                        {
                            DecreasedColumns = Data.OriginalColumns.Skip(GridSplitterIndex + 1).ToList();
                            OrderedDecreasedColumns = DecreasedColumns
                                .OrderBy(x => OrderedGridUnitTypes.IndexOf(x.Length.UnitType))
                                .ThenBy(x => DecreasedColumns.IndexOf(x))
                                .ToList();

                            IncreasedColumns = Data.OriginalColumns.Take(GridSplitterIndex).ToList();
                            OrderedIncreasedColumns = IncreasedColumns
                                .OrderBy(x => OrderedGridUnitTypes.IndexOf(x.Length.UnitType))
                                .ThenByDescending(x => IncreasedColumns.IndexOf(x))
                                .ToList();
                        }

                        int MaxReduction = DecreasedColumns.Sum(x => Math.Max(0, x.Width - (x.MinWidth ?? 0))); // Maximum width that can be removed from the previous columns
                        int MaxIncrease = IncreasedColumns.Any(x => !x.MaxWidth.HasValue) ? int.MaxValue : IncreasedColumns.Sum(x => Math.Max(0, x.MaxWidth.Value - x.Width)); // Maximum width that can be added to the next columns
                        int ResizeAmount = GeneralUtils.Min(Math.Abs(Delta), MaxReduction, MaxIncrease);

                        int TotalRemainingRemovedWidth = ResizeAmount; // The amount we still have to reduce previous columns by
                        for (int i = 0; i < OrderedDecreasedColumns.Count; i++)
                        {
                            ColumnDefinition Source = OrderedDecreasedColumns[i];
                            int CurrentRemovedWidth = GeneralUtils.Min(Source.Width - (Source.MinWidth ?? 0), TotalRemainingRemovedWidth);
                            if (CurrentRemovedWidth > 0)
                            {
                                //  Reduce the width of the source column
                                Data.OriginalToActualColumn[Source].Length = Source.Length.UnitType switch
                                {
                                    GridUnitType.Auto or GridUnitType.Pixel => GridLength.CreatePixelLength(Source.Width - CurrentRemovedWidth),
                                    GridUnitType.Weighted => GridLength.CreateWeightedLength(Source.Width - CurrentRemovedWidth),
                                    _ => throw new NotImplementedException($"Unrecognized {nameof(GridUnitType)}: {Source.Length.UnitType}"),
                                };

                                int TotalRemainingAddedWidth = CurrentRemovedWidth;
                                for (int j = 0; j < OrderedIncreasedColumns.Count; j++)
                                {
                                    ColumnDefinition Target = OrderedIncreasedColumns[j];
                                    int CurrentAddedWidth = GeneralUtils.Min((Target.MaxWidth ?? int.MaxValue) - Target.Width, TotalRemainingAddedWidth);
                                    if (CurrentAddedWidth > 0)
                                    {
                                        Data.OriginalToActualColumn[Target].Length = Target.Length.UnitType switch
                                        {
                                            GridUnitType.Auto or GridUnitType.Pixel => GridLength.CreatePixelLength(Target.Width + CurrentAddedWidth),
                                            GridUnitType.Weighted => GridLength.CreateWeightedLength(Target.Width + CurrentAddedWidth),
                                            _ => throw new NotImplementedException($"Unrecognized {nameof(GridUnitType)}: {Source.Length.UnitType}"),
                                        };

                                        Target.Width += CurrentAddedWidth;

                                        TotalRemainingAddedWidth -= CurrentAddedWidth;
                                        if (TotalRemainingAddedWidth <= 0)
                                            break;
                                    }
                                }

                                Source.Width -= CurrentRemovedWidth;

                                TotalRemainingRemovedWidth -= CurrentRemovedWidth;
                                if (TotalRemainingRemovedWidth <= 0)
                                    break;
                            }
                        }
                    }
                    break;
                default: throw new NotImplementedException($"Unrecognized {nameof(Orientation)}: {this.Orientation}");
            }
        }

        public override Thickness MeasureSelfOverride(Size AvailableSize, out Thickness SharedSize)
        {
            SharedSize = new(0);
            return new Thickness(Size, Size, 0, 0);
        }

        public override void DrawSelf(ElementDrawArgs DA, Rectangle LayoutBounds)
        {
            bool IsHorizontal = Orientation == Orientation.Horizontal;
            bool IsVertical = Orientation == Orientation.Vertical;

            List<Rectangle> TickBounds = new();
            if (Foreground != null)
            {
                int TickWidth = IsHorizontal ? Math.Min(LayoutBounds.Width - 4, TickSize.Width) : TickSize.Height;
                int TickHeight = IsVertical ? TickSize.Width : Math.Min(LayoutBounds.Height - 4, TickSize.Height);
                int TickSpacing = Math.Min(Size - (TickSize.Height + 1) * 2, 3);

                if (IsHorizontal)
                {
                    Rectangle FullTickBounds = ApplyAlignment(LayoutBounds, HorizontalAlignment.Center, VerticalAlignment.Center, new(TickWidth, TickHeight * 2 + TickSpacing));
                    TickBounds.Add(new(FullTickBounds.Left, FullTickBounds.Top, FullTickBounds.Width, TickHeight));
                    TickBounds.Add(new(FullTickBounds.Left, FullTickBounds.Top + TickHeight + TickSpacing, FullTickBounds.Width, TickHeight));
                }
                else if (IsVertical)
                {
                    Rectangle FullTickBounds = ApplyAlignment(LayoutBounds, HorizontalAlignment.Center, VerticalAlignment.Center, new(TickWidth * 2 + TickSpacing, TickHeight));
                    TickBounds.Add(new(FullTickBounds.Left, FullTickBounds.Top, TickWidth, FullTickBounds.Height));
                    TickBounds.Add(new(FullTickBounds.Left + TickWidth + TickSpacing, FullTickBounds.Top, TickWidth, FullTickBounds.Height));
                }
            }

            foreach (Rectangle Tick in TickBounds)
                Foreground.Draw(DA, this, Tick);

            if (IsLMBPressed || IsDragging)
            {
                foreach (Rectangle Tick in TickBounds)
                    PressedOverlay.Draw(DA, this, Tick);
            }
            else if (IsHovered)
            {
                foreach (Rectangle Tick in TickBounds)
                    HoveredOverlay.Draw(DA, this, Tick);
            }

            base.DrawSelf(DA, LayoutBounds);
        }
    }
}