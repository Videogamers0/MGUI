using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI.XAML
{
    [TypeConverter(typeof(RectangleStringConverter))]
    public struct XAMLRectangle
    {
        public int Left { get; set; }
        public int Top { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public XAMLRectangle() : this(0, 0, 0, 0) { }
        public XAMLRectangle(int Width, int Height) : this(0, 0, Width, Height) { }

        public XAMLRectangle(int Left, int Top, int Width, int Height)
        {
            this.Left = Left;
            this.Top = Top;
            this.Width = Width;
            this.Height = Height;
        }

        public override string ToString() => $"{nameof(XAMLRectangle)}: ({Left},{Top},{Width},{Height})";

        public Microsoft.Xna.Framework.Rectangle ToRectangle() => new Microsoft.Xna.Framework.Rectangle(Left, Top, Width, Height);
    }

    public class RectangleStringConverter : TypeConverter
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
                    0 => new XAMLRectangle(),
                    2 => new Thickness(values[0], values[1]),
                    4 => new Thickness(values[0], values[1], values[2], values[3]),
                    _ => throw new ArgumentException(stringValue)
                };
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
}
