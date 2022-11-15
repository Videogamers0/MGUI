using Microsoft.Xna.Framework;
using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DrawingColor = System.Drawing.Color;
using ColorTranslator = System.Drawing.ColorTranslator;
using System.Text.RegularExpressions;

namespace MGUI.Core.UI.XAML
{
    #region Color
    [TypeConverter(typeof(XAMLColorStringConverter))]
    public struct XAMLColor
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte A { get; set; }

        public XAMLColor() : this(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue) { }
        public XAMLColor(byte R, byte G, byte B) : this(R, G, B, byte.MaxValue) { }
        public XAMLColor(byte R, byte G, byte B, byte A)
        {
            this.R = R;
            this.G = G;
            this.B = B;
            this.A = A;
        }

        public static XAMLColor operator *(XAMLColor value, float scale) =>
            new((byte)(value.R * scale), (byte)(value.G * scale), (byte)(value.B * scale), (byte)(value.A * scale));
        public static XAMLColor operator *(float scale, XAMLColor value) =>
            new((byte)(value.R * scale), (byte)(value.G * scale), (byte)(value.B * scale), (byte)(value.A * scale));

        public override string ToString() => $"({R},{G},{B}|{A})";

        public Color ToXNAColor() => new(R, G, B, A);
    }

    public class XAMLColorStringConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
                return true;
            return base.CanConvertFrom(context, sourceType);
        }

        private static readonly Regex RGBColorRegex = 
            new($@"^(?i)RGBA?(?-i)\((?<RedComponent>\d{{1,3}}), ?(?<GreenComponent>\d{{1,3}}), ?(?<BlueComponent>\d{{1,3}})(, ?(?<AlphaComponent>\d{{1,3}}))?\)$"); // EX: "RGB(0,128,255)"

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string stringValue)
            {
                DrawingColor ParseColor(string colorName)
                {
                    Match RGBMatch = RGBColorRegex.Match(colorName);
                    if (RGBMatch.Success)
                    {
                        int r = int.Parse(RGBMatch.Groups["RedComponent"].Value);
                        int g = int.Parse(RGBMatch.Groups["GreenComponent"].Value);
                        int b = int.Parse(RGBMatch.Groups["BlueComponent"].Value);
                        int a = RGBMatch.Groups["AlphaComponent"].Success ? int.Parse(RGBMatch.Groups["AlphaComponent"].Value) : byte.MaxValue;
                        return DrawingColor.FromArgb(a, r, g, b);
                    }
                    else
                    {
                        return ColorTranslator.FromHtml(colorName);
                    }
                }

                int asteriskIndex = stringValue.IndexOf('*');
                if (asteriskIndex > 0)
                {
                    string colorName = stringValue.Substring(0, asteriskIndex).Trim();
                    DrawingColor color = ParseColor(colorName);
                    string opacityScalarString = stringValue.Substring(asteriskIndex + 1).Trim();
                    float opacityScalar = float.Parse(opacityScalarString);

                    return new XAMLColor(color.R, color.G, color.B, color.A) * opacityScalar;
                }
                else
                {
                    DrawingColor color = ParseColor(stringValue);
                    return new XAMLColor(color.R, color.G, color.B, color.A);
                }
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
    #endregion Color

    #region Fill Brush
    [TypeConverter(typeof(XAMLFillBrushStringConverter))]
    public abstract class XAMLFillBrush
    {
        public abstract IFillBrush ToFillBrush();
    }

    public class XAMLFillBrushStringConverter : TypeConverter
    {
        private readonly XAMLColorStringConverter ColorStringConverter = new();
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            => ColorStringConverter.CanConvertFrom(context, sourceType) || base.CanConvertFrom(context, sourceType);
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string stringValue)
            {
                string[] colorStrings = stringValue.Split('|');
                if (colorStrings.Length == 1)
                {
                    XAMLColor Color = (XAMLColor)ColorStringConverter.ConvertFrom(context, culture, colorStrings[0]);
                    return new XAMLSolidFillBrush(Color);
                }
                else if (colorStrings.Length == 2)
                {
                    XAMLColor Color1 = (XAMLColor)ColorStringConverter.ConvertFrom(context, culture, colorStrings[0]);
                    XAMLColor Color2 = (XAMLColor)ColorStringConverter.ConvertFrom(context, culture, colorStrings[1]);
                    Color Lerped = Color.Lerp(Color1.ToXNAColor(), Color2.ToXNAColor(), 0.5f);
                    XAMLColor Diagonals = new XAMLColor(Lerped.R, Lerped.G, Lerped.B, Lerped.A);
                    return new XAMLGradientFillBrush(Color1, Diagonals, Color2, Diagonals);
                }
                else if (colorStrings.Length == 4)
                {
                    XAMLColor[] Colors = colorStrings.Select(x => (XAMLColor)ColorStringConverter.ConvertFrom(context, culture, x)).ToArray();
                    return new XAMLGradientFillBrush(Colors[0], Colors[1], Colors[2], Colors[3]);
                }
            }

            return base.ConvertFrom(context, culture, value);
        }
    }

    public class XAMLSolidFillBrush : XAMLFillBrush
    {
        public XAMLColor Color { get; set; }

        public XAMLSolidFillBrush() : this(new XAMLColor()) { }
        public XAMLSolidFillBrush(XAMLColor Color)
        {
            this.Color = Color;
        }

        public override string ToString() => $"{nameof(XAMLSolidFillBrush)}: {Color}";

        public override IFillBrush ToFillBrush() => new MGSolidFillBrush(Color.ToXNAColor());
    }

    public class XAMLGradientFillBrush : XAMLFillBrush
    {
        public XAMLColor TopLeftColor { get; set; }
        public XAMLColor TopRightColor { get; set; }
        public XAMLColor BottomLeftColor { get; set; }
        public XAMLColor BottomRightColor { get; set; }

        public XAMLGradientFillBrush() : this(new XAMLColor(), new XAMLColor(), new XAMLColor(), new XAMLColor()) { }
        public XAMLGradientFillBrush(XAMLColor TopLeft, XAMLColor TopRight, XAMLColor BottomRight, XAMLColor BottomLeft)
        {
            this.TopLeftColor = TopLeft;
            this.TopRightColor = TopRight;
            this.BottomRightColor = BottomRight;
            this.BottomLeftColor = BottomLeft;
        }

        public override string ToString() => $"{nameof(XAMLGradientFillBrush)}: {TopLeftColor} {TopRightColor} {BottomRightColor} {BottomLeftColor}";

        public override IFillBrush ToFillBrush() => new MGGradientFillBrush(TopLeftColor.ToXNAColor(), TopRightColor.ToXNAColor(), BottomRightColor.ToXNAColor(), BottomLeftColor.ToXNAColor());
    }
    #endregion Fill Brush

    #region Border Brush
    [TypeConverter(typeof(XAMLBorderBrushStringConverter))]
    public abstract class XAMLBorderBrush
    {
        public abstract IBorderBrush ToBorderBrush();
    }

    public class XAMLBorderBrushStringConverter : TypeConverter
    {
        private readonly XAMLFillBrushStringConverter FillBrushStringConverter = new();
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            => FillBrushStringConverter.CanConvertFrom(context, sourceType) || base.CanConvertFrom(context, sourceType);
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string stringValue)
            {
                string[] fillBrushStrings = stringValue.Split('-');
                if (fillBrushStrings.Length == 1)
                {
                    XAMLFillBrush FillBrush = (XAMLFillBrush)FillBrushStringConverter.ConvertFrom(context, culture, fillBrushStrings[0]);
                    return new XAMLUniformBorderBrush(FillBrush);
                }
                else if (fillBrushStrings.Length == 4)
                {
                    XAMLFillBrush[] FillBrushes = fillBrushStrings.Select(x => (XAMLFillBrush)FillBrushStringConverter.ConvertFrom(context, culture, x)).ToArray();
                    return new XAMLDockedBorderBrush(FillBrushes[0], FillBrushes[1], FillBrushes[2], FillBrushes[3]);
                }
            }

            return base.ConvertFrom(context, culture, value);
        }
    }

    public class XAMLUniformBorderBrush : XAMLBorderBrush
    {
        public XAMLFillBrush Brush { get; set; }

        public XAMLUniformBorderBrush() : this(null) { }
        public XAMLUniformBorderBrush(XAMLFillBrush Brush)
        {
            this.Brush = Brush;
        }

        public override string ToString() => $"{nameof(XAMLUniformBorderBrush)}: {Brush}";

        public override IBorderBrush ToBorderBrush() => new MGUniformBorderBrush(Brush.ToFillBrush());
    }

    public class XAMLDockedBorderBrush : XAMLBorderBrush
    {
        public XAMLFillBrush Left { get; set; }
        public XAMLFillBrush Top { get; set; }
        public XAMLFillBrush Right { get; set; }
        public XAMLFillBrush Bottom { get; set; }

        public XAMLDockedBorderBrush() : this(null, null, null, null) { }
        public XAMLDockedBorderBrush(XAMLFillBrush Left, XAMLFillBrush Top, XAMLFillBrush Right, XAMLFillBrush Bottom)
        {
            this.Left = Left;
            this.Top = Top;
            this.Right = Right;
            this.Bottom = Bottom;
        }


        public override string ToString() => $"{nameof(XAMLDockedBorderBrush)}: {Left}, {Top}, {Right}, {Bottom}";

        public override IBorderBrush ToBorderBrush() => new MGDockedBorderBrush(Left.ToFillBrush(), Top.ToFillBrush(), Right.ToFillBrush(), Bottom.ToFillBrush());
    }
    #endregion Border Brush
}
