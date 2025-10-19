using MGUI.Shared.Rendering;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using System.Collections.Generic;
using System.Linq;

namespace MGUI.Core.UI.Brushes.Border_Brushes
{
    /// <summary>An <see cref="IBorderBrush"/> that draws several nested <see cref="IBorderBrush"/>es in order.<para/>
    /// See also: <see cref="MGUniformBorderBrush"/>, <see cref="MGDockedBorderBrush"/>, <see cref="MGTexturedBorderBrush"/>, <see cref="MGBandedBorderBrush"/>, <see cref="MGHighlightBorderBrush"/></summary>
    public class MGCompositedBorderBrush : IBorderBrush
    {
        public readonly List<IBorderBrush> Brushes;

        public MGCompositedBorderBrush(params IBorderBrush[] Brushes)
        {
            this.Brushes = Brushes.Where(x => x != null).ToList();
        }

        void IBorderBrush.Update(UpdateBaseArgs UA)
        {
            foreach (IBorderBrush Brush in Brushes)
                Brush.Update(UA);
        }

        public void Draw(ElementDrawArgs DA, MGElement Element, Rectangle Bounds, Thickness BT)
        {
            foreach (IBorderBrush Brush in Brushes)
                Brush.Draw(DA, Element, Bounds, BT);
        }

        public IBorderBrush Copy() => new MGCompositedBorderBrush(Brushes.Select(x => x.Copy()).ToArray());
    }
}
