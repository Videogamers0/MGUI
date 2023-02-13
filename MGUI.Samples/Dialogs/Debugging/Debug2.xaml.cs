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
            //  Calling Desktop.AddNamedTexture/Desktop.AddNamedRegion allows us to initialize things like Images in XAML
            Desktop.AddNamedTexture("BorderEdgeTexture1", Content.Load<Texture2D>(Path.Combine("Border Textures", "1_RightEdge")));
            Desktop.AddNamedRegion(new NamedTextureRegion("BorderEdgeTexture1", "BorderEdgeRegion1", null, null));
            Desktop.AddNamedTexture("BorderCornerTexture1", Content.Load<Texture2D>(Path.Combine("Border Textures", "1_BottomRightCorner")));
            Desktop.AddNamedRegion(new NamedTextureRegion("BorderCornerTexture1", "BorderCornerRegion1", null, null));
        }

        public Debug2(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Dialogs)}.{nameof(Debugging)}", $"{nameof(Debug2)}.xaml", null, () => { InitializeResources(Content, Desktop); })
        {

        }
    }
}
