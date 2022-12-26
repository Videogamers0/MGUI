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

namespace MGUI.Core.UI.Text
{
    public record struct MGTextRunConfig(bool IsBold, bool IsItalic = false, bool IsUnderlined = false, float Opacity = 1.0f,
        Color? Foreground = null, IFillBrush Background = null, bool IsShadowed = false, Color? ShadowColor = null, Vector2? ShadowOffset = null)
    {
        public MGTextRunConfig ApplyAction(FTAction Action,
            IList<float> PreviousOpacities, IList<Color?> PreviousForegrounds, IList<IFillBrush> PreviousBackgrounds, 
            IList<bool> PreviousShadowStates, IList<Color?> PreviousShadowColors, IList<Vector2?> PreviousShadowOffsets)
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
                case FTActionType.EnableUnderline:
                    return this with { IsUnderlined = true };
                case FTActionType.DisableUnderline:
                    return this with { IsUnderlined = false };

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
                        Color Value = ColorTranslator.FromHtml(Action.Parameter.Substring(1)).AsXNAColor();
                        return this with { Background = new MGSolidFillBrush(Value) };
                    }
                case FTActionType.RevertBackground:
                    return this with { Background = GeneralUtils.RemoveLast(PreviousBackgrounds, null) };

                //  Shadowing
                case FTActionType.SetShadowing:
                    {
                        PreviousShadowStates.Add(IsShadowed);
                        PreviousShadowColors.Add(ShadowColor);
                        PreviousShadowOffsets.Add(ShadowOffset);
                        string[] Words = Action.Parameter.Substring(1).Split(' ');
                        Color ParsedShadowColor = ColorTranslator.FromHtml(Words[0]).AsXNAColor();
                        Vector2 ParsedShadowOffset = Words.Length == 3 ? new(int.Parse(Words[1]), int.Parse(Words[2])) : new(1, 1);
                        return this with { IsShadowed = true, ShadowColor = ParsedShadowColor, ShadowOffset = ParsedShadowOffset };
                    }
                case FTActionType.RevertShadowing:
                    return this with
                    { 
                        IsShadowed = GeneralUtils.RemoveLast(PreviousShadowStates, false), 
                        ShadowColor = GeneralUtils.RemoveLast(PreviousShadowColors, null), 
                        ShadowOffset = GeneralUtils.RemoveLast(PreviousShadowOffsets, null)
                    };

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

            if (!Tokenizer.TryTokenize(FormattedText, true, out List<FTTokenMatch> Tokens))
            {
                FTTokenMatch InvalidToken = Tokens.First(x => x.TokenType == FTTokenType.InvalidToken);
                throw new InvalidDataException($"Formatted text does not match valid format: {FormattedText}\n\nError at token #{Tokens.IndexOf(InvalidToken) + 1} with value: {InvalidToken.Value}");
            }

            foreach (MGTextRun Run in ParseRuns(Tokens, DefaultSettings))
                yield return Run;
        }

        public static IEnumerable<MGTextRun> ParseRuns(List<FTTokenMatch> Tokens, MGTextRunConfig DefaultSettings)
        {
            MGTextRunConfig CurrentState = DefaultSettings with { };

            List<float> PreviousOpacities = new();
            List<Color?> PreviousForegrounds = new();
            List<IFillBrush> PreviousBackgrounds = new();
            List<bool> PreviousShadowStates = new();
            List<Color?> PreviousShadowColors = new();
            List<Vector2?> PreviousShadowOffsets = new();

            string CurrentToolTipId = null;
            List<string> PreviousToolTipIds = new();

            string CurrentActionId = null;
            List<string> PreviousActionIds = new();

            List<FTAction> Actions = Parser.ParseTokens(Tokens).ToList();
            for (int i = 0; i < Actions.Count; i++)
            {
                FTAction CurrentAction = Actions[i];
                CurrentState = CurrentState.ApplyAction(CurrentAction, PreviousOpacities, PreviousForegrounds, PreviousBackgrounds, PreviousShadowStates, PreviousShadowColors, PreviousShadowOffsets);

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
                    var (RegionName, TargetWidth, TargetHeight) = FTTokenizer.ParseImageValue(CurrentAction.Parameter.Substring(1));
                    yield return new MGTextRunImage(RegionName, TargetWidth, TargetHeight, CurrentToolTipId, CurrentActionId);
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

        public override string ToString() => $"{nameof(MGTextRunLineBreak)}: {Text?.Truncate(50) ?? ""} ({Settings})";
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
        /// <summary>The name of the <see cref="NamedTextureRegion"/> to render.<para/>
        /// See also:<br/><see cref="MGDesktop.NamedRegions"/><br/><see cref="MGDesktop.AddNamedRegion(NamedTextureRegion)"/><br/><see cref="MGDesktop.TryDrawNamedRegion(DrawTransaction, string, Point, int?, int?, float)"/></summary>
        public readonly string RegionName;
        public readonly int TargetWidth;
        public readonly int TargetHeight;

        public MGTextRunImage(string RegionName, int TargetWidth, int TargetHeight, string ToolTipId, string ActionId)
            : base(TextRunType.Image, ToolTipId, ActionId)
        {
            this.RegionName = RegionName;
            this.TargetWidth = TargetWidth;
            this.TargetHeight = TargetHeight;
        }

        public override string ToString() => $"{nameof(MGTextRunImage)}: {RegionName} - {TargetWidth},{TargetHeight}";
    }
}
