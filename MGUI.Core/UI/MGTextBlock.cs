﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MGUI.Shared.Helpers;
using MGUI.Core.UI.Text;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoGame.Extended;
using MGUI.Shared.Text;
using MGUI.Shared.Rendering;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Shared.Input.Mouse;

namespace MGUI.Core.UI
{
    public class MGTextBlock : MGElement, ITextMeasurer
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _FontFamily;
        /// <summary>To set this value, use <see cref="TrySetFont(string, int)"/></summary>
        public string FontFamily { get => _FontFamily; set => _ = TrySetFont(value, FontSize); }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _FontSize;
        /// <summary>To set this value, use <see cref="TrySetFont(string, int)"/> or <see cref="TrySetFontSize(int)"/></summary>
        public int FontSize { get => _FontSize; set => _ = TrySetFontSize(value); }

        internal float FontScale { get; private set; }
        internal Vector2 FontOrigin { get; private set; }
        internal int FontHeight { get; private set; }
        internal float SpaceWidth { get; private set; }

        internal SpriteFont SF_Regular { get; private set; }
        internal SpriteFont SF_Bold { get; private set; }
        internal SpriteFont SF_Italic { get; private set; }
        internal SpriteFont SF_BoldItalic { get; private set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Dictionary<char, SpriteFont.Glyph> SF_Regular_Glyphs { get; set; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Dictionary<char, SpriteFont.Glyph> SF_Bold_Glyphs { get; set; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Dictionary<char, SpriteFont.Glyph> SF_Italic_Glyphs { get; set; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Dictionary<char, SpriteFont.Glyph> SF_BoldItalic_Glyphs { get; set; }

        internal SpriteFont GetFont(bool IsBold, bool IsItalic, out Dictionary<char, SpriteFont.Glyph> Glyphs)
        {
            if (!IsBold && !IsItalic)
            {
                Glyphs = SF_Regular_Glyphs;
                return SF_Regular;
            }
            else if (IsBold && IsItalic)
            {
                Glyphs = SF_BoldItalic_Glyphs;
                return SF_BoldItalic;
            }
            else if (IsBold)
            {
                Glyphs = SF_Bold_Glyphs;
                return SF_Bold;
            }
            else
            {
                Glyphs = SF_Italic_Glyphs;
                return SF_Italic;
            }
        }

        public bool TrySetFontSize(int FontSize) => TrySetFont(FontFamily, FontSize);
        public bool TrySetFont(string FontFamily, int FontSize)
        {
            if (this.FontFamily != FontFamily || this.FontSize != FontSize)
            {
                string PreviousFontFamily = FontFamily;
                int PreviousFontSize = FontSize;

                if (!GetDesktop().FontManager.TryGetFont(FontFamily, CustomFontStyles.Normal, FontSize, true, out FontSet FS, out SpriteFont Font, out int Size, out float ExactScale, out float SuggestedScale))
                    return false;

                this._FontFamily = FontFamily;
                this._FontSize = FontSize;

                this.SF_Regular = Font;
                this.FontScale = GetTheme().FontSettings.UseExactScale ? ExactScale : SuggestedScale;
                this.FontOrigin = FS.Origins[Size];
                this.FontHeight = FS.Heights[Size];
                this.SpaceWidth = (SF_Regular.MeasureString(" ") * FontScale).X;

                SF_Bold = GetFontStyleOrDefault(FS, CustomFontStyles.Bold, Size, SF_Regular);
                SF_Italic = GetFontStyleOrDefault(FS, CustomFontStyles.Italic, Size, SF_Regular);
                SF_BoldItalic = GetFontStyleOrDefault(FS, CustomFontStyles.Bold | CustomFontStyles.Italic, Size, SF_Bold ?? SF_Italic ?? SF_Regular);

                SF_Regular_Glyphs = SF_Regular?.GetGlyphs();
                SF_Bold_Glyphs = SF_Bold?.GetGlyphs();
                SF_Italic_Glyphs = SF_Italic?.GetGlyphs();
                SF_BoldItalic_Glyphs = SF_BoldItalic?.GetGlyphs();

                InvokeLayoutChanged();

                if (PreviousFontFamily != this.FontFamily)
                    NPC(nameof(FontFamily));
                if (PreviousFontSize != this.FontSize)
                    NPC(nameof(FontSize));

                return true;
            }
            else
                return true;
        }

        private static SpriteFont GetFontStyleOrDefault(FontSet FS, CustomFontStyles Style, int Size, SpriteFont Default)
        {
            if (FS.TryGetFont(Style, Size, false, out SpriteFont Result, out _, out _, out _))
                return Result;
            else
                return Default;
        }

        #region Font Style
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _IsBold;
        public bool IsBold
        {
            get => _IsBold;
            set
            {
                if (_IsBold != value)
                {
                    _IsBold = value;
                    if (!string.IsNullOrEmpty(Text))
                    {
                        UpdateRuns();
                        InvokeLayoutChanged();
                    }
                    NPC(nameof(IsBold));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _IsItalic;
        public bool IsItalic
        {
            get => _IsItalic;
            set
            {
                if (_IsItalic != value)
                {
                    _IsItalic = value;
                    if (!string.IsNullOrEmpty(Text))
                    {
                        UpdateRuns();
                        InvokeLayoutChanged();
                    }
                    NPC(nameof(IsItalic));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _IsUnderlined;
        public bool IsUnderlined
        {
            get => _IsUnderlined;
            set
            {
                if (_IsUnderlined != value)
                {
                    _IsUnderlined = value;
                    if (!string.IsNullOrEmpty(Text))
                    {
                        UpdateRuns();
                        UpdateLines();
                    }
                    NPC(nameof(IsUnderlined));
                }
            }
        }

        private MGTextRunUnderlineConfig DefaultUnderlineSettings => new MGTextRunUnderlineConfig(IsUnderlined);
        private MGTextRunShadowConfig DefaultShadowSettings => 
            IsShadowed ? new MGTextRunShadowConfig(ShadowColor ?? GetTheme().FontSettings.DefaultFontShadowColor, ShadowOffset?.ToVector2() ?? GetTheme().FontSettings.DefaultFontShadowOffset.ToVector2()) : default;

        private MGTextRunConfig DefaultTextRunSettings => new(IsBold, IsItalic, 1, null, DefaultUnderlineSettings, default, DefaultShadowSettings);
        #endregion Font Style

        #region Shadow
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _IsShadowed;
        public bool IsShadowed
        {
            get => _IsShadowed;
            set
            {
                if (_IsShadowed != value)
                {
                    _IsShadowed = value;
                    if (!string.IsNullOrEmpty(Text))
                    {
                        UpdateRuns();
                        UpdateLines();
                    }
                    NPC(nameof(IsShadowed));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Point? _ShadowOffset;
        /// <summary>Only relevant if <see cref="IsShadowed"/> is true.<br/>
        /// Determines the offset applied to <see cref="Text"/> when drawing the shadow.<br/>
        /// If null, uses <see cref="ThemeFontSettings.DefaultFontShadowOffset"/> from <see cref="MGTheme.FontSettings"/><para/>
        /// Warning - shadowed text does not affect the layout bounds of this <see cref="MGTextBlock"/>.<br/>
        /// Using a large <see cref="ShadowOffset"/> value may result in parts of the shadow being clipped to <see cref="MGElement.ActualLayoutBounds"/>.</summary>
        public Point? ShadowOffset
        {
            get => _ShadowOffset;
            set
            {
                if (_ShadowOffset != value)
                {
                    _ShadowOffset = value;
                    if (!string.IsNullOrEmpty(Text))
                    {
                        UpdateRuns();
                        UpdateLines();
                    }
                    NPC(nameof(ShadowOffset));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Color? _ShadowColor;
        /// <summary>Only relevant if <see cref="IsShadowed"/> is true.<br/>
        /// If null, uses <see cref="ThemeFontSettings.DefaultFontShadowColor"/> from <see cref="MGTheme.FontSettings"/></summary>
        public Color? ShadowColor
        {
            get => _ShadowColor;
            set
            {
                if (_ShadowColor != value)
                {
                    _ShadowColor = value;
                    if (!string.IsNullOrEmpty(Text))
                    {
                        UpdateRuns();
                        UpdateLines();
                    }
                    NPC(nameof(ShadowColor));
                }
            }
        }
        #endregion Shadow

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private VisualStateSetting<Color?> _Foreground;
        /// <summary>The foreground color to use when rendering the text.<br/>
        /// If the text is formatted with color codes (such as '[color=Red]Hello World[/color]'), the color specified in the <see cref="MGTextRun"/> will take precedence.<para/>
        /// If the value for the current <see cref="MGElement.VisualState"/> is null, will attempt to resolve the value from <see cref="MGElement.DerivedDefaultTextForeground"/>, or <see cref="MGTheme.TextBlockFallbackForeground"/> if no value is specified.<para/>
        /// See also:<br/><see cref="MGElement.DefaultTextForeground"/><br/><see cref="MGElement.DerivedDefaultTextForeground"/><br/><see cref="ActualForeground"/><br/>
        /// <see cref="MGTheme.TextBlockFallbackForeground"/><br/><see cref="MGWindow.Theme"/><br/><see cref="MGDesktop.Theme"/></summary>
        public VisualStateSetting<Color?> Foreground
        {
            get => _Foreground;
            set
            {
                if (_Foreground != value)
                {
                    _Foreground = value;
                    NPC(nameof(Foreground));
                    NPC(nameof(ActualForeground));
                }
            }
        }

        public Color ActualForeground => Foreground.GetValue(VisualState.Primary) ?? DerivedDefaultTextForeground ?? GetTheme().TextBlockFallbackForeground.GetValue(false).GetValue(VisualState.Primary);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _AllowsInlineFormatting = true;
        /// <summary>If true, <see cref="Text"/> can contain formatting codes such as "[bold]...[/bold]" or "[color=green]...[/color]" etc.<br/>
        /// If false, all formatting codes within <see cref="Text"/> will be treated as literal strings instead of affecting how the text is rendered.<para/>
        /// Default value: true</summary>
        public bool AllowsInlineFormatting
        {
            get => _AllowsInlineFormatting;
            set
            {
                if (_AllowsInlineFormatting != value)
                {
                    _AllowsInlineFormatting = value;
                    UpdateRuns();
                    InvokeLayoutChanged();
                    NPC(nameof(AllowsInlineFormatting));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _Text;
        public string Text
        {
            get => _Text;
            set => SetText(value, false);
        }

        /// <param name="SuppressLayoutChanged">If true, <see cref="Text"/> will be set without calling <see cref="InvokeLayoutChanged"/><para/>
        /// Intended to be used for performance purposes when changing the text value, without actually changing the text layout.<br/>
        /// For example, changing text from: "Hello World" to "Hello [bg=Red]World[/bg]" does not affect the rendered text's layout/size.</param>
        public void SetText(string Value, bool SuppressLayoutChanged = false)
        {
            if (_Text != Value)
            {
                _Text = Value;
                UpdateRuns();
                if (!SuppressLayoutChanged)
                    InvokeLayoutChanged();
                else
                    UpdateLines();
                NPC(nameof(Text));
            }
        }

        private bool _IsTrackingMouseClicks;
        private bool IsTrackingMouseClicks
        {
            get => _IsTrackingMouseClicks;
            set
            {
                if (_IsTrackingMouseClicks != value)
                {
                    _IsTrackingMouseClicks = value;

                    if (IsTrackingMouseClicks)
                        MouseHandler.ReleasedInside += Mouse_ReleasedInside;
                    else if (_MouseHandler != null)
                        MouseHandler.ReleasedInside -= Mouse_ReleasedInside;
                }
            }
        }

        private void Mouse_ReleasedInside(object sender, BaseMouseReleasedEventArgs e)
        {
            if (e.IsLMB)
            {
                //  Handle the '[Action]' inlined formatting code
                //  EX: Text="[Action=Action1]Click here[/Action] but not here"
                //  Then clicking the substring "Click here" should invoke the command named "Action1" (in MGResources.Commands)
                if (ActionBounds.Any())
                {
                    Point MousePosition = ConvertCoordinateSpace(CoordinateSpace.Screen, CoordinateSpace.Layout, InputTracker.Mouse.CurrentPosition);

                    foreach (var KVP in ActionBounds)
                    {
                        string CommandName = KVP.Key;
                        if (GetResources().TryGetCommand(CommandName, out Action<MGElement> Command))
                        {
                            foreach (Rectangle Bounds in KVP.Value)
                            {
                                if (Bounds.Contains(MousePosition))
                                {
                                    Command(this);
                                    e.SetHandledBy(this, false);
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void UpdateRuns()
        {
            if (AllowsInlineFormatting)
            {
                bool WasTrackingMouseClicks = IsTrackingMouseClicks;

                MGDesktop Desktop = GetDesktop();

                IEnumerable<MGTextRun> ParsedRuns = MGTextRun.ParseRuns(Text, DefaultTextRunSettings);
                List<MGTextRun> Temp = new();

                //  Sanitize the runs
                foreach (MGTextRun Run in ParsedRuns)
                {
                    MGTextRun CurrentRun = Run;

                    //  If a TextRunImage didn't specify destination dimensions, use the default size of the image
                    if (Run.RunType == TextRunType.Image && Run is MGTextRunImage ImageRun && 
                        ImageRun.TargetWidth <= 0 && ImageRun.TargetHeight <= 0)
                    {
                        (int? DefaultWidth, int? DefaultHeight) = Desktop.Resources.GetTextureDimensions(ImageRun.SourceName);
                        CurrentRun = new MGTextRunImage(ImageRun.SourceName, DefaultWidth ?? 0, DefaultHeight ?? 0, ImageRun.ToolTipId, ImageRun.ActionId);
                    }

                    Temp.Add(CurrentRun);
                }

                Runs = Temp.AsReadOnly();
            }
            else
            {
                List<FTTokenMatch> Tokens = FTTokenizer.TokenizeLineBreaks(Text, true).ToList();
                Runs = MGTextRun.ParseRuns(Tokens, DefaultTextRunSettings).ToList().AsReadOnly();
            }

            IsTrackingMouseClicks = AllowsInlineFormatting && Runs.Any(x => x.HasAction);

            NPC(nameof(Runs));
        }

        public ReadOnlyCollection<MGTextRun> Runs { get; private set; }
        public ReadOnlyCollection<MGTextLine> Lines { get; private set; }

        internal void UpdateLines()
        {
            this.Lines = MGTextLine.ParseLines(this, LayoutBounds.Width - Padding.Width, WrapText, Runs, IgnoreEmptySpaceLines).ToList().AsReadOnly();
            NPC(nameof(Lines));
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _WrapText;
        public bool WrapText
        {
            get => _WrapText;
            set
            {
                if (_WrapText != value)
                {
                    _WrapText = value;
                    InvokeLayoutChanged();
                    NPC(nameof(WrapText));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private float _LinePadding;
        /// <summary>Additional vertical space between each line of text</summary>
        public float LinePadding
        {
            get => _LinePadding;
            set
            {
                if (_LinePadding != value)
                {
                    _LinePadding = value;
                    InvokeLayoutChanged();
                    NPC(nameof(LinePadding));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _MinLines;
        /// <summary>The minimum # of lines to display, regardless of how many lines the actual text content requires.<para/>
        /// Default value: 0<para/>
        /// See also: <see cref="MaxLines"/></summary>
        public int MinLines
        {
            get => _MinLines;
            set
            {
                if (_MinLines != value)
                {
                    _MinLines = value;
                    LayoutChanged(this, true);
                    NPC(nameof(MinLines));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int? _MaxLines;
        /// <summary>The maximum # of lines to display, regardless of how many lines the actual text content requires.<br/>
        /// Use null to indicate there is no maximum.<para/>
        /// Default value: null<para/>
        /// See also: <see cref="MinLines"/></summary>
        public int? MaxLines
        {
            get => _MaxLines;
            set
            {
                if (_MaxLines != value)
                {
                    _MaxLines = value;
                    LayoutChanged(this, true);
                    NPC(nameof(MaxLines));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private HorizontalAlignment _TextAlignment;
        public HorizontalAlignment TextAlignment
        {
            get => _TextAlignment;
            set
            {
                if (_TextAlignment != value)
                {
                    _TextAlignment = value;
                    NPC(nameof(TextAlignment));
                }
            }
        }

        /// <param name="FontSize">If null, uses the font size specified by <see cref="ThemeFontSettings.DefaultFontSize"/>.<para/>
        /// See also:<br/><see cref="MGWindow.Theme"/><br/><see cref="MGDesktop.Theme"/><br/><see cref="MGTheme.FontSettings"/></param>
        public MGTextBlock(MGWindow Window, string Text, Color? Foreground = null, int? FontSize = null, bool AllowsInlineFormatting = true)
            : base(Window, MGElementType.TextBlock)
        {
            using (BeginInitializing())
            {
                this.WrapText = true;

                MGDesktop Desktop = GetDesktop();
                MGTheme Theme = GetTheme();
                if (!TrySetFont(Theme.FontSettings.DefaultFontFamily ?? Desktop.FontManager.DefaultFontFamily, FontSize ?? GetTheme().FontSettings.DefaultFontSize))
                    throw new ArgumentException($"Default font not found.");

                this.AllowsInlineFormatting = AllowsInlineFormatting;
                this.IsBold = false;
                this.IsItalic = false;
                this.IsUnderlined = false;
                this.Text = Text;
                this.MinLines = 0;
                this.MaxLines = null;
                this.Foreground = new VisualStateSetting<Color?>(Foreground, Foreground, Foreground);
                this.LinePadding = 2;
                this.TextAlignment = HorizontalAlignment.Left;
                this.Padding = new(1,2,1,1);
                this.VerticalContentAlignment = VerticalAlignment.Center;

                OnLayoutUpdated += (sender, e) => { UpdateLines(); };
            }
        }

        public override string ToString() => $"{base.ToString()}: \"{Text?.Truncate(100)}\"";

        public Vector2 MeasureText(string Text, bool IsBold, bool IsItalic, bool IgnoreFirstGlyphNegativeLeftSideBearing)
        {
            if (string.IsNullOrEmpty(Text))
                return Vector2.Zero;

            SpriteFont SF = GetFont(IsBold, IsItalic, out Dictionary<char, SpriteFont.Glyph> Glyphs);

#if true
            //  Referenced SpriteFont.MeasureString source code from:
            //  https://github.com/MonoGame/MonoGame/blob/develop/MonoGame.Framework/Graphics/SpriteFont.cs

            float Width = 0;
            float Height = SF.LineSpacing;

            float CurrentXOffset = 0;
            bool IsFirstCharacter = true;

            if (SF.Spacing != 0)
                throw new NotImplementedException($"{nameof(MGTextBlock)}.{nameof(MeasureText)} currently does not handle {nameof(SpriteFont)}s with a non-zero Spacing.");

            foreach (char c in Text)
            {
                if (!Glyphs.TryGetValue(c, out SpriteFont.Glyph Glyph))
                    Glyph = Glyphs[SF.DefaultCharacter.Value];

#if NEVER
                if (IsFirstCharacter)
                    IsFirstCharacter = false;
                else
                    CurrentXOffset += SF.Spacing;

                if (IgnoreFirstGlyphNegativeLeftSideBearing)
                {
                    CurrentXOffset += Math.Max(Glyph.LeftSideBearing, 0);
                    IgnoreFirstGlyphNegativeLeftSideBearing = false;
                }
                else
                    CurrentXOffset += Glyph.LeftSideBearing;

                CurrentXOffset += Glyph.Width;
                Width = Math.Max(Width, CurrentXOffset + Math.Max(Glyph.RightSideBearing, 0));
                CurrentXOffset += Glyph.RightSideBearing;
                Height = Math.Max(Height, Glyph.Cropping.Height);
#else
                //TODO figure out why MonoGame's SpriteFont.cs uses Math.Max when adding the left/right bearings, then fix this logic accordingly
                CurrentXOffset += Glyph.WidthIncludingBearings;
                Width += Glyph.WidthIncludingBearings;
                Height = Math.Max(Height, Glyph.Cropping.Height);
#endif
            }

            return new Vector2(Width, FontHeight) * FontScale;
#else
            //  The problem with this logic is that it doesn't always result in the same total width when measuring a string versus measuring the sum of its substrings
            //  EX: [Measure("HelloWorld").Width] isn't always equivalent to [Measure("Hello").Width + Measure("World").Width]
            //  Which can cause a ton of issues in MGTextLine.ParseRuns(...)
            return new Vector2(SF.MeasureString(Text).X, FontHeight) * FontScale;
#endif
        }

        private readonly List<ElementMeasurement> RecentSelfMeasurements = new();
        private void CacheSelfMeasurement(ElementMeasurement Value)
        {
            while (RecentSelfMeasurements.Count > MeasurementCacheSize)
                RecentSelfMeasurements.RemoveAt(RecentSelfMeasurements.Count - 1);
            if (!RecentSelfMeasurements.Contains(Value))
                RecentSelfMeasurements.Insert(0, Value);
        }

        private bool TryGetCachedSelfMeasurement(Size AvailableSize, out Thickness? Result)
        {
            foreach (ElementMeasurement Measurement in RecentSelfMeasurements)
            {
                //  Not sure what logic is appropriate for this
                //  The thought process is, if we measured this textblock already with a larger available size,
                //      we should be able to re-use the measurement as long as the new available size is still >= whatever was previously requested.
                bool IsMatch = false;
                if (Measurement.AvailableSize == AvailableSize)
                    IsMatch = true;
                else if (Measurement.AvailableSize.Width == AvailableSize.Width)
                    IsMatch = AvailableSize.Height >= Measurement.RequestedSize.Height;
                else if (Measurement.IsAvailableSizeGreaterThanOrEqual(AvailableSize))
                    IsMatch = Measurement.IsRequestedSizeLessThanOrEqual(AvailableSize);

                if (IsMatch)
                {
                    Result = Measurement.RequestedSize;
                    return true;
                }
            }

            Result = null;
            return false;
        }

        private void InvokeLayoutChanged()
        {
            RecentSelfMeasurements.Clear();
            LayoutChanged(this, true);
        }

        /// <summary>If true, lines that consist of only a single whitespace character will be ignored when rendering wrapped text content.</summary>
        private const bool IgnoreEmptySpaceLines = true;

        public override Thickness MeasureSelfOverride(Size AvailableSize, out Thickness SharedSize)
        {
            Size PaddedSize = AvailableSize.Subtract(PaddingSize, 0, 0);

            Size RemainingSize = new(
                Math.Max(0, PreferredWidth.HasValue ? PreferredWidth.Value - HorizontalPadding : PaddedSize.Width),
                Math.Max(0, PreferredHeight.HasValue ? PreferredHeight.Value - VerticalPadding : PaddedSize.Height)
            );

            SharedSize = new(0);
            if (TryGetCachedSelfMeasurement(RemainingSize, out Thickness? CachedMeasurement))
                return CachedMeasurement.Value;

            List<MGTextLine> Lines = MGTextLine.ParseLines(this, RemainingSize.Width, WrapText, Runs, IgnoreEmptySpaceLines).ToList();
            List<MGTextLine> MeasuredLines = Lines;
            if (MaxLines.HasValue && MeasuredLines.Count > MaxLines.Value)
                MeasuredLines = MeasuredLines.Take(MaxLines.Value).ToList();

            Vector2 Size = new(MeasuredLines.Select(x => x.LineWidth).DefaultIfEmpty(0).Max(), MeasuredLines.Sum(x => x.LineTotalHeight) + LinePadding * Math.Max(0, MeasuredLines.Count - 1));
            if (MinLines > MeasuredLines.Count)
                Size = Size.SetY(Size.Y + (MinLines - MeasuredLines.Count) * (FontHeight + LinePadding));

            Thickness Measurement = new((int)Math.Ceiling(Size.X), (int)Math.Ceiling(Size.Y), 0, 0);

            ElementMeasurement SelfMeasurement = new(RemainingSize, Measurement, SharedSize, new(0));
            CacheSelfMeasurement(SelfMeasurement);
            return Measurement;
        }

        /// <summary>
        /// Key = the name of a named delegate to invoke when clicking within any of the given bounds.<br/>
        /// Value = the bounds of the text content that the named action corresponds to. Usually a list of 1 rectangle, but might be multiple if the bound text spans multiple lines.
        /// </summary>
        private readonly Dictionary<string, List<Rectangle>> ActionBounds = new();

        /// <summary>
        /// Key = the name of a ToolTip to reference,<br/>
        /// Value = the bounds of the text content that the tooltip is applied to. Usually a list of 1 rectangle, but might be multiple if the text spans multiple lines.
        /// </summary>
        private readonly Dictionary<string, List<Rectangle>> ToolTipBounds = new();

        protected override bool TryGetToolTip(out MGToolTip ToolTip)
        {
            if (ToolTipBounds.Any())
            {
                try
                {
                    Point MousePosition = ConvertCoordinateSpace(CoordinateSpace.Screen, CoordinateSpace.Layout, InputTracker.Mouse.CurrentPosition);

                    foreach (var KVP in ToolTipBounds)
                    {
                        string ToolTipName = KVP.Key;
                        if (ParentWindow.NamedToolTips.TryGetValue(ToolTipName, out ToolTip))
                        {
                            foreach (Rectangle Bounds in KVP.Value)
                            {
                                if (Bounds.Contains(MousePosition))
                                    return true;
                            }
                        }
                    }
                }
                finally { ToolTipBounds.Clear(); }
            }

            ToolTip = null;
            return false;
        }

        public override void UpdateSelf(ElementUpdateArgs UA)
        {
            base.UpdateSelf(UA);
            if (ActionBounds.Any())
                ActionBounds.Clear();
        }

#if false //true
        public override void DrawSelf(ElementDrawArgs DA, Rectangle LayoutBounds)
        {
            MGDesktop Desktop = GetDesktop();
            DrawTransaction DT = DA.DT;
            float Opacity = DA.Opacity;
            Color DefaultForeground = this.ActualForeground;

            float CurrentY = LayoutBounds.Top + Padding.Top;

            foreach (MGTextLine Line in this.Lines)
            {
                Rectangle LineBounds = new(LayoutBounds.Left + Padding.Left, (int)CurrentY, LayoutBounds.Width - PaddingSize.Width, (int)Line.LineTotalHeight);
                float CurrentX = ApplyAlignment(LineBounds, TextAlignment, VerticalContentAlignment, new Size((int)Line.LineWidth, (int)Line.LineTotalHeight)).Left;
                float TextYPosition = ApplyAlignment(LineBounds, TextAlignment, VerticalContentAlignment, new Size((int)Line.LineWidth, (int)Line.LineTextHeight)).Y;

                bool IsStartOfLine = true;

                foreach (MGTextRun Run in Line.Runs)
                {
                    if (Run.RunType == TextRunType.Image && Run is MGTextRunImage ImageRun)
                    {
                        int ImgWidth = ImageRun.TargetWidth;
                        int ImgHeight = ImageRun.TargetHeight;
                        int YPosition = ApplyAlignment(LineBounds, HorizontalAlignment.Center, VerticalContentAlignment, new Size(ImgWidth, ImgHeight)).Top;
                        Desktop.Resources.TryDrawTexture(DT, ImageRun.RegionName, new Point((int)CurrentX, YPosition), ImgWidth, ImgHeight);
                        CurrentX += ImgWidth;
                    }
                    else if (Run.RunType == TextRunType.Text && Run is MGTextRunText TextRun)
                    {
                        bool IsBold = TextRun.Settings.IsBold;
                        bool IsItalic = TextRun.Settings.IsItalic;
                        SpriteFont SF = GetFont(IsBold, IsItalic, out _);

                        float ActualOpacity = Opacity * TextRun.Settings.Opacity;
                        Color Foreground = (TextRun.Settings.Foreground ?? DefaultForeground) * ActualOpacity;

                        Vector2 TextSize = MeasureText(TextRun.Text, IsBold, IsItalic, IsStartOfLine);
                        TextRun.Settings.Background?.Draw(DA.SetOpacity(ActualOpacity), this, new((int)CurrentX, (int)TextYPosition, (int)TextSize.X, (int)Line.LineTextHeight));

                        if (TextRun.Settings.IsUnderlined)
                        {
                            DT.FillRectangle(DA.Offset.ToVector2(), new(CurrentX, TextYPosition + Line.LineTextHeight - 2, TextSize.X, 1), Foreground);
                        }

                        Vector2 Position = new Vector2(CurrentX, TextYPosition) + DA.Offset.ToVector2();
                        if (TextRun.Settings.IsShadowed)
                        {
                            Color ShadowColor = (TextRun.Settings.ShadowColor ?? DefaultForeground) * ActualOpacity;
                            Vector2 ShadowOffset = TextRun.Settings.ShadowOffset ?? new(1, 1);

                            DT.DrawSpriteFontText(SF, TextRun.Text, Position + ShadowOffset, ShadowColor, FontOrigin, FontScale, FontScale);
                            DT.DrawSpriteFontText(SF, TextRun.Text, Position, Foreground, FontOrigin, FontScale, FontScale);
                        }
                        else
                        {
                            DT.DrawSpriteFontText(SF, TextRun.Text, Position, Foreground, FontOrigin, FontScale, FontScale);
                        }

                        CurrentX += TextSize.X;
                        IsStartOfLine = false;
                    }
                    else
                        throw new NotImplementedException($"{nameof(MGTextBlock)}.{nameof(DrawSelf)} does not support rendering {nameof(MGTextRun)}s of type={nameof(TextRunType)}.{Run.RunType}");
                }

                CurrentY += Line.LineTotalHeight + LinePadding;
            }
        }
#else
        public override void DrawSelf(ElementDrawArgs DA, Rectangle LayoutBounds)
        {
            float WindowScale = ParentWindow.Scale;
            MGDesktop Desktop = GetDesktop();
            FontManager FontManager = Desktop.FontManager;
            DrawTransaction DT = DA.DT;
            float Opacity = DA.Opacity;
            Color DefaultForeground = this.ActualForeground;
            Vector2 FontOrigin = this.FontOrigin;

            Matrix Transform = Matrix.CreateTranslation(new Vector3(DA.Offset.ToVector2(), 0));
            IDisposable TemporaryDrawTransform = null;
            bool UseScaledSpriteFont = false;
            float ImageSizeScalar = 1.0f;
#if NEVER
            //  This logic *almost* works but not quite :( Seems like there's minor issues with the measured text width, so the X-positioning is slightly inaccurate
            //  Maybe issues due to truncating floating point positions to an int?
            //  Maybe issues related to Kerning? 
            //      EX: SpriteFont1 has FontSize=24
            //          SpriteFont2 has FontSize=12
            //          Are the bearings in SpriteFont1's Kerning always exactly twice that of SpriteFont2? Check SpriteFont.Glyphs[char].LeftSideBearing and RightSideBearing/Width
            //  If you re-enable this logic, try typing "iiiiiiiiiiiiiiiiiiiiiiiiiii" inside a TextBox and you'll notice the Caret positioning becomes less and less corect with each successive character
            if (ParentWindow.IsWindowScaled)
            {
                //  Experimental logic that attempts to handle MGWindow.Scale by using a larger SpriteFont rather than scaling the current, smaller SpriteFont.
                //  This probably only works correctly if the FontSet for the current FontFamily contains a SpriteFont that exactly matches the scale
                //      EX: If using 12pt font, and MGWindow.Scale=1.5f, we need an 18pt font. Using something like a 16pt font with 1.125 scale will likely yield poor results
                float ExactScaledFontSize = this.FontSize * ParentWindow.Scale;
                if (ExactScaledFontSize == (int)ExactScaledFontSize && FontManager.FontsByFamily[FontFamily].SupportedSizes.Contains((int)ExactScaledFontSize))
                {
                    TemporaryDrawTransform = DA.DT.SetTransformTemporary(Matrix.Identity); // Remove the current transform since we'll calculate the scaled positions ourself
                    Transform = GetTransform(CoordinateSpace.Layout, CoordinateSpace.Screen) *
                        Matrix.CreateTranslation(new Vector3((DA.Offset + Origin).ToVector2() * WindowScale, 0));
                    UseScaledSpriteFont = true;
                    ImageSizeScalar = WindowScale;
                    FontOrigin = this.FontOrigin / WindowScale;
                }
            }
#endif

            ActionBounds.Clear();
            ToolTipBounds.Clear();

            float CurrentY = LayoutBounds.Top + Padding.Top;

            foreach (MGTextLine Line in this.Lines)
            {
                Rectangle LineBounds = new(LayoutBounds.Left + Padding.Left, (int)CurrentY, LayoutBounds.Width - PaddingSize.Width, (int)Line.LineTotalHeight);
                float CurrentX = ApplyAlignment(LineBounds, TextAlignment, VerticalContentAlignment, new Size((int)Line.LineWidth, (int)Line.LineTotalHeight)).Left;
                float TextYPosition = ApplyAlignment(LineBounds, TextAlignment, VerticalContentAlignment, new Size((int)Line.LineWidth, (int)Line.LineTextHeight)).Y;

                bool IsStartOfLine = true;

                foreach (MGTextRun Run in Line.Runs)
                {
                    RectangleF RunBounds; // The bounds of the MGTextRun, in CoordinateSpace.Layout space

                    if (Run.RunType == TextRunType.Image && Run is MGTextRunImage ImageRun)
                    {
                        int ImgWidth = ImageRun.TargetWidth;
                        int ImgHeight = ImageRun.TargetHeight;
                        int YPosition = ApplyAlignment(LineBounds, HorizontalAlignment.Center, VerticalContentAlignment, new Size(ImgWidth, ImgHeight)).Top;
                        Point Position = new Vector2((int)CurrentX, YPosition).TransformBy(Transform).ToPoint();
                        Desktop.Resources.TryDrawTexture(DT, ImageRun.SourceName, Position, (int)(ImgWidth * ImageSizeScalar), (int)(ImgHeight * ImageSizeScalar), DA.Opacity);
                        RunBounds = new(CurrentX, YPosition, ImgWidth, ImgHeight);
                        CurrentX += ImgWidth;
                    }
                    else if (Run.RunType == TextRunType.Text && Run is MGTextRunText TextRun)
                    {
                        bool IsBold = TextRun.Settings.IsBold;
                        bool IsItalic = TextRun.Settings.IsItalic;
                        SpriteFont SF = GetFont(IsBold, IsItalic, out _);

                        float FontScale = this.FontScale;
                        if (UseScaledSpriteFont)
                        {
                            CustomFontStyles FontStyle = 
                                IsBold && IsItalic ? CustomFontStyles.Bold | CustomFontStyles.Italic : 
                                IsBold ? CustomFontStyles.Bold : 
                                IsItalic ? CustomFontStyles.Italic : 
                                CustomFontStyles.Normal;
                            SF = Desktop.FontManager.GetFont(FontFamily, FontStyle,(int)(FontSize * FontScale * WindowScale), true, out _, out float ExactScale, out float SuggestedScale, out _);
                            FontScale = ExactScale;
                        }

                        float ActualOpacity = Opacity * TextRun.Settings.Opacity;
                        Color Foreground = (TextRun.Settings.Foreground ?? DefaultForeground) * ActualOpacity;

                        Vector2 TextSize = MeasureText(TextRun.Text, IsBold, IsItalic, IsStartOfLine);

                        //  Draw background
                        if (TextRun.Settings.HasBackground)
                        {
                            IFillBrush BackgroundBrush = TextRun.Settings.Background.Brush;
                            Thickness BackgroundPadding = TextRun.Settings.Background.Padding;

                            if (UseScaledSpriteFont)
                            {
                                Rectangle BackgroundDestination = new Rectangle((int)CurrentX, (int)TextYPosition, (int)TextSize.X, (int)Line.LineTextHeight)
                                    .GetExpanded(BackgroundPadding)
                                    .CreateTransformed(Transform);
                                BackgroundBrush.Draw(DA.SetOpacity(ActualOpacity).AsZeroOffset(), this, BackgroundDestination);
                            }
                            else
                            {
                                Rectangle BackgroundDestination = new Rectangle((int)CurrentX, (int)TextYPosition, (int)TextSize.X, (int)Line.LineTextHeight)
                                    .GetExpanded(BackgroundPadding);
                                BackgroundBrush.Draw(DA.SetOpacity(ActualOpacity), this, BackgroundDestination);
                            }
                        }

                        //  Draw underline
                        MGTextRunUnderlineConfig UnderlineSettings = TextRun.Settings.Underline;
                        if (TextRun.Settings.Underline.IsEnabled)
                        {
                            int UnderlineHeight = UnderlineSettings.Height;
                            int UnderlineYOffset = UnderlineSettings.VerticalOffset;
                            IFillBrush UnderlineBrush = TextRun.Settings.Underline.Brush ?? Foreground.AsFillBrush();

                            RectangleF Destination = new RectangleF(CurrentX, TextYPosition + Line.LineTextHeight - 2 + UnderlineYOffset, TextSize.X, UnderlineHeight);
                                //.CreateTransformedF(Transform); // IFillBrush.Draw will already account for ElementDrawArgs.Offset
                            UnderlineBrush.Draw(DA.SetOpacity(ActualOpacity), this, Destination.RoundUp());
                            //DT.FillRectangle(Vector2.Zero, Destination, Foreground);
                        }

                        Vector2 Position = new Vector2(CurrentX, TextYPosition).TransformBy(Transform);
                        if (TextRun.Settings.IsShadowed)
                        {
                            //  Draw text twice, once for the shadow, then again for itself
                            Color ShadowColor = (TextRun.Settings.Shadow.ShadowColor ?? DefaultForeground) * ActualOpacity;
                            Vector2 ShadowOffset = TextRun.Settings.Shadow.ShadowOffset ?? new(1, 1);

                            DT.DrawSpriteFontText(SF, TextRun.Text, Position + ShadowOffset, ShadowColor, FontOrigin, FontScale, FontScale);
                            DT.DrawSpriteFontText(SF, TextRun.Text, Position, Foreground, FontOrigin, FontScale, FontScale);
                        }
                        else
                        {
                            DT.DrawSpriteFontText(SF, TextRun.Text, Position, Foreground, FontOrigin, FontScale, FontScale);
                        }

                        RunBounds = new(CurrentX, TextYPosition, TextSize.X, TextSize.Y);
                        CurrentX += TextSize.X;
                        IsStartOfLine = false;
                    }
                    else
                        throw new NotImplementedException($"{nameof(MGTextBlock)}.{nameof(DrawSelf)} does not support rendering {nameof(MGTextRun)}s of type={nameof(TextRunType)}.{Run.RunType}");

                    //  Keep track of which parts of the textblock content have their own tooltip or delegate to invoke when clicking in the bounds
                    if (Run.HasToolTip || Run.HasAction)
                    {
                        Rectangle RoundedRunBounds = RunBounds.RoundUp();

                        if (Run.HasToolTip)
                        {
                            string ToolTipName = Run.ToolTipId;
                            if (!ToolTipBounds.TryGetValue(ToolTipName, out List<Rectangle> Bounds))
                            {
                                Bounds = new();
                                ToolTipBounds.Add(ToolTipName, Bounds);
                            }

                            Bounds.Add(RoundedRunBounds);
                        }

                        if (Run.HasAction)
                        {
                            string ActionName = Run.ActionId;
                            if (!ActionBounds.TryGetValue(ActionName, out List<Rectangle> Bounds))
                            {
                                Bounds = new();
                                ActionBounds.Add(ActionName, Bounds);
                            }

                            Bounds.Add(RoundedRunBounds);
                        }
                    }
                }

                CurrentY += Line.LineTotalHeight + LinePadding;
            }

            TemporaryDrawTransform?.Dispose();
        }
#endif
    }
}
