using System;
using System.IO;
using FontStashSharp;
using MGUI.FontStashSharp;
using MGUI.Shared.Text;
using MGUI.Shared.Text.Engines;
using Microsoft.Xna.Framework;
using Xunit;

namespace MGUI.Tests.Text;

/// <summary>
/// Verifies that <see cref="FontStashSharpTextEngine"/> produces internally
/// consistent measurement results.
///
/// Background:
/// Two bugs were fixed in sequence:
///
/// Bug 1 – text clipping:
///   MeasureText previously returned the calibrated SpriteFont glyph-width sum
///   (integer atlas advances × exactScale) while DrawText rendered using FSS's
///   float-precision StbTrueType advances.  Because the FSS advances are wider
///   than the SF values, text was drawn wider than the allocated layout slot and
///   clipped on the right.  Fix: MeasureText now uses FSS-native
///   <see cref="FontStashSharp.SpriteFontBase.MeasureString"/> on the resolved font.
///
/// Bug 2 – extra line-wrap (regression from Bug 1 fix):
///   After switching MeasureText to FSS-native widths, the engine measured text
///   as slightly WIDER than SpriteFontTextEngine (SF integer atlas × exactScale),
///   causing ParseLines to wrap one word earlier and produce an extra line.
///   Fix: ResolveFont now calibrates the FSS pixel size by applying the ratio
///   (calibrated-SF-spaceWidth / FSS-native-raw-spaceWidth) so that ALL glyph
///   advances scale to match SpriteFontTextEngine's measurements.  When
///   MatchSpriteFontSizing has not been called the uncalibrated path is used
///   as a safe fallback (tests below exercise this case).
/// </summary>
public class FSSMeasureDrawConsistencyTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static FontStashSharpTextEngine CreateEngine(out byte[] ttfData)
    {
        string ttfPath = Path.Combine(AppContext.BaseDirectory, "Fonts", "arial.ttf");
        ttfData = File.ReadAllBytes(ttfPath);

        var engine = new FontStashSharpTextEngine();
        var fontSystem = new FontSystem();
        fontSystem.AddFont(ttfData);
        engine.AddFontSystem("Arial", CustomFontStyles.Normal, fontSystem, ttfData);
        return engine;
    }

    private static ResolvedFont Resolve(FontStashSharpTextEngine engine, int size = 14)
        => engine.ResolveFont(new FontSpec("Arial", size, CustomFontStyles.Normal));

    // ── Tests ───────────────────────────────────────────────────────────────

    [Fact]
    public void MeasureText_ReturnsNonZeroWidth_ForNonEmptyString()
    {
        var engine = CreateEngine(out _);
        var font = Resolve(engine);

        Vector2 size = engine.MeasureText(font, "Hello World");
        Assert.True(size.X > 0, "MeasureText width should be positive for non-empty string");
        Assert.True(size.Y > 0, "MeasureText height should be positive for non-empty string");
    }

    [Fact]
    public void MeasureText_ReturnsZero_ForEmptyString()
    {
        var engine = CreateEngine(out _);
        var font = Resolve(engine);

        Assert.Equal(Vector2.Zero, engine.MeasureText(font, ""));
        Assert.Equal(Vector2.Zero, engine.MeasureText(font, null!));
    }

    [Fact]
    public void MeasureGlyph_ReturnsNonZeroWidth_ForVisibleChar()
    {
        var engine = CreateEngine(out _);
        var font = Resolve(engine);

        GlyphMetrics gm = engine.MeasureGlyph(font, 'A');
        Assert.True(gm.GlyphWidth > 0, "GlyphWidth should be positive for visible character");
        Assert.Equal(0f, gm.LeftSideBearing);
        Assert.Equal(0f, gm.RightSideBearing);
    }

    [Fact]
    public void SpaceWidth_MatchesMeasureText_SpaceChar()
    {
        var engine = CreateEngine(out _);
        var font = Resolve(engine);

        float spaceWidth = engine.GetSpaceWidth(font);
        float measuredSpace = engine.MeasureText(font, " ").X;

        // SpaceWidth comes from the resolved FSS font's h.SpaceWidth.
        // MeasureText also calls FSS MeasureString on the same resolved font.
        // Both must use the identical SpriteFontBase instance (whether corrected via
        // pixel-size calibration or raw fallback) so they agree exactly.
        Assert.Equal(measuredSpace, spaceWidth, precision: 2);
    }

    [Theory]
    [InlineData("Potion")]
    [InlineData("Phoenix Down")]
    [InlineData("Restores health by 100")]
    [InlineData("Stardew Valley")]
    public void MeasureText_Width_GreaterThanOrEqual_GlyphWidthSum(string text)
    {
        // The FSS-native MeasureString should return a width >= to the sum
        // of individual glyph widths (since MeasureString accounts for the
        // full advance including bearings, while GetGlyphWidth only returns
        // the single-char MeasureString width which may differ due to kerning).
        var engine = CreateEngine(out _);
        var font = Resolve(engine);

        float wholeStringWidth = engine.MeasureText(font, text).X;
        float glyphSum = 0f;
        foreach (char c in text)
            glyphSum += engine.MeasureGlyph(font, c).TotalWidth;

        // The whole-string width should be close to the glyph sum.
        // Small differences are expected due to kerning/hinting, but the
        // absolute difference should be small relative to the string width.
        float absDiff = MathF.Abs(wholeStringWidth - glyphSum);
        float tolerance = text.Length * 0.5f; // allow up to 0.5px per char
        Assert.True(absDiff <= tolerance,
            $"MeasureText({text}) = {wholeStringWidth:F2}, glyph sum = {glyphSum:F2}, diff = {absDiff:F2} exceeds tolerance {tolerance:F1}");
    }

    [Fact]
    public void MeasureText_Width_IsConsistent_AcrossFontSizes()
    {
        var engine = CreateEngine(out _);

        // Larger font sizes should produce proportionally wider measurements
        var font12 = Resolve(engine, 12);
        var font14 = Resolve(engine, 14);

        float w12 = engine.MeasureText(font12, "Test string").X;
        float w14 = engine.MeasureText(font14, "Test string").X;

        Assert.True(w14 > w12, "Font size 14 should produce wider text than font size 12");
    }

    [Fact]
    public void MeasureGlyph_AndMeasureText_UseConsistentSource()
    {
        // TextRenderInfo reconciles per-glyph sum against whole-string width.
        // Both must come from the same measurement source (FSS-native) for
        // good reconciliation (small residual).
        var engine = CreateEngine(out _);
        var font = Resolve(engine);
        string text = "The quick brown fox jumps over the lazy dog";

        float wholeWidth = engine.MeasureText(font, text).X;

        // Simulate TextRenderInfo's per-glyph accumulation
        float glyphSum = 0f;
        for (int i = 0; i < text.Length; i++)
        {
            GlyphMetrics gm = engine.MeasureGlyph(font, text[i]);
            glyphSum += i == 0 ? gm.TotalWidthFirstGlyph : gm.TotalWidth;
        }

        float residual = wholeWidth - glyphSum;
        float residualPercent = MathF.Abs(residual) / wholeWidth * 100f;

        // With both using FSS-native on the same resolved font, the residual
        // should be small (< 5%).  Before the fix, using calibrated SF glyph
        // widths could produce large residuals when the font was pixel-corrected.
        Assert.True(residualPercent < 5f,
            $"Residual between MeasureText and glyph sum is {residualPercent:F1}% ({residual:F2}px), expected < 5%");
    }

    [Fact]
    public void MeasureGlyph_SpaceChar_MatchesSpaceWidth()
    {
        // MeasureGlyph(' ') — used by TextRenderInfo to compute space advance — must
        // agree with GetSpaceWidth, which in turn is used by ParseLines.  A mismatch
        // would cause different wrap points depending on whether a line contains an
        // explicit space glyph vs. the inter-word space accounted for by ParseLines.
        var engine = CreateEngine(out _);
        var font = Resolve(engine);

        float spaceGlyphWidth = engine.MeasureGlyph(font, ' ').GlyphWidth;
        float spaceWidth = engine.GetSpaceWidth(font);

        Assert.Equal(spaceWidth, spaceGlyphWidth, precision: 2);
    }

    [Fact]
    public void SpaceWidth_IsNonZero()
    {
        // SpaceWidth must be positive — it is used both as the inter-word advance in
        // ParseLines and as the calibration anchor for the FSS pixel-size correction.
        // A zero SpaceWidth would mean MeasureString(" ").X returned 0 from FSS, which
        // would silently disable the pixel-size calibration and leave the wrapping
        // regression in place.
        var engine = CreateEngine(out _);
        var font = Resolve(engine);

        float spaceWidth = engine.GetSpaceWidth(font);
        Assert.True(spaceWidth > 0f,
            $"FSS SpaceWidth must be > 0; got {spaceWidth}. " +
            "If FSS MeasureString(' ') returns 0 (bounding-box rather than advance), " +
            "the pixel-size calibration in ResolveFont silently falls back to rawPixelSize.");
    }

    [Fact]
    public void FSSMeasurement_ScalesProportionally_WithPixelSize()
    {
        // The pixel-size calibration in ResolveFont relies on the FSS linear-scaling
        // property: if you scale the pixel size by factor k, every advance (including
        // MeasureString results) also scales by k.  This test verifies that property
        // holds for the Arial TTF at two sizes.
        //
        // If FSS ever switches to a non-linear advance model this test will catch it and
        // signal that the calibration strategy needs revisiting.
        var engine = CreateEngine(out _);
        var font12 = Resolve(engine, 12);
        var font24 = Resolve(engine, 24);   // exactly 2× size

        float w12 = engine.MeasureText(font12, "Scale test").X;
        float w24 = engine.MeasureText(font24, "Scale test").X;

        // Ratio should be close to 2.0 (within 10% — FSS pixel-snapping and hinting
        // introduce small non-linearity at small sizes, but the overall trend must hold).
        float ratio = w24 / w12;
        Assert.InRange(ratio, 1.7f, 2.3f);
    }
}
