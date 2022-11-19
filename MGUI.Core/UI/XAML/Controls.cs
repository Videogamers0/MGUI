using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Containers;
using MGUI.Core.UI.Containers.Grids;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

#if UseWPF
using System.Windows.Markup;
#endif

namespace MGUI.Core.UI.XAML
{
    public class ContentPresenter : SingleContentHost
    {
        public override MGElementType ElementType => MGElementType.ContentPresenter;
        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGContentPresenter(Window);
        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element) => base.ApplyDerivedSettings(Parent, Element);
    }

    public class HeaderedContentPresenter : SingleContentHost
    {
        public override MGElementType ElementType => MGElementType.HeaderedContentPresenter;

        public Element Header { get; set; }

        public Dock? HeaderPosition { get; set; }
        public int? Spacing { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGHeaderedContentPresenter(Window, null, null);
        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGHeaderedContentPresenter ContentPresenter = Element as MGHeaderedContentPresenter;

            if (Header != null)
                ContentPresenter.Header = Header.ToElement<MGElement>(ContentPresenter.ParentWindow, ContentPresenter);
            if (HeaderPosition.HasValue)
                ContentPresenter.HeaderPosition = HeaderPosition.Value;
            if (Spacing.HasValue)
                ContentPresenter.Spacing = Spacing.Value;

            base.ApplyDerivedSettings(Parent, Element);
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            foreach (Element Element in base.GetChildren())
                yield return Element;

            if (Header != null)
                yield return Header;
        }
    }

    public class Border : SingleContentHost
    {
        public override MGElementType ElementType => MGElementType.Border;

        public BorderBrush BorderBrush { get; set; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BorderBrush BB { get => BorderBrush; set => BorderBrush = value; }

        public Thickness? BorderThickness { get; set; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Thickness? BT { get => BorderThickness; set => BorderThickness = value; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGBorder(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGBorder Border = Element as MGBorder;

            if (BorderBrush != null)
                Border.BorderBrush = BorderBrush.ToBorderBrush();
            if (BorderThickness.HasValue)
                Border.BorderThickness = BorderThickness.Value.ToThickness();

            base.ApplyDerivedSettings(Parent, Element);
        }
    }

    public class Button : SingleContentHost
    {
        public override MGElementType ElementType => MGElementType.Button;

        public Border Border { get; set; } = new();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BorderBrush BorderBrush { get => Border.BorderBrush; set => Border.BorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BorderBrush BB { get => BorderBrush; set => BorderBrush = value; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Thickness? BorderThickness { get => Border.BorderThickness; set => Border.BorderThickness = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Thickness? BT { get => BorderThickness; set => BorderThickness = value; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGButton(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGButton Button = Element as MGButton;
            Border.ApplySettings(Button, Button.BorderComponent.Element);

            base.ApplyDerivedSettings(Parent, Element);
        }
    }

    public class CheckBox : SingleContentHost
    {
        public override MGElementType ElementType => MGElementType.CheckBox;

        public Button Button { get; set; } = new();

        public int? CheckBoxComponentSize { get; set; }
        public int? SpacingWidth { get; set; }
        public XAMLColor? CheckMarkColor { get; set; }
        public bool? IsThreeState { get; set; }
        public bool? IsChecked { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGCheckBox(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGCheckBox CheckBox = Element as MGCheckBox;
            Button.ApplySettings(CheckBox, CheckBox.ButtonComponent.Element);

            if (CheckBoxComponentSize.HasValue)
                CheckBox.CheckBoxComponentSize = CheckBoxComponentSize.Value;
            if (SpacingWidth.HasValue)
                CheckBox.SpacingWidth = SpacingWidth.Value;
            if (CheckMarkColor.HasValue)
                CheckBox.CheckMarkColor = CheckMarkColor.Value.ToXNAColor();
            if (IsThreeState.HasValue)
                CheckBox.IsThreeState = IsThreeState.Value;
            if (IsChecked.HasValue)
                CheckBox.IsChecked = IsChecked.Value;

            base.ApplyDerivedSettings(Parent, Element);
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            foreach (Element Element in base.GetChildren())
                yield return Element;
            yield return Button;
        }
    }

#if UseWPF
    [ContentProperty(nameof(Items))]
#endif
    public class ComboBox : MultiContentHost
    {
        public override MGElementType ElementType => MGElementType.ComboBox;

        /// <summary>The generic type that will be used when instantiating <see cref="MGComboBox{TItemType}"/>.<para/>
        /// To set this value from a XAML string, you must define the namespace the type belongs to, then use the x:Type Markup Extension<br/>
        /// (See: <see href="https://learn.microsoft.com/en-us/dotnet/desktop/xaml-services/xtype-markup-extension"/>)<para/>
        /// Example:
        /// <code>&lt;ComboBox xmlns:System="clr-namespace:System;assembly=mscorlib" ItemType="{x:Type System:Double}" /&gt;</code><para/>
        /// Default value: <code>typeof(object)</code></summary>
        public Type ItemType { get; set; } = typeof(object);

        public Border Border { get; set; } = new();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BorderBrush BorderBrush { get => Border.BorderBrush; set => Border.BorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BorderBrush BB { get => BorderBrush; set => BorderBrush = value; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Thickness? BorderThickness { get => Border.BorderThickness; set => Border.BorderThickness = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Thickness? BT { get => BorderThickness; set => BorderThickness = value; }

        public ContentPresenter DropdownArrow { get; set; } = new();
        public XAMLColor? DropdownArrowColor { get; set; }

        public List<object> Items { get; set; } = new();

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent)
        {
            Type GenericType = typeof(MGComboBox<>).MakeGenericType(new Type[] { ItemType });
            object Element = Activator.CreateInstance(GenericType, new object[] { Window });
            return Element as MGElement;
        }

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            Type GenericType = typeof(MGComboBox<>).MakeGenericType(new Type[] { ItemType });
            MethodInfo Method = GenericType.GetMethod(nameof(MGComboBox<object>.LoadSettings), BindingFlags.Instance | BindingFlags.NonPublic);
            Method.Invoke(Element, new object[] { this });
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            foreach (Element Element in base.GetChildren())
                yield return Element;

            yield return Border;
            yield return DropdownArrow;
        }
    }

    public class Expander : SingleContentHost
    {
        public override MGElementType ElementType => MGElementType.Expander;

        public ToggleButton ExpanderToggleButton { get; set; } = new();

        public int? ExpanderButtonSize { get; set; }
        public BorderBrush ExpanderButtonBorderBrush { get; set; }
        public Thickness? ExpanderButtonBorderThickness { get; set; }
        public FillBrush ExpanderButtonBackgroundBrush { get; set; }
        public XAMLColor? ExpanderDropdownArrowColor { get; set; }
        public int? ExpanderDropdownArrowSize { get; set; }

        public int? HeaderSpacingWidth { get; set; }
        public Element Header { get; set; }

        public bool? IsExpanded { get; set; }
        public Visibility? ExpandedVisibility { get; set; }
        public Visibility? CollapsedVisibility { get; set; }

        public StackPanel HeadersPanel { get; set; } = new();

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGExpander(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGExpander Expander = Element as MGExpander;
            ExpanderToggleButton.ApplySettings(Expander, Expander.ExpanderToggleButton);
            HeadersPanel.ApplySettings(Expander, Expander.HeadersPanelComponent.Element);

            if (ExpanderButtonSize.HasValue)
                Expander.ExpanderButtonSize = ExpanderButtonSize.Value;
            if (ExpanderButtonBorderBrush != null)
                Expander.ExpanderButtonBorderBrush = ExpanderButtonBorderBrush.ToBorderBrush();
            if (ExpanderButtonBorderThickness.HasValue)
                Expander.ExpanderButtonBorderThickness = ExpanderButtonBorderThickness.Value.ToThickness();
            if (ExpanderButtonBackgroundBrush != null)
                Expander.ExpanderButtonBackgroundBrush.NormalValue = ExpanderButtonBackgroundBrush.ToFillBrush();
            if (ExpanderDropdownArrowColor.HasValue)
                Expander.ExpanderDropdownArrowColor = ExpanderDropdownArrowColor.Value.ToXNAColor();
            if (ExpanderDropdownArrowSize.HasValue)
                Expander.ExpanderDropdownArrowSize = ExpanderDropdownArrowSize.Value;

            if (HeaderSpacingWidth.HasValue)
                Expander.HeaderSpacingWidth = HeaderSpacingWidth.Value;
            if (Header != null)
                Expander.Header = Header.ToElement<MGElement>(Element.SelfOrParentWindow, Parent);

            if (IsExpanded.HasValue)
                Expander.IsExpanded = IsExpanded.Value;
            if (ExpandedVisibility.HasValue)
                Expander.ExpandedVisibility = ExpandedVisibility.Value;
            if (CollapsedVisibility.HasValue)
                Expander.CollapsedVisibility = CollapsedVisibility.Value;

            base.ApplyDerivedSettings(Parent, Element);
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            foreach (Element Element in base.GetChildren())
                yield return Element;

            yield return ExpanderToggleButton;

            if (Header != null)
                yield return Header;

            yield return HeadersPanel;
        }
    }

    public class GroupBox : SingleContentHost
    {
        public override MGElementType ElementType => MGElementType.GroupBox;

        public UniformBorderBrush BorderBrush { get; set; }
        public Thickness? BorderThickness { get; set; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Thickness? BT { get => BorderThickness; set => BorderThickness = value; }

        public Expander Expander { get; set; } = new();
        public ContentPresenter HeaderPresenter { get; set; } = new();

        public bool? IsExpandable { get; set; }

        public Element Header { get; set; }

        public int? HeaderHorizontalMargin { get; set; }
        public int? HeaderHorizontalPadding { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGGroupBox(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGGroupBox GroupBox = Element as MGGroupBox;

            Expander.ApplySettings(GroupBox, GroupBox.Expander);
            HeaderPresenter.ApplySettings(GroupBox, GroupBox.HeaderPresenter);

            if (BorderBrush != null)
                GroupBox.BorderBrush = (MGUniformBorderBrush)BorderBrush.ToBorderBrush();
            if (BorderThickness.HasValue)
                GroupBox.BorderThickness = BorderThickness.Value.ToThickness();
            if (IsExpandable.HasValue)
                GroupBox.IsExpandable = IsExpandable.Value;
            if (Header != null)
                GroupBox.Header = Header.ToElement<MGElement>(Element.SelfOrParentWindow, Parent);
            if (HeaderHorizontalMargin.HasValue)
                GroupBox.HeaderHorizontalMargin = HeaderHorizontalMargin.Value;
            if (HeaderHorizontalPadding.HasValue)
                GroupBox.HeaderHorizontalPadding = HeaderHorizontalPadding.Value;

            base.ApplyDerivedSettings(Parent, Element);
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            foreach (Element Element in base.GetChildren())
                yield return Element;

            yield return Expander;
            yield return HeaderPresenter;

            if (Header != null)
                yield return Header;
        }
    }

    public class Image : Element
    {
        public override MGElementType ElementType => MGElementType.Image;

        public string TextureName { get; set; }
        public string RegionName { get; set; }

        public XAMLColor? TextureColor { get; set; }
        public XAMLRectangle? SourceRect { get; set; }
        public Stretch? Stretch { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent)
        {
            if (!string.IsNullOrEmpty(RegionName))
                return new MGImage(Window, RegionName);
            else
            {
                MGDesktop Desktop = Window.GetDesktop();
                if (!string.IsNullOrEmpty(TextureName))
                    return new MGImage(Window, Desktop.NamedTextures[TextureName], SourceRect?.ToRectangle());
                else
                    return new MGImage(Window, null as Texture2D);
            }
        }

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGImage Image = Element as MGImage;

            if (TextureColor.HasValue)
                Image.TextureColor = TextureColor.Value.ToXNAColor();
            if (SourceRect.HasValue)
                Image.SetTexture(Image.Texture, SourceRect.Value.ToRectangle());
            if (Stretch.HasValue)
                Image.Stretch = Stretch.Value;
        }

        protected internal override IEnumerable<Element> GetChildren() => Enumerable.Empty<Element>();
    }

    public class PasswordBox : TextBox
    {
        public override MGElementType ElementType => MGElementType.PasswordBox;

        public char? PasswordCharacter { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGPasswordBox(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGPasswordBox PasswordBox = Element as MGPasswordBox;

            if (PasswordCharacter.HasValue)
                PasswordBox.PasswordCharacter = PasswordCharacter.Value;

            base.ApplyDerivedSettings(Parent, Element);
        }
    }

    public class ProgressBar : Element
    {
        public override MGElementType ElementType => MGElementType.ProgressBar;

        public Border Border { get; set; } = new();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BorderBrush BorderBrush { get => Border.BorderBrush; set => Border.BorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BorderBrush BB { get => BorderBrush; set => BorderBrush = value; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Thickness? BorderThickness { get => Border.BorderThickness; set => Border.BorderThickness = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Thickness? BT { get => BorderThickness; set => BorderThickness = value; }

        public TextBlock ValueTextBlock { get; set; } = new();
        public bool? ShowValue { get; set; }
        public string ValueDisplayFormat { get; set; }
        public string NumberFormat { get; set; }

        public float? Minimum { get; set; }
        public float? Maximum { get; set; }
        public float? Value { get; set; }

        public int? Size { get; set; }

        public FillBrush CompletedBrush { get; set; }
        public FillBrush IncompleteBrush { get; set; }

        public Orientation? Orientation { get; set; }
        public bool? IsReversed { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGProgressBar(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGProgressBar ProgressBar = Element as MGProgressBar;
            Border.ApplySettings(Parent, ProgressBar.BorderComponent.Element);
            ValueTextBlock.ApplySettings(Parent, ProgressBar.ValueComponent.Element);

            if (ShowValue.HasValue)
                ProgressBar.ShowValue = ShowValue.Value;
            if (ValueDisplayFormat != null)
                ProgressBar.ValueDisplayFormat = ValueDisplayFormat;
            if (NumberFormat != null)
                ProgressBar.NumberFormat = NumberFormat;

            if (Minimum.HasValue)
                ProgressBar.Minimum = Minimum.Value;
            if (Maximum.HasValue)
                ProgressBar.Maximum = Maximum.Value;
            if (Value.HasValue)
                ProgressBar.Value = Value.Value;

            if (Size.HasValue)
                ProgressBar.Size = Size.Value;

            if (CompletedBrush != null)
                ProgressBar.CompletedBrush.NormalValue = CompletedBrush.ToFillBrush();
            if (IncompleteBrush != null)
                ProgressBar.IncompleteBrush.NormalValue = IncompleteBrush.ToFillBrush();

            if (Orientation.HasValue)
                ProgressBar.Orientation = Orientation.Value;
            if (IsReversed.HasValue)
                ProgressBar.IsReversed = IsReversed.Value;
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            yield return Border;
            yield return ValueTextBlock;
        }
    }

    public class RadioButton : SingleContentHost
    {
        public override MGElementType ElementType => MGElementType.RadioButton;

        public Button Button { get; set; } = new();

        public string GroupName { get; set; }

        public int? BubbleComponentSize { get; set; }
        public XAMLColor? BubbleComponentBorderColor { get; set; }
        public float? BubbleComponentBorderThickness { get; set; }
        public XAMLColor? BubbleComponentBackground { get; set; }
        public XAMLColor? BubbleCheckedColor { get; set; }

        public int? SpacingWidth { get; set; }

        public XAMLColor? HoveredHighlightColor { get; set; }
        public float? PressedDarkenIntensity { get; set; }

        public bool? IsChecked { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGRadioButton(Window, GroupName ?? "");

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGRadioButton RadioButton = Element as MGRadioButton;
            Button.ApplySettings(Parent, RadioButton.ButtonComponent.Element);

            if (BubbleComponentSize.HasValue)
                RadioButton.BubbleComponentSize = BubbleComponentSize.Value;
            if (BubbleComponentBorderColor.HasValue)
                RadioButton.BubbleComponentBorderColor = BubbleComponentBorderColor.Value.ToXNAColor();
            if (BubbleComponentBorderThickness.HasValue)
                RadioButton.BubbleComponentBorderThickness = BubbleComponentBorderThickness.Value;
            if (BubbleComponentBackground.HasValue)
                RadioButton.BubbleComponentBackground = new VisualStateColorBrush(BubbleComponentBackground.Value.ToXNAColor());
            if (BubbleCheckedColor.HasValue)
                RadioButton.BubbleCheckedColor = BubbleCheckedColor.Value.ToXNAColor();
            if (SpacingWidth.HasValue)
                RadioButton.SpacingWidth = SpacingWidth.Value;
            if (IsChecked.HasValue)
                RadioButton.IsChecked = IsChecked.Value;

            base.ApplyDerivedSettings(Parent, Element);
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            foreach (Element Element in base.GetChildren())
                yield return Element;

            yield return Button;
        }
    }

    public class RatingControl : Element
    {
        public override MGElementType ElementType => MGElementType.RatingControl;

        public RatingItemShape? ItemShape { get; set; }
        public int? ItemSize { get; set; }
        public int? Spacing { get; set; }

        public float? Minimum { get; set; }
        public float? Maximum { get; set; }
        public float? Value { get; set; }

        public bool? UseDiscreteValues { get; set; }
        public float? DiscreteValueInterval { get; set; }

        public int? UnfilledShapeStrokeThickness { get; set; }
        public XAMLColor? UnfilledShapeStrokeColor { get; set; }
        public XAMLColor? UnfilledShapeFillColor { get; set; }

        public int? FilledShapeStrokeThickness { get; set; }
        public XAMLColor? FilledShapeStrokeColor { get; set; }
        public XAMLColor? FilledShapeFillColor { get; set; }

        public int? PreviewShapeStrokeThickness { get; set; }
        public XAMLColor? PreviewShapeStrokeColor { get; set; }
        public XAMLColor? PreviewShapeFillColor { get; set; }

        public bool? IsReadonly { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGRatingControl(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGRatingControl RatingControl = Element as MGRatingControl;

            if (ItemShape.HasValue)
                RatingControl.ItemShape = ItemShape.Value;
            if (ItemSize.HasValue)
                RatingControl.ItemSize = ItemSize.Value;
            if (Spacing.HasValue)
                RatingControl.Spacing = Spacing.Value;
            if (Minimum.HasValue || Maximum.HasValue)
                RatingControl.SetRange(Minimum ?? RatingControl.Minimum, Maximum ?? RatingControl.Maximum);
            if (Value.HasValue)
                RatingControl.SetValue(Value.Value);
            if (UseDiscreteValues.HasValue)
                RatingControl.UseDiscreteValues = UseDiscreteValues.Value;
            if (DiscreteValueInterval.HasValue)
                RatingControl.DiscreteValueInterval = DiscreteValueInterval.Value;

            if (UnfilledShapeStrokeThickness.HasValue)
                RatingControl.UnfilledShapeStrokeThickness = UnfilledShapeStrokeThickness.Value;
            if (UnfilledShapeStrokeColor.HasValue)
                RatingControl.UnfilledShapeStrokeColor = UnfilledShapeStrokeColor.Value.ToXNAColor();
            if (UnfilledShapeFillColor.HasValue)
                RatingControl.UnfilledShapeFillColor = UnfilledShapeFillColor.Value.ToXNAColor();

            if (FilledShapeStrokeThickness.HasValue)
                RatingControl.FilledShapeStrokeThickness = FilledShapeStrokeThickness.Value;
            if (FilledShapeStrokeColor.HasValue)
                RatingControl.FilledShapeStrokeColor = FilledShapeStrokeColor.Value.ToXNAColor();
            if (FilledShapeFillColor.HasValue)
                RatingControl.FilledShapeFillColor = FilledShapeFillColor.Value.ToXNAColor();

            if (PreviewShapeStrokeThickness.HasValue)
                RatingControl.PreviewShapeStrokeThickness = PreviewShapeStrokeThickness.Value;
            if (PreviewShapeStrokeColor.HasValue)
                RatingControl.PreviewShapeStrokeColor = PreviewShapeStrokeColor.Value.ToXNAColor();
            if (PreviewShapeFillColor.HasValue)
                RatingControl.PreviewShapeFillColor = PreviewShapeFillColor.Value.ToXNAColor();

            if (IsReadonly.HasValue)
                RatingControl.IsReadonly = IsReadonly.Value;
        }

        protected internal override IEnumerable<Element> GetChildren() => Enumerable.Empty<Element>();
    }

    public class Rectangle : Element
    {
        public override MGElementType ElementType => MGElementType.Rectangle;

        public XAMLColor? Stroke { get; set; }
        public int? StrokeThickness { get; set; }
        public FillBrush Fill { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent)
            => new MGRectangle(Window, Width ?? 16, Height ?? 16, Stroke?.ToXNAColor() ?? Color.Transparent, StrokeThickness ?? 1, Fill?.ToFillBrush());

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGRectangle Rectangle = Element as MGRectangle;

            if (Stroke.HasValue)
                Rectangle.Stroke = Stroke.Value.ToXNAColor();
            if (StrokeThickness.HasValue)
                Rectangle.StrokeThickness = StrokeThickness.Value;
            if (Fill != null)
                Rectangle.Fill = Fill.ToFillBrush();
        }

        protected internal override IEnumerable<Element> GetChildren() => Enumerable.Empty<Element>();
    }

    public class ResizeGrip : Element
    {
        public override MGElementType ElementType => MGElementType.ResizeGrip;

        public XAMLColor? Foreground { get; set; }
        public int? MaxDots { get; set; }
        public int? Spacing { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent)
            => throw new InvalidOperationException($"Unsupported feature - cannot instantiate {nameof(MGResizeGrip)} through XAML.");

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGResizeGrip ResizeGrip = Element as MGResizeGrip;

            if (Foreground.HasValue)
                ResizeGrip.Foreground = new(Foreground.Value.ToXNAColor());
            if (MaxDots.HasValue)
                ResizeGrip.MaxDots = MaxDots.Value;
            if (Spacing.HasValue)
                ResizeGrip.Spacing = Spacing.Value;
        }

        protected internal override IEnumerable<Element> GetChildren() => Enumerable.Empty<Element>();
    }

    public class ScrollViewer : SingleContentHost
    {
        public override MGElementType ElementType => MGElementType.ScrollViewer;

        public ScrollBarVisibility? VerticalScrollBarVisibility { get; set; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public ScrollBarVisibility? VSBVisibility { get => VerticalScrollBarVisibility; set => VerticalScrollBarVisibility = value; }

        public ScrollBarVisibility? HorizontalScrollBarVisibility { get; set; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public ScrollBarVisibility? HSBVisibility { get => HorizontalScrollBarVisibility; set => HorizontalScrollBarVisibility = value; }

        public float? VerticalOffset { get; set; }
        public float? HorizontalOffset { get; set; }

        public FillBrush ScrollBarUnfocusedOuterBrush { get; set; }
        public FillBrush ScrollBarFocusedOuterBrush { get; set; }
        public FillBrush ScrollBarUnfocusedInnerBrush { get; set; }
        public FillBrush ScrollBarFocusedInnerBrush { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGScrollViewer(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGScrollViewer ScrollViewer = Element as MGScrollViewer;

            if (VerticalScrollBarVisibility.HasValue)
                ScrollViewer.VerticalScrollBarVisibility = VerticalScrollBarVisibility.Value;
            if (HorizontalScrollBarVisibility.HasValue)
                ScrollViewer.HorizontalScrollBarVisibility = HorizontalScrollBarVisibility.Value;
            if (VerticalOffset.HasValue)
                ScrollViewer.VerticalOffset = VerticalOffset.Value;
            if (HorizontalOffset.HasValue)
                ScrollViewer.HorizontalOffset = HorizontalOffset.Value;

            if (ScrollBarUnfocusedOuterBrush != null)
                ScrollViewer.ScrollBarOuterBrush.NormalValue = ScrollBarUnfocusedOuterBrush.ToFillBrush();
            if (ScrollBarFocusedOuterBrush != null)
                ScrollViewer.ScrollBarOuterBrush.SelectedValue = ScrollBarFocusedOuterBrush.ToFillBrush();

            if (ScrollBarUnfocusedInnerBrush != null)
                ScrollViewer.ScrollBarInnerBrush.NormalValue = ScrollBarUnfocusedInnerBrush.ToFillBrush();
            if (ScrollBarFocusedInnerBrush != null)
                ScrollViewer.ScrollBarInnerBrush.SelectedValue = ScrollBarFocusedInnerBrush.ToFillBrush();

            base.ApplyDerivedSettings(Parent, Element);
        }
    }

    public class Separator : Element
    {
        public override MGElementType ElementType => MGElementType.Separator;

        public Orientation? Orientation { get; set; }
        public int? Size { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent)
            => new MGSeparator(Window, Orientation ?? UI.Orientation.Horizontal);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGSeparator Separator = Element as MGSeparator;

            if (Orientation.HasValue)
                Separator.Orientation = Orientation.Value;
            if (Size.HasValue)
                Separator.Size = Size.Value;
        }

        protected internal override IEnumerable<Element> GetChildren() => Enumerable.Empty<Element>();
    }

    public class Slider : Element
    {
        public override MGElementType ElementType => MGElementType.Slider;

        public float? Minimum { get; set; }
        public float? Maximum { get; set; }
        public float? Value { get; set; }

        public bool? UseDiscreteValues { get; set; }
        public float? DiscreteValueInterval { get; set; }

        public int? NumberLineSize { get; set; }
        public Thickness? NumberLineBorderThickness { get; set; }
        public BorderBrush NumberLineBorderBrush { get; set; }
        public FillBrush NumberLineFillBrush { get; set; }

        public float? TickFrequency { get; set; }
        public bool? DrawTicks { get; set; }
        public int? TickWidth { get; set; }
        public int? TickHeight { get; set; }
        public Thickness? TickBorderThickness { get; set; }
        public BorderBrush TickBorderBrush { get; set; }
        public FillBrush TickFillBrush { get; set; }

        public int? ThumbWidth { get; set; }
        public int? ThumbHeight { get; set; }
        public Thickness? ThumbBorderThickness { get; set; }
        public BorderBrush ThumbBorderBrush { get; set; }
        public FillBrush ThumbFillBrush { get; set; }

        public Orientation? Orientation { get; set; }

        public FillBrush Foreground { get; set; }

        public bool? AcceptsMouseScrollWheel { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent)
            => new MGSlider(Window, Minimum ?? 0, Maximum ?? 100, Value ?? Minimum ?? 0);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGSlider Slider = Element as MGSlider;

            if (Minimum.HasValue || MaxHeight.HasValue)
                Slider.SetRange(Minimum ?? Slider.Minimum, Maximum ?? Slider.Maximum);
            if (Value.HasValue)
                Slider.SetValue(Value.Value);

            if (UseDiscreteValues.HasValue)
                Slider.UseDiscreteValues = UseDiscreteValues.Value;
            if (DiscreteValueInterval.HasValue)
                Slider.DiscreteValueInterval = DiscreteValueInterval.Value;

            if (NumberLineSize.HasValue)
                Slider.NumberLineSize = NumberLineSize.Value;
            if (NumberLineBorderThickness.HasValue)
                Slider.NumberLineBorderThickness = NumberLineBorderThickness.Value.ToThickness();
            if (NumberLineBorderBrush != null)
                Slider.NumberLineBorderBrush = NumberLineBorderBrush.ToBorderBrush();
            if (NumberLineFillBrush != null)
                Slider.NumberLineFillBrush = NumberLineFillBrush.ToFillBrush();

            if (TickFrequency.HasValue)
                Slider.TickFrequency = TickFrequency.Value;
            if (DrawTicks.HasValue)
                Slider.DrawTicks = DrawTicks.Value;
            if (TickWidth.HasValue)
                Slider.TickWidth = TickWidth.Value;
            if (TickHeight.HasValue)
                Slider.TickHeight = TickHeight.Value;
            if (TickBorderThickness.HasValue)
                Slider.TickBorderThickness = TickBorderThickness.Value.ToThickness();
            if (TickBorderBrush != null)
                Slider.TickBorderBrush = TickBorderBrush.ToBorderBrush();
            if (TickFillBrush != null)
                Slider.TickFillBrush = TickFillBrush.ToFillBrush();

            if (ThumbWidth.HasValue)
                Slider.ThumbWidth = ThumbWidth.Value;
            if (ThumbHeight.HasValue)
                Slider.ThumbHeight = ThumbHeight.Value;
            if (ThumbBorderThickness.HasValue)
                Slider.ThumbBorderThickness = ThumbBorderThickness.Value.ToThickness();
            if (ThumbBorderBrush != null)
                Slider.ThumbBorderBrush = ThumbBorderBrush.ToBorderBrush();
            if (ThumbFillBrush != null)
                Slider.ThumbFillBrush = ThumbFillBrush.ToFillBrush();

            if (Orientation.HasValue)
                Slider.Orientation = Orientation.Value;

            if (Foreground != null)
                Slider.Foreground = Foreground.ToFillBrush();

            if (AcceptsMouseScrollWheel.HasValue)
                Slider.AcceptsMouseScrollWheel = AcceptsMouseScrollWheel.Value;
        }

        protected internal override IEnumerable<Element> GetChildren() => Enumerable.Empty<Element>();
    }

    public class Spacer : Element
    {
        public override MGElementType ElementType => MGElementType.Spacer;
        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGSpacer(Window, Width ?? 0, Height ?? 0);
        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element) { }
        protected internal override IEnumerable<Element> GetChildren() => Enumerable.Empty<Element>();
    }

    public class Spoiler : SingleContentHost
    {
        public override MGElementType ElementType => MGElementType.Spoiler;

        public Button Button { get; set; } = new();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BorderBrush UnspoiledBorderBrush { get => Button.BorderBrush; set => Button.BorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Thickness? UnspoiledBorderThickness { get => Button.BorderThickness; set => Button.BorderThickness = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public FillBrush UnspoiledBackgroundBrush { get => Button.Background; set => Button.Background = value; }

        public string UnspoiledText { get; set; }
        public HorizontalAlignment? UnspoiledTextAlignment { get; set; }

        public bool? IsRevealed { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGSpoiler(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGSpoiler Spoiler = Element as MGSpoiler;
            Button.ApplySettings(Parent, Spoiler.ButtonComponent.Element);

            if (UnspoiledText != null)
                Spoiler.UnspoiledText = UnspoiledText;
            if (UnspoiledTextAlignment.HasValue)
                Spoiler.UnspoiledTextAlignment = UnspoiledTextAlignment.Value;
            if (IsRevealed.HasValue)
                Spoiler.IsRevealed = IsRevealed.Value;

            base.ApplyDerivedSettings(Parent, Element);
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            foreach (Element Element in base.GetChildren())
                yield return Element;

            yield return Button;
        }
    }

    public class Stopwatch : Element
    {
        public override MGElementType ElementType => MGElementType.Stopwatch;

        public Border Border { get; set; } = new();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BorderBrush BorderBrush { get => Border.BorderBrush; set => Border.BorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BorderBrush BB { get => BorderBrush; set => BorderBrush = value; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Thickness? BorderThickness { get => Border.BorderThickness; set => Border.BorderThickness = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Thickness? BT { get => BorderThickness; set => BorderThickness = value; }

        public TextBlock Value { get; set; } = new();

        public string ValueDisplayFormat { get; set; }
        public TimeSpan? Elapsed { get; set; }
        public double? TimeScale { get; set; }
        public bool? IsRunning { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGStopwatch(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGStopwatch StopWatch = Element as MGStopwatch;
            Border.ApplySettings(Parent, StopWatch.BorderComponent.Element);
            Value.ApplySettings(Parent, StopWatch.ValueComponent.Element);

            if (ValueDisplayFormat != null)
                StopWatch.ValueDisplayFormat = ValueDisplayFormat;
            if (Elapsed.HasValue)
                StopWatch.Elapsed = Elapsed.Value;
            if (TimeScale.HasValue)
                StopWatch.TimeScale = TimeScale.Value;
            if (IsRunning.HasValue)
            {
                if (IsRunning.Value)
                    StopWatch.Start();
                else
                    StopWatch.Stop();
            }
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            yield return Border;
            yield return Value;
        }
    }

#if UseWPF
    [ContentProperty(nameof(Tabs))]
#endif
    public class TabControl : Element
    {
        public override MGElementType ElementType => MGElementType.TabControl;

        public Border Border { get; set; } = new();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BorderBrush BorderBrush { get => Border.BorderBrush; set => Border.BorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BorderBrush BB { get => BorderBrush; set => BorderBrush = value; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Thickness? BorderThickness { get => Border.BorderThickness; set => Border.BorderThickness = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Thickness? BT { get => BorderThickness; set => BorderThickness = value; }

        public StackPanel HeadersPanel { get; set; } = new();
        public FillBrush HeaderAreaBackground { get; set; }

        public List<TabItem> Tabs { get; set; } = new();

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGTabControl(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGTabControl TabControl = Element as MGTabControl;
            Border.ApplySettings(Parent, TabControl.BorderComponent.Element);
            HeadersPanel.ApplySettings(TabControl, TabControl.HeadersPanelComponent.Element);

            if (HeaderAreaBackground != null)
                TabControl.HeaderAreaBackground.NormalValue = HeaderAreaBackground.ToFillBrush();

            foreach (TabItem Child in Tabs)
            {
                _ = Child.ToElement<MGTabItem>(TabControl.ParentWindow, TabControl);
            }
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            yield return Border;
            yield return HeadersPanel;

            foreach (TabItem Tab in Tabs)
                yield return Tab;
        }
    }

    public class TabItem : SingleContentHost
    {
        public override MGElementType ElementType => MGElementType.TabItem;

        public Element Header { get; set; }

        public bool? IsTabSelected { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent)
        {
            if (Parent is MGTabControl TabControl)
            {
                MGElement HeaderElement = Header?.ToElement<MGElement>(Window, null);
                MGElement ContentElement = Content?.ToElement<MGElement>(Window, null);
                return TabControl.AddTab(HeaderElement, ContentElement);
            }
            else
                throw new InvalidOperationException($"The {nameof(Parent)} {nameof(MGElement)} of an {nameof(MGTabItem)} should be of type {nameof(MGTabControl)}");
        }

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGTabItem TabItem = Element as MGTabItem;

            if (IsTabSelected.HasValue)
                TabItem.IsTabSelected = IsTabSelected.Value;

            //base.ApplyDerivedSettings(Parent, Element);
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            foreach (Element Element in base.GetChildren())
                yield return Element;

            if (Header != null)
                yield return Header;
        }
    }

    public class TextBlock : Element
    {
        public override MGElementType ElementType => MGElementType.TextBlock;

        public string FontFamily { get; set; }
        public int? FontSize { get; set; }

        public bool? IsBold { get; set; }
        public bool? IsItalic { get; set; }
        public bool? IsUnderlined { get; set; }

        public bool? AllowsInlineFormatting { get; set; }

        public XAMLColor? Foreground { get; set; }
        public string Text { get; set; }
        public bool? WrapText { get; set; }
        public float? LinePadding { get; set; }
        public HorizontalAlignment? TextAlignment { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGTextBlock(Window, Text, Foreground?.ToXNAColor());

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGTextBlock TextBlock = Element as MGTextBlock;

            if (FontFamily != null || FontSize.HasValue)
                TextBlock.TrySetFont(FontFamily ?? TextBlock.FontFamily, FontSize ?? TextBlock.FontSize);

            if (IsBold.HasValue)
                TextBlock.IsBold = IsBold.Value;
            if (IsItalic.HasValue)
                TextBlock.IsItalic = IsItalic.Value;
            if (IsUnderlined.HasValue)
                TextBlock.IsUnderlined = IsUnderlined.Value;

            if (AllowsInlineFormatting.HasValue)
                TextBlock.AllowsInlineFormatting = AllowsInlineFormatting.Value;

            if (Foreground.HasValue)
                TextBlock.Foreground.NormalValue = Foreground.Value.ToXNAColor();
            if (Text != null)
                TextBlock.Text = Text;
            if (WrapText.HasValue)
                TextBlock.WrapText = WrapText.Value;
            if (LinePadding.HasValue)
                TextBlock.LinePadding = LinePadding.Value;
            if (TextAlignment.HasValue)
                TextBlock.TextAlignment = TextAlignment.Value;
        }

        protected internal override IEnumerable<Element> GetChildren() => Enumerable.Empty<Element>();
    }

    public class TextBox : Element
    {
        public override MGElementType ElementType => MGElementType.TextBox;

        public Border Border { get; set; } = new();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BorderBrush BorderBrush { get => Border.BorderBrush; set => Border.BorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BorderBrush BB { get => BorderBrush; set => BorderBrush = value; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Thickness? BorderThickness { get => Border.BorderThickness; set => Border.BorderThickness = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Thickness? BT { get => BorderThickness; set => BorderThickness = value; }

        public TextBlock TextBlock { get; set; } = new();

        public string Text { get; set; }
        public bool? WrapText { get; set; }

        public TextBlock Placeholder { get; set; } = new();
        public string PlaceholderText { get; set; }

        public TextBlock CharacterCounter { get; set; } = new();
        public int? CharacterLimit { get; set; }
        public bool? ShowCharacterCount { get; set; }
        public string LimitedCharacterCountFormatString { get; set; }
        public string LimitlessCharacterCountFormatString { get; set; }

        public XAMLColor? FocusedSelectionForegroundColor { get; set; }
        public XAMLColor? FocusedSelectionBackgroundColor { get; set; }
        public XAMLColor? UnfocusedSelectionForegroundColor { get; set; }
        public XAMLColor? UnfocusedSelectionBackgroundColor { get; set; }

        public int? UndoRedoHistorySize { get; set; }

        public bool? IsReadonly { get; set; }

        public bool? AcceptsReturn { get; set; }
        public bool? AcceptsTab { get; set; }

        public bool? IsHeldKeyRepeated { get; set; }
        public TimeSpan? InitialKeyRepeatDelay { get; set; }
        public TimeSpan? KeyRepeatInterval { get; set; }
        public TextEntryMode? TextEntryMode { get; set; }

        public ResizeGrip ResizeGrip { get; set; } = new();
        public bool? IsUserResizable { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGTextBox(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGTextBox TextBox = Element as MGTextBox;
            Border.ApplySettings(Parent, TextBox.BorderComponent.Element);
            TextBlock.ApplySettings(Parent, TextBox.TextBlockComponent.Element);
            Placeholder.ApplySettings(Parent, TextBox.PlaceholderTextBlockComponent.Element);
            CharacterCounter.ApplySettings(Parent, TextBox.CharacterCountComponent.Element);
            ResizeGrip.ApplySettings(Parent, TextBox.ResizeGripComponent.Element);

            if (Text != null)
                TextBox.SetText(Text);
            if (WrapText.HasValue)
                TextBox.WrapText = WrapText.Value;

            if (PlaceholderText != null)
                TextBox.PlaceholderText = PlaceholderText;

            if (CharacterLimit.HasValue)
                TextBox.CharacterLimit = CharacterLimit.Value;
            if (ShowCharacterCount.HasValue)
                TextBox.ShowCharacterCount = ShowCharacterCount.Value;
            if (LimitedCharacterCountFormatString != null)
                TextBox.LimitedCharacterCountFormatString = LimitedCharacterCountFormatString;
            if (LimitlessCharacterCountFormatString != null)
                TextBox.LimitlessCharacterCountFormatString = LimitlessCharacterCountFormatString;

            if (FocusedSelectionForegroundColor != null)
                TextBox.FocusedSelectionForegroundColor = FocusedSelectionForegroundColor.Value.ToXNAColor();
            if (FocusedSelectionBackgroundColor != null)
                TextBox.FocusedSelectionBackgroundColor = FocusedSelectionBackgroundColor.Value.ToXNAColor();
            if (UnfocusedSelectionForegroundColor != null)
                TextBox.UnfocusedSelectionForegroundColor = UnfocusedSelectionForegroundColor.Value.ToXNAColor();
            if (UnfocusedSelectionBackgroundColor != null)
                TextBox.UnfocusedSelectionBackgroundColor = UnfocusedSelectionBackgroundColor.Value.ToXNAColor();

            if (UndoRedoHistorySize.HasValue)
                TextBox.UndoRedoHistorySize = UndoRedoHistorySize.Value;

            if (IsReadonly.HasValue)
                TextBox.IsReadonly = IsReadonly.Value;

            if (AcceptsReturn.HasValue)
                TextBox.AcceptsReturn = AcceptsReturn.Value;
            if (AcceptsTab.HasValue)
                TextBox.AcceptsTab = AcceptsTab.Value;

            if (IsHeldKeyRepeated.HasValue)
                TextBox.IsHeldKeyRepeated = IsHeldKeyRepeated.Value;
            if (InitialKeyRepeatDelay.HasValue)
                TextBox.InitialKeyRepeatDelay = InitialKeyRepeatDelay.Value;
            if (KeyRepeatInterval.HasValue)
                TextBox.KeyRepeatInterval = KeyRepeatInterval.Value;
            if (TextEntryMode.HasValue)
                TextBox.TextEntryMode = TextEntryMode.Value;

            if (IsUserResizable.HasValue)
                TextBox.IsUserResizable = IsUserResizable.Value;
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            yield return Border;
            yield return TextBlock;
            yield return Placeholder;
            yield return CharacterCounter;
            yield return ResizeGrip;
        }
    }

    public class Timer : Element
    {
        public override MGElementType ElementType => MGElementType.Timer;

        public Border Border { get; set; } = new();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BorderBrush BorderBrush { get => Border.BorderBrush; set => Border.BorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BorderBrush BB { get => BorderBrush; set => BorderBrush = value; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Thickness? BorderThickness { get => Border.BorderThickness; set => Border.BorderThickness = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Thickness? BT { get => BorderThickness; set => BorderThickness = value; }

        public TextBlock Value { get; set; } = new();
        public string ValueDisplayFormat { get; set; }
        public TimeSpan? RemainingDuration { get; set; }

        public bool? AllowsNegativeDuration { get; set; }

        public double? TimeScale { get; set; }

        public bool? IsPaused { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent)
            => new MGTimer(Window, RemainingDuration ?? TimeSpan.FromSeconds(60.0), IsPaused ?? true, AllowsNegativeDuration ?? false);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGTimer Timer = Element as MGTimer;
            Border.ApplySettings(Parent, Timer.BorderComponent.Element);
            Value.ApplySettings(Parent, Timer.ValueComponent.Element);

            if (ValueDisplayFormat != null)
                Timer.ValueDisplayFormat = ValueDisplayFormat;
            if (RemainingDuration.HasValue)
                Timer.RemainingDuration = RemainingDuration.Value;

            //if (AllowsNegativeDuration.HasValue)
            //    Timer.AllowsNegativeDuration = AllowsNegativeDuration.Value;

            if (TimeScale.HasValue)
                Timer.TimeScale = TimeScale.Value;
            if (IsPaused.HasValue)
            {
                if (IsPaused.Value)
                    Timer.Pause();
                else
                    Timer.Resume();
            }
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            yield return Border;
            yield return Value;
        }
    }

    public class ToggleButton : SingleContentHost
    {
        public override MGElementType ElementType => MGElementType.ToggleButton;

        public Border Border { get; set; } = new();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BorderBrush BorderBrush { get => Border.BorderBrush; set => Border.BorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BorderBrush BB { get => BorderBrush; set => BorderBrush = value; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Thickness? BorderThickness { get => Border.BorderThickness; set => Border.BorderThickness = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Thickness? BT { get => BorderThickness; set => BorderThickness = value; }

        public FillBrush CheckedBackgroundBrush { get; set; }
        public XAMLColor? CheckedTextForeground { get; set; }
        public bool? IsChecked { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGToggleButton(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGToggleButton ToggleButton = Element as MGToggleButton;
            Border.ApplySettings(Parent, ToggleButton.BorderComponent.Element);

            if (CheckedBackgroundBrush != null)
                ToggleButton.CheckedBackgroundBrush = CheckedBackgroundBrush.ToFillBrush();
            if (CheckedTextForeground.HasValue)
                ToggleButton.CheckedTextForeground = CheckedTextForeground.Value.ToXNAColor();
            if (IsChecked.HasValue)
                ToggleButton.IsChecked = IsChecked.Value;

            base.ApplyDerivedSettings(Parent, Element);
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            foreach (Element Element in base.GetChildren())
                yield return Element;

            yield return Border;
        }
    }

    public class ToolTip : Window
    {
        public override MGElementType ElementType => MGElementType.ToolTip;

        public bool? ShowOnDisabled { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent)
            => new MGToolTip(Window, Parent, Width ?? 0, Height ?? 0);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGToolTip ToolTip = Element as MGToolTip;

            if (ShowOnDisabled.HasValue)
                ToolTip.ShowOnDisabled = ShowOnDisabled.Value;

            base.ApplyDerivedSettings(Parent, Element);
        }
    }

    public class Window : SingleContentHost
    {
        public override MGElementType ElementType => MGElementType.Window;

        public int? Left { get; set; }
        public int? Top { get; set; }

        public SizeToContent? SizeToContent { get; set; }

        public ResizeGrip ResizeGrip { get; set; } = new();
        public bool? IsUserResizable { get; set; }

        public Border Border { get; set; } = new();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BorderBrush BorderBrush { get => Border.BorderBrush; set => Border.BorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BorderBrush BB { get => BorderBrush; set => BorderBrush = value; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Thickness? BorderThickness { get => Border.BorderThickness; set => Border.BorderThickness = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Thickness? BT { get => BorderThickness; set => BorderThickness = value; }

        public Window ModalWindow { get; set; }
        public List<Window> NestedWindows { get; set; } = new();

        public DockPanel TitleBar { get; set; } = new();

        public TextBlock TitleBarTextBlock { get; set; } = new();
        public string TitleText { get; set; }
        public bool? IsTitleBarVisible { get; set; }

        public Button CloseButton { get; set; } = new();
        public bool? IsCloseButtonVisible { get; set; }
        public bool? CanCloseWindow { get; set; }

        public bool? AllowsClickThrough { get; set; }

        public bool? IsDraggable { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent)
        {
            int WindowWidth = Math.Clamp(Width ?? 0, MinWidth ?? 0, MaxWidth ?? int.MaxValue);
            int WindowHeight = Math.Clamp(Height ?? 0, MinHeight ?? 0, MaxHeight ?? int.MaxValue);
            MGWindow Instance = new(Window, Left ?? 0, Top ?? 0, WindowWidth, WindowHeight);
            foreach (Window Nested in NestedWindows)
                Instance.AddNestedWindow(Nested.ToElement<MGWindow>(Window, Window));
            return Instance;
        }

        public MGWindow ToElement(MGDesktop Desktop)
        {
            int WindowWidth = Math.Clamp(Width ?? 0, MinWidth ?? 0, MaxWidth ?? int.MaxValue);
            int WindowHeight = Math.Clamp(Height ?? 0, MinHeight ?? 0, MaxHeight ?? int.MaxValue);
            MGWindow Window = new(Desktop, Left ?? 0, Top ?? 0, WindowWidth, WindowHeight);
            ApplySettings(null, Window);
            return Window;
        }

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGWindow Window = Element as MGWindow;
            ResizeGrip.ApplySettings(Window, Window.ResizeGripComponent.Element);
            Border.ApplySettings(Window, Window.BorderComponent.Element);
            TitleBar.ApplySettings(Window, Window.TitleBarComponent.Element);
            TitleBarTextBlock.ApplySettings(Window, Window.TitleBarTextBlockElement);
            CloseButton.ApplySettings(Window, Window.CloseButtonElement);

            if (IsUserResizable.HasValue)
                Window.IsUserResizable = IsUserResizable.Value;

            if (ModalWindow != null)
                Window.ModalWindow = ModalWindow.ToElement<MGWindow>(Window, Window);

            if (TitleText != null)
                Window.TitleText = TitleText;
            if (IsTitleBarVisible.HasValue)
                Window.IsTitleBarVisible = IsTitleBarVisible.Value;

            if (IsCloseButtonVisible.HasValue)
                Window.IsCloseButtonVisible = IsCloseButtonVisible.Value;
            if (CanCloseWindow.HasValue)
                Window.CanCloseWindow = CanCloseWindow.Value;

            if (AllowsClickThrough.HasValue)
                Window.AllowsClickThrough = AllowsClickThrough.Value;

            if (IsDraggable.HasValue)
                Window.IsDraggable = IsDraggable.Value;

            if (TitleBar.Children.Any())
            {
                MGDockPanel TitleBarDP = Window.TitleBarComponent.Element;
                using (TitleBarDP.AllowChangingContentTemporarily())
                {
                    foreach (MGElement Child in TitleBarDP.Children)
                        Child.Visibility = UI.Visibility.Collapsed;
                    foreach (Element Child in TitleBar.Children)
                        TitleBarDP.TryAddChild(Child.ToElement<MGElement>(Window, TitleBarDP), Child.Dock);
                }
            }

            base.ApplyDerivedSettings(Parent, Element);

            if (SizeToContent != null)
                Window.ApplySizeToContent(SizeToContent.Value, 50, 50, null, null, false);
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            foreach (Element Element in base.GetChildren())
                yield return Element;

            yield return ResizeGrip;
            yield return Border;

            if (ModalWindow != null)
                yield return ModalWindow;

            foreach (Window Window in NestedWindows)
                yield return Window;

            yield return TitleBar;
            yield return TitleBarTextBlock;
            yield return CloseButton;
        }
    }
}
