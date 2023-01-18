using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI.Text
{
    /// <summary>Represents an action to apply to a piece of formatted text.</summary>
    public enum FTActionType
    {
        Ignore,

        EnableBold,
        DisableBold,

        EnableItalic,
        DisableItalic,

        SetUnderline,
        RevertUnderline,

        SetOpacity,
        RevertOpacity,

        SetForeground,
        RevertForeground,

        SetBackground,
        RevertBackground,

        SetShadowing,
        RevertShadowing,

        Image,

        SetToolTip,
        RevertToolTip,

        SetAction,
        RevertAction,

        StringLiteral,

        LineBreak

        //Could also support a tag like [size=14] (or [scale=1.5]) but could be annoying when measuring and wrapping text
        //Could also support \t as 'TabBreak'? Treat it as a set amount of spaces, such as 4
    }

    public record struct FTAction(FTActionType ActionType, string Parameter);

    /// <summary>Describes a pattern of <see cref="FTTokenType"/>s to look for to determine what action to apply to a piece of formatted text that contains markdown.</summary>
    public class FTActionDefinition
    {
        public FTActionType ActionType { get; }

        /// <summary>The token type whose match contains the value that is used as the parameter for the action.<para/>
        /// For example, <see cref="FTTokenType.OpacityValue"/> contains the value that should be used when the <see cref="FTActionType.SetOpacity"/> action is applied.<para/>
        /// Not all actions require a parameter (such as <see cref="FTActionType.EnableBold"/>), so this property is not relevant in all cases.</summary>
        public FTTokenType PrimaryTokenType { get; }

        /// <summary>The token types that must occur in a specific order to match this action.<para/>
        /// For example: <see cref="FTActionType.EnableBold"/> occurs when these tokens appear in this exact order:<br/>
        /// 1. <see cref="FTTokenType.OpenTag"/><br/>
        /// 2. <see cref="FTTokenType.BoldOpenTagType"/><br/>
        /// 3. <see cref="FTTokenType.CloseTag"/></summary>
        public ReadOnlyCollection<FTTokenType> OrderedTokenTypes { get; }

        public FTActionDefinition(FTActionType ActionType, FTTokenType PrimaryTokenType, params FTTokenType[] OrderedTokenTypes)
        {
            this.ActionType = ActionType;
            this.PrimaryTokenType = PrimaryTokenType;
            this.OrderedTokenTypes = OrderedTokenTypes.ToList().AsReadOnly();
        }

        public bool IsNextMatch(IReadOnlyList<FTTokenMatch> NextTokens, out FTAction Match)
        {
            if (NextTokens.Count >= OrderedTokenTypes.Count && NextTokens.Select(x => x.TokenType).Take(OrderedTokenTypes.Count).SequenceEqual(OrderedTokenTypes))
            {
                Match = new(ActionType, NextTokens.First(x => x.TokenType == PrimaryTokenType).Value);
                return true;
            }
            else
            {
                Match = default;
                return false;
            }
        }
    }

    /// <summary>Parser that determines what actions to apply to a piece of formatted text, such as enabling or disabling bold font, or changing the font foreground color.</summary>
    public class FTParser
    {
        private List<FTActionDefinition> Definitions { get; }

        public FTParser()
        {
            this.Definitions = new List<FTActionDefinition>();

            //  Bold
            Definitions.Add(new(FTActionType.EnableBold, FTTokenType.BoldOpenTagType, 
                FTTokenType.OpenTag, FTTokenType.BoldOpenTagType, FTTokenType.CloseTag));
            Definitions.Add(new(FTActionType.DisableBold, FTTokenType.BoldCloseTagType,
                FTTokenType.OpenTag, FTTokenType.BoldCloseTagType, FTTokenType.CloseTag));

            //  Italic
            Definitions.Add(new(FTActionType.EnableItalic, FTTokenType.ItalicOpenTagType,
                FTTokenType.OpenTag, FTTokenType.ItalicOpenTagType, FTTokenType.CloseTag));
            Definitions.Add(new(FTActionType.DisableItalic, FTTokenType.ItalicCloseTagType,
                FTTokenType.OpenTag, FTTokenType.ItalicCloseTagType, FTTokenType.CloseTag));

            //  Opacity
            Definitions.Add(new(FTActionType.SetOpacity, FTTokenType.OpacityValue,
                FTTokenType.OpenTag, FTTokenType.OpacityOpenTagType, FTTokenType.OpacityValue, FTTokenType.CloseTag));
            Definitions.Add(new(FTActionType.RevertOpacity, FTTokenType.OpacityCloseTagType,
                FTTokenType.OpenTag, FTTokenType.OpacityCloseTagType, FTTokenType.CloseTag));

            //  Foreground
            Definitions.Add(new(FTActionType.SetForeground, FTTokenType.ForegroundValue,
                FTTokenType.OpenTag, FTTokenType.ForegroundOpenTagType, FTTokenType.ForegroundValue, FTTokenType.CloseTag));
            Definitions.Add(new(FTActionType.RevertForeground, FTTokenType.ForegroundCloseTagType,
                FTTokenType.OpenTag, FTTokenType.ForegroundCloseTagType, FTTokenType.CloseTag));

            //  Underline
            Definitions.Add(new(FTActionType.SetUnderline, FTTokenType.UnderlineValue,
                FTTokenType.OpenTag, FTTokenType.UnderlineOpenTagType, FTTokenType.UnderlineValue, FTTokenType.CloseTag));
            Definitions.Add(new(FTActionType.RevertUnderline, FTTokenType.UnderlineCloseTagType,
                FTTokenType.OpenTag, FTTokenType.UnderlineCloseTagType, FTTokenType.CloseTag));

            //  Background
            Definitions.Add(new(FTActionType.SetBackground, FTTokenType.BackgroundValue,
                FTTokenType.OpenTag, FTTokenType.BackgroundOpenTagType, FTTokenType.BackgroundValue, FTTokenType.CloseTag));
            Definitions.Add(new(FTActionType.RevertBackground, FTTokenType.BackgroundCloseTagType,
                FTTokenType.OpenTag, FTTokenType.BackgroundCloseTagType, FTTokenType.CloseTag));

            //  Shadow
            Definitions.Add(new(FTActionType.SetShadowing, FTTokenType.ShadowValue,
                FTTokenType.OpenTag, FTTokenType.ShadowOpenTagType, FTTokenType.ShadowValue, FTTokenType.CloseTag));
            Definitions.Add(new(FTActionType.RevertShadowing, FTTokenType.ShadowCloseTagType,
                FTTokenType.OpenTag, FTTokenType.ShadowCloseTagType, FTTokenType.CloseTag));

            //  Image
            Definitions.Add(new(FTActionType.Image, FTTokenType.ImageValue,
                FTTokenType.OpenTag, FTTokenType.ImageOpenTagType, FTTokenType.ImageValue, FTTokenType.CloseTag));

            //  ToolTip
            Definitions.Add(new(FTActionType.SetToolTip, FTTokenType.ToolTipValue,
                FTTokenType.OpenTag, FTTokenType.ToolTipOpenTagType, FTTokenType.ToolTipValue, FTTokenType.CloseTag));
            Definitions.Add(new(FTActionType.RevertToolTip, FTTokenType.ToolTipCloseTagType,
                FTTokenType.OpenTag, FTTokenType.ToolTipCloseTagType, FTTokenType.CloseTag));

            //  Action
            Definitions.Add(new(FTActionType.SetAction, FTTokenType.ActionValue,
                FTTokenType.OpenTag, FTTokenType.ActionOpenTagType, FTTokenType.ActionValue, FTTokenType.CloseTag));
            Definitions.Add(new(FTActionType.RevertAction, FTTokenType.ActionCloseTagType,
                FTTokenType.OpenTag, FTTokenType.ActionCloseTagType, FTTokenType.CloseTag));

            //  String value
            Definitions.Add(new(FTActionType.StringLiteral, FTTokenType.StringLiteral,
                FTTokenType.StringLiteral));

            //  LineBreak
            Definitions.Add(new(FTActionType.LineBreak, FTTokenType.LineBreak,
                FTTokenType.LineBreak));
        }

        public IEnumerable<FTAction> ParseTokens(IReadOnlyList<FTTokenMatch> TokenMatches)
        {
            List<FTTokenMatch> RemainingTokens = TokenMatches.ToList();
            while (RemainingTokens.Any())
            {
                bool Found = false;
                foreach (var Definition in Definitions)
                {
                    if (Definition.IsNextMatch(RemainingTokens, out FTAction Match))
                    {
                        if (Match.ActionType != FTActionType.Ignore)
                            yield return Match;
                        RemainingTokens.RemoveRange(0, Definition.OrderedTokenTypes.Count);
                        Found = true;
                        break;
                    }
                }

                if (!Found)
                {
                    string ErrorMsg = $"Failed to parse {nameof(FTTokenMatch)}s: No valid actions found in remaining sequence: {string.Join(", ", RemainingTokens.Select(x => x.TokenType))}";
                    throw new InvalidOperationException(ErrorMsg);
                }
            }
        }
    }
}
