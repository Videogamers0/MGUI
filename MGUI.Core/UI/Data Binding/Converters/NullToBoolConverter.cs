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
    public class NullToBoolConverter : MarkupExtension, IValueConverter
    {
        public bool NullValue { get; set; } = true;
        public bool NonNullValue { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value == null ? NullValue : NonNullValue;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider) => this;
    }
}
