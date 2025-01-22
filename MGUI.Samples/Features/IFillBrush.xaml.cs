using MGUI.Core.UI;
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
    public class IFillBrushSamples : SampleBase
    {
        public IFillBrushSamples(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Features)}", "IFillBrush.xaml")
        {
            Window.GetElementByName<MGTextBox>("TB1").Text = @"<Button Background=""Red"" />";

            Window.GetElementByName<MGTextBox>("TB2").Text = @"<Button>
    <Button.Background>
        <SolidFillBrush Color=""Red"" />
    </Button.Background>
</Button>";

            Window.GetElementByName<MGTextBox>("TB3").Text = @"<Button Background=""Red|Black"" />";

            Window.GetElementByName<MGTextBox>("TB4").Text = @"<Button>
    <Button.Background>
        <DiagonalGradientFillBrush Color1=""Red"" Color2=""Black"" />
    </Button.Background>
</Button>";

            Window.GetElementByName<MGTextBox>("TB5").Text = @"<Button Background=""Brown|Brown|DarkGray|DarkGray"" />";

            Window.GetElementByName<MGTextBox>("TB6").Text = @"<Button>
    <Button.Background>
        <GradientFillBrush TopLeftColor=""Brown"" TopRightColor=""Brown""
                            BottomRightColor=""DarkGray"" BottomLeftColor=""DarkGray"" />
    </Button.Background>
</Button>";


            MGProgressBar ProgressBar1 = Window.GetElementByName<MGProgressBar>("ProgressBar1");
            ProgressBar1.CompletedBrush.NormalValue = new MGProgressBarGradientBrush(ProgressBar1, Color.Red, Color.Yellow, new Color(0, 255, 0));
            int Counter = 0;
            ProgressBar1.OnEndUpdate += (sender, e) =>
            {
                if (Counter % 3 == 0)
                    ProgressBar1.Value = (ProgressBar1.Value + 0.5f + ProgressBar1.Maximum) % ProgressBar1.Maximum;
                Counter++;
            };
        }
    }
}
