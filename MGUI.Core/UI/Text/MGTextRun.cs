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

namespace MGUI.Core.UI.Text
{
    public record struct MGTextRunConfig(bool IsBold, bool IsItalic = false, bool IsUnderlined = false, float Opacity = 1.0f,
        Color? Foreground = null, IFillBrush Background = null, bool IsShadowed = false, Color? ShadowColor = null, Vector2? ShadowOffset = null)
    {
        private static T RemoveLast<T>(IList<T> Items, T DefaultValue)
        {
            if (!Items.Any())
                return DefaultValue;
            else
            {
                T Value = Items[Items.Count - 1];
                Items.RemoveAt(Items.Count - 1);
                return Value;
            }
        }

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
                    return this with { Opacity = RemoveLast(PreviousOpacities, 1.0f) };

                //  Foreground
                case FTActionType.SetForeground:
                    {
                        PreviousForegrounds.Add(Foreground);
                        Color Value = ColorTranslator.FromHtml(Action.Parameter.Substring(1)).AsXNAColor();
                        return this with { Foreground = Value };
                    }
                case FTActionType.RevertForeground:
                    return this with { Foreground = RemoveLast(PreviousForegrounds, null) };

                //  Background
                case FTActionType.SetBackground:
                    {
                        PreviousBackgrounds.Add(Background);
                        Color Value = ColorTranslator.FromHtml(Action.Parameter.Substring(1)).AsXNAColor();
                        return this with { Background = new MGSolidFillBrush(Value) };
                    }
                case FTActionType.RevertBackground:
                    return this with { Background = RemoveLast(PreviousBackgrounds, null) };

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
                        IsShadowed = RemoveLast(PreviousShadowStates, false), 
                        ShadowColor = RemoveLast(PreviousShadowColors, null), 
                        ShadowOffset = RemoveLast(PreviousShadowOffsets, null)
                    };

                //  Image
                case FTActionType.Image:
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

    public readonly record struct MGTextRunImage(string RegionName, int TargetWidth, int TargetHeight)
    {
        public static readonly MGTextRunImage Empty = new(null, 0, 0);
    }

    public readonly record struct MGTextRun(string Text, bool IsLineBreak, bool IsImage, MGTextRunImage ImageSettings, MGTextRunConfig Settings)
    {
        public static readonly FTParser Parser = new();
        public static readonly FTTokenizer Tokenizer = new();

        public string FirstWord(params char[] WordDelimiters)
            => new string(Text.TakeWhile(x => !WordDelimiters.Contains(x)).ToArray());

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

                    yield return new(LiteralValue.ToString(), false, false, MGTextRunImage.Empty, CurrentState);
                }
                else if (CurrentAction.ActionType == FTActionType.LineBreak)
                    yield return new(CurrentAction.Parameter, true, false, MGTextRunImage.Empty, CurrentState);
                else if (CurrentAction.ActionType == FTActionType.Image)
                {
                    var (RegionName, TargetWidth, TargetHeight) = FTTokenizer.ParseImageValue(CurrentAction.Parameter.Substring(1));
                    yield return new(CurrentAction.Parameter, false, true, new MGTextRunImage(RegionName, TargetWidth, TargetHeight), CurrentState);
                }
            }
        }
    }
}
