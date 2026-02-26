using MGUI.Core.UI;
using MGUI.FontStashSharp;
using MGUI.Shared.Text;
using MGUI.Shared.Text.Engines;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MGUI.Samples
{
    /// <summary>
    /// Debug utility that prints side-by-side metrics from <see cref="SpriteFontTextEngine"/>
    /// and <see cref="FontStashSharpTextEngine"/> to the Debug output window.
    /// Call once after both engines are initialized.
    /// </summary>
    public static class TextEngineDiagnostic
    {
        // ── Test corpus ─────────────────────────────────────────────────────────

        private static readonly int[] DiagSizes =
            { 8, 9, 10, 11, 12, 13, 14, 16, 18, 20, 24, 36, 48 };

        private static readonly int[] WidthTestSizes = { 10, 12, 14, 20, 24 };

        private static readonly string[] WidthTestStrings =
        {
            "A",
            "Hello World",
            "Stardew Valley",
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
            "The quick brown fox jumps over the lazy dog"
        };

        private const int GlyphTestSize    = 20;
        private const string GlyphTestStr  = "Hello World";

        // ── Entry point ─────────────────────────────────────────────────────────

        /// <summary>
        /// Runs all comparisons and writes the results to <see cref="Debug"/>.
        /// </summary>
        public static void Run(
            SpriteFontTextEngine       sfEngine,
            FontStashSharpTextEngine   fssEngine)
        {
            if (sfEngine  == null) { Debug.WriteLine("[Diagnostic] sfEngine is null — aborting."); return; }
            if (fssEngine == null) { Debug.WriteLine("[Diagnostic] fssEngine is null — aborting."); return; }

            Debug.WriteLine(new string('=', 110));
            Debug.WriteLine("  ITextEngine diagnostic — SpriteFontTextEngine vs FontStashSharpTextEngine");
            Debug.WriteLine(new string('=', 110));

            RunLineHeightTable(sfEngine, fssEngine);
            RunWidthTable(sfEngine, fssEngine);
            RunGlyphTable(sfEngine, fssEngine);
            RunSpacingTable(sfEngine, fssEngine);

            Debug.WriteLine(new string('=', 110));
            Debug.WriteLine("  Diagnostic complete.");
            Debug.WriteLine(new string('=', 110));
        }

        // ── Section 1 — LineHeight / SpaceWidth / DrawOrigin ────────────────────

        private static void RunLineHeightTable(
            SpriteFontTextEngine sfEngine, FontStashSharpTextEngine fssEngine)
        {
            Debug.WriteLine("");
            Debug.WriteLine("── SECTION 1 : LineHeight / SpaceWidth / DrawOrigin.Y ──────────────────────");
            Debug.WriteLine(
                $"{"Size",4} | {"SF.LH",8} {"FSS.LH",8} {"ΔLH",8} " +
                $"| {"SF.SW",8} {"FSS.SW",8} {"ΔSW",8} " +
                $"| {"SF.OY",7} {"FSS.OY",7} {"ΔOY",7}");
            Debug.WriteLine(new string('-', 90));

            foreach (int size in DiagSizes)
            {
                var spec = new FontSpec("Arial", size, CustomFontStyles.Normal);
                ResolvedFont sf  = sfEngine .ResolveFont(spec);
                ResolvedFont fss = fssEngine.ResolveFont(spec);

                float deltaLH  = fss.LineHeight  - sf.LineHeight;
                float deltaSW  = fss.SpaceWidth  - sf.SpaceWidth;
                float deltaOY  = fss.DrawOrigin.Y - sf.DrawOrigin.Y;

                Debug.WriteLine(
                    $"{size,4} | " +
                    $"{sf.LineHeight,8:F2} {fss.LineHeight,8:F2} {deltaLH,+8:F2} | " +
                    $"{sf.SpaceWidth,8:F2} {fss.SpaceWidth,8:F2} {deltaSW,+8:F2} | " +
                    $"{sf.DrawOrigin.Y,7:F2} {fss.DrawOrigin.Y,7:F2} {deltaOY,+7:F2}");
            }
        }

        // ── Section 2 — MeasureText widths ──────────────────────────────────────

        private static void RunWidthTable(
            SpriteFontTextEngine sfEngine, FontStashSharpTextEngine fssEngine)
        {
            Debug.WriteLine("");
            Debug.WriteLine("── SECTION 2 : MeasureText widths ──────────────────────────────────────────");
            Debug.WriteLine(
                $"{"Size",4} | {"SF.W",8} {"FSS.W",8} {"ΔW",+8} | {"SF.H",8} {"FSS.H",8} {"ΔH",+8} | String");
            Debug.WriteLine(new string('-', 100));

            foreach (int size in WidthTestSizes)
            {
                var spec = new FontSpec("Arial", size, CustomFontStyles.Normal);
                ResolvedFont sf  = sfEngine .ResolveFont(spec);
                ResolvedFont fss = fssEngine.ResolveFont(spec);

                foreach (string text in WidthTestStrings)
                {
                    var sfSize  = sfEngine .MeasureText(sf,  text);
                    var fssSize = fssEngine.MeasureText(fss, text);

                    float dw = fssSize.X - sfSize.X;
                    float dh = fssSize.Y - sfSize.Y;

                    string label = text.Length > 30 ? text[..27] + "..." : text;
                    Debug.WriteLine(
                        $"{size,4} | " +
                        $"{sfSize.X,8:F2} {fssSize.X,8:F2} {dw,+8:F2} | " +
                        $"{sfSize.Y,8:F2} {fssSize.Y,8:F2} {dh,+8:F2} | \"{label}\"");
                }
                Debug.WriteLine(new string('-', 100));
            }
        }

        // ── Section 3 — MeasureGlyph per character ──────────────────────────────

        private static void RunGlyphTable(
            SpriteFontTextEngine sfEngine, FontStashSharpTextEngine fssEngine)
        {
            Debug.WriteLine("");
            Debug.WriteLine($"── SECTION 3 : MeasureGlyph per char — \"{GlyphTestStr}\" @ size {GlyphTestSize} ──────────────");
            Debug.WriteLine(
                $"{"Ch",3} | " +
                $"{"SF.LSB",7} {"SF.W",7} {"SF.RSB",7} {"SF.Tot",7} | " +
                $"{"FSS.LSB",7} {"FSS.W",7} {"FSS.RSB",7} {"FSS.Tot",7} | " +
                $"{"ΔTotal",+7}");
            Debug.WriteLine(new string('-', 90));

            var spec = new FontSpec("Arial", GlyphTestSize, CustomFontStyles.Normal);
            ResolvedFont sf  = sfEngine .ResolveFont(spec);
            ResolvedFont fss = fssEngine.ResolveFont(spec);

            foreach (char c in GlyphTestStr)
            {
                GlyphMetrics gSF  = sfEngine .MeasureGlyph(sf,  c);
                GlyphMetrics gFSS = fssEngine.MeasureGlyph(fss, c);

                float dTotal = gFSS.TotalWidth - gSF.TotalWidth;

                Debug.WriteLine(
                    $"'{c}' | " +
                    $"{gSF.LeftSideBearing,7:F2} {gSF.GlyphWidth,7:F2} {gSF.RightSideBearing,7:F2} {gSF.TotalWidth,7:F2} | " +
                    $"{gFSS.LeftSideBearing,7:F2} {gFSS.GlyphWidth,7:F2} {gFSS.RightSideBearing,7:F2} {gFSS.TotalWidth,7:F2} | " +
                    $"{dTotal,+7:F2}");
            }

            // also check sum vs whole-string measure
            float sfSum  = GlyphTestStr.Sum(c => sfEngine .MeasureGlyph(sf,  c).TotalWidth);
            float fssSum = GlyphTestStr.Sum(c => fssEngine.MeasureGlyph(fss, c).TotalWidth);
            float sfWhole  = sfEngine .MeasureText(sf,  GlyphTestStr).X;
            float fssWhole = fssEngine.MeasureText(fss, GlyphTestStr).X;

            Debug.WriteLine(new string('-', 90));
            Debug.WriteLine($"{"Sum",3} | {"",7} {"",7} {"",7} {sfSum,7:F2} | {"",7} {"",7} {"",7} {fssSum,7:F2} | {fssSum-sfSum,+7:F2}  (sum of TotalWidth)");
            Debug.WriteLine($"{"Str",3} | {"",7} {"",7} {"",7} {sfWhole,7:F2} | {"",7} {"",7} {"",7} {fssWhole,7:F2} | {fssWhole-sfWhole,+7:F2}  (MeasureText whole string)");
        }

        // ── Section 4 — SpriteFont.Spacing ──────────────────────────────────────

        private static void RunSpacingTable(
            SpriteFontTextEngine sfEngine, FontStashSharpTextEngine fssEngine)
        {
            Debug.WriteLine("");
            Debug.WriteLine("── SECTION 4 : SpriteFont.Spacing (inter-glyph spacing) ─────────────────────");
            Debug.WriteLine($"{"Size",4} | {"SF.Spacing",10} | {"SF.ExactScale",13} | {"SF.SuggestedScale",17}");
            Debug.WriteLine(new string('-', 60));

            foreach (int size in DiagSizes)
            {
                var spec   = new FontSpec("Arial", size, CustomFontStyles.Normal);
                ResolvedFont sf = sfEngine.ResolveFont(spec);

                // SpriteFontHandle is a private type — read SF.Spacing via reflection.
                float spacing = GetSpacingViaReflection(sf.NativeFont);

                Debug.WriteLine(
                    $"{size,4} | {spacing,10:F1} | {sf.ExactScale,13:F4} | {sf.SuggestedScale,17:F4}");
            }
        }

        /// <summary>
        /// Reads <c>SpriteFont.Spacing</c> from the opaque <c>SpriteFontHandle</c> object
        /// stored in <see cref="ResolvedFont.NativeFont"/> using reflection.
        /// This is intentionally only used in the debug diagnostic — production code should
        /// never rely on reflection to access private types.
        /// </summary>
        private static float GetSpacingViaReflection(object nativeFont)
        {
            if (nativeFont == null) return 0f;
            try
            {
                PropertyInfo sfProp = nativeFont.GetType().GetProperty(
                    "SF", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (sfProp?.GetValue(nativeFont) is SpriteFont sf)
                    return sf.Spacing;
            }
            catch { /* ignore — diagnostic only */ }
            return 0f;
        }
    }
}
