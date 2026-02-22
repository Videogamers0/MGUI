using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MGUI.Shared.Text;
using MGUI.Shared.Text.Engines;

namespace MGUI.Tests.Text.Engines
{
    /// <summary>
    /// Headless <see cref="ITextEngine"/> for unit tests — no GPU required.
    /// Uses a configurable fixed character width (monospace model) and line height.
    /// </summary>
    public sealed class MockTextEngine : ITextEngine
    {
        // ── Config ───────────────────────────────────────────────────────────────

        /// <summary>Width of each character in scaled pixels (default 8).</summary>
        public float CharWidth { get; set; } = 8f;

        /// <summary>Line height in scaled pixels (default 14).</summary>
        public float LineHeightValue { get; set; } = 14f;

        /// <summary>If non-null, the engine will return a fallback for this family name.</summary>
        public string? FallbackTriggerFamily { get; set; }

        // ── Cache ────────────────────────────────────────────────────────────────

        private readonly Dictionary<FontSpec, ResolvedFont> _cache = new();

        // ── ITextEngine ──────────────────────────────────────────────────────────

        public ResolvedFont ResolveFont(FontSpec spec)
        {
            if (_cache.TryGetValue(spec, out var cached))
                return cached;

            bool isFallback = spec.Family == FallbackTriggerFamily;
            var resolved = new ResolvedFont(
                spec,
                spec.Size,
                exactScale: 1f,
                suggestedScale: 1f,
                lineHeight: LineHeightValue,
                spaceWidth: CharWidth,
                drawOrigin: Vector2.Zero,
                isFallback: isFallback,
                nativeFont: null);

            _cache[spec] = resolved;
            return resolved;
        }

        public Vector2 MeasureText(ResolvedFont font, string text)
        {
            if (string.IsNullOrEmpty(text))
                return Vector2.Zero;
            return new Vector2(text.Length * CharWidth, LineHeightValue);
        }

        public GlyphMetrics MeasureGlyph(ResolvedFont font, char c)
            => new GlyphMetrics(0f, CharWidth, 0f, LineHeightValue);

        public float GetLineHeight(ResolvedFont font) => LineHeightValue;

        public float GetSpaceWidth(ResolvedFont font) => CharWidth;

        public void DrawText(SpriteBatch spriteBatch, ResolvedFont font, string text,
            Vector2 position, Color color, Vector2 origin,
            float scale, float rotation = 0f, float depth = 0f, SpriteEffects effects = SpriteEffects.None)
        {
            // No-op in tests — no GPU available.
        }

        public void InvalidateCache() => _cache.Clear();
    }
}
