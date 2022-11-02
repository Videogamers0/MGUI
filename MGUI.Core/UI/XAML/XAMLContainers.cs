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
using System.Windows.Markup;
using MGUI.Core.UI.Brushes.Border_Brushes;

namespace MGUI.Core.UI.XAML
{
    [ContentProperty(nameof(Children))]
    public abstract class XAMLMultiContentHost : XAMLElement
    {
        public List<XAMLElement> Children { get; set; } = new();
    }

    [ContentProperty(nameof(Content))]
    public abstract class XAMLSingleContentHost : XAMLElement
    {
        public XAMLElement Content { get; set; }

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, Dictionary<string, Texture2D> NamedTextures)
        {
            if (Content != null)
            {
                MGSingleContentHost TypedElement = Element as MGSingleContentHost;
                TypedElement.SetContent(Content.ToElement<MGElement>(Element.SelfOrParentWindow, Element, NamedTextures));
            }
        }
    }

    public struct XAMLColumnDefinition
    {
        public GridLength Length { get; set; }
        public int? MinWidth { get; set; }
        public int? MaxWidth { get; set; }
        public override string ToString() => $"{nameof(XAMLColumnDefinition)}: {Length}";
    }

    public struct XAMLRowDefinition
    {
        public GridLength Length { get; set; }
        public int? MinHeight { get; set; }
        public int? MaxHeight { get; set; }
        public override string ToString() => $"{nameof(XAMLRowDefinition)}: {Length}";
    }

    public class XAMLGridSplitter : XAMLElement
    {
        public int? Size { get; set; }
        public XAMLSize? TickSize { get; set; }
        public XAMLFillBrush Foreground { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent, Dictionary<string, Texture2D> NamedTextures) => new MGGridSplitter(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, Dictionary<string, Texture2D> NamedTextures)
        {
            MGGridSplitter GridSplitter = Element as MGGridSplitter;

            if (Size.HasValue)
                GridSplitter.Size = Size.Value;
            if (TickSize.HasValue)
                GridSplitter.TickSize = TickSize.Value.ToSize();
            if (Foreground != null)
                GridSplitter.Foreground = new VisualStateFillBrush(Foreground.ToFillBrush());
        }
    }

    public class XAMLGrid : XAMLMultiContentHost
    {
        public string RowLengths { get; set; }
        public string ColumnLengths { get; set; }
        public List<XAMLRowDefinition> RowDefinitions { get; set; } = new();
        public List<XAMLColumnDefinition> ColumnDefinitions { get; set; } = new();

        public GridSelectionMode? SelectionMode { get; set; }
        public bool? CanDeselectByClickingSelectedCell { get; set; }
        public XAMLFillBrush SelectionBackground { get; set; }
        public XAMLFillBrush SelectionOverlay { get; set; }

        public GridLineIntersection? GridLineIntersectionHandling { get; set; }
        public GridLinesVisibility? GridLinesVisibility { get; set; }
        public int? GridLineMargin { get; set; }
        public XAMLFillBrush HorizontalGridLineBrush { get; set; }
        public XAMLFillBrush VerticalGridLineBrush { get; set; }

        public int? RowSpacing { get; set; }
        public int? ColumnSpacing { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent, Dictionary<string, Texture2D> NamedTextures) => new MGGrid(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, Dictionary<string, Texture2D> NamedTextures)
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

            foreach (XAMLRowDefinition RowDefinition in RowDefinitions)
            {
                RowDefinition RD = Grid.AddRow(RowDefinition.Length);
                RD.SetSizeConstraints(RowDefinition.MinHeight, RowDefinition.MaxHeight);
            }

            foreach (XAMLColumnDefinition ColumnDefinition in ColumnDefinitions)
            {
                ColumnDefinition CD = Grid.AddColumn(ColumnDefinition.Length);
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

            foreach (XAMLElement Child in Children)
            {
                MGElement ChildElement = Child.ToElement<MGElement>(Grid.SelfOrParentWindow, Grid, NamedTextures);
                Grid.TryAddChild(Child.GridRow, Child.GridColumn, new GridSpan(Child.GridRowSpan, Child.GridColumnSpan, Child.GridAffectsMeasure), ChildElement);
            }
        }
    }

    public class XAMLDockPanel : XAMLMultiContentHost
    {
        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent, Dictionary<string, Texture2D> NamedTextures) => new MGDockPanel(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, Dictionary<string, Texture2D> NamedTextures)
        {
            MGDockPanel DockPanel = Element as MGDockPanel;

            foreach (XAMLElement Child in Children)
            {
                MGElement ChildElement = Child.ToElement<MGElement>(DockPanel.ParentWindow, DockPanel, NamedTextures);
                DockPanel.TryAddChild(ChildElement, Child.Dock);
            }
        }
    }

    public class XAMLStackPanel : XAMLMultiContentHost
    {
        public Orientation? Orientation { get; set; }
        public int? Spacing { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent, Dictionary<string, Texture2D> NamedTextures) => new MGStackPanel(Window, Orientation ?? UI.Orientation.Vertical);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, Dictionary<string, Texture2D> NamedTextures)
        {
            MGStackPanel StackPanel = Element as MGStackPanel;

            if (Orientation.HasValue)
                StackPanel.Orientation = Orientation.Value;
            if (Spacing.HasValue)
                StackPanel.Spacing = Spacing.Value;

            foreach (XAMLElement Child in Children)
            {
                MGElement ChildElement = Child.ToElement<MGElement>(StackPanel.ParentWindow, StackPanel, NamedTextures);
                StackPanel.TryAddChild(ChildElement);
            }
        }
    }

    public class XAMLOverlayPanel : XAMLMultiContentHost
    {
        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent, Dictionary<string, Texture2D> NamedTextures) => new MGOverlayPanel(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, Dictionary<string, Texture2D> NamedTextures)
        {
            MGOverlayPanel OverlayPanel = Element as MGOverlayPanel;

            foreach (XAMLElement Child in Children)
            {
                MGElement ChildElement = Child.ToElement<MGElement>(OverlayPanel.ParentWindow, OverlayPanel, NamedTextures);
                OverlayPanel.TryAddChild(ChildElement, Child.Offset.ToThickness());
            }
        }
    }
}
