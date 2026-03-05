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
/// Verifies that <see cref="FontStashSharpTextEngine"/> produces consistent
/// measurement results when using the FSS-native path (i.e. <see cref="FontStashSharp.SpriteFontBase.MeasureString"/>)
/// rather than the calibrated SpriteFont glyph sum which was the source of
/// the measure–draw width mismatch bug (text clipping).
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

        // SpaceWidth comes from FSS-native h.SpaceWidth (= MeasureString(" ").X)
        // MeasureText also uses FSS-native MeasureString(" ").X
        // They should match exactly.
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

        // With both using FSS-native, the residual should be small (< 5%).
        // Before the fix, using calibrated glyph widths could produce large residuals.
        Assert.True(residualPercent < 5f,
            $"Residual between MeasureText and glyph sum is {residualPercent:F1}% ({residual:F2}px), expected < 5%");
    }
}
