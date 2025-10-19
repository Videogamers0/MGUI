using MGUI.Core.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;
using System.IO;

namespace MGUI.Samples.Dialogs.Debugging
{
    public class Debug2 : SampleBase
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Color _TestColor;
        public Color TestColor
        {
            get => _TestColor;
            set
            {
                if (_TestColor != value)
                {
                    _TestColor = value;
                    NPC(nameof(TestColor));
                }
            }
        }

        private static void InitializeResources(ContentManager Content, MGDesktop Desktop)
        {
            //  XAML uses named resources to reference Texture2D objects.
            //  Calling Desktop.Resources.AddTexture(...) allows us to initialize things like Images in XAML
            MGResources Resources = Desktop.Resources;

            Texture2D BorderEdgeTexture1 = Content.Load<Texture2D>(Path.Combine("Border Textures", "1_RightEdge"));
            Texture2D BorderCornerTexture1 = Content.Load<Texture2D>(Path.Combine("Border Textures", "1_BottomRightCorner"));
            Resources.AddTexture("BorderEdgeRegion1", new MGTextureData(BorderEdgeTexture1));
            Resources.AddTexture("BorderCornerRegion1", new MGTextureData(BorderCornerTexture1));

            //  SourceMargin=52
            Resources.AddTexture("DEBUG_9SliceTexture1", new MGTextureData(Content.Load<Texture2D>(Path.Combine("Brush Textures", "9SliceTexture-1"))));

            //  SourceMargin=40
            Texture2D NineSliceTextureAtlas = Content.Load<Texture2D>(Path.Combine("Brush Textures", "9SliceTextures-2"));
            Resources.AddTexture("DEBUG_9SliceTexture2", new MGTextureData(NineSliceTextureAtlas, new Rectangle(136, 532, 128, 128)));
            Resources.AddTexture("DEBUG_9SliceTexture3", new MGTextureData(NineSliceTextureAtlas, new Rectangle(4, 400, 128, 128)));
            //Resources.AddTexture("9SliceTexture3", new MGTextureData(NineSliceTextureAtlas, new Rectangle(136, 532, 128, 128)));
        }

        public Debug2(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Dialogs)}.{nameof(Debugging)}", $"{nameof(Debug2)}.xaml", () => { InitializeResources(Content, Desktop); })
        {
            TestColor = Color.Orange;
            if (Window.TryGetElementByName<MGButton>("TestBtn", out MGButton TestBtn))
            {
                TestBtn.Command = (btn) =>
                {
                    TestColor = Color.Green;
                    return true;
                };
            }

            Window.WindowDataContext = this;
        }
    }
}
