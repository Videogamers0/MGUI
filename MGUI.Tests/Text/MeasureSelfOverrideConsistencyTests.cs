using System;
using System.Collections.Generic;
using System.Linq;
using MGUI.Core.UI.Text;
using Microsoft.Xna.Framework;
using Xunit;

namespace MGUI.Tests.Text
{
    /// <summary>
    /// Tests that simulate the MeasureSelfOverride flow from MGTextBlock:
    ///
    ///   1. ParseLines(availableWidth)  →  lines where Max(LineWidth) = W'
    ///   2. ParseLines(Math.Ceiling(W')) →  same number of lines  (idempotency guarantee)
    ///
    /// MGTextBlock.MeasureSelfOverride calls ParseLines with RemainingSize.Width and
    /// returns Thickness(Math.Ceiling(MaxLineWidth), ...).  The WPF-like layout engine can
    /// then call MeasureSelfOverride again with the exact Ceiling width, so the two passes
    /// must agree.
    ///
    /// Note: MGTextBlock itself cannot be tested without a live GraphicsDevice / MGDesktop,
    /// so these tests exercise the ParseLines layer directly, which is the only code path
    /// that needs to be idempotent for correct layout.
    /// </summary>
    public class MeasureSelfOverrideConsistencyTests
    {
        // ------------------------------------------------------------------ helpers

        private static MGTextRunConfig Plain => new(false);

        private static List<MGTextRun> Runs(string text)
            => new() { new MGTextRunText(text, Plain, null, null) };

        /// <summary>
        /// Simulates Math.Ceiling applied to the max LineWidth, matching MeasureSelfOverride.
        /// </summary>
        private static double CeilingWidth(IEnumerable<MGTextLine> lines)
            => Math.Ceiling(lines.Select(l => (double)l.LineWidth).DefaultIfEmpty(0).Max());

        // ------------------------------------------------------------------ measurer variants

        /// <summary>Monospaced mock: all strings measure exactly 7 px per character.</summary>
        private sealed class MonospacedMeasurer : ITextMeasurer
        {
            public Vector2 MeasureText(string Text, bool IsBold, bool IsItalic)
                => new(7f * (Text?.Length ?? 0), 14f);
        }

        /// <summary>
        /// Kerning mock: whole-string measurements are tighter than per-glyph sums.
        /// Pair "AB" saves 1 px per adjacent pair compared to A+B individually.
        /// </summary>
        private sealed class KerningMeasurer2 : ITextMeasurer
        {
            // Simulate simple pair-kerning: consecutive pairs save 1 px each.
            public Vector2 MeasureText(string Text, bool IsBold, bool IsItalic)
            {
                if (string.IsNullOrEmpty(Text)) return new Vector2(0, 14f);
                float width = 7f * Text.Length;
                // Deduct 1 px for every adjacent pair (simulate kerning)
                if (Text.Length > 1) width -= (Text.Length - 1);
                return new Vector2(Math.Max(0, width), 14f);
            }
        }

        // ------------------------------------------------------------------ tests

        /// <summary>
        /// Monospaced text: zero kerning, so the wrapping decision and FlushLine measurement
        /// are always identical.  Re-parse with Ceiling(LineWidth) must produce the same layout.
        /// </summary>
        [Theory]
        [InlineData("Hello World", 80)]
        [InlineData("The quick brown fox", 100)]
        [InlineData("Single", 50)]
        [InlineData("A B C D E F", 30)]
        public void MeasureSelfOverride_Monospaced_IsIdempotent(string text, double availableWidth)
        {
            var measurer = new MonospacedMeasurer();
            var runs = Runs(text);

            var firstPass = MGTextLine.ParseLines(measurer, availableWidth, true, runs, false).ToList();
            double ceilingWidth = CeilingWidth(firstPass);

            var secondPass = MGTextLine.ParseLines(measurer, ceilingWidth, true, runs, false).ToList();

            Assert.Equal(firstPass.Count, secondPass.Count);
        }

        /// <summary>
        /// Text with kerning: whole-string measurements are smaller than word-by-word sums.
        /// Re-parse with Ceiling(LineWidth) must still produce the same line count.
        /// </summary>
        [Theory]
        [InlineData("Hello World", 80)]
        [InlineData("The quick brown fox", 100)]
        [InlineData("Word One Two Three", 70)]
        [InlineData("A B C D E F G H", 30)]
        public void MeasureSelfOverride_WithKerning_IsIdempotent(string text, double availableWidth)
        {
            var measurer = new KerningMeasurer2();
            var runs = Runs(text);

            var firstPass = MGTextLine.ParseLines(measurer, availableWidth, true, runs, false).ToList();
            double ceilingWidth = CeilingWidth(firstPass);

            var secondPass = MGTextLine.ParseLines(measurer, ceilingWidth, true, runs, false).ToList();

            Assert.Equal(firstPass.Count, secondPass.Count);
        }

        /// <summary>
        /// Verify the Stardew Valley scenario end-to-end with the MeasureSelfOverride flow:
        ///   available=92 → 1 line, LineWidth=87, Ceiling=87
        ///   available=87 → 1 line (idempotent after fix)
        /// </summary>
        [Fact]
        public void MeasureSelfOverride_StardewValley_ReturnsConsistentWidth()
        {
            // Same measurer as ParseLinesIdempotencyTests
            var measurer = new SparseKerningMeasurer();
            var runs = Runs("Stardew Valley");

            // First pass (simulates MeasureSelfOverride with the parent's available width)
            var firstPass = MGTextLine.ParseLines(measurer, 92, true, runs, false).ToList();
            Assert.Single(firstPass);
            double ceilingW = CeilingWidth(firstPass); // Math.Ceiling(87) = 87

            // Second pass (simulates MeasureSelfOverride re-called with the exact measured width)
            var secondPass = MGTextLine.ParseLines(measurer, ceilingW, true, runs, false).ToList();
            Assert.Single(secondPass);
            Assert.Equal(firstPass[0].LineWidth, secondPass[0].LineWidth);
        }

        /// <summary>
        /// Sparse kerning measurer: known values from ParseLinesIdempotencyTests.
        /// </summary>
        private sealed class SparseKerningMeasurer : ITextMeasurer
        {
            public Vector2 MeasureText(string Text, bool IsBold, bool IsItalic)
            {
                float w = Text switch
                {
                    "Stardew"        => 43f,
                    " "              =>  7f,
                    "Valley"         => 42f,
                    "Stardew "       => 50f,
                    "Stardew Valley" => 87f,
                    _                => 7f * (Text?.Length ?? 0)
                };
                return new Vector2(w, 14f);
            }
        }

        /// <summary>
        /// No-wrap mode: ParseLines should always produce a single line regardless of width.
        /// Idempotency is trivially satisfied here.
        /// </summary>
        [Fact]
        public void MeasureSelfOverride_NoWrap_AlwaysOneLine()
        {
            var measurer = new MonospacedMeasurer();
            var runs = Runs("This is a very long line that would wrap if wrapping were enabled");

            var lines = MGTextLine.ParseLines(measurer, 50, false, runs, false).ToList();
            Assert.Single(lines);
        }
    }
}
