using Microsoft.Xna.Framework;
using MGUI.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI.Brushes.Fill_Brushes
{
    /// <summary>An <see cref="IFillBrush"/> that fills its bounds with a gradient. Each corner has a specified <see cref="Color"/> that the gradient linearly interpolates to.<para/>
    /// For a simpler version, use <see cref="MGDiagonalGradientFillBrush"/></summary>
    public readonly struct MGGradientFillBrush : IFillBrush
    {
        public readonly Color TopLeftColor;
        public readonly Color TopRightColor;
        public readonly Color BottomLeftColor;
        public readonly Color BottomRightColor;

        public MGGradientFillBrush(Color TopLeft, Color TopRight, Color BottomRight, Color BottomLeft)
        {
            this.TopLeftColor = TopLeft;
            this.TopRightColor = TopRight;
            this.BottomLeftColor = BottomLeft;
            this.BottomRightColor = BottomRight;
        }

        public void Draw(ElementDrawArgs DA, MGElement Element, Rectangle Bounds)
        {
            float Opacity = DA.Opacity;
            if (Opacity > 0 && !Opacity.IsAlmostZero())
            {
                DA.DT.FillQuadrilateralLinearClamp(DA.Offset.ToVector2(),
                    Bounds.TopLeft().ToVector2(), TopLeftColor * Opacity,
                    Bounds.TopRight().ToVector2(), TopRightColor * Opacity,
                    Bounds.BottomRight().ToVector2(), BottomRightColor * Opacity,
                    Bounds.BottomLeft().ToVector2(), BottomLeftColor * Opacity);
            }
        }

        public IFillBrush Copy() => new MGGradientFillBrush(TopLeftColor, TopRightColor, BottomRightColor, BottomLeftColor);
    }

    /// <summary>A simplified version of <see cref="MGGradientFillBrush"/> that only requires 2 <see cref="Color"/>s for opposite corners of the bounds.</summary>
    public readonly struct MGDiagonalGradientFillBrush : IFillBrush
    {
        public readonly Color Color1;
        public readonly Color Color2;
        public readonly CornerType Color1Position;
        public readonly CornerType Color2Position => OppositeCorners[Color1Position];

        public Color GetColor(CornerType Corner)
        {
            if (Corner == Color1Position)
                return Color1;
            else if (Corner == Color2Position)
                return Color2;
            else
                return Color.Lerp(Color1, Color2, 0.5f);
        }

        private static readonly Dictionary<CornerType, CornerType> OppositeCorners = new Dictionary<CornerType, CornerType>()
        {
            { CornerType.TopLeft, CornerType.BottomRight },
            { CornerType.TopRight, CornerType.BottomLeft },
            { CornerType.BottomRight, CornerType.TopLeft },
            { CornerType.BottomLeft, CornerType.TopRight }
        };

        /// <param name="Color1Position">The corner that <paramref name="Color1"/> is associated with. <see cref="Color2"/> will be at the opposite corner.</param>
        public MGDiagonalGradientFillBrush(Color Color1, Color Color2, CornerType Color1Position)
        {
            this.Color1 = Color1;
            this.Color2 = Color2;
            this.Color1Position = Color1Position;
        }

        public void Draw(ElementDrawArgs DA, MGElement Element, Rectangle Bounds)
        {
            float Opacity = DA.Opacity;
            if (Opacity > 0 && !Opacity.IsAlmostZero())
            {
                DA.DT.FillQuadrilateralLinearClamp(DA.Offset.ToVector2(),
                    Bounds.TopLeft().ToVector2(), GetColor(CornerType.TopLeft) * Opacity,
                    Bounds.TopRight().ToVector2(), GetColor(CornerType.TopRight) * Opacity,
                    Bounds.BottomRight().ToVector2(), GetColor(CornerType.BottomRight) * Opacity,
                    Bounds.BottomLeft().ToVector2(), GetColor(CornerType.BottomLeft) * Opacity);
            }
        }

        public IFillBrush Copy() => new MGDiagonalGradientFillBrush(Color1, Color2, Color1Position);
    }
}
