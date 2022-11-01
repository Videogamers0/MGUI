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
    /// <summary>A wrapper class for <see cref="IFillBrush"/> that controls the getter function, typically to return a copy of the value (to mimic treating class implementations of <see cref="IFillBrush"/> as value-types)</summary>
    public class ThemeFillBrush
    {
        private IFillBrush _Value;
        /// <summary>This property intentionally has no getter. To get the value, use <see cref="GetValue(bool)"/></summary>
        public IFillBrush Value { set => _Value = value; }

        /// <param name="Copy">If true, a copy of the underlying <see cref="Value"/> will be returned. If false, a direct reference to the underlying <see cref="Value"/> will be returned.<para/>
        /// Some implementations of <see cref="IFillBrush"/> are structs (value-types) rather than classes (reference-types),<br/>
        /// so for consistency, recommended to always retrieve a copy, essentially treating all brushes as value-types.</param>
        public IFillBrush GetValue(bool Copy = true) => Copy ? _Value?.Copy() : _Value;

        public ThemeFillBrush() : this(null) { }
        public ThemeFillBrush(IFillBrush Value) { this.Value = Value; }
    }

    public class MGTheme
    {
        /// <summary>The default overlay color to alpha-blend on top of element background brushes. This should always be a transparent <see cref="Color"/></summary>
        public Color HoveredColor { get; set; }
        /// <summary>The default percentage to darken the <see cref="HoveredColor"/> by when the element is both Pressed and Hovered.<para/>
        /// Only used if the element's <see cref="VisualStateBrush.PressedModifierType"/> is <see cref="PressedModifierType.Darken"/></summary>
        public float PressedDarkenModifier { get; set; }
        /// <summary>The default percentage to brighten the <see cref="HoveredColor"/> by when the element is both Pressed and Hovered.<para/>
        /// Only used if the element's <see cref="VisualStateBrush.PressedModifierType"/> is <see cref="PressedModifierType.Brighten"/></summary>
        public float PressedBrightenModifier { get; set; }

        /// <summary>The default background brush to use on clickable controls like <see cref="MGButton"/>, <see cref="MGToggleButton"/>, <see cref="MGComboBox{TItemType}"/><br/>
        /// when their <see cref="PrimaryVisualState"/> is <see cref="PrimaryVisualState.Normal"/></summary>
        public ThemeFillBrush ClickableNormalBackground { get; }
        /// <summary>The default background brush to use on clickable controls like <see cref="MGButton"/>, <see cref="MGToggleButton"/>, <see cref="MGComboBox{TItemType}"/><br/>
        /// when their <see cref="PrimaryVisualState"/> is <see cref="PrimaryVisualState.Selected"/></summary>
        public ThemeFillBrush ClickableSelectedBackground { get; }

        public ThemeFillBrush DimNeutralBackground { get; }
        public ThemeFillBrush BrightNeutralBackground { get; }

        public ThemeFillBrush ContextMenuBackground { get; }
        public ThemeFillBrush ToolTipBackground { get; }

        /// <summary>The default background brush to use on title/header content, like the title bar of a window or the header row of a listview</summary>
        public ThemeFillBrush TitleBackground { get; }

        public ThemeFillBrush AccentBackground { get; }

        public Color RadioButtonBubbleFillColor { get; set; }

        public ThemeFillBrush SliderForeground { get; }
        public ThemeFillBrush SliderThumbFillBrush { get; }

        public ThemeFillBrush ProgressBarCompletedBrush { get; }

        public ThemeFillBrush GridSplitterForeground { get; }

        public Color ResizeGripForeground { get; set; }

        public VisualStateBrush GetSelectedTabHeaderBackgroundBrush() => new(this, BrightNeutralBackground.GetValue(true), HoveredColor);
        public VisualStateBrush GetUnselectedTabHeaderBackgroundBrush() => new(this, DimNeutralBackground.GetValue(true), HoveredColor);

        public VisualStateBrush GetComboBoxDropdownBackgroundBrush() => new(this, BrightNeutralBackground.GetValue(true));

        private VisualStateBrush _DropdownItemBackgroundBrush;
        /// <summary>The default background brush to use on items in an <see cref="MGComboBox{TItemType}"/>'s dropdown.</summary>
        public VisualStateBrush DropdownItemBackgroundBrush { set => _DropdownItemBackgroundBrush = value; }
        public VisualStateBrush GetDropdownItemBackgroundBrush() => _DropdownItemBackgroundBrush?.GetCopy();

        public VisualStateBrush GetTitleBackgroundBrush() => new(this, TitleBackground.GetValue(true), HoveredColor * 0.35f);
        public VisualStateBrush GetSpoilerUnspoiledBackgroundBrush() => new(this, AccentBackground.GetValue(true), HoveredColor);

        public Color GetGridSplitterHoverColor() => HoveredColor.Brighten(0.1f);

        public VisualStateBrush GetBackgroundBrush(MGElementType ElementType)
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
                => new(this, ClickableNormalBackground.GetValue(true), HoveredColor),

                MGElementType.ContextMenu => new(this, ContextMenuBackground.GetValue(true)),

                MGElementType.GroupBox or
                MGElementType.ListView or
                MGElementType.ProgressBar or
                MGElementType.TabControl => new(this, BrightNeutralBackground.GetValue(true)),

                MGElementType.PasswordBox or 
                MGElementType.TextBox => new(this, ClickableNormalBackground.GetValue(true), HoveredColor * 0.4f) { PressedModifier = 0f },

                MGElementType.Separator or
                MGElementType.Stopwatch or
                MGElementType.Timer => new(this, AccentBackground.GetValue(true)),

                MGElementType.ToggleButton => new(this, ClickableNormalBackground.GetValue(true), ClickableSelectedBackground.GetValue(true), ClickableNormalBackground.GetValue(true), HoveredColor),

                MGElementType.ToolTip => new(this, ToolTipBackground.GetValue(true)),

                MGElementType.Window => new(this, DimNeutralBackground.GetValue(true)),

                MGElementType.DockPanel or 
                MGElementType.Grid or
                MGElementType.GridSplitter or
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
            this.HoveredColor = Color.White * 0.18f;
            this.PressedDarkenModifier = 0.06f;
            this.PressedBrightenModifier = 0.06f;

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

            this.DropdownItemBackgroundBrush = new(this, null, (Color.Yellow * 0.65f).AsFillBrush(), null, Color.LightBlue * 0.8f);

            this.ResizeGripForeground = Color.Black;
        }
    }
}
