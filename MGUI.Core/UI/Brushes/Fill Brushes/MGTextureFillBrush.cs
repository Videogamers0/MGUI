using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MGUI.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI.Brushes.Fill_Brushes
{
    /// <summary>An <see cref="IFillBrush"/> that fills its bounds with a <see cref="Texture2D"/></summary>
    public readonly struct MGTextureFillBrush : IFillBrush
    {
        public readonly MGTextureData Source;
        public readonly Stretch Stretch;
        public readonly Color Color;

        //TODO Option to make it tesselate if Stretch is not Fill/UniformToFill? (LinearWrap SamplerState?)
        //      So if Stretch is None or UniformToFill, the empty space would instead be filled up with LinearWrap texture

        /// <param name="SourceName">The name of the <see cref="MGTextureData"/> in <see cref="MGResources.Textures"/> that should be drawn by this <see cref="MGTextureFillBrush"/>.<para/>
        /// See also: <see cref="MGElement.GetResources"/>, <see cref="MGResources.Textures"/>, <see cref="MGResources.AddTexture(string, MGTextureData)"/></param>
        public MGTextureFillBrush(MGDesktop Desktop, string SourceName, Stretch Stretch = Stretch.Fill, Color? Color = null)
        {
            if (Desktop == null)
                throw new ArgumentNullException(nameof(Desktop));
            if (SourceName == null)
                throw new ArgumentNullException(nameof(SourceName));
            if (!Desktop.Resources.TryGetTexture(SourceName, out MGTextureData Source))
                throw new InvalidOperationException($"No Texture was found with the name '{SourceName}' in {nameof(MGResources)}.{nameof(MGResources.Textures)}.");

            this.Source = Source;
            this.Stretch = Stretch;
            this.Color = Color ?? Microsoft.Xna.Framework.Color.White;
        }

        public MGTextureFillBrush(MGTextureData Source, Stretch Stretch = Stretch.Fill, Color? Color = null)
        {
            this.Source = Source;
            this.Stretch = Stretch;
            this.Color = Color ?? Microsoft.Xna.Framework.Color.White;
        }

        private int UnstretchedWidth => Source.RenderSize.Width;
        private int UnstretchedHeight => Source.RenderSize.Height;
        private double UnstretchedAspectRatio => UnstretchedHeight == 0 ? 1.0 : UnstretchedWidth * 1.0 / UnstretchedHeight;

        private static int GetWidthByAspectRatio(int Height, double AspectRatio) => (int)Math.Round(Height * AspectRatio, MidpointRounding.ToEven);
        private static int GetHeightByAspectRatio(int Width, double AspectRatio) => (int)Math.Round(Width * 1 / AspectRatio, MidpointRounding.ToEven);

        public void Draw(ElementDrawArgs DA, MGElement Element, Rectangle Bounds)
        {
            if (DA.Opacity > 0 && !DA.Opacity.IsAlmostZero())
            {
                double AspectRatio = UnstretchedAspectRatio;
                Rectangle Destination;
                if (Stretch == Stretch.None)
                {
                    Destination = MGElement.ApplyAlignment(Bounds, HorizontalAlignment.Center, VerticalAlignment.Center, new Size(UnstretchedWidth, UnstretchedHeight));
                }
                else if (Stretch == Stretch.Uniform)
                {
                    int AvailableWidth = Bounds.Width;
                    int AvailableHeight = Bounds.Height;
                    int ConsumedWidth = Math.Min(AvailableWidth, GetWidthByAspectRatio(AvailableHeight, AspectRatio));
                    int ConsumedHeight = Math.Min(AvailableHeight, GetHeightByAspectRatio(AvailableWidth, AspectRatio));
                    Destination = MGElement.ApplyAlignment(Bounds, HorizontalAlignment.Center, VerticalAlignment.Center, new Size(ConsumedWidth, ConsumedHeight));
                }
                else if (Stretch == Stretch.UniformToFill)
                {
                    int AvailableWidth = Bounds.Width;
                    int AvailableHeight = Bounds.Height;
                    int ConsumedWidth = Math.Max(AvailableWidth, GetWidthByAspectRatio(AvailableHeight, AspectRatio));
                    int ConsumedHeight = Math.Max(AvailableHeight, GetHeightByAspectRatio(AvailableWidth, AspectRatio));
                    Destination = MGElement.ApplyAlignment(Bounds, HorizontalAlignment.Center, VerticalAlignment.Center, new Size(ConsumedWidth, ConsumedHeight));
                }
                else if (Stretch == Stretch.Fill)
                {
                    Destination = Bounds;
                }
                else
                {
                    throw new NotImplementedException($"Unrecognized {nameof(Stretch)}: {Stretch}");
                }

                DA.DT.DrawTextureTo(Source.Texture, Source.SourceRect, Destination.GetTranslated(DA.Offset), Color * DA.Opacity * Source.Opacity);
            }
        }

        public IFillBrush Copy() => new MGTextureFillBrush(Source, Stretch, Color);
    }

}
