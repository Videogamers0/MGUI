using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI.XAML
{
    [TypeConverter(typeof(XAMLSizeStringConverter))]
    public struct XAMLSize
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public XAMLSize() : this(0, 0) { }
        public XAMLSize(int Both) : this(Both, Both) { }
        public XAMLSize(int Width, int Height)
        {
            this.Width = Width;
            this.Height = Height;
        }

        public override string ToString() => $"{nameof(XAMLSize)}: ({Width},{Height})";

        public Size ToSize() => new Size(Width, Height);
    }

    public class XAMLSizeStringConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
                return true;
            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string stringValue)
            {
                int[] values = stringValue.Split(',').Select(x => int.Parse(x)).ToArray();
                return values.Length switch
                {
                    0 => new XAMLSize(0),
                    1 => new XAMLSize(values[0]),
                    2 => new XAMLSize(values[0], values[1]),
                    _ => throw new ArgumentException(stringValue)
                };
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
}
