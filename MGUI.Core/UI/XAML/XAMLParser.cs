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
using System.IO;
using System.Xml;
using System.Xml.Linq;
using MGUI.Shared.Helpers;

#if UseWPF
using System.Xaml;
using System.Windows.Markup;
#endif

namespace MGUI.Core.UI.XAML
{
    public class XAMLParser
    {
        private const string XMLNameSpaceBaseUri = @"http://schemas.microsoft.com/winfx/2006/xaml/presentation";

        public const string XMLLocalNameSpacePrefix = "MGUI";
        public static readonly string XMLLocalNameSpaceUri = $"clr-namespace:{nameof(MGUI)}.{nameof(Core)}.{nameof(UI)}.{nameof(XAML)};assembly={nameof(MGUI)}.{nameof(Core)}";

        private static readonly string XMLNameSpaces = 
            $"xmlns=\"{XMLNameSpaceBaseUri}\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:{XMLLocalNameSpacePrefix}=\"{XMLLocalNameSpaceUri}\"";
            //$"xmlns:x=\"{XMLNameSpaceBaseUri}\" xmlns=\"{XMLLocalNameSpaceUri}\""; // This URI avoids using a prefix for the MGUI namespace

        private static readonly Dictionary<string, string> ElementNameAliases = new()
        {
            { "ContentPresenter", nameof(XAMLContentPresenter) },
            { "HeaderedContentPresenter", nameof(XAMLHeaderedContentPresenter) },

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
            { "ListBox", nameof(XAMLListBox) },
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
            { "Stopwatch", nameof(XAMLStopwatch) },
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
            { "UniformGrid", nameof(XAMLUniformGrid) },
            { "DockPanel", nameof(XAMLDockPanel) },
            { "StackPanel", nameof(XAMLStackPanel) },
            { "OverlayPanel", nameof(XAMLOverlayPanel) },

            { "Style", nameof(XAMLStyle) },
            { "Setter", nameof(XAMLStyleSetter) },

            //  Abbreviated names
            { "CP", nameof(XAMLContentPresenter) },
            { "HCP", nameof(XAMLHeaderedContentPresenter) },
            { "CM", nameof(XAMLContextMenu) },
            { "CMB", nameof(XAMLContextMenuButton) },
            { "CMT", nameof(XAMLContextMenuToggle) },
            { "CMS", nameof(XAMLContextMenuSeparator) },
            { "GB", nameof(XAMLGroupBox) },
            { "LB", nameof(XAMLListBox) },
            { "LV", nameof(XAMLListView) },
            { "LVC", nameof(XAMLListViewColumn) },
            { "RB", nameof(XAMLRadioButton) },
            { "RC", nameof(XAMLRatingControl) },
            { "RG", nameof(XAMLResizeGrip) },
            { "SV", nameof(XAMLScrollViewer) },
            { "TC", nameof(XAMLTabControl) },
            { "TI", nameof(XAMLTabItem) },
            { "TB", nameof(XAMLTextBlock) },
            { "TT", nameof(XAMLToolTip) },
            { "RD", nameof(XAMLRowDefinition) },
            { "CD", nameof(XAMLColumnDefinition) },
            { "GS", nameof(XAMLGridSplitter) },
            { "UG", nameof(XAMLUniformGrid) },
            { "DP", nameof(XAMLDockPanel) },
            { "SP", nameof(XAMLStackPanel) },
            { "OP", nameof(XAMLOverlayPanel) }
        };

        private static string ValidateXAMLString(string XAMLString)
        {
            XAMLString = XAMLString.Trim();

            //  TODO: First line might not be an XElement. Could be something like a comment,
            //  so we should use more robust logic to insert the namespaces
            int FirstLineBreakIndex = XAMLString.IndexOfAny(new char[] { '\n', '\r' });
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

            //  Replace all element names with their fully-qualified, aliased name, such as:
            //  "<Button/>"                 --> "<MGUI:XAMLButton/>"
            //  "<Button Content="Foo" />"  --> "<MGUI:XAMLButton Content="Foo" />"
            //  "<Button.Content>"          --> "<MGUI:XAMLButton.Content>"
            //  ("XAMLButton" is the aliased name for "Button", and the type exists in the xml namespace referenced by the "MGUI" prefix)
#if true
            XDocument Document = XDocument.Parse(XAMLString);

            foreach (var Element in Document.Descendants())
            {
                string ElementName = Element.Name.LocalName;
                foreach (KeyValuePair<string, string> KVP in ElementNameAliases)
                {
                    if (ElementName.StartsWith(KVP.Key))
                    {
                        Element.Name = XName.Get(ElementName.ReplaceFirstOccurrence(KVP.Key, KVP.Value), XMLLocalNameSpaceUri);
                        break;
                    }
                }
            }

            using (StringWriter SW = new())
            {
                using (XmlWriter XW = XmlWriter.Create(SW, new XmlWriterSettings() { OmitXmlDeclaration = true, Indent = true }))
                {
                    Document.WriteTo(XW);
                }

                string Result = SW.ToString();
                return Result;
            }
#else
            StringBuilder SB = new(XAMLString);
            foreach (KeyValuePair<string, string> KVP in ElementNameAliases)
            {
                SB.Replace($"<{KVP.Key} ", $"<{XMLLocalNameSpacePrefix}:{KVP.Value} ");
                SB.Replace($"<{KVP.Key}.", $"<{XMLLocalNameSpacePrefix}:{KVP.Value}.");
                SB.Replace($"<{KVP.Key}/", $"<{XMLLocalNameSpacePrefix}:{KVP.Value}/");
                SB.Replace($"<{KVP.Key}>", $"<{XMLLocalNameSpacePrefix}:{KVP.Value}>");

                SB.Replace($"</{KVP.Key} ", $"</{XMLLocalNameSpacePrefix}:{KVP.Value} ");
                SB.Replace($"</{KVP.Key}.", $"</{XMLLocalNameSpacePrefix}:{KVP.Value}.");
                SB.Replace($"</{KVP.Key}>", $"</{XMLLocalNameSpacePrefix}:{KVP.Value}>");
            }
            return SB.ToString();
#endif
        }

#if UseWPF
        /// <param name="SanitizeXAMLString">If true, the given <paramref name="XAMLString"/> will be pre-processed via the following logic:<para/>
        /// 1. Trim leading and trailing whitespace<br/>
        /// 2. Insert required XML namespaces (such as "xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation")<br/>
        /// 3. Replace aliased type names with their fully-qualified type names, such as "Button" -> "MGUI:XAMLButton" where the "MGUI" namespace prefix points to the URI defined by <see cref="XMLLocalNameSpaceUri"/><para/>
        /// If your XAML isn't using any aliased element names, you probably should set this to false.</param>
        /// <param name="ReplaceLinebreakLiterals">If true, the literal string @"\n" will be replaced with "&#38;#x0a;", which is the XAML representation of the linebreak character '\n'.<br/>
        /// If false, setting the text of an <see cref="MGTextBlock"/> requires encoding the '\n' character as ""&#38;#x0a;""<para/>
        /// See also: <see href="https://stackoverflow.com/a/183435/11689514"/></param>
        public static T Load<T>(MGWindow Window, string XAMLString, bool SanitizeXAMLString, bool ReplaceLinebreakLiterals = true)
            where T : MGElement
        {
            if (SanitizeXAMLString)
                XAMLString = ValidateXAMLString(XAMLString);
            if (ReplaceLinebreakLiterals)
                XAMLString = XAMLString.Replace(@"\n", "&#x0a;");
            XAMLElement Parsed = (XAMLElement)XamlServices.Parse(XAMLString);
            Parsed.ProcessStyles();
            return Parsed.ToElement<T>(Window, null);
        }

        /// <param name="SanitizeXAMLString">If true, the given <paramref name="XAMLString"/> will be pre-processed via the following logic:<para/>
        /// 1. Trim leading and trailing whitespace<br/>
        /// 2. Insert required XML namespaces (such as "xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation")<br/>
        /// 3. Replace aliased type names with their fully-qualified type names, such as "Button" -> "MGUI:XAMLButton" where the "MGUI" namespace prefix points to the URI defined by <see cref="XMLLocalNameSpaceUri"/><para/>
        /// If your XAML isn't using any aliased element names, you probably should set this to false.</param>
        /// <param name="ReplaceLinebreakLiterals">If true, the literal string @"\n" will be replaced with the "v", which is the XAML representation of the linebreak character '\n'.<br/>
        /// If false, setting the text of an <see cref="MGTextBlock"/> requires encoding the '\n' character as ""&#38;#x0a;""<para/>
        /// See also: <see href="https://stackoverflow.com/a/183435/11689514"/></param>
        public static MGWindow LoadRootWindow(MGDesktop Desktop, string XAMLString, bool SanitizeXAMLString, bool ReplaceLinebreakLiterals = true)
        {
            if (SanitizeXAMLString)
                XAMLString = ValidateXAMLString(XAMLString);
            if (ReplaceLinebreakLiterals)
                XAMLString = XAMLString.Replace(@"\n", "&#x0a;");
            XAMLWindow Parsed = (XAMLWindow)XamlServices.Parse(XAMLString);
            Parsed.ProcessStyles();
            return Parsed.ToElement(Desktop);
        }
#endif
    }
}
