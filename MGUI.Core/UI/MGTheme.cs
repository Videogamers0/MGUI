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
        public IFillBrush ClickableNormalBackground { get; set; }
        /// <summary>The default background brush to use on clickable controls like <see cref="MGButton"/>, <see cref="MGToggleButton"/>, <see cref="MGComboBox{TItemType}"/><br/>
        /// when their <see cref="PrimaryVisualState"/> is <see cref="PrimaryVisualState.Selected"/></summary>
        public IFillBrush ClickableSelectedBackground { get; set; }

        public IFillBrush DimNeutralBackground { get; set; }
        public IFillBrush BrightNeutralBackground { get; set; }

        public IFillBrush ContextMenuBackground { get; set; }
        public IFillBrush ToolTipBackground { get; set; }

        /// <summary>The default background brush to use on title/header content, like the title bar of a window or the header row of a listview</summary>
        public IFillBrush TitleBackground { get; set; }

        public IFillBrush AccentBackground { get; set; }

        public Color RadioButtonBubbleFillColor { get; set; }

        public IFillBrush SliderForeground { get; set; }
        public IFillBrush SliderThumbFillBrush { get; set; }

        public IFillBrush ProgressBarCompletedBrush { get; set; }

        public Color ResizeGripForeground { get; set; }

        public VisualStateBrush GetSelectedTabHeaderBackgroundBrush() => new(this, BrightNeutralBackground.Copy(), HoveredColor);
        public VisualStateBrush GetUnselectedTabHeaderBackgroundBrush() => new(this, DimNeutralBackground.Copy(), HoveredColor);

        public VisualStateBrush GetComboBoxDropdownBackgroundBrush() => new(this, BrightNeutralBackground.Copy());

        /// <summary>The default background brush to use on items in an <see cref="MGComboBox{TItemType}"/>'s dropdown.</summary>
        public VisualStateBrush DropdownItemBackgroundBrush { get; set; }
        public VisualStateBrush GetDropdownItemBackgroundBrush() => DropdownItemBackgroundBrush.GetCopy();

        public VisualStateBrush GetTitleBackgroundBrush() => new(this, TitleBackground.Copy(), HoveredColor * 0.35f);
        public VisualStateBrush GetSpoilerUnspoiledBackgroundBrush() => new(this, AccentBackground.Copy(), HoveredColor);

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
                => new(this, ClickableNormalBackground.Copy(), HoveredColor),

                MGElementType.ContextMenu => new(this, ContextMenuBackground.Copy()),

                MGElementType.GroupBox or
                MGElementType.ListView or
                MGElementType.ProgressBar or
                MGElementType.TabControl => new(this, BrightNeutralBackground.Copy()),

                MGElementType.PasswordBox or 
                MGElementType.TextBox => new(this, ClickableNormalBackground.Copy(), HoveredColor * 0.4f) { PressedModifier = 0f },

                MGElementType.Separator or
                MGElementType.Stopwatch or
                MGElementType.Timer => new(this, AccentBackground.Copy()),

                MGElementType.ToggleButton => new(this, ClickableNormalBackground.Copy(), ClickableSelectedBackground.Copy(), ClickableNormalBackground.Copy(), HoveredColor),

                MGElementType.ToolTip => new(this, ToolTipBackground.Copy()),

                MGElementType.Window => new(this, DimNeutralBackground.Copy()),

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
            this.HoveredColor = Color.White * 0.18f;
            this.PressedDarkenModifier = 0.06f;
            this.PressedBrightenModifier = 0.06f;

            this.ClickableNormalBackground = MGSolidFillBrush.LightGray;
            this.ClickableSelectedBackground = Color.Green.AsFillBrush();

            this.DimNeutralBackground = MGSolidFillBrush.LightGray;
            this.BrightNeutralBackground = MGSolidFillBrush.White;

            this.ContextMenuBackground = new Color(233, 238, 255).AsFillBrush(); //MGSolidFillBrush.White;
            this.ToolTipBackground = (new Color(255, 255, 210) * 0.75f).AsFillBrush();

            this.TitleBackground = MGSolidFillBrush.SemiBlack;

            this.AccentBackground = MGSolidFillBrush.SemiBlack;

            this.RadioButtonBubbleFillColor = Color.LightGray;

            this.SliderForeground = MGSolidFillBrush.LightGray;
            this.SliderThumbFillBrush = Color.LightGray.Darken(0.1f).AsFillBrush();

            this.ProgressBarCompletedBrush = Color.Green.AsFillBrush();

            this.DropdownItemBackgroundBrush = new(this, null, (Color.Yellow * 0.65f).AsFillBrush(), null, Color.LightBlue * 0.8f);

            this.ResizeGripForeground = Color.Black;
        }
    }
}
