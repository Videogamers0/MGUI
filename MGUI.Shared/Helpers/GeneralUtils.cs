using Microsoft.Xna.Framework;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Shared.Helpers
{
    public enum RelativePosition : byte
    {
        TopLeft = 0b_1000_0000,
        TopCenter = 0b_0100_0000,
        TopRight = 0b_0010_0000,
        MiddleLeft = 0b_0001_0000,
        MiddleCenter = 0b_1111_1111,
        MiddleRight = 0b_0000_1000,
        BottomLeft = 0b_0000_0100,
        BottomCenter = 0b_0000_0010,
        BottomRight = 0b_0000_0001
    }

    public static class GeneralUtils
    {
        public static bool IsAlmostEqual(this double d1, double d2, double Epsilon = 1e-12) => Math.Abs(d1 - d2) <= Epsilon;
        public static bool IsAlmostZero(this double value, double Epsilon = 1e-12) => value.IsAlmostEqual(0.0, Epsilon);
        public static bool IsGreaterThan(this double d1, double d2, double Epsilon = 1e-12) => d1 > d2 && !d1.IsAlmostEqual(d2, Epsilon);
        public static bool IsGreaterThanOrEqual(this double d1, double d2, double Epsilon = 1e-12) => d1 >= d2 || d1.IsAlmostEqual(d2, Epsilon);
        public static bool IsLessThan(this double d1, double d2, double Epsilon = 1e-12) => d1 < d2 && !d1.IsAlmostEqual(d2, Epsilon);
        public static bool IsLessThanOrEqual(this double d1, double d2, double Epsilon = 1e-12) => d1 <= d2 || d1.IsAlmostEqual(d2, Epsilon);

        public static bool IsAlmostEqual(this float f1, float f2, double Epsilon = 1e-9) => Math.Abs(f1 - f2) <= Epsilon;
        public static bool IsAlmostZero(this float value, double Epsilon = 1e-9) => value.IsAlmostEqual(0.0f, Epsilon);
        public static bool IsGreaterThan(this float f1, float f2, double Epsilon = 1e-9) => f1 > f2 && !f1.IsAlmostEqual(f2, Epsilon);
        public static bool IsGreaterThanOrEqual(this float f1, float f2, double Epsilon = 1e-9) => f1 >= f2 || f1.IsAlmostEqual(f2, Epsilon);
        public static bool IsLessThan(this float f1, float f2, double Epsilon = 1e-9) => f1 < f2 && !f1.IsAlmostEqual(f2, Epsilon);
        public static bool IsLessThanOrEqual(this float f1, float f2, double Epsilon = 1e-9) => f1 <= f2 || f1.IsAlmostEqual(f2, Epsilon);

        //Taken from: https://stackoverflow.com/questions/6219614/convert-a-long-to-two-int-for-the-purpose-of-reconstruction
        /// <summary>Packs 2 ints into 1 long via bitwise operations. To go from 1 long back to 2 ints, use <see cref="UnpackInts(long)"/></summary>
        public static long PackInts(int first, int second)
        {
            long b = second;
            b = b << 32;
            b = b | (uint)first;
            return b;
        }

        //Taken from: https://stackoverflow.com/questions/6219614/convert-a-long-to-two-int-for-the-purpose-of-reconstruction
        /// <summary>Unpacks 2 ints from 1 long via bitwise operations. To go from 2 ints back to 1 long, use <see cref="PackInts(int, int)"/></summary>
        public static (int First, int Second) UnpackInts(long value)
        {
            int first = (int)(value & uint.MaxValue);
            int second = (int)(value >> 32);
            return (first, second);
        }

        public static IEnumerable<T> AsEnumerable<T>(params T[] Values) => Values;

        public static T Max<T>(params T[] Values) => Values.Max();
        public static T Min<T>(params T[] Values) => Values.Min();

        public record Permutation<T>(T First, T Second);

        /// <summary>Returns every possible permutation pair of elements in this <see cref="IList{T}"/> (order matters, I.E. Permutation(1, 2) is not the same as Permutation(2, 1))</summary>
        public static IEnumerable<Permutation<T>> GetPermutations<T>(this IList<T> @this)
        {
            for (int i = 0; i < @this.Count; i++)
            {
                for (int j = 0; j < @this.Count; j++)
                {
                    if (i == j)
                        continue;

                    yield return new(@this[i], @this[j]);
                }
            }
        }

        //Adapted from: https://stackoverflow.com/a/364993/11689514
        /// <summary>Returns a power of 2 whose value is >= the given <paramref name="Value"/>. Examples:<para/>
        /// <code>Input     Output<br/>
        /// 31.9f       32<br/>
        /// 32.0f       32<br/>
        /// 32.1f       64<br/>
        /// 200f        256</code></summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        public static int NextPowerOf2(float Value) => 1 << (int)Math.Ceiling(Math.Log2(Value));

        //Taken from: https://stackoverflow.com/questions/35327255/c-sharp-linq-select-two-consecutive-items-at-once
        public static IEnumerable<TResult> SelectConsecutivePairs<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TSource, TResult> selector)
            => Enumerable.Zip(source, source.Skip(1), selector);

        /// <summary>Enumerates every consecutive pair in <paramref name="this"/> enumerable.</summary>
        /// <param name="includeLastToFirst">If true, will attempt to pair the last element in <paramref name="this"/> with the first element in <paramref name="this"/>, 
        /// provided they are not the same element. (I.E. <paramref name="this"/>'s length is > 2)</param>
        public static IEnumerable<(TSource, TSource)> SelectConsecutivePairs<TSource>(this IEnumerable<TSource> @this, bool includeLastToFirst)
        {
            if (!includeLastToFirst || !@this.Skip(2).Any())
                return Enumerable.Zip(@this, @this.Skip(1));
            else
                return Enumerable.Zip(@this, @this.Skip(1).Append(@this.First()));
        }

        public static IEnumerable<(T Current, T Next, bool HasNext)> EnumerateWithLookahead<T>(this IEnumerable<T> Items)
        {
            if (!Items.Any())
                yield break;

            bool First = true;
            T Previous = default;

            foreach (T Item in Items)
            {
                if (First)
                    First = false;
                else
                    yield return (Previous, Item, true);
                Previous = Item;
            }

            yield return new(Previous, default, false);
        }

        //Taken from: https://stackoverflow.com/questions/3041946/how-to-integer-divide-round-negative-numbers-down
        public static int DivideRoundDown(int Numerator, int Denominator) => (Numerator / Denominator) + ((Numerator % Denominator) >> 31);

        public static bool ContainsMoreThanOneElement<TSource>(this IEnumerable<TSource> source) => source.Skip(1).Any();
        public static bool ContainsExactlyOneElement<TSource>(this IEnumerable<TSource> source) => source.Any() && !source.Skip(1).Any();

        /// <summary>Computes where the topleft position would be if the given <paramref name="Position"/> is at the given <see cref="RelativePosition"/> of an object with the given <paramref name="Size"/>.<para/>
        /// EX:<br/>Inputs: <paramref name="Position"/>=(5,20), <paramref name="Anchor"/>=<see cref="RelativePosition.MiddleRight"/>, <paramref name="Size"/>=(30, 22)<br/>
        /// Returns: (5,20)-(30,22/2)=(-25,9)</summary>
        public static Vector2 GetTopleft(Vector2 Position, RelativePosition Anchor, System.Drawing.SizeF Size)
        {
            Vector2 Offset = Anchor switch
            {
                RelativePosition.TopLeft => Vector2.Zero,
                RelativePosition.TopCenter => new Vector2(Size.Width / 2, 0),
                RelativePosition.TopRight => new Vector2(Size.Width, 0),
                RelativePosition.MiddleLeft => new Vector2(0, Size.Height / 2),
                RelativePosition.MiddleCenter => new Vector2(Size.Width / 2, Size.Height / 2),
                RelativePosition.MiddleRight => new Vector2(Size.Width, Size.Height / 2),
                RelativePosition.BottomLeft => new Vector2(0, Size.Height),
                RelativePosition.BottomCenter => new Vector2(Size.Width / 2, Size.Height),
                RelativePosition.BottomRight => new Vector2(Size.Width, Size.Height),
                _ => throw new NotImplementedException($"Unrecognized {nameof(RelativePosition)}: {Anchor}"),
            };

            return Position - Offset;
        }

        /// <summary>Computes where the topleft position would be if the given <paramref name="Position"/> is at the given <see cref="RelativePosition"/> of an object with the given <paramref name="Size"/>.<para/>
        /// EX:<br/>Inputs: <paramref name="Position"/>=(5,20), <paramref name="Anchor"/>=<see cref="RelativePosition.MiddleRight"/>, <paramref name="Size"/>=(30, 22)<br/>
        /// Returns: (5,20)-(30,22/2)=(-25,9)</summary>
        public static Point GetTopleft(Point Position, RelativePosition Anchor, Size Size)
        {
            Point Offset = Anchor switch
            {
                RelativePosition.TopLeft => Point.Zero,
                RelativePosition.TopCenter => new Point(Size.Width / 2, 0),
                RelativePosition.TopRight => new Point(Size.Width, 0),
                RelativePosition.MiddleLeft => new Point(0, Size.Height / 2),
                RelativePosition.MiddleCenter => new Point(Size.Width / 2, Size.Height / 2),
                RelativePosition.MiddleRight => new Point(Size.Width, Size.Height / 2),
                RelativePosition.BottomLeft => new Point(0, Size.Height),
                RelativePosition.BottomCenter => new Point(Size.Width / 2, Size.Height),
                RelativePosition.BottomRight => new Point(Size.Width, Size.Height),
                _ => throw new NotImplementedException($"Unrecognized {nameof(RelativePosition)}: {Anchor}"),
            };

            return Position - Offset;
        }

        public static float DegreesToRadians(float Degrees) => (float)(Degrees * Math.PI / 180.0f);
        public static float RadiansToDegrees(float Radians) => (float)(Radians / Math.PI * 180.0f);

        public static int PreviousLoopedIndex<T>(this IList<T> @this, int Current)
        {
            if (Current < 0)
                Current = (Current % @this.Count) + @this.Count;
            return Current == 0 ? @this.Count - 1 : Current - 1;
        }
        public static int NextLoopedIndex<T>(this IList<T> @this, int Current) => (Current + 1) % @this.Count;
        public static T PreviousLoopedItem<T>(this IList<T> @this, int Current) => @this[@this.PreviousLoopedIndex(Current)];
        public static T NextLoopedItem<T>(this IList<T> @this, int Current) => @this[@this.NextLoopedIndex(Current)];

        /// <summary>Groups the elements in <see cref="IEnumerable{T}"/> into chunks of <paramref name="GroupLength"/> consecutive elements.<para/>
        /// Example:<br/>
        /// Input: { 20, 12, 16, 4, 55, 123, 17, 4 }, <paramref name="GroupLength"/>=3<br/>
        /// Output: { { 20, 12, 16 }, { 4, 55, 123 }, { 17, 4 } }</summary>
        /// <param name="GroupLength">How many elements should be in each group.</param>
        public static IEnumerable<IGrouping<int, TValue>> AsGroupsOfLength<TValue>(this IEnumerable<TValue> @this, int GroupLength)
            => @this.Select((s, i) => new { Value = s, Index = i }).GroupBy(item => item.Index / GroupLength, item => item.Value);

        private static readonly ReadOnlyCollection<int> UnixPlatformIds = new List<int>() { 4, 6, 128, (int)PlatformID.Unix }.AsReadOnly();
        //Adapted from: https://mono.fandom.com/wiki/Detecting_the_execution_platform
        public static bool IsPlatformUnix => UnixPlatformIds.Contains((int)Environment.OSVersion.Platform);

        /// <summary>This method is intended to replace the usage of <see cref="Collection{T}.Clear"/> on <see cref="ObservableCollection{T}"/>s<br/> 
        /// because Clear does not include the old items that were removed in the <see cref="ObservableCollection{T}.CollectionChanged"/> event args.<para/>
        /// See also: <see href="https://stackoverflow.com/questions/224155/when-clearing-an-observablecollection-there-are-no-items-in-e-olditems"/></summary>
        public static void ClearOneByOne<T>(this ObservableCollection<T> @this)
        {
            while (@this.Count > 0)
            {
                @this.RemoveAt(@this.Count - 1);
            }
        }

        public static T RemoveLast<T>(IList<T> Items, T DefaultValue)
        {
            if (!Items.Any())
                return DefaultValue;
            else
            {
                T Value = Items[Items.Count - 1];
                Items.RemoveAt(Items.Count - 1);
                return Value;
            }
        }

        public static string ReadEmbeddedResourceAsString(Assembly CurrentAssembly, string ResourceName)
        {
            using (Stream ResourceStream = CurrentAssembly.GetManifestResourceStream(ResourceName))
            using (StreamReader Reader = new StreamReader(ResourceStream))
                return Reader.ReadToEnd();
        }
    }
}
