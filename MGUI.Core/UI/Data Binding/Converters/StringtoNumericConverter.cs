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
    public class StringToNumericConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string StringValue)
                return System.Convert.ChangeType(StringValue, targetType, culture);
            else
                throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IConvertible TypedValue)
                return TypedValue.ToString(culture);
            else
                throw new NotImplementedException();
        }

        private static readonly StringToNumericConverter Instance = new();
        public override object ProvideValue(IServiceProvider serviceProvider) => Instance;
    }
}
