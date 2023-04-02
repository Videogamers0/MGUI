using Microsoft.Xna.Framework;
using MGUI.Core.UI.Brushes.Border_Brushes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MGUI.Shared.Helpers;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using DrawingColor = System.Drawing.Color;
using ColorTranslator = System.Drawing.ColorTranslator;

namespace MGUI.Core.UI.Brushes.Fill_Brushes
{
    /// <summary>See also:<para/>
    /// <see cref="MGSolidFillBrush"/><br/>
    /// <see cref="MGCompositedFillBrush"/><br/>
    /// <see cref="MGTextureFillBrush"/><br/>
    /// <see cref="MGGradientFillBrush"/><br/>
    /// <see cref="MGDiagonalGradientFillBrush"/><br/>
    /// <see cref="MGPaddedFillBrush"/><br/>
    /// <see cref="MGBorderedFillBrush"/><br/>
    /// <see cref="MGProgressBarGradientBrush"/></summary>
    [TypeConverter(typeof(IFillBrushStringConverter))]
    public interface IFillBrush : ICloneable
    {
        public void Draw(ElementDrawArgs DA, MGElement Element, Rectangle Bounds);
        public MGUniformBorderBrush AsUniformBorderBrush() => new(this);

        /// <summary>Attempts to darken this <see cref="IFillBrush"/>'s color by the given <paramref name="ShadowIntensity"/> if it is a <see cref="MGSolidFillBrush"/></summary>
        public bool TryDarken(float ShadowIntensity, out IFillBrush Result)
        {
            if (this is MGSolidFillBrush SolidFillBrush)
            {
                Color CurrentColor = SolidFillBrush.Color;
                Color NewColor = CurrentColor.Darken(ShadowIntensity);
                Result = NewColor.AsFillBrush();
                return true;
            }
            else
            {
                Result = null;
                return false;
            }
        }

        public IFillBrush Copy();
        object ICloneable.Clone() => Copy();
    }

    public class IFillBrushStringConverter : TypeConverter
    {
        private readonly XNAColorStringConverter ColorStringConverter = new();
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            => ColorStringConverter.CanConvertFrom(context, sourceType) || base.CanConvertFrom(context, sourceType);
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string stringValue)
                return ParseFillBrush(stringValue);
            else
                return base.ConvertFrom(context, culture, value);
        }

        public static IFillBrush ParseFillBrush(string Value)
        {
            string[] colorStrings = Value.Split('|');
            if (colorStrings.Length == 1)
            {
                Color Color = XNAColorStringConverter.ParseColor(colorStrings[0]);
                return new MGSolidFillBrush(Color);
            }
            else if (colorStrings.Length == 2)
            {
                Color Color1 = XNAColorStringConverter.ParseColor(colorStrings[0]);
                Color Color2 = XNAColorStringConverter.ParseColor(colorStrings[1]);
                Color Lerped = Color.Lerp(Color1, Color2, 0.5f);
                Color Diagonals = Lerped;
                return new MGGradientFillBrush(Color1, Diagonals, Color2, Diagonals);
            }
            else if (colorStrings.Length == 4)
            {
                Color[] Colors = colorStrings.Select(x => XNAColorStringConverter.ParseColor(x)).ToArray();
                return new MGGradientFillBrush(Colors[0], Colors[1], Colors[2], Colors[3]);
            }
            else
                throw new InvalidOperationException($"{Value} is not a valid format for a {nameof(IFillBrush)}.");
        }
    }

    public class XNAColorStringConverter : TypeConverter
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
                return ParseColor(stringValue);
            else
                return base.ConvertFrom(context, culture, value);
        }

        public static Color ParseColor(string Value)
        {
            static DrawingColor ParseDrawingColor(string colorName)
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
                //  Special case because System.Drawing.Color.Transparent [rgb(255,255,255,0)] is NOT the same as Microsoft.Xna.Framework.Color.Transparent [rgb(0,0,0,0)]
                //  Drawing XNA color=(255,255,255,0) will render White, whereas drawing XNA color=(0,0,0,0) draws nothing.
                else if (colorName.Equals("transparent", StringComparison.CurrentCultureIgnoreCase))
                {
                    return DrawingColor.FromArgb(0, 0, 0, 0);
                }
                else
                {
                    return ColorTranslator.FromHtml(colorName);
                }
            }

            int asteriskIndex = Value.IndexOf('*');
            if (asteriskIndex > 0)
            {
                string colorName = Value.Substring(0, asteriskIndex).Trim();
                DrawingColor color = ParseDrawingColor(colorName);
                string opacityScalarString = Value.Substring(asteriskIndex + 1).Trim();
                if (opacityScalarString.EndsWith("f", StringComparison.CurrentCultureIgnoreCase))
                    opacityScalarString = opacityScalarString[..^1];
                float opacityScalar = float.Parse(opacityScalarString);

                return new Color(color.R, color.G, color.B, color.A) * opacityScalar;
            }
            else
            {
                DrawingColor color = ParseDrawingColor(Value);
                return new Color(color.R, color.G, color.B, color.A);
            }
        }
    }
}
