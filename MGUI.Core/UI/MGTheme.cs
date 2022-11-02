using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Shared.Helpers;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI
{
    /// <summary>A wrapper class for the given <typeparamref name="TDataType"/> that controls the getter function, typically to return a copy of the value (to mimic treating class implementations as value-types)</summary>
    public class ThemeManagedGetter<TDataType>
        where TDataType : class, ICloneable
    {
        private TDataType _Value;
        /// <summary>This property intentionally has no getter. To get the value, use <see cref="GetValue(bool)"/></summary>
        public TDataType Value { set => _Value = value; }

        /// <param name="Copy">If true, a copy of the underlying <see cref="Value"/> will be returned. If false, a direct reference to the underlying <see cref="Value"/> will be returned.<para/>
        /// Some implementations of <typeparamref name="TDataType"/> are structs (value-types) rather than classes (reference-types),<br/>
        /// so for consistency, recommended to always retrieve a copy, essentially treating all objects as value-types.</param>
        public TDataType GetValue(bool Copy = true) => Copy ? _Value?.Clone() as TDataType : _Value;

        public ThemeManagedGetter() : this(null) { }
        public ThemeManagedGetter(TDataType Value) { this.Value = Value; }
    }

    public class ThemeManagedFillBrush : ThemeManagedGetter<IFillBrush>
    {
        public ThemeManagedFillBrush(IFillBrush Value) : base(Value) { }
    }

    public class ThemeManagedVisualStateFillBrush : ThemeManagedGetter<VisualStateFillBrush>
    {
        public ThemeManagedVisualStateFillBrush(VisualStateFillBrush Value) : base(Value) { }
    }

    public class ThemeManagedVisualStateColorBrush : ThemeManagedGetter<VisualStateColorBrush>
    {
        public ThemeManagedVisualStateColorBrush(VisualStateColorBrush Value) : base(Value) { }
    }

    public class MGTheme
    {
        /// <summary>The default overlay color to alpha-blend on top of element background brushes. This should always be a transparent <see cref="Color"/></summary>
        public Color ClickableHoveredColor { get; set; }
        /// <summary>The default percentage to darken the <see cref="ClickableHoveredColor"/> by when the element is both Pressed and Hovered.<para/>
        /// Only used if the element's <see cref="VisualStateBrush{TDataType}.PressedModifierType"/> is <see cref="PressedModifierType.Darken"/></summary>
        public float ClickablePressedModifier { get; set; }

        /// <summary>The default background brush to use on clickable controls like <see cref="MGButton"/>, <see cref="MGToggleButton"/>, <see cref="MGComboBox{TItemType}"/><br/>
        /// when their <see cref="PrimaryVisualState"/> is <see cref="PrimaryVisualState.Normal"/></summary>
        public ThemeManagedFillBrush ClickableNormalBackground { get; }
        /// <summary>The default background brush to use on clickable controls like <see cref="MGButton"/>, <see cref="MGToggleButton"/>, <see cref="MGComboBox{TItemType}"/><br/>
        /// when their <see cref="PrimaryVisualState"/> is <see cref="PrimaryVisualState.Selected"/></summary>
        public ThemeManagedFillBrush ClickableSelectedBackground { get; }

        public ThemeManagedFillBrush DimNeutralBackground { get; }
        public ThemeManagedFillBrush BrightNeutralBackground { get; }

        public ThemeManagedFillBrush ContextMenuBackground { get; }
        public ThemeManagedFillBrush ToolTipBackground { get; }

        /// <summary>The default background brush to use on title/header content, like the title bar of a window or the header row of a listview</summary>
        public ThemeManagedFillBrush TitleBackground { get; }

        public ThemeManagedFillBrush AccentBackground { get; }

        public Color RadioButtonBubbleFillColor { get; set; }

        public ThemeManagedFillBrush SliderForeground { get; }
        public ThemeManagedFillBrush SliderThumbFillBrush { get; }

        public ThemeManagedFillBrush ProgressBarCompletedBrush { get; }

        public ThemeManagedFillBrush GridSplitterForeground { get; }
        public VisualStateFillBrush GetGridSplitterForegroundBrush() => new(this, GridSplitterForeground.GetValue(true), ClickableHoveredColor.Brighten(0.1f));

        public ThemeManagedVisualStateColorBrush ResizeGripForeground { get; }

        public VisualStateFillBrush GetSelectedTabHeaderBackgroundBrush() => new(this, BrightNeutralBackground.GetValue(true), ClickableHoveredColor);
        public VisualStateFillBrush GetUnselectedTabHeaderBackgroundBrush() => new(this, DimNeutralBackground.GetValue(true), ClickableHoveredColor);

        public VisualStateFillBrush GetComboBoxDropdownBackgroundBrush() => new(this, BrightNeutralBackground.GetValue(true));

        /// <summary>The default background brush to use on items in an <see cref="MGComboBox{TItemType}"/>'s dropdown.</summary>
        public ThemeManagedVisualStateFillBrush DropdownItemBackgroundBrush { get; }

        public VisualStateFillBrush GetTitleBackgroundBrush() => new(this, TitleBackground.GetValue(true), ClickableHoveredColor * 0.35f);
        public VisualStateFillBrush GetSpoilerUnspoiledBackgroundBrush() => new(this, AccentBackground.GetValue(true), ClickableHoveredColor);

        public VisualStateFillBrush GetBackgroundBrush(MGElementType ElementType)
        {
            return ElementType switch
            {
                MGElementType.Border or 
                MGElementType.CheckBox or 
                MGElementType.ContentPresenter or
                MGElementType.ContextMenuItem or
                MGElementType.Expander or
                MGElementType.Image or
                MGElementType.RadioButton or
                MGElementType.RatingControl or
                MGElementType.Rectangle or
                MGElementType.ResizeGrip or
                MGElementType.ScrollViewer or
                MGElementType.Slider or
                MGElementType.Spacer or
                MGElementType.Spoiler or
                MGElementType.TabItem or
                MGElementType.TextBlock
                => new(this, null),

                MGElementType.Button or
                MGElementType.ComboBox
                => new(this, ClickableNormalBackground.GetValue(true), ClickableHoveredColor),

                MGElementType.ContextMenu => new(this, ContextMenuBackground.GetValue(true)),

                MGElementType.GridSplitter => new(this, null, ClickableHoveredColor),

                MGElementType.GroupBox or
                MGElementType.ListView or
                MGElementType.ProgressBar or
                MGElementType.TabControl => new(this, BrightNeutralBackground.GetValue(true)),

                MGElementType.PasswordBox or 
                MGElementType.TextBox => new(this, ClickableNormalBackground.GetValue(true), ClickableHoveredColor * 0.4f) { PressedModifier = 0f },

                MGElementType.Separator or
                MGElementType.Stopwatch or
                MGElementType.Timer => new(this, AccentBackground.GetValue(true)),

                MGElementType.ToggleButton => new(this, ClickableNormalBackground.GetValue(true), ClickableSelectedBackground.GetValue(true), ClickableNormalBackground.GetValue(true), ClickableHoveredColor),

                MGElementType.ToolTip => new(this, ToolTipBackground.GetValue(true)),

                MGElementType.Window => new(this, DimNeutralBackground.GetValue(true)),

                MGElementType.DockPanel or 
                MGElementType.Grid or
                MGElementType.OverlayPanel or 
                MGElementType.StackPanel or 
                MGElementType.UniformGrid => new(this, null),

                MGElementType.Misc or 
                MGElementType.Custom => new(this, null),

                _ => new(this, null)
            };
        }

        public MGTheme()
        {
            this.ClickableHoveredColor = Color.White * 0.18f;
            this.ClickablePressedModifier = 0.06f;

            this.ClickableNormalBackground = new(MGSolidFillBrush.LightGray);
            this.ClickableSelectedBackground = new(Color.Green.AsFillBrush());

            this.DimNeutralBackground = new(MGSolidFillBrush.LightGray);
            this.BrightNeutralBackground = new(MGSolidFillBrush.White);

            this.ContextMenuBackground = new(new Color(233, 238, 255).AsFillBrush()); //MGSolidFillBrush.White;
            this.ToolTipBackground = new((new Color(255, 255, 210) * 0.75f).AsFillBrush());

            this.TitleBackground = new(MGSolidFillBrush.SemiBlack);

            this.AccentBackground = new(MGSolidFillBrush.SemiBlack);

            this.RadioButtonBubbleFillColor = Color.LightGray;

            this.SliderForeground = new(MGSolidFillBrush.LightGray);
            this.SliderThumbFillBrush = new(Color.LightGray.Darken(0.1f).AsFillBrush());

            this.GridSplitterForeground = new(MGSolidFillBrush.SemiBlack);

            this.ProgressBarCompletedBrush = new(Color.Green.AsFillBrush());

            this.DropdownItemBackgroundBrush = new ThemeManagedVisualStateFillBrush(
                new VisualStateFillBrush(this, null, (Color.Yellow * 0.65f).AsFillBrush(), null, Color.LightBlue * 0.8f));

            this.ResizeGripForeground = new ThemeManagedVisualStateColorBrush(
                new VisualStateColorBrush(this, Color.Black, Color.White * 0.36f) 
                { 
                    PressedModifierType = PressedModifierType.Brighten,
                    PressedModifier = 0.2f
                });
        }
    }
}
