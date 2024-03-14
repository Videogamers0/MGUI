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
        /// <param name="IgnoreFirstGlyphNegativeLeftSideBearing">Typically true for the first glyph of a line that is being rendered, but false in all other cases.<para/>
        /// See also: <see cref="SpriteFont.MeasureString(string)"/> source code at:<br/>
        /// <see href="https://github.com/MonoGame/MonoGame/blob/develop/MonoGame.Framework/Graphics/SpriteFont.cs"/><para/>
        /// <code>
        /// if (firstGlyphOfLine) {
        ///     offset.X = Math.Max(pCurrentGlyph->LeftSideBearing, 0);
        ///     firstGlyphOfLine = false;
        /// }
        /// </code></param>
        Vector2 MeasureText(string Text, bool IsBold, bool IsItalic, bool IgnoreFirstGlyphNegativeLeftSideBearing);
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
            this.OriginalCharacterIndices = Indices.ToList().AsReadOnly();
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

                this.WrappableRuns = OriginalRuns.Where(x => x.RunType != TextRunType.Text || (!string.IsNullOrEmpty(((MGTextRunText)x).Text)))
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

                this.RemainingRuns = new(WrappableRuns);
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

                this.RemainingWords = new List<WrappableRunWord>(OriginalWords);
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
        public static IEnumerable<MGTextLine> ParseLines(ITextMeasurer Measurer, double MaxLineWidth, bool WrapText, IEnumerable<MGTextRun> Runs, bool IgnoreEmptySpaceLines, params char[] WordDelimiters)
        {
            if (Runs?.Any() != true || MaxLineWidth < 1)
                yield break;

            const string MultiLineWordSuffix = "-"; // A suffix to append to the end of a line, when the line only consists of a single word that must wrap across multiple lines

            float MinLineHeight = (float)Math.Ceiling(Measurer.MeasureText(" ", false, false, false).Y);
            int LineNumber = 1;
            List<MGTextRun> CurrentLine = new();
            float CurrentX = 0;
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

                List<Vector2> TextRunSizes = CurrentLine.Where(x => x.RunType == TextRunType.Text).Cast<MGTextRunText>()
                    .Select((x, index) => Measurer.MeasureText(x.Text, x.Settings.IsBold, x.Settings.IsItalic, index == 0)).ToList();
                List<Vector2> ImageRunSizes = CurrentLine.Where(x => x.RunType == TextRunType.Image).Cast<MGTextRunImage>()
                    .Select(x => new Vector2(x.TargetWidth, x.TargetHeight)).ToList();

                float LineWidth = Math.Max(CurrentX, TextRunSizes.Sum(x => x.X) + ImageRunSizes.Sum(x => x.X));
                float LineTextHeight = TextRunSizes.DefaultIfEmpty(Vector2.Zero).Max(x => x.Y);
                float LineImageHeight = ImageRunSizes.DefaultIfEmpty(Vector2.Zero).Max(x => x.Y);
                float LineTotalHeight = GeneralUtils.Max(MinLineHeight, LineTextHeight, LineImageHeight);

                Line = new(CurrentLine, LineWidth, LineTextHeight, LineImageHeight, LineTotalHeight, LineNumber, EndsInLinebreakCharacter, CurrentIndicesMap);
                LineNumber++;
                CurrentLine.Clear();
                CurrentIndicesMap.Clear();
                CurrentX = 0;

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
                        if (CurrentLine.Any() && CurrentX + ImgWidth > MaxLineWidth && FlushLine(out Line, false))
                            yield return Line;

                        CurrentLine.Add(ImageRun);
                        CurrentX += ImgWidth;
                    }
                }
                else if (Run.IsText && Run.OriginalRun is MGTextRunText TextRun)
                {
                    if (!WrapText || MaxLineWidth == double.MaxValue || MaxLineWidth >= 100000)
                    {
                        string Text = Run.GetAllRemainingText();
                        CurrentLine.Add(Run.AsTextRun(Text));
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
                                Measurer.MeasureText(Word.Text, Word.IsBold, Word.IsItalic, CurrentLine.Count == 0 && Index == 0 && UnwrappedText.Length == 0)).ToList();
                            float CurrentWidth = Measurements[0].X;
                            float TotalWidth = Measurements.Sum(x => x.X);

                            if (CurrentX + TotalWidth <= MaxLineWidth)
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
                                    double MaxLineSuffixWidth = string.IsNullOrEmpty(MultiLineWordSuffix) ? 0 : CurrentAndNext.Max(x => Measurer.MeasureText(MultiLineWordSuffix, x.IsBold, x.IsItalic, false).X);
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
                                            Vector2 WordSize = Measurer.MeasureText(Word.Text, Word.IsBold, Word.IsItalic, CurrentLine.Count == 0 && UnwrappedText.Length == 0);
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
                                                    float CharacterWidth = Measurer.MeasureText(CurrentChar.ToString(), Word.IsBold, Word.IsItalic, CurrentLine.Count == 0 && UnwrappedText.Length == 0).X;
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
                            CurrentLine.Add(Run.AsTextRun(UnwrappedText.ToString()));
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
