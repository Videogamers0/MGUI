using MGUI.Core.UI;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace MGUI.Samples.Controls
{
    public class ExpanderSamples : SampleBase
    {
        private MGExpander Expander1 { get; }
        private MGTextBlock TextBlock1 { get; }

        public ExpanderSamples(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Controls)}", "Expander.xaml")
        {
            Expander1 = Window.GetElementByName<MGExpander>("Expander1");
            TextBlock1 = Window.GetElementByName<MGTextBlock>("TextBlock1");
            UpdateTextBlock1Text();
            Expander1.ExpandedStateChanged += (sender, e) => { UpdateTextBlock1Text(); };

            MGExpander CustomIconExpander = Window.GetElementByName<MGExpander>("CustomIconExpander");
            CustomIconExpander.OnEndingDraw += (sender, e) =>
            {
                string TextureName = CustomIconExpander.IsExpanded ? "ArrowUpGreen" : "ArrowDownGreen";
                const int Size = 16;
                Point Position = CustomIconExpander.ExpanderToggleButton.LayoutBounds.Center - new Point(Size / 2, Size / 2) + e.DA.Offset;
                Desktop.TryDrawNamedRegion(e.DA.DT, TextureName, Position, Size, Size);
            };
        }

        private void UpdateTextBlock1Text() => TextBlock1.SetText($"The above expander's [c=Turquoise][i]IsExpanded[/i][/c] state is: [c=LightBlue][s=Blue]{Expander1.IsExpanded}[/s][/c]");
    }
}
