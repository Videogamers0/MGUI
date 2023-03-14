using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if UseWPF
using System.Windows.Data;
using System.Windows.Markup;
#endif

namespace MGUI.Core.UI.Data_Binding.Converters
{
#if UseWPF
    public class InverseBoolConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool BoolValue)
                return !BoolValue;
            else
                throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool BoolValue)
                return !BoolValue;
            else
                throw new NotImplementedException();
        }

        private static readonly InverseBoolConverter Instance = new();
        public override object ProvideValue(IServiceProvider serviceProvider) => Instance;
    }
#endif
}
