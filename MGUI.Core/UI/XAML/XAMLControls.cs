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
    public class XAMLContentPresenter : XAMLSingleContentHost
    {
        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent, Dictionary<string, Texture2D> NamedTextures) => new MGContentPresenter(Window);
        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, Dictionary<string, Texture2D> NamedTextures) => base.ApplyDerivedSettings(Parent, Element, NamedTextures);
    }

    public class XAMLBorder : XAMLSingleContentHost
    {
        //public static readonly DependencyProperty BorderThicknessProperty = DependencyProperty.RegisterAttached(nameof(BorderThickness), typeof(XAMLThickness?), typeof(XAMLBorder));
        //public static XAMLThickness? GetBorderThickness(DependencyObject element) => ((XAMLThickness?)element.GetValue(BorderThicknessProperty));
        //public static void SetBorderThickness(DependencyObject element, XAMLThickness? borderThickness) => element.SetValue(BorderThicknessProperty, borderThickness);

        public XAMLBorderBrush BorderBrush { get; set; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLBorderBrush BB { get => BorderBrush; set => BorderBrush = value; }

        public XAMLThickness? BorderThickness { get; set; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLThickness? BT { get => BorderThickness; set => BorderThickness = value; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent, Dictionary<string, Texture2D> NamedTextures) => new MGBorder(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, Dictionary<string, Texture2D> NamedTextures)
        {
            MGBorder Border = Element as MGBorder;

            if (BorderBrush != null)
                Border.BorderBrush = BorderBrush.ToBorderBrush();
            if (BorderThickness.HasValue)
                Border.BorderThickness = BorderThickness.Value.ToThickness();

            base.ApplyDerivedSettings(Parent, Element, NamedTextures);
        }
    }

    public class XAMLButton : XAMLSingleContentHost
    {
        public XAMLBorder Border { get; set; } = new();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLBorderBrush BorderBrush { get => Border.BorderBrush; set => Border.BorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLBorderBrush BB { get => BorderBrush; set => BorderBrush = value; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLThickness? BorderThickness { get => Border.BorderThickness; set => Border.BorderThickness = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLThickness? BT { get => BorderThickness; set => BorderThickness = value; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent, Dictionary<string, Texture2D> NamedTextures) => new MGButton(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, Dictionary<string, Texture2D> NamedTextures)
        {
            MGButton Button = Element as MGButton;
            Border.ApplySettings(Button, Button.BorderComponent.Element, NamedTextures);

            base.ApplyDerivedSettings(Parent, Element, NamedTextures);
        }
    }

    public class XAMLCheckBox : XAMLSingleContentHost
    {
        public XAMLButton Button { get; set; } = new();

        public int? CheckBoxComponentSize { get; set; }
        public int? SpacingWidth { get; set; }
        public XAMLColor? CheckMarkColor { get; set; }
        public bool? IsThreeState { get; set; }
        public bool? IsChecked { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent, Dictionary<string, Texture2D> NamedTextures) => new MGCheckBox(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, Dictionary<string, Texture2D> NamedTextures)
        {
            MGCheckBox CheckBox = Element as MGCheckBox;
            Button.ApplySettings(CheckBox, CheckBox.ButtonComponent.Element, NamedTextures);

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

            base.ApplyDerivedSettings(Parent, Element, NamedTextures);
        }
    }

    public class XAMLComboBox : XAMLElement
    {
        /// <summary>The generic type that will be used when instantiating <see cref="MGComboBox{TItemType}"/>.<para/>
        /// To set this value from a XAML string, you must define the namespace the type belongs to, then use the x:Type Markup Extension<br/>
        /// (See: <see href="https://learn.microsoft.com/en-us/dotnet/desktop/xaml-services/xtype-markup-extension"/>)<para/>
        /// Example:
        /// <code>&lt;ComboBox xmlns:System="clr-namespace:System;assembly=mscorlib" ItemType="{x:Type System:Double}" /&gt;</code><para/>
        /// Default value: typeof(string)</summary>
        public Type ItemType { get; set; } = typeof(string);

        public XAMLBorder Border { get; set; } = new();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLBorderBrush BorderBrush { get => Border.BorderBrush; set => Border.BorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLBorderBrush BB { get => BorderBrush; set => BorderBrush = value; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLThickness? BorderThickness { get => Border.BorderThickness; set => Border.BorderThickness = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLThickness? BT { get => BorderThickness; set => BorderThickness = value; }

        public int? DropdownArrowLeftMargin { get; set; }
        public int? DropdownArrowRightMargin { get; set; }
        public XAMLColor? DropdownArrowColor { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent, Dictionary<string, Texture2D> NamedTextures)
        {
            Type GenericType = typeof(MGComboBox<>).MakeGenericType(new Type[] { ItemType });
            object Element = Activator.CreateInstance(GenericType, new object[] { Window });
            return Element as MGElement;
        }

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, Dictionary<string, Texture2D> NamedTextures)
        {
            Type GenericType = typeof(MGComboBox<>).MakeGenericType(new Type[] { ItemType });
            MethodInfo Method = GenericType.GetMethod(nameof(MGComboBox<object>.LoadSettings), BindingFlags.Instance | BindingFlags.NonPublic);
            Method.Invoke(Element, new object[] { this, NamedTextures });
        }
    }

    public class XAMLExpander : XAMLSingleContentHost
    {
        public XAMLToggleButton ExpanderToggleButton { get; set; } = new();

        public int? ExpanderButtonSize { get; set; }
        public XAMLBorderBrush ExpanderButtonBorderBrush { get; set; }
        public XAMLThickness? ExpanderButtonBorderThickness { get; set; }
        public XAMLFillBrush ExpanderButtonBackgroundBrush { get; set; }
        public XAMLColor? ExpanderDropdownArrowColor { get; set; }
        public int? ExpanderDropdownArrowSize { get; set; }

        public int? HeaderSpacingWidth { get; set; }
        public XAMLElement HeaderContent { get; set; }

        public bool? IsExpanded { get; set; }
        public Visibility? ExpandedVisibility { get; set; }
        public Visibility? CollapsedVisibility { get; set; }

        public XAMLStackPanel HeadersPanel { get; set; } = new();

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent, Dictionary<string, Texture2D> NamedTextures) => new MGExpander(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, Dictionary<string, Texture2D> NamedTextures)
        {
            MGExpander Expander = Element as MGExpander;
            ExpanderToggleButton.ApplySettings(Expander, Expander.ExpanderToggleButton, NamedTextures);
            HeadersPanel.ApplySettings(Expander, Expander.HeadersPanelComponent.Element, NamedTextures);

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
            if (HeaderContent != null)
                Expander.HeaderContent = HeaderContent.ToElement<MGElement>(Element.SelfOrParentWindow, Parent, NamedTextures);

            if (IsExpanded.HasValue)
                Expander.IsExpanded = IsExpanded.Value;
            if (ExpandedVisibility.HasValue)
                Expander.ExpandedVisibility = ExpandedVisibility.Value;
            if (CollapsedVisibility.HasValue)
                Expander.CollapsedVisibility = CollapsedVisibility.Value;

            base.ApplyDerivedSettings(Parent, Element, NamedTextures);
        }
    }

    public class XAMLGroupBox : XAMLSingleContentHost
    {
        public XAMLUniformBorderBrush BorderBrush { get; set; }
        public XAMLThickness? BorderThickness { get; set; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLThickness? BT { get => BorderThickness; set => BorderThickness = value; }

        public XAMLElement HeaderContent { get; set; }
        public int? HeaderHorizontalMargin { get; set; }
        public int? HeaderHorizontalPadding { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent, Dictionary<string, Texture2D> NamedTextures) => new MGGroupBox(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, Dictionary<string, Texture2D> NamedTextures)
        {
            MGGroupBox GroupBox = Element as MGGroupBox;

            if (BorderBrush != null)
                GroupBox.BorderBrush = (MGUniformBorderBrush)BorderBrush.ToBorderBrush();
            if (BorderThickness.HasValue)
                GroupBox.BorderThickness = BorderThickness.Value.ToThickness();
            if (HeaderContent != null)
                GroupBox.HeaderContent = HeaderContent.ToElement<MGElement>(Element.SelfOrParentWindow, Parent, NamedTextures);
            if (HeaderHorizontalMargin.HasValue)
                GroupBox.HeaderHorizontalMargin = HeaderHorizontalMargin.Value;
            if (HeaderHorizontalPadding.HasValue)
                GroupBox.HeaderHorizontalPadding = HeaderHorizontalPadding.Value;

            base.ApplyDerivedSettings(Parent, Element, NamedTextures);
        }
    }

    public class XAMLImage : XAMLElement
    {
        public string Texture { get; set; }
        public XAMLRectangle? SourceRect { get; set; }
        public Stretch? Stretch { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent, Dictionary<string, Texture2D> NamedTextures) => new MGImage(Window, Texture == null ? null : NamedTextures[Texture], SourceRect?.ToRectangle());

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, Dictionary<string, Texture2D> NamedTextures)
        {
            MGImage Image = Element as MGImage;

            if (SourceRect.HasValue)
                Image.SetTexture(Image.Texture, SourceRect.Value.ToRectangle());
            if (Stretch.HasValue)
                Image.Stretch = Stretch.Value;
        }
    }

    public class XAMLPasswordBox : XAMLTextBox
    {
        public char? PasswordCharacter { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent, Dictionary<string, Texture2D> NamedTextures) => new MGProgressBar(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, Dictionary<string, Texture2D> NamedTextures)
        {
            MGPasswordBox PasswordBox = Element as MGPasswordBox;

            if (PasswordCharacter.HasValue)
                PasswordBox.PasswordCharacter = PasswordCharacter.Value;

            base.ApplyDerivedSettings(Parent, Element, NamedTextures);
        }
    }

    public class XAMLProgressBar : XAMLElement
    {
        public XAMLBorder Border { get; set; } = new();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLBorderBrush BorderBrush { get => Border.BorderBrush; set => Border.BorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLBorderBrush BB { get => BorderBrush; set => BorderBrush = value; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLThickness? BorderThickness { get => Border.BorderThickness; set => Border.BorderThickness = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLThickness? BT { get => BorderThickness; set => BorderThickness = value; }

        public XAMLTextBlock ValueTextBlock { get; set; } = new();
        public bool? ShowValue { get; set; }
        public string ValueDisplayFormat { get; set; }

        public float? Minimum { get; set; }
        public float? Maximum { get; set; }
        public float? Value { get; set; }

        public int? Size { get; set; }

        public XAMLFillBrush CompletedBrush { get; set; }
        public XAMLFillBrush IncompleteBrush { get; set; }

        public Orientation? Orientation { get; set; }
        public bool? IsReversed { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent, Dictionary<string, Texture2D> NamedTextures) => new MGProgressBar(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, Dictionary<string, Texture2D> NamedTextures)
        {
            MGProgressBar ProgressBar = Element as MGProgressBar;
            Border.ApplySettings(Parent, ProgressBar.BorderComponent.Element, NamedTextures);
            ValueTextBlock.ApplySettings(Parent, ProgressBar.ValueComponent.Element, NamedTextures);

            if (ShowValue.HasValue)
                ProgressBar.ShowValue = ShowValue.Value;
            if (ValueDisplayFormat != null)
                ProgressBar.ValueDisplayFormat = ValueDisplayFormat;

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
    }

    public class XAMLRadioButton : XAMLSingleContentHost
    {
        public XAMLButton Button { get; set; } = new();

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

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent, Dictionary<string, Texture2D> NamedTextures) => new MGRadioButton(Window, GroupName ?? "");

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, Dictionary<string, Texture2D> NamedTextures)
        {
            MGRadioButton RadioButton = Element as MGRadioButton;
            Button.ApplySettings(Parent, RadioButton.ButtonComponent.Element, NamedTextures);

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

            base.ApplyDerivedSettings(Parent, Element, NamedTextures);
        }
    }

    public class XAMLRatingControl : XAMLElement
    {
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

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent, Dictionary<string, Texture2D> NamedTextures) => new MGRatingControl(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, Dictionary<string, Texture2D> NamedTextures)
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
    }

    public class XAMLRectangleElement : XAMLElement
    {
        public XAMLColor? Stroke { get; set; }
        public int? StrokeThickness { get; set; }
        public XAMLFillBrush Fill { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent, Dictionary<string, Texture2D> NamedTextures)
            => new MGRectangle(Window, Width ?? 16, Height ?? 16, Stroke?.ToXNAColor() ?? Color.Transparent, StrokeThickness ?? 1, Fill?.ToFillBrush());

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, Dictionary<string, Texture2D> NamedTextures)
        {
            MGRectangle Rectangle = Element as MGRectangle;

            if (Stroke.HasValue)
                Rectangle.Stroke = Stroke.Value.ToXNAColor();
            if (StrokeThickness.HasValue)
                Rectangle.StrokeThickness = StrokeThickness.Value;
            if (Fill != null)
                Rectangle.Fill = Fill.ToFillBrush();
        }
    }

    public class XAMLResizeGrip : XAMLElement
    {
        public XAMLColor? Foreground { get; set; }
        public int? MaxDots { get; set; }
        public int? Spacing { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent, Dictionary<string, Texture2D> NamedTextures)
            => throw new InvalidOperationException($"Unsupported feature - cannot instantiate {nameof(MGResizeGrip)} through XAML.");

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, Dictionary<string, Texture2D> NamedTextures)
        {
            MGResizeGrip ResizeGrip = Element as MGResizeGrip;

            if (Foreground.HasValue)
                ResizeGrip.Foreground = new(Foreground.Value.ToXNAColor());
            if (MaxDots.HasValue)
                ResizeGrip.MaxDots = MaxDots.Value;
            if (Spacing.HasValue)
                ResizeGrip.Spacing = Spacing.Value;
        }
    }

    public class XAMLScrollViewer : XAMLSingleContentHost
    {
        public ScrollBarVisibility? VerticalScrollBarVisibility { get; set; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public ScrollBarVisibility? VSBVisibility { get => VerticalScrollBarVisibility; set => VerticalScrollBarVisibility = value; }

        public ScrollBarVisibility? HorizontalScrollBarVisibility { get; set; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public ScrollBarVisibility? HSBVisibility { get => HorizontalScrollBarVisibility; set => HorizontalScrollBarVisibility = value; }

        public float? VerticalOffset { get; set; }
        public float? HorizontalOffset { get; set; }

        public XAMLFillBrush ScrollBarOuterBrush { get; set; }
        public XAMLFillBrush ScrollBarUnfocusedBrush { get; set; }
        public XAMLFillBrush ScrollBarFocusedBrush { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent, Dictionary<string, Texture2D> NamedTextures) => new MGScrollViewer(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, Dictionary<string, Texture2D> NamedTextures)
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

            if (ScrollBarOuterBrush != null)
                ScrollViewer.ScrollBarOuterBrush = ScrollBarOuterBrush.ToFillBrush();
            if (ScrollBarUnfocusedBrush != null)
                ScrollViewer.ScrollBarUnfocusedBrush = ScrollBarUnfocusedBrush.ToFillBrush();
            if (ScrollBarFocusedBrush != null)
                ScrollViewer.ScrollBarFocusedBrush = ScrollBarFocusedBrush.ToFillBrush();

            base.ApplyDerivedSettings(Parent, Element, NamedTextures);
        }
    }

    public class XAMLSeparator : XAMLElement
    {
        public Orientation? Orientation { get; set; }
        public int? Size { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent, Dictionary<string, Texture2D> NamedTextures)
            => new MGSeparator(Window, Orientation ?? UI.Orientation.Horizontal);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, Dictionary<string, Texture2D> NamedTextures)
        {
            MGSeparator Separator = Element as MGSeparator;

            if (Orientation.HasValue)
                Separator.Orientation = Orientation.Value;
            if (Size.HasValue)
                Separator.Size = Size.Value;
        }
    }

    public class XAMLSlider : XAMLElement
    {
        public float? Minimum { get; set; }
        public float? Maximum { get; set; }
        public float? Value { get; set; }

        public bool? UseDiscreteValues { get; set; }
        public float? DiscreteValueInterval { get; set; }

        public int? NumberLineSize { get; set; }
        public XAMLThickness? NumberLineBorderThickness { get; set; }
        public XAMLBorderBrush NumberLineBorderBrush { get; set; }
        public XAMLFillBrush NumberLineFillBrush { get; set; }

        public float? TickFrequency { get; set; }
        public bool? DrawTicks { get; set; }
        public int? TickWidth { get; set; }
        public int? TickHeight { get; set; }
        public XAMLThickness? TickBorderThickness { get; set; }
        public XAMLBorderBrush TickBorderBrush { get; set; }
        public XAMLFillBrush TickFillBrush { get; set; }

        public int? ThumbWidth { get; set; }
        public int? ThumbHeight { get; set; }
        public XAMLThickness? ThumbBorderThickness { get; set; }
        public XAMLBorderBrush ThumbBorderBrush { get; set; }
        public XAMLFillBrush ThumbFillBrush { get; set; }

        public Orientation? Orientation { get; set; }

        public XAMLFillBrush Foreground { get; set; }

        public bool? AcceptsMouseScrollWheel { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent, Dictionary<string, Texture2D> NamedTextures)
            => new MGSlider(Window, Minimum ?? 0, Maximum ?? 100, Value ?? Minimum ?? 0);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, Dictionary<string, Texture2D> NamedTextures)
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
    }

    public class XAMLSpacer : XAMLElement
    {
        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent, Dictionary<string, Texture2D> NamedTextures) => new MGSpacer(Window, Width ?? 0, Height ?? 0);
        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, Dictionary<string, Texture2D> NamedTextures) { }
    }

    public class XAMLSpoiler : XAMLSingleContentHost
    {
        public XAMLButton Button { get; set; } = new();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLBorderBrush UnspoiledBorderBrush { get => Button.BorderBrush; set => Button.BorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLThickness? UnspoiledBorderThickness { get => Button.BorderThickness; set => Button.BorderThickness = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLFillBrush UnspoiledBackgroundBrush { get => Button.Background; set => Button.Background = value; }

        public string UnspoiledText { get; set; }
        public HorizontalAlignment? UnspoiledTextAlignment { get; set; }

        public bool? IsRevealed { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent, Dictionary<string, Texture2D> NamedTextures) => new MGSpoiler(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, Dictionary<string, Texture2D> NamedTextures)
        {
            MGSpoiler Spoiler = Element as MGSpoiler;
            Button.ApplySettings(Parent, Spoiler.ButtonComponent.Element, NamedTextures);

            if (UnspoiledText != null)
                Spoiler.UnspoiledText = UnspoiledText;
            if (UnspoiledTextAlignment.HasValue)
                Spoiler.UnspoiledTextAlignment = UnspoiledTextAlignment.Value;
            if (IsRevealed.HasValue)
                Spoiler.IsRevealed = IsRevealed.Value;

            base.ApplyDerivedSettings(Parent, Element, NamedTextures);
        }
    }

    public class XAMLStopWatch : XAMLElement
    {
        public XAMLBorder Border { get; set; } = new();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLBorderBrush BorderBrush { get => Border.BorderBrush; set => Border.BorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLBorderBrush BB { get => BorderBrush; set => BorderBrush = value; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLThickness? BorderThickness { get => Border.BorderThickness; set => Border.BorderThickness = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLThickness? BT { get => BorderThickness; set => BorderThickness = value; }

        public XAMLTextBlock Value { get; set; } = new();

        public string ValueDisplayFormat { get; set; }
        public TimeSpan? Elapsed { get; set; }
        public double? TimeScale { get; set; }
        public bool? IsRunning { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent, Dictionary<string, Texture2D> NamedTextures) => new MGStopwatch(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, Dictionary<string, Texture2D> NamedTextures)
        {
            MGStopwatch StopWatch = Element as MGStopwatch;
            Border.ApplySettings(Parent, StopWatch.BorderComponent.Element, NamedTextures);
            Value.ApplySettings(Parent, StopWatch.ValueComponent.Element, NamedTextures);

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
    }

#if UseWPF
    [ContentProperty(nameof(Tabs))]
#endif
    public class XAMLTabControl : XAMLElement
    {
        public XAMLBorder Border { get; set; } = new();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLBorderBrush BorderBrush { get => Border.BorderBrush; set => Border.BorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLBorderBrush BB { get => BorderBrush; set => BorderBrush = value; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLThickness? BorderThickness { get => Border.BorderThickness; set => Border.BorderThickness = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLThickness? BT { get => BorderThickness; set => BorderThickness = value; }

        public XAMLStackPanel HeadersPanel { get; set; } = new();
        public XAMLFillBrush HeaderAreaBackground { get; set; }

        public List<XAMLTabItem> Tabs { get; set; } = new();

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent, Dictionary<string, Texture2D> NamedTextures) => new MGTabControl(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, Dictionary<string, Texture2D> NamedTextures)
        {
            MGTabControl TabControl = Element as MGTabControl;
            Border.ApplySettings(Parent, TabControl.BorderComponent.Element, NamedTextures);
            HeadersPanel.ApplySettings(TabControl, TabControl.HeadersPanelComponent.Element, NamedTextures);

            if (HeaderAreaBackground != null)
                TabControl.HeaderAreaBackground.NormalValue = HeaderAreaBackground.ToFillBrush();

            foreach (XAMLTabItem Child in Tabs)
            {
                MGElement Header = Child.HeaderContent?.ToElement<MGElement>(TabControl.ParentWindow, TabControl, NamedTextures);
                MGElement Content = Child.Content?.ToElement<MGElement>(TabControl.ParentWindow, TabControl, NamedTextures);
                TabControl.AddTab(Header, Content);
            }
        }
    }

    public class XAMLTabItem : XAMLSingleContentHost
    {
        public XAMLElement HeaderContent { get; set; }
        public bool? IsTabSelected { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent, Dictionary<string, Texture2D> NamedTextures)
        {
            if (Parent is MGTabControl TabControl)
            {
                MGElement HeaderElement = HeaderContent?.ToElement<MGElement>(Window, null, NamedTextures);
                MGElement ContentElement = Content?.ToElement<MGElement>(Window, null, NamedTextures);
                return TabControl.AddTab(HeaderElement, ContentElement);
            }
            else
                throw new InvalidOperationException($"The {nameof(Parent)} {nameof(MGElement)} of an {nameof(MGTabItem)} should be of type {nameof(MGTabControl)}");
        }

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, Dictionary<string, Texture2D> NamedTextures)
        {
            MGTabItem TabItem = Element as MGTabItem;

            //if (HeaderContent != null) // This is already handled in CreateElementInstance
            //    TabItem.HeaderContent = HeaderContent.ToElement<MGElement>(Element.SelfOrParentWindow, null, NamedTextures);
            if (IsTabSelected.HasValue)
                TabItem.IsTabSelected = IsTabSelected.Value;

            //base.ApplyDerivedSettings(Parent, Element, NamedTextures);
        }
    }

    public class XAMLTextBlock : XAMLElement
    {
        public string FontFamily { get; set; }
        public int? FontSize { get; set; }
        public XAMLColor? Foreground { get; set; }
        public string Text { get; set; }
        public bool? WrapText { get; set; }
        public float? LinePadding { get; set; }
        public HorizontalAlignment? TextAlignment { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent, Dictionary<string, Texture2D> NamedTextures) => new MGTextBlock(Window, Text, Foreground?.ToXNAColor());

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, Dictionary<string, Texture2D> NamedTextures)
        {
            MGTextBlock TextBlock = Element as MGTextBlock;

            if (FontFamily != null || FontSize.HasValue)
                TextBlock.TrySetFont(FontFamily ?? TextBlock.FontFamily, FontSize ?? TextBlock.FontSize);
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
    }

    public class XAMLTextBox : XAMLElement
    {
        public XAMLBorder Border { get; set; } = new();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLBorderBrush BorderBrush { get => Border.BorderBrush; set => Border.BorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLBorderBrush BB { get => BorderBrush; set => BorderBrush = value; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLThickness? BorderThickness { get => Border.BorderThickness; set => Border.BorderThickness = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLThickness? BT { get => BorderThickness; set => BorderThickness = value; }

        public XAMLTextBlock TextBlock { get; set; } = new();

        public string Text { get; set; }
        public bool? WrapText { get; set; }

        public XAMLTextBlock Placeholder { get; set; } = new();
        public string PlaceholderText { get; set; }

        public XAMLTextBlock CharacterCounter { get; set; } = new();
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

        public XAMLResizeGrip ResizeGrip { get; set; } = new();
        public bool? IsUserResizable { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent, Dictionary<string, Texture2D> NamedTextures) => new MGTextBox(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, Dictionary<string, Texture2D> NamedTextures)
        {
            MGTextBox TextBox = Element as MGTextBox;
            Border.ApplySettings(Parent, TextBox.BorderComponent.Element, NamedTextures);
            TextBlock.ApplySettings(Parent, TextBox.TextBlockComponent.Element, NamedTextures);
            Placeholder.ApplySettings(Parent, TextBox.PlaceholderTextBlockComponent.Element, NamedTextures);
            CharacterCounter.ApplySettings(Parent, TextBox.CharacterCountComponent.Element, NamedTextures);
            ResizeGrip.ApplySettings(Parent, TextBox.ResizeGripComponent.Element, NamedTextures);

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
    }

    public class XAMLTimer : XAMLElement
    {
        public XAMLBorder Border { get; set; } = new();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLBorderBrush BorderBrush { get => Border.BorderBrush; set => Border.BorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLBorderBrush BB { get => BorderBrush; set => BorderBrush = value; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLThickness? BorderThickness { get => Border.BorderThickness; set => Border.BorderThickness = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLThickness? BT { get => BorderThickness; set => BorderThickness = value; }

        public XAMLTextBlock Value { get; set; } = new();
        public string ValueDisplayFormat { get; set; }
        public TimeSpan? RemainingDuration { get; set; }

        public bool? AllowsNegativeDuration { get; set; }

        public double? TimeScale { get; set; }

        public bool? IsPaused { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent, Dictionary<string, Texture2D> NamedTextures)
            => new MGTimer(Window, RemainingDuration ?? TimeSpan.FromSeconds(60.0), IsPaused ?? true, AllowsNegativeDuration ?? false);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, Dictionary<string, Texture2D> NamedTextures)
        {
            MGTimer Timer = Element as MGTimer;
            Border.ApplySettings(Parent, Timer.BorderComponent.Element, NamedTextures);
            Value.ApplySettings(Parent, Timer.ValueComponent.Element, NamedTextures);

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
    }

    public class XAMLToggleButton : XAMLSingleContentHost
    {
        public XAMLBorder Border { get; set; } = new();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLBorderBrush BorderBrush { get => Border.BorderBrush; set => Border.BorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLBorderBrush BB { get => BorderBrush; set => BorderBrush = value; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLThickness? BorderThickness { get => Border.BorderThickness; set => Border.BorderThickness = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLThickness? BT { get => BorderThickness; set => BorderThickness = value; }

        public XAMLFillBrush CheckedBackgroundBrush { get; set; }
        public XAMLColor? CheckedTextForeground { get; set; }
        public bool? IsChecked { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent, Dictionary<string, Texture2D> NamedTextures) => new MGToggleButton(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, Dictionary<string, Texture2D> NamedTextures)
        {
            MGToggleButton ToggleButton = Element as MGToggleButton;
            Border.ApplySettings(Parent, ToggleButton.BorderComponent.Element, NamedTextures);

            if (CheckedBackgroundBrush != null)
                ToggleButton.CheckedBackgroundBrush = CheckedBackgroundBrush.ToFillBrush();
            if (CheckedTextForeground.HasValue)
                ToggleButton.CheckedTextForeground = CheckedTextForeground.Value.ToXNAColor();
            if (IsChecked.HasValue)
                ToggleButton.IsChecked = IsChecked.Value;

            base.ApplyDerivedSettings(Parent, Element, NamedTextures);
        }
    }

    public class XAMLToolTip : XAMLWindow
    {
        public bool? ShowOnDisabled { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent, Dictionary<string, Texture2D> NamedTextures)
            => new MGToolTip(Window, Parent, Width ?? 0, Height ?? 0);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, Dictionary<string, Texture2D> NamedTextures)
        {
            MGToolTip ToolTip = Element as MGToolTip;

            if (ShowOnDisabled.HasValue)
                ToolTip.ShowOnDisabled = ShowOnDisabled.Value;

            base.ApplyDerivedSettings(Parent, Element, NamedTextures);
        }
    }

    public class XAMLWindow : XAMLSingleContentHost
    {
        public int? Left { get; set; }
        public int? Top { get; set; }

        public XAMLResizeGrip ResizeGrip { get; set; } = new();
        public bool? IsUserResizable { get; set; }

        public XAMLBorder Border { get; set; } = new();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLBorderBrush BorderBrush { get => Border.BorderBrush; set => Border.BorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLBorderBrush BB { get => BorderBrush; set => BorderBrush = value; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLThickness? BorderThickness { get => Border.BorderThickness; set => Border.BorderThickness = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public XAMLThickness? BT { get => BorderThickness; set => BorderThickness = value; }

        public XAMLWindow ModalWindow { get; set; }
        public List<XAMLWindow> NestedWindows { get; set; } = new();

        public XAMLDockPanel TitleBar { get; set; } = new();

        public XAMLTextBlock TitleBarTextBlock { get; set; } = new();
        public string TitleText { get; set; }
        public bool? IsTitleBarVisible { get; set; }

        public XAMLButton CloseButton { get; set; } = new();
        public bool? IsCloseButtonVisible { get; set; }
        public bool? CanCloseWindow { get; set; }

        public bool? AllowsClickThrough { get; set; }

        public bool? IsDraggable { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent, Dictionary<string, Texture2D> NamedTextures)
        {
            MGWindow Instance = new(Window, Left ?? 0, Top ?? 0, Width ?? 0, Height ?? 0);
            foreach (XAMLWindow Nested in NestedWindows)
                Instance.AddNestedWindow(Nested.ToElement<MGWindow>(Window, Window, NamedTextures));
            return Instance;
        }

        public MGWindow ToElement(MGDesktop Desktop, Dictionary<string, Texture2D> NamedTextures)
        {
            MGWindow Window = new(Desktop, Left ?? 0, Top ?? 0, Width ?? 0, Height ?? 0);
            ApplySettings(null, Window, NamedTextures);
            return Window;
        }

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, Dictionary<string, Texture2D> NamedTextures)
        {
            MGWindow Window = Element as MGWindow;
            ResizeGrip.ApplySettings(Window, Window.ResizeGripComponent.Element, NamedTextures);
            Border.ApplySettings(Window, Window.BorderComponent.Element, NamedTextures);
            TitleBar.ApplySettings(Window, Window.TitleBarComponent.Element, NamedTextures);
            TitleBarTextBlock.ApplySettings(Window, Window.TitleBarTextBlockElement, NamedTextures);
            CloseButton.ApplySettings(Window, Window.CloseButtonElement, NamedTextures);

            if (IsUserResizable.HasValue)
                Window.IsUserResizable = IsUserResizable.Value;

            if (ModalWindow != null)
                Window.ModalWindow = ModalWindow.ToElement<MGWindow>(Window, Window, NamedTextures);

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
                    foreach (XAMLElement Child in TitleBar.Children)
                        TitleBarDP.TryAddChild(Child.ToElement<MGElement>(Window, TitleBarDP, NamedTextures), Child.Dock);
                }
            }

            base.ApplyDerivedSettings(Parent, Element, NamedTextures);
        }
    }
}
