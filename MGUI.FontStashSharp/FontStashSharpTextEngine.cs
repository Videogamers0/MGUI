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

            public FSSFontHandle(SpriteFontBase font)
            {
                Font = font;
                // MeasureString on a single pipe gives a reliable line-height approximation
                var lineSize  = font.MeasureString("|");
                LineHeight = lineSize.Y;
                var spaceSize = font.MeasureString(" ");
                SpaceWidth = spaceSize.X;
            }
        }

        // ── Fields ───────────────────────────────────────────────────────────────

        /// <summary>Dictionary of FontSystem per (family, style).</summary>
        private readonly Dictionary<(string Family, CustomFontStyles Style), FontSystem> _fontSystems = new();

        /// <summary>Fallback FontSystem used when a family+style combination is not registered.</summary>
        private FontSystem? _fallbackFontSystem;

        /// <summary>Cache: FontSpec → ResolvedFont (+ embedded FSSFontHandle as NativeFont).</summary>
        private readonly Dictionary<FontSpec, ResolvedFont> _cache = new();

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

            SpriteFontBase spriteFontBase = fs.GetFont(spec.Size);
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

        /// <inheritdoc/>
        public Vector2 MeasureText(ResolvedFont font, string text)
        {
            if (string.IsNullOrEmpty(text) || font.NativeFont is null)
                return Vector2.Zero;

            var h    = (FSSFontHandle)font.NativeFont;
            var size = h.Font.MeasureString(text);
            // Use the consistent line height rather than the string's own Y
            return new Vector2(size.X, h.LineHeight);
        }

        /// <inheritdoc/>
        public GlyphMetrics MeasureGlyph(ResolvedFont font, char c)
        {
            if (font.NativeFont is null)
                return new GlyphMetrics(0, 0, 0, font.LineHeight);

            var h    = (FSSFontHandle)font.NativeFont;
            // FSS doesn't expose per-glyph bearings via public API;
            // measure the single-char string and report the full width as GlyphWidth
            // (LeftSideBearing = RightSideBearing = 0, so TotalWidth == TotalWidthFirstGlyph).
            var size = h.Font.MeasureString(c.ToString());
            return new GlyphMetrics(0f, size.X, 0f, h.LineHeight);
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
            if (string.IsNullOrEmpty(text) || font.NativeFont is null)
                return;

            var h = (FSSFontHandle)font.NativeFont;
            h.Font.DrawText(spriteBatch, text, position, color,
                rotation:   rotation,
                origin:     origin,
                scale:      new Vector2(scale),
                layerDepth: depth);
        }

        /// <inheritdoc/>
        public void InvalidateCache() => _cache.Clear();
    }
}
