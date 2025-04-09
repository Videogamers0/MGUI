using MGUI.Shared.Helpers;
using MGUI.Shared.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI.Brushes.Fill_Brushes
{
    /// <summary>An <see cref="IFillBrush"/> that draws a nine-sliced (also called a nine-patch) texture to the destination bounds using a customizable margin to control how each patch scales to the bounds.<para/>
    /// See also: <see href="https://en.wikipedia.org/wiki/9-slice_scaling"/></summary>
    public readonly struct MGNineSliceFillBrush : IFillBrush
    {
        public readonly Thickness TargetMargin;

        public readonly MGTextureData TopLeft;
        public readonly MGTextureData TopCenter;
        public readonly MGTextureData TopRight;
        public readonly MGTextureData MiddleLeft;
        public readonly MGTextureData MiddleCenter;
        public readonly MGTextureData MiddleRight;
        public readonly MGTextureData BottomLeft;
        public readonly MGTextureData BottomCenter;
        public readonly MGTextureData BottomRight;

        /// <param name="Source">The texture that will be divided up into 9 rectangular regions.</param>
        /// <param name="TargetMargin">Determines the size of each slice when rendering the texture to the destination bounds.<para/>
        /// EX: If margin.Left=10 and margin.Top=16, the top-left region of the source texture will be drawn to the topleft 10x16 pixels of the destination bounds whenever this brush is rendered.</param>
        /// <param name="SourceMargin">Optional. Determines how the source texture is divided up into 9 rectangular regions.<para/>
        /// If <see langword="null"/>, each region of the source texture is assumed to be equally-sized (and thus its dimensions should be an exact multiple of 3)</param>
        public MGNineSliceFillBrush(Thickness TargetMargin, MGTextureData Source, Thickness? SourceMargin = null)
        {
            this.TargetMargin = TargetMargin;

            Texture2D Texture = Source.Texture;
            if (Texture == null)
                throw new ArgumentNullException(nameof(Source));

            Rectangle Bounds = Source.SourceRect ?? Texture.Bounds;

            //  Value the source margin
            Thickness Margin;
            if (SourceMargin.HasValue)
            {
                if (SourceMargin.Value.Sides().Any(x => x <= 0))
                    throw new InvalidDataException($"Invalid {nameof(SourceMargin)}. All sides must have a value greater than zero. Actual value: {SourceMargin.Value}");
                Margin = SourceMargin.Value;
            }
            else
            {
                if (Bounds.Width % 3 != 0 || Bounds.Height % 3 != 0)
                {
                    throw new InvalidDataException($"Invalid input texture dimensions. " +
                        $"The source texture must be evenly-divisible by 3 to calculate each of the 9 regions. Actual dimensions: {Bounds.Width}x{Bounds.Height}");
                }
                Margin = new(Bounds.Width / 3, Bounds.Height / 3);
            }

            int LeftColumnSize = Margin.Left;
            int RightColumnSize = Margin.Right;
            int CenterColumnSize = Bounds.Width - LeftColumnSize - RightColumnSize;

            int TopRowSize = Margin.Top;
            int BottomRowSize = Margin.Bottom;
            int CenterRowSize = Bounds.Height - TopRowSize - BottomRowSize;

            //  Compute the top row regions
            TopLeft = new(Texture, new(Bounds.Left, Bounds.Top, LeftColumnSize, TopRowSize), Source.Opacity, Source.RenderSizeOverride);
            TopCenter = new(Texture, new(Bounds.Left + LeftColumnSize, Bounds.Top, CenterColumnSize, TopRowSize), Source.Opacity, Source.RenderSizeOverride);
            TopRight = new(Texture, new(Bounds.Left + LeftColumnSize + CenterColumnSize, Bounds.Top, RightColumnSize, TopRowSize), Source.Opacity, Source.RenderSizeOverride);

            //  Compute the center row regions
            MiddleLeft = new(Texture, new(Bounds.Left, Bounds.Top + TopRowSize, LeftColumnSize, CenterRowSize), Source.Opacity, Source.RenderSizeOverride);
            MiddleCenter = new(Texture, new(Bounds.Left + LeftColumnSize, Bounds.Top + TopRowSize, CenterColumnSize, CenterRowSize), Source.Opacity, Source.RenderSizeOverride);
            MiddleRight = new(Texture, new(Bounds.Left + LeftColumnSize + CenterColumnSize, Bounds.Top + TopRowSize, RightColumnSize, CenterRowSize), Source.Opacity, Source.RenderSizeOverride);

            //  Compute the bottom row regions
            BottomLeft = new(Texture, new(Bounds.Left, Bounds.Top + TopRowSize + CenterRowSize, LeftColumnSize, BottomRowSize), Source.Opacity, Source.RenderSizeOverride);
            BottomCenter = new(Texture, new(Bounds.Left + LeftColumnSize, Bounds.Top + TopRowSize + CenterRowSize, CenterColumnSize, BottomRowSize), Source.Opacity, Source.RenderSizeOverride);
            BottomRight = new(Texture, new(Bounds.Left + LeftColumnSize + CenterColumnSize, Bounds.Top + TopRowSize + CenterRowSize, RightColumnSize, BottomRowSize), Source.Opacity, Source.RenderSizeOverride);
        }

        /// <param name="TargetMargin">Determines the size of each slice when rendering the texture to the destination bounds.<para/>
        /// EX: If margin.Left=10 and margin.Top=16, the top-left region of the source texture will be drawn to the topleft 10x16 pixels of the destination bounds whenever this brush is rendered.</param>
        public MGNineSliceFillBrush(Thickness TargetMargin,
            MGTextureData TopLeft, MGTextureData TopCenter, MGTextureData TopRight,
            MGTextureData MiddleLeft, MGTextureData MiddleCenter, MGTextureData MiddleRight,
            MGTextureData BottomLeft, MGTextureData BottomCenter, MGTextureData BottomRight)
        {
            this.TargetMargin = TargetMargin;

            this.TopLeft = TopLeft;
            this.TopCenter = TopCenter;
            this.TopRight = TopRight;
            this.MiddleLeft = MiddleLeft;
            this.MiddleCenter = MiddleCenter;
            this.MiddleRight = MiddleRight;
            this.BottomLeft = BottomLeft;
            this.BottomCenter = BottomCenter;
            this.BottomRight = BottomRight;
        }

        public IFillBrush Copy() => new MGNineSliceFillBrush(TargetMargin, TopLeft, TopCenter, TopRight, MiddleLeft, MiddleCenter, MiddleRight, BottomLeft, BottomCenter, BottomRight);

        public void Draw(ElementDrawArgs DA, MGElement Element, Rectangle Bounds)
        {
            DrawTransaction DT = DA.DT;

            int LeftColumnSize = TargetMargin.Left;
            int RightColumnSize = TargetMargin.Right;
            int CenterColumnSize = Bounds.Width - LeftColumnSize - RightColumnSize;

            int TopRowSize = TargetMargin.Top;
            int BottomRowSize = TargetMargin.Bottom;
            int CenterRowSize = Bounds.Height - TopRowSize - BottomRowSize;

            //  Draw the top row
            if (TopRowSize > 0)
            {
                if (LeftColumnSize > 0)
                    TopLeft.Draw(DT, new Rectangle(Bounds.Left, Bounds.Top, LeftColumnSize, TopRowSize), null, DA.Opacity);
                if (CenterColumnSize > 0)
                    TopCenter.Draw(DT, new Rectangle(Bounds.Left + LeftColumnSize, Bounds.Top, CenterColumnSize, TopRowSize), null, DA.Opacity);
                if (RightColumnSize > 0)
                    TopRight.Draw(DT, new Rectangle(Bounds.Left + LeftColumnSize + CenterColumnSize, Bounds.Top, RightColumnSize, TopRowSize), null, DA.Opacity);
            }

            //  Draw the center row
            if (CenterRowSize > 0)
            {
                if (LeftColumnSize > 0)
                    MiddleLeft.Draw(DT, new Rectangle(Bounds.Left, Bounds.Top + TopRowSize, LeftColumnSize, CenterRowSize), null, DA.Opacity);
                if (CenterColumnSize > 0)
                {
#if NEVER   // trying to tile the texture but LinearWrap isn't working unless I draw the entire texture, using the destination's width/height as the SourceRect
            // But the problem is I only want to tile the center region of the input texture, not the entire thing...
                    using (DT.SetDrawSettingsTemporary(DT.CurrentSettings with { SamplerType = SamplerType.LinearWrap }))
                    {
                        Rectangle Destination = new Rectangle(Bounds.Left + LeftColumnSize, Bounds.Top + TopRowSize, CenterColumnSize, CenterRowSize);

                        Rectangle ActualSourceRect = MiddleCenter.SourceRect.Value;
                        Rectangle? SourceRect = new Rectangle(ActualSourceRect.X, ActualSourceRect.Y, Destination.Width, Destination.Height); // MiddleCenter.SourceRect;
                        DT.DrawTextureTo(MiddleCenter.Texture, SourceRect, Destination, Color.White * MiddleCenter.Opacity * DA.Opacity);
                    }
#else
                    MiddleCenter.Draw(DT, new Rectangle(Bounds.Left + LeftColumnSize, Bounds.Top + TopRowSize, CenterColumnSize, CenterRowSize), null, DA.Opacity);
#endif
                }
                if (RightColumnSize > 0)
                    MiddleRight.Draw(DT, new Rectangle(Bounds.Left + LeftColumnSize + CenterColumnSize, Bounds.Top + TopRowSize, RightColumnSize, CenterRowSize), null, DA.Opacity);
            }

            //  Draw the bottom row
            if (BottomRowSize > 0)
            {
                if (LeftColumnSize > 0)
                    BottomLeft.Draw(DT, new Rectangle(Bounds.Left, Bounds.Top + TopRowSize + CenterRowSize, LeftColumnSize, BottomRowSize), null, DA.Opacity);
                if (CenterColumnSize > 0)
                    BottomCenter.Draw(DT, new Rectangle(Bounds.Left + LeftColumnSize, Bounds.Top + TopRowSize + CenterRowSize, CenterColumnSize, BottomRowSize), null, DA.Opacity);
                if (RightColumnSize > 0)
                    BottomRight.Draw(DT, new Rectangle(Bounds.Left + LeftColumnSize + CenterColumnSize, Bounds.Top + TopRowSize + CenterRowSize, RightColumnSize, BottomRowSize), null, DA.Opacity);
            }
        }
    }
}