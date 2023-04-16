using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XNAColor = Microsoft.Xna.Framework.Color;

#if UseWPF
using System.Windows.Markup;
#else
using Portable.Xaml.Markup;
#endif

namespace MGUI.Core.UI.XAML
{
    #region Color
    [TypeConverter(typeof(ColorStringConverter))]
    public struct XAMLColor
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte A { get; set; }

        public XAMLColor() : this(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue) { }
        public XAMLColor(XNAColor Color) : this(Color.R, Color.G, Color.B, Color.A) { }
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

        public XNAColor ToXNAColor() => new(R, G, B, A);
    }

    public class ColorStringConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            => value is string stringValue ? ParseColor(stringValue) : base.ConvertFrom(context, culture, value);
        public static XAMLColor ParseColor(string Value)
            => new(XNAColorStringConverter.ParseColor(Value));
    }
    #endregion Color

    #region Fill Brush
    [TypeConverter(typeof(FillBrushStringConverter))]
    public abstract class FillBrush
    {
        public abstract IFillBrush ToFillBrush(MGDesktop Desktop);
    }

    public class FillBrushStringConverter : TypeConverter
    {
        private readonly ColorStringConverter ColorStringConverter = new();
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            => ColorStringConverter.CanConvertFrom(context, sourceType) || base.CanConvertFrom(context, sourceType);
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string stringValue)
                return ParseFillBrush(stringValue);
            else
                return base.ConvertFrom(context, culture, value);
        }

        public static FillBrush ParseFillBrush(string Value)
        {
            string[] colorStrings = Value.Split('|');
            if (colorStrings.Length == 1)
            {
                XAMLColor Color = ColorStringConverter.ParseColor(colorStrings[0]);
                return new SolidFillBrush(Color);
            }
            else if (colorStrings.Length == 2)
            {
                XAMLColor Color1 = ColorStringConverter.ParseColor(colorStrings[0]);
                XAMLColor Color2 = ColorStringConverter.ParseColor(colorStrings[1]);
                XNAColor Lerped = XNAColor.Lerp(Color1.ToXNAColor(), Color2.ToXNAColor(), 0.5f);
                XAMLColor Diagonals = new XAMLColor(Lerped.R, Lerped.G, Lerped.B, Lerped.A);
                return new GradientFillBrush(Color1, Diagonals, Color2, Diagonals);
            }
            else if (colorStrings.Length == 4)
            {
                XAMLColor[] Colors = colorStrings.Select(x => ColorStringConverter.ParseColor(x)).ToArray();
                return new GradientFillBrush(Colors[0], Colors[1], Colors[2], Colors[3]);
            }
            else
                throw new InvalidOperationException($"{Value} is not a valid format for a {nameof(FillBrush)}.");
        }
    }

    public class SolidFillBrush : FillBrush
    {
        public XAMLColor Color { get; set; }

        public SolidFillBrush() : this(new XAMLColor()) { }
        public SolidFillBrush(XAMLColor Color)
        {
            this.Color = Color;
        }

        public override string ToString() => $"{nameof(SolidFillBrush)}: {Color}";

        public override IFillBrush ToFillBrush(MGDesktop Desktop) => new MGSolidFillBrush(Color.ToXNAColor());
    }

    public class GradientFillBrush : FillBrush
    {
        public XAMLColor TopLeftColor { get; set; }
        public XAMLColor TopRightColor { get; set; }
        public XAMLColor BottomLeftColor { get; set; }
        public XAMLColor BottomRightColor { get; set; }

        public GradientFillBrush() : this(new XAMLColor(), new XAMLColor(), new XAMLColor(), new XAMLColor()) { }
        public GradientFillBrush(XAMLColor TopLeft, XAMLColor TopRight, XAMLColor BottomRight, XAMLColor BottomLeft)
        {
            this.TopLeftColor = TopLeft;
            this.TopRightColor = TopRight;
            this.BottomRightColor = BottomRight;
            this.BottomLeftColor = BottomLeft;
        }

        public override string ToString() => $"{nameof(GradientFillBrush)}: {TopLeftColor} {TopRightColor} {BottomRightColor} {BottomLeftColor}";

        public override IFillBrush ToFillBrush(MGDesktop Desktop) => new MGGradientFillBrush(TopLeftColor.ToXNAColor(), TopRightColor.ToXNAColor(), BottomRightColor.ToXNAColor(), BottomLeftColor.ToXNAColor());
    }

    public class DiagonalGradientFillBrush : FillBrush
    {
        public XAMLColor Color1 { get; set; }
        public XAMLColor Color2 { get; set; }
        public CornerType Color1Position { get; set; } = CornerType.TopLeft;

        public DiagonalGradientFillBrush() : this(new XAMLColor(), new XAMLColor(), CornerType.TopLeft) { }
        public DiagonalGradientFillBrush(XAMLColor Color1, XAMLColor Color2, CornerType Color1Position)
        {
            this.Color1 = Color1;
            this.Color2 = Color2;
            this.Color1Position = Color1Position;
        }

        public override string ToString() => $"{nameof(DiagonalGradientFillBrush)}: {Color1} ({Color1Position}) / {Color2}";

        public override IFillBrush ToFillBrush(MGDesktop Desktop) => new MGDiagonalGradientFillBrush(Color1.ToXNAColor(), Color2.ToXNAColor(), Color1Position);
    }

    public class TextureFillBrush : FillBrush
    {
        public string SourceName { get; set; }
        public Stretch Stretch { get; set; } = Stretch.Fill;
        public XAMLColor Color { get; set; } = new XAMLColor();

        public TextureFillBrush() : this(null, Stretch.Fill, new XAMLColor()) { }
        public TextureFillBrush(string SourceName, Stretch Stretch, XAMLColor Color)
        {
            this.SourceName = SourceName;
            this.Stretch = Stretch;
            this.Color = Color;
        }

        public override string ToString() => $"{nameof(TextureFillBrush)}: {SourceName} ({Stretch})";

        public override IFillBrush ToFillBrush(MGDesktop Desktop) => new MGTextureFillBrush(Desktop, SourceName, Stretch, Color.ToXNAColor());
    }

    [ContentProperty(nameof(FillBrush))]
    public class BorderedFillBrush : FillBrush
    {
        public Thickness BorderThickness { get; set; }
        public BorderBrush BorderBrush { get; set; }
        public FillBrush FillBrush { get; set; }
        public bool PadFillBoundsByBorderThickness { get; set; } = false;

        public BorderedFillBrush() : this(new Thickness(0), null, null, false) { }
        public BorderedFillBrush(Thickness BorderThickness, BorderBrush BorderBrush, FillBrush FillBrush, bool PadFillBoundsByBorderThickness)
        {
            this.BorderThickness = BorderThickness;
            this.BorderBrush = BorderBrush;
            this.FillBrush = FillBrush;
            this.PadFillBoundsByBorderThickness = PadFillBoundsByBorderThickness;
        }

        public override string ToString() => $"{nameof(BorderedFillBrush)}: {BorderBrush} / {FillBrush}";

        public override IFillBrush ToFillBrush(MGDesktop Desktop) => new MGBorderedFillBrush(BorderThickness.ToThickness(), BorderBrush?.ToBorderBrush(Desktop), FillBrush?.ToFillBrush(Desktop), PadFillBoundsByBorderThickness);
    }

    [ContentProperty(nameof(Brushes))]
    public class CompositedFillBrush : FillBrush
    {
        public List<FillBrush> Brushes { get; set; } = new();

        public CompositedFillBrush() : this(new List<FillBrush>()) { }
        public CompositedFillBrush(IEnumerable<FillBrush> Brushes)
        {
            this.Brushes = Brushes.ToList();
        }

        public override string ToString() => $"{nameof(CompositedFillBrush)}: {Brushes.Count} brush(es)";

        public override IFillBrush ToFillBrush(MGDesktop Desktop) => new MGCompositedFillBrush(Brushes.Select(x => x.ToFillBrush(Desktop)).ToArray());
    }

    [ContentProperty(nameof(Brush))]
    public class PaddedFillBrush : FillBrush
    {
        public FillBrush Brush { get; set; }
        public Thickness Padding { get; set; }

        public float? Scale { get; set; }

        public int? MinWidth { get; set; }
        public int? MinHeight { get; set; }
        public int? MaxWidth { get; set; }
        public int? MaxHeight { get; set; }

        public HorizontalAlignment? HorizontalAlignment { get; set; }
        public VerticalAlignment? VerticalAlignment { get; set; }

        public PaddedFillBrush() : this(null, new(0), null, null, null, null, null, null, null) { }
        public PaddedFillBrush(FillBrush Brush, Thickness Padding, float? Scale, int? MinWidth, int? MinHeight, int? MaxWidth, int? MaxHeight, 
            HorizontalAlignment? HorizontalAlignment, VerticalAlignment? VerticalAlignment)
        {
            this.Brush = Brush;
            this.Padding = Padding;
            this.Scale = Scale;
            this.MinWidth = MinWidth;
            this.MinHeight = MinHeight;
            this.MaxWidth = MaxWidth;
            this.MaxHeight = MaxHeight;
            this.HorizontalAlignment = HorizontalAlignment;
            this.VerticalAlignment = VerticalAlignment;
        }

        public override string ToString() => $"{nameof(PaddedFillBrush)}: {Brush}";

        public override IFillBrush ToFillBrush(MGDesktop Desktop) => new MGPaddedFillBrush(Brush?.ToFillBrush(Desktop), Padding.ToThickness(), Scale, MinWidth, MinHeight, MaxWidth, MaxHeight, HorizontalAlignment, VerticalAlignment);
    }
    #endregion Fill Brush

    #region Border Brush
    [TypeConverter(typeof(BorderBrushStringConverter))]
    public abstract class BorderBrush
    {
        public abstract IBorderBrush ToBorderBrush(MGDesktop Desktop);
    }

    public class BorderBrushStringConverter : TypeConverter
    {
        private readonly FillBrushStringConverter FillBrushStringConverter = new();
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            => FillBrushStringConverter.CanConvertFrom(context, sourceType) || base.CanConvertFrom(context, sourceType);
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string stringValue)
            {
                string[] fillBrushStrings = stringValue.Split('-');
                if (fillBrushStrings.Length == 1)
                {
                    FillBrush FillBrush = (FillBrush)FillBrushStringConverter.ConvertFrom(context, culture, fillBrushStrings[0]);
                    return new UniformBorderBrush(FillBrush);
                }
                else if (fillBrushStrings.Length is 2 or 4)
                {
                    FillBrush[] FillBrushes = fillBrushStrings.Select(x => (FillBrush)FillBrushStringConverter.ConvertFrom(context, culture, x)).ToArray();
                    if (FillBrushes.Length == 2)
                        return new DockedBorderBrush(FillBrushes[0], FillBrushes[1], FillBrushes[1], FillBrushes[0]);
                    else
                        return new DockedBorderBrush(FillBrushes[0], FillBrushes[1], FillBrushes[2 % FillBrushes.Length], FillBrushes[3 % FillBrushes.Length]);
                }
            }

            return base.ConvertFrom(context, culture, value);
        }
    }

    [ContentProperty(nameof(Brush))]
    public class UniformBorderBrush : BorderBrush
    {
        public FillBrush Brush { get; set; }

        public UniformBorderBrush() : this(null) { }
        public UniformBorderBrush(FillBrush Brush)
        {
            this.Brush = Brush;
        }

        public override string ToString() => $"{nameof(UniformBorderBrush)}: {Brush}";

        public override IBorderBrush ToBorderBrush(MGDesktop Desktop) => new MGUniformBorderBrush(Brush.ToFillBrush(Desktop));
    }

    public class DockedBorderBrush : BorderBrush
    {
        public FillBrush Left { get; set; }
        public FillBrush Top { get; set; }
        public FillBrush Right { get; set; }
        public FillBrush Bottom { get; set; }

        public DockedBorderBrush() : this(null, null, null, null) { }
        public DockedBorderBrush(FillBrush Left, FillBrush Top, FillBrush Right, FillBrush Bottom)
        {
            this.Left = Left;
            this.Top = Top;
            this.Right = Right;
            this.Bottom = Bottom;
        }

        public override string ToString() => $"{nameof(DockedBorderBrush)}: {Left}, {Top}, {Right}, {Bottom}";

        public override IBorderBrush ToBorderBrush(MGDesktop Desktop) => new MGDockedBorderBrush(Left.ToFillBrush(Desktop), Top.ToFillBrush(Desktop), Right.ToFillBrush(Desktop), Bottom.ToFillBrush(Desktop));
    }

    [ContentProperty(nameof(Bands))]
    public class BandedBorderBrush : BorderBrush
    {
        public List<BorderBand> Bands { get; set; } = new();

        public BandedBorderBrush() : this(new List<BorderBand>()) { }
        public BandedBorderBrush(IList<BorderBand> Bands)
        {
            this.Bands = Bands.ToList();
        }

        public override string ToString() => $"{nameof(BandedBorderBrush)}: {Bands.Count} band(s)";

        public override IBorderBrush ToBorderBrush(MGDesktop Desktop) => new MGBandedBorderBrush(Bands.Select(x => x.ToBorderBand(Desktop)).ToArray());
    }

    [ContentProperty(nameof(Brush))]
    public class BorderBand
    {
        public BorderBrush Brush { get; set; } = new UniformBorderBrush(new SolidFillBrush(new XAMLColor(255, 255, 255, 0)));
        public double ThicknessWeight { get; set; } = 1.0;

        public MGBorderBand ToBorderBand(MGDesktop Desktop) => new(Brush.ToBorderBrush(Desktop), ThicknessWeight);
    }

    public class TexturedBorderBrush : BorderBrush
    {
        public string EdgeTextureName { get; set; }
        public XAMLColor? EdgeColor { get; set; }
        public string CornerTextureName { get; set; }
        public XAMLColor? CornerColor { get; set; }
        public float Opacity { get; set; } = 1.0f;

        public Edge? EdgeBasis { get; set; } = Edge.Left;
        public Corner? CornerBasis { get; set; } = Corner.TopLeft;
        private TextureTransforms Transforms => EdgeBasis.HasValue && CornerBasis.HasValue ? TextureTransforms.CreateStandardRotated(EdgeBasis.Value, CornerBasis.Value) : new();

        public TexturedBorderBrush() : this(null, null, null, null, 1.0f) { }
        public TexturedBorderBrush(string EdgeTextureName, XAMLColor? EdgeColor, string CornerTextureName, XAMLColor? CornerColor, float Opacity)
        {
            this.EdgeTextureName = EdgeTextureName;
            this.EdgeColor = EdgeColor;
            this.CornerTextureName = CornerTextureName;
            this.CornerColor = CornerColor;
            this.Opacity = Opacity;
        }

        public override string ToString() => $"{nameof(TexturedBorderBrush)}: {EdgeTextureName} / {CornerTextureName}";

        public override IBorderBrush ToBorderBrush(MGDesktop Desktop)
            => new MGTexturedBorderBrush(Desktop, EdgeTextureName, CornerTextureName, EdgeColor?.ToXNAColor(), CornerColor?.ToXNAColor(), Transforms, Opacity);
    }
    #endregion Border Brush
}
