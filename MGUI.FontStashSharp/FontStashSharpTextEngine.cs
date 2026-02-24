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
        /// Maps logical pt size → the <em>effective</em> pt size that SpriteFontTextEngine
        /// would use for the same logical size (i.e. baked atlas size × suggested scale).
        /// <c>null</c> when <see cref="MatchSpriteFontSizing"/> has not been called.
        /// </summary>
        private Dictionary<int, float>? _calibratedEffectivePt;

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
        /// Builds a per-size calibration table so that every logical font size used by
        /// MGUI produces the same glyph advances (and therefore the same text widths) as
        /// <see cref="SpriteFontTextEngine"/>.
        /// <para/>
        /// Call this after <see cref="AddFontSystem(string, CustomFontStyles, FontSystem, byte[])"/>
        /// has set <see cref="FontSizeScale"/>.  You only need to call it once; the table
        /// remains valid as long as <paramref name="fontManager"/>'s content does not change.
        /// <para/>
        /// <b>Why this is necessary:</b> <see cref="SpriteFontTextEngine"/> renders every
        /// logical pt size from a discrete baked atlas (e.g. 36 pt atlas at 1/3 scale for
        /// sizes 11–12).  The effective pt size is therefore
        /// <c>bakedSize × suggestedScale</c> (e.g. 12 for logical size 11), not the
        /// nominal logical size.  Without this correction FontStashSharp would measure
        /// text at <c>ptSize × FontSizeScale</c> (e.g. 16.4 px) instead of
        /// <c>effectivePt × FontSizeScale</c> (e.g. 17.9 px), producing advance widths
        /// that differ by up to ~10 %, which causes visible misalignment or truncation.
        /// </summary>
        /// <param name="fontManager">
        /// The <see cref="FontManager"/> that owns the SpriteFonts to match.
        /// Typically <c>desktop.FontManager</c>.
        /// </param>
        public void MatchSpriteFontSizing(FontManager fontManager)
        {
            if (fontManager is null) throw new ArgumentNullException(nameof(fontManager));

            string family = fontManager.DefaultFontFamily;
            var table = new Dictionary<int, float>();

            for (int ptSize = 1; ptSize <= 96; ptSize++)
            {
                if (fontManager.TryGetFont(family, CustomFontStyles.Normal, ptSize,
                        true,
                        out _, out _, out int bakedSize, out _, out float suggestedScale))
                {
                    // effectivePt is the actual size SpriteFontTextEngine renders at
                    // for this logical ptSize.  FSS must use the same reference size.
                    table[ptSize] = bakedSize * suggestedScale;
                }
            }

            _calibratedEffectivePt = table;
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
            // SpriteFont calibration table (bakedSize × suggestedScale) rather than the
            // raw logical size.  This ensures advance widths match SpriteFontTextEngine
            // across all font sizes despite the quantised baked-atlas scheme it uses.
            float effectivePt = _calibratedEffectivePt != null
                && _calibratedEffectivePt.TryGetValue(spec.Size, out float cal)
                ? cal
                : (float)spec.Size;
            SpriteFontBase spriteFontBase = fs.GetFont(effectivePt * FontSizeScale);
            var handle = new FSSFontHandle(spriteFontBase);

            var resolved = new ResolvedFont(
                spec,
                actualSize:      spec.Size,          // FSS resolves fractional sizes exactly
                exactScale:      1f,
                suggestedScale:  1f,
                lineHeight:      handle.LineHeight,
                spaceWidth:      handle.SpaceWidth,
                drawOrigin:      Vector2.Zero,        // FSS uses top-left origin by default
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
        public Vector2 MeasureText(ResolvedFont font, string text)
        {
            if (string.IsNullOrEmpty(text))
                return Vector2.Zero;

            var h = GetHandle(font);
            if (h is null) return Vector2.Zero;

            var size = h.Font.MeasureString(text);
            // Use the consistent line height rather than the string's own Y
            return new Vector2(size.X, h.LineHeight);
        }

        /// <inheritdoc/>
        public GlyphMetrics MeasureGlyph(ResolvedFont font, char c)
        {
            var h = GetHandle(font);
            if (h is null)
                return new GlyphMetrics(0, 0, 0, font.LineHeight);

            // FSS doesn't expose per-glyph bearings; report the full advance as GlyphWidth
            // (LeftSideBearing = RightSideBearing = 0, so TotalWidth == TotalWidthFirstGlyph).
            // Width is cached per-char on the handle to avoid repeated string allocations.
            return new GlyphMetrics(0f, h.GetGlyphWidth(c), 0f, h.LineHeight);
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
