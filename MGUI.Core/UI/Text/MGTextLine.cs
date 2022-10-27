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
        public Size2 LineSize { get; }
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
        public MGTextLine(IEnumerable<MGTextRun> Runs, Size2 LineSize, int LineNumber, bool EndsInLinebreakCharacter, IEnumerable<int> Indices)
        {
            this.Runs = Runs.ToList().AsReadOnly();
            this.LineSize = LineSize;
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

                this.WrappableRuns = OriginalRuns.Where(x => x.IsLineBreak || !string.IsNullOrEmpty(x.Text))
                    .Select(Run => new WrappableRun(this, Run)).ToList().AsReadOnly();

                for (int i = 0; i < WrappableRuns.Count; i++)
                {
                    WrappableRun Current = WrappableRuns[i];
                    WrappableRun Next = i == WrappableRuns.Count - 1 ? null : WrappableRuns[i + 1];

                    if (Next != null && !Current.IsLineBreak && !Next.IsLineBreak)
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
            public readonly MGTextRunConfig Settings;
            public readonly bool IsLineBreak;

            public readonly List<WrappableRunWord> OriginalWords;
            public readonly List<WrappableRunWord> RemainingWords;

            public WrappableRun(WrappableRunGroup Group, MGTextRun OriginalRun)
            {
                this.Group = Group;
                this.Settings = OriginalRun.Settings;
                this.IsLineBreak = OriginalRun.IsLineBreak;

                if (IsLineBreak)
                {
                    OriginalWords = new List<WrappableRunWord>();
                }
                else
                {
                    List<string> Words = OriginalRun.Text.SplitAndKeepDelimiters(Group.WordDelimiters).ToList();
                    OriginalWords = Words.Select(x => new WrappableRunWord(this, x, false)).ToList();
                }

                this.RemainingWords = new List<WrappableRunWord>(OriginalWords);
            }

            public MGTextRun AsRun(string Text) => new(Text, IsLineBreak, Settings);
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

            public override string ToString() => IsLineBreak ? $"{nameof(WrappableRun)}: LineBreak" : $"{nameof(WrappableRun)}: {GetAllRemainingText()}";
        }

        private class WrappableRunWord
        {
            public readonly WrappableRun Run;
            public readonly string Text;
            public bool HasNext { get; set; }

            public bool IsBold => Run.Settings.IsBold;
            public bool IsItalic => Run.Settings.IsItalic;

            public WrappableRunWord(WrappableRun Run, string Text, bool HasNext)
            {
                this.Run = Run;
                this.Text = Text;
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

        public static IEnumerable<MGTextLine> ParseLines(ITextMeasurer Measurer, double MaxLineWidth, bool WrapText, IEnumerable<MGTextRun> Runs)
            => ParseLines(Measurer, MaxLineWidth, WrapText, Runs, ' ', '-');

        public static IEnumerable<MGTextLine> ParseLines(ITextMeasurer Measurer, double MaxLineWidth, bool WrapText, IEnumerable<MGTextRun> Runs, params char[] WordDelimiters)
        {
            if (Runs?.Any() != true || MaxLineWidth < 1)
                yield break;

            const string MultiLineWordSuffix = "-";

            float MinLineHeight = (float)Math.Ceiling(Measurer.MeasureText(" ", false, false, false).Y);
            int LineNumber = 1;
            List<MGTextRun> CurrentLine = new();
            float CurrentX = 0;
            List<int> CurrentIndicesMap = new();
            int CurrentIndexInOriginalText = 0;

            MGTextLine FlushLine(bool EndsInLinebreakCharacter)
            {
                if (EndsInLinebreakCharacter)
                {
                    CurrentIndicesMap.Add(CurrentIndexInOriginalText);
                    CurrentIndexInOriginalText++;
                }
                else if (CurrentLine.Count > 0 && CurrentLine.All(x => string.IsNullOrEmpty(x.Text)))
                {
                    CurrentIndicesMap.Add(CurrentIndexInOriginalText);
                }

                List<Vector2> RunSizes = new();
                for (int i = 0; i < CurrentLine.Count; i++)
                {
                    MGTextRun Run = CurrentLine[i];
                    Vector2 RunSize = Measurer.MeasureText(Run.Text, Run.Settings.IsBold, Run.Settings.IsItalic, i == 0);
                    RunSizes.Add(RunSize);
                }
                Size2 LineSize = new(Math.Max(CurrentX, RunSizes.Sum(x => x.X)), Math.Max(MinLineHeight, RunSizes.DefaultIfEmpty(Vector2.Zero).Max(x => x.Y)));
                MGTextLine Line = new(CurrentLine, LineSize, LineNumber, EndsInLinebreakCharacter, CurrentIndicesMap);
                LineNumber++;
                CurrentLine.Clear();
                CurrentIndicesMap.Clear();
                CurrentX = 0;
                return Line;
            }

            WrappableRunGroup Group = new(WordDelimiters, Runs);

            while (Group.RemainingRuns.Any())
            {
                WrappableRun Run = Group.RemainingRuns.First();
                Group.RemainingRuns.RemoveAt(0);

                if (Run.IsLineBreak)
                {
                    if (!CurrentLine.Any())
                        CurrentLine.Add(new("", false, Run.Settings));
                    yield return FlushLine(true);
                }
                else
                {
                    if (!WrapText || MaxLineWidth == double.MaxValue || MaxLineWidth >= 100000)
                    {
                        string Text = Run.GetAllRemainingText();
                        CurrentLine.Add(new(Text, false, Run.Settings));
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
                            //  EX: "Hello[b]World" results in 2 runs: Run1="Hello", Run2="World" but there is no word delimiter (space) between them so it's a single world "HelloWorld"
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
                                    MGTextRun NewRun = new(UnwrappedText.ToString(), false, Run.Settings);
                                    CurrentLine.Add(NewRun);
                                }

                                if (CurrentLine.Any())
                                    yield return FlushLine(false);
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
                                        if (CurrentLine.Any())
                                            yield return FlushLine(false);
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

                                                        CurrentLine.Add(WordsByRun.Key.AsRun(UnwrappedText.ToString()));
                                                        yield return FlushLine(false);
                                                        UnwrappedText.Clear();
                                                        CharIndex--;
                                                    }
                                                }
                                            }
                                        }

                                        if (UnwrappedText.Length > 0)
                                        {
                                            CurrentLine.Add(WordsByRun.Key.AsRun(UnwrappedText.ToString()));
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
                            CurrentLine.Add(Run.AsRun(UnwrappedText.ToString()));
                    }
                }
            }

            //  Form a line from any leftover runs that didn't span an entire line
            //if (CurrentLine.Any())
            //    yield return FlushLine(false);

            if (!CurrentLine.Any())
                CurrentLine.Add(new("", false, Runs.Last().Settings));
            yield return FlushLine(false);
        }
    }
}
