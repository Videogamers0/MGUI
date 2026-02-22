using Microsoft.Xna.Framework;

namespace MGUI.Shared.Text
{
    /// <summary>
    /// The result of resolving a <see cref="FontSpec"/> through an <see cref="Engines.ITextEngine"/>.
    /// Consumers pass this handle to subsequent measure / draw calls — they never inspect the
    /// underlying native font object directly.
    /// </summary>
    public sealed class ResolvedFont
    {
        /// <summary>The spec that was requested.</summary>
        public FontSpec Spec { get; }

        /// <summary>The actual native font size that was chosen (may differ from <see cref="FontSpec.Size"/>
        /// when an engine uses downsampling heuristics).</summary>
        public int ActualSize { get; }

        /// <summary>
        /// Scale that produces the exact requested point size: DesiredSize / ActualSize.
        /// May yield slightly blurred text when non-integer.
        /// </summary>
        public float ExactScale { get; }

        /// <summary>
        /// Rounded / snapped scale that yields sharper text, at the cost of a slightly
        /// different visual size.
        /// </summary>
        public float SuggestedScale { get; }

        /// <summary>Line height in scaled pixels (already multiplied by <see cref="SuggestedScale"/>).</summary>
        public float LineHeight { get; }

        /// <summary>Width of a space character in scaled pixels.</summary>
        public float SpaceWidth { get; }

        /// <summary>
        /// Y origin offset used for drawing (e.g. from <c>FontSet.Origins[size]</c> in the
        /// SpriteFont backend). Already in the native (un-scaled) font coordinate space —
        /// backends pass it directly to <c>SpriteBatch.DrawString</c> as the origin.
        /// </summary>
        public Vector2 DrawOrigin { get; }

        /// <summary>True when the requested family was not found and a fallback was used.</summary>
        public bool IsFallback { get; }

        /// <summary>
        /// Opaque native font handle owned by the backend (e.g. a <c>SpriteFontHandle</c>
        /// for <c>SpriteFontTextEngine</c>, or an FSS wrapper for <c>FontStashSharpTextEngine</c>).
        /// This property is intended for backend implementations only — do not inspect or cast
        /// it outside of the engine that created this <see cref="ResolvedFont"/>.
        /// </summary>
        public object NativeFont { get; }

        /// <param name="nativeFont">Backend-specific object (e.g. <c>SpriteFont</c> or <c>SpriteFontBase</c>).
        /// Pass <c>null</c> when no native font is applicable (e.g. in headless test engines).</param>
        public ResolvedFont(
            FontSpec spec,
            int actualSize,
            float exactScale,
            float suggestedScale,
            float lineHeight,
            float spaceWidth,
            Vector2 drawOrigin,
            bool isFallback,
            object nativeFont)
        {
            Spec = spec;
            ActualSize = actualSize;
            ExactScale = exactScale;
            SuggestedScale = suggestedScale;
            LineHeight = lineHeight;
            SpaceWidth = spaceWidth;
            DrawOrigin = drawOrigin;
            IsFallback = isFallback;
            NativeFont = nativeFont;
        }

        /// <inheritdoc/>
        public override string ToString() =>
            $"ResolvedFont({Spec}, actualSize={ActualSize}, scale={SuggestedScale:F3}{(IsFallback ? ", FALLBACK" : "")})";
    }
}
