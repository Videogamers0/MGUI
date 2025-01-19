using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Border_Brushes;
using Microsoft.Xna.Framework;
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
            MGBorder Test1 = Window.GetElementByName<MGBorder>("Test1");
            Test1.BorderBrush = new MGHighlightBorderBrush(MGUniformBorderBrush.Black, Color.Yellow, HighlightAnimation.Pulse, Test1);
            MGBorder Test2 = Window.GetElementByName<MGBorder>("Test2");
            Test2.BorderBrush = new MGHighlightBorderBrush(MGUniformBorderBrush.Black, Color.Yellow, HighlightAnimation.Flash, Test2);
            MGBorder Test3 = Window.GetElementByName<MGBorder>("Test3");
            Test3.BorderBrush = new MGHighlightBorderBrush(MGUniformBorderBrush.Black, Color.Yellow, HighlightAnimation.Progress, Test3)
            {
                ProgressSize = 0.25,
                ProgressFlowDirection = HighlightFlowDirection.Clockwise
            };
            MGBorder Test4 = Window.GetElementByName<MGBorder>("Test4");
            Test4.BorderBrush = new MGHighlightBorderBrush(MGUniformBorderBrush.Black, Color.Yellow, HighlightAnimation.Scan, Test4)
            {
                ScanOrientation = Orientation.Horizontal,
                ScanIsReversed = true
            };
        }
    }
}
