using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
