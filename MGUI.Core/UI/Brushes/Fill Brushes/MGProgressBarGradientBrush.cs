using MGUI.Shared.Helpers;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI.Brushes.Fill_Brushes
{
    /// <summary>An <see cref="IFillBrush"/> that linearly interpolates between 2 colors based on the <see cref="MGProgressBar"/>'s <see cref="MGProgressBar.ValuePercent"/> to determine what color to fill the bounds with.</summary>
    public readonly struct MGProgressBarGradientBrush : IFillBrush
    {
        public readonly MGProgressBar ProgressBar;
        public readonly Color MinimumValueColor;
        public readonly Color MiddleValueColor;
        public readonly Color MaximumValueColor;

        public MGProgressBarGradientBrush(MGProgressBar ProgressBar)
            : this(ProgressBar, Color.Red, Color.Yellow, new Color(0, 255, 0)) { }

        /// <param name="MinimumValueColor">The color to use when <paramref name="ProgressBar"/>'s <see cref="MGProgressBar.ValuePercent"/> is 0.0</param>
        /// <param name="MiddleValueColor">The color to use when <paramref name="ProgressBar"/>'s <see cref="MGProgressBar.ValuePercent"/> is 50.0</param>
        /// <param name="MaximumValueColor">The color to use when <paramref name="ProgressBar"/>'s <see cref="MGProgressBar.ValuePercent"/> is 100.0</param>
        public MGProgressBarGradientBrush(MGProgressBar ProgressBar, Color MinimumValueColor, Color MiddleValueColor, Color MaximumValueColor)
        {
            this.ProgressBar = ProgressBar ?? throw new ArgumentNullException(nameof(ProgressBar));
            this.MinimumValueColor = MinimumValueColor;
            this.MiddleValueColor = MiddleValueColor;
            this.MaximumValueColor = MaximumValueColor;
        }

        public void Draw(ElementDrawArgs DA, MGElement Element, Rectangle Bounds)
        {
            if (ProgressBar != null && DA.Opacity > 0 && !DA.Opacity.IsAlmostZero())
            {
                float Progress = ProgressBar.ValuePercent / 100f;

                float Min, Max;
                Color c1, c2;
                if (Progress > 0.5f)
                {
                    Min = 0.5f;
                    Max = 1.0f;
                    c1 = MiddleValueColor;
                    c2 = MaximumValueColor;
                }
                else
                {
                    Min = 0f;
                    Max = 0.5f;
                    c1 = MinimumValueColor;
                    c2 = MiddleValueColor;
                }

                Progress = (Progress - Min) / (Max - Min);
                int Alpha = (int)(c1.A * (1.0f - Progress) + c2.A * Progress);
                Color FillColor = new Color(Color.Lerp(c1, c2, Progress), Alpha) * DA.Opacity;
                DA.DT.FillRectangle(DA.Offset.ToVector2(), Bounds, FillColor);
            }
        }

        public override string ToString() => $"{nameof(MGProgressBarGradientBrush)}: {MinimumValueColor} - {MaximumValueColor}";

        public IFillBrush Copy() => new MGProgressBarGradientBrush(ProgressBar, MinimumValueColor, MiddleValueColor, MaximumValueColor);
    }
}
