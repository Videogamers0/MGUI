using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using MonoGame.Extended;

namespace MGUI.Shared.Helpers
{
    public static class ThicknessUtils
    {
        /// <summary>Returns true if all sides are 0</summary>
        public static bool IsEmpty(this Thickness @this) => @this.Sides().All(x => x == 0);

        [DebuggerStepThrough]
        public static Thickness ChangeLeft(this Thickness @this, int NewValue) => new(NewValue, @this.Top, @this.Right, @this.Bottom);
        [DebuggerStepThrough]
        public static Thickness ChangeTop(this Thickness @this, int NewValue) => new(@this.Left, NewValue, @this.Right, @this.Bottom);
        [DebuggerStepThrough]
        public static Thickness ChangeRight(this Thickness @this, int NewValue) => new(@this.Left, @this.Top, NewValue, @this.Bottom);
        [DebuggerStepThrough]
        public static Thickness ChangeBottom(this Thickness @this, int NewValue) => new(@this.Left, @this.Top, @this.Right, NewValue);

        public static Thickness Add(this Thickness @this, Thickness other)
            => new(@this.Left + other.Left, @this.Top + other.Top, @this.Right + other.Right, @this.Bottom + other.Bottom);

        public static Thickness Subtract(this Thickness @this, Thickness other, int? MinLeft = null, int? MinTop = null, int? MinRight = null, int? MinBottom = null)
            => new(Math.Max(MinLeft ?? int.MinValue, @this.Left - other.Left), 
                Math.Max(MinTop ?? int.MinValue, @this.Top - other.Top), 
                Math.Max(MinRight ?? int.MinValue , @this.Right - other.Right), 
                Math.Max(MinBottom ?? int.MinValue, @this.Bottom - other.Bottom));

        /// <summary>Adjusts the total width and height of this <see cref="Thickness"/> to fit within the inclusive range [<paramref name="MinSize"/>, <paramref name="MaxSize"/>].<para/>
        /// If the width or height must be increased, the extra width or height is added to <see cref="Thickness.Left"/> / <see cref="Thickness.Top"/>.<br/>
        /// If the width or height must be decreased, the excess width or height is first removed from <see cref="Thickness.Right"/> / <see cref="Thickness.Bottom"/>, then from <see cref="Thickness.Left"/> / <see cref="Thickness.Top"/></summary>
        public static Thickness Clamp(this Thickness @this, Size MinSize, Size MaxSize)
            => @this.ClampWidth(MinSize.Width, MaxSize.Width).ClampHeight(MinSize.Height, MaxSize.Height);

        /// <summary>Adjusts the total width of this <see cref="Thickness"/> to fit within the inclusive range [<paramref name="MinWidth"/>, <paramref name="MaxWidth"/>].<para/>
        /// If the width must be increased, extra width is added to <see cref="Thickness.Left"/>.<br/>
        /// If the width must be decreased, extra width is first removed from <see cref="Thickness.Right"/>, then from <see cref="Thickness.Left"/></summary>
        public static Thickness ClampWidth(this Thickness @this, int MinWidth, int MaxWidth)
        {
            if (MinWidth > MaxWidth)
                throw new ArgumentException($"{nameof(ThicknessUtils)}.{nameof(ClampWidth)}: {nameof(MinWidth)} cannot exceed {nameof(MaxWidth)}");

            int CurrentWidth = @this.Width;
            if (CurrentWidth >= MinWidth && CurrentWidth <= MaxWidth)
                return @this;
            else if (CurrentWidth < MinWidth)
            {
                int ExtraWidth = MinWidth - CurrentWidth;
                return new(@this.Left + ExtraWidth, @this.Top, @this.Right, @this.Bottom);
            }
            else
            {
                int ExcessWidth = CurrentWidth - MaxWidth;

                if (@this.Right >= ExcessWidth)
                    return new(@this.Left, @this.Top, @this.Right - ExcessWidth, @this.Bottom);
                else
                    return new(@this.Left - (ExcessWidth - @this.Right), @this.Top, 0, @this.Bottom);
            }
        }

        /// <summary>Adjusts the total height of this <see cref="Thickness"/> to fit within the inclusive range [<paramref name="MinHeight"/>, <paramref name="MaxHeight"/>].<para/>
        /// If the height must be increased, extra height is added to <see cref="Thickness.Top"/>.<br/>
        /// If the height must be decreased, extra height is first removed from <see cref="Thickness.Bottom"/>, then from <see cref="Thickness.Top"/></summary>
        public static Thickness ClampHeight(this Thickness @this, int MinHeight, int MaxHeight)
        {
            if (MinHeight > MaxHeight)
                throw new ArgumentException($"{nameof(ThicknessUtils)}.{nameof(ClampHeight)}: {nameof(MinHeight)} cannot exceed {nameof(MaxHeight)}");

            int CurrentHeight = @this.Height;
            if (CurrentHeight >= MinHeight && CurrentHeight <= MaxHeight)
                return @this;
            else if (CurrentHeight < MinHeight)
            {
                int ExtraHeight = MinHeight - CurrentHeight;
                return new(@this.Left, @this.Top + ExtraHeight, @this.Right, @this.Bottom);
            }
            else
            {
                int ExcessHeight = CurrentHeight - MaxHeight;

                if (@this.Bottom >= ExcessHeight)
                    return new(@this.Left, @this.Top, @this.Right, @this.Bottom - ExcessHeight);
                else
                    return new(@this.Left, @this.Top - (ExcessHeight - @this.Bottom), @this.Right, 0);
            }
        }

        public static IEnumerable<int> Sides(this Thickness @this)
        {
            yield return @this.Left;
            yield return @this.Top;
            yield return @this.Right;
            yield return @this.Bottom;
        }
    }
}
