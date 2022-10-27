using MGUI.Core.UI.XAML;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
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

        /// <param name="Dimensions">To represent a <see cref="GridUnitType.Auto"/> dimension: "Auto" (case-insensitive)<br/>
        /// To represent a <see cref="GridUnitType.Pixel"/> dimension: A positive integral value. EX: "50"<br/>
        /// To represent a <see cref="GridUnitType.Weighted"/> dimension: A positive numeric value, suffixed with '*'. EX: "1.5*". If no numeric value is present, it is assumed to be "1*"</param>
        public static GridLength Parse(string Dimensions)
        {
            if (Dimensions.Equals("Auto", StringComparison.CurrentCultureIgnoreCase))
                return Auto;
            else if (Dimensions.EndsWith('*'))
            {
                double Weight = Dimensions.Length == 1 ? 1.0 : double.Parse(Dimensions[..^1]);
                return CreateWeightedLength(Weight);
            }
            else
            {
                int Pixels = int.Parse(Dimensions);
                return CreatePixelLength(Pixels);
            }
        }

        /// <param name="CommaSeparatedDimensions">A comma-separated list of dimensions, where each value is either:<para/>
        /// To represent a <see cref="GridUnitType.Auto"/> dimension: "Auto" (case-insensitive)<br/>
        /// To represent a <see cref="GridUnitType.Pixel"/> dimension: A positive integral value. EX: "50"<br/>
        /// To represent a <see cref="GridUnitType.Weighted"/> dimension: A positive numeric value, suffixed with '*'. EX: "1.5*". If no numeric value is present, it is assumed to be "1*"<para/>
        /// EX: "1.25*,1*,200,Auto" would yield 4 <see cref="GridLength"/>s.</param>
        public static IEnumerable<GridLength> ParseMultiple(string CommaSeparatedDimensions)
        {
            if (!string.IsNullOrEmpty(CommaSeparatedDimensions))
            {
                foreach (string Item in CommaSeparatedDimensions.Split(','))
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
