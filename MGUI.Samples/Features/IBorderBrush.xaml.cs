using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Samples.Features
{
    public class IBorderBrushSamples : SampleBase
    {
        public Compendium Compendium { get; }

        public IBorderBrushSamples(ContentManager Content, MGDesktop Desktop, Compendium Compendium)
            : base(Content, Desktop, $"{nameof(Features)}", "IBorderBrush.xaml")
        {
            this.Compendium = Compendium;

            Resources.AddCommand("OpenAndActivateFillBrushSamples", x =>
            {
                Compendium.IFillBrushSamples.IsVisible = true;
                Desktop.BringToFront(Compendium.IFillBrushSamples.Window);
            });

            Window.GetElementByName<MGTextBox>("TB1").Text = @"<Button BorderThickness=""5"" BorderBrush=""Red-Orange-Yellow-Brown"" />";

            Window.GetElementByName<MGTextBox>("TB2").Text = @"<Button BorderThickness=""5"">
    <Button.BorderBrush>
        <DockedBorderBrush Left=""Red"" Top=""Orange"" Right=""Yellow"" Bottom=""Brown"" />
    </Button.BorderBrush>
</Button>";

            Window.WindowDataContext = this;
        }
    }
}