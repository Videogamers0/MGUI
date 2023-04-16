using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MGUI.Core.UI.XAML;

#if UseWPF
using System.Windows.Markup;
using System.Windows.Data;
#else
using Portable.Xaml.Markup;
#endif

namespace MGUI.Core.UI.Data_Binding.Converters
{
    public class StringToToolTipConverter : MarkupExtension, IValueConverter
    {
        private static readonly Dictionary<string, Action<MGToolTip, MGTextBlock>> _Styles = new();
        /// <summary>See also:<br/>
        /// <see cref="AddNamedStyle(string, Action{MGToolTip, MGTextBlock})"/><br/>
        /// <see cref="RemoveNamedStyle(string)"/></summary>
        public static IReadOnlyDictionary<string, Action<MGToolTip, MGTextBlock>> Styles => _Styles;
        /// <summary>Adds a named style to <see cref="Styles"/> so that it can be re-used and referenced via <see cref="StylePreset"/>.<para/>
        /// See also: <see cref="RemoveNamedStyle(string)"/></summary>
        public static void AddNamedStyle(string Name, Action<MGToolTip, MGTextBlock> Style) => _Styles.Add(Name, Style);
        public static bool RemoveNamedStyle(string Name) => _Styles.Remove(Name);

        /// <summary>If specified, the style with this name in <see cref="Styles"/> will be applied to the generated <see cref="MGToolTip"/> 
        /// when converting from a string value to a <see cref="MGToolTip"/>.<para/>
        /// This style is applied first, before any other properties such as <see cref="Background"/> or <see cref="FontSize"/> are resolved.</summary>
        public string StylePreset { get; set; }

        //  This property is automatically set when processing the XAML DataBindings in MGUI.Core.UI.XAML.Element.ProcessBindings(...)
        /// <summary>The UI element that the generated <see cref="MGToolTip"/> will be applied to.</summary>
        public MGElement Host { get; internal set; }

        public Thickness? Padding { get; set; }
        public FillBrush Background { get; set; }

        public int? Width { get; set; }
        public int? MinWidth { get; set; }
        public int? MaxWidth { get; set; }
        public int? Height { get; set; }
        public int? MinHeight { get; set; }
        public int? MaxHeight { get; set; }

        public SizeToContent? SizeToContent { get; set; }

        public TimeSpan? ShowDelay { get; set; }
        public bool? ShowOnDisabled { get; set; }

        public string FontFamily { get; set; }
        public int? FontSize { get; set; }

        public bool? IsBold { get; set; }
        public bool? IsItalic { get; set; }
        public bool? IsUnderlined { get; set; }
        public bool? IsShadowed { get; set; }

        public XAMLColor? Foreground { get; set; }

        public bool? AllowsInlineFormatting { get; set; }
        public bool? WrapText { get; set; }
        public HorizontalAlignment? TextAlignment { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(MGToolTip))
                throw new InvalidOperationException($"{nameof(StringToToolTipConverter)} can only convert from string to {nameof(MGToolTip)}. Invalid {nameof(targetType)}: {targetType.FullName}");
            else if (value == null)
                return null;
            else if (value is not string StringValue)
                throw new InvalidOperationException($"{nameof(StringToToolTipConverter)} can only convert from string to {nameof(MGToolTip)}. Invalid input {nameof(value)}: {value}");
            else
            {
                MGToolTip ToolTip = StringToToolTipTypeConverter.ToToolTip(Host, StringValue);
                MGTextBlock TextBlock = ToolTip.Content as MGTextBlock;

                if (!string.IsNullOrEmpty(StylePreset) && _Styles.TryGetValue(StylePreset, out var DefaultStyle))
                    DefaultStyle?.Invoke(ToolTip, TextBlock);

                if (Padding.HasValue)
                    ToolTip.Padding = Padding.Value.ToThickness();
                if (Background != null)
                    ToolTip.BackgroundBrush.NormalValue = Background.ToFillBrush(ToolTip.GetDesktop());

                if (Width.HasValue)
                    ToolTip.PreferredWidth = Width.Value;
                if (MinWidth.HasValue)
                    ToolTip.MinWidth = MinWidth.Value;
                if (MaxWidth.HasValue)
                    ToolTip.MaxWidth = MaxWidth.Value;
                if (Height.HasValue)
                    ToolTip.PreferredHeight = Height.Value;
                if (MinHeight.HasValue)
                    ToolTip.MinHeight = MinHeight.Value;
                if (MaxHeight.HasValue)
                    ToolTip.MaxHeight = MaxHeight.Value;

                if (ShowOnDisabled.HasValue)
                    ToolTip.ShowOnDisabled = ShowOnDisabled.Value;
                if (ShowDelay.HasValue)
                    ToolTip.ShowDelayOverride = ShowDelay.Value;

                if (!string.IsNullOrEmpty(FontFamily))
                    TextBlock.FontFamily = FontFamily;
                if (FontSize.HasValue)
                    TextBlock.FontSize = FontSize.Value;

                if (IsBold.HasValue)
                    TextBlock.IsBold = IsBold.Value;
                if (IsItalic.HasValue)
                    TextBlock.IsItalic = IsItalic.Value;
                if (IsUnderlined.HasValue)
                    TextBlock.IsUnderlined = IsUnderlined.Value;
                if (IsShadowed.HasValue)
                    TextBlock.IsShadowed = IsShadowed.Value;

                if (Foreground.HasValue)
                    TextBlock.Foreground.NormalValue = Foreground.Value.ToXNAColor();

                if (AllowsInlineFormatting.HasValue)
                    TextBlock.AllowsInlineFormatting = AllowsInlineFormatting.Value;
                if (WrapText.HasValue)
                    TextBlock.WrapText = WrapText.Value;
                if (TextAlignment.HasValue)
                    TextBlock.TextAlignment = TextAlignment.Value;

                if (SizeToContent.HasValue)
                    ToolTip.ApplySizeToContent(SizeToContent.Value, 10, 10, null, null, false);

                return ToolTip;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException($"{nameof(StringToToolTipConverter)}.{nameof(ConvertBack)} is not possible " +
                $"because there does not exist a 1-1 mapping between strings and ToolTips.");
        }

        public override object ProvideValue(IServiceProvider serviceProvider) => this;
    }
}
