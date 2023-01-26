using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MGUI.Shared.Helpers;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MGUI.Shared.Rendering;

namespace MGUI.Core.UI.Brushes.Border_Brushes
{
    /// <summary>An <see cref="IBorderBrush"/> that uses separate <see cref="IFillBrush"/>es for each side: Left, Top, Right, Bottom<para/>
    /// See also: <see cref="MGUniformBorderBrush"/>, <see cref="MGBandedBorderBrush"/></summary>
    public struct MGDockedBorderBrush : IBorderBrush
    {
        public IFillBrush Left { get; }
        public IFillBrush Top { get; }
        public IFillBrush Right { get; }
        public IFillBrush Bottom { get; }

        private bool IsSolidColorsOnly { get; }

        /// <summary>Uses an <see cref="MGSolidFillBrush"/> from the given Colors for each side.</summary>
        public MGDockedBorderBrush(Color Left, Color Top, Color Right, Color Bottom)
        {
            this.Left = new MGSolidFillBrush(Left);
            this.Top = new MGSolidFillBrush(Top);
            this.Right = new MGSolidFillBrush(Right);
            this.Bottom = new MGSolidFillBrush(Bottom);

            IsSolidColorsOnly = true;
        }

        public MGDockedBorderBrush(IFillBrush Left, IFillBrush Top, IFillBrush Right, IFillBrush Bottom)
        {
            this.Left = Left ?? throw new ArgumentNullException(nameof(Left));
            this.Top = Top ?? throw new ArgumentNullException(nameof(Top));
            this.Right = Right ?? throw new ArgumentNullException(nameof(Right));
            this.Bottom = Bottom ?? throw new ArgumentNullException(nameof(Bottom));

            IsSolidColorsOnly = Left is MGSolidFillBrush && Top is MGSolidFillBrush && Right is MGSolidFillBrush && Bottom is MGSolidFillBrush;
        }

        private static IEnumerable<T> AsEnum<T>(params T[] values) => values;

        public void Draw(ElementDrawArgs DA, MGElement Element, Rectangle Bounds, Thickness BT)
        {
            if (BT.IsEmpty())
                return;

            DrawTransaction DT = DA.DT;
            float Opacity = DA.Opacity;

            if (IsSolidColorsOnly)
            {
                Point TL = Bounds.TopLeft();
                Point TR = Bounds.TopRight();
                Point BL = Bounds.BottomLeft();
                Point BR = Bounds.BottomRight();

                Point InnerTL = TL + new Point(BT.Left, BT.Top);
                Point InnerTR = TR + new Point(-BT.Right, BT.Top);
                Point InnerBL = BL + new Point(BT.Left, -BT.Bottom);
                Point InnerBR = BR + new Point(-BT.Right, -BT.Bottom);

                if (BT.Left > 0)
                {
                    Left.Draw(DA, Element, new(Bounds.Left, Bounds.Top + BT.Top, BT.Left, Bounds.Height - BT.Height));
                    DT.FillPolygon(DA.Offset.ToVector2(), AsEnum(TL, InnerTL, TL + new Point(0, BT.Top)).Select(x => x.ToVector2()), ((MGSolidFillBrush)Left).Color * Opacity);
                    DT.FillPolygon(DA.Offset.ToVector2(), AsEnum(BL, InnerBL, BL + new Point(0, -BT.Bottom)).Select(x => x.ToVector2()), ((MGSolidFillBrush)Left).Color * Opacity);
                }
                if (BT.Top > 0)
                {
                    Top.Draw(DA, Element, new(Bounds.Left + BT.Left, Bounds.Top, Bounds.Width - BT.Width, BT.Top));
                    DT.FillPolygon(DA.Offset.ToVector2(), AsEnum(TL, InnerTL, TL + new Point(BT.Left, 0)).Select(x => x.ToVector2()), ((MGSolidFillBrush)Top).Color * Opacity);
                    DT.FillPolygon(DA.Offset.ToVector2(), AsEnum(TR, InnerTR, TR + new Point(-BT.Right, 0)).Select(x => x.ToVector2()), ((MGSolidFillBrush)Top).Color * Opacity);
                }
                if (BT.Right > 0)
                {
                    Right.Draw(DA, Element, new(Bounds.Right - BT.Right, Bounds.Top + BT.Top, BT.Right, Bounds.Height - BT.Height));
                    DT.FillPolygon(DA.Offset.ToVector2(), AsEnum(TR, InnerTR, TR + new Point(0, BT.Top)).Select(x => x.ToVector2()), ((MGSolidFillBrush)Right).Color * Opacity);
                    DT.FillPolygon(DA.Offset.ToVector2(), AsEnum(BR, InnerBR, BR + new Point(0, -BT.Bottom)).Select(x => x.ToVector2()), ((MGSolidFillBrush)Right).Color * Opacity);
                }
                if (BT.Bottom > 0)
                {
                    Bottom.Draw(DA, Element, new(Bounds.Left + BT.Left, Bounds.Bottom - BT.Bottom, Bounds.Width - BT.Width, BT.Bottom));
                    DT.FillPolygon(DA.Offset.ToVector2(), AsEnum(BL, InnerBL, BL + new Point(BT.Left, 0)).Select(x => x.ToVector2()), ((MGSolidFillBrush)Bottom).Color * Opacity);
                    DT.FillPolygon(DA.Offset.ToVector2(), AsEnum(BR, InnerBR, BR + new Point(-BT.Right, 0)).Select(x => x.ToVector2()), ((MGSolidFillBrush)Bottom).Color * Opacity);
                }
            }
            else
            {
                if (BT.Left > 0)
                    Left.Draw(DA, Element, new(Bounds.Left, Bounds.Top, BT.Left, Bounds.Height));
                if (BT.Right > 0)
                    Right.Draw(DA, Element, new(Bounds.Right - BT.Right, Bounds.Top, BT.Right, Bounds.Height));
                if (BT.Top > 0)
                    Top.Draw(DA, Element, new(Bounds.Left, Bounds.Top, Bounds.Width, BT.Top));
                if (BT.Bottom > 0)
                    Bottom.Draw(DA, Element, new(Bounds.Left, Bounds.Bottom - BT.Bottom, Bounds.Width, BT.Bottom));
            }
        }

        public IBorderBrush Copy() => new MGDockedBorderBrush(
            Left?.Copy() ?? MGSolidFillBrush.Transparent, 
            Top?.Copy() ?? MGSolidFillBrush.Transparent, 
            Right?.Copy() ?? MGSolidFillBrush.Transparent, 
            Bottom?.Copy() ?? MGSolidFillBrush.Transparent);

        public static explicit operator MGDockedBorderBrush(MGUniformBorderBrush uniform) => new(uniform.Brush, uniform.Brush, uniform.Brush, uniform.Brush);
    }
}
