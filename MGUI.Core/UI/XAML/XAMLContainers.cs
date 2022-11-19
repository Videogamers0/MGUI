using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MGUI.Core.UI.Containers;
using MGUI.Core.UI.Containers.Grids;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MGUI.Core.UI.Brushes.Border_Brushes;
using MonoGame.Extended;

#if UseWPF
using System.Windows.Markup;
#endif

namespace MGUI.Core.UI.XAML
{
#if UseWPF
    [ContentProperty(nameof(Children))]
#endif
    public abstract class MultiContentHost : Element
    {
        public List<Element> Children { get; set; } = new();

        protected internal override IEnumerable<Element> GetChildren() => Children;
    }

#if UseWPF
    [ContentProperty(nameof(Content))]
#endif
    public abstract class SingleContentHost : Element
    {
        public Element Content { get; set; }

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            if (Content != null)
            {
                MGSingleContentHost TypedElement = Element as MGSingleContentHost;
                TypedElement.SetContent(Content.ToElement<MGElement>(Element.SelfOrParentWindow, Element));
            }
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            if (Content != null)
                yield return Content;
        }
    }

    public struct ColumnDefinition
    {
        public GridLength Length { get; set; }
        public int? MinWidth { get; set; }
        public int? MaxWidth { get; set; }
        public override string ToString() => $"{nameof(ColumnDefinition)}: {Length}";
    }

    public struct RowDefinition
    {
        public GridLength Length { get; set; }
        public int? MinHeight { get; set; }
        public int? MaxHeight { get; set; }
        public override string ToString() => $"{nameof(RowDefinition)}: {Length}";
    }

    public class GridSplitter : Element
    {
        public override MGElementType ElementType => MGElementType.GridSplitter;

        public int? Size { get; set; }
        public Size? TickSize { get; set; }
        public FillBrush Foreground { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGGridSplitter(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGGridSplitter GridSplitter = Element as MGGridSplitter;

            if (Size.HasValue)
                GridSplitter.Size = Size.Value;
            if (TickSize.HasValue)
                GridSplitter.TickSize = TickSize.Value.ToSize();
            if (Foreground != null)
                GridSplitter.Foreground.NormalValue = Foreground.ToFillBrush();
        }

        protected internal override IEnumerable<Element> GetChildren() => Enumerable.Empty<Element>();
    }

    public class Grid : MultiContentHost
    {
        public override MGElementType ElementType => MGElementType.Grid;

        public string RowLengths { get; set; }
        public string ColumnLengths { get; set; }
        public List<RowDefinition> RowDefinitions { get; set; } = new();
        public List<ColumnDefinition> ColumnDefinitions { get; set; } = new();

        public GridSelectionMode? SelectionMode { get; set; }
        public bool? CanDeselectByClickingSelectedCell { get; set; }
        public FillBrush SelectionBackground { get; set; }
        public FillBrush SelectionOverlay { get; set; }

        public GridLineIntersection? GridLineIntersectionHandling { get; set; }
        public GridLinesVisibility? GridLinesVisibility { get; set; }
        public int? GridLineMargin { get; set; }
        public FillBrush HorizontalGridLineBrush { get; set; }
        public FillBrush VerticalGridLineBrush { get; set; }

        public int? RowSpacing { get; set; }
        public int? ColumnSpacing { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGGrid(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGGrid Grid = Element as MGGrid;

            if (RowLengths != null)
            {
                Grid.AddRows(ConstrainedGridLength.ParseMultiple(RowLengths));
            }

            if (ColumnLengths != null)
            {
                Grid.AddColumns(ConstrainedGridLength.ParseMultiple(ColumnLengths));
            }

            foreach (RowDefinition RowDefinition in RowDefinitions)
            {
                Containers.Grids.RowDefinition RD = Grid.AddRow(RowDefinition.Length);
                RD.SetSizeConstraints(RowDefinition.MinHeight, RowDefinition.MaxHeight);
            }

            foreach (ColumnDefinition ColumnDefinition in ColumnDefinitions)
            {
                Containers.Grids.ColumnDefinition CD = Grid.AddColumn(ColumnDefinition.Length);
                CD.SetSizeConstraints(ColumnDefinition.MinWidth, ColumnDefinition.MaxWidth);
            }

            if (SelectionMode.HasValue)
                Grid.SelectionMode = SelectionMode.Value;
            if (CanDeselectByClickingSelectedCell.HasValue)
                Grid.CanDeselectByClickingSelectedCell = CanDeselectByClickingSelectedCell.Value;
            if (SelectionBackground != null)
                Grid.SelectionBackground = SelectionBackground.ToFillBrush();
            if (SelectionOverlay != null)
                Grid.SelectionOverlay = SelectionOverlay.ToFillBrush();

            if (GridLineIntersectionHandling.HasValue)
                Grid.GridLineIntersectionHandling = GridLineIntersectionHandling.Value;
            if (GridLinesVisibility.HasValue)
                Grid.GridLinesVisibility = GridLinesVisibility.Value;
            if (GridLineMargin.HasValue)
                Grid.GridLineMargin = GridLineMargin.Value;
            if (HorizontalGridLineBrush != null)
                Grid.HorizontalGridLineBrush = HorizontalGridLineBrush.ToFillBrush();
            if (VerticalGridLineBrush != null)
                Grid.VerticalGridLineBrush = VerticalGridLineBrush.ToFillBrush();

            if (RowSpacing.HasValue)
                Grid.RowSpacing = RowSpacing.Value;
            if (ColumnSpacing.HasValue)
                Grid.ColumnSpacing = ColumnSpacing.Value;

            foreach (Element Child in Children)
            {
                MGElement ChildElement = Child.ToElement<MGElement>(Grid.SelfOrParentWindow, Grid);
                Grid.TryAddChild(Child.GridRow, Child.GridColumn, new GridSpan(Child.GridRowSpan, Child.GridColumnSpan, Child.GridAffectsMeasure), ChildElement);
            }
        }
    }

    public class UniformGrid : MultiContentHost
    {
        public override MGElementType ElementType => MGElementType.UniformGrid;

        public int? Rows { get; set; }
        public int? Columns { get; set; }
        public Size? CellSize { get; set; }
        public int? HeaderRowHeight { get; set; }
        public int? HeaderColumnWidth { get; set; }

        public GridSelectionMode? SelectionMode { get; set; }
        public bool? CanDeselectByClickingSelectedCell { get; set; }
        public FillBrush SelectionBackground { get; set; }
        public FillBrush SelectionOverlay { get; set; }

        public GridLineIntersection? GridLineIntersectionHandling { get; set; }
        public GridLinesVisibility? GridLinesVisibility { get; set; }
        public int? GridLineMargin { get; set; }
        public FillBrush HorizontalGridLineBrush { get; set; }
        public FillBrush VerticalGridLineBrush { get; set; }

        public int? RowSpacing { get; set; }
        public int? ColumnSpacing { get; set; }

        public FillBrush CellBackground { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGUniformGrid(Window, Rows ?? 0, Columns ?? 0, CellSize?.ToSize() ?? MonoGame.Extended.Size.Empty);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGUniformGrid Grid = Element as MGUniformGrid;

            if (Rows.HasValue)
                Grid.Rows = Rows.Value;
            if (Columns.HasValue)
                Grid.Columns = Columns.Value;
            if (CellSize.HasValue)
                Grid.CellSize = CellSize.Value.ToSize();
            if (HeaderRowHeight.HasValue)
                Grid.HeaderRowHeight = HeaderRowHeight.Value;
            if (HeaderColumnWidth.HasValue)
                Grid.HeaderColumnWidth = HeaderColumnWidth.Value;

            if (SelectionMode.HasValue)
                Grid.SelectionMode = SelectionMode.Value;
            if (CanDeselectByClickingSelectedCell.HasValue)
                Grid.CanDeselectByClickingSelectedCell = CanDeselectByClickingSelectedCell.Value;
            if (SelectionBackground != null)
                Grid.SelectionBackground = SelectionBackground.ToFillBrush();
            if (SelectionOverlay != null)
                Grid.SelectionOverlay = SelectionOverlay.ToFillBrush();

            if (GridLineIntersectionHandling.HasValue)
                Grid.GridLineIntersectionHandling = GridLineIntersectionHandling.Value;
            if (GridLinesVisibility.HasValue)
                Grid.GridLinesVisibility = GridLinesVisibility.Value;
            if (GridLineMargin.HasValue)
                Grid.GridLineMargin = GridLineMargin.Value;
            if (HorizontalGridLineBrush != null)
                Grid.HorizontalGridLineBrush = HorizontalGridLineBrush.ToFillBrush();
            if (VerticalGridLineBrush != null)
                Grid.VerticalGridLineBrush = VerticalGridLineBrush.ToFillBrush();

            if (RowSpacing.HasValue)
                Grid.RowSpacing = RowSpacing.Value;
            if (ColumnSpacing.HasValue)
                Grid.ColumnSpacing = ColumnSpacing.Value;

            if (CellBackground != null)
                Grid.CellBackground.NormalValue = CellBackground.ToFillBrush();

            foreach (Element Child in Children)
            {
                MGElement ChildElement = Child.ToElement<MGElement>(Grid.SelfOrParentWindow, Grid);
                Grid.TryAddChild(Child.GridRow, Child.GridColumn, ChildElement);
            }
        }
    }

    public class DockPanel : MultiContentHost
    {
        public override MGElementType ElementType => MGElementType.DockPanel;

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGDockPanel(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGDockPanel DockPanel = Element as MGDockPanel;

            foreach (Element Child in Children)
            {
                MGElement ChildElement = Child.ToElement<MGElement>(DockPanel.ParentWindow, DockPanel);
                DockPanel.TryAddChild(ChildElement, Child.Dock);
            }
        }
    }

    public class StackPanel : MultiContentHost
    {
        public override MGElementType ElementType => MGElementType.StackPanel;

        public Orientation? Orientation { get; set; }
        public int? Spacing { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGStackPanel(Window, Orientation ?? UI.Orientation.Vertical);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGStackPanel StackPanel = Element as MGStackPanel;

            if (Orientation.HasValue)
                StackPanel.Orientation = Orientation.Value;
            if (Spacing.HasValue)
                StackPanel.Spacing = Spacing.Value;

            foreach (Element Child in Children)
            {
                MGElement ChildElement = Child.ToElement<MGElement>(StackPanel.ParentWindow, StackPanel);
                StackPanel.TryAddChild(ChildElement);
            }
        }
    }

    public class OverlayPanel : MultiContentHost
    {
        public override MGElementType ElementType => MGElementType.OverlayPanel;

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGOverlayPanel(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGOverlayPanel OverlayPanel = Element as MGOverlayPanel;

            foreach (Element Child in Children)
            {
                MGElement ChildElement = Child.ToElement<MGElement>(OverlayPanel.ParentWindow, OverlayPanel);
                OverlayPanel.TryAddChild(ChildElement, Child.Offset.ToThickness());
            }
        }
    }
}
