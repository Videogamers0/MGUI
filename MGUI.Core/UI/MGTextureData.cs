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

namespace MGUI.Core.UI
{
    /// <param name="RenderSizeOverride">The <see cref="Size"/> to use for the destination <see cref="Rectangle"/> when drawing this texture via <see cref="Draw(DrawTransaction, Point, Color?, float)"/>.<para/>
    /// See also: <see cref="RenderSize"/></param>
    public readonly record struct MGTextureData(Texture2D Texture, Rectangle? SourceRect = null, float Opacity = 1f, Size? RenderSizeOverride = null)
    {
        /// <summary>The actual size this texture will be drawn at if using <see cref="Draw(DrawTransaction, Point, Microsoft.Xna.Framework.Color?, float)"/></summary>
        public Size RenderSize => RenderSizeOverride ?? SourceRect?.Size.AsSize() ?? new Size(Texture.Width, Texture.Height);

        private static Rectangle GetRectangle(Point Topleft, Size Size) => new(Topleft.X, Topleft.Y, Size.Width, Size.Height);

        public void Draw(DrawTransaction DT, Point Position, Color? Color = null, float Opacity = 1f)
            => Draw(DT, GetRectangle(Position, RenderSize), Color, Opacity);
        public void Draw(DrawTransaction DT, Rectangle Destination, Color? Color = null, float Opacity = 1f)
            => DT.DrawTextureTo(Texture, SourceRect, Destination, (Color ?? Microsoft.Xna.Framework.Color.White) * this.Opacity * Opacity);
    }
}
