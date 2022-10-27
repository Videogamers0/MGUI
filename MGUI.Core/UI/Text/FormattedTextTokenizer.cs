using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MGUI.Core.UI.Text
{
    /// <summary>A token parsed from a piece of formatted text that contains markdown. These tokens are used as a first-pass, prior to more detailed parsing performed via <see cref="FTParser"/>.</summary>
    public enum FTTokenType
    {
        EscapeMarkdown,

        OpenTag,
        CloseTag,

        BoldOpenTagType,
        BoldCloseTagType,

        ItalicOpenTagType,
        ItalicCloseTagType,

        UnderlineOpenTagType,
        UnderlineCloseTagType,

        OpacityOpenTagType,
        OpacityCloseTagType,
        OpacityValue,

        ForegroundOpenTagType,
        ForegroundCloseTagType,
        ForegroundValue,

        BackgroundOpenTagType,
        BackgroundCloseTagType,
        BackgroundValue,

        ShadowOpenTagType,
        ShadowCloseTagType,
        ShadowValue,

        InvalidToken,

        StringValue,

        LineBreak
    }

    /// <summary>Describes a pattern to look for when tokenizing a piece of formatted text.</summary>
    public class FTTokenDefinition
    {
        private Regex Regex { get; }
        private FTTokenType Type { get; }
        private ReadOnlyCollection<FTTokenType?> PrecededBy { get; }
        private ReadOnlyCollection<FTTokenType?> NotPrecededBy { get; }

        public FTTokenDefinition(FTTokenType Type, string Pattern, IEnumerable<FTTokenType?> PrecededBy, IEnumerable<FTTokenType?> NotPrecededBy)
        {
            Regex = new Regex(Pattern);
            this.Type = Type;
            this.PrecededBy = PrecededBy.ToList().AsReadOnly();
            this.NotPrecededBy = NotPrecededBy.ToList().AsReadOnly();
        }

        public FTTokenMatch? Match(FTTokenType? Previous, string Input)
        {
            if (PrecededBy.Any() && !PrecededBy.Any(x => x == null && !Previous.HasValue || x != null && Previous.HasValue && x.Value == Previous.Value))
                return null;
            if (NotPrecededBy.Any(x => x == null && !Previous.HasValue || x != null && Previous.HasValue && x.Value == Previous.Value))
                return null;

            Match Match = Regex.Match(Input);
            if (Match.Success)
            {
                string RemainingText = "";
                if (Match.Length != Input.Length)
                    RemainingText = Input.Substring(Match.Length);

                return new FTTokenMatch(Type, Match.Value, RemainingText);
            }
            else
            {
                return null;
            }
        }
    }

    public record struct FTTokenMatch(FTTokenType TokenType, string Value, string RemainingText);

    /// <summary>Tokenizes a piece of formatted text that contains markdown into a list of <see cref="FTTokenType"/> tokens.</summary>
    public class FTTokenizer
    {
        public const char EscapeOpenTagChar = '\\';
        public const char OpenTagChar = '[';
        public const char CloseTagChar = ']';

        private List<FTTokenDefinition> Definitions { get; }
        private static IEnumerable<T> AsEnumerable<T>(params T[] values) => values;

        public FTTokenizer()
        {
            Definitions = new();

            List<FTTokenType> OpenTagTypesWithoutValues = new() {
                FTTokenType.BoldOpenTagType,
                FTTokenType.ItalicOpenTagType,
                FTTokenType.UnderlineOpenTagType
            };

            List<FTTokenType> OpenTagTypesWithValues = new() {
                FTTokenType.OpacityOpenTagType,
                FTTokenType.ForegroundOpenTagType,
                FTTokenType.BackgroundOpenTagType,
                FTTokenType.ShadowOpenTagType
            };

            List<FTTokenType> CloseTagTypes = new() {
                FTTokenType.BoldCloseTagType,
                FTTokenType.ItalicCloseTagType,
                FTTokenType.UnderlineCloseTagType,
                FTTokenType.OpacityCloseTagType,
                FTTokenType.ForegroundCloseTagType,
                FTTokenType.BackgroundCloseTagType,
                FTTokenType.ShadowCloseTagType
            };

            List<FTTokenType> TagValues = new() {
                FTTokenType.OpacityValue,
                FTTokenType.ForegroundValue,
                FTTokenType.BackgroundValue,
                FTTokenType.ShadowValue
            };

            string EscapedEscapeMarkdown = Regex.Escape(EscapeOpenTagChar.ToString());
            string EscapedOpenTag = Regex.Escape(OpenTagChar.ToString());
            string EscapedCloseTag = Regex.Escape(CloseTagChar.ToString());

            //  Escape Markdown
            Definitions.Add(new(FTTokenType.EscapeMarkdown, $@"^{EscapedEscapeMarkdown}(?={EscapedOpenTag})",
                AsEnumerable<FTTokenType?>(null, FTTokenType.CloseTag), //PrecededBy 
                AsEnumerable<FTTokenType?>(FTTokenType.InvalidToken))   //NotPrecededBy
            );

            //  Open Tag
            Definitions.Add(new(FTTokenType.OpenTag, $@"^{EscapedOpenTag}",
                AsEnumerable<FTTokenType?>(null, FTTokenType.CloseTag),
                AsEnumerable<FTTokenType?>(FTTokenType.EscapeMarkdown, FTTokenType.InvalidToken))
            );

            //  Close Tag
            Definitions.Add(new(FTTokenType.CloseTag, $@"^{EscapedCloseTag}",
                OpenTagTypesWithoutValues.Union(TagValues).Union(CloseTagTypes).Cast<FTTokenType?>(),
                Enumerable.Empty<FTTokenType?>())
            );

            //  Tag Types
            string BoldTypePattern = $@"(?i)(bold|b(?={EscapedCloseTag}))(?-i)";
            Definitions.Add(new(FTTokenType.BoldOpenTagType, $@"^{BoldTypePattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.OpenTag),
                Enumerable.Empty<FTTokenType?>())
            );
            Definitions.Add(new(FTTokenType.BoldCloseTagType, $@"^(\\|\/){BoldTypePattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.OpenTag),
                Enumerable.Empty<FTTokenType?>())
            );

            string ItalicTypePattern = $@"(?i)(italic|i(?={EscapedCloseTag}))(?-i)";
            Definitions.Add(new(FTTokenType.ItalicOpenTagType, $@"^{ItalicTypePattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.OpenTag),
                Enumerable.Empty<FTTokenType?>())
            );
            Definitions.Add(new(FTTokenType.ItalicCloseTagType, $@"^(\\|\/){ItalicTypePattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.OpenTag),
                Enumerable.Empty<FTTokenType?>())
            );

            string UnderlineTypePattern = $@"(?i)(underline|u(?={EscapedCloseTag}))(?-i)";
            Definitions.Add(new(FTTokenType.UnderlineOpenTagType, $@"^{UnderlineTypePattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.OpenTag),
                Enumerable.Empty<FTTokenType?>())
            );
            Definitions.Add(new(FTTokenType.UnderlineCloseTagType, $@"^(\\|\/){UnderlineTypePattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.OpenTag),
                Enumerable.Empty<FTTokenType?>())
            );

            string OpacityTypePattern = $@"(?i)(opacity|o(?=\=))(?-i)";
            Definitions.Add(new(FTTokenType.OpacityOpenTagType, $@"^{OpacityTypePattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.OpenTag),
                Enumerable.Empty<FTTokenType?>())
            );
            Definitions.Add(new(FTTokenType.OpacityCloseTagType, $@"^(\\|\/){OpacityTypePattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.OpenTag),
                Enumerable.Empty<FTTokenType?>())
            );

            string ForegroundTypePattern = $@"(?i)(fg|foreground|color|c(?=\=))(?-i)";
            Definitions.Add(new(FTTokenType.ForegroundOpenTagType, $@"^{ForegroundTypePattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.OpenTag),
                Enumerable.Empty<FTTokenType?>())
            );
            Definitions.Add(new(FTTokenType.ForegroundCloseTagType, $@"^(\\|\/){ForegroundTypePattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.OpenTag),
                Enumerable.Empty<FTTokenType?>())
            );

            string BackgroundTypePattern = @"(?i)(bg|background)(?-i)";
            Definitions.Add(new(FTTokenType.BackgroundOpenTagType, $@"^{BackgroundTypePattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.OpenTag),
                Enumerable.Empty<FTTokenType?>())
            );
            Definitions.Add(new(FTTokenType.BackgroundCloseTagType, $@"^(\\|\/){BackgroundTypePattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.OpenTag),
                Enumerable.Empty<FTTokenType?>())
            );

            string ShadowTypePattern = $@"(?i)(shadow|s(?=\=))(?-i)";
            Definitions.Add(new(FTTokenType.ShadowOpenTagType, $@"^{ShadowTypePattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.OpenTag),
                Enumerable.Empty<FTTokenType?>())
            );
            Definitions.Add(new(FTTokenType.ShadowCloseTagType, $@"^(\\|\/){ShadowTypePattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.OpenTag),
                Enumerable.Empty<FTTokenType?>())
            );

            //  Tag Values
            string OpacityPattern = $@"0?\.\d\d?";
            Definitions.Add(new(FTTokenType.OpacityValue, $@"^={OpacityPattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.OpacityOpenTagType),
                Enumerable.Empty<FTTokenType?>())
            );

            string ColorHexPattern = @"#[a-fA-F0-9]{6,8}";
            string ColorNamePattern = @"[a-zA-Z]+";
            string ColorPattern = $@"({ColorHexPattern}|{ColorNamePattern})";
            Definitions.Add(new(FTTokenType.ForegroundValue, $@"^={ColorPattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.ForegroundOpenTagType),
                Enumerable.Empty<FTTokenType?>())
            );
            Definitions.Add(new(FTTokenType.BackgroundValue, $@"^={ColorPattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.BackgroundOpenTagType),
                Enumerable.Empty<FTTokenType?>())
            );

            string ShadowValuePattern = $@"{ColorPattern}( -?\d{{1,2}} -?\d{{1,2}})?";
            Definitions.Add(new(FTTokenType.ShadowValue, $@"^={ShadowValuePattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.ShadowOpenTagType),
                Enumerable.Empty<FTTokenType?>())
            );

            //  Invalid Token
            Definitions.Add(new(FTTokenType.InvalidToken, @"^.*",
                Enumerable.Empty<FTTokenType?>(),
                AsEnumerable<FTTokenType?>(null, FTTokenType.CloseTag, FTTokenType.EscapeMarkdown))
            );

#if DEBUG
            try
            {
                string sample = "H[b]e\nllo\\[bold]W[bg=Red]orld[color=Green][shadow=Red 1 2]def[/color]Test";
                var result = Tokenize(sample, true).ToList();
                //result = Tokenize(@"\nABC\nDEF\n", true).ToList();
            }
            catch (Exception) { }
#endif
        }

        private static bool AreAllOpenTagsEscaped(string Text)
        {
            if (string.IsNullOrEmpty(Text))
            {
                return true;
            }
            else if (Text[0] == OpenTagChar)
            {
                return false;
            }
            else
            {
                char PreviousCharacter = Text[0];
                foreach (char c in Text.Skip(1))
                {
                    if (c == OpenTagChar && PreviousCharacter != EscapeOpenTagChar)
                        return false;
                    PreviousCharacter = c;
                }

                return true;
            }
        }

        /// <summary>Removes all formatted text markdown from the given <paramref name="Text"/> by prefixing all instances of <see cref="OpenTagChar"/> with <see cref="EscapeOpenTagChar"/>.<para/>
        /// (This method does not modify <see cref="FTTokenType.LineBreak"/>s)</summary>
        public static string EscapeMarkdown(string Text)
        {
            if (string.IsNullOrEmpty(Text))
                return Text;

            StringBuilder Result = new();

            if (Text[0] == OpenTagChar)
                Result.Append(EscapeOpenTagChar);
            Result.Append(Text[0]);

            char PreviousCharacter = Text[0];
            foreach (char c in Text.Skip(1))
            {
                if (c == OpenTagChar && PreviousCharacter != EscapeOpenTagChar)
                    Result.Append(EscapeOpenTagChar);
                Result.Append(c);
                PreviousCharacter = c;
            }

            return Result.ToString();
        }

        /// <param name="ShouldTokenizeLineBreak">If true, '\n' will be tokenized as <see cref="FTTokenType.LineBreak"/>.<br/>
        /// If false, '\n' will not be tokenized, and will instead remain as a substring inside a <see cref="FTTokenType.StringValue"/> token.<para/>
        /// Warning - A linebreak is treated as the character literal '\n', as opposed to the string @"\n". Be sure not to accidentally use a literal escape character '\\', such as if prefixing a hard-coded string with @.</param>
        public bool TryTokenize(string Text, bool ShouldTokenizeLineBreak, out List<FTTokenMatch> Result)
        {
            Result = Tokenize(Text, ShouldTokenizeLineBreak).ToList();
            return !Result.Any(x => x.TokenType == FTTokenType.InvalidToken);
        }

        /// <param name="ShouldTokenizeLineBreaks">If true, '\n' will be tokenized as <see cref="FTTokenType.LineBreak"/>.<br/>
        /// If false, '\n' will not be tokenized, and will instead remain as a substring inside a <see cref="FTTokenType.StringValue"/> token.<para/>
        /// Warning - A linebreak is treated as the character literal '\n', as opposed to the string @"\n". Be sure not to accidentally use a literal escape character '\\', such as if prefixing a hard-coded string with @.</param>
        public IEnumerable<FTTokenMatch> Tokenize(string Text, bool ShouldTokenizeLineBreaks)
        {
            if (string.IsNullOrEmpty(Text))
                yield break;

            if (AreAllOpenTagsEscaped(Text))
            {
                string LiteralText = Text.Replace($"{EscapeOpenTagChar}{OpenTagChar}", OpenTagChar.ToString());
                foreach (FTTokenMatch Item in TokenizeLineBreaks(LiteralText, ShouldTokenizeLineBreaks))
                    yield return Item;
                yield break;
            }

            string RemainingText = Text;

            StringBuilder LiteralValue = new();
            FTTokenType? Previous = null;
            while (!string.IsNullOrEmpty(RemainingText))
            {
                FTTokenMatch? Match = Definitions.Select(x => x.Match(Previous, RemainingText)).FirstOrDefault(x => x != null);
                if (Match != null)
                {
                    if (LiteralValue.Length > 0)
                    {
                        foreach (FTTokenMatch Item in TokenizeLineBreaks(LiteralValue.ToString(), ShouldTokenizeLineBreaks))
                            yield return Item;
                        LiteralValue.Clear();
                    }

                    yield return Match.Value;
                    RemainingText = Match.Value.RemainingText;
                    Previous = Match.Value.TokenType;
                }
                else
                {
                    LiteralValue.Append(RemainingText[0]);
                    RemainingText = RemainingText.Substring(1);
                    if (Previous.HasValue && Previous.Value == FTTokenType.EscapeMarkdown)
                        Previous = null;
                }
            }

            if (LiteralValue.Length > 0)
            {
                foreach (FTTokenMatch Item in TokenizeLineBreaks(LiteralValue.ToString(), ShouldTokenizeLineBreaks))
                    yield return Item;
            }
        }

        /// <param name="Enabled">True if '\n' characters should be treated be tokenized as <see cref="FTTokenType.LineBreak"/>.<br/>
        /// False if the entire <paramref name="Text"/> should be returned as a single <see cref="FTTokenType.StringValue"/> token.</param>
        private static IEnumerable<FTTokenMatch> TokenizeLineBreaks(string Text, bool Enabled)
        {
            if (string.IsNullOrEmpty(Text))
                yield break;

            if (Enabled)
            {
                int Index;
                while ((Index = Text.IndexOf('\n')) >= 0)
                {
                    if (Index != 0)
                        yield return new(FTTokenType.StringValue, Text.Substring(0, Index), "");
                    yield return new(FTTokenType.LineBreak, '\n'.ToString(), "");
                    Text = Text.Substring(Index + 1);
                    if (Text == string.Empty)
                        break;
                }

                if (Text != string.Empty)
                {
                    yield return new(FTTokenType.StringValue, Text, "");
                }
            }
            else
            {
                yield return new(FTTokenType.StringValue, Text, "");
            }
        }
    }
}
