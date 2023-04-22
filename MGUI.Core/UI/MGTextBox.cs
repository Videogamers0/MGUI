using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MGUI.Shared.Helpers;
using MGUI.Core.UI.Text;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ColorTranslator = System.Drawing.ColorTranslator;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;
using MonoGame.Extended;
using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Shared.Input.Keyboard;

namespace MGUI.Core.UI
{
    public enum TextEntryMode
    {
        /// <summary>New characters will be inserted into the existing Text at the caret position.</summary>
        Insert,
        /// <summary>New characters will replace the existing character at the caret position.</summary>
        Overwrite
    }

    public class MGTextBox : MGElement
    {
        public override bool CanHandleKeyboardInput => true;

        #region Border
        /// <summary>Provides direct access to this element's border.</summary>
        public MGComponent<MGBorder> BorderComponent { get; }
        private MGBorder BorderElement { get; }
        public override MGBorder GetBorder() => BorderElement;

        public IBorderBrush BorderBrush
        {
            get => BorderElement.BorderBrush;
            set => BorderElement.BorderBrush = value;
        }

        public Thickness BorderThickness
        {
            get => BorderElement.BorderThickness;
            set => BorderElement.BorderThickness = value;
        }
        #endregion Border

        #region Text
        /// <summary>Provides direct access to the textblock component that displays this textbox's text.</summary>
        public MGComponent<MGTextBlock> TextBlockComponent { get; }
        private MGTextBlock TextBlockElement { get; }

        protected virtual string GetTextBackingField() => _Text ?? "";
        protected virtual void SetTextBackingField(string Value) => _Text = Value;

        public int FontSize => TextBlockElement.FontSize;
        public bool TrySetFontSize(int Value) => TextBlockElement.TrySetFontSize(Value);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _Text;
        /// <summary>To set this value, use <see cref="SetText(string)"/>.<para/>
        /// If this <see cref="MGTextBox"/> is an <see cref="MGPasswordBox"/>, this value will only contain <see cref="MGPasswordBox.PasswordCharacter"/>s (and special characters such as \n).<para/>
        /// See also: <see cref="MGPasswordBox.Password"/></summary>
        public string Text { 
            get => _Text ?? string.Empty;
            //  This setter is mainly intended for use by XAML DataBindings
            set => SetText(value);
        }

        /// <returns>True if <see cref="Text"/> value was changed.</returns>
        public virtual bool SetText(string Value) => SetText(Value, false);

        /// <param name="ExecuteEvenIfSameValue">If true, will attempt to set the value even if <see cref="Text"/> already has the same value as <paramref name="Value"/>.<para/>
        /// This is mainly intended for use by subclasses that alter the <paramref name="Value"/>, such as <see cref="MGPasswordBox"/><br/>
        /// (For example, a Password might change from "123" to "234", but this method would only see "***" -> "***"</param>
        /// <returns>True if <see cref="Text"/> value was changed.</returns>
        protected virtual bool SetText(string Value, bool ExecuteEvenIfSameValue)
        {
            if (!AcceptsReturn && (Value.Contains('\n') || Value.Contains('\r')))
                return false;

            if (CharacterLimit.HasValue && Value.Length > CharacterLimit.Value)
                return false;

            if (GetTextBackingField() != Value || ExecuteEvenIfSameValue)
            {
                if (TextChanging != null)
                {
                    CancelEventArgs<string> Args = new(Text);
                    TextChanging.Invoke(this, Args);
                    if (Args.Cancel)
                        return false;
                }

                string Previous = Text;

                _Text = Value;
                NPC(nameof(Text));

                if (!IsExecutingUndoRedo)
                    ClearRedoStack();

                UpdateCharacterCountText();
                UpdatePlaceholderVisibility();
                UpdateFormattedText(false);

                TextChanged?.Invoke(this, new(Previous, Text));

                //if (Caret.HasPosition && Caret.Position.Value.IndexInOriginalText >= Text.Length)
                //    Caret.MoveToStartOfLine(TextRenderInfo.Lines.FirstOrDefault());

                if (Caret.HasPosition && string.IsNullOrEmpty(Text))
                    Caret.MoveToStartOfLine(TextRenderInfo.Lines.FirstOrDefault());

                return true;
            }
            else
                return false;
        }

        /// <summary>Invoked when <see cref="Text"/> is about to change.</summary>
        public event EventHandler<CancelEventArgs<string>> TextChanging;
        /// <summary>Invoked immediately after <see cref="Text"/> has changed.<para/>
        /// For <see cref="MGPasswordBox"/>, consider using <see cref="MGPasswordBox.PasswordChanged"/></summary>
        public event EventHandler<EventArgs<string>> TextChanged;

        #region Formmated Text
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _FormattedText;
        public string FormattedText { get => _FormattedText; }

        /// <param name="Silent">If true, <see cref="MGTextBlock"/> will not invoke its LayoutChanged event.<para/>
        /// This value should only be trued when changing the markdown of the text, but not the actual rendered text itself.<br/>
        /// For example, changing the foreground color of the text does not affect its layout.</param>
        private void SetFormattedText(string Value, bool Silent)
        {
            if (_FormattedText != Value)
            {
                _FormattedText = Value;
                TextBlockElement.SetText(FormattedText, Silent);
                NPC(nameof(FormattedText));
            }
        }

        private void UpdateFormattedText(bool Silent)
        {
            string EscapedText = FTTokenizer.EscapeMarkdown(Text);
            string FormattedText = EscapedText;

            if (CurrentSelection.HasValue && CurrentSelection.Value.Length > 0)
            {
                List<int> EscapedIndices = new();
                int CurrentEscapedIndex = 0;
                for (int i = 0; i < Text.Length; i++)
                {
                    EscapedIndices.Add(CurrentEscapedIndex);
                    if (Text[i] == FTTokenizer.OpenTagChar)
                        CurrentEscapedIndex++;
                    CurrentEscapedIndex++;
                }
                EscapedIndices.Add(CurrentEscapedIndex);

                bool HasFocus = GetDesktop().FocusedKeyboardHandler == this;
                string FGColor = HasFocus ? FocusedSelectionForegroundColorString : UnfocusedSelectionForegroundColorString;
                string BGColor = HasFocus ? FocusedSelectionBackgroundColorString : UnfocusedSelectionBackgroundColorString;

                string SelectionStartMarkdown = $"[fg={FGColor}][bg={BGColor}]";
                string SelectionEndMarkdown = @"[\bg][\fg]";

                int ActualStartIndex = Math.Clamp(CurrentSelection.Value.StartIndex, 0, EscapedIndices.Count - 1);
                int ActualEndIndex = Math.Clamp(CurrentSelection.Value.EndIndex, ActualStartIndex, EscapedIndices.Count - 1);

                //  '\\' is an escape character for '[', so strings like @"Hello\[b]World" are rendered as: "Hello[b]World",
                //  treating the formatting code that follows the escape character as a literal string value.
                //  Since we are about to insert a '[' character, we must double-escape any escape characters that precede the inserted markdown.
                //  EX: @"Hello\World" -> @"Hello\\[fg=white][bg=black]World[/bg][/fg]"
                void DoubleEscapeAtIndex(int Index)
                {
                    int CurrentIndex = Index;
                    int UnescapedCount = 0;
                    while (CurrentIndex >= 0 && CurrentIndex < Text.Length && Text[CurrentIndex] == FTTokenizer.EscapeOpenTagChar)
                    {
                        UnescapedCount++;
                        CurrentIndex--;
                    }
                    CurrentIndex++;

                    if (UnescapedCount > 0)
                    {
                        string InsertionValue = string.Concat(Enumerable.Repeat(FTTokenizer.EscapeOpenTagChar, UnescapedCount));
                        FormattedText = FormattedText.Insert(Index, InsertionValue);

                        //TODO: There's still a bug with this logic. Set the textbox's text to: @"A\\B",
                        //Then try selecting just the 2nd backslash, and there's some kind of off-by-one issue with the escaped indices.
                        //Since it only happens when there's multiple consecutive backslashes, and only when selecting just the backslashes, I don't really care enough to fix it
                        for (int i = CurrentIndex + 1; i < CurrentIndex + 1 + UnescapedCount; i++)
                        {
                            for (int j = i; j < EscapedIndices.Count; j++)
                            {
                                EscapedIndices[j]++;
                            }
                        }
                    }
                }

                DoubleEscapeAtIndex(ActualStartIndex - 1);
                DoubleEscapeAtIndex(ActualEndIndex - 1);

                FormattedText = FormattedText.Insert(EscapedIndices[ActualStartIndex], SelectionStartMarkdown);
                FormattedText = FormattedText.Insert(EscapedIndices[ActualEndIndex] + SelectionStartMarkdown.Length, SelectionEndMarkdown);
            }

            SetFormattedText(FormattedText, Silent);
        }
        #endregion Formmated Text

        #region Placeholder Text
        /// <summary>Provides direct access to the textblock component that displays the <see cref="PlaceholderText"/> when <see cref="Text"/> is empty.</summary>
        public MGComponent<MGTextBlock> PlaceholderTextBlockComponent { get; }
        private MGTextBlock PlaceholderTextBlockElement { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _PlaceholderText;
        /// <summary>Text to display in when <see cref="Text"/> is empty. Default value: null<para/>
        /// This value supports some basic markdown, such as:<br/>
        /// <code>"[opacity=0.5][i][b][u][fg=Red][shadow=Black 1 1]Enter a value[/shadow][/fg][/u][/b][/i][/opacity]"</code><para/>
        /// Recommended to surround your text with "[opacity=0.5][i] ... [/i][/opacity]", such as:<para/>
        /// "[opacity=0.5][i]Enter a value[/i][/opacity]"</summary>
        public string PlaceholderText
        {
            get => _PlaceholderText;
            set
            {
                if (_PlaceholderText != value)
                {
                    _PlaceholderText = value;
                    PlaceholderTextBlockElement.Text = PlaceholderText;
                    UpdatePlaceholderVisibility();
                    NPC(nameof(PlaceholderText));
                }
            }
        }

        private void UpdatePlaceholderVisibility()
        {
            PlaceholderTextBlockElement.Visibility = !string.IsNullOrEmpty(PlaceholderText) && string.IsNullOrEmpty(Text) ? Visibility.Visible : Visibility.Collapsed;
        }
        #endregion Placeholder Text

        internal TextRenderInfo TextRenderInfo { get; }

        /// <summary>The minimum # of lines to display, regardless of how many lines the actual text content requires.<para/>
        /// Default value: 0<para/>
        /// See also: <see cref="MaxLines"/>, <see cref="MGTextBlock.MinLines"/>, <see cref="MGTextBlock.MaxLines"/></summary>
        public int MinLines
        {
            get => TextBlockElement.MinLines;
            set
            {
                if (TextBlockElement.MinLines != value)
                {
                    TextBlockElement.MinLines = value;
                    NPC(nameof(MinLines));
                }
            }
        }

        /// <summary>The maximum # of lines to display, regardless of how many lines the actual text content requires.<br/>
        /// Use null to indicate there is no maximum.<para/>
        /// Default value: null<para/>
        /// See also: <see cref="MinLines"/>, <see cref="MGTextBlock.MinLines"/>, <see cref="MGTextBlock.MaxLines"/></summary>
        public int? MaxLines
        {
            get => TextBlockElement.MaxLines;
            set
            {
                if (TextBlockElement.MaxLines != value)
                {
                    TextBlockElement.MaxLines = value;
                    NPC(nameof(MaxLines));
                }
            }
        }

        public bool WrapText
        {
            get => TextBlockElement.WrapText;
            set
            {
                if (TextBlockElement.WrapText != value)
                {
                    TextBlockElement.WrapText = value;
                    NPC(nameof(WrapText));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int? _CharacterLimit;
        /// <summary>The maximum # of characters that can be inputted in this <see cref="MGTextBox"/>. Use null for no character limit.<para/>
        /// Some characters are automatically added to this <see cref="MGTextBox"/>,<br/>
        /// such as hyphens that are appended at the end of a line when a single word's width exceeds the line's width and the word must wrap across multiple lines.<br/>
        /// These characters do not count towards the limit.<para/>
        /// Linebreaks count as 1 character ('\n'). Tabs are typically treated as 4 spaces and thus count as 4 characters. See also: <see cref="TabSpacesCount"/></summary>
        public int? CharacterLimit
        {
            get => _CharacterLimit;
            set
            {
                if (_CharacterLimit != value)
                {
                    int? Previous = CharacterLimit;
                    _CharacterLimit = value;
                    if (CharacterLimit.HasValue && Text.Length > CharacterLimit)
                    {
                        SetText(GetTextBackingField().Substring(0, CharacterLimit.Value));
                    }
                    UpdateCharacterCountText();
                    NPC(nameof(CharacterLimit));
                    OnCharacterLimitChanged?.Invoke(this, new(Previous, CharacterLimit));
                }
            }
        }

        public event EventHandler<EventArgs<int?>> OnCharacterLimitChanged;

        /// <summary>Provides direct access to the textblock component that displays the character counts when <see cref="ShowCharacterCount"/> is true.</summary>
        public MGComponent<MGTextBlock> CharacterCountComponent { get; }
        private MGTextBlock CharacterCountElement { get; }

        /// <summary>If true, the current character count will be shown in the bottom-right corner of this <see cref="MGTextBox"/>.<para/>
        /// If <see cref="CharacterLimit"/> has a non-null value, the counter will also display the limit, such as "100 / 500"</summary>
        public bool ShowCharacterCount
        {
            get => CharacterCountElement.Visibility == Visibility.Visible;
            set
            {
                if (ShowCharacterCount != value)
                {
                    CharacterCountElement.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                    if (CharacterCountElement.Visibility == Visibility.Visible)
                        UpdateCharacterCountText();
                    NPC(nameof(ShowCharacterCount));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _LimitedCharacterCountFormatString;
        /// <summary>Only relevant if <see cref="ShowCharacterCount"/> is true and <see cref="CharacterLimit"/> is not null.<para/>
        /// A format string to use when computing the character-count text displayed in the bottom-right corner of this <see cref="MGTextBox"/>.<br/>
        /// "{{CharacterCount}}" and "{{CharacterLimit}}" will be replaced with their actual underlying values when formatting the string.<para/>
        /// Default value:<code>"[b]{{CharacterCount}}[/b] / [b]{{CharacterLimit}}[/b]"</code><para/>
        /// This value supports some basic text markdown, such as "[b]" for bold text, "[fg=Red]" to set the text foreground color to a given value, "[opacity=0.5]" etc.<para/>
        /// See also: <see cref="ShowCharacterCount"/>, <see cref="CharacterLimit"/>, <see cref="LimitlessCharacterCountFormatString"/></summary>
        public string LimitedCharacterCountFormatString
        {
            get => _LimitedCharacterCountFormatString;
            set
            {
                if (_LimitedCharacterCountFormatString != value)
                {
                    _LimitedCharacterCountFormatString = value;
                    if (CharacterLimit.HasValue)
                        UpdateCharacterCountText();
                    NPC(nameof(LimitedCharacterCountFormatString));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _LimitlessCharacterCountFormatString;
        /// <summary>Only relevant if <see cref="ShowCharacterCount"/> is true and <see cref="CharacterLimit"/> is null.<para/>
        /// A format string to use when computing the character-count text displayed in the bottom-right corner of this <see cref="MGTextBox"/>.<br/>
        /// "{{CharacterCount}}" will be replaced with its actual underlying value when formatting the string.<para/>
        /// Default value:<code>"[b]{{CharacterCount}}[/b] character(s)"</code><para/>
        /// This value supports some basic text markdown, such as "[b]" for bold text, "[fg=Red]" to set the text foreground color to a given value, "[opacity=0.5]" etc.<para/>
        /// See also: <see cref="ShowCharacterCount"/>, <see cref="CharacterLimit"/>, <see cref="LimitedCharacterCountFormatString"/></summary>
        public string LimitlessCharacterCountFormatString
        {
            get => _LimitlessCharacterCountFormatString;
            set
            {
                if (_LimitlessCharacterCountFormatString != value)
                {
                    _LimitlessCharacterCountFormatString = value;
                    if (!CharacterLimit.HasValue)
                        UpdateCharacterCountText();
                    NPC(nameof(LimitlessCharacterCountFormatString));
                }
            }
        }

        private void UpdateCharacterCountText()
        {
            if (ShowCharacterCount)
            {
                string Value;
                if (CharacterLimit.HasValue && !string.IsNullOrEmpty(LimitedCharacterCountFormatString))
                    Value = LimitedCharacterCountFormatString.Replace("{{CharacterCount}}", (Text?.Length ?? 0).ToString()).Replace("{{CharacterLimit}}", CharacterLimit.Value.ToString());
                else if (!CharacterLimit.HasValue && !string.IsNullOrEmpty(LimitlessCharacterCountFormatString))
                    Value = LimitlessCharacterCountFormatString.Replace("{{CharacterCount}}", (Text?.Length ?? 0).ToString());
                else
                    Value = $"{Text?.Length ?? 0}";
                CharacterCountElement.Text = Value;
            }
        }
        #endregion Text

        #region Selection
        private bool _AllowsTextSelection = true;
        /// <summary>True if the user should be able to click+drag to select Text.<para/>
        /// Default value: true</summary>
        public bool AllowsTextSelection
        {
            get => _AllowsTextSelection;
            set
            {
                if (_AllowsTextSelection != value)
                {
                    _AllowsTextSelection = value;
                    NPC(nameof(AllowsTextSelection));
                    if (!AllowsTextSelection)
                        CurrentSelection = null;
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool IsDraggingSelection { get; set; } = false;

        public record struct TextSelection(int Index1, int Index2)
        {
            public int StartIndex => Math.Min(Index1, Index2);
            public int EndIndex => Math.Max(Index1, Index2);
            public int Length => EndIndex - StartIndex;

            public int ActualStartIndex(string Text) => Math.Clamp(StartIndex, 0, Text.Length);
            public int ActualEndIndex(string Text) => Math.Clamp(EndIndex, 0, Text.Length);
            public int ActualLength(string Text) => ActualEndIndex(Text) - ActualStartIndex(Text);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private TextSelection? _CurrentSelection;
        /// <summary>The currently-selected substring.<para/>
        /// Recommended to use <see cref="TrySelectText(string, bool)"/> or <see cref="SelectAll"/> instead of directly setting this property.<para/>
        /// See also: <see cref="SelectionChanged"/></summary>
        public TextSelection? CurrentSelection
        {
            get => _CurrentSelection;
            set
            {
                TextSelection? ActualValue = !AllowsTextSelection ? null : value;
                if (_CurrentSelection != ActualValue)
                {
                    TextSelection? Previous = CurrentSelection;
                    _CurrentSelection = ActualValue;
                    UpdateFormattedText(true);
                    //if (CurrentSelection.HasValue)
                    //    CurrentCursorPosition = null;
                    NPC(nameof(CurrentSelection));
                    SelectionChanged?.Invoke(this, new(Previous, CurrentSelection));
                }
            }
        }

        /// <summary>Invoked after <see cref="CurrentSelection"/> changes.</summary>
        public event EventHandler<EventArgs<TextSelection?>> SelectionChanged;

        public void SelectAll()
        {
            CurrentSelection = new(0, TextRenderInfo.GetLastChar().IndexInOriginalText + 1);
        }

        /// <summary>Attempts to select the given <paramref name="Text"/>. Does not modify the selection if the <paramref name="Text"/> was not found.</summary>
        /// <param name="FirstOccurrence">If true, the first occurrence of the given <paramref name="Text"/> will be selected, if found within this.<see cref="Text"/>.<para/>
        /// If false, the last occurrence of the given <paramref name="Text"/> will be selected, if found within this.<see cref="Text"/></param>
        public bool TrySelectText(string Text, bool FirstOccurrence = true)
        {
            if (string.IsNullOrEmpty(Text))
                return false;

            int Index = FirstOccurrence ? this.Text.IndexOf(Text) : this.Text.LastIndexOf(Text);
            if (Index < 0)
                return false;
            else
            {
                this.CurrentSelection = new(Index, Index + Text.Length);
                return true;
            }
        }

        #region Colors
        #region Focused
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Color _FocusedSelectionForegroundColor;
        /// <summary>The text foreground color of the selected text while this <see cref="MGTextBox"/> has focus.<para/>
        /// See also: <see cref="FocusedSelectionForegroundColor"/>, <see cref="FocusedSelectionBackgroundColor"/>, 
        /// <see cref="UnfocusedSelectionForegroundColor"/>, <see cref="UnfocusedSelectionBackgroundColor"/></summary>
        public Color FocusedSelectionForegroundColor
        {
            get => _FocusedSelectionForegroundColor;
            set
            {
                if (_FocusedSelectionForegroundColor != value)
                {
                    _FocusedSelectionForegroundColor = value;
                    FocusedSelectionForegroundColorString = ColorTranslator.ToHtml(FocusedSelectionForegroundColor.AsDrawingColor());
                    NPC(nameof(FocusedSelectionForegroundColor));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _FocusedSelectionForegroundColorString;
        private string FocusedSelectionForegroundColorString
        {
            get => _FocusedSelectionForegroundColorString;
            set
            {
                if (_FocusedSelectionForegroundColorString != value)
                {
                    _FocusedSelectionForegroundColorString = value;
                    if (CurrentSelection.HasValue)
                        UpdateFormattedText(true);
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Color _FocusedSelectedBackgroundColor;
        /// <summary>The text background color of the selected text while this <see cref="MGTextBox"/> has focus.<para/>
        /// See also: <see cref="FocusedSelectionForegroundColor"/>, <see cref="FocusedSelectionBackgroundColor"/>, 
        /// <see cref="UnfocusedSelectionForegroundColor"/>, <see cref="UnfocusedSelectionBackgroundColor"/></summary>
        public Color FocusedSelectionBackgroundColor
        {
            get => _FocusedSelectedBackgroundColor;
            set
            {
                if (_FocusedSelectedBackgroundColor != value)
                {
                    _FocusedSelectedBackgroundColor = value;
                    FocusedSelectionBackgroundColorString = ColorTranslator.ToHtml(FocusedSelectionBackgroundColor.AsDrawingColor());
                    NPC(nameof(FocusedSelectionBackgroundColor));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _FocusedSelectionBackgroundColorString;
        private string FocusedSelectionBackgroundColorString
        {
            get => _FocusedSelectionBackgroundColorString;
            set
            {
                if (_FocusedSelectionBackgroundColorString != value)
                {
                    _FocusedSelectionBackgroundColorString = value;
                    if (CurrentSelection.HasValue)
                        UpdateFormattedText(true);
                }
            }
        }
        #endregion Focused

        #region Unfocused
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Color _UnfocusedSelectionForegroundColor;
        /// <summary>The text foreground color of the selected text while this <see cref="MGTextBox"/> does NOT have focus.<para/>
        /// See also: <see cref="FocusedSelectionForegroundColor"/>, <see cref="FocusedSelectionBackgroundColor"/>, 
        /// <see cref="UnfocusedSelectionForegroundColor"/>, <see cref="UnfocusedSelectionBackgroundColor"/></summary>
        public Color UnfocusedSelectionForegroundColor
        {
            get => _UnfocusedSelectionForegroundColor;
            set
            {
                if (_UnfocusedSelectionForegroundColor != value)
                {
                    _UnfocusedSelectionForegroundColor = value;
                    UnfocusedSelectionForegroundColorString = ColorTranslator.ToHtml(UnfocusedSelectionForegroundColor.AsDrawingColor());
                    NPC(nameof(UnfocusedSelectionForegroundColor));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _UnfocusedSelectionForegroundColorString;
        private string UnfocusedSelectionForegroundColorString
        {
            get => _UnfocusedSelectionForegroundColorString;
            set
            {
                if (_UnfocusedSelectionForegroundColorString != value)
                {
                    _UnfocusedSelectionForegroundColorString = value;
                    if (CurrentSelection.HasValue)
                        UpdateFormattedText(true);
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Color _UnfocusedSelectionBackgroundColor;
        /// <summary>The text background color of the selected text while this <see cref="MGTextBox"/> does NOT have focus.<para/>
        /// See also: <see cref="FocusedSelectionForegroundColor"/>, <see cref="FocusedSelectionBackgroundColor"/>, 
        /// <see cref="UnfocusedSelectionForegroundColor"/>, <see cref="UnfocusedSelectionBackgroundColor"/></summary>
        public Color UnfocusedSelectionBackgroundColor
        {
            get => _UnfocusedSelectionBackgroundColor;
            set
            {
                if (_UnfocusedSelectionBackgroundColor != value)
                {
                    _UnfocusedSelectionBackgroundColor = value;
                    UnfocusedSelectionBackgroundColorString = ColorTranslator.ToHtml(UnfocusedSelectionBackgroundColor.AsDrawingColor());
                    NPC(nameof(UnfocusedSelectionBackgroundColor));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _UnfocusedSelectionBackgroundColorString;
        private string UnfocusedSelectionBackgroundColorString
        {
            get => _UnfocusedSelectionBackgroundColorString;
            set
            {
                if (_UnfocusedSelectionBackgroundColorString != value)
                {
                    _UnfocusedSelectionBackgroundColorString = value;
                    if (CurrentSelection.HasValue)
                        UpdateFormattedText(true);
                }
            }
        }
        #endregion Unfocused
        #endregion Colors
        #endregion Selection

        #region Mouse History
        private record struct MousePressMetadata(Vector2 Position)
        {
            public DateTime Timestamp { get; init; } = DateTime.Now;
            public bool IsRecent(TimeSpan Interval) => DateTime.Now.Subtract(Timestamp) <= Interval;
            public bool IsNearby(Vector2 Position, int Threshold = 8) => Vector2.DistanceSquared(this.Position, Position) <= Threshold * Threshold;
        }

        private static readonly TimeSpan DoublePressInterval = TimeSpan.FromSeconds(0.4);
        private static readonly TimeSpan TriplePressInterval = TimeSpan.FromSeconds(0.6);
        private static readonly TimeSpan QuadruplePressInterval = TimeSpan.FromSeconds(0.8);

        private const int MousePressHistorySize = 10;
        private readonly List<MousePressMetadata> MousePressHistory = new();
        private void AddMousePressHistory(Vector2 Position, out bool IsDoublePress, out bool IsTriplePress, out bool IsQuadruplePress)
        {
            MousePressHistory.Add(new(Position));
            if (MousePressHistory.Count > MousePressHistorySize)
                MousePressHistory.RemoveAt(0);

            IsDoublePress = MousePressHistory.Count >= 2 && MousePressHistory[^2].IsRecent(DoublePressInterval) && MousePressHistory[^2].IsNearby(Position);
            IsTriplePress = MousePressHistory.Count >= 3 && MousePressHistory.Skip(MousePressHistory.Count - 3).All(x => x.IsRecent(TriplePressInterval) && x.IsNearby(Position));
            IsQuadruplePress = MousePressHistory.Count >= 4 && MousePressHistory.Skip(MousePressHistory.Count - 4).All(x => x.IsRecent(QuadruplePressInterval) && x.IsNearby(Position));
        }
        #endregion Mouse History

        #region Undo / Redo
        private class LimitedStack<T>
        {
            private List<T> Stack;
            public int Count => Stack.Count;

            private int Limit;
            /// <summary>If the previous limit is greater than <paramref name="Value"/>, the oldest items will be removed.</summary>
            /// <param name="Value"></param>
            public void SetLimit(int Value)
            {
                if (Value <= 0)
                    throw new ArgumentOutOfRangeException($"{nameof(LimitedStack<T>)}.{nameof(Limit)} cannot be <= 0.");

                if (Value != Limit)
                {
                    if (Value > Count)
                    {
                        Limit = Value;
                    }
                    else
                    {
                        Stack = new(Stack.Skip(Count - Value));
                    }
                }
            }

            public LimitedStack(int Limit)
            {
                this.Limit = Limit;
                Stack = new(Limit);
            }

            public void Clear() => Stack.Clear();

            public T Peek() => Stack[^1];

            public void Push(T Item)
            {
                if (Stack.Count == Limit) 
                    Stack.RemoveAt(0);
                Stack.Add(Item);
            }

            public T Pop()
            {
                T Item = Peek();
                Stack.RemoveAt(Stack.Count - 1);
                return Item;
            }

            public bool TryPop(out T Item)
            {
                if (Stack.Count > 0)
                {
                    Item = Pop();
                    return true;
                }
                else
                {
                    Item = default;
                    return false;
                }
            }

            public override string ToString() => $"{nameof(LimitedStack<T>)}: {Count} / {Limit} (Top={Peek})";
        }

        public const int DefaultUndoRedoHistorySize = 20;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _UndoRedoHistorySize;
        /// <summary>The maximum number of undo or redo states that will be kept in memory. Decreasing this value may result in the oldest undo or redo states being lost.<para/>
        /// Default value: <see cref="DefaultUndoRedoHistorySize"/></summary>
        public int UndoRedoHistorySize
        {
            get => _UndoRedoHistorySize;
            set
            {
                if (_UndoRedoHistorySize != value)
                {
                    _UndoRedoHistorySize = value;
                    UndoStack.SetLimit(UndoRedoHistorySize);
                    RedoStack.SetLimit(UndoRedoHistorySize);
                    NPC(nameof(UndoRedoHistorySize));
                }
            }
        }

        private record struct RestorableState(MGTextCaret.CaretPosition? CaretPosition, TextSelection? Selection, string Text);
        private RestorableState CreateRestorableState() => new(Caret.Position, CurrentSelection, GetTextBackingField());

        private readonly LimitedStack<RestorableState> UndoStack = new(DefaultUndoRedoHistorySize);

        private void AddUndoState(RestorableState State) => UndoStack.Push(State);

        private bool IsExecutingUndoRedo = false;

        public bool TryUndo()
        {
            if (IsReadonly || !IsEnabled)
                return false;

            try
            {
                IsExecutingUndoRedo = true;
                if (UndoStack.TryPop(out RestorableState State))
                {
                    RestorableState RedoState = CreateRestorableState();
                    if (SetText(State.Text))
                    {
                        this.Caret.Position = State.CaretPosition;
                        this.CurrentSelection = State.Selection;
                        AddRedoState(RedoState);
                        return true;
                    }
                }

                return false;
            }
            finally { IsExecutingUndoRedo = false; }
        }

        private readonly LimitedStack<RestorableState> RedoStack = new(DefaultUndoRedoHistorySize);

        private void AddRedoState(RestorableState State) => RedoStack.Push(State);
        private void ClearRedoStack() => RedoStack.Clear();

        public bool TryRedo()
        {
            if (IsReadonly || !IsEnabled)
                return false;

            try
            {
                IsExecutingUndoRedo = true;
                if (RedoStack.TryPop(out RestorableState State))
                {
                    RestorableState UndoState = CreateRestorableState();
                    if (SetText(State.Text))
                    {
                        this.Caret.Position = State.CaretPosition;
                        this.CurrentSelection = State.Selection;
                        AddUndoState(UndoState);
                        return true;
                    }
                }

                return false;
            }
            finally { IsExecutingUndoRedo = false; }
        }
        #endregion Undo / Redo

        /// <summary>Attempts to set this element as the value for <see cref="MGDesktop.FocusedKeyboardHandler"/> at the end of the next update tick.</summary>
        public void RequestFocus() => GetDesktop().QueuedFocusedKeyboardHandler = this;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _IsReadonly;
        public bool IsReadonly
        {
            get => _IsReadonly;
            set
            {
                if (_IsReadonly != value)
                {
                    _IsReadonly = value;
                    if (IsReadonly)
                    {
                        this.IsDraggingSelection = false;
                        this.Caret.Position = null;
                        this.CurrentSelection = null;
                    }

                    NPC(nameof(IsReadonly));
                    ReadonlyChanged?.Invoke(this, IsReadonly);
                }
            }
        }

        /// <summary>Invoked when <see cref="IsReadonly"/> changes.</summary>
        public event EventHandler<bool> ReadonlyChanged;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _AcceptsReturn;
        /// <summary>Note: This feature is not available for <see cref="MGPasswordBox"/>.<para/>
        /// Default value: true</summary>
        public virtual bool AcceptsReturn
        {
            get => _AcceptsReturn;
            set
            {
                if (_AcceptsReturn != value)
                {
                    _AcceptsReturn = value;
                    NPC(nameof(AcceptsReturn));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _AcceptsTab;
        /// <summary>Note: This feature is not available for <see cref="MGPasswordBox"/>.<para/>
        /// Default value: true</summary>
        public virtual bool AcceptsTab
        {
            get => _AcceptsTab;
            set
            {
                if (_AcceptsTab != value)
                {
                    _AcceptsTab = value;
                    NPC(nameof(AcceptsTab));
                }
            }
        }

        private const int TabSpacesCount = 4;

        private record struct RecentKeyPress(BaseKeyPressedEventArgs KeyArgs)
        {
            public DateTime PressedAt { get; } = DateTime.Now;
            public DateTime RepeatedAt { get; set; } = DateTime.Now;
            public bool PressedWithin(TimeSpan t) => DateTime.Now.Subtract(PressedAt) <= t;
            public bool RepeatedWithin(TimeSpan t) => DateTime.Now.Subtract(RepeatedAt) <= t;
        }

        private RecentKeyPress? LastKeyPress { get; set; } = null;

        [DebuggerBrowsable (DebuggerBrowsableState.Never)]
        private bool _IsHeldKeyRepeated = true;
        /// <summary>If true, the most-recently pressed key will be repeatedly inputted (~30 times/second). Default value: true<br/>
        /// Some keys might not be repeated, such as special characters, or special keyboard shortcuts like 'Ctrl+C'.<para/>
        /// EX: Text="". Press 'A'. Text="A". Keep holding 'A' for about 1 second. Text="AAAAAAAAAAAAAAAA". Release 'A'.<para/>
        /// See also: <see cref="InitialKeyRepeatDelay"/>, <see cref="KeyRepeatInterval"/></summary>
        public bool IsHeldKeyRepeated
        {
            get => _IsHeldKeyRepeated;
            set
            {
                if (_IsHeldKeyRepeated != value)
                {
                    _IsHeldKeyRepeated = value;
                    NPC(nameof(IsHeldKeyRepeated));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private TimeSpan _InitialKeyRepeatDelay = TimeSpan.FromSeconds(0.5);
        /// <summary>The initial delay before a pressed key will be repeatedly inputted.<para/>
        /// See also: <see cref="IsHeldKeyRepeated"/> <see cref="KeyRepeatInterval"/></summary>
        public TimeSpan InitialKeyRepeatDelay
        {
            get => _InitialKeyRepeatDelay;
            set
            {
                if (_InitialKeyRepeatDelay != value)
                {
                    _InitialKeyRepeatDelay = value;
                    NPC(nameof(InitialKeyRepeatDelay));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private TimeSpan _KeyRepeatInterval = TimeSpan.FromSeconds(1.0 / 30); // 30 repetitions per second when holding down a key
        /// <summary>How often to repeatedly input the most-recently pressed key.<para/>
        /// See also: <see cref="IsHeldKeyRepeated"/>, <see cref="InitialKeyRepeatDelay"/></summary>
        public TimeSpan KeyRepeatInterval
        {
            get => _KeyRepeatInterval;
            set
            {
                if (_KeyRepeatInterval != value)
                {
                    _KeyRepeatInterval = value;
                    NPC(nameof(KeyRepeatInterval));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private TextEntryMode _TextEntryMode = UI.TextEntryMode.Insert;
        public TextEntryMode TextEntryMode
        {
            get => _TextEntryMode;
            set
            {
                if (_TextEntryMode != value)
                {
                    _TextEntryMode = value;
                    NPC(nameof(TextEntryMode));
                }
            }
        }

        public MGTextCaret Caret { get; }

        private StringClipboard Clipboard { get; } = new();

        #region Resizing
        /// <summary>Provides direct access to the resizer grip that appears in the bottom-right corner of this textbox when <see cref="IsUserResizable"/> is true.</summary>
        public MGComponent<MGResizeGrip> ResizeGripComponent { get; }
        private MGResizeGrip ResizeGripElement { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _IsUserResizable;
        /// <summary>If true, a <see cref="MGResizeGrip"/> will be visible in the bottom-right corner of the window, 
        /// allowing the user to click+drag it to adjust this <see cref="MGElement"/>'s <see cref="MGElement.PreferredWidth"/> / <see cref="MGElement.PreferredHeight"/></summary>
        public bool IsUserResizable
        {
            get => _IsUserResizable;
            set
            {
                _IsUserResizable = value;
                ResizeGripElement.Visibility = IsUserResizable ? Visibility.Visible : Visibility.Collapsed;
                NPC(nameof(IsUserResizable));
            }
        }
        #endregion Resizing

        /// <param name="CharacterLimit">Use null for no limit. Recommended to set this to a reasonable value to avoid performance issues.</param>
        public MGTextBox(MGWindow Window, int? CharacterLimit = 1000, bool ShowCharacterCount = false, bool IsUserResizable = false)
            : this(Window, MGElementType.TextBox, CharacterLimit, ShowCharacterCount, IsUserResizable)
        {

        }

        protected MGTextBox(MGWindow Window, MGElementType ElementType, int? CharacterLimit, bool ShowCharacterCount, bool IsUserResizable) 
            : base(Window, ElementType)
        {
            using (BeginInitializing())
            {
                MGTheme Theme = GetTheme();

                this.BorderElement = new(Window);
                this.BorderComponent = MGComponentBase.Create(BorderElement);
                AddComponent(BorderComponent);
                BorderElement.OnBorderBrushChanged += (sender, e) => { NPC(nameof(BorderBrush)); };
                BorderElement.OnBorderThicknessChanged += (sender, e) => { NPC(nameof(BorderThickness)); };

                this.ResizeGripElement = new(Window);
                this.ResizeGripComponent = MGComponentBase.Create(ResizeGripElement);
                AddComponent(ResizeGripComponent);
                this.IsUserResizable = IsUserResizable;

                this.PlaceholderTextBlockElement = new(Window, "");
                this.PlaceholderTextBlockComponent = new(PlaceholderTextBlockElement, ComponentUpdatePriority.AfterContents, ComponentDrawPriority.BeforeSelf,
                    true, true, false, false, false, false, true,
                    (AvailableBounds, ComponentSize) => AvailableBounds.GetCompressed(Padding));
                AddComponent(PlaceholderTextBlockComponent);
                this.PlaceholderText = null;
                this.PlaceholderTextBlockElement.Visibility = Visibility.Collapsed;

                this.CharacterCountElement = new(Window, (Text?.Length ?? 0).ToString());
                this.CharacterCountElement.Margin = new(0, 0, 8, 4);
                this.CharacterCountElement.TrySetFont(GetDesktop().FontManager.DefaultFontFamily, 9);
                this.CharacterCountComponent = new(CharacterCountElement, ComponentUpdatePriority.AfterContents, ComponentDrawPriority.BeforeSelf,
                    true, false, false, false, false, true, true,
                    (AvailableBounds, ComponentSize) => ApplyAlignment(AvailableBounds, HorizontalAlignment.Right, VerticalAlignment.Bottom, ComponentSize.Size));
                AddComponent(CharacterCountComponent);
                this.ShowCharacterCount = ShowCharacterCount;

                this.LimitedCharacterCountFormatString = "[b]{{CharacterCount}}[/b] / [b]{{CharacterLimit}}[/b]";
                this.LimitlessCharacterCountFormatString = "[b]{{CharacterCount}}[/b] character(s)";

                this.TextBlockElement = new(Window, "");
                TextBlockElement.ClipToBounds = false;
                this.TextBlockComponent = new(TextBlockElement, ComponentUpdatePriority.AfterContents, ComponentDrawPriority.BeforeSelf, 
                    false, false, true, true, false, false, true,
                    (AvailableBounds, ComponentSize) => AvailableBounds.GetCompressed(Padding));
                AddComponent(TextBlockComponent);

                this.TextRenderInfo = new(this, TextBlockElement);

                this.Padding = new(6, 2, 6, 2);

                this.MinHeight = 26;

                this.IsReadonly = false;
                this.CharacterLimit = CharacterLimit;

                this.AcceptsReturn = true;
                this.AcceptsTab = true;

                this.Caret = new(this, TextBlockElement);

                this.FocusedSelectionForegroundColor = Theme.TextBoxFocusedSelectionForeground;
                this.FocusedSelectionBackgroundColor = Theme.TextBoxFocusedSelectionBackground;
                this.UnfocusedSelectionForegroundColor = Theme.TextBoxUnfocusedSelectionForeground;
                this.UnfocusedSelectionBackgroundColor = Theme.TextBoxUnfocusedSelectionBackground;

                MouseHandler.LMBPressedInside += (sender, e) =>
                {
                    try
                    {
                        Point Position = ConvertCoordinateSpace(CoordinateSpace.Screen, CoordinateSpace.Layout, e.Position);
                        AddMousePressHistory(Position.ToVector2(), out bool IsDoublePress, out bool IsTriplePress, out bool IsQuadruplePress);

                        GetDesktop().QueuedFocusedKeyboardHandler = this;

                        Caret.MoveToApproximateScreenPosition(Position.ToVector2());

                        CurrentSelection = null;
                        if (TextRenderInfo.TryGetCharAtScreenPosition(Position.ToVector2(), out CharRenderInfo CharInfo))
                        {
                            if (IsQuadruplePress)
                                SelectAll();
                            else if (IsTriplePress)
                                this.CurrentSelection = new(CharInfo.Line.FirstCharacter.IndexInOriginalText, CharInfo.Line.LastCharacter.IndexInOriginalText + 1);
                            else if (IsDoublePress)
                            {
                                int Index = CharInfo.IndexInOriginalText;
                                if (Index >= 0 && Index < Text.Length)
                                {
                                    bool ClickedWordCharacter = Regex.IsMatch(Text[Index].ToString(), @"\w");
                                    if (ClickedWordCharacter)
                                    {
                                        //  Get all word-characters to the left of the pressed character
                                        int PreviousCharacters = Text.Substring(0, Index).Reverse().TakeWhile(x => Regex.IsMatch(x.ToString(), @"\w")).Count();
                                        //  Get all word-characters to the right of the pressed character
                                        int NextCharacters = Text.Skip(Index).TakeWhile(x => Regex.IsMatch(x.ToString(), @"\w")).Count();

                                        //  Also include the next space if it's the first non-word character we find while traversing to the right
                                        if (CharInfo.IndexInOriginalText + NextCharacters < Text.Length && Text[CharInfo.IndexInOriginalText + NextCharacters] == ' ')
                                            NextCharacters++;

                                        this.CurrentSelection = new(CharInfo.IndexInOriginalText - PreviousCharacters, Math.Min(Text.Length, CharInfo.IndexInOriginalText + NextCharacters));
                                    }
                                    else
                                    {
                                        //  Get all non-word characters to the left of the pressed character
                                        int PreviousCharacters = Text.Substring(0, Index).Reverse().TakeWhile(x => Regex.IsMatch(x.ToString(), @"\W")).Count();
                                        //  Get all non-word characters to the right of the pressed character
                                        int NextCharacters = Text.Skip(Index).TakeWhile(x => Regex.IsMatch(x.ToString(), @"\W")).Count();

                                        this.CurrentSelection = new(CharInfo.IndexInOriginalText - PreviousCharacters, Math.Min(Text.Length, CharInfo.IndexInOriginalText + NextCharacters));
                                    }
                                }
                            }
                        }

                        e.SetHandledBy(this, false);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message + "\n" + ex.ToString());
                    }
                };

                MouseHandler.LMBReleasedInside += (sender, e) =>
                {
                    if (e.PressedArgs.HandledBy == this)
                        e.SetHandledBy(this, false);
                };

                MouseHandler.DragStart += (sender, e) =>
                {
                    if (e.IsLMB && AllowsTextSelection)
                    {
                        Point LayoutSpacePosition = ConvertCoordinateSpace(CoordinateSpace.Screen, CoordinateSpace.Layout, e.Position);
                        if (TextRenderInfo.TryGetCharAtScreenPosition(LayoutSpacePosition.ToVector2(), out CharRenderInfo CharInfo))
                        {
                            IsDraggingSelection = true;
                            bool IsLeftEdge = LayoutSpacePosition.X <= CharInfo.CenterX;
                            int SelectionIndex = IsLeftEdge ? CharInfo.IndexInOriginalText : CharInfo.IndexInOriginalText + 1;
                            CurrentSelection = new(SelectionIndex, SelectionIndex);
                            e.SetHandledBy(this, false);
                        }
                    }
                };

                MouseHandler.DragEnd += (sender, e) =>
                {
                    if (e.IsLMB)
                        IsDraggingSelection = false;
                };

                MouseHandler.Dragged += (sender, e) =>
                {
                    if (e.IsLMB && IsDraggingSelection && (Text?.Length ?? 0) > 0 && CurrentSelection.HasValue && AllowsTextSelection)
                    {
                        Point LayoutSpacePosition = ConvertCoordinateSpace(CoordinateSpace.Screen, CoordinateSpace.Layout, e.Position);

                        int LayoutBoundsVerticalPadding = 5;
                        if (LayoutSpacePosition.Y < LayoutBounds.Top - LayoutBoundsVerticalPadding)
                        {
                            int SelectionIndex = TextRenderInfo.GetFirstChar().IndexInOriginalText;
                            CurrentSelection = new(CurrentSelection.Value.Index1, SelectionIndex);
                        }
                        else if (LayoutSpacePosition.Y > LayoutBounds.Bottom + LayoutBoundsVerticalPadding)
                        {
                            int SelectionIndex = TextRenderInfo.GetLastChar().IndexInOriginalText + 1;
                            CurrentSelection = new(CurrentSelection.Value.Index1, SelectionIndex);
                        }
                        else if (TextRenderInfo.TryGetCharAtScreenPosition(LayoutSpacePosition.ToVector2(), out CharRenderInfo CharInfo))
                        {
                            bool IsLeftEdge = LayoutSpacePosition.X <= CharInfo.CenterX;
                            int SelectionIndex = IsLeftEdge ? CharInfo.IndexInOriginalText : CharInfo.IndexInOriginalText + 1;
                            //Debug.WriteLine($"Dragged from {CurrentSelection.Value.StartIndex} to {SelectionIndex}");
                            CurrentSelection = new(CurrentSelection.Value.Index1, SelectionIndex);
                        }

                        e.SetHandled(this, false);
                    }
                };

                GetDesktop().FocusedKeyboardHandlerChanged += (sender, e) =>
                {
                    if (e.PreviousValue == this || e.NewValue == this)
                    {
                        LastKeyPress = null;
                        UpdateFormattedText(true);
                    }
                };

                this.KeyboardHandler.Pressed += (sender, e) =>
                {
                    LastKeyPress = new(e);
                    HandleKeyPress(e);
                    e.SetHandledBy(this, false);
                };

                this.KeyboardHandler.Released += (sender, e) =>
                {
                    if (e.Key == LastKeyPress?.KeyArgs.Key)
                    {
                        LastKeyPress = null;
                    }
                };
            }
        }

        //TODO
        //Built-in scrolling? Could wrap the textblock inside a scrollviewer with scrollbar visibilities set to 'Hidden'.
        //Using the arrow keys adjusts the scrollviewer's horizontal/vertical offset

        private void HandleKeyPress(BaseKeyPressedEventArgs e)
        {
            //Debug.WriteLine($"{e.PrintableValue} {e.IsHandled}");
            if (!e.IsPrintableKey)
            {
                //  Handle non-printable keys such as arrow keys
                switch (e.Key)
                {
                    case Keys.Insert when !IsReadonly:
                        TextEntryMode = TextEntryMode == TextEntryMode.Insert ? TextEntryMode.Overwrite : TextEntryMode.Insert;
                        break;

                    case Keys.Left:
                        if (Caret.MoveLeft(1))
                            CurrentSelection = null;
                        break;
                    case Keys.Right:
                        if (Caret.MoveRight(1))
                            CurrentSelection = null;
                        break;
                    case Keys.Up:
                        if (Caret.MoveUp(1))
                            CurrentSelection = null;
                        break;
                    case Keys.Down:
                        if (Caret.MoveDown(1))
                            CurrentSelection = null;
                        break;

                    case Keys.Home:
                        if (Caret.MoveToStartOfCurrentLine())
                            CurrentSelection = null;
                        break;
                    case Keys.End:
                        if (Caret.MoveToEndOfCurrentLine())
                            CurrentSelection = null;
                        break;

                    case Keys.Back or Keys.Delete when !IsReadonly:
                        if (CurrentSelection.HasValue && CurrentSelection.Value.ActualLength(Text) > 0)
                        {
                            RestorableState UndoState = CreateRestorableState();
                            int SelectionStart = CurrentSelection.Value.ActualStartIndex(Text);
                            int SelectionEnd = CurrentSelection.Value.ActualEndIndex(Text);
                            string CurrentText = GetTextBackingField();
                            string NewValue = CurrentText.Substring(0, SelectionStart) + CurrentText.Substring(SelectionEnd);
                            if (SetText(NewValue))
                            {
                                AddUndoState(UndoState);
                                CurrentSelection = null;
                                TextBlockElement.UpdateLines();
                                _ = Caret.MoveToOriginalCharacterIndexOrEnd(SelectionStart, true);
                            }
                        }
                        else if (Caret.HasPosition)
                        {
                            bool IsBackspace = e.Key == Keys.Back;
                            bool IsDelete = e.Key == Keys.Delete;

                            int Offset = IsDelete ? 1 : 0;
                            int Index = Caret.Position.Value.IndexInOriginalText;

                            if ((IsBackspace && Index > 0) || (IsDelete && Index < Text.Length))
                            {
                                StringBuilder SB = new();
                                string CurrentText = GetTextBackingField();
                                string NewText;
                                SB.Append(CurrentText.AsSpan(0, Index - 1 + Offset));
                                if (Index < Text.Length)
                                    SB.Append(CurrentText.AsSpan(Index + Offset));
                                NewText = SB.ToString();
                                if (SetText(NewText))
                                {
                                    TextBlockElement.UpdateLines();

                                    if (IsBackspace)
                                        _ = Caret.MoveToOriginalCharacterIndexOrLeft(Caret.Position.Value.IndexInOriginalText - 1 + Offset, true);
                                    else if (IsDelete)
                                        _ = Caret.MoveToOriginalCharacterIndexOrRight(Caret.Position.Value.IndexInOriginalText - 1 + Offset, true);
                                }
                            }
                        }
                        break;
                }
            }
            else
            {
                bool Handled = false;

                if (e.Tracker.IsControlDown)
                {
                    //  Handle keyboard shortcuts such as Ctrl+C or Ctrl+V
                    bool IsKeyboardShortcut = e.Key is Keys.X or Keys.C or Keys.V or Keys.Z or Keys.Y or Keys.A or Keys.D;
                    if (IsKeyboardShortcut)
                    {
                        string CurrentText = GetTextBackingField();
                        switch (e.Key)
                        {
                            case Keys.X when !IsReadonly:
                                //  Cut
                                if (CurrentSelection.HasValue && CurrentSelection.Value.ActualLength(Text) > 0)
                                {
                                    RestorableState UndoState = CreateRestorableState();
                                    int SelectionStart = CurrentSelection.Value.ActualStartIndex(Text);
                                    int SelectionEnd = CurrentSelection.Value.ActualEndIndex(Text);
                                    string SelectedText = Text.Substring(SelectionStart, SelectionEnd - SelectionStart); // Could use CurrentText instead of Text to allow PasswordBoxes to copy the underlying text instead of the password characters *
                                    string NewValue = CurrentText.Substring(0, SelectionStart) + CurrentText.Substring(SelectionEnd);
                                    if (SetText(NewValue))
                                    {
                                        AddUndoState(UndoState);
                                        CurrentSelection = null;
                                        TextBlockElement.UpdateLines();
                                        _ = Caret.MoveToOriginalCharacterIndexOrRight(SelectionStart - 1, false);
                                        Clipboard.Text = SelectedText;
                                    }
                                }
                                Handled = true;
                                break;
                            case Keys.C:
                                if (CurrentSelection.HasValue && CurrentSelection.Value.ActualLength(Text) > 0)
                                {
                                    int SelectionStart = CurrentSelection.Value.ActualStartIndex(Text);
                                    int SelectionEnd = CurrentSelection.Value.ActualEndIndex(Text);
                                    string SelectedText = Text.Substring(SelectionStart, SelectionEnd - SelectionStart); // Could use CurrentText instead of Text to allow PasswordBoxes to copy the underlying text instead of the password character *
                                    Clipboard.Text = SelectedText;
                                }
                                Handled = true;
                                break;
                            case Keys.V when !IsReadonly:
                                //  Paste
                                string ClipboardText = Clipboard.Text;
                                if (!string.IsNullOrEmpty(ClipboardText))
                                {
                                    if (CurrentSelection.HasValue && CurrentSelection.Value.ActualLength(Text) > 0)
                                    {
                                        RestorableState UndoState = CreateRestorableState();
                                        int SelectionStart = CurrentSelection.Value.ActualStartIndex(Text);
                                        int SelectionEnd = CurrentSelection.Value.ActualEndIndex(Text);
                                        string NewValue = CurrentText.Substring(0, SelectionStart) + ClipboardText + CurrentText.Substring(SelectionEnd);
                                        if (SetText(NewValue))
                                        {
                                            AddUndoState(UndoState);
                                            CurrentSelection = null;
                                            TextBlockElement.UpdateLines();
                                            int NumCharactersInserted = ClipboardText.Length;
                                            _ = Caret.MoveToOriginalCharacterIndexOrRight(SelectionStart + NumCharactersInserted - 1, false);
                                        }
                                        //  We should still clear the selection even if pasting didn't change the text value.
                                        //  EX: Text="Foo", Clipboard="oo", Select the text "oo" and paste.
                                        //  Text attempts to change from "Foo" to "Foo", SetText returns false since nothing changed, so previous if-statement didn't execute
                                        else if (GetTextBackingField() == NewValue)
                                            CurrentSelection = null;
                                    }
                                    else if (Caret.HasPosition)
                                    {
                                        RestorableState UndoState = CreateRestorableState();
                                        int CaretIndex = Caret.Position.Value.IndexInOriginalText;
                                        string NewValue = CurrentText.Substring(0, CaretIndex) + ClipboardText + CurrentText.Substring(CaretIndex);
                                        if (SetText(NewValue))
                                        {
                                            AddUndoState(UndoState);
                                            CurrentSelection = null;
                                            TextBlockElement.UpdateLines();
                                            int NumCharactersInserted = ClipboardText.Length;
                                            _ = Caret.MoveToOriginalCharacterIndexOrRight(CaretIndex + NumCharactersInserted - 1, false);
                                        }
                                    }
                                }
                                Handled = true;
                                break;

                            case Keys.Z when !IsReadonly:
                                //  Undo
                                _ = TryUndo();
                                Handled = true;
                                break;
                            case Keys.Y when !IsReadonly:
                                //  Redo
                                _ = TryRedo();
                                Handled = true;
                                break;

                            case Keys.A:
                                SelectAll();
                                Handled = true;
                                break;

                            case Keys.D when !IsReadonly:
                                //  Duplicate current line
                                if (Caret.HasPosition && TextRenderInfo.TryGetCharAtScreenPosition(Caret.Position.Value.Bounds.Center.ToVector2(), out CharRenderInfo CharInfo))
                                {
                                    RestorableState UndoState = CreateRestorableState();
                                    int LineStart = CharInfo.Line.FirstCharacter.IndexInOriginalText;
                                    int LineEnd = CharInfo.Line.LastCharacter.IndexInOriginalText + 1;
                                    if (CurrentText.Substring(LineStart).Length >= (LineEnd - LineStart))
                                    {
                                        string LineText = CurrentText.Substring(LineStart, LineEnd - LineStart);
                                        string NewValue = CurrentText.Substring(0, LineStart) + LineText + '\n' + LineText + CurrentText.Substring(LineEnd);
                                        if (SetText(NewValue))
                                        {
                                            AddUndoState(UndoState);
                                            CurrentSelection = null;
                                            TextBlockElement.UpdateLines();
                                            int NumCharactersInserted = LineText.Length + 1; // +1 because of the linebreak character appended to the end of the line
                                                                                             //_ = Caret.MoveToOriginalCharacterIndexOrRight(CharInfo.IndexInOriginalText + NumCharactersInserted - 1, false);
                                        }
                                    }
                                }
                                Handled = true;
                                break;
                        }
                    }
                }

                if (!Handled && !IsReadonly && (AcceptsReturn || e.Key != Keys.Enter) && (AcceptsTab || e.Key != Keys.Tab))
                {
                    bool IsEnter = e.Key == Keys.Enter;
                    bool IsTab = e.Key == Keys.Tab;

                    if (CurrentSelection.HasValue && CurrentSelection.Value.ActualLength(Text) > 0)
                    {
                        RestorableState UndoState = CreateRestorableState();
                        int SelectionStart = CurrentSelection.Value.ActualStartIndex(Text);
                        int SelectionEnd = CurrentSelection.Value.ActualEndIndex(Text);
                        string CurrentText = GetTextBackingField();
                        string NewValue = CurrentText.Substring(0, SelectionStart) + e.PrintableValue + CurrentText.Substring(SelectionEnd);
                        if (SetText(NewValue))
                        {
                            AddUndoState(UndoState);
                            CurrentSelection = null;
                            TextBlockElement.UpdateLines();
                            int NumCharactersInserted = IsTab ? TabSpacesCount : 1;
                            _ = Caret.MoveToOriginalCharacterIndexOrEnd(SelectionStart + NumCharactersInserted - 1, false);
                        }
                    }
                    else if (Caret.HasPosition)
                    {
                        int Index = Caret.Position.Value.IndexInOriginalText;
                        StringBuilder SB = new();
                        string NewText;
                        string CurrentText = GetTextBackingField();

                        switch (TextEntryMode)
                        {
                            case TextEntryMode.Insert:
                                SB.Append(CurrentText.AsSpan(0, Index));
                                SB.Append(e.PrintableValue);
                                if (Index < Text.Length)
                                    SB.Append(CurrentText.AsSpan(Index));
                                NewText = SB.ToString();
                                break;
                            case TextEntryMode.Overwrite:
                                SB.Append(CurrentText.AsSpan(0, Index));
                                SB.Append(e.PrintableValue);
                                if (Index + 1 < Text.Length)
                                    SB.Append(CurrentText.AsSpan(Index + 1));
                                NewText = SB.ToString();
                                break;
                            default: throw new NotImplementedException($"Unrecognized {nameof(UI.TextEntryMode)}: {TextEntryMode}");
                        }

                        //Debug.WriteLine($"{nameof(MGTextBox)}: Insert key - {e.PrintableValue}");
                        if (SetText(NewText))
                            TextBlockElement.UpdateLines();
                        int NumCharactersInserted = IsTab ? TabSpacesCount : 1;
                        if (IsEnter)
                            _ = Caret.MoveToOriginalCharacterIndexOrLeft(Caret.Position.Value.IndexInOriginalText + NumCharactersInserted, true);
                        else
                            _ = Caret.MoveToOriginalCharacterIndexOrRight(Caret.Position.Value.IndexInOriginalText + NumCharactersInserted - 1, false);
                    }
                }
            }
        }

        public override void UpdateSelf(ElementUpdateArgs UA)
        {
            base.UpdateSelf(UA);
            if (IsEnabled && !IsReadonly && IsHitTestVisible && IsHeldKeyRepeated && GetDesktop().FocusedKeyboardHandler == this &&
                LastKeyPress.HasValue && !LastKeyPress.Value.PressedWithin(InitialKeyRepeatDelay) && !LastKeyPress.Value.RepeatedWithin(KeyRepeatInterval))
            {
                BaseKeyPressedEventArgs KeyArgs = LastKeyPress.Value.KeyArgs;

                //  Probably don't want to repeat things like function keys, INS, PRTSCR etc.
                //  Also probably don't want to repeat keys while control is pressed because that could repeatedly execute keyboard shortcuts like Ctrl+V (Or maybe we do want that?)
                bool IsRepeatableKey = !KeyArgs.Tracker.IsControlDown && (KeyArgs.IsPrintableKey || KeyArgs.Key is Keys.Back or Keys.Delete or Keys.Left or Keys.Right or Keys.Up or Keys.Down);
                if (IsRepeatableKey)
                {
                    LastKeyPress = LastKeyPress.Value with { RepeatedAt = DateTime.Now };
                    HandleKeyPress(LastKeyPress.Value.KeyArgs);
                }
            }
        }

        public override void DrawSelf(ElementDrawArgs DA, Rectangle LayoutBounds)
        {
            DrawSelfBaseImplementation(DA, LayoutBounds);
            Caret.Draw(DA, LayoutBounds);
        }
    }
}
