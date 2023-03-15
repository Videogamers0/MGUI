using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if UseWPF
using System.Windows.Markup;
using System.Windows.Data;
#else
using Portable.Xaml.Markup;
#endif

namespace MGUI.Core.UI.Data_Binding.Converters
{
    public class BoolToVisibilityConverter : MarkupExtension, IValueConverter
    {
        public Visibility TrueVisibility { get; set; } = Visibility.Visible;
        public Visibility FalseVisibility { get; set; } = Visibility.Collapsed;
        public Visibility NullVisibility { get; set; } = Visibility.Collapsed;
        public bool AllowNullBoolean { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null && !AllowNullBoolean)
                throw new InvalidOperationException($"{nameof(BoolToVisibilityConverter)}: Cannot convert from null value when {nameof(AllowNullBoolean)} is false.");
            else if (value == null)
                return NullVisibility;
            else if (value is bool BoolValue)
                return BoolValue ? TrueVisibility : FalseVisibility;
            else
                throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility VisibilityValue)
            {
                if (!AllowNullBoolean)
                {
                    if (VisibilityValue == TrueVisibility)
                        return true;
                    else if (VisibilityValue == FalseVisibility)
                        return false;
                    else
                        throw new NotImplementedException();
                }
                else
                {
                    if (VisibilityValue == TrueVisibility)
                        return true;
                    else if (VisibilityValue == FalseVisibility)
                        return false;
                    else if (VisibilityValue == NullVisibility)
                        return null;
                    else
                        throw new NotImplementedException();
                }
            }
            else
            {
                throw new InvalidOperationException($"{nameof(BoolToVisibilityConverter)}: Cannot convert back to a boolean from value of type {value?.GetType()?.Name ?? "null"}. " +
                    $"Expected value of type: {nameof(Visibility)}");
            }
        }

        public override object ProvideValue(IServiceProvider serviceProvider) => this;
    }
}
