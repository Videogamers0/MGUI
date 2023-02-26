using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using MGUI.Shared.Helpers;
using MonoGame.Extended;

namespace MGUI.Core.UI.Text
{
    internal class TextRenderInfo
    {
        public MGTextBox TextBox { get; }
        private MGTextBlock TextBlockElement { get; }
        internal float DefaultCharacterWidth => TextBlockElement.SpaceWidth * 2;

        internal bool IsDirty { get; private set; } = true;

        private List<LineRenderInfo> _Lines { get; }
        public IReadOnlyList<LineRenderInfo> Lines
        {
            get
            {
                if (IsDirty)
                    UpdateLines();
                return _Lines;
            }
        }

        public LineRenderInfo FirstLine => Lines[0];
        public LineRenderInfo LastLine => Lines[^1];

        internal CharRenderInfo GetFirstChar() => Lines.First(x => x.Characters.Any()).FirstCharacter;
        internal CharRenderInfo GetLastChar() => Lines.Last(x => x.Characters.Any()).LastCharacter;

        internal TextRenderInfo(MGTextBox TextBox, MGTextBlock TextBlockElement)
        {
            this.TextBox = TextBox;
            this.TextBlockElement = TextBlockElement;

            this._Lines = new();

            this.TextBox.TextChanged += (sender, e) => { IsDirty = true; };
            this.TextBlockElement.OnLayoutBoundsChanged += (sender, e) => { IsDirty = true; };
        }

        private void UpdateLines()
        {
            IsDirty = false;
            int LineIndex = 0;
            int CharCounter = 0;
            _Lines.Clear();

            Rectangle LayoutBounds = TextBlockElement.LayoutBounds;
            Thickness Padding = TextBlockElement.Padding;
            float LinePadding = TextBlockElement.LinePadding;

            SpriteFont SF = TextBlockElement.GetFont(TextBlockElement.IsBold, TextBlockElement.IsItalic, out Dictionary<char, SpriteFont.Glyph> Glyphs);
            Vector2 MeasurementScale = new Vector2(1.0f, TextBlockElement.FontHeight) * TextBlockElement.FontScale;

            float CurrentY = LayoutBounds.Top + Padding.Top;
            if (TextBlockElement.Lines?.Any() != true)
            {
                LineRenderInfo LineInfo = new(this, null, LineIndex, CurrentY, TextBlockElement.MeasureText("|", false, false, false).Y);
                _Lines.Add(LineInfo);

                float X = LayoutBounds.Left + Padding.Left;
                LineInfo.AddCharacter(0, 0, X, DefaultCharacterWidth);
            }
            else
            {
                foreach (MGTextLine Line in TextBlockElement.Lines)
                {
                    int CharacterIndexInWrappedLines = 0;
                    Rectangle LineBounds = new(LayoutBounds.Left + Padding.Left, (int)CurrentY, LayoutBounds.Width - TextBox.PaddingSize.Width, (int)Line.LineTotalHeight);
                    float CurrentX = MGElement.ApplyAlignment(LineBounds, TextBlockElement.TextAlignment, VerticalAlignment.Center, new Size((int)Line.LineWidth, (int)Line.LineTotalHeight)).Left;

                    LineRenderInfo LineInfo = new(this, Line, LineIndex, LineBounds.Top, Line.LineTotalHeight);
                    _Lines.Add(LineInfo);
                    LineIndex++;

                    if (!Line.Runs.All(x => x.RunType == TextRunType.Text))
                        throw new NotImplementedException($"{nameof(TextRenderInfo)}.{nameof(UpdateLines)} can only handle {nameof(MGTextLine)}s which consist only of {nameof(MGTextRun)}s of type={nameof(TextRunType)}.{nameof(TextRunType.Text)}");

                    List<MGTextRunText> Runs = Line.Runs.Cast<MGTextRunText>().ToList();
                    if (!Runs.Any(x => !string.IsNullOrEmpty(x.Text)))
                    {
                        LineInfo.AddCharacter(Line.OriginalCharacterIndices[CharacterIndexInWrappedLines], CharCounter, CurrentX, DefaultCharacterWidth);
                        //CharacterIndexInWrappedLines++;
                    }
                    else
                    {
                        foreach (MGTextRunText Run in Runs)
                        {
                            bool IsStartOfLine = true;

                            foreach (char c in Run.Text)
                            {
                                //SpriteFont.MeasureString source code is here:
                                //https://github.com/MonoGame/MonoGame/blob/develop/MonoGame.Framework/Graphics/SpriteFont.cs

                                SpriteFont.Glyph Glyph = Glyphs[c];

                                float ActualLeftSideBearing;
                                if (IsStartOfLine)
                                {
                                    IsStartOfLine = false;
                                    ActualLeftSideBearing = Math.Max(Glyph.LeftSideBearing, 0);
                                }
                                else
                                    ActualLeftSideBearing = SF.Spacing + Glyph.LeftSideBearing;

                                float Width = (ActualLeftSideBearing + Glyph.Width + Glyph.RightSideBearing) * MeasurementScale.X;
                                LineInfo.AddCharacter(Line.OriginalCharacterIndices[CharacterIndexInWrappedLines], CharCounter, CurrentX, Width);
                                CharacterIndexInWrappedLines++;
                                CharCounter++;
                                CurrentX += Width;
                            }
                        }
                    }

                    CurrentY += Line.LineTotalHeight + LinePadding;
                }
            }
        }

        internal bool TryGetCharAtOriginalIndex(int Index, out CharRenderInfo Result)
        {
            foreach (LineRenderInfo Line in Lines)
            {
                if (Line.TryGetCharAtOriginalIndex(Index, out Result))
                    return true;
            }

            Result = default;
            return false;
        }

        internal bool TryGetCharAtParsedIndex(int Index, out CharRenderInfo Result)
        {
            foreach (LineRenderInfo Line in Lines)
            {
                if (Line.TryGetCharAtParsedIndex(Index, out Result))
                    return true;
            }

            Result = default;
            return false;
        }

        internal bool TryGetCharAtScreenPosition(Vector2 Position, out CharRenderInfo Result)
        {
            LineRenderInfo Line = Lines.OrderBy(x => x.DistanceToCenter(Position.Y)).FirstOrDefault();
            if (Line == null || !Line.Characters.Any())
            {
                Result = default;
                return false;
            }

            Result = Line.Characters.OrderBy(x => x.DistanceToMinX(Position.X)).First();
            return true;
        }

        public override string ToString() => $"{nameof(TextRenderInfo)}: {string.Join(", ", Lines.Select(x => x.ToString()))}";
    }

    internal class LineRenderInfo
    {
        public readonly TextRenderInfo TextInfo;
        public readonly MGTextLine Source;

        public readonly int LineIndex;
        public readonly float YPosition;
        public readonly float Height;

        public float MinY => YPosition;
        public float MaxY => MinY + Height;
        public float CenterY => (MinY + MaxY) / 2.0f;

        public float DistanceToMinY(float YValue) => Math.Abs(YValue - MinY);
        public float DistanceToCenter(float YValue) => Math.Abs(YValue - CenterY);
        public float DistanceToMaxY(float YValue) => Math.Abs(YValue - MaxY);

        private readonly List<CharRenderInfo> _Characters;
        public IReadOnlyList<CharRenderInfo> Characters => _Characters;
        public bool HasCharacters => _Characters.Count > 0;

        public CharRenderInfo FirstCharacter => _Characters[0];
        public CharRenderInfo LastCharacter => _Characters[^1];

        internal LineRenderInfo(TextRenderInfo TextInfo, MGTextLine Source, int LineIndex, float YPosition, float Height)
        {
            this.TextInfo = TextInfo;
            this.Source = Source;
            this.LineIndex = LineIndex;
            this.YPosition = YPosition;
            this.Height = Height;
            this._Characters = new();
        }

        internal bool TryGetCharAtOriginalIndex(int Index, out CharRenderInfo Result)
        {
            if (!Characters.Any() || _Characters[^1].IndexInOriginalText < Index || Index < 0)
            {
                Result = default;
                return false;
            }
            else
            {
                foreach (CharRenderInfo CharInfo in Characters)
                {
                    if (CharInfo.IndexInOriginalText == Index)
                    {
                        Result = CharInfo;
                        return true;
                    }
                }

                Result = default;
                return false;
            }
        }

        internal bool TryGetCharAtParsedIndex(int Index, out CharRenderInfo Result)
        {
            if (!Characters.Any() || _Characters[^1].IndexInParsedText < Index || Index < 0)
            {
                Result = default;
                return false;
            }
            else
            {
                foreach (CharRenderInfo CharInfo in Characters)
                {
                    if (CharInfo.IndexInParsedText == Index)
                    {
                        Result = CharInfo;
                        return true;
                    }
                }

                Result = default;
                return false;
            }
        }

        internal CharRenderInfo AddCharacter(int IndexInOriginalText, int IndexInParsedText, float XPosition, float Width)
        {
            CharRenderInfo Result = new(this, IndexInOriginalText, IndexInParsedText, XPosition, Width);
            _Characters.Add(Result);
            return Result;
        }

        internal LineRenderInfo GetTranslated(Vector2 Offset)
        {
            LineRenderInfo TranslatedLine = new(TextInfo, Source, LineIndex, YPosition + Offset.Y, Height);
            foreach (CharRenderInfo CharInfo in Characters)
                TranslatedLine.AddCharacter(CharInfo.IndexInOriginalText, CharInfo.IndexInParsedText, CharInfo.XPosition + Offset.X, CharInfo.Width);
            return TranslatedLine;
        }

        public override string ToString() => $"{nameof(LineRenderInfo)}: {string.Join(", ", Characters.Select(x => x.ToString()))}";
    }

    internal struct CharRenderInfo
    {
        public readonly LineRenderInfo Line;
        public readonly int IndexInOriginalText;
        public readonly int IndexInParsedText;
        public readonly float XPosition;
        public readonly float Width;

        public float MinX => XPosition;
        public float MaxX => MinX + Width;
        public float CenterX => (MinX + MaxX) / 2.0f;

        public float DistanceToMinX(float XValue) => Math.Abs(XValue - MinX);
        public float DistanceToCenter(float XValue) => Math.Abs(XValue - CenterX);
        public float DistanceToMaxX(float XValue) => Math.Abs(XValue - MaxX);

        internal CharRenderInfo(LineRenderInfo Line, int IndexInOriginalText, int IndexInParsedText, float XPosition, float Width)
        {
            this.Line = Line;
            this.IndexInOriginalText = IndexInOriginalText;
            this.IndexInParsedText = IndexInParsedText;
            this.XPosition = XPosition;
            this.Width = Width;
        }

        public override string ToString() => $"({IndexInOriginalText}|{IndexInParsedText})";
    }
}
