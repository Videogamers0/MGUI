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
    [TypeConverter(typeof(XAMLThicknessStringConverter))]
    public struct XAMLThickness
    {
        public int Left { get; set; }
        public int Top { get; set; }
        public int Right { get; set; }
        public int Bottom { get; set; }

        public XAMLThickness() : this(0, 0, 0, 0) { }
        public XAMLThickness(int All) : this(All, All, All, All) { }
        public XAMLThickness(int LeftRight, int TopBottom) : this(LeftRight, TopBottom, LeftRight, TopBottom) { }

        public XAMLThickness(int Left, int Top, int Right, int Bottom)
        {
            this.Left = Left;
            this.Top = Top;
            this.Right = Right;
            this.Bottom = Bottom;
        }

        public override string ToString() => $"{nameof(XAMLThickness)}: ({Left},{Top},{Right},{Bottom})";

        public Thickness ToThickness() => new Thickness(Left, Top, Right, Bottom);
    }

    public class XAMLThicknessStringConverter : TypeConverter
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
                    0 => new XAMLThickness(0),
                    1 => new XAMLThickness(values[0]),
                    2 => new XAMLThickness(values[0], values[1]),
                    4 => new XAMLThickness(values[0], values[1], values[2], values[3]),
                    _ => throw new ArgumentException(stringValue)
                };
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
}
