using Microsoft.Xna.Framework;
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

namespace MGUI.Core.UI
{
    public class MGTextBlock : MGElement, ITextMeasurer
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _FontFamily;
        /// <summary>To set this value, use <see cref="TrySetFont(string, int)"/></summary>
        public string FontFamily { get => _FontFamily; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _FontSize;
        /// <summary>To set this value, use <see cref="TrySetFont(string, int)"/></summary>
        public int FontSize { get => _FontSize; }

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

        private SpriteFont GetFont(bool IsBold, bool IsItalic, out Dictionary<char, SpriteFont.Glyph> Glyphs)
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

        public bool TrySetFont(string FontFamily, int FontSize)
        {
            if (this.FontFamily != FontFamily || this.FontSize != FontSize)
            {
                if (!GetDesktop().FontManager.TryGetFont(FontFamily, CustomFontStyles.Normal, FontSize, true, out FontSet FS, out SpriteFont Font, out int Size, out float ExactScale, out float SuggestedScale))
                    return false;

                this._FontFamily = FontFamily;
                this._FontSize = FontSize;

                this.SF_Regular = Font;
                this.FontScale = GetTheme().FontSettings.UseExactScale ? ExactScale : SuggestedScale;
                this.FontOrigin = FS.Origins[Size];
                this.FontHeight = FS.Heights[Size];
                this.SpaceWidth = (SF_Regular.MeasureString(" ") * FontScale).X;

                SF_Bold = GetFontStyleOrDefault(FS, CustomFontStyles.Bold, Size, null);
                SF_Italic = GetFontStyleOrDefault(FS, CustomFontStyles.Italic, Size, null);
                SF_BoldItalic = GetFontStyleOrDefault(FS, CustomFontStyles.Bold | CustomFontStyles.Italic, Size, null);

                SF_Regular_Glyphs = SF_Regular?.GetGlyphs();
                SF_Bold_Glyphs = SF_Bold?.GetGlyphs();
                SF_Italic_Glyphs = SF_Italic?.GetGlyphs();
                SF_BoldItalic_Glyphs = SF_BoldItalic?.GetGlyphs();

                InvokeLayoutChanged();

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
                }
            }
        }

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
                }
            }
        }

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
                }
            }
        }

        private MGTextRunConfig DefaultTextRunSettings => new(IsBold, IsItalic, IsUnderlined);
        #endregion Font Style

        /// <summary>The foreground color to use when rendering the text.<br/>
        /// If the text is formatted with color codes (such as '[color=Red]Hello World[/color]'), the color specified in the <see cref="MGTextRun"/> will take precedence.<para/>
        /// If the value for the current <see cref="MGElement.VisualState"/> is null, will attempt to resolve the value from <see cref="MGElement.DerivedDefaultTextForeground"/>, or <see cref="MGTheme.TextBlockFallbackForeground"/> if no value is specified.<para/>
        /// See also:<br/><see cref="MGElement.DefaultTextForeground"/><br/><see cref="MGElement.DerivedDefaultTextForeground"/><br/><see cref="ActualForeground"/><br/>
        /// <see cref="MGTheme.TextBlockFallbackForeground"/><br/><see cref="MGDesktop.Theme"/></summary>
        public VisualStateSetting<Color?> Foreground { get; set; }

        public Color ActualForeground => Foreground.GetValue(VisualState.Primary) ?? DerivedDefaultTextForeground ?? GetTheme().TextBlockFallbackForeground.GetValue(false).GetValue(VisualState.Primary);

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
            }
        }

        private void UpdateRuns()
        {
            if (AllowsInlineFormatting)
                Runs = MGTextRun.ParseRuns(Text, DefaultTextRunSettings).ToList().AsReadOnly();
            else
            {
                List<FTTokenMatch> Tokens = FTTokenizer.TokenizeLineBreaks(Text, true).ToList();
                Runs = MGTextRun.ParseRuns(Tokens, DefaultTextRunSettings).ToList().AsReadOnly();
            }
        }

        public ReadOnlyCollection<MGTextRun> Runs { get; private set; }
        public ReadOnlyCollection<MGTextLine> Lines { get; private set; }

        internal void UpdateLines()
        {
            this.Lines = MGTextLine.ParseLines(this, LayoutBounds.Width - Padding.Width, WrapText, Runs).ToList().AsReadOnly();
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
                }
            }
        }

        public HorizontalAlignment TextAlignment { get; set; }

        /// <param name="FontSize">If null, uses the font size specified by <see cref="ThemeFontSettings.DefaultFontSize"/>.<para/>
        /// See also: <see cref="MGDesktop.Theme"/>, <see cref="MGTheme.FontSettings"/></param>
        public MGTextBlock(MGWindow Window, string Text, Color? Foreground = null, int? FontSize = null)
            : base(Window, MGElementType.TextBlock)
        {
            using (BeginInitializing())
            {
                this.WrapText = true;

                MGDesktop Desktop = GetDesktop();
                if (!TrySetFont(Desktop.Theme.FontSettings.DefaultFontFamily ?? Desktop.FontManager.DefaultFontFamily, FontSize ?? GetTheme().FontSettings.DefaultFontSize))
                    throw new ArgumentException($"Default font not found.");

                this.IsBold = false;
                this.IsItalic = false;
                this.IsUnderlined = false;
                this.Text = Text;
                this.Foreground = new VisualStateSetting<Color?>(Foreground, Foreground, Foreground);
                this.LinePadding = 2;
                this.TextAlignment = HorizontalAlignment.Left;
                this.Padding = new(1,2,1,1);

                OnLayoutUpdated += (sender, e) => { UpdateLines(); };
            }
        }

        public override string ToString() => $"{base.ToString()}: \"{Text?.Truncate(100)}\"";

        public Vector2 MeasureText(string Text, bool IsBold, bool IsItalic, bool IgnoreFirstGlyphNegativeLeftSideBearing)
        {
            if (string.IsNullOrEmpty(Text))
                return Vector2.Zero;

            SpriteFont SF = GetFont(IsBold, IsItalic, out Dictionary<char, SpriteFont.Glyph> Glyphs);

#if false
            //  Referenced SpriteFont.MeasureString source code from:
            //  https://github.com/MonoGame/MonoGame/blob/develop/MonoGame.Framework/Graphics/SpriteFont.cs

            float Width = 0;
            float Height = SF.LineSpacing;

            float CurrentXOffset = 0;
            bool IsFirstCharacter = true;

            foreach (char c in Text)
            {
                SpriteFont.Glyph Glyph = Glyphs[c];

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

                float ProposedWidth = CurrentXOffset + Math.Max(Glyph.RightSideBearing, 0);
                if (ProposedWidth > Width)
                    Width = ProposedWidth;

                CurrentXOffset += Glyph.RightSideBearing;

                if (Glyph.Cropping.Height > Height)
                    Height = Glyph.Cropping.Height;
            }

            return new Vector2(Width, FontHeight) * FontScale;
#else
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

            List<MGTextLine> Lines = MGTextLine.ParseLines(this, RemainingSize.Width, WrapText, Runs).ToList();
            Vector2 Size = new(Lines.Select(x => x.LineSize.Width).DefaultIfEmpty(0).Max(), Lines.Sum(x => x.LineSize.Height) + LinePadding * (Lines.Count - 1));
            Thickness Measurement = new((int)Math.Ceiling(Size.X), (int)Math.Ceiling(Size.Y), 0, 0);

            ElementMeasurement SelfMeasurement = new(RemainingSize, Measurement, SharedSize, new(0));
            CacheSelfMeasurement(SelfMeasurement);
            return Measurement;
        }

        public override void DrawSelf(ElementDrawArgs DA, Rectangle LayoutBounds)
        {
            DrawTransaction DT = DA.DT;
            float Opacity = DA.Opacity;
            Color DefaultForeground = this.ActualForeground;

            float CurrentY = LayoutBounds.Top + Padding.Top;

            foreach (MGTextLine Line in this.Lines)
            {
                Rectangle LineBounds = new(LayoutBounds.Left + Padding.Left, (int)CurrentY, LayoutBounds.Width - PaddingSize.Width, (int)Line.LineSize.Height);
                float CurrentX = ApplyAlignment(LineBounds, TextAlignment, VerticalAlignment.Center, new Size((int)Line.LineSize.Width, (int)Line.LineSize.Height)).Left;

                bool IsStartOfLine = true;

                foreach (MGTextRun Run in Line.Runs)
                {
                    bool IsBold = Run.Settings.IsBold;
                    bool IsItalic = Run.Settings.IsItalic;
                    SpriteFont SF = GetFont(IsBold, IsItalic, out _);

                    float ActualOpacity = Opacity * Run.Settings.Opacity;
                    Color Foreground = (Run.Settings.Foreground ?? DefaultForeground) * ActualOpacity;

                    Vector2 TextSize = MeasureText(Run.Text, IsBold, IsItalic, IsStartOfLine);
                    Run.Settings.Background?.Draw(DA.SetOpacity(ActualOpacity), this, new((int)CurrentX, (int)CurrentY, (int)TextSize.X, (int)Line.LineSize.Height));

                    if (Run.Settings.IsUnderlined)
                    {
                        DT.FillRectangle(DA.Offset.ToVector2(), new(CurrentX, CurrentY + Line.LineSize.Height - 2, TextSize.X, 1), Foreground);
                    }

                    Vector2 Position = new Vector2(CurrentX, CurrentY) + DA.Offset.ToVector2();
                    if (Run.Settings.IsShadowed)
                    {
                        Color ShadowColor = (Run.Settings.ShadowColor ?? DefaultForeground) * ActualOpacity;
                        Vector2 ShadowOffset = Run.Settings.ShadowOffset ?? new(1, 1);

                        DT.DrawSpriteFontText(SF, Run.Text, Position + ShadowOffset, ShadowColor, FontOrigin, FontScale, FontScale);
                        DT.DrawSpriteFontText(SF, Run.Text, Position, Foreground, FontOrigin, FontScale, FontScale);
                    }
                    else
                    {
                        DT.DrawSpriteFontText(SF, Run.Text, Position, Foreground, FontOrigin, FontScale, FontScale);
                    }

                    CurrentX += TextSize.X;
                    IsStartOfLine = false;
                }

                CurrentY += Line.LineSize.Height + LinePadding;
            }
        }
    }
}
