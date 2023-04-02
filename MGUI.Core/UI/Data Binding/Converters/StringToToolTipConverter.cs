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
        //TODO a convenient way to specify a set of common settings:
        //static readonly Dictionary<string, Action<MGToolTip, MGTextBlock> _Styles
        //IReadOnlyDictionary<string, Action<MGToolTip, MGTextBlock>> Styles => _Styles
        //AddNamedStyle(string Name, Action...)
        //string Preset (uses a named key in Styles, invokes the action on the tooltip/textblock at start of the Convert method.)

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
