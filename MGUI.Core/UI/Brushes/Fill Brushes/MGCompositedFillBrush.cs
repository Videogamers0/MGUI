using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI.Brushes.Fill_Brushes
{
    /// <summary>An <see cref="IFillBrush"/> that draws several nested <see cref="IFillBrush"/>es in order.<para/>
    /// Especially useful when compositing multiple transparent colors, in cases where you still need to keep track of each individual Color used instead of reducing it down to a single value.</summary>
    public class MGCompositedFillBrush : IFillBrush
    {
        public readonly List<IFillBrush> Brushes;

        public MGCompositedFillBrush(params IFillBrush[] Brushes)
        {
            this.Brushes = Brushes.Where(x => x != null).ToList();
        }

        public void Draw(ElementDrawArgs DA, MGElement Element, Rectangle Bounds)
        {
            foreach (IFillBrush Brush in Brushes)
                Brush.Draw(DA, Element, Bounds);
        }

        public IFillBrush Copy() => new MGCompositedFillBrush(Brushes.Select(x => x.Copy()).ToArray());
    }
}
