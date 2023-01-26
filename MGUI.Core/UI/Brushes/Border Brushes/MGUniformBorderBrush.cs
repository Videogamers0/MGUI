using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MGUI.Shared.Helpers;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI.Brushes.Border_Brushes
{
    /// <summary>An <see cref="IBorderBrush"/> that uses the same <see cref="IFillBrush"/> for each side: Left, Top, Right, Bottom<para/>
    /// See also: <see cref="MGDockedBorderBrush"/>, <see cref="MGBandedBorderBrush"/></summary>
    public struct MGUniformBorderBrush : IBorderBrush
    {
        public static readonly MGUniformBorderBrush Transparent = new(MGSolidFillBrush.Transparent);
        public static readonly MGUniformBorderBrush White = new(MGSolidFillBrush.White);
        public static readonly MGUniformBorderBrush LightGray = new(MGSolidFillBrush.LightGray);
        public static readonly MGUniformBorderBrush Gray = new(MGSolidFillBrush.Gray);
        public static readonly MGUniformBorderBrush DarkGray = new(MGSolidFillBrush.DarkGray);
        public static readonly MGUniformBorderBrush Black = new(MGSolidFillBrush.Black);

        private readonly IFillBrush _Brush;
        public IFillBrush Brush => _Brush ?? MGSolidFillBrush.Transparent;

        /// <summary>Uses an <see cref="MGSolidFillBrush"/> from the given <paramref name="Color"/> for each side.</summary>
        /// <param name="Color"></param>
        public MGUniformBorderBrush(Color Color)
        {
            this._Brush = new MGSolidFillBrush(Color);
        }

        public MGUniformBorderBrush(IFillBrush Brush)
        {
            this._Brush = Brush ?? throw new ArgumentNullException(nameof(Brush));
        }

        public void Draw(ElementDrawArgs DA, MGElement Element, Rectangle Bounds, Thickness BT)
        {
            if (BT.IsEmpty())
                return;

            if (BT.Left > 0)
                Brush.Draw(DA, Element, new(Bounds.Left, Bounds.Top, BT.Left, Bounds.Height));
            if (BT.Right > 0)
                Brush.Draw(DA, Element, new(Bounds.Right - BT.Right, Bounds.Top, BT.Right, Bounds.Height));
            if (BT.Top > 0)
                Brush.Draw(DA, Element, new(Bounds.Left + BT.Left, Bounds.Top, Bounds.Width - BT.Width, BT.Top));
            if (BT.Bottom > 0)
                Brush.Draw(DA, Element, new(Bounds.Left + BT.Left, Bounds.Bottom - BT.Bottom, Bounds.Width - BT.Width, BT.Bottom));
        }

        public IBorderBrush Copy() => new MGUniformBorderBrush(Brush.Copy());

        public static explicit operator MGUniformBorderBrush(Color color) => new(color);
    }
}
