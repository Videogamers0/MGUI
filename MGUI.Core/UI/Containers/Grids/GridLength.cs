using MGUI.Core.UI.XAML;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MGUI.Core.UI.Containers.Grids
{
    /// <summary>Describes the kind of value that a <see cref="GridLength"/> object is holding.</summary>
    public enum GridUnitType
    {
        /// <summary>The size is determined by the size properties of the content object.</summary>
        Auto = 0,
        /// <summary>The value is expressed as a pixel.</summary>
        Pixel = 1,
        /// <summary>The value is expressed as a weighted proportion of available space.</summary>
        Weighted = 2
    }

    /// <summary>To instantiate this struct, use:<para/>
    /// <see cref="Auto"/><br/>
    /// <see cref="CreatePixelLength(int)"/><br/>
    /// <see cref="CreateWeightedLength(double)"/>.<para/>
    /// <see cref="Parse(string)"/><br/>
    /// <see cref="ParseMultiple(string)"/></summary>
    [TypeConverter(typeof(GridLengthStringConverter))]
    public struct GridLength : IEquatable<GridLength>
    {
        public readonly GridUnitType UnitType;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsPixelLength => UnitType == GridUnitType.Pixel;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsAutoLength => UnitType == GridUnitType.Auto;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsWeightedLength => UnitType == GridUnitType.Weighted;

        private readonly int? _Pixels;
        /// <summary>Only valid if <see cref="IsPixelLength"/> is true</summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int Pixels => IsPixelLength ? _Pixels.Value : throw new InvalidOperationException($"{nameof(GridLength)}.{nameof(Pixels)} is only available when {nameof(UnitType)} equals {nameof(GridUnitType)}.{nameof(GridUnitType.Pixel)}");

        private readonly double? _Weight;
        /// <summary>Only valid if <see cref="IsWeightedLength"/> is true</summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public double Weight => IsWeightedLength ? _Weight.Value : throw new InvalidOperationException($"{nameof(GridLength)}.{nameof(Weight)} is only available when {nameof(UnitType)} equals {nameof(GridUnitType)}.{nameof(GridUnitType.Weighted)}");

        private GridLength(GridUnitType UnitType, int? Pixels, double? Weight)
        {
            this.UnitType = UnitType;
            _Pixels = Pixels;
            _Weight = Weight;

            if (IsPixelLength && !Pixels.HasValue)
                throw new ArgumentNullException(nameof(Pixels));
            else if (IsWeightedLength && !Weight.HasValue)
                throw new ArgumentNullException(nameof(Weight));
        }

        public static readonly GridLength Auto = new(GridUnitType.Auto, null, null);
        public static GridLength CreatePixelLength(int Pixels) => new(GridUnitType.Pixel, Pixels, null);
        public static GridLength CreateWeightedLength(double Weight) => new(GridUnitType.Weighted, null, Weight);

        public static bool operator ==(GridLength gl1, GridLength gl2)
            => gl1.UnitType == gl2.UnitType && gl1._Pixels == gl2._Pixels && gl1._Weight == gl2._Weight;
        public static bool operator !=(GridLength gl1, GridLength gl2)
            => gl1.UnitType != gl2.UnitType || gl1._Pixels != gl2._Pixels || gl1._Weight != gl2._Weight;

        override public bool Equals(object other)
        {
            if (other is GridLength gl)
                return this == gl;
            else
                return false;
        }
        public bool Equals(GridLength other) => this == other;

        public override int GetHashCode() => (UnitType, _Pixels ?? _Weight ?? 0).GetHashCode();

        public override string ToString()
            => UnitType switch
            {
                GridUnitType.Auto => "Auto",
                GridUnitType.Pixel => $"{Pixels}px",
                GridUnitType.Weighted => $"{Weight:0.##}*",
                _ => throw new NotImplementedException($"Unrecognized {nameof(GridUnitType)}: {UnitType}")
            };

        private const string AutoLengthPattern = @"(?<AutoLength>(?i)auto(?-i))";
        private const string PixelLengthPattern = @"(?<PixelLength>(?<PixelValue>\d+)((?i)px(?-i))?)";
        private const string WeightedLengthPattern = @"(?<WeightedLength>(?<WeightValue>(\d+(\.\d+)?)?)\*)";
        private const string DimensionLengthPattern = $@"({AutoLengthPattern}|{WeightedLengthPattern}|{PixelLengthPattern})";

        /// <summary>A <see cref="Regex"/> that parses <see cref="GridLength"/>s from strings.<br/>
        /// Does not include start of string ('^') or end of string ('$') regex anchors.<para/>
        /// See also: <see cref="AnchoredParser"/><para/>
        /// EX: "Auto,16px,25,*,1.5*,Auto" would contain 6 Matches</summary>
        public static readonly Regex UnanchoredParser = new(DimensionLengthPattern);
        /// <summary>A <see cref="Regex"/> that parses <see cref="GridLength"/>s from strings.<br/>
        /// Include start of string ('^') and end of string ('$') regex anchors.<para/>
        /// See also: <see cref="UnanchoredParser"/><para/>
        /// EX: "1.5*" would result in a weighted <see cref="GridLength"/> with Weight=1.5.<br/>
        /// But "1.5*,1.2*" would not match because the start of string and end of string anchors only allow 1 <see cref="GridLength"/> match within the string.</summary>
        public static readonly Regex AnchoredParser = new($@"^{DimensionLengthPattern}$");

        /// <param name="Length">To represent a <see cref="GridUnitType.Auto"/> dimension: "Auto" (case-insensitive)<br/>
        /// To represent a <see cref="GridUnitType.Pixel"/> dimension: A positive integral value. EX: "50"<br/>
        /// To represent a <see cref="GridUnitType.Weighted"/> dimension: A positive numeric value, suffixed with '*'. EX: "1.5*". If no numeric value is present, it is assumed to be "1*"</param>
        public static GridLength Parse(string Length)
        {
            Match Match = AnchoredParser.Match(Length);
            if (Match.Groups["AutoLength"].Success)
            {
                return Auto;
            }
            else if (Match.Groups["PixelLength"].Success)
            {
                string PixelValueString = Match.Groups["PixelValue"].Value;
                int Pixels = int.Parse(PixelValueString);
                return CreatePixelLength(Pixels);
            }
            else if (Match.Groups["WeightedLength"].Success)
            {
                string WeightValueString = Match.Groups["WeightValue"].Value;
                double Weight = WeightValueString == string.Empty ? 1.0 : double.Parse(WeightValueString);
                return CreateWeightedLength(Weight);
            }
            else
                throw new NotImplementedException($"Unrecognized {nameof(GridLength)} value: {Length}");
        }

        /// <param name="CommaSeparatedValues">A comma-separated list of dimensions, where each value is either:<para/>
        /// To represent a <see cref="GridUnitType.Auto"/> dimension: "Auto" (case-insensitive)<br/>
        /// To represent a <see cref="GridUnitType.Pixel"/> dimension: A positive integral value. EX: "50"<br/>
        /// To represent a <see cref="GridUnitType.Weighted"/> dimension: A positive numeric value, suffixed with '*'. EX: "1.5*". If no numeric value is present, it is assumed to be "1*"<para/>
        /// EX: "1.25*,1*,200,Auto" would yield 4 <see cref="GridLength"/>s.</param>
        public static IEnumerable<GridLength> ParseMultiple(string CommaSeparatedValues)
        {
            if (!string.IsNullOrEmpty(CommaSeparatedValues))
            {
                foreach (string Item in CommaSeparatedValues.Split(','))
                    yield return Parse(Item);
            }
        }
    }

    public class GridLengthStringConverter : TypeConverter
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
                GridLength Length = GridLength.Parse(stringValue);
                return Length;
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
}
