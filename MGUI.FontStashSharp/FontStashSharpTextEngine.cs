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
    /// var fontSystem = new FontSystem();
    /// fontSystem.AddFont(File.ReadAllBytes("Arial.ttf"));
    ///
    /// var engine = new FontStashSharpTextEngine();
    /// engine.AddFontSystem("Arial", CustomFontStyles.Normal, fontSystem);
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
                // MeasureString on a single pipe gives a reliable line-height approximation
                var lineSize  = font.MeasureString("|");
                LineHeight = lineSize.Y;
                var spaceSize = font.MeasureString(" ");
                SpaceWidth = spaceSize.X;
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
        /// Scale factor applied to MGUI's logical <c>FontSize</c> before passing it to
        /// <see cref="FontSystem.GetFont"/>.
        /// <para/>
        /// MonoGame's built-in SpriteFont renderer reports a <c>LineSpacing</c> that is
        /// typically ~1.5× the nominal font size, whereas FontStashSharp's
        /// <c>SpriteFontBase.LineHeight</c> is ~1.0× the pixel size passed to
        /// <c>GetFont()</c>.  Multiplying by <c>1.5f</c> makes both engines produce
        /// visually equivalent text metrics under MGUI's default layout.
        /// <para/>
        /// Set to <c>1f</c> if your <see cref="FontSystem"/> instances were built with sizes
        /// already expressed in the same unit as <c>FontSize</c>.
        /// <para/>
        /// Changing this value automatically clears the resolved-font cache so that all
        /// subsequent <see cref="ResolveFont"/> calls use the new scale.
        /// Must be set before rendering begins (MonoGame is single-threaded; do not change
        /// this property from a background thread while the engine is in use).
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
        private float _fontSizeScale = 1.5f;

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

            SpriteFontBase spriteFontBase = fs.GetFont((int)Math.Round(spec.Size * FontSizeScale));
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
