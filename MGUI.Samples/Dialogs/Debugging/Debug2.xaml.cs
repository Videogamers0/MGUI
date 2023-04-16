using MGUI.Core.UI;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Samples.Dialogs.Debugging
{
    public class Debug2 : SampleBase
    {
        private static void InitializeResources(ContentManager Content, MGDesktop Desktop)
        {
            //  XAML uses named resources to reference Texture2D objects.
            //  Calling Desktop.Resources.AddTexture(...) allows us to initialize things like Images in XAML
            MGResources Resources = Desktop.Resources;
            Texture2D BorderEdgeTexture1 = Content.Load<Texture2D>(Path.Combine("Border Textures", "1_RightEdge"));
            Texture2D BorderCornerTexture1 = Content.Load<Texture2D>(Path.Combine("Border Textures", "1_BottomRightCorner"));
            Resources.AddTexture("BorderEdgeRegion1", new MGTextureData(BorderEdgeTexture1));
            Resources.AddTexture("BorderCornerRegion1", new MGTextureData(BorderCornerTexture1));
        }

        public Debug2(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Dialogs)}.{nameof(Debugging)}", $"{nameof(Debug2)}.xaml", () => { InitializeResources(Content, Desktop); })
        {

        }
    }
}
