using Microsoft.Xna.Framework;
using MGUI.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MonoGame.Extended;

namespace MGUI.Core.UI.Text
{
    public class MGTextCaret
    {
        public MGTextBox TextBox { get; }
        private MGTextBlock TextBlockElement { get; }
        private TextRenderInfo TextRenderInfo => TextBox.TextRenderInfo;

        internal record struct CaretPosition(int LineIndex, int IndexInOriginalText, int IndexInParsedText, Rectangle Bounds)
        {
            public DateTime InitialShowTime { get; init; } = DateTime.Now;
            public CaretPosition Translate(Point Offset) => new(LineIndex, IndexInOriginalText, IndexInParsedText, Bounds.GetTranslated(Offset)) { InitialShowTime = this.InitialShowTime };
        }

        private CaretPosition? _Position;
        internal CaretPosition? Position
        {
            get => _Position;
            set
            {
                _Position = value;
                //Debug.WriteLine($"{nameof(MGTextCaret)}: Position={(Position == null ? "null" : $"({Position.Value.IndexInOriginalText}|{Position.Value.IndexInParsedText})")}");
            }
        }
        public bool HasPosition => Position.HasValue;

        public Color Color { get; set; }

        /// <summary>The <see cref="Position"/> cycles between showing the cursor for this amount of time, then hiding the cursor for this amount of time. Repeat.</summary>
        public TimeSpan BlinkRate { get; set; } = TimeSpan.FromSeconds(0.5);

        internal MGTextCaret(MGTextBox TextBox, MGTextBlock TextBlockElement)
        {
            this.TextBox = TextBox;
            this.TextBlockElement = TextBlockElement;

            this.Color = TextBox.GetTheme().TextBlockFallbackForeground.GetValue(true).NormalValue;
            this.Position = null;

            this.TextBlockElement.OnLayoutBoundsChanged += (sender, e) =>
            {
                if (HasPosition)
                {
                    if (e.PreviousValue.Size != e.NewValue.Size || TextRenderInfo.IsDirty)
                    {
                        //This is messing up the character index when caret is at end of a line and types a new character
                        //MoveToOriginalCharacterIndexOrLeft(Position.Value.IndexInParsedText, true);
                    }
                    else
                    {
                        Point Offset = e.NewValue.TopLeft() - e.PreviousValue.TopLeft();
                        Position = Position.Value.Translate(Offset);
                    }
                }
            };
        }

        public override string ToString() => $"{Position}";

        #region Navigation
        /// <summary>Moves the caret position to the given side of the given <paramref name="CharIndex"/>.<br/>
        /// If no character was found at the index, continually navigates left until finding one.</summary>
        /// <param name="CharIndex">The index in the original text, before it was parsed into <see cref="MGTextLine"/>s.</param>
        public bool MoveToOriginalCharacterIndexOrLeft(int CharIndex, bool LeftSide)
        {
            //Debug.WriteLine($"{nameof(MGTextCaret)}: Move to original index or left: {CharIndex} (Left={LeftSide})");
            int FirstIndex = TextRenderInfo.GetFirstChar().IndexInOriginalText;
            int CurrentIndex = CharIndex;

            while (true)
            {
                if (TextRenderInfo.TryGetCharAtOriginalIndex(CurrentIndex, out CharRenderInfo CharInfo))
                    return MoveToCharacter(CharInfo, CurrentIndex == CharIndex ? LeftSide : false);
                else
                {
                    CurrentIndex--;
                    if (CurrentIndex < FirstIndex)
                        return false;
                }
            }
        }

        /// <summary>Moves the caret position to the given side of the given <paramref name="CharIndex"/>.<br/>
        /// If no character was found at the index, continually navigates right until finding one.</summary>
        /// <param name="CharIndex">The index in the original text, before it was parsed into <see cref="MGTextLine"/>s.</param>
        public bool MoveToOriginalCharacterIndexOrRight(int CharIndex, bool LeftSide)
        {
            //Debug.WriteLine($"{nameof(MGTextCaret)}: Move to original index or right: {CharIndex} (Left={LeftSide})");
            int LastIndex = TextRenderInfo.GetLastChar().IndexInOriginalText;
            int CurrentIndex = CharIndex;

            while (true)
            {
                if (TextRenderInfo.TryGetCharAtOriginalIndex(CurrentIndex, out CharRenderInfo CharInfo))
                    return MoveToCharacter(CharInfo, CurrentIndex == CharIndex ? LeftSide : true);
                else
                {
                    CurrentIndex++;
                    if (CurrentIndex > LastIndex)
                        return false;
                }
            }
        }

        /// <summary>Moves the caret position to the given side of the given <paramref name="CharIndex"/>.<br/>
        /// If no character was found at the index, navigates to the end of the text.</summary>
        /// <param name="CharIndex">The index in the original text, before it was parsed into <see cref="MGTextLine"/>s.</param>
        public bool MoveToOriginalCharacterIndexOrEnd(int CharIndex, bool LeftSide)
        {
            //Debug.WriteLine($"{nameof(MGTextCaret)}: Move to original index or end: {CharIndex} (Left={LeftSide})");
            if (TextRenderInfo.TryGetCharAtOriginalIndex(CharIndex, out CharRenderInfo CharInfo))
                return MoveToCharacter(CharInfo, LeftSide);
            else
                return MoveToEndOfLine(TextRenderInfo.LastLine);
        }

        /// <summary>Moves the caret position to the given side of the given <paramref name="CharIndex"/>.<br/>
        /// If no character was found at the index, continually navigates left until finding one.</summary>
        /// <param name="CharIndex">The index in the parsed text (after it was converted into <see cref="MGTextLine"/>s)</param>
        public bool MoveToParsedCharacterIndexOrLeft(int CharIndex, bool LeftSide)
        {
            //Debug.WriteLine($"{nameof(MGTextCaret)}: Move to parsed index or left: {CharIndex} (Left={LeftSide})");
            int FirstIndex = TextRenderInfo.GetFirstChar().IndexInParsedText;
            int CurrentIndex = CharIndex;

            while (true)
            {
                if (TextRenderInfo.TryGetCharAtParsedIndex(CurrentIndex, out CharRenderInfo CharInfo))
                    return MoveToCharacter(CharInfo, CurrentIndex == CharIndex ? LeftSide : false);
                else
                {
                    CurrentIndex--;
                    if (CurrentIndex < FirstIndex)
                        return false;
                }
            }
        }

        /// <summary>Moves the caret position to the given side of the given <paramref name="CharIndex"/>.<br/>
        /// If no character was found at the index, continually navigates right until finding one.</summary>
        /// <param name="CharIndex">The index in the parsed text (after it was converted into <see cref="MGTextLine"/>s)</param>
        public bool MoveToParsedCharacterIndexOrRight(int CharIndex, bool LeftSide)
        {
            //Debug.WriteLine($"{nameof(MGTextCaret)}: Move to parsed index or right: {CharIndex} (Left={LeftSide})");
            int LastIndex = TextRenderInfo.GetLastChar().IndexInParsedText;
            int CurrentIndex = CharIndex;

            while (true)
            {
                if (TextRenderInfo.TryGetCharAtParsedIndex(CurrentIndex, out CharRenderInfo CharInfo))
                    return MoveToCharacter(CharInfo, CurrentIndex == CharIndex ? LeftSide : true);
                else
                {
                    CurrentIndex++;
                    if (CurrentIndex > LastIndex)
                        return false;
                }
            }
        }

        /// <summary>Moves the caret position to the given side of the given <paramref name="CharIndex"/>.<br/>
        /// If no character was found at the index, navigates to the end of the text.</summary>
        /// <param name="CharIndex">The index in the parsed text (after it was converted into <see cref="MGTextLine"/>s)</param>
        public bool MoveToParsedCharacterIndexOrEnd(int CharIndex, bool LeftSide)
        {
            //Debug.WriteLine($"{nameof(MGTextCaret)}: Move to parsed index or end: {CharIndex} (Left={LeftSide})");
            if (TextRenderInfo.TryGetCharAtParsedIndex(CharIndex, out CharRenderInfo CharInfo))
                return MoveToCharacter(CharInfo, LeftSide);
            else
                return MoveToEndOfLine(TextRenderInfo.LastLine);
        }

        public bool MoveToApproximateScreenPosition(Vector2 ScreenPosition)
        {
            //Debug.WriteLine($"{nameof(MGTextCaret)}: Move to screen position: {ScreenPosition}");
            if (TextRenderInfo.TryGetCharAtScreenPosition(ScreenPosition, out CharRenderInfo CharInfo))
            {
                bool IsLeftEdge = ScreenPosition.X <= CharInfo.CenterX || !CharInfo.Line.HasCharacters || CharInfo.Line.Source == null;
                if (!IsLeftEdge)
                    IsLeftEdge = CharInfo.Line.Source?.Runs.All(x => x is MGTextRunText r && string.IsNullOrEmpty(r.Text)) != false;
                return MoveToCharacter(CharInfo, IsLeftEdge);
            }
            else
                return false;
        }

        internal bool MoveToCharacter(CharRenderInfo CharInfo, bool LeftSide)
        {
            if (LeftSide)
            {
                //Debug.WriteLine($"{nameof(MGTextCaret)}: Move to left side of ({CharInfo.IndexInOriginalText}|{CharInfo.IndexInParsedText})");
                Rectangle Bounds = new((int)CharInfo.MinX, (int)CharInfo.Line.MinY, (int)CharInfo.Width, (int)CharInfo.Line.Height);
                Position = new(CharInfo.Line.LineIndex, CharInfo.IndexInOriginalText, CharInfo.IndexInParsedText, Bounds);
                return true;
            }
            else
            {
                //Debug.WriteLine($"{nameof(MGTextCaret)}: Move to right side of ({CharInfo.IndexInOriginalText}|{CharInfo.IndexInParsedText})");
                Rectangle Bounds = new((int)CharInfo.MaxX, (int)CharInfo.Line.MinY, (int)TextRenderInfo.DefaultCharacterWidth, (int)CharInfo.Line.Height);
                Position = new(CharInfo.Line.LineIndex, CharInfo.IndexInOriginalText + 1, CharInfo.IndexInParsedText + 1, Bounds);
                return true;
            }
        }

        public bool MoveLeft(int Amount)
        {
            if (!HasPosition)
                return false;

            int DesiredIndex = Position.Value.IndexInParsedText - Amount;
            if (TextRenderInfo.TryGetCharAtParsedIndex(DesiredIndex, out CharRenderInfo CharInfo))
            {
                return MoveToCharacter(CharInfo, true);
            }
            else
            {
                CharInfo = TextRenderInfo.GetFirstChar();
                return MoveToCharacter(CharInfo, true);
            }
        }

        public bool MoveRight(int Amount)
        {
            if (!HasPosition)
                return false;

            int DesiredIndex = Position.Value.IndexInParsedText + Amount - 1;
            if (TextRenderInfo.TryGetCharAtParsedIndex(DesiredIndex, out CharRenderInfo CharInfo))
            {
                return MoveToCharacter(CharInfo, false);
            }
            else
            {
                CharInfo = TextRenderInfo.GetLastChar();
                return MoveToCharacter(CharInfo, false);
            }
        }

        public bool MoveUp(int Amount)
        {
            if (!HasPosition)
                return false;

            if (Position.Value.LineIndex <= 0)
                return MoveToStartOfCurrentLine();
            else
            {
                float LineHeight = Position.Value.Bounds.Height;
                Vector2 CurrentScreenPosition = Position.Value.Bounds.TopLeft().ToVector2() + new Vector2(0, LineHeight / 2);
                Vector2 DesiredScreenPosition = CurrentScreenPosition - new Vector2(0, LineHeight) * Amount;
                return MoveToApproximateScreenPosition(DesiredScreenPosition);
            }
        }

        public bool MoveDown(int Amount)
        {
            if (!HasPosition)
                return false;

            if (Position.Value.LineIndex >= TextRenderInfo.Lines.Max(x => x.LineIndex))
                return MoveToEndOfCurrentLine();
            else
            {
                float LineHeight = Position.Value.Bounds.Height;
                Vector2 CurrentScreenPosition = Position.Value.Bounds.TopLeft().ToVector2() + new Vector2(0, LineHeight / 2);
                Vector2 DesiredScreenPosition = CurrentScreenPosition + new Vector2(0, LineHeight) * Amount;
                return MoveToApproximateScreenPosition(DesiredScreenPosition);
            }
        }

        public bool MoveToStartOfCurrentLine()
        {
            if (!HasPosition)
                return false;

            LineRenderInfo Line = TextRenderInfo.Lines.FirstOrDefault(x => x.LineIndex == Position.Value.LineIndex);
            return MoveToStartOfLine(Line);
        }

        internal bool MoveToStartOfLine(LineRenderInfo Line)
        {
            if (Line == null)
                return false;

            CharRenderInfo First = Line.Characters.First();
            return MoveToCharacter(First, true);
        }

        public bool MoveToEndOfCurrentLine()
        {
            if (!HasPosition)
                return false;

            LineRenderInfo Line = TextRenderInfo.Lines.FirstOrDefault(x => x.LineIndex == Position.Value.LineIndex);
            return MoveToEndOfLine(Line);
        }

        internal bool MoveToEndOfLine(LineRenderInfo Line)
        {
            if (Line == null || !Line.Characters.Any())
                return false;

            CharRenderInfo Last = Line.LastCharacter;
            return MoveToCharacter(Last, false);
        }
        #endregion Navigation

        internal void Draw(ElementDrawArgs DA, Rectangle LayoutBounds)
        {
            if (HasPosition && TextBox.GetDesktop().FocusedKeyboardHandler == TextBox)
            {
                double SecondsShown = DateTime.Now.Subtract(Position.Value.InitialShowTime).TotalSeconds;
                if ((int)(SecondsShown / BlinkRate.TotalSeconds) % 2 == 0)
                {
                    switch (TextBox.TextEntryMode)
                    {
                        case TextEntryMode.Insert:
                            float CursorPaddingY = 2;
                            Vector2 CursorPadding = new(0, CursorPaddingY);
                            Vector2 PaddedTop = Position.Value.Bounds.TopLeft().ToVector2() - CursorPadding;
                            Vector2 PaddedBottom = Position.Value.Bounds.BottomLeft().ToVector2() + CursorPadding;
                            float CursorThickness = 1;
                            DA.DT.StrokeLineSegment(DA.Offset.ToVector2(), PaddedTop, PaddedBottom, Color * DA.Opacity, CursorThickness);
                            break;
                        case TextEntryMode.Overwrite:
                            DA.DT.FillRectangle(DA.Offset.ToVector2(), Position.Value.Bounds, Color.Orange * 0.35f * DA.Opacity);
                            break;
                        default: throw new NotImplementedException($"Unrecognized {nameof(TextEntryMode)}: {TextBox.TextEntryMode}");
                    }
                }
            }
        }
    }
}
