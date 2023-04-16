using MGUI.Shared.Helpers;
using MGUI.Shared.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI.Brushes.Border_Brushes
{
    public enum Corner
    {
        TopLeft,
        TopRight,
        BottomRight,
        BottomLeft
    }

    public enum Edge
    {
        Left,
        Top,
        Right,
        Bottom
    }

    public readonly record struct TextureTransforms(EdgeTransforms EdgeTransforms = new(), CornerTransforms CornerTransforms = new())
    {
        private static readonly float PI = (float)Math.PI;

        /// <param name="EdgeBasis">The <see cref="Edge"/> that corresponds to a 0° rotation. The next clockwise <see cref="Edge"/> will be rotated by 90°, and the one after that 180° etc.</param>
        /// <param name="CornerBasis">The <see cref="Corner"/> that corresponds to a 0° rotation. The next clockwise <see cref="Corner"/> will be rotated by 90°, and the one after that 180° etc.</param>
        public static TextureTransforms CreateStandardRotated(Edge EdgeBasis = Edge.Left, Corner CornerBasis = Corner.TopLeft)
        {
            EdgeTransforms EdgeTransforms = EdgeBasis switch
            {
                Edge.Left => new(0, PI / 2.0f, PI, PI / 2.0f * 3),
                Edge.Top => new(PI / 2.0f * 3, 0, PI / 2.0f, PI),
                Edge.Right => new(PI, PI / 2.0f * 3, 0, PI / 2.0f),
                Edge.Bottom => new(PI / 2.0f, PI, PI / 2.0f * 3, 0),
                _ => throw new NotImplementedException($"Unrecognized {nameof(Edge)}: {EdgeBasis}")
            };

            CornerTransforms CornerTransforms = CornerBasis switch
            {
                Corner.TopLeft => new(0, PI / 2.0f, PI, PI / 2.0f * 3),
                Corner.TopRight => new(PI / 2.0f * 3, 0, PI / 2.0f, PI),
                Corner.BottomRight => new(PI, PI / 2.0f * 3, 0, PI / 2.0f),
                Corner.BottomLeft => new(PI / 2.0f, PI, PI / 2.0f * 3, 0),
                _ => throw new NotImplementedException($"Unrecognized {nameof(Corner)}: {CornerBasis}")
            };

            return new TextureTransforms(EdgeTransforms, CornerTransforms);
        }
    }

    /// <summary>Represents rotations and reflections to apply to each edge when rendering the <see cref="MGTexturedBorderBrush.EdgeTexture"/></summary>
    /// <param name="LeftRotation">Rotation in radians</param>
    /// <param name="TopRotation">Rotation in radians</param>
    /// <param name="RightRotation">Rotation in radians</param>
    /// <param name="BottomRotation">Rotation in radians</param>
    public readonly record struct EdgeTransforms(
        float LeftRotation = 0, float TopRotation = 0, float RightRotation = 0, float BottomRotation = 0,
        SpriteEffects LeftReflections = SpriteEffects.None, SpriteEffects TopReflections = SpriteEffects.None,
        SpriteEffects RightReflections = SpriteEffects.None, SpriteEffects BottomReflections = SpriteEffects.None)
    {
        public bool HasLeftRotation => !LeftRotation.IsAlmostZero();
        public bool HasTopRotation => !TopRotation.IsAlmostZero();
        public bool HasRightRotation => !RightRotation.IsAlmostZero();
        public bool HasBottomRotation => !BottomRotation.IsAlmostZero();
    }

    /// <summary>Represents rotations and reflections to apply to each corner when rendering the <see cref="MGTexturedBorderBrush.CornerTexture"/></summary>
    /// <param name="TopLeftRotation">Rotation in radians</param>
    /// <param name="TopRightRotation">Rotation in radians</param>
    /// <param name="BottomRightRotation">Rotation in radians</param>
    /// <param name="BottomLeftRotation">Rotation in radians</param>
    public readonly record struct CornerTransforms(
        float TopLeftRotation = 0, float TopRightRotation = 0, float BottomRightRotation = 0, float BottomLeftRotation = 0,
        SpriteEffects TopLeftReflections = SpriteEffects.None, SpriteEffects TopRightReflections = SpriteEffects.None,
        SpriteEffects BottomRightReflections = SpriteEffects.None, SpriteEffects BottomLeftReflections = SpriteEffects.None)
    {
        public bool HasTopLeftRotation => !TopLeftRotation.IsAlmostZero();
        public bool HasTopRightRotation => !TopRightRotation.IsAlmostZero();
        public bool HasBottomRightRotation => !BottomRightRotation.IsAlmostZero();
        public bool HasBottomLeftRotation => !BottomLeftRotation.IsAlmostZero();
    }

    /// <summary>An <see cref="IBorderBrush"/> that is composed of 2 textures:<br/>
    /// 1 for the edges (Left, Top, Right, Bottom), and 1 for the corners (TopLeft, TopRight, BottomRight, BottomLeft)<para/>
    /// This brush renders the edge texture on all 4 edges, then renders the corner texture on all 4 corners, optionally applying reflections or rotations on each part</summary>
    public readonly struct MGTexturedBorderBrush : IBorderBrush
    {
        public readonly MGTextureData EdgeTexture;
        /// <summary>A color mask to use when drawing the <see cref="EdgeTexture"/>. Usually <see cref="Color.White"/></summary>
        public readonly Color EdgeColor;

        public readonly MGTextureData CornerTexture;
        /// <summary>A color mask to use when drawing the <see cref="CornerTexture"/>. Usually <see cref="Color.White"/></summary>
        public readonly Color CornerColor;

        public readonly float Opacity;

        public readonly TextureTransforms Transforms;

        public MGTexturedBorderBrush()
        {
            this.EdgeTexture = default;
            this.EdgeColor = Color.White;

            this.CornerTexture = default;
            this.CornerColor = Color.White;

            this.Opacity = 1.0f;

            this.Transforms = new();
        }

        /// <param name="EdgeTextureName">The name of the <see cref="MGTextureData"/> used to reference the <see cref="EdgeTexture"/> settings by.<br/>
        /// This name must exist in <see cref="MGResources.Textures"/><para/>
        /// See also: <see cref="MGElement.GetResources"/>, <see cref="MGDesktop.Resources"/>, <see cref="MGResources.Textures"/></param>
        /// <param name="CornerTextureName">The name of the <see cref="MGTextureData"/> used to reference the <see cref="CornerTexture"/> settings by.<br/>
        /// This name must exist in <see cref="MGResources.Textures"/><para/>
        /// See also: <see cref="MGElement.GetResources"/>, <see cref="MGDesktop.Resources"/>, <see cref="MGResources.Textures"/></param>
        public MGTexturedBorderBrush(MGDesktop Desktop, string EdgeTextureName, string CornerTextureName, Color? EdgeColor = null, Color? CornerColor = null, TextureTransforms? Transforms = null, float Opacity = 1.0f)
            : this(Desktop.Resources.Textures[EdgeTextureName], EdgeColor, Desktop.Resources.Textures[CornerTextureName], CornerColor, Transforms, Opacity) { }

        public MGTexturedBorderBrush(Texture2D EdgeTexture, Texture2D CornerTexture, TextureTransforms? Transforms = null, float Opacity = 1.0f)
            : this(new MGTextureData(EdgeTexture), null, new MGTextureData(CornerTexture), null, Transforms, Opacity) { }

        public MGTexturedBorderBrush(MGTextureData EdgeTexture, Color? EdgeColor, MGTextureData CornerTexture, Color? CornerColor, 
            TextureTransforms? Transforms = null, float Opacity = 1.0f)
        {
            this.EdgeTexture = EdgeTexture;
            this.EdgeColor = EdgeColor ?? Color.White;

            this.CornerTexture = CornerTexture;
            this.CornerColor = CornerColor ?? Color.White;

            this.Opacity = Opacity;

            this.Transforms = Transforms ?? new();
        }

        public void Draw(ElementDrawArgs DA, MGElement Element, Rectangle Bounds, Thickness BT)
        {
            DrawTransaction DT = DA.DT;
            float Opacity = DA.Opacity * this.Opacity;

            if (EdgeTexture.Texture?.IsDisposed == false)
            {
                EdgeTransforms EdgeTransforms = Transforms.EdgeTransforms;
                Color EdgeColor = this.EdgeColor * Opacity * EdgeTexture.Opacity;

                int SourceWidth = EdgeTexture.RenderSize.Width;
                int SourceHeight = EdgeTexture.RenderSize.Height;
                Vector2 Origin = new(SourceWidth / 2f, SourceHeight / 2f);

                Vector2 Scale;

                //  A very small amount to increase scale values by to hopefully avoid off-by-1 pixel rounding issues when rotating
                const float ScaleOffset = 0.0001f;

                Rectangle LeftBounds = new(Bounds.Left, Bounds.Top + BT.Top, BT.Left, Bounds.Height - BT.Height);
                if (!EdgeTransforms.HasLeftRotation)
                    DT.DrawTextureTo(EdgeTexture.Texture, EdgeTexture.SourceRect, LeftBounds, EdgeColor, Vector2.Zero, 0, 0, EdgeTransforms.LeftReflections);
                else
                {
                    Rectangle RotatedLeftBounds = LeftBounds.CreateTransformed(Matrix.CreateRotationZ(EdgeTransforms.LeftRotation));
                    Scale = new(Math.Abs(RotatedLeftBounds.Width) / (float)SourceWidth + ScaleOffset, Math.Abs(RotatedLeftBounds.Height) / (float)SourceHeight + ScaleOffset);
                    DT.DrawTextureAt(EdgeTexture.Texture, EdgeTexture.SourceRect, LeftBounds.Center.ToVector2(),
                        EdgeColor, Origin, EdgeTransforms.LeftRotation, Scale.X, Scale.Y, 0, EdgeTransforms.LeftReflections);
                }

                Rectangle TopBounds = new(Bounds.Left + BT.Left, Bounds.Top, Bounds.Width - BT.Width, BT.Top);
                if (!EdgeTransforms.HasTopRotation)
                    DT.DrawTextureTo(EdgeTexture.Texture, EdgeTexture.SourceRect, TopBounds, EdgeColor, Vector2.Zero, 0, 0, EdgeTransforms.TopReflections);
                else
                {
                    Rectangle RotatedTopBounds = TopBounds.CreateTransformed(Matrix.CreateRotationZ(EdgeTransforms.TopRotation));
                    Scale = new(Math.Abs(RotatedTopBounds.Width) / (float)SourceWidth + ScaleOffset, Math.Abs(RotatedTopBounds.Height) / (float)SourceHeight + ScaleOffset);
                    DT.DrawTextureAt(EdgeTexture.Texture, EdgeTexture.SourceRect, TopBounds.Center.ToVector2(),
                        EdgeColor, Origin, EdgeTransforms.TopRotation, Scale.X, Scale.Y, 0, EdgeTransforms.TopReflections);
                }

                Rectangle RightBounds = new(Bounds.Right - BT.Right, Bounds.Top + BT.Top, BT.Right, Bounds.Height - BT.Height);
                if (!EdgeTransforms.HasRightRotation)
                    DT.DrawTextureTo(EdgeTexture.Texture, EdgeTexture.SourceRect, RightBounds, EdgeColor, Vector2.Zero, 0, 0, EdgeTransforms.RightReflections);
                else
                {
                    Rectangle RotatedRightBounds = RightBounds.CreateTransformed(Matrix.CreateRotationZ(EdgeTransforms.RightRotation));
                    Scale = new(Math.Abs(RotatedRightBounds.Width) / (float)SourceWidth + ScaleOffset, Math.Abs(RotatedRightBounds.Height) / (float)SourceHeight + ScaleOffset);
                    DT.DrawTextureAt(EdgeTexture.Texture, EdgeTexture.SourceRect, RightBounds.Center.ToVector2(),
                        EdgeColor, Origin, EdgeTransforms.RightRotation, Scale.X, Scale.Y, 0, EdgeTransforms.RightReflections);
                }

                Rectangle BottomBounds = new(Bounds.Left + BT.Left, Bounds.Bottom - BT.Bottom, Bounds.Width - BT.Width, BT.Bottom);
                if (!EdgeTransforms.HasBottomRotation)
                    DT.DrawTextureTo(EdgeTexture.Texture, EdgeTexture.SourceRect, BottomBounds, EdgeColor, Vector2.Zero, 0, 0, EdgeTransforms.BottomReflections);
                else
                {
                    Rectangle RotatedBottomBounds = BottomBounds.CreateTransformed(Matrix.CreateRotationZ(EdgeTransforms.BottomRotation));
                    Scale = new(Math.Abs(RotatedBottomBounds.Width) / (float)SourceWidth + ScaleOffset, Math.Abs(RotatedBottomBounds.Height) / (float)SourceHeight + ScaleOffset);
                    DT.DrawTextureAt(EdgeTexture.Texture, EdgeTexture.SourceRect, BottomBounds.Center.ToVector2(),
                        EdgeColor, Origin, EdgeTransforms.BottomRotation, Scale.X, Scale.Y, 0, EdgeTransforms.BottomReflections);
                }
            }

            if (CornerTexture.Texture?.IsDisposed == false)
            {
                CornerTransforms CornerTransforms = Transforms.CornerTransforms;
                Color CornerColor = this.CornerColor * Opacity *CornerTexture.Opacity;

                int SourceWidth = CornerTexture.RenderSize.Width;
                int SourceHeight = CornerTexture.RenderSize.Height;
                Vector2 Origin = new(SourceWidth / 2f, SourceHeight / 2f);

                bool IsUniformThickness = BT.Sides().All(x => x == BT.Left);
                if (IsUniformThickness)
                {
                    Rectangle TopLeftBounds = new Rectangle(Bounds.Left, Bounds.Top, BT.Left, BT.Top).GetTranslated(BT.Left / 2, BT.Top / 2);
                    DT.DrawTextureTo(CornerTexture.Texture, CornerTexture.SourceRect, TopLeftBounds, CornerColor, Origin, CornerTransforms.TopLeftRotation, 0, CornerTransforms.TopLeftReflections);

                    Rectangle TopRightBounds = new Rectangle(Bounds.Right - BT.Right, Bounds.Top, BT.Right, BT.Top).GetTranslated(BT.Right / 2, BT.Top / 2);
                    DT.DrawTextureTo(CornerTexture.Texture, CornerTexture.SourceRect, TopRightBounds, CornerColor, Origin, CornerTransforms.TopRightRotation, 0, CornerTransforms.TopRightReflections);

                    Rectangle BottomRightBounds = new Rectangle(Bounds.Right - BT.Right, Bounds.Bottom - BT.Bottom, BT.Right, BT.Bottom).GetTranslated(BT.Right / 2, BT.Bottom / 2);
                    DT.DrawTextureTo(CornerTexture.Texture, CornerTexture.SourceRect, BottomRightBounds, CornerColor, Origin, CornerTransforms.BottomRightRotation, 0, CornerTransforms.BottomRightReflections);

                    Rectangle BottomLeftBounds = new Rectangle(Bounds.Left, Bounds.Bottom - BT.Bottom, BT.Left, BT.Bottom).GetTranslated(BT.Left / 2, BT.Bottom / 2);
                    DT.DrawTextureTo(CornerTexture.Texture, CornerTexture.SourceRect, BottomLeftBounds, CornerColor, Origin, CornerTransforms.BottomLeftRotation, 0, CornerTransforms.BottomLeftReflections);
                }
                else
                {
                    Vector2 Scale;

                    //  A very small amount to increase scale values by to hopefully avoid off-by-1 pixel rounding issues when rotating
                    const float ScaleOffset = 0.0001f;

                    Rectangle TopLeftBounds = new(Bounds.Left, Bounds.Top, BT.Left, BT.Top);
                    if (!CornerTransforms.HasTopLeftRotation)
                        DT.DrawTextureTo(CornerTexture.Texture, CornerTexture.SourceRect, TopLeftBounds, CornerColor, Vector2.Zero, 0, 0, CornerTransforms.TopLeftReflections);
                    else
                    {
                        Rectangle RotatedTopLeftBounds = TopLeftBounds.CreateTransformed(Matrix.CreateRotationZ(CornerTransforms.TopLeftRotation));
                        Scale = new(Math.Abs(RotatedTopLeftBounds.Width) / (float)SourceWidth + ScaleOffset, Math.Abs(RotatedTopLeftBounds.Height) / (float)SourceHeight + ScaleOffset);
                        DT.DrawTextureAt(CornerTexture.Texture, CornerTexture.SourceRect, TopLeftBounds.Center.ToVector2(),
                            CornerColor, Origin, CornerTransforms.TopLeftRotation, Scale.X, Scale.Y, 0, CornerTransforms.TopLeftReflections);
                    }

                    Rectangle TopRightBounds = new(Bounds.Right - BT.Right, Bounds.Top, BT.Right, BT.Top);
                    if (!CornerTransforms.HasTopRightRotation)
                        DT.DrawTextureTo(CornerTexture.Texture, CornerTexture.SourceRect, TopRightBounds, CornerColor, Vector2.Zero, 0, 0, CornerTransforms.TopRightReflections);
                    else
                    {
                        Rectangle RotatedTopRightBounds = TopRightBounds.CreateTransformed(Matrix.CreateRotationZ(CornerTransforms.TopRightRotation));
                        Scale = new(Math.Abs(RotatedTopRightBounds.Width) / (float)SourceWidth + ScaleOffset, Math.Abs(RotatedTopRightBounds.Height) / (float)SourceHeight + ScaleOffset);
                        DT.DrawTextureAt(CornerTexture.Texture, CornerTexture.SourceRect, TopRightBounds.Center.ToVector2(),
                            CornerColor, Origin, CornerTransforms.TopRightRotation, Scale.X, Scale.Y, 0, CornerTransforms.TopRightReflections);
                    }

                    Rectangle BottomRightBounds = new(Bounds.Right - BT.Right, Bounds.Bottom - BT.Bottom, BT.Right, BT.Bottom);
                    if (!CornerTransforms.HasBottomRightRotation)
                        DT.DrawTextureTo(CornerTexture.Texture, CornerTexture.SourceRect, BottomRightBounds, CornerColor, Vector2.Zero, 0, 0, CornerTransforms.BottomRightReflections);
                    else
                    {
                        Rectangle RotatedBottomRightBounds = BottomRightBounds.CreateTransformed(Matrix.CreateRotationZ(CornerTransforms.BottomRightRotation));
                        Scale = new(Math.Abs(RotatedBottomRightBounds.Width) / (float)SourceWidth + ScaleOffset, Math.Abs(RotatedBottomRightBounds.Height) / (float)SourceHeight + ScaleOffset);
                        DT.DrawTextureAt(CornerTexture.Texture, CornerTexture.SourceRect, BottomRightBounds.Center.ToVector2(),
                            CornerColor, Origin, CornerTransforms.BottomRightRotation, Scale.X, Scale.Y, 0, CornerTransforms.BottomRightReflections);
                    }

                    Rectangle BottomLeftBounds = new(Bounds.Left, Bounds.Bottom - BT.Bottom, BT.Left, BT.Bottom);
                    if (!CornerTransforms.HasBottomLeftRotation)
                        DT.DrawTextureTo(CornerTexture.Texture, CornerTexture.SourceRect, BottomLeftBounds, CornerColor, Vector2.Zero, 0, 0, CornerTransforms.BottomLeftReflections);
                    else
                    {
                        Rectangle RotatedBottomLeftBounds = BottomLeftBounds.CreateTransformed(Matrix.CreateRotationZ(CornerTransforms.BottomLeftRotation));
                        Scale = new(Math.Abs(RotatedBottomLeftBounds.Width) / (float)SourceWidth + ScaleOffset, Math.Abs(RotatedBottomLeftBounds.Height) / (float)SourceHeight + ScaleOffset);
                        DT.DrawTextureAt(CornerTexture.Texture, CornerTexture.SourceRect, BottomLeftBounds.Center.ToVector2(),
                            CornerColor, Origin, CornerTransforms.BottomLeftRotation, Scale.X, Scale.Y, 0, CornerTransforms.BottomLeftReflections);
                    }
                }
            }
        }

        public IBorderBrush Copy() => new MGTexturedBorderBrush(EdgeTexture, EdgeColor, CornerTexture, CornerColor, Transforms, Opacity);
    }
}
