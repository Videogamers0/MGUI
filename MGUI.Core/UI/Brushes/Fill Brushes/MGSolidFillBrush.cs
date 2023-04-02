using Microsoft.Xna.Framework;
using MGUI.Core.UI.Brushes.Border_Brushes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI.Brushes.Fill_Brushes
{
    public static class ColorExtensions
    {
        /// <summary>Converts this <see cref="Color"/> into an <see cref="MGSolidFillBrush"/></summary>
        public static MGSolidFillBrush AsFillBrush(this Color @this) => new(@this);
    }

    /// <summary>An <see cref="IFillBrush"/> that uses a single solid <see cref="Color"/> to fill its bounds.<para/>
    /// See also: <see cref="SolidFillBrushes"/>, which contains several static <see cref="MGSolidFillBrush"/> such as <see cref="SolidFillBrushes.Green"/></summary>
    public readonly struct MGSolidFillBrush : IFillBrush
    {
        //TODO change the references to these static MGSolidFillBrushes to use the newer statics in SolidFillBrushes class
        public static readonly MGSolidFillBrush Transparent = new(Color.Transparent);
        public static readonly MGSolidFillBrush White = new(Color.White);
        public static readonly MGSolidFillBrush LightGray = new(Color.LightGray);
        public static readonly MGSolidFillBrush Gray = new(Color.Gray);
        public static readonly MGSolidFillBrush DarkGray = new(Color.DarkGray);
        public static readonly MGSolidFillBrush Black = new(Color.Black);
        public static readonly MGSolidFillBrush SemiBlack = new(new Color(76, 74, 72));
        public static readonly MGSolidFillBrush Yellow = new(Color.Yellow);

        public readonly Color Color;

        public MGSolidFillBrush(Color Color)
        {
            this.Color = Color;
        }

        public void Draw(ElementDrawArgs DA, MGElement Element, Rectangle Bounds)
        {
            Color ActualColor = this.Color * DA.Opacity;
            if (ActualColor != Color.Transparent)
                DA.DT.FillRectangle(DA.Offset.ToVector2(), Bounds, ActualColor);
        }

        public override string ToString() => $"{nameof(MGSolidFillBrush)}: {Color}";

        public MGUniformBorderBrush AsUniformBorderBrush() => new(this);

        public IFillBrush Copy() => new MGSolidFillBrush(Color);

        public static explicit operator MGSolidFillBrush(Color color) => new(color);
        public static explicit operator Color(MGSolidFillBrush brush) => brush.Color;

        public static MGSolidFillBrush operator *(MGSolidFillBrush brush, float opacity) => new(brush.Color * opacity);
    }

    /// <summary>This class contains a static collection of <see cref="MGSolidFillBrush"/>es, one for each named HTML color.<para/>
    /// See also: <see href="https://learn.microsoft.com/en-us/dotnet/media/art-color-table.png?view=windowsdesktop-6.0"/></summary>
    public static class SolidFillBrushes
    {
        public static readonly MGSolidFillBrush Transparent = new(Color.Transparent);
        public static readonly MGSolidFillBrush AliceBlue = new(Color.AliceBlue);
        public static readonly MGSolidFillBrush AntiqueWhite = new(Color.AntiqueWhite);
        public static readonly MGSolidFillBrush Aqua = new(Color.Aqua);
        public static readonly MGSolidFillBrush Aquamarine = new(Color.Aquamarine);
        public static readonly MGSolidFillBrush Azure = new(Color.Azure);
        public static readonly MGSolidFillBrush Beige = new(Color.Beige);
        public static readonly MGSolidFillBrush Bisque = new(Color.Bisque);
        public static readonly MGSolidFillBrush Black = new(Color.Black);
        public static readonly MGSolidFillBrush BlanchedAlmond = new(Color.BlanchedAlmond);
        public static readonly MGSolidFillBrush Blue = new(Color.Blue);
        public static readonly MGSolidFillBrush BlueViolet = new(Color.BlueViolet);
        public static readonly MGSolidFillBrush Brown = new(Color.Brown);
        public static readonly MGSolidFillBrush BurlyWood = new(Color.BurlyWood);
        public static readonly MGSolidFillBrush CadetBlue = new(Color.CadetBlue);
        public static readonly MGSolidFillBrush Chartreuse = new(Color.Chartreuse);
        public static readonly MGSolidFillBrush Chocolate = new(Color.Chocolate);
        public static readonly MGSolidFillBrush Coral = new(Color.Coral);
        public static readonly MGSolidFillBrush CornflowerBlue = new(Color.CornflowerBlue);
        public static readonly MGSolidFillBrush Cornsilk = new(Color.Cornsilk);
        public static readonly MGSolidFillBrush Crimson = new(Color.Crimson);
        public static readonly MGSolidFillBrush Cyan = new(Color.Cyan);
        public static readonly MGSolidFillBrush DarkBlue = new(Color.DarkBlue);
        public static readonly MGSolidFillBrush DarkCyan = new(Color.DarkCyan);
        public static readonly MGSolidFillBrush DarkGoldenrod = new(Color.DarkGoldenrod);
        public static readonly MGSolidFillBrush DarkGray = new(Color.DarkGray);
        public static readonly MGSolidFillBrush DarkGreen = new(Color.DarkGreen);
        public static readonly MGSolidFillBrush DarkKhaki = new(Color.DarkKhaki);
        public static readonly MGSolidFillBrush DarkMagenta = new(Color.DarkMagenta);
        public static readonly MGSolidFillBrush DarkOliveGreen = new(Color.DarkOliveGreen);
        public static readonly MGSolidFillBrush DarkOrange = new(Color.DarkOrange);
        public static readonly MGSolidFillBrush DarkOrchid = new(Color.DarkOrchid);
        public static readonly MGSolidFillBrush DarkRed = new(Color.DarkRed);
        public static readonly MGSolidFillBrush DarkSalmon = new(Color.DarkSalmon);
        public static readonly MGSolidFillBrush DarkSeaGreen = new(Color.DarkSeaGreen);
        public static readonly MGSolidFillBrush DarkSlateBlue = new(Color.DarkSlateBlue);
        public static readonly MGSolidFillBrush DarkSlateGray = new(Color.DarkSlateGray);
        public static readonly MGSolidFillBrush DarkTurquoise = new(Color.DarkTurquoise);
        public static readonly MGSolidFillBrush DarkViolet = new(Color.DarkViolet);
        public static readonly MGSolidFillBrush DeepPink = new(Color.DeepPink);
        public static readonly MGSolidFillBrush DeepSkyBlue = new(Color.DeepSkyBlue);
        public static readonly MGSolidFillBrush DimGray = new(Color.DimGray);
        public static readonly MGSolidFillBrush DodgerBlue = new(Color.DodgerBlue);
        public static readonly MGSolidFillBrush Firebrick = new(Color.Firebrick);
        public static readonly MGSolidFillBrush FloralWhite = new(Color.FloralWhite);
        public static readonly MGSolidFillBrush ForestGreen = new(Color.ForestGreen);
        public static readonly MGSolidFillBrush Fuchsia = new(Color.Fuchsia);
        public static readonly MGSolidFillBrush Gainsboro = new(Color.Gainsboro);
        public static readonly MGSolidFillBrush GhostWhite = new(Color.GhostWhite);
        public static readonly MGSolidFillBrush Gold = new(Color.Gold);
        public static readonly MGSolidFillBrush Goldenrod = new(Color.Goldenrod);
        public static readonly MGSolidFillBrush Gray = new(Color.Gray);
        public static readonly MGSolidFillBrush Green = new(Color.Green);
        public static readonly MGSolidFillBrush GreenYellow = new(Color.GreenYellow);
        public static readonly MGSolidFillBrush Honeydew = new(Color.Honeydew);
        public static readonly MGSolidFillBrush HotPink = new(Color.HotPink);
        public static readonly MGSolidFillBrush IndianRed = new(Color.IndianRed);
        public static readonly MGSolidFillBrush Indigo = new(Color.Indigo);
        public static readonly MGSolidFillBrush Ivory = new(Color.Ivory);
        public static readonly MGSolidFillBrush Khaki = new(Color.Khaki);
        public static readonly MGSolidFillBrush Lavender = new(Color.Lavender);
        public static readonly MGSolidFillBrush LavenderBlush = new(Color.LavenderBlush);
        public static readonly MGSolidFillBrush LawnGreen = new(Color.LawnGreen);
        public static readonly MGSolidFillBrush LemonChiffon = new(Color.LemonChiffon);
        public static readonly MGSolidFillBrush LightBlue = new(Color.LightBlue);
        public static readonly MGSolidFillBrush LightCoral = new(Color.LightCoral);
        public static readonly MGSolidFillBrush LightCyan = new(Color.LightCyan);
        public static readonly MGSolidFillBrush LightGoldenrodYellow = new(Color.LightGoldenrodYellow);
        public static readonly MGSolidFillBrush LightGray = new(Color.LightGray);
        public static readonly MGSolidFillBrush LightGreen = new(Color.LightGreen);
        public static readonly MGSolidFillBrush LightPink = new(Color.LightPink);
        public static readonly MGSolidFillBrush LightSalmon = new(Color.LightSalmon);
        public static readonly MGSolidFillBrush LightSeaGreen = new(Color.LightSeaGreen);
        public static readonly MGSolidFillBrush LightSkyBlue = new(Color.LightSkyBlue);
        public static readonly MGSolidFillBrush LightSlateGray = new(Color.LightSlateGray);
        public static readonly MGSolidFillBrush LightSteelBlue = new(Color.LightSteelBlue);
        public static readonly MGSolidFillBrush LightYellow = new(Color.LightYellow);
        public static readonly MGSolidFillBrush Lime = new(Color.Lime);
        public static readonly MGSolidFillBrush LimeGreen = new(Color.LimeGreen);
        public static readonly MGSolidFillBrush Linen = new(Color.Linen);
        public static readonly MGSolidFillBrush Magenta = new(Color.Magenta);
        public static readonly MGSolidFillBrush Maroon = new(Color.Maroon);
        public static readonly MGSolidFillBrush MediumAquamarine = new(Color.MediumAquamarine);
        public static readonly MGSolidFillBrush MediumBlue = new(Color.MediumBlue);
        public static readonly MGSolidFillBrush MediumOrchid = new(Color.MediumOrchid);
        public static readonly MGSolidFillBrush MediumPurple = new(Color.MediumPurple);
        public static readonly MGSolidFillBrush MediumSeaGreen = new(Color.MediumSeaGreen);
        public static readonly MGSolidFillBrush MediumSlateBlue = new(Color.MediumSlateBlue);
        public static readonly MGSolidFillBrush MediumSpringGreen = new(Color.MediumSpringGreen);
        public static readonly MGSolidFillBrush MediumTurquoise = new(Color.MediumTurquoise);
        public static readonly MGSolidFillBrush MediumVioletRed = new(Color.MediumVioletRed);
        public static readonly MGSolidFillBrush MidnightBlue = new(Color.MidnightBlue);
        public static readonly MGSolidFillBrush MintCream = new(Color.MintCream);
        public static readonly MGSolidFillBrush MistyRose = new(Color.MistyRose);
        public static readonly MGSolidFillBrush Moccasin = new(Color.Moccasin);
        public static readonly MGSolidFillBrush MonoGameOrange = new(Color.MonoGameOrange);
        public static readonly MGSolidFillBrush NavajoWhite = new(Color.NavajoWhite);
        public static readonly MGSolidFillBrush Navy = new(Color.Navy);
        public static readonly MGSolidFillBrush OldLace = new(Color.OldLace);
        public static readonly MGSolidFillBrush Olive = new(Color.Olive);
        public static readonly MGSolidFillBrush OliveDrab = new(Color.OliveDrab);
        public static readonly MGSolidFillBrush Orange = new(Color.Orange);
        public static readonly MGSolidFillBrush OrangeRed = new(Color.OrangeRed);
        public static readonly MGSolidFillBrush Orchid = new(Color.Orchid);
        public static readonly MGSolidFillBrush PaleGoldenrod = new(Color.PaleGoldenrod);
        public static readonly MGSolidFillBrush PaleGreen = new(Color.PaleGreen);
        public static readonly MGSolidFillBrush PaleTurquoise = new(Color.PaleTurquoise);
        public static readonly MGSolidFillBrush PaleVioletRed = new(Color.PaleVioletRed);
        public static readonly MGSolidFillBrush PapayaWhip = new(Color.PapayaWhip);
        public static readonly MGSolidFillBrush PeachPuff = new(Color.PeachPuff);
        public static readonly MGSolidFillBrush Peru = new(Color.Peru);
        public static readonly MGSolidFillBrush Pink = new(Color.Pink);
        public static readonly MGSolidFillBrush Plum = new(Color.Plum);
        public static readonly MGSolidFillBrush PowderBlue = new(Color.PowderBlue);
        public static readonly MGSolidFillBrush Purple = new(Color.Purple);
        public static readonly MGSolidFillBrush Red = new(Color.Red);
        public static readonly MGSolidFillBrush RosyBrown = new(Color.RosyBrown);
        public static readonly MGSolidFillBrush RoyalBlue = new(Color.RoyalBlue);
        public static readonly MGSolidFillBrush SaddleBrown = new(Color.SaddleBrown);
        public static readonly MGSolidFillBrush Salmon = new(Color.Salmon);
        public static readonly MGSolidFillBrush SandyBrown = new(Color.SandyBrown);
        public static readonly MGSolidFillBrush SeaGreen = new(Color.SeaGreen);
        public static readonly MGSolidFillBrush SeaShell = new(Color.SeaShell);
        public static readonly MGSolidFillBrush Sienna = new(Color.Sienna);
        public static readonly MGSolidFillBrush Silver = new(Color.Silver);
        public static readonly MGSolidFillBrush SkyBlue = new(Color.SkyBlue);
        public static readonly MGSolidFillBrush SlateBlue = new(Color.SlateBlue);
        public static readonly MGSolidFillBrush SlateGray = new(Color.SlateGray);
        public static readonly MGSolidFillBrush Snow = new(Color.Snow);
        public static readonly MGSolidFillBrush SpringGreen = new(Color.SpringGreen);
        public static readonly MGSolidFillBrush SteelBlue = new(Color.SteelBlue);
        public static readonly MGSolidFillBrush Tan = new(Color.Tan);
        public static readonly MGSolidFillBrush Teal = new(Color.Teal);
        public static readonly MGSolidFillBrush Thistle = new(Color.Thistle);
        public static readonly MGSolidFillBrush Tomato = new(Color.Tomato);
        public static readonly MGSolidFillBrush Turquoise = new(Color.Turquoise);
        public static readonly MGSolidFillBrush Violet = new(Color.Violet);
        public static readonly MGSolidFillBrush Wheat = new(Color.Wheat);
        public static readonly MGSolidFillBrush White = new(Color.White);
        public static readonly MGSolidFillBrush WhiteSmoke = new(Color.WhiteSmoke);
        public static readonly MGSolidFillBrush Yellow = new(Color.Yellow);
        public static readonly MGSolidFillBrush YellowGreen = new(Color.YellowGreen);
    }
}
