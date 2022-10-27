using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MGUI.Shared.Helpers;
using MGUI.Core.UI.Brushes.Border_Brushes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI.Brushes.Fill_Brushes
{
    /// <summary>An <see cref="IFillBrush"/> that combines the functionality of an <see cref="IFillBrush"/> and an <see cref="IBorderBrush"/></summary>
    public class MGBorderedFillBrush : IFillBrush
    {
        public Thickness BorderThickness { get; set; }
        public IBorderBrush BorderBrush { get; set; }
        public IFillBrush FillBrush { get; set; }
        /// <summary>If true, <see cref="FillBrush"/> will not be drawn to the entire bounds and will instead be compressed by the <see cref="BorderThickness"/>,<br/>
        /// only filling the portion of the bounds that don't intersect the border.</summary>
        public bool PadFillBoundsByBorderThickness { get; set; }

        /// <param name="PadFillBoundsByBorderThickness">If true, <paramref name="FillBrush"/> will not be drawn to the entire bounds and will instead be compressed by the <paramref name="BorderThickness"/>,<br/>
        /// only filling the portion of the bounds that don't intersect the border.</param>
        public MGBorderedFillBrush(Thickness BorderThickness, IBorderBrush BorderBrush, IFillBrush FillBrush, bool PadFillBoundsByBorderThickness)
        {
            this.BorderThickness = BorderThickness;
            this.BorderBrush = BorderBrush;
            this.FillBrush = FillBrush;
            this.PadFillBoundsByBorderThickness = PadFillBoundsByBorderThickness;
        }

        public void Draw(ElementDrawArgs DA, MGElement Element, Rectangle Bounds)
        {
            float Opacity = DA.Opacity;
            if (Opacity > 0 && !Opacity.IsAlmostZero())
            {
                if (FillBrush != null)
                {
                    Rectangle FillBounds = PadFillBoundsByBorderThickness ? Bounds.GetCompressed(BorderThickness) : Bounds;
                    FillBrush.Draw(DA, Element, FillBounds);
                }

                BorderBrush?.Draw(DA, Element, Bounds, BorderThickness);
            }
        }

        public IFillBrush Copy() => new MGBorderedFillBrush(BorderThickness, BorderBrush?.Copy(), FillBrush?.Copy(), PadFillBoundsByBorderThickness);
    }
}
