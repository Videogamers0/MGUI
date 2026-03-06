namespace MGUI.Shared.Text
{
    /// <summary>
    /// Per-glyph metrics returned by <see cref="Engines.ITextEngine.MeasureGlyph"/>.
    /// All values are already scaled (i.e. multiplied by the font's effective scale factor).
    /// </summary>
    public readonly record struct GlyphMetrics(
        float LeftSideBearing,
        float GlyphWidth,
        float RightSideBearing,
        float Height)
    {
        /// <summary>
        /// LeftSideBearing + GlyphWidth + RightSideBearing — the advance width consumed by this glyph.
        /// </summary>
        public float TotalWidth => LeftSideBearing + GlyphWidth + RightSideBearing;

        /// <summary>
        /// Total width starting from the maximum of LeftSideBearing and 0 (first glyph on a line behaviour).
        /// </summary>
        public float TotalWidthFirstGlyph =>
            (LeftSideBearing < 0f ? 0f : LeftSideBearing) + GlyphWidth + RightSideBearing;
    }
}
