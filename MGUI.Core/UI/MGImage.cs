using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MGUI.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

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
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _SourceName;
        /// <summary>The name of the <see cref="MGTextureData"/> resource to draw, 
        /// or null if the <see cref="MGTextureData"/> is explicitly specified via <see cref="Source"/>.<br/>
        /// This resource is retrieved from <see cref="MGResources.Textures"/><para/>
        /// See also:<br/><see cref="MGElement.GetResources"/><br/><see cref="MGResources.Textures"/><br/><see cref="MGResources.AddTexture(string, MGTextureData)"/></summary>
        public string SourceName
        {
            get => _SourceName;
            set
            {
                if (_SourceName != value)
                {
                    MGResources Resources = GetResources();
                    if (SourceName != null)
                    {
                        Resources.OnTextureAdded -= Resources_OnTextureAddedRemoved;
                        Resources.OnTextureRemoved -= Resources_OnTextureAddedRemoved;
                    }
                    _SourceName = value;
                    NPC(nameof(SourceName));
                    if (SourceName != null)
                    {
                        Resources.OnTextureAdded += Resources_OnTextureAddedRemoved;
                        Resources.OnTextureRemoved += Resources_OnTextureAddedRemoved;
                    }
                    UpdateActualSource();
                }
            }
        }

        private void Resources_OnTextureAddedRemoved(object sender, (string Name, MGTextureData Data) e)
        {
            if (Name == SourceName)
                UpdateActualSource();
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private MGTextureData? _Source;
        /// <summary>The <see cref="MGTextureData"/> resource to draw,
        /// or null if the <see cref="MGTextureData"/> is instead referenced by name via <see cref="SourceName"/>.<para/>
        /// See also: <see cref="SourceName"/>, <see cref="ActualSource"/></summary>
        public MGTextureData? Source
        {
            get => _Source;
            set
            {
                if (_Source != value)
                {
                    _Source = value;
                    NPC(nameof(Source));
                    UpdateActualSource();
                }
            }
        }

        private void UpdateActualSource()
        {
            if (Source != null)
                _ActualSource = Source;
            else if (GetResources().TryGetTexture(SourceName, out MGTextureData Texture))
                _ActualSource = Texture;
            else
                _ActualSource = null;
        }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private MGTextureData? _ActualSource;
        /// <summary>Prioritizes <see cref="Source"/> if specified, otherwise attempts to retrieve the named texture resource from <see cref="MGResources.Textures"/> based on <see cref="SourceName"/></summary>
        public MGTextureData? ActualSource
        {
            get => _ActualSource;
            private set
            {
                if (ActualSource != value)
                {
                    Size? PreviousSize = ActualSource?.RenderSize;
                    _ActualSource = value;
                    NPC(nameof(ActualSource));
                    if (ActualSource?.RenderSize != PreviousSize)
                        LayoutChanged(this, true);
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Color? _TextureColor;
        /// <summary>A color to use when drawing the texture. Uses <see cref="Color.White"/> if null.</summary>
        public Color? TextureColor
        {
            get => _TextureColor;
            set
            {
                if (_TextureColor != value)
                {
                    _TextureColor = value;
                    NPC(nameof(TextureColor));
                }
            }
        }

        private int UnstretchedWidth => ActualSource?.RenderSize.Width ?? 0;
        private int UnstretchedHeight => ActualSource?.RenderSize.Height ?? 0;
        private double UnstretchedAspectRatio => UnstretchedHeight == 0 ? 1.0 : UnstretchedWidth * 1.0 / UnstretchedHeight;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Stretch _Stretch;
        public Stretch Stretch
        {
            get => _Stretch;
            set
            {
                if (_Stretch != value)
                {
                    _Stretch = value;
                    NPC(nameof(Stretch));
                    LayoutChanged(this, true);
                }
            }
        }

        //  I'm too lazy to try implementing this. Nobody will care, right?
        /*[DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private StretchDirection _StretchDirection;
        public StretchDirection StretchDirection
        {
            get => _StretchDirection;
            set
            {
                if (_StretchDirection != value)
                {
                    _StretchDirection = value;
                    NPC(nameof(StretchDirection));
                    LayoutChanged(this, true);
                }
            }
        }*/

        /// <param name="SourceName">The name of the <see cref="MGTextureData"/> in <see cref="MGResources.Textures"/> that should be drawn by this <see cref="MGImage"/>.<para/>
        /// See also: <see cref="MGElement.GetResources"/>, <see cref="MGResources.Textures"/>, <see cref="MGImage.SourceName"/></param>
        public MGImage(MGWindow Window, string SourceName, Stretch Stretch = Stretch.Uniform)
            : base(Window, MGElementType.Image)
        {
            using (BeginInitializing())
            {
                this.SourceName = SourceName;
                this.TextureColor = null;
                this.Stretch = Stretch;
            }
        }

        public MGImage(MGWindow Window, Texture2D Texture, Rectangle? SourceRect = null, Color? TextureColor = null, Stretch Stretch = Stretch.Uniform)
            : this(Window, new MGTextureData(Texture, SourceRect), TextureColor, Stretch) { }

        public MGImage(MGWindow Window, MGTextureData Source, Color? TextureColor = null, Stretch Stretch = Stretch.Uniform)
            : base(Window, MGElementType.Image)
        {
            using (BeginInitializing())
            {
                this.Source = Source;
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
            if (ActualSource?.Texture == null)
                return;

            Rectangle PaddedBounds = LayoutBounds.GetCompressed(Padding);
            double AspectRatio = UnstretchedAspectRatio;

            Rectangle Bounds;
            if (Stretch == Stretch.None)
            {
                Bounds = ApplyAlignment(PaddedBounds, HorizontalAlignment.Center, VerticalAlignment.Center, new Size(UnstretchedWidth, UnstretchedHeight));
            }
            else if (Stretch == Stretch.Uniform)
            {
                int AvailableWidth = PaddedBounds.Width;
                int AvailableHeight = PaddedBounds.Height;
                int ConsumedWidth = Math.Min(AvailableWidth, GetWidthByAspectRatio(AvailableHeight, AspectRatio));
                int ConsumedHeight = Math.Min(AvailableHeight, GetHeightByAspectRatio(AvailableWidth, AspectRatio));
                Bounds = ApplyAlignment(PaddedBounds, HorizontalAlignment.Center, VerticalAlignment.Center, new Size(ConsumedWidth, ConsumedHeight));
            }
            else if (Stretch == Stretch.UniformToFill)
            {
                int AvailableWidth = PaddedBounds.Width;
                int AvailableHeight = PaddedBounds.Height;
                int ConsumedWidth = Math.Max(AvailableWidth, GetWidthByAspectRatio(AvailableHeight, AspectRatio));
                int ConsumedHeight = Math.Max(AvailableHeight, GetHeightByAspectRatio(AvailableWidth, AspectRatio));
                Bounds = ApplyAlignment(PaddedBounds, HorizontalAlignment.Center, VerticalAlignment.Center, new Size(ConsumedWidth, ConsumedHeight));
            }
            else if (Stretch == Stretch.Fill)
            {
                Bounds = PaddedBounds;
            }
            else
            {
                throw new NotImplementedException($"Unrecognized {nameof(Stretch)}: {Stretch}");
            }

            DA.DT.DrawTextureTo(ActualSource.Value.Texture, ActualSource.Value.SourceRect, Bounds.GetTranslated(DA.Offset), (this.TextureColor ?? Color.White) * DA.Opacity);
        }
    }
}
