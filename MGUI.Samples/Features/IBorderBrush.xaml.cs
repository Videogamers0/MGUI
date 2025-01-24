using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Samples.Features
{
    public class IBorderBrushSamples : SampleBase
    {
        public Compendium Compendium { get; }

        private static void InitializeResources(ContentManager Content, MGDesktop Desktop)
        {
            //  XAML uses named resources to reference Texture2D objects.
            //  Calling Desktop.Resources.AddTexture(...) allows us to initialize things like Images in XAML
            MGResources Resources = Desktop.Resources;
            Texture2D BorderEdgeTexture1 = Content.Load<Texture2D>(Path.Combine("Border Textures", "1_RightEdge"));
            Texture2D BorderCornerTexture1 = Content.Load<Texture2D>(Path.Combine("Border Textures", "1_BottomRightCorner"));
            Resources.AddTexture("BorderEdgeTexture1", new MGTextureData(BorderEdgeTexture1));
            Resources.AddTexture("BorderCornerTexture1", new MGTextureData(BorderCornerTexture1));
        }

        public IBorderBrushSamples(ContentManager Content, MGDesktop Desktop, Compendium Compendium)
            : base(Content, Desktop, $"{nameof(Features)}", "IBorderBrush.xaml", () => InitializeResources(Content, Desktop))
        {
            this.Compendium = Compendium;

            Resources.AddCommand("OpenAndActivateFillBrushSamples", x =>
            {
                Compendium.IFillBrushSamples.IsVisible = true;
                Desktop.BringToFront(Compendium.IFillBrushSamples.Window);
            });

            Window.GetElementByName<MGTextBox>("TB1").Text = @"<Button BorderThickness=""5"" BorderBrush=""Red"" />";

            Window.GetElementByName<MGTextBox>("TB2").Text = @"<Button BorderThickness=""5"">
    <Button.BorderBrush>
        <UniformBorderBrush Brush=""Red"" />
    </Button.BorderBrush>
</Button>";

            Window.GetElementByName<MGTextBox>("TB3").Text = @"<Button BorderThickness=""5"" BorderBrush=""Red-Orange-Yellow-Brown"" />";

            Window.GetElementByName<MGTextBox>("TB4").Text = @"<Button BorderThickness=""5"">
    <Button.BorderBrush>
        <DockedBorderBrush Left=""Red"" Top=""Orange"" Right=""Yellow"" Bottom=""Brown"" />
    </Button.BorderBrush>
</Button>";

            Window.WindowDataContext = this;
        }
    }
}