using MGUI.Core.UI;
using Microsoft.Xna.Framework.Content;

namespace MGUI.Samples.Controls
{
    public class CheckBoxSamples : SampleBase
    {
        public CheckBoxSamples(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Controls)}", "CheckBox.xaml")
        {
            Window.WindowDataContext = this;
        }
    }
}
