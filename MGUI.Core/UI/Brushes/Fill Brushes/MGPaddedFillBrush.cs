using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MGUI.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI.Brushes.Fill_Brushes
{
    /// <summary>A wrapper class that allows you to manipulate the rectangular region that the nested <see cref="IFillBrush"/> is applied to.</summary>
    public class MGPaddedFillBrush : IFillBrush
    {
        public IFillBrush Brush { get; set; }
        public Thickness Padding { get; set; }

        public float? Scale { get; set; }

        public int? MinWidth { get; set; }
        public int? MinHeight { get; set; }
        public int? MaxWidth { get; set; }
        public int? MaxHeight { get; set; }

        public HorizontalAlignment? HorizontalAlignment { get; set; }
        public VerticalAlignment? VerticalAlignment { get; set; }

        public MGPaddedFillBrush(IFillBrush Brush, Thickness Padding, float? Scale = null, int? MinWidth = null, int? MinHeight = null, int? MaxWidth = null, int? MaxHeight = null,
            HorizontalAlignment? HorizontalAlignment = null, VerticalAlignment? VerticalAlignment = null)
        {
            this.Brush = Brush;
            this.Padding = Padding;

            this.Scale = Scale;

            this.MinWidth = MinWidth;
            this.MinHeight = MinHeight;
            this.MaxWidth = MaxWidth;
            this.MaxHeight = MaxHeight;

            this.HorizontalAlignment = HorizontalAlignment;
            this.VerticalAlignment = VerticalAlignment;
        }

        public void Draw(ElementDrawArgs DA, MGElement Element, Rectangle Bounds)
        {
            float Opacity = DA.Opacity;
            if (Opacity > 0 && !Opacity.IsAlmostZero() && Brush != null)
            {
                Rectangle PaddedBounds = Bounds.GetCompressed(Padding);

                if (Scale.HasValue)
                    PaddedBounds = PaddedBounds.GetScaledFromCenter(Scale.Value);

                int DesiredWidth = Math.Clamp(PaddedBounds.Width, MinWidth ?? 0, MaxWidth ?? int.MaxValue);
                int DesiredHeight = Math.Clamp(PaddedBounds.Height, MinHeight ?? 0, MaxHeight ?? int.MaxValue);

                //Rectangle ClampedBounds = new(
                //    PaddedBounds.Left - (DesiredWidth - PaddedBounds.Width) / 2, 
                //    PaddedBounds.Top - (DesiredHeight - PaddedBounds.Height) / 2, 
                //    DesiredWidth, DesiredHeight);

                Rectangle ActualBounds = MGElement.ApplyAlignment(PaddedBounds, HorizontalAlignment ?? UI.HorizontalAlignment.Stretch, VerticalAlignment ?? UI.VerticalAlignment.Stretch, new Size(DesiredWidth, DesiredHeight));

                Brush.Draw(DA, Element, ActualBounds);
            }
        }

        public IFillBrush Copy() => new MGPaddedFillBrush(Brush?.Copy(), Padding, Scale, MinWidth, MinHeight, MaxWidth, MaxHeight, HorizontalAlignment, VerticalAlignment);
    }
}
