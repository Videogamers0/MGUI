using MGUI.Shared.Text;
using Xunit;

namespace MGUI.Tests.Text
{
    public class GlyphMetricsTests
    {
        [Fact]
        public void TotalWidth_IsSum()
        {
            var g = new GlyphMetrics(1f, 6f, 1f, 12f);
            Assert.Equal(8f, g.TotalWidth, precision: 3);
        }

        [Fact]
        public void TotalWidthFirstGlyph_ClampsNegativeLeftBearing()
        {
            // Negative left bearing should be clamped to 0 for first glyph
            var g = new GlyphMetrics(-2f, 6f, 1f, 12f);
            Assert.Equal(7f, g.TotalWidthFirstGlyph, precision: 3);
        }

        [Fact]
        public void TotalWidthFirstGlyph_PositiveBearing_Unchanged()
        {
            var g = new GlyphMetrics(1f, 6f, 1f, 12f);
            Assert.Equal(8f, g.TotalWidthFirstGlyph, precision: 3);
        }

        [Fact]
        public void TotalWidth_CanBeZero()
        {
            var g = new GlyphMetrics(0f, 0f, 0f, 0f);
            Assert.Equal(0f, g.TotalWidth);
        }

        [Fact]
        public void EqualityIsValueBased()
        {
            var a = new GlyphMetrics(1f, 6f, 1f, 12f);
            var b = new GlyphMetrics(1f, 6f, 1f, 12f);
            var c = new GlyphMetrics(0f, 6f, 1f, 12f);
            Assert.Equal(a, b);
            Assert.NotEqual(a, c);
        }
    }
}
