using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MGUI.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MGUI.Core.UI
{
    /// <summary>Describes how content is resized to fill its allocated space.</summary>
    public enum Stretch
    {
        /// <summary>The content preserves its original size.</summary>
        None = 0,
        /// <summary>The content is resized to fill the destination dimensions. The aspect ratio is not preserved.</summary>
        Fill = 1,
        /// <summary>The content is resized to fit in the destination dimensions while it preserves its native aspect ratio.</summary>
        Uniform = 2,
        /// <summary>The content is resized to fill the destination dimensions while it preserves its native aspect ratio. 
        /// If the aspect ratio of the destination rectangle differs from the source, the source content is clipped to fit in the destination dimensions.</summary>
        UniformToFill = 3
    }

    /// <summary>Describes how scaling applies to content and restricts scaling to named axis types.</summary>
    public enum StretchDirection
    {
        /// <summary>The content scales upward only when it is smaller than the parent. If the content is larger, no scaling downward is performed.</summary>
        UpOnly = 0,
        /// <summary>The content scales downward only when it is larger than the parent. If the content is smaller, no scaling upward is performed.</summary>
        DownOnly = 1,
        /// <summary>The content stretches to fit the parent according to the Stretch mode.</summary>
        Both = 2
    }

    public class MGImage : MGElement
    {
        private Texture2D _Texture;
        public Texture2D Texture { get => _Texture; }

        private Rectangle? _SourceRect;
        public Rectangle? SourceRect { get => _SourceRect; }

        /// <summary>A color to use when drawing the texture. Uses <see cref="Color.White"/> if null.</summary>
        public Color? TextureColor { get; set; }

        private int UnstretchedWidth => SourceRect?.Width ?? Texture?.Width ?? 0;
        private int UnstretchedHeight => SourceRect?.Height ?? Texture?.Height ?? 0;
        private double UnstretchedAspectRatio => UnstretchedHeight == 0 ? 1.0 : UnstretchedWidth * 1.0 / UnstretchedHeight;

        public void SetTexture(Texture2D Texture, Rectangle? SourceRect)
        {
            if (_Texture != Texture || _SourceRect != SourceRect)
            {
                int PreviousWidth = UnstretchedWidth;
                int PreviousHeight = UnstretchedHeight;

                _Texture = Texture;
                _SourceRect = SourceRect;

                if (PreviousWidth != UnstretchedWidth || PreviousHeight != UnstretchedHeight)
                    LayoutChanged(this, true);
            }
        }

        private Stretch _Stretch;
        public Stretch Stretch
        {
            get => _Stretch;
            set
            {
                if (_Stretch != value)
                {
                    _Stretch = value;
                    LayoutChanged(this, true);
                }
            }
        }

        //  I'm too lazy to try implementing this. Nobody will care, right?
        /*private StretchDirection _StretchDirection;
        public StretchDirection StretchDirection
        {
            get => _StretchDirection;
            set
            {
                if (_StretchDirection != value)
                {
                    _StretchDirection = value;
                    LayoutChanged(this, true);
                }
            }
        }*/

        /// <param name="RegionName">The name of the <see cref="NamedTextureRegion"/> used to reference the texture settings by. This name must exist in <see cref="MGDesktop.NamedRegions"/><para/>
        /// See also: <see cref="MGDesktop.NamedTextures"/>, <see cref="MGDesktop.NamedRegions"/></param>
        public MGImage(MGWindow Window, string RegionName, Stretch Stretch = Stretch.Uniform)
            : base(Window, MGElementType.Image)
        {
            using (BeginInitializing())
            {
                MGDesktop Desktop = Window.GetDesktop();
                NamedTextureRegion Region = Desktop.NamedRegions[RegionName];

                SetTexture(Desktop.NamedTextures[Region.TextureName], Region.SourceRect);
                this.TextureColor = Region.Color;
                this.Stretch = Stretch;
            }
        }

        public MGImage(MGWindow Window, Texture2D Texture, Rectangle? SourceRect = null, Color? TextureColor = null, Stretch Stretch = Stretch.Uniform)
            : base(Window, MGElementType.Image)
        {
            using (BeginInitializing())
            {
                SetTexture(Texture, SourceRect);
                this.TextureColor = TextureColor;
                this.Stretch = Stretch;
            }
        }

        private static int GetWidthByAspectRatio(int Height, double AspectRatio) => (int)Math.Round(Height * AspectRatio, MidpointRounding.ToEven);
        private static int GetHeightByAspectRatio(int Width, double AspectRatio) => (int)Math.Round(Width * 1 / AspectRatio, MidpointRounding.ToEven);

        public override Thickness MeasureSelfOverride(Size AvailableSize, out Thickness SharedSize)
        {
            SharedSize = new(0);

            int AvailableWidth = AvailableSize.Width;
            int AvailableHeight = AvailableSize.Height;

            //  If Width or Height is arbitrarily large, this element is being measured within a ScrollViewer.
            //  That means we can't just request the AvailableSize, or we'd end up with infinitely-sized content inside the ScrollViewer.
            bool IsPseudoInfiniteWidth = AvailableWidth >= 1000000;
            bool IsPseduoInfiniteHeight = AvailableHeight >= 1000000;

            int Width;
            int Height;

            double AspectRatio = UnstretchedAspectRatio;
            if (Stretch == Stretch.None)
            {
                Width = UnstretchedWidth;
                Height = UnstretchedHeight;
            }
            else if (Stretch == Stretch.Uniform)
            {
                //int Width = Math.Min(AvailableSize.Width, UnstretchedWidth);
                //int Height = (int)Math.Round(Math.Min(AvailableSize.Width, UnstretchedWidth) * 1 / UnstretchedAspectRatio, MidpointRounding.ToEven);

                if (IsPseudoInfiniteWidth && IsPseudoInfiniteWidth)
                {
                    //  If both dimensions are infinite, it's ambiguous as to how much space to request
                    Width = UnstretchedWidth;
                    Height = UnstretchedHeight;
                }
                else if (IsPseudoInfiniteWidth)
                {
                    Height = AvailableHeight;
                    Width = GetWidthByAspectRatio(Height, AspectRatio);
                }
                else if (IsPseduoInfiniteHeight)
                {
                    Width = AvailableWidth;
                    Height = GetHeightByAspectRatio(Width, AspectRatio);
                }
                else
                {
                    Width = Math.Min(AvailableWidth, GetWidthByAspectRatio(AvailableHeight, AspectRatio));
                    Height = Math.Min(AvailableHeight, GetHeightByAspectRatio(AvailableWidth, AspectRatio));
                }
            }
            else if (Stretch == Stretch.UniformToFill)
            {
                //int Width = Math.Min(AvailableSize.Width, UnstretchedWidth);
                //int Height = Math.Max(UnstretchedHeight, (int)Math.Round(Math.Min(AvailableSize.Width, UnstretchedWidth) * 1 / UnstretchedAspectRatio, MidpointRounding.ToEven));

                if (IsPseudoInfiniteWidth && IsPseudoInfiniteWidth)
                {
                    Width = UnstretchedWidth;
                    Height = UnstretchedHeight;
                }
                else if (IsPseudoInfiniteWidth)
                {
                    Height = AvailableHeight;
                    Width = GetWidthByAspectRatio(Height, AspectRatio);
                }
                else if (IsPseduoInfiniteHeight)
                {
                    Width = AvailableWidth;
                    Height = GetHeightByAspectRatio(Width, AspectRatio);
                }
                else
                {
                    Width = Math.Max(AvailableWidth, GetWidthByAspectRatio(AvailableHeight, AspectRatio));
                    Height = Math.Max(AvailableHeight, GetHeightByAspectRatio(AvailableWidth, AspectRatio));
                }
            }
            else if (Stretch == Stretch.Fill)
            {
                if (IsPseudoInfiniteWidth && IsPseudoInfiniteWidth)
                {
                    Width = UnstretchedWidth;
                    Height = UnstretchedHeight;
                }
                else if (IsPseudoInfiniteWidth)
                {
                    Width = UnstretchedWidth;
                    Height = AvailableHeight;
                }
                else if (IsPseduoInfiniteHeight)
                {
                    Width = AvailableWidth;
                    Height = UnstretchedHeight;
                }
                else
                {
                    Width = AvailableWidth;
                    Height = AvailableHeight;
                }
            }
            else
            {
                throw new NotImplementedException($"Unrecognized {nameof(Stretch)}: {Stretch}");
            }

            return new Thickness(Width, Height, 0, 0);
        }

        public override void DrawSelf(ElementDrawArgs DA, Rectangle LayoutBounds)
        {
            if (Texture == null)
                return;

            double AspectRatio = UnstretchedAspectRatio;

            Rectangle Bounds;
            if (Stretch == Stretch.None)
            {
                Bounds = ApplyAlignment(LayoutBounds, HorizontalAlignment.Center, VerticalAlignment.Center, new Size(UnstretchedWidth, UnstretchedHeight));
            }
            else if (Stretch == Stretch.Uniform)
            {
                int AvailableWidth = LayoutBounds.Width;
                int AvailableHeight = LayoutBounds.Height;
                int ConsumedWidth = Math.Min(AvailableWidth, GetWidthByAspectRatio(AvailableHeight, AspectRatio));
                int ConsumedHeight = Math.Min(AvailableHeight, GetHeightByAspectRatio(AvailableWidth, AspectRatio));
                Bounds = ApplyAlignment(LayoutBounds, HorizontalAlignment.Center, VerticalAlignment.Center, new Size(ConsumedWidth, ConsumedHeight));
            }
            else if (Stretch == Stretch.UniformToFill)
            {
                int AvailableWidth = LayoutBounds.Width;
                int AvailableHeight = LayoutBounds.Height;
                int ConsumedWidth = Math.Max(AvailableWidth, GetWidthByAspectRatio(AvailableHeight, AspectRatio));
                int ConsumedHeight = Math.Max(AvailableHeight, GetHeightByAspectRatio(AvailableWidth, AspectRatio));
                Bounds = ApplyAlignment(LayoutBounds, HorizontalAlignment.Center, VerticalAlignment.Center, new Size(ConsumedWidth, ConsumedHeight));
            }
            else if (Stretch == Stretch.Fill)
            {
                Bounds = LayoutBounds;
            }
            else
            {
                throw new NotImplementedException($"Unrecognized {nameof(Stretch)}: {Stretch}");
            }

            DA.DT.DrawTextureTo(Texture, SourceRect, Bounds.GetTranslated(DA.Offset), this.TextureColor ?? Color.White * DA.Opacity);
        }
    }
}
