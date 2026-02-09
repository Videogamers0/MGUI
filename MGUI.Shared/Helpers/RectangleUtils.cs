using Microsoft.Xna.Framework;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Shared.Helpers
{
    public static class RectangleUtils
    {
        /// <summary>Coordinates are INCLUSIVE</summary>
        public static Rectangle GetRectangle(int MinX, int MaxX, int MinY, int MaxY) => new(MinX, MinY, MaxX - MinX + 1, MaxY - MinY + 1);
        /// <summary>Coordinates are INCLUSIVE</summary>
        public static Rectangle GetRectangle(float MinX, float MaxX, float MinY, float MaxY) => new((int)MinX, (int)MinY, (int)(MaxX - MinX + 1), (int)(MaxY - MinY + 1));
        /// <summary>Coordinates are INCLUSIVE</summary>
        public static Rectangle GetRectangle(Vector2 Min, Vector2 Max) => new((int)Min.X, (int)Min.Y, (int)(Max.X - Min.X + 1), (int)(Max.Y - Min.Y + 1));
        /// <summary>Coordinates are INCLUSIVE</summary>
        public static Rectangle GetRectangle(Point Min, Point Max) => new(Min.X, Min.Y, (Max.X - Min.X + 1), (Max.Y - Min.Y + 1));

        /// <summary>Coordinates are INCLUSIVE</summary>
        public static RectangleF GetRectangleF(int MinX, int MaxX, int MinY, int MaxY) => new(MinX, MinY, MaxX - MinX + 1, MaxY - MinY + 1);
        /// <summary>Coordinates are INCLUSIVE</summary>
        public static RectangleF GetRectangleF(float MinX, float MaxX, float MinY, float MaxY) => new(MinX, MinY, MaxX - MinX + 1, MaxY - MinY + 1);
        /// <summary>Coordinates are INCLUSIVE</summary>
        public static RectangleF GetRectangleF(Vector2 Min, Vector2 Max) => new(Min.X, Min.Y, Max.X - Min.X + 1, Max.Y - Min.Y + 1);

        public static Rectangle CreateTransformed(this Rectangle @this, Matrix Transform) => (Rectangle)@this.CreateTransformedF(Transform);

        public static RectangleF CreateTransformedF(this Rectangle @this, Matrix Transform)
        {
            Vector2 TopLeft = @this.TopLeft().ToVector2().TransformBy(Transform);
            Vector2 BottomRight = @this.BottomRight().ToVector2().TransformBy(Transform);
            return new RectangleF(TopLeft.X, TopLeft.Y, BottomRight.X - TopLeft.X, BottomRight.Y - TopLeft.Y);
        }

        public static RectangleF CreateTransformedF(this RectangleF @this, Matrix Transform)
        {
            Vector2 TopLeft = @this.TopLeft().TransformBy(Transform);
            Vector2 BottomRight = @this.BottomRight().TransformBy(Transform);
            return new RectangleF(TopLeft.X, TopLeft.Y, BottomRight.X - TopLeft.X, BottomRight.Y - TopLeft.Y);
        }

        [DebuggerStepThrough]
        public static bool ContainsInclusive(this Rectangle @this, Point point)
            => ContainsInclusive(@this, point.ToVector2());
        [DebuggerStepThrough]
        public static bool ContainsInclusive(this Rectangle @this, Vector2 point)
            => point.X >= @this.Left && point.X <= @this.Right && point.Y >= @this.Top && point.Y <= @this.Bottom;

        [DebuggerStepThrough]
        public static bool ContainsInclusive(this RectangleF @this, Point point)
            => ContainsInclusive(@this, point.ToVector2());
        [DebuggerStepThrough]
        public static bool ContainsInclusive(this RectangleF @this, Vector2 point)
            => point.X >= @this.Left && point.X <= @this.Right && point.Y >= @this.Top && point.Y <= @this.Bottom;

        [DebuggerStepThrough]
        public static RectangleF ToFloat(this Rectangle @this) => new(@this.X, @this.Y, @this.Width, @this.Height);

        /// <summary>Returns a <see cref="Rectangle"/> that fully encloses the given <see cref="RectangleF"/></summary>
        [DebuggerStepThrough]
        public static Rectangle RoundUp(this RectangleF @this)
            => new Rectangle((int)@this.Left, (int)@this.Top, (int)Math.Ceiling(@this.Right) - (int)(@this.Left), (int)Math.Ceiling(@this.Bottom) - (int)(@this.Top));

        [DebuggerStepThrough]
        public static Rectangle TranslatedCopy(this Rectangle @this, Point Amount)
            => new(@this.Left + Amount.X, @this.Top + Amount.Y, @this.Width, @this.Height);

        /// <summary>Returns a <see cref="Rectangle"/> that fully spans all the given <paramref name="Inputs"/> <see cref="Rectangle"/>s.</summary>
        public static Rectangle Union(IEnumerable<Rectangle> Inputs)
        {
            if (Inputs?.Any() != true)
                return default(Rectangle);
            else
            {
                int MinLeft = int.MaxValue;
                int MaxRight = int.MinValue;
                int MinTop = int.MaxValue;
                int MaxBottom = int.MinValue;

                foreach (Rectangle r in Inputs)
                {
                    if (r.Left < MinLeft)
                        MinLeft = r.Left;
                    if (r.Right > MaxRight)
                        MaxRight = r.Right;
                    if (r.Top < MinTop)
                        MinTop = r.Top;
                    if (r.Bottom > MaxBottom)
                        MaxBottom = r.Bottom;
                }

                return new(MinLeft, MinTop, MaxRight - MinLeft, MaxBottom - MinTop);
            }
        }

        /// <summary>Returns a <see cref="RectangleF"/> that fully spans all the given <paramref name="Inputs"/> <see cref="RectangleF"/>s.</summary>
        public static RectangleF Union(IEnumerable<RectangleF> Inputs)
        {
            if (Inputs?.Any() != true)
                return default(RectangleF);
            else
            {
                float MinLeft = float.MaxValue;
                float MaxRight = float.MinValue;
                float MinTop = float.MaxValue;
                float MaxBottom = float.MinValue;

                foreach (RectangleF r in Inputs)
                {
                    if (r.Left < MinLeft)
                        MinLeft = r.Left;
                    if (r.Right > MaxRight)
                        MaxRight = r.Right;
                    if (r.Top < MinTop)
                        MinTop = r.Top;
                    if (r.Bottom > MaxBottom)
                        MaxBottom = r.Bottom;
                }

                return new(MinLeft, MinTop, MaxRight - MinLeft, MaxBottom - MinTop);
            }
        }

        [DebuggerStepThrough]
        public static Point TopLeft(this Rectangle @this) => new(@this.Left, @this.Top);
        [DebuggerStepThrough]
        public static Point TopRight(this Rectangle @this) => new(@this.Right, @this.Top);
        [DebuggerStepThrough]
        public static Point BottomLeft(this Rectangle @this) => new(@this.Left, @this.Bottom);
        [DebuggerStepThrough]
        public static Point BottomRight(this Rectangle @this) => new(@this.Right, @this.Bottom);

        [DebuggerStepThrough]
        public static Vector2 TopLeft(this RectangleF @this) => new(@this.Left, @this.Top);
        [DebuggerStepThrough]
        public static Vector2 TopRight(this RectangleF @this) => new(@this.Right, @this.Top);
        [DebuggerStepThrough]
        public static Vector2 BottomLeft(this RectangleF @this) => new(@this.Left, @this.Bottom);
        [DebuggerStepThrough]
        public static Vector2 BottomRight(this RectangleF @this) => new(@this.Right, @this.Bottom);

        public static RectangleF GetBounds(IEnumerable<Vector2> Vertices)
        {
            float MinX = float.MaxValue;
            float MaxX = float.MinValue;
            float MinY = float.MaxValue;
            float MaxY = float.MinValue;

            foreach (Vector2 Vertex in Vertices)
            {
                if (Vertex.X < MinX)
                    MinX = Vertex.X;
                if (Vertex.X > MaxX)
                    MaxX = Vertex.X;

                if (Vertex.Y < MinY)
                    MinY = Vertex.Y;
                if (Vertex.Y > MaxY)
                    MaxY = Vertex.Y;
            }

            return new(MinX, MinY, MaxX - MinX, MaxY - MinY);
        }

        public static RectangleF GetScaledFromCenter(this RectangleF @this, float ScaleFactor)
        {
            Point2 Center = @this.Center;
            return new(Center.X - (Center.X - @this.X) * ScaleFactor, Center.Y - (Center.Y - @this.Y) * ScaleFactor, 
                @this.Width * ScaleFactor, @this.Height * ScaleFactor);
        }

        [DebuggerStepThrough]
        public static Rectangle GetTranslated(this Rectangle @this, Point Offset)
            => new(@this.X + Offset.X, @this.Y + Offset.Y, @this.Width, @this.Height);
        [DebuggerStepThrough]
        public static Rectangle GetTranslated(this Rectangle @this, int X, int Y)
            => new(@this.X + X, @this.Y + Y, @this.Width, @this.Height);

        /// <summary>Returns a new <see cref="Rectangle"/> that is expanded on all sides by the given <paramref name="Amount"/></summary>
        [DebuggerStepThrough]
        public static Rectangle GetExpanded(this Rectangle @this, int Amount)
            => new(@this.Left - Amount, @this.Top - Amount, @this.Width + Amount * 2, @this.Height + Amount * 2);

        /// <summary>Returns a new <see cref="Rectangle"/> that is expanded on each side by the given <paramref name="Amount"/></summary>
        [DebuggerStepThrough]
        public static Rectangle GetExpanded(this Rectangle @this, Thickness Amount)
            => new(@this.Left - Amount.Left, @this.Top - Amount.Top, @this.Width + Amount.Width, @this.Height + Amount.Height);

        /// <summary>Returns a new <see cref="Rectangle"/> that is compressed on all sides by the given <paramref name="Amount"/></summary>
        [DebuggerStepThrough]
        public static Rectangle GetCompressed(this Rectangle @this, int Amount)
            => new(@this.Left + Amount, @this.Top + Amount, @this.Width - Amount * 2, @this.Height - Amount * 2);

        /// <summary>Returns a new <see cref="Rectangle"/> that is compressed on each side by the given <paramref name="Amount"/></summary>
        [DebuggerStepThrough]
        public static Rectangle GetCompressed(this Rectangle @this, Thickness Amount)
            => new(@this.Left + Amount.Left, @this.Top + Amount.Top, @this.Width - Amount.Width, @this.Height - Amount.Height);

        public static Rectangle GetScaledFromCenter(this Rectangle @this, float ScaleFactor)
        {
            int NewWidth = (int)(@this.Width * ScaleFactor);
            int NewHeight = (int)(@this.Height * ScaleFactor);

            Point Center = @this.Center;
            return new(Center.X - NewWidth / 2, Center.Y - NewHeight / 2, NewWidth, NewHeight);
        }


        [DebuggerStepThrough]
        public static RectangleF GetTranslated(this RectangleF @this, Vector2 Offset)
            => new(@this.X + Offset.X, @this.Y + Offset.Y, @this.Width, @this.Height);

        /// <summary>Returns a new <see cref="RectangleF"/> that is expanded on all sides by the given <paramref name="Amount"/></summary>
        [DebuggerStepThrough]
        public static RectangleF GetExpanded(this RectangleF @this, float Amount)
            => new(@this.Left - Amount, @this.Top - Amount, @this.Width + Amount * 2, @this.Height + Amount * 2);

        /// <summary>Returns a new <see cref="RectangleF"/> that is expanded on each side by the given <paramref name="Amount"/></summary>
        [DebuggerStepThrough]
        public static RectangleF GetExpanded(this RectangleF @this, Thickness Amount)
            => new(@this.Left - Amount.Left, @this.Top - Amount.Top, @this.Width + Amount.Width, @this.Height + Amount.Height);

        /// <summary>Returns a new <see cref="RectangleF"/> that is compressed on all sides by the given <paramref name="Amount"/></summary>
        [DebuggerStepThrough]
        public static RectangleF GetCompressed(this RectangleF @this, float Amount)
            => new(@this.Left + Amount, @this.Top + Amount, @this.Width - Amount * 2, @this.Height - Amount * 2);

        /// <summary>Returns a new <see cref="RectangleF"/> that is compressed on each side by the given <paramref name="Amount"/></summary>
        [DebuggerStepThrough]
        public static RectangleF GetCompressed(this RectangleF @this, Thickness Amount)
            => new(@this.Left + Amount.Left, @this.Top + Amount.Top, @this.Width - Amount.Width, @this.Height - Amount.Height);
    }
}
