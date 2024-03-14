using Microsoft.Xna.Framework;
using MGUI.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ColorTranslator = System.Drawing.ColorTranslator;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using Microsoft.Xna.Framework.Graphics;
using MGUI.Shared.Rendering;
using MonoGame.Extended;
using MGUI.Core.UI.XAML;
using Thickness = MonoGame.Extended.Thickness;

namespace MGUI.Core.UI.Text
{
    public record struct MGTextRunUnderlineConfig(bool IsEnabled = false, int Height = 1, int VerticalOffset = 0, IFillBrush Brush = null);

    public record struct MGTextRunBackgroundConfig(IFillBrush Brush = null, Thickness Padding = default)
    {
        public static readonly MGTextRunBackgroundConfig Empty = new(null, default);
        public bool HasBackground => Brush != null;
    }

    public record struct MGTextRunShadowConfig(Color? ShadowColor = null, Vector2? ShadowOffset = null)
    {
        public static readonly MGTextRunShadowConfig Empty = new(null, null);
        public bool IsShadowed => ShadowColor.HasValue;
    }

    public record struct MGTextRunConfig(bool IsBold, bool IsItalic = false, float Opacity = 1.0f,
        Color? Foreground = null, MGTextRunUnderlineConfig Underline = new(), MGTextRunBackgroundConfig Background = new(), MGTextRunShadowConfig Shadow = new())
    {
        public bool HasBackground => Background.HasBackground;
        public bool IsShadowed => Shadow.IsShadowed;

        public MGTextRunConfig ApplyAction(FTAction Action, IList<float> PreviousOpacities, IList<Color?> PreviousForegrounds,
            IList<MGTextRunUnderlineConfig> PreviousUnderlines, IList<MGTextRunBackgroundConfig> PreviousBackgrounds, IList<MGTextRunShadowConfig> PreviousShadows)
        {
            switch (Action.ActionType)
            {
                //  Bold
                case FTActionType.EnableBold:
                    return this with { IsBold = true };
                case FTActionType.DisableBold:
                    return this with { IsBold = false };

                //  Italic
                case FTActionType.EnableItalic:
                    return this with { IsItalic = true };
                case FTActionType.DisableItalic:
                    return this with { IsItalic = false };

                //  Underline
                case FTActionType.SetUnderline:
                    {
                        PreviousUnderlines.Add(Underline);
                        MGTextRunUnderlineConfig UnderlineConfig = new(true);
                        if (!string.IsNullOrEmpty(Action.Parameter))
                        {
                            (int? Height, int? VerticalOffset, IFillBrush Brush) = FTTokenizer.ParseUnderlineValue(Action.Parameter.Substring(1));
                            UnderlineConfig = new MGTextRunUnderlineConfig(true, Height ?? 1, VerticalOffset ?? 0, Brush);
                        }
                        return this with { Underline = UnderlineConfig };
                    }
                case FTActionType.RevertUnderline:
                    return this with { Underline = GeneralUtils.RemoveLast(PreviousUnderlines, default) };

                //  Opacity
                case FTActionType.SetOpacity:
                    {
                        PreviousOpacities.Add(Opacity);
                        float Value = float.Parse(Action.Parameter.Substring(1));
                        return this with { Opacity = Value };
                    }
                case FTActionType.RevertOpacity:
                    return this with { Opacity = GeneralUtils.RemoveLast(PreviousOpacities, 1.0f) };

                //  Foreground
                case FTActionType.SetForeground:
                    {
                        PreviousForegrounds.Add(Foreground);
                        Color Value = ColorTranslator.FromHtml(Action.Parameter.Substring(1)).AsXNAColor();
                        return this with { Foreground = Value };
                    }
                case FTActionType.RevertForeground:
                    return this with { Foreground = GeneralUtils.RemoveLast(PreviousForegrounds, null) };

                //  Background
                case FTActionType.SetBackground:
                    {
                        PreviousBackgrounds.Add(Background);
                        (IFillBrush Brush, Thickness Padding) = FTTokenizer.ParseBackgroundValue(Action.Parameter.Substring(1));
                        return this with { Background = new MGTextRunBackgroundConfig(Brush, Padding) };
                    }
                case FTActionType.RevertBackground:
                    return this with { Background = GeneralUtils.RemoveLast(PreviousBackgrounds, default) };

                //  Shadowing
                case FTActionType.SetShadowing:
                    {
                        PreviousShadows.Add(Shadow);
                        string[] Words = Action.Parameter.Substring(1).Split(' ');
                        Color ParsedShadowColor = ColorTranslator.FromHtml(Words[0]).AsXNAColor();
                        Vector2 ParsedShadowOffset = Words.Length == 3 ? new(int.Parse(Words[1]), int.Parse(Words[2])) : new(1, 1);
                        return this with { Shadow = new MGTextRunShadowConfig(ParsedShadowColor, ParsedShadowOffset) };
                    }
                case FTActionType.RevertShadowing:
                    return this with { Shadow = GeneralUtils.RemoveLast(PreviousShadows, default) };

                //  Image
                case FTActionType.Image:
                    return this;

                //  ToolTip
                case FTActionType.SetToolTip:
                    return this;
                case FTActionType.RevertToolTip:
                    return this;

                //  Action
                case FTActionType.SetAction:
                    return this;
                case FTActionType.RevertAction:
                    return this;

                //  String value
                case FTActionType.StringLiteral:
                    return this;

                //  LineBreak
                case FTActionType.LineBreak:
                    return this;
                default: throw new NotImplementedException($"Unrecognized {nameof(FTActionType)}: {Action.ActionType}");
            }
        }
    }

    public enum TextRunType
    {
        Text,
        LineBreak,
        Image
    }

    /// <summary>Represents data that can be rendered in-line with other <see cref="MGTextRun"/>s.<para/>
    /// See also:<br/><see cref="MGTextRunText"/><br/><see cref="MGTextRunLineBreak"/><br/><see cref="MGTextRunImage"/></summary>
    public abstract class MGTextRun
    {
        /// <summary>The number of spaces to convert tab characters into before parsing <see cref="MGTextRun"/>s.<para/>
        /// Default value: 4</summary>
        public const int TabSpacesCount = 4;
        public static readonly string TabReplacement = string.Join("", Enumerable.Repeat(" ", TabSpacesCount));

        public static readonly FTParser Parser = new();
        public static readonly FTTokenizer Tokenizer = new();

        public readonly TextRunType RunType;

        public readonly string ToolTipId;
        public bool HasToolTip => !string.IsNullOrEmpty(ToolTipId);

        public readonly string ActionId;
        public bool HasAction => !string.IsNullOrEmpty(ActionId);

        protected MGTextRun(TextRunType RunType, string ToolTipId, string ActionId)
        {
            this.RunType = RunType;
            this.ToolTipId = ToolTipId;
            this.ActionId = ActionId;
        }

        public static IEnumerable<MGTextRun> ParseRuns(string FormattedText, MGTextRunConfig DefaultSettings)
        {
            if (string.IsNullOrEmpty(FormattedText))
                yield break;

            FormattedText = FormattedText.Replace("\t", TabReplacement);

            if (!Tokenizer.TryTokenize(FormattedText, true, out List<FTTokenMatch> Tokens))
            {
                //FTTokenMatch InvalidToken = Tokens.First(x => x.TokenType == FTTokenType.InvalidToken);
                //throw new InvalidDataException($"Formatted text does not match valid format: {FormattedText}\n\nError at token #{Tokens.IndexOf(InvalidToken) + 1} with value: {InvalidToken.Value}");

                //  Parse as much as we can until hitting an InvalidToken.
                //  Once we do, convert all remaining text into a StringLiteral token
                List<FTTokenMatch> Temp = Tokens;
                Tokens = new List<FTTokenMatch>();
                for (int i = 0; i < Temp.Count; i++)
                {
                    FTTokenMatch CurrentToken = Temp[i];
                    if (i + 1 < Temp.Count)
                    {
                        FTTokenMatch NextToken = Temp[i + 1];
                        if (NextToken.TokenType == FTTokenType.InvalidToken)
                        {
                            string Value = CurrentToken.Value + CurrentToken.RemainingText;
                            Tokens.Add(new FTTokenMatch(FTTokenType.StringLiteral, Value, ""));
                            break;
                        }
                    }

                    Tokens.Add(CurrentToken);
                }
            }

            foreach (MGTextRun Run in ParseRuns(Tokens, DefaultSettings))
                yield return Run;
        }

        public static IEnumerable<MGTextRun> ParseRuns(List<FTTokenMatch> Tokens, MGTextRunConfig DefaultSettings)
        {
            MGTextRunConfig CurrentState = DefaultSettings with { };

            List<float> PreviousOpacities = new();
            List<Color?> PreviousForegrounds = new();
            List<MGTextRunUnderlineConfig> PreviousUnderlines = new();
            List<MGTextRunBackgroundConfig> PreviousBackgrounds = new();
            List<MGTextRunShadowConfig> PreviousShadows = new();

            string CurrentToolTipId = null;
            List<string> PreviousToolTipIds = new();

            string CurrentActionId = null;
            List<string> PreviousActionIds = new();

            List<FTAction> Actions = Parser.ParseTokens(Tokens).ToList();
            for (int i = 0; i < Actions.Count; i++)
            {
                FTAction CurrentAction = Actions[i];
                CurrentState = CurrentState.ApplyAction(CurrentAction, PreviousOpacities, PreviousForegrounds, PreviousUnderlines, PreviousBackgrounds, PreviousShadows);

                if (CurrentAction.ActionType == FTActionType.StringLiteral)
                {
                    StringBuilder LiteralValue = new();

                    bool HasNextAction = true;
                    FTAction NextAction = CurrentAction;
                    while (HasNextAction && NextAction.ActionType is FTActionType.StringLiteral or FTActionType.Ignore)
                    {
                        CurrentAction = NextAction;
                        if (CurrentAction.ActionType == FTActionType.StringLiteral)
                            LiteralValue.Append(CurrentAction.Parameter);

                        int NextIndex = i + 1;
                        HasNextAction = NextIndex < Actions.Count;
                        NextAction = HasNextAction ? Actions[NextIndex] : default;
                        i++;
                    }
                    i--;

                    yield return new MGTextRunText(LiteralValue.ToString(), CurrentState, CurrentToolTipId, CurrentActionId);
                }
                else if (CurrentAction.ActionType == FTActionType.LineBreak)
                    yield return new MGTextRunLineBreak(CurrentAction.Parameter?.Length ?? 1);
                else if (CurrentAction.ActionType == FTActionType.Image)
                {
                    var (SourceName, TargetWidth, TargetHeight) = FTTokenizer.ParseImageValue(CurrentAction.Parameter.Substring(1));
                    yield return new MGTextRunImage(SourceName, TargetWidth ?? 0, TargetHeight ?? 0, CurrentToolTipId, CurrentActionId);
                }
                else if (CurrentAction.ActionType == FTActionType.SetToolTip)
                {
                    if (!string.IsNullOrEmpty(CurrentToolTipId))
                        PreviousToolTipIds.Add(CurrentToolTipId);

                    string NewToolTipValue = CurrentAction.Parameter.Substring(1);
                    CurrentToolTipId = FTTokenizer.ToolTipValueParser.Match(NewToolTipValue).Groups["ToolTipName"].Value;
                }
                else if (CurrentAction.ActionType == FTActionType.RevertToolTip)
                {
                    CurrentToolTipId = GeneralUtils.RemoveLast(PreviousToolTipIds, null);
                }
                else if (CurrentAction.ActionType == FTActionType.SetAction)
                {
                    if (!string.IsNullOrEmpty(CurrentActionId))
                        PreviousActionIds.Add(CurrentActionId);

                    string NewActionValue = CurrentAction.Parameter.Substring(1);
                    CurrentActionId = FTTokenizer.ActionValueParser.Match(NewActionValue).Groups["ActionName"].Value;
                }
                else if (CurrentAction.ActionType == FTActionType.RevertAction)
                {
                    CurrentActionId = GeneralUtils.RemoveLast(PreviousActionIds, null);
                }
            }
        }
    }

    /// <summary>An <see cref="MGTextRun"/> that renders a piece of text with the given <see cref="MGTextRunConfig"/> settings.</summary>
    public class MGTextRunText : MGTextRun
    {
        public readonly string Text;
        public readonly MGTextRunConfig Settings;

        public MGTextRunText(string Text, MGTextRunConfig Settings, string ToolTipId, string ActionId)
            : base(TextRunType.Text, ToolTipId, ActionId)
        {
            this.Text = Text;
            this.Settings = Settings;
        }

        public string FirstWord(params char[] WordDelimiters) => new string(Text.TakeWhile(x => !WordDelimiters.Contains(x)).ToArray());

        public bool HasAny(params char[] characters)
        {
            foreach (char c in characters)
            {
                if (Text.Contains(c))
                    return true;
            }

            return false;
        }

        public bool StartsWithAny(params char[] characters)
        {
            if (string.IsNullOrEmpty(Text))
                return false;

            foreach (char c in characters)
            {
                if (Text.StartsWith(c))
                    return true;
            }

            return false;
        }

        public override string ToString() => $"{nameof(MGTextRunText)}: {Text?.Truncate(50) ?? ""} ({Settings})";
    }

    /// <summary>An <see cref="MGTextRun"/> that instructs the renderer to move to the start of a new line.</summary>
    public class MGTextRunLineBreak : MGTextRun
    {
        /// <summary>The number of characters used to specify the linebreak.<para/>
        /// Windows typically uses "\r\n" (2 characters) while Mac and Linux typically uses '\r' and '\n' (1 character)</summary>
        public readonly int LineBreakCharacterCount;

        /// <param name="LineBreakCharactersCount">The number of characters used to specify the linebreak.<para/>
        /// Windows typically uses "\r\n" (2 characters) while Mac and Linux typically uses '\r' and '\n' (1 character)</param>
        public MGTextRunLineBreak(int LineBreakCharactersCount)
            : base(TextRunType.LineBreak, null, null)
        {
            this.LineBreakCharacterCount = LineBreakCharactersCount;
        }

        public override string ToString() => $"{nameof(MGTextRunLineBreak)}: {LineBreakCharacterCount} characters";
    }

    /// <summary>An <see cref="MGTextRun"/> that renders a specific portion of a <see cref="Texture2D"/> with a given size.</summary>
    public class MGTextRunImage : MGTextRun
    {
        /// <summary>The name of the <see cref="MGTextureData"/> to render. This name should exist in <see cref="MGResources.Textures"/><para/>
        /// See also:<br/><see cref="MGDesktop.Resources"/><br/><see cref="MGElement.GetResources"/><br/><see cref="MGResources.Textures"/><br/><see cref="MGResources.AddTexture(string, MGTextureData)"/></summary>
        public readonly string SourceName;
        public readonly int TargetWidth;
        public readonly int TargetHeight;

        public MGTextRunImage(string SourceName, int TargetWidth, int TargetHeight, string ToolTipId, string ActionId)
            : base(TextRunType.Image, ToolTipId, ActionId)
        {
            this.SourceName = SourceName;
            this.TargetWidth = TargetWidth;
            this.TargetHeight = TargetHeight;
        }

        public override string ToString() => $"{nameof(MGTextRunImage)}: {SourceName} - {TargetWidth},{TargetHeight}";
    }
}
