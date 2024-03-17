using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Samples.Controls
{
    public class OverlaySamples : SampleBase
    {
        public MGOverlay DesktopOverlay { get; }

        public OverlaySamples(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Controls)}", "Overlays.xaml")
        {
            MGTextBox TextBox1 = Window.GetElementByName<MGTextBox>("TextBox1");
            string XAMLString =
@"<OverlayHost>
    <!-- This is the Content underneath the overlay -->
    <StackPanel Orientation=""Vertical"">
        <TextBlock Text=""This content has an MGOverlay"" />
        <CheckBox Content=""Lorem ipsum dolor"" />
    </StackPanel>

    <OverlayHost.Overlays>
        <!-- This is the overlay -->
        <Overlay IsOpen=""true"" Background=""Yellow * 0.75"">
            <TextBlock Foreground=""Black"" 
                             Text=""This is the content of an MGOverlay"" />
        </Overlay>
	</OverlayHost.Overlays>
</OverlayHost>";
            TextBox1.SetText(XAMLString);

            MGWindow OverlayWindow = Desktop.OverlayHost.ParentWindow;
            MGTextBlock OverlayContent = new MGTextBlock(OverlayWindow, "This is an [c=Turquoise][s=Black]MGOverlay[/s][/c] that has been added to [c=Turquoise][s=Black]MGDesktop.OverlayHost[/s][/c]", Color.Blue, 11)
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new(15)
            };
            DesktopOverlay = Desktop.OverlayHost.AddOverlay(OverlayContent, true);
            DesktopOverlay.BackgroundBrush.SetAll(Color.DarkGray.AsFillBrush());

            Window.WindowDataContext = this;
        }
    }
}
