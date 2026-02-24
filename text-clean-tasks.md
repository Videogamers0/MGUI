# Text System – Cleanup & Fix Tasks

Findings from a deep analysis of the text measurement, layout, and rendering code
in `MGUI.Core/UI/Text/` and `MGUI.Core/UI/MGTextBlock.cs`.

Each task is self-contained and ordered by priority.
Estimated impact: **HIGH** = visible bugs or measurement drift, **MEDIUM** = technical debt / inconsistency, **LOW** = dead code / cosmetic.

---

## T1 – [HIGH] Unify measurement path in `MGTextBlock.MeasureText`

**Problem:**  
`MGTextBlock.MeasureText` (line 621) sums per-character `GlyphMetrics.TotalWidth`
via `engine.MeasureGlyph(resolved, c)`. This is mathematically wrong for SpriteFont
because `SpriteFont.MeasureString` accounts for pair-wise kerning offsets that a
glyph-by-glyph sum cannot reproduce.  This is the root cause of the MGUI dev's
reported issue: *"measuring a set of substrings and summing their measurements
wasn't always giving the same width as measuring the whole string at once"*.

**Fix:**  
Replace the per-glyph loop with a single call to `engine.MeasureText(resolved, text)`:

```csharp
public Vector2 MeasureText(string Text, bool IsBold, bool IsItalic,
                            bool IgnoreFirstGlyphNegativeLeftSideBearing)
{
    if (string.IsNullOrEmpty(Text))
        return Vector2.Zero;

    ResolvedFont resolved = GetResolvedFont(IsBold, IsItalic);
    return TextEngine.MeasureText(resolved, Text);
}
```

`IgnoreFirstGlyphNegativeLeftSideBearing` can be dropped here because
`SpriteFontTextEngine.MeasureText` already delegates to `SpriteFont.MeasureString`
which applies that logic internally.

**Files:** `MGUI.Core/UI/MGTextBlock.cs` (lines 620-643)

---

## T2 – [HIGH] Fix per-run font style in `TextRenderInfo.UpdateLines`

**Problem:**  
`TextRenderInfo.UpdateLines` resolves a *single* `ResolvedFont` at line 62 using
`TextBlockElement.IsBold / TextBlockElement.IsItalic` (the base style of the
TextBlock). It then iterates over all runs and measures every character with that
same font, even though each `MGTextRunText` has its own
`Run.Settings.IsBold / IsItalic`.

This means inline bold/italic/mixed spans are measured with the wrong font,
producing incorrect caret positioning in `MGTextBox`.

**Fix:**  
Resolve the font *inside* the per-run loop:

```csharp
foreach (MGTextRunText Run in Runs)
{
    ITextEngine engine = TextBlockElement.GetTextEngine();
    ResolvedFont resolved = TextBlockElement.GetResolvedFont(
        Run.Settings.IsBold, Run.Settings.IsItalic);
    bool IsStartOfLine = true;
    foreach (char c in Run.Text) { ... }
}
```

**Files:** `MGUI.Core/UI/Text/TextRenderInfo.cs` (lines 60-115)

---

## T3 – [HIGH] Remove dual-measurement in `FlushLine`

**Problem:**  
`MGTextLine.FlushLine` (line ~290) computes `LineWidth` as:
```csharp
float LineWidth = Math.Max(CurrentX,
    TextRunSizes.Sum(x => x.X) + ImageRunSizes.Sum(x => x.X));
```
`CurrentX` was accumulated word-by-word during wrapping, while `TextRunSizes` is a
*separate* re-measurement of each run's full text. These two values can disagree
(because of the T1 issue, or rounding), and `Math.Max` silently hides the
discrepancy.

**Fix:**  
Once T1 is applied, the two paths should agree.  Replace the `Math.Max` with a
single authoritative measurement – use `TextRunSizes` (re-measured per-run) and drop
the redundant `CurrentX` accumulation, or simply set `LineWidth = CurrentX` and
remove the `TextRunSizes` re-measurement inside `FlushLine`.

**Files:** `MGUI.Core/UI/Text/MGTextLine.cs` (`FlushLine`, lines ~280-300)

---

## T4 – [MEDIUM] Generalise `ITextMeasurer` interface

**Problem:**  
`ITextMeasurer.MeasureText` accepts a parameter
`IgnoreFirstGlyphNegativeLeftSideBearing` whose semantics are SpriteFont-specific
(the XML doc even links to MonoGame SpriteFont source code). A FontStashSharp or
other engine does not have this concept.

**Fix options (pick one):**

1. **Remove the parameter entirely** – after T1, `MeasureText` delegates to
   `engine.MeasureText(resolved, text)` which handles first-glyph behaviour
   internally.  The parameter is no longer needed; its only caller is `ParseLines`.
2. **Rename to `IsFirstRunOnLine`** – keep the semantic but phrase it in
   engine-agnostic terms.

**Files:** `MGUI.Core/UI/Text/MGTextLine.cs` (interface, lines 14-27),
`MGUI.Core/UI/MGTextBlock.cs` (implementation)

---

## T5 – [MEDIUM] Drop redundant re-measurement in `DrawTransaction.DrawText`

**Problem:**  
`DrawTransaction.DrawText(Family, Style, Text, ...)` (line ~254) first draws the
text, then calls `TextEngine.MeasureText` to return the visual size.
This is a convenience for callers like `DrawShadowedText`, but any code path that
already knows the size pays an extra measurement that allocates a `Vector2` +
font resolve for nothing.

**Fix:**  
Add an overload that does not return size (void), or cache the measurement inside
the method and offer a `TryMeasure` variant. Alternatively, accept it as acceptable
overhead and mark with a comment. Low real-world impact.

**Files:** `MGUI.Shared/Rendering/DrawTransaction.cs` (lines 246-270)

---

## T6 – [MEDIUM] Use `TextRenderInfo` with `MeasureText` (whole-string) instead of per-glyph

**Problem:**  
Even after T2 fixes the per-run font, `TextRenderInfo.UpdateLines` still measures
each character individually with `engine.MeasureGlyph`. For caret positioning this
is fine (you need per-char X offsets), but the accumulated widths can drift from the
whole-string measurement used for line layout.

**Fix:**  
For caret X-positions, keep per-glyph advances.  But reconcile the total with
`engine.MeasureText(resolved, runText)` and distribute any residual evenly across
characters, so the last character's right edge equals the whole-string width.
This guarantees the caret at end-of-run aligns exactly with where the next run
starts.

**Files:** `MGUI.Core/UI/Text/TextRenderInfo.cs` (lines 95-115)

---

## T7 – [LOW] Remove legacy SpriteFont fields from `MGTextBlock`

**Problem:**  
After the ITextEngine migration, these fields are only used in the `#if false` dead
code path (old `DrawSelf`) and the obsolete `DrawSpriteFontText` shim:

- `SF_Regular`, `SF_Bold`, `SF_Italic`, `SF_BoldItalic`
- `SF_Regular_Glyphs` … `SF_BoldItalic_Glyphs`
- `GetFont(bool, bool, out Glyphs)` method
- `FontScale`, `FontOrigin`, `FontHeight` (superseded by `ResolvedFont.*`)
- `SpaceWidth` computed from `SF_Regular` (duplicate of `RF_Regular.SpaceWidth`)

**Fix:**  
Delete the `SF_*` fields, `GetFont`, `SF_*_Glyphs`, and the `#if false` / `#if NEVER`
blocks.  Redirect the 3 remaining callers of `DrawSpriteFontText` in the old DrawSelf
to `DrawTextViaEngine`.  Keep `SpaceWidth` if needed, but compute it from
`RF_Regular.SpaceWidth`.

**Files:** `MGUI.Core/UI/MGTextBlock.cs` (lines 38-95, 108-120, 776-845)

---

## T8 – [LOW] Remove dead `#if false` / `#if NEVER` DrawSelf block

**Problem:**  
There are two `DrawSelf` overrides guarded by `#if false //true` (line 776) and
`#if NEVER` (line 857). These are 250+ lines of unreachable code that dilute
readability.

**Fix:**  
Delete both dead blocks entirely. If the experimental window-scale logic is needed
in the future, it can be recovered from git history.

**Files:** `MGUI.Core/UI/MGTextBlock.cs` (lines 776-1020)

---

## T9 – [LOW] Optimise `FontStashSharpTextEngine.MeasureGlyph`

**Problem:**  
`FontStashSharpTextEngine.MeasureGlyph(resolved, c)` calls
`font.MeasureString(c.ToString())` — allocating a `string` per character.
In `TextRenderInfo.UpdateLines` this is called once per character of the entire
TextBox content on every layout pass.

**Fix:**  
Cache single-character strings (`char → string` dictionary or `stackalloc` +
`Span<char>`), or use the FontStashSharp `DynamicSpriteFont.GetGlyphs()` API to
read advance widths directly without string allocation.

**Files:** `MGUI.FontStashSharp/FontStashSharpTextEngine.cs`

---

## Summary of dependencies

```
T1 ──► T3  (FlushLine fix depends on MeasureText fix)
T1 ──► T4  (ITextMeasurer param removal depends on MeasureText fix)
T2       (independent)
T5       (independent)
T6 ──► T1, T2  (caret reconciliation needs correct per-run + whole-string)
T7 ──► T8  (remove fields before or with dead code blocks)
T9       (independent, FontStashSharp project)
```

Recommended order: **T1 → T2 → T3 → T4 → T6 → T5 → T7 → T8 → T9**
