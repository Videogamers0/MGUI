using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Border_Brushes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        }

        public Debug2(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Dialogs)}.{nameof(Debugging)}", $"{nameof(Debug2)}.xaml", () => { InitializeResources(Content, Desktop); })
        {
            TestColor = Color.Orange;

            Window.GetElementByName<MGButton>("TestBtn").Command = (btn) =>
            {
                TestColor = Color.Green;
                return true;
            };

            Window.WindowDataContext = this;
        }
    }
}
