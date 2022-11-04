using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.Brushes.Border_Brushes;
using Microsoft.Xna.Framework.Graphics;

#if UseWPF
using System.Xaml;
using System.Windows.Markup;
#endif

namespace MGUI.Core.UI.XAML
{
    public class XAMLParser
    {
        private const string XMLNameSpaceBaseUri = @"http://schemas.microsoft.com/winfx/2006/xaml/presentation";

        public const string XMLLocalNameSpacePrefix = "local";
        public static readonly string XMLLocalNameSpaceUri = $"clr-namespace:{nameof(MGUI)}.{nameof(Core)}.{nameof(UI)}.{nameof(XAML)};assembly={nameof(MGUI)}.{nameof(Core)}";

        private static readonly string XMLNameSpaces = 
            $"xmlns=\"{XMLNameSpaceBaseUri}\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:{XMLLocalNameSpacePrefix}=\"{XMLLocalNameSpaceUri}\"";
            //$"xmlns:x=\"{XMLNameSpaceBaseUri}\" xmlns=\"{XMLLocalNameSpaceUri}\"";; // This URI avoids using a prefix for the local namespace

        private static readonly Dictionary<string, string> ElementNameAliases = new()
        {
            { "ContentPresenter", nameof(XAMLContentPresenter) },
            { "Border", nameof(XAMLBorder) },
            { "Button", nameof(XAMLButton) },
            { "CheckBox", nameof(XAMLCheckBox) },
            { "ComboBox", nameof(XAMLComboBox) },

            { "ContextMenu", nameof(XAMLContextMenu) },
            { "ContextMenuButton", nameof(XAMLContextMenuButton) },
            { "ContextMenuToggle", nameof(XAMLContextMenuToggle) },
            { "ContextMenuSeparator", nameof(XAMLContextMenuSeparator) },

            { "Expander", nameof(XAMLExpander) },
            { "GroupBox", nameof(XAMLGroupBox) },
            { "Image", nameof(XAMLImage) },
            { "ListView", nameof(XAMLListView) },
            { "ListViewColumn", nameof(XAMLListViewColumn) },
            { "PasswordBox", nameof(XAMLPasswordBox) },
            { "ProgressBar", nameof(XAMLProgressBar) },
            { "RadioButton", nameof(XAMLRadioButton) },
            { "RatingControl", nameof(XAMLRatingControl) },
            { "Rectangle", nameof(XAMLRectangleElement) },
            { "ResizeGrip", nameof(XAMLResizeGrip) },
            { "ScrollViewer", nameof(XAMLScrollViewer) },
            { "Separator", nameof(XAMLSeparator) },
            { "Slider", nameof(XAMLSlider) },
            { "Spacer", nameof(XAMLSpacer) },
            { "Spoiler", nameof(XAMLSpoiler) },
            { "StopWatch", nameof(XAMLStopWatch) },
            { "TabControl", nameof(XAMLTabControl) },
            { "TabItem", nameof(XAMLTabItem) },
            { "TextBlock", nameof(XAMLTextBlock) },
            { "TextBox", nameof(XAMLTextBox) },
            { "Timer", nameof(XAMLTimer) },
            { "ToggleButton", nameof(XAMLToggleButton) },
            { "ToolTip", nameof(XAMLToolTip) },
            { "Window", nameof(XAMLWindow) },

            { "RowDefinition", nameof(XAMLRowDefinition) },
            { "ColumnDefinition", nameof(XAMLColumnDefinition) },
            { "GridSplitter", nameof(XAMLGridSplitter) },
            { "Grid", nameof(XAMLGrid) },
            { "DockPanel", nameof(XAMLDockPanel) },
            { "StackPanel", nameof(XAMLStackPanel) },
            { "OverlayPanel", nameof(XAMLOverlayPanel) }
        };

        private static string ValidateXAMLString(string XAMLString)
        {
            XAMLString = XAMLString.Trim();

            int FirstLineBreakIndex = XAMLString.IndexOf('\n');
            string FirstLine = FirstLineBreakIndex < 0 ? XAMLString : XAMLString.Substring(0, FirstLineBreakIndex);
            if (!FirstLine.Contains(XMLNameSpaces))
            {
                //  Insert the required xml namespaces into the XAML string
                int SpaceIndex = XAMLString.IndexOf(' ');
                if (SpaceIndex >= 0)
                    XAMLString = $"{XAMLString.Substring(0, SpaceIndex)} {XMLNameSpaces} {XAMLString.Substring(SpaceIndex + 1)}";
                else
                {
                    int InsertionIndex = XAMLString.IndexOf('>');
                    XAMLString = $"{XAMLString.Substring(0, InsertionIndex)} {XMLNameSpaces} {XAMLString.Substring(InsertionIndex)}";
                }
            }

            StringBuilder SB = new(XAMLString);
            foreach (KeyValuePair<string, string> KVP in ElementNameAliases)
            {
#if true
                SB.Replace($"<{KVP.Key}", $"<{XMLLocalNameSpacePrefix}:{KVP.Value}");
                SB.Replace($"</{KVP.Key}", $"</{XMLLocalNameSpacePrefix}:{KVP.Value}");
#else
                SB.Replace($"<{KVP.Key}", $"<{KVP.Value}");
                SB.Replace($"</{KVP.Key}", $"</{KVP.Value}");
#endif
            }
            return SB.ToString();
        }

#if UseWPF
        /// <param name="NamedTextures">Can be null. This dictionary is used to resolve textures used by <see cref="MGImage"/>s, such as if the <paramref name="XAMLString"/> contained:
        /// <code>&lt;Image Texture='Sample1' /&gt;</code>
        /// Then this dictionary would be expected to contain a value for Key='Sample1'</param>
        /// <param name="SanitizeXAMLString">If true, the given <paramref name="XAMLString"/> will be pre-processed via the following logic:<para/>
        /// 1. Trim leading and trailing whitespace<br/>
        /// 2. Insert required XML namespaces (such as "xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation")<br/>
        /// 3. Replace aliased type names with their fully-qualified type names, such as "Button" -> "local:XAMLButton" where the "local" namespace prefix points to the URI defined by <see cref="XMLLocalNameSpaceUri"/></param>
        /// <param name="ReplaceLinebreakLiterals">If true, the literal string @"\n" will be replaced with "&#38;#x0a;", which is the XAML representation of the linebreak character '\n'.<br/>
        /// If false, setting the text of an <see cref="MGTextBlock"/> requires encoding the '\n' character as ""&#38;#x0a;""<para/>
        /// See also: <see href="https://stackoverflow.com/a/183435/11689514"/></param>
        public static T Load<T>(MGWindow Window, string XAMLString, Dictionary<string, Texture2D> NamedTextures = null,
            bool SanitizeXAMLString = true, bool ReplaceLinebreakLiterals = true)
            where T : MGElement
        {
            if (SanitizeXAMLString)
                XAMLString = ValidateXAMLString(XAMLString);
            if (ReplaceLinebreakLiterals)
                XAMLString = XAMLString.Replace(@"\n", "&#x0a;");
            XAMLElement Parsed = (XAMLElement)XamlServices.Parse(XAMLString);
            return Parsed.ToElement<T>(Window, null, NamedTextures);
        }

        /// <param name="NamedTextures">Can be null. This dictionary is used to resolve textures used by <see cref="MGImage"/>s, such as if the <paramref name="XAMLString"/> contained:
        /// <code>&lt;Image Texture='Sample1' /&gt;</code>
        /// Then this dictionary would be expected to contain a value for Key='Sample1'</param>
        /// <param name="SanitizeXAMLString">If true, the given <paramref name="XAMLString"/> will be pre-processed via the following logic:<para/>
        /// 1. Trim leading and trailing whitespace<br/>
        /// 2. Insert required XML namespaces (such as "xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation")<br/>
        /// 3. Replace aliased type names with their fully-qualified type names, such as "Button" -> "local:XAMLButton" where the "local" namespace prefix points to the URI defined by <see cref="XMLLocalNameSpaceUri"/></param>
        /// <param name="ReplaceLinebreakLiterals">If true, the literal string @"\n" will be replaced with the "v", which is the XAML representation of the linebreak character '\n'.<br/>
        /// If false, setting the text of an <see cref="MGTextBlock"/> requires encoding the '\n' character as ""&#38;#x0a;""<para/>
        /// See also: <see href="https://stackoverflow.com/a/183435/11689514"/></param>
        public static MGWindow LoadRootWindow(MGDesktop Desktop, string XAMLString, Dictionary<string, Texture2D> NamedTextures = null,
            bool SanitizeXAMLString = true, bool ReplaceLinebreakLiterals = true)
        {
            if (SanitizeXAMLString)
                XAMLString = ValidateXAMLString(XAMLString);
            if (ReplaceLinebreakLiterals)
                XAMLString = XAMLString.Replace(@"\n", "&#x0a;");
            XAMLWindow Parsed = (XAMLWindow)XamlServices.Parse(XAMLString);
            return Parsed.ToElement(Desktop, NamedTextures);
        }
#endif
    }
}
