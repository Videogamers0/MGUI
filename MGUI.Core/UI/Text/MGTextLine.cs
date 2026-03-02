using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MGUI.Shared.Helpers;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace MGUI.Core.UI.Text
{
    public interface ITextMeasurer
    {
        /// <summary>
        /// Measures the rendered size of <paramref name="Text"/> using the given font style.
        /// Implementations must use a whole-string measurement (not per-glyph summation) so that
        /// kerning between characters is accounted for correctly.
        /// </summary>
        Vector2 MeasureText(string Text, bool IsBold, bool IsItalic);
    }

    public record class MGTextLine
    {
        public ReadOnlyCollection<MGTextRun> Runs { get; }

        public float LineWidth { get; }

        /// <summary>The height of the text-content of this line.<para/>
        /// If the line only includes text-content (See: <see cref="MGTextRunText"/>), then this value is typically equivalent to <see cref="LineTotalHeight"/> (unless the line has a MinimumHeight, which is usually the height of a space ' ' character).<br/>
        /// If the line includes images (See: <see cref="MGTextRunImage"/>), then this value may differ from <see cref="LineTotalHeight"/>, depending on if the tallest image is taller than the tallest piece of text.</summary>
        public float LineTextHeight { get; }

        /// <summary>The height of the image-content of this line.<para/>
        /// If the line only includes image-content (See: <see cref="MGTextRunImage"/>), then this value is equivalent to <see cref="LineTotalHeight"/> (unless the line has a MinimumHeight, which is usually the height of a space ' ' character).<br/>
        /// If the line includes text (See: <see cref="MGTextRunText"/>), then this value may differ from <see cref="LineTotalHeight"/>, depending on if the tallest piece of text is taller than the tallest image.</summary>
        public float LineImageHeight { get; }

        /// <summary>The total height of all content of this line.<para/>
        /// If the line only includes text-content (See: <see cref="MGTextRunText"/>), then this value is typically equivalent to <see cref="LineTextHeight"/>.<br/>
        /// If the line only includes image-content (See: <see cref="MGTextRunImage"/>), then this value is typically equivalent to <see cref="LineImageHeight"/>.<br/>
        /// If the line includes both text and images, then represents the larger value of <see cref="LineTextHeight"/> and <see cref="LineImageHeight"/>.<para/>
        /// Note: In some cases, the line may have a MinimumHeight (usually the height of a space ' ' character)<br/>
        /// which may cause this value to be larger than <see cref="LineTextHeight"/> and/or <see cref="LineImageHeight"/></summary>
        public float LineTotalHeight { get; }

        /// <summary>The 1-based line number. This value does not use 0-based indexing.</summary>
        public int LineNumber { get; }

        /// <summary>True if the end of this line is due to a linebreak. False if the end of this line is due to text wrapping or because there is no more text afterwards.</summary>
        public bool EndsInLinebreakCharacter { get; }

        public ReadOnlyCollection<int> OriginalCharacterIndices { get; }

        /// <param name="Indices">The indices of the original character that each character in the lines corresponds to. Only used for very specific purposes.<para/>
        /// EX: If input is a run with Text = "ABC12345", and that text couldn't fit on a single line, it may be split to something like: [ "ABC12-", "345" ]<br/>
        /// Notice that a '-' character was automatically inserted at the end of the first line, so now the wrapped text at index=5 is '-', while the wrapped text in the input "ABC12345" at index=5 is '3'.<br/>
        /// So the indices mapping in this case would be: [ 0, 1, 2, 3, 4, 4, 5, 6, 7 ]<para/>
        /// This parameter is intended to know to which character in the original input corresponds to each character in the wrapped output.</param>
        public MGTextLine(IEnumerable<MGTextRun> Runs, float LineWidth, float LineTextHeight, float LineImageHeight, float LineTotalHeight, int LineNumber, bool EndsInLinebreakCharacter, IEnumerable<int> Indices)
        {
            this.Runs = Runs.ToList().AsReadOnly();
            this.LineWidth = LineWidth;
            this.LineTextHeight = LineTextHeight;
            this.LineTotalHeight = LineTotalHeight;
            this.LineImageHeight = LineImageHeight;
            this.LineNumber = LineNumber;
            this.EndsInLinebreakCharacter = EndsInLinebreakCharacter;
            OriginalCharacterIndices = Indices.ToList().AsReadOnly();
        }

        private class WrappableRunGroup
        {
            public readonly ReadOnlyCollection<char> WordDelimiters;
            public readonly ReadOnlyCollection<MGTextRun> OriginalRuns;
            public readonly ReadOnlyCollection<WrappableRun> WrappableRuns;
            public readonly List<WrappableRun> RemainingRuns;

            public bool IsDelimiterWord(string Word) => WordDelimiters.Any(x => x.ToString() == Word);

            public WrappableRunGroup(IEnumerable<char> WordDelimiters, IEnumerable<MGTextRun> OriginalRuns)
            {
                this.WordDelimiters = WordDelimiters.ToList().AsReadOnly();
                this.OriginalRuns = OriginalRuns.ToList().AsReadOnly();

                WrappableRuns = OriginalRuns.Where(x => x.RunType != TextRunType.Text || (!string.IsNullOrEmpty(((MGTextRunText)x).Text)))
                    .Select(Run => new WrappableRun(this, Run)).ToList().AsReadOnly();

                for (int i = 0; i < WrappableRuns.Count; i++)
                {
                    WrappableRun Current = WrappableRuns[i];
                    WrappableRun Next = i == WrappableRuns.Count - 1 ? null : WrappableRuns[i + 1];

                    if (Next != null && Current.IsText && Next.IsText)
                    {
                        WrappableRunWord LastWordOfCurrent = Current.OriginalWords[^1];
                        WrappableRunWord FirstWordOfNext = Next.OriginalWords[0];

                        if (!IsDelimiterWord(LastWordOfCurrent.Text) && !IsDelimiterWord(FirstWordOfNext.Text))
                        {
                            LastWordOfCurrent.HasNext = true;
                        }
                    }
                }

                RemainingRuns = new(WrappableRuns);
            }

            public WrappableRun GetNext(WrappableRun Current)
            {
                int Index = RemainingRuns.IndexOf(Current);
                int NextIndex = Index + 1;
                if (RemainingRuns.Count > NextIndex)
                    return RemainingRuns[NextIndex];
                else
                    return null;
            }
        }

        private class WrappableRun
        {
            public readonly WrappableRunGroup Group;
            public readonly MGTextRun OriginalRun;

            public TextRunType RunType => OriginalRun.RunType;
            public bool IsText => RunType == TextRunType.Text;
            public bool IsLineBreak => RunType == TextRunType.LineBreak;
            public bool IsImage => RunType == TextRunType.Image;

            public readonly List<WrappableRunWord> OriginalWords;
            public readonly List<WrappableRunWord> RemainingWords;

            public WrappableRun(WrappableRunGroup Group, MGTextRun OriginalRun)
            {
                this.Group = Group;
                this.OriginalRun = OriginalRun;

                if (OriginalRun is MGTextRunText TextRun)
                {
                    List<string> Words = TextRun.Text.SplitAndKeepDelimiters(Group.WordDelimiters).ToList();
                    OriginalWords = Words.Select(x => new WrappableRunWord(this, x, TextRun.Settings, false)).ToList();
                }
                else
                    OriginalWords = new();

                RemainingWords = new List<WrappableRunWord>(OriginalWords);
            }

            public MGTextRunText AsTextRun(string Text)
            {
                if (!IsText)
                    throw new InvalidOperationException();
                else
                    return new MGTextRunText(Text, ((MGTextRunText)OriginalRun).Settings, OriginalRun.ToolTipId, OriginalRun.ActionId);
            }

            public string GetAllRemainingText() => string.Join("", RemainingWords.Select(x => x.Text));

            public WrappableRunWord GetNext(WrappableRunWord Current)
            {
                int Index = RemainingWords.IndexOf(Current);
                int NextIndex = Index + 1;
                if (RemainingWords.Count > NextIndex)
                    return RemainingWords[NextIndex];
                else
                    return Group.GetNext(this).RemainingWords[0];
            }

            public override string ToString() =>
                RunType switch
                {
                    TextRunType.Text => $"{nameof(WrappableRun)}: {GetAllRemainingText()}",
                    TextRunType.LineBreak => $"{nameof(WrappableRun)}: LineBreak",
                    TextRunType.Image => $"{nameof(WrappableRun)}: Image",
                    _ => throw new NotImplementedException($"Unrecognized {nameof(TextRunType)}: {RunType}")
                };
        }

        private class WrappableRunWord
        {
            public readonly WrappableRun Run;
            public readonly string Text;
            public bool HasNext { get; set; }

            private readonly MGTextRunConfig Settings;
            public bool IsBold => Settings.IsBold;
            public bool IsItalic => Settings.IsItalic;

            public WrappableRunWord(WrappableRun Run, string Text, MGTextRunConfig Settings, bool HasNext)
            {
                this.Run = Run;
                this.Text = Text;
                this.Settings = Settings;
                this.HasNext = HasNext;
            }

            public IEnumerable<WrappableRunWord> GetNextWords(bool IncludeSelf)
            {
                if (IncludeSelf)
                    yield return this;

                WrappableRunWord Current = this;
                while (Current.HasNext)
                {
                    Current = Current.GetNextWord();
                    yield return Current;
                }
            }

            public WrappableRunWord GetNextWord() => Run.GetNext(this);

            public override string ToString() => $"{Text} ({nameof(HasNext)}={HasNext})";
        }

        /// <param name="IgnoreEmptySpaceLines">If true, lines consisting of only a single space character will be ignored, unless it is the first line or if it immediately follows a linebreak.<para/>
        /// For example, if the line could fit exactly 5 characters, and the text is "Hello World", the result would normally be:
        /// <code>
        /// -------<br/>
        /// |Hello|<br/>
        /// | ....| (This line only contains a space)<br/>
        /// |World|<br/>
        /// -------</code>
        /// If <paramref name="IgnoreEmptySpaceLines"/> is true, it would instead result in:
        /// <code>
        /// -------<br/>
        /// |Hello|<br/>
        /// |World|<br/>
        /// -------</code>
        /// Note that lines consisting of multiple consecutive spaces will still be returned.</param>
        public static IEnumerable<MGTextLine> ParseLines(ITextMeasurer Measurer, double MaxLineWidth, bool WrapText, IEnumerable<MGTextRun> Runs, bool IgnoreEmptySpaceLines)
            => ParseLines(Measurer, MaxLineWidth, WrapText, Runs, IgnoreEmptySpaceLines, ' ', '-');

        /// <param name="IgnoreEmptySpaceLines">If true, lines consisting of only a single space character will be ignored, unless it is the first line or if it immediately follows a linebreak.<para/>
        /// For example, if the line could fit exactly 5 characters, and the text is "Hello World", the result would normally be:
        /// <code>
        /// -------<br/>
        /// |Hello|<br/>
        /// | ....| (This line only contains a space)<br/>
        /// |World|<br/>
        /// -------</code>
        /// If <paramref name="IgnoreEmptySpaceLines"/> is true, it would instead result in:
        /// <code>
        /// -------<br/>
        /// |Hello|<br/>
        /// |World|<br/>
        /// -------</code>
        /// Note that lines consisting of multiple consecutive spaces will still be returned.</param>
        /// <param name="WordDelimiters">Recommended: ' ' (space) and '-' (hyphen)</param>
        /// <remarks>
        /// <b>Known non-idempotency issue (PR #35):</b><br/>
        /// The wrapping <i>decision</i> uses a word-by-word accumulated width (<c>CurrentX</c>),
        /// while the resulting <see cref="MGTextLine.LineWidth"/> is computed by re-measuring each
        /// run as a <i>complete string</i> inside <c>FlushLine</c>.<br/>
        /// Because a whole-string measurement can be smaller than the sum of its word-by-word
        /// measurements (inter-glyph kerning), calling <c>ParseLines(W)</c> may produce a line
        /// with <c>LineWidth = W' &lt; W</c>.  Calling <c>ParseLines(W')</c> then wraps again,
        /// because the per-word sum still exceeds W'.<br/>
        /// <br/>
        /// Fix strategy (Task 3): align the wrapping decision with <c>FlushLine</c> by measuring
        /// the candidate line as a whole string before deciding whether to wrap.
        /// </remarks>
        public static IEnumerable<MGTextLine> ParseLines(ITextMeasurer Measurer, double MaxLineWidth, bool WrapText, IEnumerable<MGTextRun> Runs, bool IgnoreEmptySpaceLines, params char[] WordDelimiters)
        {
            if (Runs?.Any() != true || MaxLineWidth < 1)
                yield break;

            const string MultiLineWordSuffix = "-"; // A suffix to append to the end of a line, when the line only consists of a single word that must wrap across multiple lines

            float MinLineHeight = (float)Math.Ceiling(Measurer.MeasureText(" ", false, false).Y);
            int LineNumber = 1;
            List<MGTextRun> CurrentLine = new();
            float CurrentX = 0;
            // Whole-string sum of the widths of runs already committed to CurrentLine.
            // Updated whenever a run/image is added to CurrentLine without a direct flush, and reset
            // to 0 inside FlushLine.  Used by the idempotent wrapping decision below.
            float CommittedRunsWidth = 0;
            List<int> CurrentIndicesMap = new();
            int CurrentIndexInOriginalText = 0;

            MGTextLine PreviousLine = null;

            bool FlushLine(out MGTextLine Line, bool EndsInLinebreakCharacter, int LineBreakCharacterCount = 1)
            {
                if (!CurrentLine.Any())
                    throw new InvalidOperationException($"Cannot create an {nameof(MGTextLine)} with no {nameof(MGTextRun)}s.");

                if (EndsInLinebreakCharacter)
                {
                    for (int i = 0; i < LineBreakCharacterCount; i++)
                    {
                        CurrentIndicesMap.Add(CurrentIndexInOriginalText);
                        CurrentIndexInOriginalText++;
                    }
                }
                else if (CurrentLine.Where(x => x.RunType == TextRunType.Text && x is MGTextRunText).Cast<MGTextRunText>().All(x => string.IsNullOrEmpty(x.Text)))
                {
                    CurrentIndicesMap.Add(CurrentIndexInOriginalText);
                }

                // Each run is measured as a complete string (whole-string MeasureText) so inter-word
                // kerning within a run is captured correctly.  The wrapping-accumulated CurrentX is
                // discarded because it was built word-by-word and can differ from the per-run whole-string
                // measurement after kerning adjustments.
                List<Vector2> TextRunSizes = CurrentLine.Where(x => x.RunType == TextRunType.Text).Cast<MGTextRunText>()
                    .Select(x => Measurer.MeasureText(x.Text, x.Settings.IsBold, x.Settings.IsItalic)).ToList();
                List<Vector2> ImageRunSizes = CurrentLine.Where(x => x.RunType == TextRunType.Image).Cast<MGTextRunImage>()
                    .Select(x => new Vector2(x.TargetWidth, x.TargetHeight)).ToList();

                float LineWidth = TextRunSizes.Sum(x => x.X) + ImageRunSizes.Sum(x => x.X);
                float LineTextHeight = TextRunSizes.DefaultIfEmpty(Vector2.Zero).Max(x => x.Y);
                float LineImageHeight = ImageRunSizes.DefaultIfEmpty(Vector2.Zero).Max(x => x.Y);
                float LineTotalHeight = GeneralUtils.Max(MinLineHeight, LineTextHeight, LineImageHeight);

                Line = new(CurrentLine, LineWidth, LineTextHeight, LineImageHeight, LineTotalHeight, LineNumber, EndsInLinebreakCharacter, CurrentIndicesMap);
                LineNumber++;
                CurrentLine.Clear();
                CurrentIndicesMap.Clear();
                CurrentX = 0;
                CommittedRunsWidth = 0;

                bool IsIgnoreable = IgnoreEmptySpaceLines && PreviousLine != null && !PreviousLine.EndsInLinebreakCharacter &&
                    Line.Runs.Count == 1 && Line.Runs.First() is MGTextRunText TextRun && TextRun.Text == " ";
                PreviousLine = Line;
                return !IsIgnoreable;
            }

            MGTextLine Line;
            WrappableRunGroup Group = new(WordDelimiters, Runs);

            while (Group.RemainingRuns.Any())
            {
                WrappableRun Run = Group.RemainingRuns.First();
                Group.RemainingRuns.RemoveAt(0);

                if (Run.IsLineBreak && Run.OriginalRun is MGTextRunLineBreak LineBreakRun)
                {
                    if (!CurrentLine.Any())
                        CurrentLine.Add(new MGTextRunText("", new MGTextRunConfig(false), null, null));
                    if (FlushLine(out Line, true, LineBreakRun.LineBreakCharacterCount))
                        yield return Line;
                }
                else if (Run.IsImage && Run.OriginalRun is MGTextRunImage ImageRun)
                {
                    if (!WrapText || MaxLineWidth == double.MaxValue || MaxLineWidth >= 100000)
                    {
                        CurrentLine.Add(ImageRun);
                    }
                    else
                    {
                        int ImgWidth = ImageRun.TargetWidth;
                        if (CurrentLine.Any() && CommittedRunsWidth + ImgWidth > MaxLineWidth && FlushLine(out Line, false))
                            yield return Line;

                        CurrentLine.Add(ImageRun);
                        CurrentX += ImgWidth;
                        CommittedRunsWidth += ImgWidth;
                    }
                }
                else if (Run.IsText && Run.OriginalRun is MGTextRunText TextRun)
                {
                    if (!WrapText || MaxLineWidth == double.MaxValue || MaxLineWidth >= 100000)
                    {
                        string Text = Run.GetAllRemainingText();
                        CurrentLine.Add(Run.AsTextRun(Text));
                        CommittedRunsWidth += Measurer.MeasureText(Text, TextRun.Settings.IsBold, TextRun.Settings.IsItalic).X;
                        foreach (char c in Text)
                        {
                            CurrentIndicesMap.Add(CurrentIndexInOriginalText);
                            CurrentIndexInOriginalText++;
                        }
                    }
                    else
                    {
                        StringBuilder UnwrappedText = new();
                        while (Run.RemainingWords.Any())
                        {
                            WrappableRunWord Current = Run.RemainingWords[0];

                            List<WrappableRunWord> CurrentAndNext = Current.GetNextWords(true).ToList();

                            //  Measure the next chunk to calculate word-wrapping on
                            //  This may be several consecutive items.
                            //  EX: "Hello[b]World" results in 2 runs: Run1="Hello", Run2="World" but there is no word delimiter (space) between them so it's a single word "HelloWorld"
                            List<Vector2> Measurements = CurrentAndNext.Select((Word, Index) =>
                                Measurer.MeasureText(Word.Text, Word.IsBold, Word.IsItalic)).ToList();
                            float CurrentWidth = Measurements[0].X;
                            float TotalWidth = Measurements.Sum(x => x.X);

                            // IDEMPOTENT WRAPPING DECISION
                            // We project the line width using the same whole-string-per-run measurement that
                            // FlushLine uses to compute LineWidth.  This ensures ParseLines(W') behaves
                            // identically to ParseLines(W) when W' = Max(LineWidth) of the previous pass.
                            //
                            // For intra-run candidates (all words belong to the current run) we measure
                            // UnwrappedText + candidate words as a single string — exactly what FlushLine
                            // would measure after the commit.
                            // For cross-run HasNext candidates (words from two adjacent runs with no
                            // delimiter between them) we sum per-run whole-string widths, again matching
                            // FlushLine's per-run accumulation.
                            float ProjectedWidth;
                            if (CurrentAndNext.All(w => w.Run == Run))
                            {
                                // Intra-run: whole-string measurement eliminates kerning divergence
                                string Candidate = UnwrappedText.ToString() + string.Concat(CurrentAndNext.Select(w => w.Text));
                                ProjectedWidth = CommittedRunsWidth
                                    + Measurer.MeasureText(Candidate, Current.IsBold, Current.IsItalic).X;
                            }
                            else
                            {
                                // Cross-run HasNext: sum per-run whole-string widths
                                bool IsFirstGroup = true;
                                ProjectedWidth = CommittedRunsWidth;
                                foreach (IGrouping<WrappableRun, WrappableRunWord> G in CurrentAndNext.GroupBy(w => w.Run))
                                {
                                    string GroupText = (IsFirstGroup ? UnwrappedText.ToString() : "")
                                        + string.Concat(G.Select(w => w.Text));
                                    ProjectedWidth += Measurer.MeasureText(GroupText, G.First().IsBold, G.First().IsItalic).X;
                                    IsFirstGroup = false;
                                }
                            }

                            if (ProjectedWidth <= MaxLineWidth)
                            {
                                UnwrappedText.Append(Current.Text);
                                foreach (char c in Current.Text)
                                {
                                    CurrentIndicesMap.Add(CurrentIndexInOriginalText);
                                    CurrentIndexInOriginalText++;
                                }
                                CurrentX += CurrentWidth;

                                Run.RemainingWords.RemoveAt(0);
                            }
                            else
                            {
                                if (UnwrappedText.ToString().Length > 0)
                                {
                                    MGTextRun NewRun = Run.AsTextRun(UnwrappedText.ToString());
                                    CurrentLine.Add(NewRun);
                                }

                                if (CurrentLine.Any() && FlushLine(out Line, false))
                                    yield return Line;
                                UnwrappedText.Clear();

                                if (TotalWidth <= MaxLineWidth)
                                {
                                    UnwrappedText.Append(Current.Text);
                                    foreach (char c in Current.Text)
                                    {
                                        CurrentIndicesMap.Add(CurrentIndexInOriginalText);
                                        CurrentIndexInOriginalText++;
                                    }
                                    CurrentX += CurrentWidth;

                                    Run.RemainingWords.RemoveAt(0);
                                }
                                else
                                {
                                    //  This chunk of text is wider than an entire line, so it must be split up into several pieces
                                    double MaxLineSuffixWidth = string.IsNullOrEmpty(MultiLineWordSuffix) ? 0 : CurrentAndNext.Max(x => Measurer.MeasureText(MultiLineWordSuffix, x.IsBold, x.IsItalic).X);
                                    if (MaxLineSuffixWidth >= MaxLineWidth)
                                    {
                                        if (CurrentLine.Any() && FlushLine(out Line, false))
                                            yield return Line;
                                        yield break;

                                        //string ErrorMsg = $"{nameof(MGTextLine)}.{nameof(ParseLines)} could not be evaluated because zero characters could fit on an entire line. " +
                                        //    $"{nameof(MultiLineWordSuffix)}='{MultiLineWordSuffix}'";
                                        //throw new InvalidOperationException(ErrorMsg);
                                    }

                                    foreach (IGrouping<WrappableRun, WrappableRunWord> WordsByRun in CurrentAndNext.GroupBy(x => x.Run))
                                    {
                                        foreach (WrappableRunWord Word in WordsByRun)
                                        {
                                            Vector2 WordSize = Measurer.MeasureText(Word.Text, Word.IsBold, Word.IsItalic);
                                            if (CurrentX + WordSize.X + MaxLineSuffixWidth <= MaxLineWidth)
                                            {
                                                UnwrappedText.Append(Word.Text);
                                                foreach (char c in Word.Text)
                                                {
                                                    CurrentIndicesMap.Add(CurrentIndexInOriginalText);
                                                    CurrentIndexInOriginalText++;
                                                }
                                                CurrentX += WordSize.X;
                                            }
                                            else
                                            {
                                                for (int CharIndex = 0; CharIndex < Word.Text.Length; CharIndex++)
                                                {
                                                    char CurrentChar = Word.Text[CharIndex];
                                                    float CharacterWidth = Measurer.MeasureText(CurrentChar.ToString(), Word.IsBold, Word.IsItalic).X;
                                                    bool FitsOnCurrentLine = (CurrentLine.Count == 0 && UnwrappedText.Length == 0) || // Ensure we at least have 1 character per line to avoid infinite loop
                                                        (CurrentX + CharacterWidth + MaxLineSuffixWidth <= MaxLineWidth);

                                                    if (FitsOnCurrentLine)
                                                    {
                                                        UnwrappedText.Append(CurrentChar);
                                                        CurrentIndicesMap.Add(CurrentIndexInOriginalText);
                                                        CurrentIndexInOriginalText++;
                                                        CurrentX += CharacterWidth;
                                                    }
                                                    else
                                                    {
                                                        if (!string.IsNullOrEmpty(MultiLineWordSuffix))
                                                        {
                                                            UnwrappedText.Append(MultiLineWordSuffix);
                                                            foreach (char c in MultiLineWordSuffix)
                                                            {
                                                                CurrentIndicesMap.Add(CurrentIndexInOriginalText);
                                                            }
                                                        }

                                                        CurrentLine.Add(WordsByRun.Key.AsTextRun(UnwrappedText.ToString()));
                                                        if (FlushLine(out Line, false))
                                                            yield return Line;
                                                        UnwrappedText.Clear();
                                                        CharIndex--;
                                                    }
                                                }
                                            }
                                        }

                                        if (UnwrappedText.Length > 0)
                                        {
                                            CurrentLine.Add(WordsByRun.Key.AsTextRun(UnwrappedText.ToString()));
                                            UnwrappedText.Clear();
                                        }
                                    }

                                    foreach (WrappableRunWord Word in CurrentAndNext)
                                    {
                                        Word.Run.RemainingWords.Remove(Word);
                                    }
                                }
                            }
                        }

                        if (UnwrappedText.ToString().Length > 0)
                        {
                            string CommittedText = UnwrappedText.ToString();
                            CurrentLine.Add(Run.AsTextRun(CommittedText));
                            CommittedRunsWidth += Measurer.MeasureText(CommittedText, TextRun.Settings.IsBold, TextRun.Settings.IsItalic).X;
                        }
                    }
                }
                else
                    throw new NotImplementedException($"Unrecognized {nameof(TextRunType)}: {Run.RunType}");
            }

            if (!CurrentLine.Any())
                CurrentLine.Add(new MGTextRunText("", new MGTextRunConfig(false), null, null));
            if (FlushLine(out Line, false))
                yield return Line;
        }
    }
}
