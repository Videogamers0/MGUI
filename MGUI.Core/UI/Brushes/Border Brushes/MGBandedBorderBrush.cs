using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Shared.Helpers;
using MGUI.Shared.Rendering;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI.Brushes.Border_Brushes
{
    /// <param name="ThicknessWeight">Determines how much thickness this band will be drawn with.<para/>
    /// EX: If drawing the entire <see cref="MGBandedBorderBrush"/> with <see cref="Thickness"/>=10, and there are 2 bands with weights of 0.4 and 0.6,<br/>
    /// the 1st band is drawn with Floor(10*(0.4/(0.4+0.6))=4, 2nd band is drawn with Floor(10*(0.6/(0.4+0.6))=6 thickness.<para/>
    /// (Weights do not need to sum to 1.0)<para/>
    /// Warning - the actual thickness is always rounded down, so the total rendered size of the border may end up less than the <see cref="MGBorder.BorderThickness"/> it was drawn with.</param>
    public readonly record struct MGBorderBand(IBorderBrush Brush, double ThicknessWeight);

    /// <summary>An <see cref="IBorderBrush"/> that draws several nested <see cref="IBorderBrush"/>es starting from the outside and moving inwards.<para/>
    /// See also: <see cref="MGUniformBorderBrush"/>, <see cref="MGDockedBorderBrush"/></summary>
    public struct MGBandedBorderBrush : IBorderBrush
    {
        public readonly ReadOnlyCollection<MGBorderBand> Bands;

        public MGBandedBorderBrush(IList<Color> Colors, IList<double> Weights)
        {
            if (Colors.Count != Weights.Count)
                throw new InvalidOperationException($"{nameof(MGBandedBorderBrush)}.ctor: There must be exactly 1 weight per color");

            List<MGBorderBand> Bands = new();
            for (int i = 0; i < Colors.Count; i++)
            {
                Color Color = Colors[i];
                double Weight = Weights[i];
                Bands.Add(new(Color.AsFillBrush().AsUniformBorderBrush(), Weight));
            }

            this.Bands = Bands.AsReadOnly();
        }

        /// <param name="Bands">The first band is drawn on the outer edge. Last band is drawn most inwards.</param>
        public MGBandedBorderBrush(params MGBorderBand[] Bands)
        {
            this.Bands = Bands.ToList().AsReadOnly();
        }

        public MGBandedBorderBrush()
        {
            this.Bands = new List<MGBorderBand>().AsReadOnly();
        }

        private static IEnumerable<T> AsEnum<T>(params T[] values) => values;

        public void Draw(ElementDrawArgs DA, MGElement Element, Rectangle Bounds, Thickness BT)
        {
            if (!Bands.Any())
                return;

            DrawTransaction DT = DA.DT;
            float Opacity = DA.Opacity;

            double TotalWeight = Bands.Sum(x => x.ThicknessWeight);

            Rectangle RemainingBounds = Bounds;
            foreach (MGBorderBand Band in Bands)
            {
                double PercentageThickness = Band.ThicknessWeight / TotalWeight;
                Thickness BandThickness = new(
                    (int)(BT.Left * PercentageThickness), (int)(BT.Top * PercentageThickness), 
                    (int)(BT.Right * PercentageThickness), (int)(BT.Bottom * PercentageThickness));

                Band.Brush.Draw(DA, Element, RemainingBounds, BandThickness);

                RemainingBounds = RemainingBounds.GetCompressed(BandThickness);
            }
        }

        public IBorderBrush Copy() => new MGBandedBorderBrush(Bands.Select(x => new MGBorderBand(x.Brush.Copy(), x.ThicknessWeight)).ToArray());
    }
}
