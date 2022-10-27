using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Shared.Helpers
{
    /// <summary>Creates a 1x1 pixel texture of a solid color</summary>
    public class SolidColorTexture : Texture2D
    {
        private Color _color;
        public Color Color
        {
            get { return _color; }
            set
            {
                if (value != _color)
                {
                    _color = value;
                    SetData(new Color[] { _color });
                }
            }
        }

        public SolidColorTexture(GraphicsDevice GraphicsDevice) : base(GraphicsDevice, 1, 1) { }
        public SolidColorTexture(GraphicsDevice GraphicsDevice, Color color)
            : base(GraphicsDevice, 1, 1)
        {
            Color = color;
        }

        public static implicit operator Color(SolidColorTexture tex) => tex.Color;
        //public static implicit operator SolidColorTexture(Color c) => new SolidColorTexture(RenderUtils.GD, c);
    }

    /// <summary>This class is intended to help create custom textures on the fly</summary>
    internal static class TextureUtils
    {
        /// <summary>Retrieves the Texture data as a 2D Color array. Topleft is Color[0, 0]. Move right by 1 is Color[1, 0], Down by 1 is Color[0, 1].</summary>
        public static Color[,] GetDataAs2D(this Texture2D @this)
        {
            int Width = @this.Width;
            int Height = @this.Height;

            Color[] Data1D = new Color[Width * Height];
            @this.GetData(Data1D);

            Color[,] Data2D = new Color[Width, Height];
            for (int i = 0; i < Data1D.Length; i++)
            {
                int X = i % Width;
                int Y = i / Width;
                Data2D[X, Y] = Data1D[i];
            }

            return Data2D;
        }

        //  The checkerboard texture is currently just used as a background for the current layer of the mapmaker
        public static Texture2D CreateCheckerboardTexture(SpriteBatch SpriteBatch, int TextureSize, Color Color1, Color Color2)
        {
            RenderTarget2D OldRenderTarget = SpriteBatch.GraphicsDevice.GetRenderTargets().First().RenderTarget as RenderTarget2D;
            RenderTarget2D Checkerboard = new RenderTarget2D(SpriteBatch.GraphicsDevice, TextureSize, TextureSize);
            SpriteBatch.GraphicsDevice.SetRenderTarget(Checkerboard);

            SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null);

            SpriteBatch.Draw(ContentUtils.GetSolidColorTexture(SpriteBatch.GraphicsDevice, Color1), new Rectangle(0, 0, TextureSize / 2, TextureSize / 2), Color.White);
            SpriteBatch.Draw(ContentUtils.GetSolidColorTexture(SpriteBatch.GraphicsDevice, Color1), new Rectangle(TextureSize / 2, TextureSize / 2, TextureSize / 2, TextureSize / 2), Color.White);
            SpriteBatch.Draw(ContentUtils.GetSolidColorTexture(SpriteBatch.GraphicsDevice, Color2), new Rectangle(TextureSize / 2, 0, TextureSize / 2, TextureSize / 2), Color.White);
            SpriteBatch.Draw(ContentUtils.GetSolidColorTexture(SpriteBatch.GraphicsDevice, Color2), new Rectangle(0, TextureSize / 2, TextureSize / 2, TextureSize / 2), Color.White);

            SpriteBatch.End();
            SpriteBatch.GraphicsDevice.SetRenderTarget(OldRenderTarget);

            return Checkerboard;
        }

        //Adapted from: https://stackoverflow.com/a/62684919/11689514
        /// <param name="disposeWithSpriteBatch">If true, the <see cref="Texture2D"/> will be automatically disposed when <paramref name="sb"/> is disposed.</param>
        public static Texture2D CreateCircleTexture(SpriteBatch sb, float radius, Color desiredColor, bool disposeWithSpriteBatch)
        {
            Texture2D texture = CreateCircleTexture(sb.GraphicsDevice, radius, desiredColor);

            if (disposeWithSpriteBatch)
            {
                sb.Disposing += (sender, e) =>
                {
                    texture.Dispose();
                };
            }

            return texture;
        }

        //Adapted from: https://stackoverflow.com/a/62684919/11689514
        public static Texture2D CreateCircleTexture(GraphicsDevice gd, float radius, Color desiredColor)
        {
            radius = radius / 2;
            int width = (int)radius * 2;
            int height = width;

            Vector2 center = new Vector2(radius, radius);

            CircleF circle = new CircleF(center, radius);

            Color[] dataColors = new Color[width * height];
            int row = -1; //increased on first iteration to zero!
            int column = 0;
            for (int i = 0; i < dataColors.Length; i++)
            {
                column++;
                if (i % width == 0) //if we reach the right side of the rectangle go to the next row as if we were using a 2D array.
                {
                    row++;
                    column = 0;
                }
                Vector2 point = new Vector2(row, column); //basically the next pixel.
                if (circle.Contains(point))
                {
                    dataColors[i] = desiredColor; //point lies within the radius. Paint it.
                }
                else
                {
                    dataColors[i] = Color.Transparent; //point lies outside, leave it transparent.
                }

            }
            Texture2D texture = new Texture2D(gd, width, height);
            texture.SetData(0, new Rectangle(0, 0, width, height), dataColors, 0, width * height);

            return texture;
        }

        /// <summary>Saves the given texture to a png.</summary>
        /// <param name="this">The texture to save</param>
        /// <param name="FilePath">The file path, with or without .png file extension</param>
        public static void SaveToFile(this Texture2D @this, string FilePath)
        {
            if (!FilePath.EndsWith(".png"))
                FilePath += ".png";

            using (var Stream = new FileStream(FilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                @this.SaveAsPng(Stream, @this.Width, @this.Height);
            }
        }

        public static Texture2D CreateRoundedRectangleTexture(GraphicsDevice graphics, int width, int height, int borderThickness, int borderRadius, int borderShadow, List<Color> backgroundColors, List<Color> borderColors, float initialShadowIntensity, float finalShadowIntensity)
        {
            if (backgroundColors == null || backgroundColors.Count == 0) throw new ArgumentException("Must define at least one background color (up to four).");
            if (borderColors == null || borderColors.Count == 0) throw new ArgumentException("Must define at least one border color (up to three).");
            if (borderRadius < 1) throw new ArgumentException("Must define a border radius (rounds off edges).");
            if (borderThickness < 1) throw new ArgumentException("Must define border thikness.");
            //if (borderThickness + borderRadius > height / 2 || borderThickness + borderRadius > width / 2) throw new ArgumentException("Border will be too thick and/or rounded to fit on the texture.");
            if (borderShadow > borderRadius) throw new ArgumentException("Border shadow must be lesser in magnitude than the border radius (suggeted: shadow <= 0.25 * radius).");

            Texture2D texture = new Texture2D(graphics, width, height, false, SurfaceFormat.Color);
            Color[] color = new Color[width * height];

            for (int x = 0; x < texture.Width; x++)
            {
                for (int y = 0; y < texture.Height; y++)
                {
                    switch (backgroundColors.Count)
                    {
                        case 4:
                            Color leftColor0 = Color.Lerp(backgroundColors[0], backgroundColors[1], ((float)y / (width - 1)));
                            Color rightColor0 = Color.Lerp(backgroundColors[2], backgroundColors[3], ((float)y / (height - 1)));
                            color[x + width * y] = Color.Lerp(leftColor0, rightColor0, ((float)x / (width - 1)));
                            break;
                        case 3:
                            Color leftColor1 = Color.Lerp(backgroundColors[0], backgroundColors[1], ((float)y / (width - 1)));
                            Color rightColor1 = Color.Lerp(backgroundColors[1], backgroundColors[2], ((float)y / (height - 1)));
                            color[x + width * y] = Color.Lerp(leftColor1, rightColor1, ((float)x / (width - 1)));
                            break;
                        case 2:
                            color[x + width * y] = Color.Lerp(backgroundColors[0], backgroundColors[1], ((float)x / (width - 1)));
                            break;
                        default:
                            color[x + width * y] = backgroundColors[0];
                            break;
                    }

                    color[x + width * y] = ColorBorder(x, y, width, height, borderThickness, borderRadius, borderShadow, color[x + width * y], borderColors, initialShadowIntensity, finalShadowIntensity);
                }
            }

            texture.SetData(color);
            return texture;
        }

        private static Color ColorBorder(int x, int y, int width, int height, int borderThickness, int borderRadius, int borderShadow, Color initialColor, List<Color> borderColors, float initialShadowIntensity, float finalShadowIntensity)
        {
            Rectangle internalRectangle = new Rectangle((borderThickness + borderRadius), (borderThickness + borderRadius), width - 2 * (borderThickness + borderRadius), height - 2 * (borderThickness + borderRadius));

            if (internalRectangle.Contains(x, y)) return initialColor;

            Vector2 origin = Vector2.Zero;
            Vector2 point = new Vector2(x, y);

            if (x < borderThickness + borderRadius)
            {
                if (y < borderRadius + borderThickness)
                    origin = new Vector2(borderRadius + borderThickness, borderRadius + borderThickness);
                else if (y > height - (borderRadius + borderThickness))
                    origin = new Vector2(borderRadius + borderThickness, height - (borderRadius + borderThickness));
                else
                    origin = new Vector2(borderRadius + borderThickness, y);
            }
            else if (x > width - (borderRadius + borderThickness))
            {
                if (y < borderRadius + borderThickness)
                    origin = new Vector2(width - (borderRadius + borderThickness), borderRadius + borderThickness);
                else if (y > height - (borderRadius + borderThickness))
                    origin = new Vector2(width - (borderRadius + borderThickness), height - (borderRadius + borderThickness));
                else
                    origin = new Vector2(width - (borderRadius + borderThickness), y);
            }
            else
            {
                if (y < borderRadius + borderThickness)
                    origin = new Vector2(x, borderRadius + borderThickness);
                else if (y > height - (borderRadius + borderThickness))
                    origin = new Vector2(x, height - (borderRadius + borderThickness));
            }

            if (!origin.Equals(Vector2.Zero))
            {
                float distance = Vector2.Distance(point, origin);

                if (distance > borderRadius + borderThickness + 1)
                {
                    return Color.Transparent;
                }
                else if (distance > borderRadius + 1)
                {
                    if (borderColors.Count > 2)
                    {
                        float modNum = distance - borderRadius;

                        if (modNum < borderThickness / 2)
                        {
                            return Color.Lerp(borderColors[2], borderColors[1], (float)((modNum) / (borderThickness / 2.0)));
                        }
                        else
                        {
                            return Color.Lerp(borderColors[1], borderColors[0], (float)((modNum - (borderThickness / 2.0)) / (borderThickness / 2.0)));
                        }
                    }


                    if (borderColors.Count > 0)
                        return borderColors[0];
                }
                else if (distance > borderRadius - borderShadow + 1)
                {
                    float mod = (distance - (borderRadius - borderShadow)) / borderShadow;
                    float shadowDiff = initialShadowIntensity - finalShadowIntensity;
                    return initialColor.Darken((shadowDiff * mod) + finalShadowIntensity);
                }
            }

            return initialColor;
        }
    }
}
