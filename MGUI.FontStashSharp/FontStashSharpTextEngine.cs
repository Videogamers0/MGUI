using System;
using System.Collections.Generic;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MGUI.Shared.Text;
using MGUI.Shared.Text.Engines;

namespace MGUI.FontStashSharp
{
    /// <summary>
    /// <see cref="ITextEngine"/> implementation backed by
    /// <a href="https://github.com/FontStashSharp/FontStashSharp">FontStashSharp</a>.
    /// <para/>
    /// Usage:
    /// <code>
    /// byte[] ttfData = File.ReadAllBytes("Arial.ttf");
    /// var fontSystem = new FontSystem();
    /// fontSystem.AddFont(ttfData);
    ///
    /// var engine = new FontStashSharpTextEngine();
    /// engine.AddFontSystem("Arial", CustomFontStyles.Normal, fontSystem, ttfData);
    ///
    /// desktop.TextEngine = engine;
    /// </code>
    /// </summary>
    public sealed class FontStashSharpTextEngine : ITextEngine
    {
        // ── Private helpers ──────────────────────────────────────────────────────

        private sealed class FSSFontHandle
        {
            public SpriteFontBase Font       { get; }
            public float          LineHeight { get; }
            public float          SpaceWidth { get; }

            // Cache: char → measured advance width.  Populated lazily on first request so that
            // TextRenderInfo.UpdateLines (called per-char per layout pass) avoids repeated
            // single-character string allocations and MeasureString calls.
            private readonly Dictionary<char, float> _glyphWidths = new();

            public FSSFontHandle(SpriteFontBase font)
            {
                Font = font;
                // Use the native FSS metric (ascender + descender in pixels at the
                // rasterized size).  Avoids the previous MeasureString("|") hack which
                // returned values influenced by internal leading.
                LineHeight = font.LineHeight;
                SpaceWidth = font.MeasureString(" ").X;
            }

            /// <summary>
            /// Returns the advance width of <paramref name="c"/>, allocating a single-char
            /// string and calling <see cref="SpriteFontBase.MeasureString"/> only the first
            /// time a given character is requested; subsequent calls return the cached value.
            /// </summary>
            public float GetGlyphWidth(char c)
            {
                if (_glyphWidths.TryGetValue(c, out float w))
                    return w;
                float measured = Font.MeasureString(c.ToString()).X;
                _glyphWidths[c] = measured;
                return measured;
            }
        }

        // ── Fields ───────────────────────────────────────────────────────────────

        /// <summary>Dictionary of FontSystem per (family, style).</summary>
        private readonly Dictionary<(string Family, CustomFontStyles Style), FontSystem> _fontSystems = new();

        /// <summary>Fallback FontSystem used when a family+style combination is not registered.</summary>
        private FontSystem? _fallbackFontSystem;

        /// <summary>Cache: FontSpec → ResolvedFont (+ embedded FSSFontHandle as NativeFont).</summary>
        private readonly Dictionary<FontSpec, ResolvedFont> _cache = new();

        /// <summary>
        /// Per font-size effective-pt table built by <see cref="MatchSpriteFontSizing"/>.
        /// Maps logical pt size → the effective pt size used for FSS rasterisation
        /// (<c>bakedSize × exactScale == ptSize</c>, so this equals the raw pt size).
        /// <c>null</c> when <see cref="MatchSpriteFontSizing"/> has not been called.
        /// </summary>
        private Dictionary<int, float>? _calibratedEffectivePt;

        /// <summary>
        /// Per font-size line height (px) matching SpriteFontTextEngine's tight glyph-crop
        /// metric (<c>Heights[bakedSize] × exactScale</c>).  Populated by
        /// <see cref="MatchSpriteFontSizing"/>.
        /// </summary>
        private Dictionary<int, float>? _calibratedLineHeight;

        /// <summary>
        /// Per font-size calibrated space-character width (px) matching SpriteFontTextEngine's
        /// <c>SF.MeasureString(" ") × exactScale</c>.  Populated by
        /// <see cref="MatchSpriteFontSizing"/>.
        /// </summary>
        private Dictionary<int, float>? _calibratedSpaceWidth;

        /// <summary>
        /// Per font-size draw origin (in pixels, already scaled for FSS drawScale=1)
        /// matching SpriteFontTextEngine's top-glyph-crop offset.<br/>
        /// The Y component equals <c>sfMinCroppingY × sfSuggestedScale</c> so that the
        /// on-screen vertical shift is identical to what SpriteFontTextEngine produces
        /// when it renders with <c>drawScale = SuggestedScale</c>.  Populated by
        /// <see cref="MatchSpriteFontSizing"/>.
        /// </summary>
        private Dictionary<int, Vector2>? _calibratedDrawOrigin;

        /// <summary>
        /// Per (style, size), per-char glyph metrics copied directly from the SpriteFont atlas
        /// at ExactScale.  Keyed by <c>(CustomFontStyles, ptSize)</c> so that Bold/Italic
        /// calibrations use their own atlas data instead of Normal style data.
        /// Characters not present in the SF atlas fall back to FSS’s own measurement.
        /// Populated by <see cref="MatchSpriteFontSizing"/>.
        /// </summary>
        private Dictionary<(CustomFontStyles, int), Dictionary<char, GlyphMetrics>>? _calibratedGlyphMetrics;

        /// <summary>
        /// Per (style, size) inter-glyph spacing (px, at ExactScale) copied from
        /// <c>SpriteFont.Spacing</c>.  Populated by <see cref="MatchSpriteFontSizing"/>.
        /// </summary>
        private Dictionary<(CustomFontStyles, int), float>? _calibratedSpacing;

        /// <summary>
        /// Per (style, size) fallback character mirroring <c>SpriteFont.DefaultCharacter</c>.
        /// Populated by <see cref="MatchSpriteFontSizing"/>.
        /// </summary>
        private Dictionary<(CustomFontStyles, int), char>? _calibratedDefaultChar;

        /// <summary>
        /// Per (style, size) SpriteFont + ExactScale pair used at <see cref="ResolveFont"/>
        /// time to calibrate the FSS pixel size via a multi-character reference string.
        /// The calibrated FSS font then produces advance widths that match SF, giving
        /// identical wrap points and no text clipping.
        /// Populated by <see cref="MatchSpriteFontSizing"/>.
        /// </summary>
        private Dictionary<(CustomFontStyles, int), (SpriteFont SF, float ExactScale)>? _calibratedSpriteFont;

        /// <summary>
        /// Reference string used to compute the FSS pixel-size correction ratio at
        /// <see cref="ResolveFont"/> time.  Must be the same string in both
        /// <see cref="MatchSpriteFontSizing"/> and <see cref="ResolveFont"/>.
        /// Covers a wide variety of character pairs to get an accurate average correction.
        /// </summary>
        private const string CalibrationString =
            "AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz" +
            "0123456789 ,.:;!?\"'()-";

        /// <summary>
        /// Scale factor applied to MGUI's logical <c>FontSize</c> (in points) before
        /// passing it to <see cref="FontSystem.GetFont"/> (which expects pixels).
        /// <para/>
        /// The correct value depends on the loaded font and equals
        /// <c>(96/72) × (hhea.ascender − hhea.descender) / head.unitsPerEm</c>.
        /// This accounts for two things:
        /// <list type="bullet">
        ///   <item>The DPI conversion: MonoGame’s Content Pipeline rasterises
        ///         <c>.spritefont</c> sizes at 96 DPI → <c>1 pt = 96/72 px</c>.</item>
        ///   <item>The sizing-model gap: the Content Pipeline sizes by <b>em square</b>,
        ///         while FontStashSharp (StbTrueType) sizes by <b>ascender − descender</b>.
        ///         For fonts where these differ (e.g. Arial: ratio ≈ 1.117) a pure
        ///         DPI factor (<c>4/3</c>) is not enough.</item>
        /// </list>
        /// Call <see cref="ComputeFontSizeScale(byte[])"/> to obtain the exact value
        /// for a given TTF file, or pass <c>ttfData</c> to the
        /// <see cref="AddFontSystem(string, CustomFontStyles, FontSystem, byte[])"/> overload
        /// to have it set automatically.
        /// <para/>
        /// Changing this value clears the resolved-font cache.
        /// </summary>
        /// <remarks>
        /// <b>Thread safety:</b> this property must only be set from the MonoGame
        /// update/draw thread (the same thread that drives <c>MGDesktop.Update</c> and
        /// <c>MGDesktop.Draw</c>).  It is not safe to assign it from a background thread
        /// while the UI is rendering.<br/>
        /// <b>Mutation timing:</b> set this value (or let <see cref="AddFontSystem"/> /
        /// <see cref="MatchSpriteFontSizing"/> set it automatically) <i>before</i> calling
        /// <see cref="ResolveFont"/> for the first time.  Changing it after fonts have
        /// already been resolved will clear the cache via <see cref="InvalidateCache"/>,
        /// forcing all font handles to be re-resolved on the next layout pass, which may
        /// cause a one-frame layout hiccup.
        /// </remarks>
        public float FontSizeScale
        {
            get => _fontSizeScale;
            set
            {
                if (_fontSizeScale != value)
                {
                    _fontSizeScale = value;
                    InvalidateCache();
                }
            }
        }
        private float _fontSizeScale = 4f / 3f;

        // ── Registration ─────────────────────────────────────────────────────────

        /// <summary>
        /// Registers a <see cref="FontSystem"/> for a specific font family and style variant.
        /// The first registered family/style combination is automatically set as the fallback.
        /// </summary>
        /// <param name="family">Case-sensitive font family name (e.g. "Arial").</param>
        /// <param name="style">The style this <see cref="FontSystem"/> provides.</param>
        /// <param name="fontSystem">Pre-configured <see cref="FontSystem"/> with at least one
        /// TTF font added via <see cref="FontSystem.AddFont(byte[])"/>.</param>
        public void AddFontSystem(string family, CustomFontStyles style, FontSystem fontSystem)
        {
            if (fontSystem is null) throw new ArgumentNullException(nameof(fontSystem));
            _fontSystems[(family, style)] = fontSystem;
            _fallbackFontSystem ??= fontSystem;
            InvalidateCache();
        }

        /// <summary>
        /// Registers a <see cref="FontSystem"/> and automatically computes
        /// <see cref="FontSizeScale"/> from the raw TTF data so that FontStashSharp
        /// renders at the same em-square size as MonoGame’s Content Pipeline.
        /// </summary>
        /// <param name="family">Case-sensitive font family name (e.g. "Arial").</param>
        /// <param name="style">The style this <see cref="FontSystem"/> provides.</param>
        /// <param name="fontSystem">Pre-configured <see cref="FontSystem"/>.</param>
        /// <param name="ttfData">Raw TrueType font bytes (the same array passed to
        /// <see cref="FontSystem.AddFont(byte[])"/>).  Used to read the <c>head</c> and
        /// <c>hhea</c> tables and derive the correct <see cref="FontSizeScale"/>.</param>
        public void AddFontSystem(string family, CustomFontStyles style, FontSystem fontSystem, byte[] ttfData)
        {
            AddFontSystem(family, style, fontSystem);
            if (ttfData != null)
                FontSizeScale = ComputeFontSizeScale(ttfData);
        }

        /// <summary>
        /// Calibrates FSS metrics against <see cref="SpriteFontTextEngine"/> for every
        /// logical font size, so that both engines produce identical layout results:
        /// <list type="bullet">
        ///   <item><description><b>EffectivePt</b> — FSS renders at <c>ptSize × FontSizeScale</c>
        ///     pixels, matching SF's ExactScale-based measurement.</description></item>
        ///   <item><description><b>LineHeight</b> — copied from <c>FontSet.Heights[bakedSize] × exactScale</c>,
        ///     the tight glyph-crop metric used by SpriteFontTextEngine.</description></item>
        ///   <item><description><b>DrawOrigin</b> — vertical offset that shifts text up by the
        ///     same number of screen pixels as SpriteFontTextEngine's crop origin.</description></item>
        /// </list>
        /// <para/>
        /// <b>Note on glyph-width calibration:</b> this method also populates per-glyph
        /// metric tables (<c>_calibratedGlyphMetrics</c>, <c>_calibratedSpacing</c>, etc.)
        /// from the SpriteFont atlas, but <see cref="MeasureText"/> and
        /// <see cref="MeasureGlyph"/> deliberately do <b>not</b> use them.  SpriteFont atlas
        /// glyph widths are integer-precision (rounded at bake time), whereas FSS rendering
        /// uses float-precision StbTrueType advances.  Summing the calibrated integers
        /// produces a width systematically <i>narrower</i> than the FSS DrawText output,
        /// causing text to be clipped.  All width measurements therefore use FSS-native
        /// <see cref="FontStashSharp.SpriteFontBase.MeasureString"/> for measure–draw
        /// consistency.  The calibrated tables are retained for potential future use (e.g.
        /// glyph-atlas debugging) and for <see cref="SpaceWidth"/> reference.
        /// <para/>
        /// Call this after <see cref="AddFontSystem(string, CustomFontStyles, FontSystem, byte[])"/>
        /// has set <see cref="FontSizeScale"/>.  You only need to call it once; the tables
        /// remain valid as long as <paramref name="fontManager"/>'s content does not change.
        /// </summary>
        /// <param name="fontManager">
        /// The <see cref="FontManager"/> that owns the SpriteFonts to match.
        /// Typically <c>desktop.FontManager</c>.
        /// </param>
        public void MatchSpriteFontSizing(FontManager fontManager)
        {
            if (fontManager is null) throw new ArgumentNullException(nameof(fontManager));

            string family = fontManager.DefaultFontFamily;
            var effectivePtTable  = new Dictionary<int, float>();
            var lineHeightTable   = new Dictionary<int, float>();
            var spaceWidthTable   = new Dictionary<int, float>();
            var drawOriginTable   = new Dictionary<int, Vector2>();
            var glyphMetricsTable = new Dictionary<(CustomFontStyles, int), Dictionary<char, GlyphMetrics>>();
            var spacingTable      = new Dictionary<(CustomFontStyles, int), float>();
            var defaultCharTable  = new Dictionary<(CustomFontStyles, int), char>();
            var spriteFontTable   = new Dictionary<(CustomFontStyles, int), (SpriteFont SF, float ExactScale)>();

            // Styles to calibrate glyph metrics for.  LineHeight/SpaceWidth/DrawOrigin use Normal
            // only (FontSet.Heights/Origins are computed across all styles so Normal is representative).
            var stylesToCalibrate = new[]
            {
                CustomFontStyles.Normal,
                CustomFontStyles.Bold,
                CustomFontStyles.Italic,
            };

            for (int ptSize = 1; ptSize <= 96; ptSize++)
            {
                // Per-size metrics from the Normal atlas (same for all styles at a given size).
                if (fontManager.TryGetFont(family, CustomFontStyles.Normal, ptSize,
                        true,
                        out FontSet fs, out SpriteFont sfNormal,
                        out int bakedSize, out float exactScale, out float suggestedScale))
                {
                    // effectivePt: bakedSize × exactScale == ptSize (by definition),
                    // so FSS measures text at exactly the same pt size as SF does.
                    effectivePtTable[ptSize] = bakedSize * exactScale;

                    // LineHeight: tight glyph-crop metric from FontSet.Heights.
                    float lineH = fs.Heights.TryGetValue(bakedSize, out int sfHeight)
                        ? sfHeight * exactScale
                        : bakedSize * exactScale;
                    lineHeightTable[ptSize] = lineH;

                    // SpaceWidth: SF measurement at ExactScale.
                    spaceWidthTable[ptSize] = sfNormal.MeasureString(" ").X * exactScale;

                    // DrawOrigin: on-screen shift = sfOrigin.Y × suggestedScale px.
                    // FSS draws at scale 1.0 so origin must equal that pixel count directly.
                    if (fs.Origins.TryGetValue(bakedSize, out Vector2 sfOrigin))
                        drawOriginTable[ptSize] = new Vector2(sfOrigin.X, sfOrigin.Y * suggestedScale);
                }

                // Per-style glyph metrics, spacing, and default-char.
                foreach (CustomFontStyles style in stylesToCalibrate)
                {
                    if (!fontManager.TryGetFont(family, style, ptSize,
                            true,
                            out _, out SpriteFont sf,
                            out int baked, out float es, out _))
                        continue;

                    float lh = lineHeightTable.TryGetValue(ptSize, out float lhVal)
                        ? lhVal
                        : baked * es;

                    var glyphDict = new Dictionary<char, GlyphMetrics>();
                    foreach (var kv in sf.GetGlyphs())
                    {
                        SpriteFont.Glyph g = kv.Value;
                        glyphDict[kv.Key] = new GlyphMetrics(
                            g.LeftSideBearing  * es,
                            g.Width            * es,
                            g.RightSideBearing * es,
                            lh);
                    }
                    glyphMetricsTable[(style, ptSize)] = glyphDict;
                    spacingTable[(style, ptSize)]      = sf.Spacing * es;
                    if (sf.DefaultCharacter.HasValue)
                        defaultCharTable[(style, ptSize)] = sf.DefaultCharacter.Value;

                    // Store the SpriteFont + exactScale so MeasureText can delegate
                    // whole-string measurement to SF directly, giving identical layout
                    // decisions to SpriteFontTextEngine (same wrap points, no extra lines).
                    spriteFontTable[(style, ptSize)] = (sf, es);
                }
            }

            _calibratedEffectivePt  = effectivePtTable;
            _calibratedLineHeight   = lineHeightTable;
            _calibratedSpaceWidth   = spaceWidthTable;
            _calibratedDrawOrigin   = drawOriginTable;
            _calibratedGlyphMetrics = glyphMetricsTable;
            _calibratedSpacing      = spacingTable;
            _calibratedDefaultChar  = defaultCharTable;
            _calibratedSpriteFont   = spriteFontTable;
            InvalidateCache();
        }

        // ── TTF metrics helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Computes the correct <see cref="FontSizeScale"/> for a given TrueType font
        /// so that FontStashSharp renders glyphs at the same em-square size as
        /// MonoGame’s Content Pipeline (which rasterises <c>.spritefont</c> files at 96 DPI
        /// using em-based sizing).
        /// <para/>
        /// The returned value equals
        /// <c>(96 / 72) × (hhea.ascender − hhea.descender) / head.unitsPerEm</c>.
        /// </summary>
        /// <param name="ttfData">Raw TrueType (.ttf) font file bytes.</param>
        /// <returns>The scale factor to assign to <see cref="FontSizeScale"/>.</returns>
        /// <exception cref="ArgumentException">The data is too short or is missing
        /// the required <c>head</c> / <c>hhea</c> tables.</exception>
        public static float ComputeFontSizeScale(byte[] ttfData)
        {
            if (ttfData == null || ttfData.Length < 12)
                throw new ArgumentException("Invalid TTF data.", nameof(ttfData));

            int numTables = ReadUInt16BE(ttfData, 4);
            int headOffset = -1;
            int hheaOffset = -1;

            for (int i = 0; i < numTables; i++)
            {
                int rec = 12 + i * 16;
                if (rec + 16 > ttfData.Length) break;

                if (TagEquals(ttfData, rec, 'h', 'e', 'a', 'd'))
                    headOffset = (int)ReadUInt32BE(ttfData, rec + 8);
                else if (TagEquals(ttfData, rec, 'h', 'h', 'e', 'a'))
                    hheaOffset = (int)ReadUInt32BE(ttfData, rec + 8);

                if (headOffset >= 0 && hheaOffset >= 0) break;
            }

            if (headOffset < 0 || hheaOffset < 0)
                throw new ArgumentException(
                    "TTF data is missing the required 'head' or 'hhea' table.",
                    nameof(ttfData));

            // head table: unitsPerEm at offset +18 (uint16)
            int unitsPerEm = ReadUInt16BE(ttfData, headOffset + 18);

            // hhea table: ascender at +4 (int16), descender at +6 (int16, negative)
            int ascender   = ReadInt16BE(ttfData, hheaOffset + 4);
            int descender  = ReadInt16BE(ttfData, hheaOffset + 6);

            // StbTrueType’s ScaleForPixelHeight divides by (ascender − descender).
            // The Content Pipeline scales by em square (ptSize × 96/72 / unitsPerEm).
            // To make both produce the same glyph scale:
            //   fssPixelHeight / (ascender − descender) = ptSize × (96/72) / unitsPerEm
            //   fssPixelHeight = ptSize × (96/72) × (ascender − descender) / unitsPerEm
            int ascentDescent = ascender - descender;
            return (96f / 72f) * ascentDescent / (float)unitsPerEm;
        }

        private static bool TagEquals(byte[] data, int offset, char c0, char c1, char c2, char c3)
            => data[offset] == (byte)c0 && data[offset + 1] == (byte)c1
            && data[offset + 2] == (byte)c2 && data[offset + 3] == (byte)c3;

        private static ushort ReadUInt16BE(byte[] data, int offset)
            => (ushort)((data[offset] << 8) | data[offset + 1]);

        private static short ReadInt16BE(byte[] data, int offset)
            => (short)((data[offset] << 8) | data[offset + 1]);

        private static uint ReadUInt32BE(byte[] data, int offset)
            => (uint)((data[offset] << 24) | (data[offset + 1] << 16)
                    | (data[offset + 2] << 8) | data[offset + 3]);

        // ── ITextEngine ──────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public ResolvedFont ResolveFont(FontSpec spec)
        {
            if (_cache.TryGetValue(spec, out var cached))
                return cached;

            bool isFallback = false;
            FontSystem? fs = null;

            // Try exact match
            if (!_fontSystems.TryGetValue((spec.Family, spec.Style), out fs))
            {
                // Try Normal style fallback for the same family
                if (!_fontSystems.TryGetValue((spec.Family, CustomFontStyles.Normal), out fs))
                {
                    fs         = _fallbackFontSystem;
                    isFallback = true;
                }
            }

            if (fs is null)
            {
                // No font registered at all — return a placeholder
                var placeholder = new ResolvedFont(spec, spec.Size, 1f, 1f,
                    spec.Size, 6f, Vector2.Zero, true, null);
                _cache[spec] = placeholder;
                return placeholder;
            }

            // Compute the FSS pixel size.
            // When MatchSpriteFontSizing has been called we use the effective pt from the
            // SpriteFont calibration table (bakedSize × exactScale) rather than the
            // raw logical size.  This ensures the FSS rasterisation pt aligns with SF.
            float effectivePt = _calibratedEffectivePt != null
                && _calibratedEffectivePt.TryGetValue(spec.Size, out float cal)
                ? cal
                : (float)spec.Size;

            float pixelSize = effectivePt * FontSizeScale;

            // MULTI-CHAR PIXEL-SIZE CALIBRATION
            // calSW == rawSW for single spaces (diagnostic confirmed ratio=1.0 for all sizes),
            // but FSS MeasureString for multi-character strings differs from SF * exactScale.
            // Fix: measure a representative calibration string with BOTH SF and FSS at the
            // raw pixel size, scale pixelSize by (sfWidth / fssWidth) so that the corrected
            // FSS font produces advance widths matching SF for all text.  MeasureText then
            // uses FSS-native MeasureString on the corrected font:
            //   • ParseLines uses SF-equivalent widths → same wrap points as default engine
            //   • DrawText renders with the same corrected font → no clipping
            if (_calibratedSpriteFont != null
                && _calibratedSpriteFont.TryGetValue((spec.Style, spec.Size), out var sfEntry))
            {
                float sfCalibWidth  = sfEntry.SF.MeasureString(CalibrationString).X * sfEntry.ExactScale;
                float fssCalibWidth = fs.GetFont(pixelSize).MeasureString(CalibrationString).X;
                if (sfCalibWidth > 0f && fssCalibWidth > 0f)
                {
                    float ratio = sfCalibWidth / fssCalibWidth;
                    System.Diagnostics.Debug.WriteLine(
                        $"[FSS calib] size={spec.Size} style={spec.Style} " +
                        $"sfW={sfCalibWidth:F3} fssW={fssCalibWidth:F3} ratio={ratio:F4} px={pixelSize:F3}->{pixelSize*ratio:F3}");
                    pixelSize *= ratio;
                }
            }

            SpriteFontBase spriteFontBase = fs.GetFont(pixelSize);
            var handle = new FSSFontHandle(spriteFontBase);

            // Use calibrated LineHeight when available so vertical layout is identical to
            // SpriteFontTextEngine (tight glyph-crop metric vs. FSS's full line-height).
            float lineHeight = _calibratedLineHeight != null
                && _calibratedLineHeight.TryGetValue(spec.Size, out float calLH)
                ? calLH
                : handle.LineHeight;

            // SpaceWidth comes from the calibrated-size FSS font, which now matches SF's
            // exactScale measurement — consistent with MeasureText and MeasureGlyph.
            float spaceWidth = handle.SpaceWidth;

            Vector2 drawOrigin = _calibratedDrawOrigin != null
                && _calibratedDrawOrigin.TryGetValue(spec.Size, out Vector2 calDO)
                ? calDO
                : Vector2.Zero;

            var resolved = new ResolvedFont(
                spec,
                actualSize:      spec.Size,          // FSS resolves fractional sizes exactly
                exactScale:      1f,
                suggestedScale:  1f,
                lineHeight:      lineHeight,
                spaceWidth:      spaceWidth,
                drawOrigin:      drawOrigin,
                isFallback:      isFallback,
                nativeFont:      handle);

            _cache[spec] = resolved;
            return resolved;
        }

        // ── Private helper ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the <see cref="FSSFontHandle"/> for <paramref name="font"/>.
        /// If the <see cref="ResolvedFont"/> was created by a different engine (e.g. after
        /// pressing F1 to toggle backends), the font is transparently re-resolved using its
        /// <see cref="ResolvedFont.Spec"/> so the caller always gets a valid FSS handle.
        /// </summary>
        private FSSFontHandle? GetHandle(ResolvedFont font)
        {
            if (font.NativeFont is FSSFontHandle h)
                return h;
            // Stale handle from another engine — re-resolve via spec.
            return ResolveFont(font.Spec).NativeFont as FSSFontHandle;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// <b>Why FSS-native width instead of calibrated SpriteFont glyph sum:</b><br/>
        /// The calibrated path previously summed <c>SpriteFont</c> atlas glyph metrics
        /// (integer pixel widths × <c>ExactScale</c>).  However, <see cref="DrawText"/>
        /// renders via FSS at <c>effectivePt × FontSizeScale</c> using StbTrueType's
        /// float-precision glyph advances.  Because the SpriteFont atlas stores integer
        /// widths (each rounded individually), the calibrated sum is <b>systematically
        /// narrower</b> than the actual FSS rendering.  Over a line of 30+ characters
        /// the cumulative difference can reach several pixels, causing the layout engine
        /// to allocate too little horizontal space and the text to be clipped.<br/>
        /// <br/>
        /// The FSS font resolved by <see cref="ResolveFont"/> has its pixel size corrected
        /// via <see cref="CalibrationString"/> so that FSS advance widths match
        /// <c>SF.MeasureString * exactScale</c>.  MeasureText therefore uses FSS-native
        /// <c>MeasureString</c> directly — consistent with <see cref="DrawText"/> which
        /// renders with the same corrected font — giving identical wrap points to
        /// <see cref="SpriteFontTextEngine"/> and no text clipping.
        /// Calibrated <c>LineHeight</c> is kept for the Y component.
        /// </remarks>
        public Vector2 MeasureText(ResolvedFont font, string text)
        {
            if (string.IsNullOrEmpty(text))
                return Vector2.Zero;

            // Use FSS-native whole-string measurement on the pixel-size-corrected font.
            // The correction (applied in ResolveFont) scales the FSS pixel size so that
            // MeasureString results match SF * exactScale, ensuring:
            //   • DrawText renders at the same width as MeasureText → no clipping
            //   • ParseLines wraps at the same points as SpriteFontTextEngine
            var h = GetHandle(font);
            if (h is null) return Vector2.Zero;
            return new Vector2(h.Font.MeasureString(text).X, font.LineHeight);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// <b>Consistency with <see cref="MeasureText"/>:</b> this method always returns
        /// FSS-native glyph widths (via <see cref="FSSFontHandle.GetGlyphWidth"/>), not
        /// the calibrated SpriteFont atlas values.  <c>TextRenderInfo</c> reconciles the
        /// per-glyph sum against <see cref="MeasureText"/> (whole-string), so both must
        /// come from the same measurement source to avoid caret-positioning drift.
        /// </remarks>
        public GlyphMetrics MeasureGlyph(ResolvedFont font, char c)
        {
            // Always use FSS-native per-glyph measurement so that caret positions
            // (computed in TextRenderInfo) match the FSS-rendered glyph positions.
            // FSS doesn't expose per-glyph bearings; report the full advance as GlyphWidth
            // (LeftSideBearing = RightSideBearing = 0, so TotalWidth == TotalWidthFirstGlyph).
            var h = GetHandle(font);
            if (h is null)
                return new GlyphMetrics(0, 0, 0, font.LineHeight);

            return new GlyphMetrics(0f, h.GetGlyphWidth(c), 0f, font.LineHeight);
        }

        /// <inheritdoc/>
        public float GetLineHeight(ResolvedFont font) => font.LineHeight;

        /// <inheritdoc/>
        public float GetSpaceWidth(ResolvedFont font) => font.SpaceWidth;

        /// <inheritdoc/>
        public void DrawText(
            SpriteBatch spriteBatch,
            ResolvedFont font,
            string text,
            Vector2 position,
            Color color,
            Vector2 origin,
            float scale,
            float rotation    = 0f,
            float depth       = 0f,
            SpriteEffects effects = SpriteEffects.None)
        {
            if (string.IsNullOrEmpty(text))
                return;

            var h = GetHandle(font);
            if (h is null) return;

            // FontStashSharp's DrawText does not accept SpriteEffects directly.
            // Simulate flipping by negating the scale axes and adjusting the position,
            // which is equivalent to what SpriteBatch does for sprite effects.
            float sx = scale;
            float sy = scale;
            if ((effects & SpriteEffects.FlipHorizontally) != 0) sx = -sx;
            if ((effects & SpriteEffects.FlipVertically)   != 0) sy = -sy;

            h.Font.DrawText(spriteBatch, text, position, color,
                rotation:   rotation,
                origin:     origin,
                scale:      new Vector2(sx, sy),
                layerDepth: depth);
        }

        /// <inheritdoc/>
        public void InvalidateCache() => _cache.Clear();
    }
}
