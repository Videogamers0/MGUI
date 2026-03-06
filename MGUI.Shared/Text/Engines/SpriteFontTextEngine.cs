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

            // Use exactScale for all measurements so that layout/wrapping matches the original
            // MGTextBlock.MeasureText behaviour (which multiplied by FontScale = exactScale).
            // Drawing still uses suggestedScale for sharp rendering (see DrawText / DrawTransaction).
            float lineHeight  = handle.FontHeight * handle.ExactScale;
            float spaceWidth  = (sf.MeasureString(" ") * exactScale).X;

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

        // ── Private helper ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the <see cref="SpriteFontHandle"/> for <paramref name="font"/>.
        /// If the <see cref="ResolvedFont"/> was created by a different engine (e.g. after
        /// toggling backends at runtime), the font is transparently re-resolved using its
        /// <see cref="ResolvedFont.Spec"/> so the caller always gets a valid SpriteFont handle.
        /// </summary>
        private SpriteFontHandle GetHandle(ResolvedFont font)
        {
            if (font.NativeFont is SpriteFontHandle h)
                return h;
            // Stale handle from another engine — re-resolve via spec.
            return ResolveFont(font.Spec).NativeFont as SpriteFontHandle;
        }

        /// <inheritdoc/>
        public Vector2 MeasureText(ResolvedFont font, string text)
        {
            if (string.IsNullOrEmpty(text))
                return Vector2.Zero;

            var h = GetHandle(font);
            if (h is null) return Vector2.Zero;

            // Use exactScale so measurement matches the original MGTextBlock.MeasureText
            // (which applied FontScale = exactScale).  Drawing still happens at suggestedScale
            // via DrawTransaction, so layouts designed for the original engine remain correct.
            float width = h.SF.MeasureString(text).X * h.ExactScale;
            return new Vector2(width, h.FontHeight * h.ExactScale);
        }

        /// <inheritdoc/>
        public GlyphMetrics MeasureGlyph(ResolvedFont font, char c)
        {
            var h = GetHandle(font);
            if (h is null)
                return new GlyphMetrics(0, 0, 0, font.LineHeight);

            if (!h.Glyphs.TryGetValue(c, out SpriteFont.Glyph glyph))
            {
                if (h.SF.DefaultCharacter.HasValue)
                    h.Glyphs.TryGetValue(h.SF.DefaultCharacter.Value, out glyph);
            }

            // Use exactScale to stay consistent with MeasureText and the original codebase.
            float scale  = h.ExactScale;
            float height = h.FontHeight * scale;
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
            if (string.IsNullOrEmpty(text))
                return;

            var h = GetHandle(font);
            if (h is null) return;

            spriteBatch.DrawString(h.SF, text, position, color, rotation, origin, scale, effects, depth);
        }

        /// <inheritdoc/>
        public void InvalidateCache() => _cache.Clear();
    }
}
