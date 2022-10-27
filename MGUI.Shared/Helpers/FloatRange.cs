using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Shared.Helpers
{
    public struct FloatRange : IEquatable<FloatRange>
    {
        public readonly float Minimum;
        public readonly float Maximum;

        /// <summary>You may want to add 1 to this value, such as when working with screen pixels<para/>
        /// (I.E. if a range goes from 5 to 5, it would usually be considered a length of 1 when working in screen space, as it occupies 1 pixel. But this function would treat it as length=0)</summary>
        public float GetLength() => Maximum - Minimum;

        public FloatRange(float Minimum, float Maximum)
        {
            this.Minimum = Minimum;
            this.Maximum = Maximum;
        }

        public override string ToString() => $"{nameof(FloatRange)}: [{Minimum:0.####}-{Maximum:0.####}]";

        /// <summary>Computes the distance between this <see cref="FloatRange"/> and the <paramref name="Other"/> <see cref="FloatRange"/></summary>
        /// <returns>A negative value if the ranges overlap. Positive value if they don't overlap. Zero if they are touching along the edge.</returns>
        public float SignedDistance(FloatRange Other)
        {
            if (this.Minimum < Other.Minimum)
                return Other.Minimum - this.Maximum;
            else
                return this.Minimum - Other.Maximum;
        }

        public bool Intersects(FloatRange Other, out bool IsTouching, bool TreatTouchingAsIntersection = true) => Intersects(this, Other, out IsTouching, TreatTouchingAsIntersection);

        //3 ways to represent the same logic:
        //      If treating touching as an intersection...
        //          !(A.Min - B.Max > 0 || B.Min - A.Max > 0)
        //          !(A.Max < B.Min || B.Max < A.Min)
        //          !(A.Min > B.Max || B.Min > A.Max)
        //      If not treating touching as an intersection...
        //          !(A.Min - B.Max >= 0 || B.Min - A.Max >= 0)
        //          !(A.Max <= B.Min || B.Max <= A.Min)
        //          !(A.Min >= B.Max || B.Min >= A.Max)
        public static bool Intersects(FloatRange A, FloatRange B, out bool IsTouching, bool TreatTouchingAsIntersection = true)
        {
            IsTouching = A.Maximum == B.Minimum || B.Maximum == A.Minimum;
            if (TreatTouchingAsIntersection)
                return !(A.Maximum < B.Minimum || B.Maximum < A.Minimum);
            else
                return !(A.Maximum <= B.Minimum || B.Maximum <= A.Minimum);
        }

        public FloatRange Union(FloatRange Other) => new(Math.Min(this.Minimum, Other.Minimum), Math.Max(this.Maximum, Other.Maximum));

        public static FloatRange operator +(FloatRange r) => r;
        public static FloatRange operator -(FloatRange r) => new(-r.Maximum, -r.Minimum);

        public static FloatRange operator +(FloatRange r1, float offset) => new(r1.Minimum + offset, r1.Maximum + offset);
        public static FloatRange operator -(FloatRange r1, float offset) => new(r1.Minimum - offset, r1.Maximum - offset);
        public static FloatRange operator *(FloatRange r1, float scalar) => new(r1.Minimum * scalar, r1.Maximum * scalar);
        public static FloatRange operator /(FloatRange r1, float scalar)
        {
            if (scalar == 0)
                throw new DivideByZeroException();
            return new(r1.Minimum / scalar, r1.Maximum / scalar);
        }

        public override bool Equals(object obj) => obj is FloatRange other && this.Equals(other);
        public bool Equals(FloatRange other) => Minimum.Equals(other.Minimum) && Maximum.Equals(other.Maximum);
        public override int GetHashCode() => (Minimum, Maximum).GetHashCode();
        public static bool operator ==(FloatRange left, FloatRange right) => left.Equals(right);
        public static bool operator !=(FloatRange left, FloatRange right) => !(left == right);
    }
}
