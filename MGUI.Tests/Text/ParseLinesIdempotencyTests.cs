using System.Collections.Generic;
using System.Linq;
using MGUI.Core.UI.Text;
using Microsoft.Xna.Framework;
using Xunit;

namespace MGUI.Tests.Text
{
    /// <summary>
    /// Regression tests for the ParseLines non-idempotency bug (GitHub PR #35).
    ///
    /// Root cause: the wrapping DECISION in ParseLines uses per-word measurement summed
    /// word-by-word (CurrentX accumulation), but FlushLine computes LineWidth by
    /// re-measuring each run as a whole string.  Because the whole-string measurement
    /// can be smaller than the sum of individual-word measurements (kerning), calling
    ///
    ///   ParseLines(W) → lines where Max(LineWidth) = W'
    ///
    /// and then calling
    ///
    ///   ParseLines(W') → different (wrong) number of lines
    ///
    /// The test below is intentionally RED until Task 3 fixes ParseLines.
    /// After the fix it should turn GREEN (Task 4).
    /// </summary>
    public class ParseLinesIdempotencyTests
    {
        // ------------------------------------------------------------------ helpers
        private static MGTextRunConfig Plain => new(false);

        private static List<MGTextRun> Runs(string text)
            => new() { new MGTextRunText(text, Plain, null, null) };

        /// <summary>
        /// Mock ITextMeasurer that simulates whole-string kerning compression.
        ///
        /// Key measurements (all at height 14):
        ///   "Stardew"        → 43   (word only)
        ///   " "              →  7   (space)
        ///   "Valley"         → 42   (word only)
        ///   "Stardew "       → 50   (= 43+7, used by FlushLine when wrap occurs at 87)
        ///   "Stardew Valley" → 87   (< 43+7+42=92 — kerning saves 5 px)
        ///
        /// Sum of words = 92, whole-string = 87 → this delta drives the bug.
        /// </summary>
        private sealed class KerningMeasurer : ITextMeasurer
        {
            public Vector2 MeasureText(string Text, bool IsBold, bool IsItalic)
            {
                float w = Text switch
                {
                    "Stardew"        => 43f,
                    " "              =>  7f,
                    "Valley"         => 42f,
                    "Stardew "       => 50f,   // partial flush on wrap
                    "Stardew Valley" => 87f,   // whole-string (kerning saves 5 px)
                    _                => 7f * Text.Length
                };
                return new Vector2(w, 14f);
            }
        }

        // ------------------------------------------------------------------ tests

        /// <summary>
        /// Parse with available width = 92 (word-by-word sum).
        /// The three words 43+7+42 = 92 ≤ 92 all fit on one line.
        /// FlushLine measures the whole string → LineWidth = 87.
        /// </summary>
        [Fact]
        public void ParseLines_WithWidth92_ProducesOneLine_WithLineWidth87()
        {
            var measurer = new KerningMeasurer();
            var runs = Runs("Stardew Valley");

            var lines = MGTextLine.ParseLines(measurer, 92, true, runs, false).ToList();

            Assert.Single(lines);
            Assert.Equal(87f, lines[0].LineWidth);
        }

        /// <summary>
        /// Verifies that ParseLines is idempotent: re-parsing with the resulting LineWidth
        /// gives the same line count and layout as the original parse.
        ///
        /// Scenario: "Stardew Valley" fits on one line at width=92 (word-by-word sum=92).
        /// FlushLine measures the whole string → LineWidth=87.  Calling ParseLines(87) must
        /// also return 1 line — the fix uses whole-string measurement for the wrap decision
        /// so MeasureText("Stardew Valley")=87 ≤ 87 → 1 line.
        /// </summary>
        [Fact]
        public void ParseLines_IsIdempotent_ReParsingWithResultingLineWidthGivesSameLineCount()
        {
            var measurer = new KerningMeasurer();
            var runs = Runs("Stardew Valley");

            // First parse: returns 1 line with LineWidth = 87
            var firstPass = MGTextLine.ParseLines(measurer, 92, true, runs, false).ToList();
            Assert.Single(firstPass);
            float lineWidth = firstPass[0].LineWidth; // 87

            // Second parse with the resulting width: must still produce exactly 1 line.
            var secondPass = MGTextLine.ParseLines(measurer, lineWidth, true, runs, false).ToList();

            Assert.Single(secondPass);
            Assert.Equal(lineWidth, secondPass[0].LineWidth);
        }

        /// <summary>
        /// Verifies that after the fix, ParseLines with width=87 also produces a single line,
        /// confirming idempotency: the result is now identical to ParseLines(92) because
        /// the wrapping decision uses whole-string measurement (87 ≤ 87).
        /// </summary>
        [Fact]
        public void ParseLines_WithWidth87_AfterFix_ProducesOneLine()
        {
            var measurer = new KerningMeasurer();
            var runs = Runs("Stardew Valley");

            var lines = MGTextLine.ParseLines(measurer, 87, true, runs, false).ToList();

            // After the fix: whole-string measurement MeasureText("Stardew Valley")=87 ≤ 87 → 1 line
            Assert.Single(lines);
            Assert.Equal(87f, lines[0].LineWidth);
        }
    }
}
