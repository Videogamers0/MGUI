using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using Microsoft.Xna.Framework.Content;

namespace MGUI.Samples.Dialogs
{
    public class XAMLDesignerWindow : SampleBase
    {
        public XAMLDesignerWindow(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Dialogs)}", $"{nameof(XAMLDesignerWindow)}.xaml")
        {
            if (Window.BackgroundBrush.NormalValue is MGSolidFillBrush SolidFill)
                Window.BackgroundBrush.NormalValue = SolidFill * 0.5f;
        }
    }
}
