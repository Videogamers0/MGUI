using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Shared.Helpers
{
    public static class VectorUtils
    {
        /*public static int XInt(this Vector2 @this) { return (int)@this.X; }
        public static int XIntFloor(this Vector2 @this) { return (int)Math.Floor(@this.X); }
        public static int XIntCeiling(this Vector2 @this) { return (int)Math.Ceiling(@this.X); }
        public static int XIntRounded(this Vector2 @this, MidpointRounding mode) { return (int)Math.Round(@this.X, mode); }

        public static int YInt(this Vector2 @this) { return (int)@this.Y; }
        public static int YIntFloor(this Vector2 @this) { return (int)Math.Floor(@this.Y); }
        public static int YIntCeiling(this Vector2 @this) { return (int)Math.Ceiling(@this.Y); }
        public static int YIntRounded(this Vector2 @this, MidpointRounding mode) { return (int)Math.Round(@this.Y, mode); }

        public static bool IsIntVector(this Vector2 @this) => @this.X == @this.XInt() && @this.Y == @this.YInt();*/

        //Taken from: https://stackoverflow.com/questions/13458992/angle-between-two-vectors-2d
        /// <summary>AngleBetween - the angle between 2 vectors</summary>
        /// <returns>Returns the the angle in degrees between vector1 and vector2 (from -180 to 180)</returns>
        public static double AngleBetween(Vector2 v1, Vector2 v2)
        {
            double sin = v1.X * v2.Y - v2.X * v1.Y;
            double cos = v1.X * v2.X + v1.Y * v2.Y;
            return Math.Atan2(sin, cos) * (180 / Math.PI);
        }

        public static double CrossProduct(this Vector2 @this, Vector2 Other) => @this.X * Other.Y - @this.Y * Other.X;
        /// <summary>Returns an orthogonal <see cref="Vector2"/> by rotating this <see cref="Vector2"/> 90 degrees clockwise (right normal)<para/>
        /// See also: <see cref="LeftNormal(Vector2)"/>, <see cref="RightNormal(Vector2)"/></summary>
        public static Vector2 Perpendicular(this Vector2 @this) => @this.RightNormal();

        /// <summary>Returns an orthogonal <see cref="Vector2"/> by rotating this <see cref="Vector2"/> 90 degrees counter clockwise (left normal)</summary>
        public static Vector2 LeftNormal(this Vector2 @this) => new(-@this.Y, @this.X);
        /// <summary>Returns an orthogonal <see cref="Vector2"/> by rotating this <see cref="Vector2"/> 90 degrees clockwise (right normal)</summary>
        public static Vector2 RightNormal(this Vector2 @this) => new(@this.Y, -@this.X);

        public static bool IsAlmostZero(this Vector2 @this) => @this.X.IsAlmostZero() && @this.Y.IsAlmostZero();
        public static bool IsAlmostEqual(Vector2 v1, Vector2 v2, double Epsilon = 1e-9) => Math.Abs(v1.X - v2.X) <= Epsilon && Math.Abs(v1.Y - v2.Y) <= Epsilon;
        public static bool IsAlmostEqualTo(this Vector2 @this, Vector2 other, double Epsilon = 1e-9) => IsAlmostEqual(@this, other, Epsilon);
        /// <summary>Returns true if the two vectors point in almost the same direction. I.E. one vector is a scalar multiple of the other</summary>
        public static bool IsAlmostEqualDirection(this Vector2 @this, Vector2 other, double Epsilon = 1e-9) => Math.Abs(@this.CrossProduct(other)) <= Epsilon;

        /// <summary>Returns the distance from the given <paramref name="Point"/> to a line with the given <paramref name="LineDirection"/> and passing through the given <paramref name="LinePoint"/>.<para/>
        /// Returns a negative value if the <paramref name="Point"/> is below the line (I.E. on the side of the line that the <paramref name="Normal"/> does not point towards)</summary>
        /// <param name="Normal">A <see cref="Vector2"/> that is orthogonal to the <paramref name="LineDirection"/>, and indicates which side of the <paramref name="LineDirection"/> is 'above'.</param>
        public static double GetSignedDistance(Vector2 LineDirection, Vector2 LinePoint, Vector2 Normal, Vector2 Point)
        {
            //https://stackoverflow.com/questions/29444874/function-for-signed-distance-from-a-line-plane-with-a-front-vector
            LineDirection.Normalize();
            double Distance = LineDirection.CrossProduct(LinePoint - Point);
            if (Math.Abs(AngleBetween(LineDirection.LeftNormal(), Normal)) < Math.Abs(AngleBetween(LineDirection.RightNormal(), Normal)))
                Distance *= -1;
            return Distance;
        }

        public static Vector2 Negate(this Vector2 @this) => Vector2.Negate(@this);
        public static float DotProduct(this Vector2 @this, Vector2 Other) => Vector2.Dot(@this, Other);
        public static Vector2 Scale(this Vector2 @this, float ScaleFactor) => Vector2.Multiply(@this, ScaleFactor);
        public static Vector2 Multiply(this Vector2 @this, Vector2 Other) => Vector2.Multiply(@this, Other);
        public static Vector2 TransformBy(this Vector2 @this, Matrix Transform) => Vector2.Transform(@this, Transform);

        public static bool IsScalarMultipleOf(this Vector2 @this, Vector2 other) => @this.CrossProduct(other).IsAlmostZero();
        /*public static bool IsScalarMultipleOf(this Vector2 @this, Vector2 other)
        {
            //  (Avoid dividing by zero by checking for vertical/horizontal lines first)
            if (IsAlmostVertical(@this))
                return IsAlmostVertical(other);
            else if (IsAlmostHorizontal(@this))
                return IsAlmostHorizontal(other);
            //  Check if their components have the same ratio
            else
                return (@this.X / other.X).IsAlmostEqual(@this.Y / other.Y);
        }*/

        public static bool IsAlmostVertical(this Vector2 @this) => @this.X.IsAlmostZero();
        public static bool IsAlmostHorizontal(this Vector2 @this) => @this.Y.IsAlmostZero();

        public static Vector2 AsIntVector(this Vector2 @this) => new((int)@this.X, (int)@this.Y);

        //Adapted from: https://stackoverflow.com/a/2049593/11689514
        public static bool IsInTriangle(this Vector2 pt, Vector2 v1, Vector2 v2, Vector2 v3)
        {
            double d1 = (pt - v2).CrossProduct(v1 - v2);
            double d2 = (pt - v3).CrossProduct(v2 - v3);
            double d3 = (pt - v1).CrossProduct(v3 - v1);

            bool has_neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool has_pos = (d1 > 0) || (d2 > 0) || (d3 > 0);

            return !(has_neg && has_pos);
        }
    }
}
