using MGUI.Core.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Samples.Controls
{
    public class ImageSamples : SampleBase
    {
        public ImageSamples(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Controls)}", "Image.xaml")
        {
            Texture2D AngryMeteorTexture = Desktop.Resources.Textures["AngryMeteor"].Texture;

            const int TextureTopMargin = 6;
            const int TextureSpacing = 1;
            const int TextureIconSize = 16;

            const int GoldMedalIconRow = 2;
            const int GoldMedalIconColumn = 4;
            Rectangle GoldMedalSourceRect = new(GoldMedalIconColumn * (TextureIconSize + TextureSpacing), TextureTopMargin + GoldMedalIconRow * (TextureIconSize + TextureSpacing), TextureIconSize, TextureIconSize);

            const int SteelFloorIconRow = 2;
            const int SteelFloorIconColumn = 2;
            Rectangle SteelFloorSourceRect = new(SteelFloorIconColumn * (TextureIconSize + TextureSpacing), TextureTopMargin + SteelFloorIconRow * (TextureIconSize + TextureSpacing), TextureIconSize, TextureIconSize);

            if (Window.TryGetElementByName("Image1", out MGImage Image1))
                Image1.Source = new MGTextureData(AngryMeteorTexture, GoldMedalSourceRect, 1, null);

            if (Window.TryGetElementByName("Image2", out MGImage Image2))
                Image2.Source = new MGTextureData(AngryMeteorTexture, GoldMedalSourceRect, 1, null);
            if (Window.TryGetElementByName("Image3", out MGImage Image3))
                Image3.Source = new MGTextureData(AngryMeteorTexture, GoldMedalSourceRect, 0.5f, null);
            if (Window.TryGetElementByName("Image4", out MGImage Image4))
                Image4.Source = new MGTextureData(AngryMeteorTexture, GoldMedalSourceRect, 1, null);
            if (Window.TryGetElementByName("Image5", out MGImage Image5))
                Image5.Source = new MGTextureData(AngryMeteorTexture, GoldMedalSourceRect, 0.5f, null);

            if (Window.TryGetElementByName("Image6", out MGImage Image6))
                Image6.Source = new MGTextureData(AngryMeteorTexture, SteelFloorSourceRect, 1, new MonoGame.Extended.Size(64, 32));
            if (Window.TryGetElementByName("Image7", out MGImage Image7))
                Image7.Source = new MGTextureData(AngryMeteorTexture, SteelFloorSourceRect, 1, new MonoGame.Extended.Size(32, 64));
        }
    }
}
