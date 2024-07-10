using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Shared.Helpers;
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
    public class ProgressButtonSamples : SampleBase
    {
        public ProgressButtonSamples(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Controls)}", "ProgressButton.xaml")
        {
            Desktop.Resources.AddCommand("Button1_Reset", x =>
            {
                if (Window.TryGetElementByName("ProgressButton_1", out MGProgressButton btn))
                    btn.ResetProgress();
            });

            Window.WindowDataContext = this;
        }
    }
}
