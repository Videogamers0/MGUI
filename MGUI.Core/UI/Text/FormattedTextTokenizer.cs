using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.XAML;
using MonoGame.Extended;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Thickness = MonoGame.Extended.Thickness;

namespace MGUI.Core.UI.Text
{
    /// <summary>A token parsed from a piece of formatted text that contains markdown.<br/>
    /// These tokens are used as a first-pass, prior to more detailed parsing performed via <see cref="FTParser"/>.</summary>
    public enum FTTokenType
    {
        OpenTag,
        CloseTag,

        BoldOpenTagType,
        BoldCloseTagType,

        ItalicOpenTagType,
        ItalicCloseTagType,

        OpacityOpenTagType,
        OpacityCloseTagType,
        OpacityValue,

        ForegroundOpenTagType,
        ForegroundCloseTagType,
        ForegroundValue,

        UnderlineOpenTagType,
        UnderlineCloseTagType,
        UnderlineValue,

        BackgroundOpenTagType,
        BackgroundCloseTagType,
        BackgroundValue,

        ShadowOpenTagType,
        ShadowCloseTagType,
        ShadowValue,

        ImageOpenTagType,
        ImageValue,

        ToolTipOpenTagType,
        ToolTipCloseTagType,
        ToolTipValue,

        ActionOpenTagType,
        ActionCloseTagType,
        ActionValue,

        InvalidToken,

        StringLiteral,

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
            if (PrecededBy.Any() && !PrecededBy.Any(x => (x == null && !Previous.HasValue) || (x != null && Previous.HasValue && x.Value == Previous.Value)))
                return null;
            if (NotPrecededBy.Any(x => (x == null && !Previous.HasValue) || (x != null && Previous.HasValue && x.Value == Previous.Value)))
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
        /// <summary>If this character is immediately followed by <see cref="OpenTagChar"/>, then this character instructs the tokenizer to treat the following 
        /// <see cref="OpenTagChar"/> literally instead of as the start of a markdown formatting code.</summary>
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
                FTTokenType.ItalicOpenTagType
            };

            List<FTTokenType> OpenTagTypesWithValues = new() {
                FTTokenType.OpacityOpenTagType,
                FTTokenType.ForegroundOpenTagType,
                FTTokenType.UnderlineOpenTagType,
                FTTokenType.BackgroundOpenTagType,
                FTTokenType.ShadowOpenTagType,
                FTTokenType.ImageOpenTagType,
                FTTokenType.ToolTipOpenTagType,
                FTTokenType.ActionOpenTagType
            };

            List<FTTokenType> CloseTagTypes = new() {
                FTTokenType.BoldCloseTagType,
                FTTokenType.ItalicCloseTagType,
                FTTokenType.UnderlineCloseTagType,
                FTTokenType.OpacityCloseTagType,
                FTTokenType.ForegroundCloseTagType,
                FTTokenType.BackgroundCloseTagType,
                FTTokenType.ShadowCloseTagType,
                FTTokenType.ToolTipCloseTagType,
                FTTokenType.ActionCloseTagType
            };

            List<FTTokenType> TagValues = new() {
                FTTokenType.OpacityValue,
                FTTokenType.ForegroundValue,
                FTTokenType.UnderlineValue,
                FTTokenType.BackgroundValue,
                FTTokenType.ShadowValue,
                FTTokenType.ImageValue,
                FTTokenType.ToolTipValue,
                FTTokenType.ActionValue
            };

            string EscapedOpenTag = Regex.Escape(OpenTagChar.ToString());
            string EscapedCloseTag = Regex.Escape(CloseTagChar.ToString());

            //  Open Tag
            Definitions.Add(new(FTTokenType.OpenTag, $@"^{EscapedOpenTag}",
                AsEnumerable<FTTokenType?>(null, FTTokenType.CloseTag, FTTokenType.StringLiteral, FTTokenType.LineBreak), //PrecededBy
                AsEnumerable<FTTokenType?>(FTTokenType.InvalidToken)) //NotPrecededBy
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

            string OpacityTypePattern = $@"(?i)(opacity|o(?=(\=|{EscapedCloseTag})))(?-i)";
            Definitions.Add(new(FTTokenType.OpacityOpenTagType, $@"^{OpacityTypePattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.OpenTag),
                Enumerable.Empty<FTTokenType?>())
            );
            Definitions.Add(new(FTTokenType.OpacityCloseTagType, $@"^(\\|\/){OpacityTypePattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.OpenTag),
                Enumerable.Empty<FTTokenType?>())
            );

            string UnderlineTypePattern = $@"(?i)(underline|u(?=(\=|{EscapedCloseTag})))(?-i)";
            Definitions.Add(new(FTTokenType.UnderlineOpenTagType, $@"^{UnderlineTypePattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.OpenTag),
                Enumerable.Empty<FTTokenType?>())
            );
            Definitions.Add(new(FTTokenType.UnderlineCloseTagType, $@"^(\\|\/){UnderlineTypePattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.OpenTag),
                Enumerable.Empty<FTTokenType?>())
            );

            string ForegroundTypePattern = $@"(?i)(fg|foreground|color|c(?=(\=|{EscapedCloseTag})))(?-i)";
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

            string ShadowTypePattern = $@"(?i)(shadow|s(?=(\=|{EscapedCloseTag})))(?-i)";
            Definitions.Add(new(FTTokenType.ShadowOpenTagType, $@"^{ShadowTypePattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.OpenTag),
                Enumerable.Empty<FTTokenType?>())
            );
            Definitions.Add(new(FTTokenType.ShadowCloseTagType, $@"^(\\|\/){ShadowTypePattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.OpenTag),
                Enumerable.Empty<FTTokenType?>())
            );

            string ImageTypePattern = $@"(?i)(image|img)(?=\=)(?-i)";
            Definitions.Add(new(FTTokenType.ImageOpenTagType, $@"^{ImageTypePattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.OpenTag),
                Enumerable.Empty<FTTokenType?>())
            );

            string ToolTipTypePattern = $@"(?i)(tooltip|tt)(?-i)";
            Definitions.Add(new(FTTokenType.ToolTipOpenTagType, $@"^{ToolTipTypePattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.OpenTag),
                Enumerable.Empty<FTTokenType?>())
            );
            Definitions.Add(new(FTTokenType.ToolTipCloseTagType, $@"^(\\|\/){ToolTipTypePattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.OpenTag),
                Enumerable.Empty<FTTokenType?>())
            );

            string ActionTypePattern = $@"(?i)(action|command)(?-i)";
            Definitions.Add(new(FTTokenType.ActionOpenTagType, $@"^{ActionTypePattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.OpenTag),
                Enumerable.Empty<FTTokenType?>())
            );
            Definitions.Add(new(FTTokenType.ActionCloseTagType, $@"^(\\|\/){ActionTypePattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.OpenTag),
                Enumerable.Empty<FTTokenType?>())
            );

            //  Tag Values
            string OpacityPattern = $@"0?\.\d\d?";
            Definitions.Add(new(FTTokenType.OpacityValue, $@"^={OpacityPattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.OpacityOpenTagType),
                Enumerable.Empty<FTTokenType?>())
            );

            Definitions.Add(new(FTTokenType.UnderlineValue, $@"^(={UnderlineValuePattern})?",
                AsEnumerable<FTTokenType?>(FTTokenType.UnderlineOpenTagType),
                Enumerable.Empty<FTTokenType?>())
            );

            string ColorHexPattern = @"#[a-fA-F0-9]{6,8}";
            string ColorNamePattern = @"[a-zA-Z]+";
            string ColorPattern = $@"({ColorHexPattern}|{ColorNamePattern})";
            Definitions.Add(new(FTTokenType.ForegroundValue, $@"^={ColorPattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.ForegroundOpenTagType),
                Enumerable.Empty<FTTokenType?>())
            );

            Definitions.Add(new(FTTokenType.BackgroundValue, $@"^={BackgroundValuePattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.BackgroundOpenTagType),
                Enumerable.Empty<FTTokenType?>())
            );

            string ShadowValuePattern = $@"{ColorPattern}( -?\d{{1,2}}( |,)-?\d{{1,2}})?";
            Definitions.Add(new(FTTokenType.ShadowValue, $@"^={ShadowValuePattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.ShadowOpenTagType),
                Enumerable.Empty<FTTokenType?>())
            );

            Definitions.Add(new(FTTokenType.ImageValue, $@"^={ImageValuePattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.ImageOpenTagType),
                Enumerable.Empty<FTTokenType?>())
            );

            Definitions.Add(new(FTTokenType.ToolTipValue, $@"^={ToolTipValuePattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.ToolTipOpenTagType),
                Enumerable.Empty<FTTokenType?>())
            );

            Definitions.Add(new(FTTokenType.ActionValue, $@"^={ActionValuePattern}",
                AsEnumerable<FTTokenType?>(FTTokenType.ActionOpenTagType),
                Enumerable.Empty<FTTokenType?>())
            );

            //  Invalid Token
            Definitions.Add(new(FTTokenType.InvalidToken, @"^.*",
                Enumerable.Empty<FTTokenType?>(),
                AsEnumerable<FTTokenType?>(null, FTTokenType.CloseTag, FTTokenType.StringLiteral, FTTokenType.LineBreak))
            );

#if NEVER //DEBUG
            try
            {
                string sample = $"H{EscapeOpenTagChar}{EscapeOpenTagChar}[b]e\nllo[img=Test 32x64][bold]W[bg=Red]orld[color=Green][shadow=Red 1 2]def[/color]Test";
                var result = Tokenize(sample, true).ToList();
            }
            catch (Exception) { }
#endif
        }

        //  Regex pattern that checks if the next character is either a close tag ']' or end of string, but does not consume that character (lookahead)
        private static readonly string CloseTagOrEndOfStringLookahead = $@"(?=({Regex.Escape(CloseTagChar.ToString())}|$))";
        //  Regex pattern that checks if the next character is a close tag ']', but does not consume that character (lookahead)
        private static readonly string CloseTagLookahead = $@"(?=({Regex.Escape(CloseTagChar.ToString())}))";

        //  The underline Height (positive integer) followed by a vertical offset (integer, can be negative), followed by the underline's fill brush (usually a single color) 
        //  All parameters are optional
        //  EX: "[Underline=2 -1 Blue]" uses a solid blue brush to draw a 2px tall rectangle, offsetted upwards by 1px above the bottom of the text it's anchored to
        //      "[Underline=4]" uses the TextBlock's current Foreground Color as the brush, to draw a 4px tall rectangle at the default location just below the text it's anchored to
        private static readonly string UnderlineValuePattern = $@"((?<Height>\d+)(( (?<Offset>-?\d+))(( (?<Brush>.+?)))?)?)?{CloseTagOrEndOfStringLookahead}";
        public static readonly Regex UnderlineValueParser = new(UnderlineValuePattern);
        public static (int? Height, int? VerticalOffset, IFillBrush Brush) ParseUnderlineValue(string Value)
        {
            Match Match = UnderlineValueParser.Match(Value);

            int? Height = Match.Groups["Height"].Success ? int.Parse(Match.Groups["Height"].Value) : null;
            int? VerticalOffset = Match.Groups["Offset"].Success ? int.Parse(Match.Groups["Offset"].Value) : null;
            IFillBrush Brush = null;
            if (Match.Groups["Brush"].Success)
            {
                string BrushString = Match.Groups["Brush"].Value;
                Brush = FillBrushStringConverter.ParseFillBrush(BrushString).ToFillBrush(null);
            }

            return (Height, VerticalOffset, Brush);
        }

        //  The background fill brush (usually a single color) followed by an optional Padding Thickness
        //  EX: "[Background=Red 2,-4,2,8]" uses a solid red brush, with Padding="2,-4,2,8"
        private static readonly string BackgroundValuePattern = $@"(?<Brush>.+?)( (?<Padding>-?\d+(,-?\d+){{0,3}}))?{CloseTagOrEndOfStringLookahead}";
        public static readonly Regex BackgroundValueParser = new(BackgroundValuePattern);
        public static (IFillBrush Brush, Thickness Padding) ParseBackgroundValue(string Value)
        {
            Match Match = BackgroundValueParser.Match(Value);

            string BrushString = Match.Groups["Brush"].Value;
            IFillBrush Brush = FillBrushStringConverter.ParseFillBrush(BrushString).ToFillBrush(null);
            Thickness Padding = Match.Groups["Padding"].Success ? Thickness.Parse(Match.Groups["Padding"].Value) : default;

            return (Brush, Padding);
        }

        //  The name to reference the ToolTip by
        private static readonly string ToolTipValuePattern = $@"(?<ToolTipName>.+?){CloseTagOrEndOfStringLookahead}";
        public static readonly Regex ToolTipValueParser = new(ToolTipValuePattern);

        //  The name to reference the Action delegate by
        private static readonly string ActionValuePattern = $@"(?<ActionName>.+?)(?=({Regex.Escape(CloseTagChar.ToString())}|$))";
        public static readonly Regex ActionValueParser = new(ActionValuePattern);

        //  The name to reference the texture by, followed by an optional target size X Y
        //  EX: "[img=Gold.png 32x16]" would reference a texture by name="Gold.png", and draw it with Width=32, Height=16
        private static readonly string ImageValuePattern = $@"(?<SourceName>.+?)( (?<Width>\d{{1,4}})( |,|x|X)(?<Height>\d{{1,4}}))?{CloseTagOrEndOfStringLookahead}";
        private static readonly Regex ImageValueParser = new(ImageValuePattern);

        internal static (string SourceName, int? TargetWidth, int? TargetHeight) ParseImageValue(string Value)
        {
            Match Match = ImageValueParser.Match(Value);

            string SourceName = Match.Groups["SourceName"].Value;
            int? Width = Match.Groups["Width"].Success ? int.Parse(Match.Groups["Width"].Value) : null;
            int? Height = Match.Groups["Height"].Success ? int.Parse(Match.Groups["Height"].Value) : null;

            return (SourceName, Width, Height);
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

            //TODO: This method should also handle cases where open brackets immediately follow double-escaped characters
            //EX:
            //  Unescaped:                  @"Hello\\[]World"
            //  Escaped:                    @"Hello\\\[]World"
            //  Literal rendered string:    @"Hello\[]World"
            //The @"\\" is treated as a backslash literal, rather than the 2nd backslash being treated as an escape character for the open bracket.
            //The current logic thinks the open bracket is already escaped because it's immediately after the escape character.
            //So it's not validating that the escape character isn't also escaped.
            //I guess we need to check if each open bracket has an odd number of escape characters immediately before it.

            return Result.ToString();
        }

        /// <param name="ShouldTokenizeLineBreaks">If true, linebreaks '\n', '\r', "\r\n" will be tokenized as <see cref="FTTokenType.LineBreak"/>.<br/>
        /// If false, they will not be tokenized, and will instead remain as a substring inside a <see cref="FTTokenType.StringLiteral"/> token.<para/>
        /// Warning - A linebreak is treated as the character literals: '\n', '\r', "\r\n", as opposed to the escaped strings such as @"\n". Be sure not to accidentally use a literal escape character '\\', such as if prefixing a hard-coded string with @.</param>
        public bool TryTokenize(string Text, bool ShouldTokenizeLineBreaks, out List<FTTokenMatch> Result)
        {
            Result = Tokenize(Text, ShouldTokenizeLineBreaks).ToList();
            return !Result.Any(x => x.TokenType == FTTokenType.InvalidToken);
        }

        private static readonly string EscapedEscapeOpenTagChar = Regex.Escape(EscapeOpenTagChar.ToString());
        private static readonly string EscapedOpenTagChar = Regex.Escape(OpenTagChar.ToString());

        private static readonly string SubPattern1 = $@"[^{EscapedEscapeOpenTagChar}{EscapedOpenTagChar}]";
        private static readonly string SubPattern2 = $@"({EscapedEscapeOpenTagChar}({EscapedEscapeOpenTagChar}*)(?!({EscapedEscapeOpenTagChar}|{EscapedOpenTagChar})))";
        private static readonly string SubPattern3 = $@"({EscapedEscapeOpenTagChar}{EscapedOpenTagChar})";
        private static readonly string Pattern = $"^({SubPattern1}|{SubPattern2}|{SubPattern3})*";

        /// <param name="ShouldTokenizeLineBreaks">If true, linebreaks '\n', '\r', "\r\n" will be tokenized as <see cref="FTTokenType.LineBreak"/>.<br/>
        /// If false, they will not be tokenized, and will instead remain as a substring inside a <see cref="FTTokenType.StringLiteral"/> token.<para/>
        /// Warning - A linebreak is treated as the character literals: '\n', '\r', "\r\n", as opposed to the escaped strings such as @"\n". Be sure not to accidentally use a literal escape character '\\', such as if prefixing a hard-coded string with @.</param>
        public IEnumerable<FTTokenMatch> Tokenize(string Text, bool ShouldTokenizeLineBreaks)
        {
            if (string.IsNullOrEmpty(Text))
                yield break;

            string RemainingText = Text;
            StringBuilder CurrentStringLiteral = new();

            bool IsTokenizingFormattingCode = false;

            FTTokenType? PreviousToken = null;
            while (!string.IsNullOrEmpty(RemainingText))
            {
                if (!IsTokenizingFormattingCode)
                {
                    Match Match = Regex.Match(RemainingText, Pattern);
                    if (Match.Success && !string.IsNullOrEmpty(Match.Value))
                    {
                        string StringLiteral = Match.Value.Replace($"{EscapeOpenTagChar}{OpenTagChar}", $"{OpenTagChar}");
                        CurrentStringLiteral.Append(StringLiteral);
                        RemainingText = RemainingText.Substring(Match.Length);
                    }
                    else if (RemainingText.StartsWith(string.Join("", Enumerable.Repeat(EscapeOpenTagChar, 2))))
                    {
                        CurrentStringLiteral.Append(EscapeOpenTagChar);
                        RemainingText = RemainingText.Substring(2);
                    }
                    else
                    {
                        if (CurrentStringLiteral.Length > 0)
                        {
                            foreach (FTTokenMatch Item in TokenizeLineBreaks(CurrentStringLiteral.ToString(), ShouldTokenizeLineBreaks))
                                yield return Item;
                            PreviousToken = FTTokenType.StringLiteral;
                        }

                        CurrentStringLiteral.Clear();
                        IsTokenizingFormattingCode = true;
                    }
                }
                else
                {
                    FTTokenMatch Match = Definitions.Select(x => x.Match(PreviousToken, RemainingText)).First(x => x != null).Value;
                    yield return Match;
                    RemainingText = Match.RemainingText;
                    PreviousToken = Match.TokenType;

                    if (Match.TokenType == FTTokenType.InvalidToken)
                    {
                        yield break;
                    }
                    else if (Match.TokenType == FTTokenType.CloseTag)
                    {
                        IsTokenizingFormattingCode = false;
                    }
                }
            }

            if (CurrentStringLiteral.Length > 0)
            {
                foreach (FTTokenMatch Item in TokenizeLineBreaks(CurrentStringLiteral.ToString(), ShouldTokenizeLineBreaks))
                    yield return Item;
            }
        }

        private static readonly Regex LineBreakRegex = new("(\r\n|\r|\n)"); // "\r\n" for Windows, '\r' for Mac, '\n' for Linux

        /// <param name="Enabled">True if '\n', '\r', and "\r\n" characters should be tokenized as <see cref="FTTokenType.LineBreak"/>.<br/>
        /// False if the entire <paramref name="Text"/> should be returned as a single <see cref="FTTokenType.StringLiteral"/> token.</param>
        public static IEnumerable<FTTokenMatch> TokenizeLineBreaks(string Text, bool Enabled = true)
        {
            if (string.IsNullOrEmpty(Text))
                yield break;

            if (Enabled)
            {
                Match Match;
                while ((Match = LineBreakRegex.Match(Text)).Success)
                {
                    int Index = Match.Index;
                    if (Index != 0)
                        yield return new(FTTokenType.StringLiteral, Text.Substring(0, Index), "");
                    yield return new(FTTokenType.LineBreak, Match.Value, "");
                    Text = Text.Substring(Index + Match.Length);
                    if (Text == string.Empty)
                        break;
                }

                if (Text != string.Empty)
                {
                    yield return new(FTTokenType.StringLiteral, Text, "");
                }
            }
            else
            {
                yield return new(FTTokenType.StringLiteral, Text, "");
            }
        }
    }
}
