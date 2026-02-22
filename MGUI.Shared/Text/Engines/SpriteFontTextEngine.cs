using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace MGUI.Shared.Text.Engines
{
    /// <summary>
    /// <see cref="ITextEngine"/> implementation backed by MonoGame <see cref="SpriteFont"/> via
    /// the existing <see cref="FontManager"/> / <see cref="FontSet"/> infrastructure.
    /// Behaviour is bit-for-bit identical to the original hard-coded path in
    /// <c>DrawTransaction</c> and <c>MGTextBlock</c>.
    /// </summary>
    public sealed class SpriteFontTextEngine : ITextEngine
    {
        // ── Private helpers ──────────────────────────────────────────────────────

        /// <summary>All data extracted from <see cref="FontSet"/> for one resolved font variant.</summary>
        private sealed class SpriteFontHandle
        {
            public SpriteFont SF          { get; }
            public float      Scale       { get; }   // SuggestedScale
            public float      ExactScale  { get; }
            public int        Size        { get; }
            public int        FontHeight  { get; }   // Heights[size]
            public Vector2    Origin      { get; }   // Origins[size]
            public Dictionary<char, SpriteFont.Glyph> Glyphs { get; }

            public SpriteFontHandle(SpriteFont sf, float scale, float exactScale,
                int size, int fontHeight, Vector2 origin)
            {
                SF         = sf;
                Scale      = scale;
                ExactScale = exactScale;
                Size       = size;
                FontHeight = fontHeight;
                Origin     = origin;
                Glyphs     = sf.GetGlyphs();
            }
        }

        // ── Fields ───────────────────────────────────────────────────────────────

        private readonly FontManager _fontManager;

        /// <summary>Cache: FontSpec → ResolvedFont (+ embedded SpriteFontHandle as NativeFont).</summary>
        private readonly Dictionary<FontSpec, ResolvedFont> _cache = new();

        // ── Constructor ──────────────────────────────────────────────────────────

        /// <param name="fontManager">The shared <see cref="FontManager"/> instance from
        /// <c>MainRenderer</c>.</param>
        public SpriteFontTextEngine(FontManager fontManager)
        {
            _fontManager = fontManager;
        }

        // ── ITextEngine ──────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public ResolvedFont ResolveFont(FontSpec spec)
        {
            if (_cache.TryGetValue(spec, out ResolvedFont cached))
                return cached;

            bool found = _fontManager.TryGetFont(
                spec.Family, spec.Style, spec.Size,
                /*PreferDownsampled*/ true,
                out FontSet fs, out SpriteFont sf,
                out int actualSize, out float exactScale, out float suggestedScale);

            bool isFallback = false;

            // Fallback: try the default family
            if (!found)
            {
                isFallback = true;
                string fallbackFamily = _fontManager.DefaultFontFamily;
                found = _fontManager.TryGetFont(
                    fallbackFamily, CustomFontStyles.Normal, spec.Size,
                    /*PreferDownsampled*/ true,
                    out fs, out sf, out actualSize, out exactScale, out suggestedScale);
                // If still not found, take whatever comes first
                if (!found)
                {
                    fs  = _fontManager.GetFontFamilyOrDefault(spec.Family);
                    if (fs is { IsValid: true })
                    {
                        fs.TryGetFont(CustomFontStyles.Normal, spec.Size, true, out sf, out actualSize, out exactScale, out suggestedScale);
                        found = sf != null;
                    }
                }
            }

            if (!found || sf == null)
            {
                // Last resort: an empty / placeholder ResolvedFont
                var placeholder = new ResolvedFont(spec, spec.Size, 1f, 1f, spec.Size, 6f, Vector2.Zero, true, null);
                _cache[spec] = placeholder;
                return placeholder;
            }

            var handle = new SpriteFontHandle(sf, suggestedScale, exactScale, actualSize,
                fs.Heights[actualSize], fs.Origins[actualSize]);

            float lineHeight  = handle.FontHeight * handle.Scale;
            float spaceWidth  = (sf.MeasureString(" ") * suggestedScale).X;

            var resolved = new ResolvedFont(
                spec,
                actualSize,
                exactScale,
                suggestedScale,
                lineHeight,
                spaceWidth,
                handle.Origin,
                isFallback,
                handle);

            _cache[spec] = resolved;
            return resolved;
        }

        /// <inheritdoc/>
        public Vector2 MeasureText(ResolvedFont font, string text)
        {
            if (string.IsNullOrEmpty(text))
                return Vector2.Zero;

            var h = (SpriteFontHandle)font.NativeFont;
            // Width: use SF.MeasureString for consistency with DrawTransaction.MeasureText
            float width = h.SF.MeasureString(text).X * h.Scale;
            return new Vector2(width, h.FontHeight * h.Scale);
        }

        /// <inheritdoc/>
        public GlyphMetrics MeasureGlyph(ResolvedFont font, char c)
        {
            var h = (SpriteFontHandle)font.NativeFont;

            if (!h.Glyphs.TryGetValue(c, out SpriteFont.Glyph glyph))
            {
                if (h.SF.DefaultCharacter.HasValue)
                    h.Glyphs.TryGetValue(h.SF.DefaultCharacter.Value, out glyph);
            }

            float scale  = h.Scale;
            float height = h.FontHeight * scale;
            // Reproduce the same arithmetic as MGTextBlock.MeasureText:
            // CurrentXOffset += Glyph.WidthIncludingBearings  (no separate left/right attribution)
            // We expose the real bearings so callers can apply the "first glyph" correction if needed.
            return new GlyphMetrics(
                glyph.LeftSideBearing  * scale,
                glyph.Width            * scale,
                glyph.RightSideBearing * scale,
                height);
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
            if (string.IsNullOrEmpty(text) || font.NativeFont == null)
                return;

            var h = (SpriteFontHandle)font.NativeFont;
            spriteBatch.DrawString(h.SF, text, position, color, rotation, origin, scale, effects, depth);
        }

        /// <inheritdoc/>
        public void InvalidateCache() => _cache.Clear();
    }
}
