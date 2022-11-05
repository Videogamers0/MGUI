using Microsoft.Xna.Framework;
using MGUI.Core.UI.Brushes.Border_Brushes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI.Brushes.Fill_Brushes
{
    /// <summary>See also:<para/>
    /// <see cref="MGSolidFillBrush"/><br/>
    /// <see cref="MGCompositedFillBrush"/><br/>
    /// <see cref="MGTextureFillBrush"/><br/>
    /// <see cref="MGGradientFillBrush"/><br/>
    /// <see cref="MGDiagonalGradientFillBrush"/><br/>
    /// <see cref="MGPaddedFillBrush"/><br/>
    /// <see cref="MGBorderedFillBrush"/><br/>
    /// <see cref="MGProgressBarGradientBrush"/></summary>
    public interface IFillBrush : ICloneable
    {
        public void Draw(ElementDrawArgs DA, MGElement Element, Rectangle Bounds);
        public MGUniformBorderBrush AsUniformBorderBrush() => new(this);

        public IFillBrush Copy();
        object ICloneable.Clone() => Copy();
    }
}
