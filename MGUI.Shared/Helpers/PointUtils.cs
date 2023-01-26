using Microsoft.Xna.Framework;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Shared.Helpers
{
    public static class PointUtils
    {
        /// <summary>Returns a <see cref="Rectangle"/> where the top-left corner is at this <see cref="Point"/> and with the given dimensions.</summary>
        public static Rectangle ToRectangle(this Point @this, int Size)
            => new Rectangle(@this.X, @this.Y, Size, Size);

        /// <summary>Returns a <see cref="Rectangle"/> where the top-left corner is at this <see cref="Point"/> and with the given dimensions.</summary>
        public static Rectangle ToRectangle(this Point @this, int Width, int Height)
            => new Rectangle(@this.X, @this.Y, Width, Height);

        public static double DistanceTo(this Point @this, Point Other)
            => Math.Sqrt((@this.X - Other.X) * (@this.X - Other.X) + (@this.Y - Other.Y) * (@this.Y - Other.Y));

        public static double DistanceToSquared(this Point @this, Point Other)
            => (@this.X - Other.X) * (@this.X - Other.X) + (@this.Y - Other.Y) * (@this.Y - Other.Y);

        public static Size AsSize(this Point @this) => new(@this.X, @this.Y);

        public static Point Negate(this Point @this) => new(-@this.X, -@this.Y);

        public static Point AsPositive(this Point @this) => new(Math.Abs(@this.X), Math.Abs(@this.Y));
    }
}
