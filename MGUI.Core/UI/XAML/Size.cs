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
    [TypeConverter(typeof(SizeStringConverter))]
    public struct Size
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public Size() : this(0, 0) { }
        public Size(int Both) : this(Both, Both) { }
        public Size(int Width, int Height)
        {
            this.Width = Width;
            this.Height = Height;
        }

        public override string ToString() => $"{nameof(Size)}: ({Width},{Height})";

        public MonoGame.Extended.Size ToSize() => new MonoGame.Extended.Size(Width, Height);
    }

    public class SizeStringConverter : TypeConverter
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
                    0 => new Size(0),
                    1 => new Size(values[0]),
                    2 => new Size(values[0], values[1]),
                    _ => throw new ArgumentException(stringValue)
                };
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
}
