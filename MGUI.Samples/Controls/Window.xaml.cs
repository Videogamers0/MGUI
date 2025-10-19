using MGUI.Core.UI;
using Microsoft.Xna.Framework.Content;

namespace MGUI.Samples.Controls
{
    public class WindowSamples : SampleBase
    {
        public WindowSamples(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Controls)}", "Window.xaml")
        {

        }
    }
}
