using Microsoft.Xna.Framework;
using MGUI.Shared.Text;
using MGUI.Shared.Text.Engines;
using Xunit;

namespace MGUI.Tests.Text.Engines
{
    /// <summary>
    /// Verifies the <see cref="ITextEngine"/> contract using <see cref="MockTextEngine"/>.
    /// No GPU / MonoGame runtime is required.
    /// </summary>
    public class MockTextEngineTests
    {
        private MockTextEngine CreateEngine(float charWidth = 8f, float lineHeight = 14f)
            => new MockTextEngine { CharWidth = charWidth, LineHeightValue = lineHeight };

        // ── ResolveFont ──────────────────────────────────────────────────────────

        [Fact]
        public void ResolveFont_ReturnsNonNull()
        {
            var engine = CreateEngine();
            var resolved = engine.ResolveFont(FontSpec.Normal("Arial", 12));
            Assert.NotNull(resolved);
        }

        [Fact]
        public void ResolveFont_ReturnsSameInstanceOnSecondCall()
        {
            var engine = CreateEngine();
            var spec = FontSpec.Normal("Arial", 12);
            var r1 = engine.ResolveFont(spec);
            var r2 = engine.ResolveFont(spec);
            Assert.Same(r1, r2);
        }

        [Fact]
        public void ResolveFont_FallbackTrigger_SetsFallbackFlag()
        {
            var engine = CreateEngine();
            engine.FallbackTriggerFamily = "Unknown";
            var resolved = engine.ResolveFont(new FontSpec("Unknown", 12, CustomFontStyles.Normal));
            Assert.True(resolved.IsFallback);
        }

        [Fact]
        public void ResolveFont_NormalFamily_NotFallback()
        {
            var engine = CreateEngine();
            engine.FallbackTriggerFamily = "Unknown";
            var resolved = engine.ResolveFont(FontSpec.Normal("Arial", 12));
            Assert.False(resolved.IsFallback);
        }

        [Fact]
        public void InvalidateCache_ClearsCachedInstances()
        {
            var engine = CreateEngine();
            var spec = FontSpec.Normal("Arial", 12);
            var r1 = engine.ResolveFont(spec);
            engine.InvalidateCache();
            var r2 = engine.ResolveFont(spec);
            // After invalidation a new instance is created
            Assert.NotSame(r1, r2);
        }

        // ── MeasureText ──────────────────────────────────────────────────────────

        [Fact]
        public void MeasureText_EmptyString_ReturnsZero()
        {
            var engine = CreateEngine(charWidth: 8f);
            var resolved = engine.ResolveFont(FontSpec.Normal("Arial", 12));
            Assert.Equal(Vector2.Zero, engine.MeasureText(resolved, ""));
        }

        [Fact]
        public void MeasureText_NullString_ReturnsZero()
        {
            var engine = CreateEngine(charWidth: 8f);
            var resolved = engine.ResolveFont(FontSpec.Normal("Arial", 12));
            Assert.Equal(Vector2.Zero, engine.MeasureText(resolved, null!));
        }

        [Fact]
        public void MeasureText_WidthIsLengthTimesCharWidth()
        {
            const float charWidth = 6f;
            var engine = CreateEngine(charWidth: charWidth, lineHeight: 14f);
            var resolved = engine.ResolveFont(FontSpec.Normal("Arial", 12));
            var size = engine.MeasureText(resolved, "Hello");
            Assert.Equal(5 * charWidth, size.X, precision: 3);
        }

        [Fact]
        public void MeasureText_HeightIsLineHeight()
        {
            const float lineHeight = 18f;
            var engine = CreateEngine(lineHeight: lineHeight);
            var resolved = engine.ResolveFont(FontSpec.Normal("Arial", 12));
            var size = engine.MeasureText(resolved, "Hello");
            Assert.Equal(lineHeight, size.Y, precision: 3);
        }

        // ── Additivity ───────────────────────────────────────────────────────────

        [Fact]
        public void MeasureText_IsAdditive_FullString_EqualsSumOfSubstrings()
        {
            var engine = CreateEngine(charWidth: 8f);
            var resolved = engine.ResolveFont(FontSpec.Normal("Arial", 12));
            const string full = "HelloWorld";
            const string half1 = "Hello";
            const string half2 = "World";

            float fullWidth = engine.MeasureText(resolved, full).X;
            float sumWidth  = engine.MeasureText(resolved, half1).X
                            + engine.MeasureText(resolved, half2).X;

            Assert.Equal(fullWidth, sumWidth, precision: 3);
        }

        [Fact]
        public void MeasureGlyph_SumEqualsFullMeasure()
        {
            const float charWidth = 7f;
            var engine = CreateEngine(charWidth: charWidth);
            var resolved = engine.ResolveFont(FontSpec.Normal("Arial", 12));
            const string text = "Test";

            float glyphSum = 0f;
            foreach (char c in text)
                glyphSum += engine.MeasureGlyph(resolved, c).TotalWidth;

            float measured = engine.MeasureText(resolved, text).X;
            Assert.Equal(measured, glyphSum, precision: 3);
        }

        // ── GetLineHeight / GetSpaceWidth ────────────────────────────────────────

        [Fact]
        public void GetLineHeight_MatchesResolvedFontLineHeight()
        {
            var engine = CreateEngine(lineHeight: 16f);
            var resolved = engine.ResolveFont(FontSpec.Normal("Arial", 12));
            Assert.Equal(16f, engine.GetLineHeight(resolved), precision: 3);
            Assert.Equal(resolved.LineHeight, engine.GetLineHeight(resolved), precision: 3);
        }

        [Fact]
        public void GetSpaceWidth_NonZero()
        {
            var engine = CreateEngine(charWidth: 5f);
            var resolved = engine.ResolveFont(FontSpec.Normal("Arial", 12));
            Assert.True(engine.GetSpaceWidth(resolved) > 0f);
        }

        // ── Different specs produce different resolved fonts ──────────────────────

        [Fact]
        public void DifferentSpecs_DifferentResolvedFonts()
        {
            var engine = CreateEngine();
            var r1 = engine.ResolveFont(FontSpec.Normal("Arial", 12));
            var r2 = engine.ResolveFont(FontSpec.Normal("Arial", 14));
            Assert.NotSame(r1, r2);
            Assert.NotEqual(r1.Spec, r2.Spec);
        }
    }
}
