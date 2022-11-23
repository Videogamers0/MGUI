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
        public readonly Texture2D Texture;
        public readonly Rectangle? SourceRect;
        public readonly Stretch Stretch;
        public readonly float Opacity;

        //TODO Option to make it tesselate if Stretch is not Fill/UniformToFill? (LinearWrap SamplerState?)
        //      So if Stretch is None or UniformToFill, the empty space would instead be filled up with LinearWrap texture

        /// <param name="RegionName">The name of the <see cref="NamedTextureRegion"/> used to reference the texture settings by. This name must exist in <see cref="MGDesktop.NamedRegions"/><para/>
        /// See also: <see cref="MGDesktop.NamedTextures"/>, <see cref="MGDesktop.NamedRegions"/></param>
        public MGTextureFillBrush(MGDesktop Desktop, string RegionName, Stretch Stretch = Stretch.Fill, float Opacity = 1.0f)
            : this(Desktop.NamedTextures[Desktop.NamedRegions[RegionName].TextureName], Desktop.NamedRegions[RegionName].SourceRect, Stretch, Opacity) { }

        public MGTextureFillBrush(Texture2D Texture, Rectangle? SourceRect, Stretch Stretch = Stretch.Fill, float Opacity = 1.0f)
        {
            this.Texture = Texture;
            this.SourceRect = SourceRect;
            this.Stretch = Stretch;
            this.Opacity = Opacity;
        }

        private int UnstretchedWidth => SourceRect?.Width ?? Texture?.Width ?? 0;
        private int UnstretchedHeight => SourceRect?.Height ?? Texture?.Height ?? 0;
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

                DA.DT.DrawTextureTo(Texture, SourceRect, Destination.GetTranslated(DA.Offset), Color.White * DA.Opacity * this.Opacity);
            }
        }

        public IFillBrush Copy() => new MGTextureFillBrush(Texture, SourceRect, Stretch, Opacity);
    }

}
