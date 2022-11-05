using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Shared.Helpers;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
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
        #region Background
        private Dictionary<MGElementType, ThemeManagedVisualStateFillBrush> _Backgrounds { get; }

        public VisualStateFillBrush GetBackgroundBrush(MGElementType Type)
        {
            if (_Backgrounds.TryGetValue(Type, out ThemeManagedVisualStateFillBrush Value))
                return Value.GetValue(true);
            else
                return new VisualStateFillBrush(null);
        }

        public void SetBackgroundBrush(MGElementType Type, VisualStateFillBrush Value)
        {
            if (!_Backgrounds.TryGetValue(Type, out ThemeManagedVisualStateFillBrush ManagedValue))
            {
                ManagedValue = new(Value);
                _Backgrounds.Add(Type, ManagedValue);
            }
            else
                ManagedValue.Value = Value;
        }
        #endregion Background

        public ThemeManagedVisualStateFillBrush ComboBoxDropdownBackground { get; }
        /// <summary>The default background brush to use on items in an <see cref="MGComboBox{TItemType}"/>'s dropdown.</summary>
        public ThemeManagedVisualStateFillBrush ComboBoxDropdownItemBackground { get; }

        public Color DropdownArrowColor { get; set; }

        public ThemeManagedVisualStateFillBrush GridSplitterForeground { get; }

        public ThemeManagedVisualStateFillBrush ProgressBarCompletedBrush { get; }
        public ThemeManagedVisualStateFillBrush ProgressBarIncompleteBrush { get; }

        public ThemeManagedVisualStateColorBrush RadioButtonBubbleBackground { get; set; }

        public ThemeManagedVisualStateColorBrush ResizeGripForeground { get; }

        public ThemeManagedVisualStateFillBrush ScrollBarOuterBrush { get; }
        public ThemeManagedVisualStateFillBrush ScrollBarInnerBrush { get; }

        public ThemeManagedFillBrush SliderForeground { get; }
        public ThemeManagedFillBrush SliderThumbFillBrush { get; }
        public ThemeManagedVisualStateFillBrush SliderOverlay { get; }

        public ThemeManagedVisualStateFillBrush SpoilerUnspoiledBackground { get; }

        public ThemeManagedVisualStateFillBrush SelectedTabHeaderBackground { get; }
        public ThemeManagedVisualStateFillBrush UnselectedTabHeaderBackground { get; }

        /// <summary>Default value to use for <see cref="MGTextBox.FocusedSelectionForegroundColor"/></summary>
        public Color TextBoxFocusedSelectionForeground { get; set; }
        /// <summary>Default value to use for <see cref="MGTextBox.FocusedSelectionBackgroundColor"/></summary>
        public Color TextBoxFocusedSelectionBackground { get; set; }
        /// <summary>Default value to use for <see cref="MGTextBox.UnfocusedSelectionForegroundColor"/></summary>
        public Color TextBoxUnfocusedSelectionForeground { get; set; }
        /// <summary>Default value to use for <see cref="MGTextBox.UnfocusedSelectionBackgroundColor"/></summary>
        public Color TextBoxUnfocusedSelectionBackground { get; set; }

        public ThemeManagedVisualStateFillBrush TitleBackground { get; }

        /// <summary>The fallback value to use for <see cref="MGTextBlock.ActualForeground"/> when there is no foreground color applied to the <see cref="MGTextBlock"/> or its parents.</summary>
        public ThemeManagedVisualStateColorBrush TextBlockFallbackForeground { get; }

        public enum BuiltInTheme
        {
            Windows_Gray,
            Dark_Blue
        }

        public MGTheme()
            : this(BuiltInTheme.Dark_Blue) { }

        public MGTheme(BuiltInTheme ThemeType)
        {
            _Backgrounds = new();

            List<MGElementType> TypesWithoutBackgrounds = new()
            {
                MGElementType.Border,
                MGElementType.CheckBox,
                MGElementType.ContentPresenter,
                MGElementType.ContextMenuItem,
                MGElementType.Custom,
                MGElementType.DockPanel,
                MGElementType.Expander,
                MGElementType.Grid,
                MGElementType.Image,
                MGElementType.OverlayPanel,
                MGElementType.Misc,
                MGElementType.RadioButton,
                MGElementType.RatingControl,
                MGElementType.Rectangle,
                MGElementType.ResizeGrip,
                MGElementType.ScrollViewer,
                MGElementType.Slider,
                MGElementType.Spacer,
                MGElementType.Spoiler,
                MGElementType.StackPanel,
                MGElementType.TabItem,
                MGElementType.TextBlock,
                MGElementType.UniformGrid
            };
            foreach (MGElementType Type in TypesWithoutBackgrounds)
            {
                _Backgrounds[Type] = new ThemeManagedVisualStateFillBrush(new VisualStateFillBrush(null));
            }

            if (ThemeType == BuiltInTheme.Dark_Blue)
            {
                Color PrimaryColor = new(0, 108, 214);
                MGSolidFillBrush PrimaryBG = new(PrimaryColor);
                Color BrightNeutralColor = new Color(11, 28, 72);
                IFillBrush BrightNeutralBrush = new MGSolidFillBrush(BrightNeutralColor);

                //  Button/ComboBox
                ThemeManagedVisualStateFillBrush ButtonBG =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            PrimaryBG, PrimaryBG, PrimaryBG * 0.5f,
                            Color.White * 0.12f, PressedModifierType.Darken, 0.06f)
                    );
                _Backgrounds[MGElementType.Button] = new ThemeManagedVisualStateFillBrush(ButtonBG.GetValue(true));
                _Backgrounds[MGElementType.ComboBox] = new ThemeManagedVisualStateFillBrush(ButtonBG.GetValue(true));

                //  Context Menu
                ThemeManagedVisualStateFillBrush ContextMenuBG =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            BrightNeutralBrush,
                            null, PressedModifierType.Darken, 0.06f)
                    );
                _Backgrounds[MGElementType.ContextMenu] = ContextMenuBG;

                //  GridSplitter
                ThemeManagedVisualStateFillBrush GridSplitterBG =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            null,
                            Color.White * 0.18f, PressedModifierType.Darken, 0.06f)
                    );
                _Backgrounds[MGElementType.GridSplitter] = GridSplitterBG;

                //  GroupBox/ListView/ProgressBar/TabControl
                ThemeManagedVisualStateFillBrush BrightNeutralBG =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            BrightNeutralBrush,
                            null, PressedModifierType.Darken, 0.06f)
                    );
                _Backgrounds[MGElementType.GroupBox] = new ThemeManagedVisualStateFillBrush(BrightNeutralBG.GetValue(true));
                _Backgrounds[MGElementType.ListView] = new ThemeManagedVisualStateFillBrush(BrightNeutralBG.GetValue(true));
                _Backgrounds[MGElementType.ProgressBar] = new ThemeManagedVisualStateFillBrush(BrightNeutralBG.GetValue(true));
                _Backgrounds[MGElementType.TabControl] = new ThemeManagedVisualStateFillBrush(BrightNeutralBG.GetValue(true));

                //  PasswordBox/TextBox
                ThemeManagedVisualStateFillBrush TextBoxBG =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            PrimaryBG,
                            Color.White * 0.08f, PressedModifierType.Darken, 0f)
                    );
                _Backgrounds[MGElementType.PasswordBox] = new ThemeManagedVisualStateFillBrush(TextBoxBG.GetValue(true));
                _Backgrounds[MGElementType.TextBox] = new ThemeManagedVisualStateFillBrush(TextBoxBG.GetValue(true));

                //  Separator/Stopwatch/Timer
                Color AccentColor = new Color(0, 60, 119);
                IFillBrush AccentBrush = AccentColor.AsFillBrush();
                ThemeManagedVisualStateFillBrush AccentBG =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            AccentBrush,
                            Color.White * 0.04f, PressedModifierType.Darken, 0f)
                    );
                _Backgrounds[MGElementType.Separator] = new ThemeManagedVisualStateFillBrush(AccentBG.GetValue(true));
                _Backgrounds[MGElementType.Stopwatch] = new ThemeManagedVisualStateFillBrush(AccentBG.GetValue(true));
                _Backgrounds[MGElementType.Timer] = new ThemeManagedVisualStateFillBrush(AccentBG.GetValue(true));

                //  ToggleButton
                ThemeManagedVisualStateFillBrush ToggleButtonBG =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            PrimaryBG, new Color(0, 60, 119).AsFillBrush(), PrimaryBG * 0.5f,
                            Color.White * 0.12f, PressedModifierType.Darken, 0.06f)
                    );
                _Backgrounds[MGElementType.ToggleButton] = new ThemeManagedVisualStateFillBrush(ToggleButtonBG.GetValue(true));

                //  ToolTip
                ThemeManagedVisualStateFillBrush ToolTipBG =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            (new Color(255, 255, 210) * 0.75f).AsFillBrush(),
                            null, PressedModifierType.Darken, 0.06f)
                    );
                _Backgrounds[MGElementType.ToolTip] = new ThemeManagedVisualStateFillBrush(ToolTipBG.GetValue(true));

                //  Window
                Color DimNeutralColor = new Color(0, 10, 18);
                IFillBrush DimNeutralBackground = new MGSolidFillBrush(DimNeutralColor);
                ThemeManagedVisualStateFillBrush WindowBG =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            DimNeutralBackground,
                            null, PressedModifierType.Darken, 0.06f)
                    );
                _Backgrounds[MGElementType.Window] = new ThemeManagedVisualStateFillBrush(WindowBG.GetValue(true));

                this.ComboBoxDropdownBackground =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            BrightNeutralBrush,
                            null, PressedModifierType.Darken, 0.06f)
                    );
                this.ComboBoxDropdownItemBackground =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            null, Color.Yellow.AsFillBrush() * 0.65f, null,
                            Color.LightBlue * 0.4f, PressedModifierType.Darken, 0.06f)
                    );

                this.DropdownArrowColor = Color.White;

                this.GridSplitterForeground =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            PrimaryBG,
                            Color.White * 0.22f, PressedModifierType.Darken, 0.06f)
                    );

                this.ProgressBarCompletedBrush =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            PrimaryBG,
                            Color.White * 0.02f, PressedModifierType.Darken, 0f)
                    ); ;
                this.ProgressBarIncompleteBrush =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            MGSolidFillBrush.Transparent,
                            Color.White * 0.01f, PressedModifierType.Darken, 0f)
                    );

                this.RadioButtonBubbleBackground =
                    new ThemeManagedVisualStateColorBrush(
                        new VisualStateColorBrush(
                            PrimaryColor,
                            Color.White * 0.12f, PressedModifierType.Darken, 0.06f)
                    );

                this.ResizeGripForeground =
                    new ThemeManagedVisualStateColorBrush(
                        new VisualStateColorBrush(
                            PrimaryColor, Color.White * 0.36f, PressedModifierType.Brighten, 0.2f)
                    );

                Color ScrollBarBorderColor = new Color(0, 44, 112); // could use BrightNeutralColor
                this.ScrollBarOuterBrush =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            new MGBorderedFillBrush(new Thickness(1), new MGUniformBorderBrush(ScrollBarBorderColor), DimNeutralBackground, true),
                            Color.White * 0.06f, PressedModifierType.Brighten, 0f)
                    );
                this.ScrollBarInnerBrush =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            new MGBorderedFillBrush(new Thickness(1), new MGUniformBorderBrush(ScrollBarBorderColor), new MGSolidFillBrush(AccentColor), false),
                            new MGBorderedFillBrush(new Thickness(1), new MGUniformBorderBrush(ScrollBarBorderColor), PrimaryBG, false),
                            new MGBorderedFillBrush(new Thickness(1), new MGUniformBorderBrush(ScrollBarBorderColor), new MGSolidFillBrush(AccentColor) * 0.5f, false),
                            Color.White * 0.02f, PressedModifierType.Brighten, 0f)
                    );

                this.SliderForeground = new(PrimaryBG.Copy());
                this.SliderThumbFillBrush = new(PrimaryColor.Darken(0.1f).AsFillBrush());
                this.SliderOverlay =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            null,
                            Color.White * 0.12f, PressedModifierType.Darken, 0.06f)
                    );

                this.SelectedTabHeaderBackground =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            PrimaryBG,
                            Color.White * 0.12f, PressedModifierType.Darken, 0.06f)
                    );
                this.UnselectedTabHeaderBackground =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            AccentBrush,
                            Color.White * 0.12f, PressedModifierType.Darken, 0.06f)
                    );

                this.SpoilerUnspoiledBackground =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            MGSolidFillBrush.SemiBlack,
                            Color.White * 0.15f, PressedModifierType.Darken, 0.06f)
                    );

                this.TextBoxFocusedSelectionForeground = Color.White;
                this.TextBoxFocusedSelectionBackground = Color.Black;
                this.TextBoxUnfocusedSelectionForeground = Color.White;
                this.TextBoxUnfocusedSelectionBackground = new Color(210, 240, 255) * 0.4f;

                this.TitleBackground =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            new MGSolidFillBrush(new Color(0, 52, 96)),
                            Color.White * 0.05f, PressedModifierType.Darken, 0.06f)
                    );

                this.TextBlockFallbackForeground =
                    new ThemeManagedVisualStateColorBrush(
                        new VisualStateColorBrush(Color.White)
                    );
            }
            else if (ThemeType == BuiltInTheme.Windows_Gray)
            {
                Color PrimaryColor = Color.LightGray;
                MGSolidFillBrush PrimaryBG = new(PrimaryColor);

                //  Button/ComboBox
                ThemeManagedVisualStateFillBrush ButtonBG =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            PrimaryBG, PrimaryBG, PrimaryBG * 0.5f,
                            Color.White * 0.18f, PressedModifierType.Darken, 0.06f)
                    );
                _Backgrounds[MGElementType.Button] = new ThemeManagedVisualStateFillBrush(ButtonBG.GetValue(true));
                _Backgrounds[MGElementType.ComboBox] = new ThemeManagedVisualStateFillBrush(ButtonBG.GetValue(true));

                //  Context Menu
                ThemeManagedVisualStateFillBrush ContextMenuBG =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            new Color(233, 238, 255).AsFillBrush(), // Maybe should just use MGSolidFillBrush.White
                            null, PressedModifierType.Darken, 0.06f)
                    );
                _Backgrounds[MGElementType.ContextMenu] = ContextMenuBG;

                //  GridSplitter
                ThemeManagedVisualStateFillBrush GridSplitterBG =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            null,
                            Color.White * 0.18f, PressedModifierType.Darken, 0.06f)
                    );
                _Backgrounds[MGElementType.GridSplitter] = GridSplitterBG;

                //  GroupBox/ListView/ProgressBar/TabControl
                IFillBrush BrightNeutralBrush = MGSolidFillBrush.White;
                ThemeManagedVisualStateFillBrush BrightNeutralBG =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            BrightNeutralBrush,
                            null, PressedModifierType.Darken, 0.06f)
                    );
                _Backgrounds[MGElementType.GroupBox] = new ThemeManagedVisualStateFillBrush(BrightNeutralBG.GetValue(true));
                _Backgrounds[MGElementType.ListView] = new ThemeManagedVisualStateFillBrush(BrightNeutralBG.GetValue(true));
                _Backgrounds[MGElementType.ProgressBar] = new ThemeManagedVisualStateFillBrush(BrightNeutralBG.GetValue(true));
                _Backgrounds[MGElementType.TabControl] = new ThemeManagedVisualStateFillBrush(BrightNeutralBG.GetValue(true));

                //  PasswordBox/TextBox
                ThemeManagedVisualStateFillBrush TextBoxBG =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            PrimaryBG,
                            Color.White * 0.25f, PressedModifierType.Darken, 0f)
                    );
                _Backgrounds[MGElementType.PasswordBox] = new ThemeManagedVisualStateFillBrush(TextBoxBG.GetValue(true));
                _Backgrounds[MGElementType.TextBox] = new ThemeManagedVisualStateFillBrush(TextBoxBG.GetValue(true));

                //  Separator/Stopwatch/Timer
                IFillBrush AccentBrush = MGSolidFillBrush.SemiBlack;
                ThemeManagedVisualStateFillBrush AccentBG =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            AccentBrush,
                            Color.White * 0.04f, PressedModifierType.Darken, 0f)
                    );
                _Backgrounds[MGElementType.Separator] = new ThemeManagedVisualStateFillBrush(AccentBG.GetValue(true));
                _Backgrounds[MGElementType.Stopwatch] = new ThemeManagedVisualStateFillBrush(AccentBG.GetValue(true));
                _Backgrounds[MGElementType.Timer] = new ThemeManagedVisualStateFillBrush(AccentBG.GetValue(true));

                //  ToggleButton
                ThemeManagedVisualStateFillBrush ToggleButtonBG =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            PrimaryBG, Color.LightBlue.AsFillBrush(), PrimaryBG * 0.5f,
                            Color.White * 0.18f, PressedModifierType.Darken, 0.06f)
                    );
                _Backgrounds[MGElementType.ToggleButton] = new ThemeManagedVisualStateFillBrush(ToggleButtonBG.GetValue(true));

                //  ToolTip
                ThemeManagedVisualStateFillBrush ToolTipBG =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            (new Color(255, 255, 210) * 0.75f).AsFillBrush(),
                            null, PressedModifierType.Darken, 0.06f)
                    );
                _Backgrounds[MGElementType.ToolTip] = new ThemeManagedVisualStateFillBrush(ToolTipBG.GetValue(true));

                //  Window
                IFillBrush DimNeutralBackground = MGSolidFillBrush.LightGray;
                ThemeManagedVisualStateFillBrush WindowBG =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            DimNeutralBackground,
                            null, PressedModifierType.Darken, 0.06f)
                    );
                _Backgrounds[MGElementType.Window] = new ThemeManagedVisualStateFillBrush(WindowBG.GetValue(true));

                this.ComboBoxDropdownBackground =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            MGSolidFillBrush.White,
                            null, PressedModifierType.Darken, 0.06f)
                    );
                this.ComboBoxDropdownItemBackground =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            null, Color.Yellow.AsFillBrush() * 0.65f, null,
                            Color.LightBlue * 0.8f, PressedModifierType.Darken, 0.06f)
                    );

                this.DropdownArrowColor = Color.Black;

                this.GridSplitterForeground =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            MGSolidFillBrush.SemiBlack,
                            Color.White * 0.25f, PressedModifierType.Darken, 0.06f)
                    );

                this.ProgressBarCompletedBrush =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            Color.Green.AsFillBrush(),
                            Color.White * 0.02f, PressedModifierType.Darken, 0f)
                    );
                this.ProgressBarIncompleteBrush =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            MGSolidFillBrush.SemiBlack,
                            Color.White * 0.01f, PressedModifierType.Darken, 0f)
                    );

                this.RadioButtonBubbleBackground =
                    new ThemeManagedVisualStateColorBrush(
                        new VisualStateColorBrush(
                            PrimaryColor,
                            Color.White * 0.18f, PressedModifierType.Darken, 0.06f)
                    );

                this.ResizeGripForeground =
                    new ThemeManagedVisualStateColorBrush(
                        new VisualStateColorBrush(
                            Color.Black, Color.White * 0.36f, PressedModifierType.Brighten, 0.2f)
                    );

                this.ScrollBarOuterBrush =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            new MGBorderedFillBrush(new Thickness(1), MGUniformBorderBrush.Black, new MGSolidFillBrush(new Color(88, 88, 88)), true),
                            Color.White * 0.04f, PressedModifierType.Darken, 0f)
                    );
                this.ScrollBarInnerBrush =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            new MGBorderedFillBrush(new Thickness(1), new MGUniformBorderBrush(Color.Black), new MGSolidFillBrush(new Color(204, 204, 204)), false),
                            new MGBorderedFillBrush(new Thickness(1), new MGUniformBorderBrush(Color.Black), new MGSolidFillBrush(new Color(232, 232, 232)), false),
                            new MGBorderedFillBrush(new Thickness(1), new MGUniformBorderBrush(Color.Black), new MGSolidFillBrush(new Color(204, 204, 204)) * 0.5f, false),
                            Color.White * 0.02f, PressedModifierType.Darken, 0f)
                    );

                this.SliderForeground = new(PrimaryBG.Copy());
                this.SliderThumbFillBrush = new(PrimaryColor.Darken(0.1f).AsFillBrush());
                this.SliderOverlay =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            null,
                            Color.White * 0.18f, PressedModifierType.Darken, 0.06f)
                    );

                this.SelectedTabHeaderBackground =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            MGSolidFillBrush.White,
                            Color.White * 0.18f, PressedModifierType.Darken, 0.06f)
                    );
                this.UnselectedTabHeaderBackground =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            PrimaryBG,
                            Color.White * 0.18f, PressedModifierType.Darken, 0.06f)
                    );

                this.SpoilerUnspoiledBackground =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            MGSolidFillBrush.SemiBlack,
                            Color.White * 0.18f, PressedModifierType.Darken, 0.06f)
                    );

                this.TextBoxFocusedSelectionBackground = new(60, 120, 255);
                this.TextBoxFocusedSelectionForeground = Color.White;
                this.TextBoxUnfocusedSelectionBackground = new Color(210, 240, 255) * 0.4f;
                this.TextBoxUnfocusedSelectionForeground = Color.White;

                this.TitleBackground =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            MGSolidFillBrush.SemiBlack,
                            Color.White * 0.05f, PressedModifierType.Darken, 0.06f)
                    );

                this.TextBlockFallbackForeground =
                    new ThemeManagedVisualStateColorBrush(
                        new VisualStateColorBrush(Color.Black)
                    );
            }
        }
    }
}
