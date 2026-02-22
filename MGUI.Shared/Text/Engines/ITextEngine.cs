using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MGUI.Shared.Text.Engines
{
    /// <summary>
    /// Abstraction over text measurement and rendering.  Implement this interface to provide a
    /// custom font backend (e.g. <c>SpriteFont</c>, <c>FontStashSharp</c>, …).<para/>
    /// All text operations that span both measure and draw use a two-step approach:
    /// <list type="number">
    ///   <item><description>Resolve a <see cref="ResolvedFont"/> via <see cref="ResolveFont"/>.</description></item>
    ///   <item><description>Pass the resolved handle to <see cref="MeasureText"/>, <see cref="MeasureGlyph"/>, or <see cref="DrawText"/>.</description></item>
    /// </list>
    /// Implementations must be safe to call from the MonoGame update / draw thread.
    /// </summary>
    public interface ITextEngine
    {
        // ── Resolution ──────────────────────────────────────────────────────────

        /// <summary>
        /// Resolves the best available font for <paramref name="spec"/> and returns an opaque
        /// handle.  Never throws — returns a fallback font when the requested family is not
        /// registered (check <see cref="ResolvedFont.IsFallback"/>).
        /// Results are cached internally; call <see cref="InvalidateCache"/> when fonts change.
        /// </summary>
        ResolvedFont ResolveFont(FontSpec spec);

        // ── Measurement ─────────────────────────────────────────────────────────

        /// <summary>
        /// Measures the width and height of <paramref name="text"/> when rendered with
        /// <paramref name="font"/> using <see cref="ResolvedFont.SuggestedScale"/>.
        /// Returns <see cref="Vector2.Zero"/> for null or empty strings.
        /// </summary>
        Vector2 MeasureText(ResolvedFont font, string text);

        /// <summary>
        /// Returns scaled metrics for a single glyph.
        /// Used for the additive character-by-character measurement required by
        /// <c>MGTextLine.ParseLines</c> and <c>TextRenderInfo</c>.
        /// </summary>
        GlyphMetrics MeasureGlyph(ResolvedFont font, char c);

        /// <summary>Line height in scaled pixels for <paramref name="font"/>.</summary>
        float GetLineHeight(ResolvedFont font);

        /// <summary>Width of a space character in scaled pixels for <paramref name="font"/>.</summary>
        float GetSpaceWidth(ResolvedFont font);

        // ── Rendering ───────────────────────────────────────────────────────────

        /// <summary>
        /// Draws <paramref name="text"/> onto <paramref name="spriteBatch"/>.
        /// <paramref name="spriteBatch"/> must already have <c>Begin()</c> called.
        /// <para/>
        /// <paramref name="origin"/> is the glyph origin offset (e.g. from
        /// <see cref="ResolvedFont.DrawOrigin"/>); pass <see cref="Vector2.Zero"/> to draw from
        /// the top-left corner.
        /// </summary>
        void DrawText(
            SpriteBatch spriteBatch,
            ResolvedFont font,
            string text,
            Vector2 position,
            Color color,
            Vector2 origin,
            float rotation  = 0f,
            float depth     = 0f,
            SpriteEffects effects = SpriteEffects.None);

        // ── Lifecycle ───────────────────────────────────────────────────────────

        /// <summary>
        /// Clears the internal <see cref="ResolvedFont"/> cache.  Call this when fonts are
        /// added or removed, or when switching to a completely different font set at runtime.
        /// </summary>
        void InvalidateCache();
    }
}
