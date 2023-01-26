using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SizeF = System.Drawing.SizeF;
using MonoGame.Extended;

namespace MGUI.Shared.Helpers
{
    public static class SizeUtils
    {
        public static Size Add(this Size @this, Size other, int MinWidth, int MinHeight)
            => new(Math.Max(MinWidth, @this.Width + other.Width), Math.Max(MinHeight, @this.Height + other.Height));

        public static Size Subtract(this Size @this, Size other, int MinWidth, int MinHeight)
            => new(Math.Max(MinWidth, @this.Width - other.Width), Math.Max(MinHeight, @this.Height - other.Height));

        /// <summary>Makes the sign of the width and height be positive via Math.Abs(...)</summary>
        public static Size AsPositive(this Size @this) => new(Math.Abs(@this.Width), Math.Abs(@this.Height));
        public static Size AsZeroOrGreater(this Size @this) => new(Math.Max(0, @this.Width), Math.Max(0, @this.Height));

        /// <summary>Clamps this <see cref="Size"/> to fit its Width and Height into the inclusive range specified by <paramref name="Min"/> and <paramref name="Max"/></summary>
        public static Size Clamp(this Size @this, Size Min, Size Max) =>
            new(Math.Clamp(@this.Width, Min.Width, Max.Width), Math.Clamp(@this.Height, Min.Height, Max.Height));

        public static SizeF Add(this SizeF @this, SizeF other, float MinWidth, float MinHeight)
            => new(Math.Max(MinWidth, @this.Width + other.Width), Math.Max(MinHeight,@this.Height + other.Height));

        public static SizeF Subtract(this SizeF @this, SizeF other, float MinWidth, float MinHeight)
            => new(Math.Max(MinWidth, @this.Width - other.Width), Math.Max(MinHeight, @this.Height - other.Height));

        /// <summary>Makes the sign of the width and height be positive via Math.Abs(...)</summary>
        public static SizeF AsPositive(this SizeF @this) => new(Math.Abs(@this.Width), Math.Abs(@this.Height));
        public static SizeF AsZeroOrGreater(this SizeF @this) => new(Math.Max(0, @this.Width), Math.Max(0, @this.Height));

        /// <summary>Clamps this <see cref="SizeF"/> to fit its Width and Height into the inclusive range specified by <paramref name="Min"/> and <paramref name="Max"/></summary>
        public static SizeF Clamp(this SizeF @this, SizeF Min, SizeF Max) => 
            new(Math.Clamp(@this.Width, Min.Width, Max.Width), Math.Clamp(@this.Height, Min.Height, Max.Height));
    }
}
