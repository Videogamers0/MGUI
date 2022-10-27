using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI.Brushes.Border_Brushes
{
    /// <summary>See also: <see cref="MGUniformBorderBrush"/>, <see cref="MGDockedBorderBrush"/></summary>
    public interface IBorderBrush
    {
        public IFillBrush GetLeft();
        public IFillBrush GetTop();
        public IFillBrush GetRight();
        public IFillBrush GetBottom();

        public void Draw(ElementDrawArgs DA, MGElement Element, Rectangle Bounds, Thickness BT);

        public IBorderBrush Copy();
    }
}
