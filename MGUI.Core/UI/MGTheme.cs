using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Shared.Helpers;
using MGUI.Shared.Text;
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

    public class ThemeFontSettings
    {
        /// <summary>The default fontsize for content inside an <see cref="MGContextMenu"/>, such as <see cref="MGContextMenuButton"/> and <see cref="MGContextMenuToggle"/></summary>
        public int ContextMenuFontSize { get; set; } = 10;

        public int SmallFontSize { get; set; } = 10;
        public int MediumFontSize { get; set; } = 12;
        public int LargeFontSize { get; set; } = 14;

        public int DefaultFontSize { get; set; } = 11;

        /// <summary>Changes all font sizes by the given <paramref name="Offset"/></summary>
        public void AdjustAllFontSizes(int Offset)
        {
            ContextMenuFontSize += Offset;
            SmallFontSize += Offset;
            MediumFontSize += Offset;
            LargeFontSize += Offset;
            DefaultFontSize += Offset;
        }

        /// <summary>If true, <see cref="MGTextBlock"/> will attempt to draw text with a scale that most closely results in the desired font size.<br/>
        /// If false, <see cref="MGTextBlock"/> may choose a slightly different font size that approximates the exact size, but results in better scaling results.<para/>
        /// For example, if you have SpriteFonts for these font sizes: 8, 10, 12, and you wanted to use font size = 19<br/>
        /// If <see cref="UseExactScale"/> is true: <see cref="MGTextBlock"/> would choose size=10, scale=1.9<br/>
        /// If <see cref="UseExactScale"/> is false: <see cref="MGTextBlock"/> would choose size=10, scale=2.0, preferring to scale by values such as 0.25, 0.5, 1.0, 2.0 etc<para/>
        /// Default value: false</summary>
        public bool UseExactScale { get; set; } = false;

        /// <summary>The name of the font that should be used by default in <see cref="MGTextBlock"/>s when no font family is explicitly specified.<para/>
        /// If null, uses <see cref="FontManager.DefaultFontFamily"/> instead.<para/>
        /// EX: "Arial". If not null, the <see cref="FontManager"/> must contain a <see cref="FontSet"/> with <see cref="FontSet.Name"/> that matches this value.</summary>
        public string DefaultFontFamily { get; set; }

        /// <summary>The fallback value to use for <see cref="MGTextBlock.ShadowOffset"/> when <see cref="MGTextBlock.ShadowOffset"/> is null.</summary>
        public Point DefaultFontShadowOffset { get; set; } = new(1, 1);
        /// <summary>The fallback value to use for <see cref="MGTextBlock.ShadowColor"/> when <see cref="MGTextBlock.ShadowColor"/> is null.</summary>
        public Color DefaultFontShadowColor { get; set; } = Color.Black;

        public ThemeFontSettings(string FontFamily)
        {
            this.DefaultFontFamily = FontFamily;
        }
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

        /// <summary>The default value to use for <see cref="MGCheckBox.CheckMarkColor"/></summary>
        public Color CheckMarkColor { get; set; }

        public Color DropdownArrowColor { get; set; }

        public ThemeManagedVisualStateFillBrush GridSplitterForeground { get; }

        /// <summary>The default background brush to use on items in an <see cref="MGListBox{TItemType}"/>.</summary>
        public ThemeManagedVisualStateFillBrush ListBoxItemBackground { get; }
        /// <summary>The default value to use for <see cref="MGListBox{TItemType}.AlternatingRowBackgrounds"/></summary>
        public List<ThemeManagedFillBrush> ListBoxItemAlternatingRowBackgrounds { get; }

        public ThemeManagedVisualStateFillBrush ProgressBarCompletedBrush { get; }
        public ThemeManagedVisualStateFillBrush ProgressBarIncompleteBrush { get; }

        public ThemeManagedVisualStateColorBrush RadioButtonBubbleBackground { get; set; }
        /// <summary>The default value to use for <see cref="MGRadioButton.BubbleCheckedColor"/></summary>
        public Color RadioButtonCheckedFillColor { get; set; }

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

        /// <summary>The default offset from the current mouse position to draw <see cref="MGToolTip"/>s at.<br/>
        /// This value is used to initialize <see cref="MGToolTip.DrawOffset"/><para/>
        /// Default value: (6, 6)</summary>
        public Point ToolTipOffset { get; set; }

        public VisualStateSetting<Color?> ToolTipTextForeground { get; set; }

        public ThemeFontSettings FontSettings { get; }

        public enum BuiltInTheme
        {
            Light_Gray,

            Dark_Gray,
            Dark_Red,
            Dark_Green,
            Dark_Blue,

            Dark_Purple,
            Dark_Pink,
            Dark_Yellow,
            Dark_Turquoise,
            Dark_OrangeBrown,
            Dark_LightBrown
        }

        public MGTheme(string DefaultFontFamily)
            : this(BuiltInTheme.Dark_Blue, DefaultFontFamily) { }

        public MGTheme(BuiltInTheme ThemeType, string DefaultFontFamily)
        {
            FontSettings = new(DefaultFontFamily);
            ToolTipOffset = new(6, 6);

            _Backgrounds = new();

            List<MGElementType> TypesWithoutBackgrounds = new()
            {
                MGElementType.Border,
                MGElementType.CheckBox,
                MGElementType.ContentPresenter,
                MGElementType.ContextMenuItem,
                MGElementType.Custom,
                MGElementType.XAMLDesigner,
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
                MGElementType.UniformGrid,
                MGElementType.TabItem,
                MGElementType.TextBlock,
                MGElementType.UserControl,
                MGElementType.OverlayHost,
                MGElementType.Overlay
            };
            foreach (MGElementType Type in TypesWithoutBackgrounds)
            {
                _Backgrounds[Type] = new ThemeManagedVisualStateFillBrush(new VisualStateFillBrush(null));
            }

            if (ThemeType != BuiltInTheme.Light_Gray)
            {
                Color ToolTipBGColor = new(56, 56, 56);

                Color ListItemSelectedColor = Color.Yellow;
                Color ListItemHoveredColor = Color.LightBlue;

                Color TextColor = Color.White;
                Color TextBoxFocusedSelectionBG = Color.Black;
                Color TextBoxUnfocusedSelectionBG = new(210, 240, 255);

                Color CheckMarkColor = Color.Black;

                Color PrimaryColor, BrightNeutralColor, DropdownArrowColor;
                float TitleShadowIntensity;
                if (ThemeType == BuiltInTheme.Dark_Gray)
                {
                    PrimaryColor = new(188, 188, 188);
                    BrightNeutralColor = new(84, 84, 84);
                    DropdownArrowColor = Color.Black;
                    TitleShadowIntensity = 0.76f;
                }
                else if (ThemeType == BuiltInTheme.Dark_Red)
                {
                    PrimaryColor = new(204, 0, 36);
                    BrightNeutralColor = new(60, 8, 12);
                    DropdownArrowColor = Color.White;
                    TitleShadowIntensity = 0.48f;
                }
                else if (ThemeType == BuiltInTheme.Dark_Green)
                {
                    PrimaryColor = new(18, 168, 30);
                    BrightNeutralColor = new(24, 56, 10);
                    DropdownArrowColor = Color.White;
                    TitleShadowIntensity = 0.48f;
                }
                else if (ThemeType == BuiltInTheme.Dark_Blue)
                {
                    PrimaryColor = new(0, 108, 214);
                    BrightNeutralColor = new(11, 28, 72);
                    DropdownArrowColor = Color.White;
                    TitleShadowIntensity = 0.48f;
                }
                else if (ThemeType == BuiltInTheme.Dark_Purple)
                {
                    PrimaryColor = new(154, 16, 184);
                    BrightNeutralColor = new(50, 24, 70);
                    DropdownArrowColor = Color.White;
                    TitleShadowIntensity = 0.48f;
                }
                else if (ThemeType == BuiltInTheme.Dark_Pink)
                {
                    PrimaryColor = new(255, 32, 188);
                    BrightNeutralColor = new(136, 0, 96);
                    DropdownArrowColor = Color.Black;
                    TitleShadowIntensity = 0.70f;
                }
                else if (ThemeType == BuiltInTheme.Dark_Yellow)
                {
                    PrimaryColor = new(250, 230, 18);
                    BrightNeutralColor = new(144, 130, 10);
                    DropdownArrowColor = Color.Black;
                    TitleShadowIntensity = .72f;

                    TextColor = Color.Black;
                    TextBoxFocusedSelectionBG = Color.White;
                }
                else if (ThemeType == BuiltInTheme.Dark_Turquoise)
                {
                    PrimaryColor = new(60, 210, 192);
                    BrightNeutralColor = new(4, 66, 52);
                    DropdownArrowColor = Color.Black;
                    TitleShadowIntensity = 0.25f;
                }
                else if (ThemeType == BuiltInTheme.Dark_OrangeBrown)
                {
                    PrimaryColor = new(236, 150, 12);
                    BrightNeutralColor = new(62, 42, 12);
                    DropdownArrowColor = Color.Black;
                    TitleShadowIntensity = 0.70f;
                }
                else if (ThemeType == BuiltInTheme.Dark_LightBrown)
                {
                    PrimaryColor = new(228, 178, 130);
                    BrightNeutralColor = new(106, 84, 64);
                    DropdownArrowColor = Color.Black;
                    TitleShadowIntensity = 0.7f;
                }
                else
                    throw new NotImplementedException();

                Color DimNeutralColor = new(0, 10, 18);
                Color AccentColor = Color.Lerp(Color.Black, PrimaryColor, 0.55f);
                Color ToggleButtonUnselected = AccentColor;
                Color TitleBG = Color.Lerp(Color.Black, PrimaryColor, TitleShadowIntensity);
                Color ScrollBarBorderColor = Color.Lerp(BrightNeutralColor, Color.White, 0.06f);

                MGSolidFillBrush PrimaryBG = PrimaryColor.AsFillBrush();
                IFillBrush BrightNeutralBrush = BrightNeutralColor.AsFillBrush();
                IFillBrush AccentBrush = AccentColor.AsFillBrush();
                IFillBrush DimNeutralBackground = DimNeutralColor.AsFillBrush();
                MGSolidFillBrush SpoilerUnpsoiledBG = MGSolidFillBrush.SemiBlack;

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

                //  GroupBox/ListBox/ListView/ProgressBar/TabControl/ChatBox
                ThemeManagedVisualStateFillBrush BrightNeutralBG =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            BrightNeutralBrush,
                            null, PressedModifierType.Darken, 0.06f)
                    );
                _Backgrounds[MGElementType.GroupBox] = new ThemeManagedVisualStateFillBrush(BrightNeutralBG.GetValue(true));
                _Backgrounds[MGElementType.ListBox] = new ThemeManagedVisualStateFillBrush(BrightNeutralBG.GetValue(true));
                _Backgrounds[MGElementType.ListView] = new ThemeManagedVisualStateFillBrush(BrightNeutralBG.GetValue(true));
                _Backgrounds[MGElementType.ProgressBar] = new ThemeManagedVisualStateFillBrush(BrightNeutralBG.GetValue(true));
                _Backgrounds[MGElementType.TabControl] = new ThemeManagedVisualStateFillBrush(BrightNeutralBG.GetValue(true));
                _Backgrounds[MGElementType.ChatBox] = new ThemeManagedVisualStateFillBrush(BrightNeutralBG.GetValue(true));

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
                            ToggleButtonUnselected.AsFillBrush(), PrimaryBG, ToggleButtonUnselected.AsFillBrush() * 0.5f,
                            Color.White * 0.12f, PressedModifierType.Darken, 0.06f)
                    );
                _Backgrounds[MGElementType.ToggleButton] = new ThemeManagedVisualStateFillBrush(ToggleButtonBG.GetValue(true));

                //  ToolTip
                ThemeManagedVisualStateFillBrush ToolTipBG =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            (ToolTipBGColor * 0.85f).AsFillBrush(),
                            null, PressedModifierType.Darken, 0.06f)
                    );
                _Backgrounds[MGElementType.ToolTip] = new ThemeManagedVisualStateFillBrush(ToolTipBG.GetValue(true));
                Color ToolTipForegroundColor = new Color(240, 240, 240);
                ToolTipTextForeground = new VisualStateSetting<Color?>(ToolTipForegroundColor, ToolTipForegroundColor, ToolTipForegroundColor);

                //  Window
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
                            null, ListItemSelectedColor.AsFillBrush() * 0.65f, null,
                            ListItemHoveredColor * 0.4f, PressedModifierType.Darken, 0.06f)
                    );

                this.DropdownArrowColor = DropdownArrowColor;

                this.ListBoxItemBackground =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            null, ListItemSelectedColor.AsFillBrush() * 0.65f, null,
                            ListItemHoveredColor * 0.18f, PressedModifierType.Darken, 0.04f)
                    );
                this.ListBoxItemAlternatingRowBackgrounds = new List<ThemeManagedFillBrush>()
                {
                    new ThemeManagedFillBrush(BrightNeutralColor.Darken(0.04f).AsFillBrush()),
                    new ThemeManagedFillBrush(BrightNeutralColor.Brighten(0.04f).AsFillBrush())
                };

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

                this.CheckMarkColor = CheckMarkColor;
                this.RadioButtonBubbleBackground =
                    new ThemeManagedVisualStateColorBrush(
                        new VisualStateColorBrush(
                            PrimaryColor,
                            Color.White * 0.12f, PressedModifierType.Darken, 0.06f)
                    );
                this.RadioButtonCheckedFillColor = CheckMarkColor;

                this.ResizeGripForeground =
                    new ThemeManagedVisualStateColorBrush(
                        new VisualStateColorBrush(
                            PrimaryColor, Color.White * 0.36f, PressedModifierType.Brighten, 0.2f)
                    );

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
                            SpoilerUnpsoiledBG,
                            Color.White * 0.15f, PressedModifierType.Darken, 0.06f)
                    );

                this.TextBoxFocusedSelectionForeground = TextColor;
                this.TextBoxFocusedSelectionBackground = TextBoxFocusedSelectionBG;
                this.TextBoxUnfocusedSelectionForeground = TextColor;
                this.TextBoxUnfocusedSelectionBackground = TextBoxUnfocusedSelectionBG * 0.4f;

                this.TitleBackground =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            new MGSolidFillBrush(TitleBG),
                            Color.White * 0.05f, PressedModifierType.Darken, 0.06f)
                    );

                this.TextBlockFallbackForeground =
                    new ThemeManagedVisualStateColorBrush(
                        new VisualStateColorBrush(TextColor)
                    );
            }
            else if (ThemeType == BuiltInTheme.Light_Gray)
            {
                Color PrimaryColor = Color.LightGray;
                MGSolidFillBrush PrimaryBG = new(PrimaryColor);
                Color BrightNeutralColor = Color.White;
                IFillBrush BrightNeutralBrush = BrightNeutralColor.AsFillBrush();

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
                _Backgrounds[MGElementType.ListBox] = new ThemeManagedVisualStateFillBrush(BrightNeutralBG.GetValue(true));
                _Backgrounds[MGElementType.ListView] = new ThemeManagedVisualStateFillBrush(BrightNeutralBG.GetValue(true));
                _Backgrounds[MGElementType.ProgressBar] = new ThemeManagedVisualStateFillBrush(BrightNeutralBG.GetValue(true));
                _Backgrounds[MGElementType.TabControl] = new ThemeManagedVisualStateFillBrush(BrightNeutralBG.GetValue(true));
                _Backgrounds[MGElementType.ChatBox] = new ThemeManagedVisualStateFillBrush(BrightNeutralBG.GetValue(true));

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
                            (new Color(255, 255, 210) * 0.8f).AsFillBrush(),
                            null, PressedModifierType.Darken, 0.06f)
                    );
                _Backgrounds[MGElementType.ToolTip] = new ThemeManagedVisualStateFillBrush(ToolTipBG.GetValue(true));
                Color ToolTipForegroundColor = new Color(24, 24, 24);
                ToolTipTextForeground = new VisualStateSetting<Color?>(ToolTipForegroundColor, ToolTipForegroundColor, ToolTipForegroundColor);

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

                this.ListBoxItemBackground =
                    new ThemeManagedVisualStateFillBrush(
                        new VisualStateFillBrush(
                            null, Color.Yellow.AsFillBrush() * 0.65f, null,
                            Color.LightBlue * 0.4f, PressedModifierType.Darken, 0.04f)
                    );
                this.ListBoxItemAlternatingRowBackgrounds = new List<ThemeManagedFillBrush>()
                {
                    new ThemeManagedFillBrush(BrightNeutralColor.Darken(0.04f).AsFillBrush()),
                    new ThemeManagedFillBrush(BrightNeutralColor.Brighten(0.04f).AsFillBrush())
                };

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

                this.CheckMarkColor = new(0, 128, 0);
                this.RadioButtonBubbleBackground =
                    new ThemeManagedVisualStateColorBrush(
                        new VisualStateColorBrush(
                            PrimaryColor,
                            Color.White * 0.18f, PressedModifierType.Darken, 0.06f)
                    );
                this.RadioButtonCheckedFillColor = CheckMarkColor;

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
