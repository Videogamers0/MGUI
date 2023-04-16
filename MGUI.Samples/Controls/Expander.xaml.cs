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
        public ExpanderSamples(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Controls)}", "Expander.xaml")
        {
#if NEVER
            //  This is an alternative implementation of drawing custom graphics inside the expander's toggle button
            //  (The current implementation uses a ContextualContentPresenter whose Value is bound to the Expander's IsExpanded property.
            //      The ContextualContentPresenter alternates between showing 2 different MGImages)
            MGExpander CustomIconExpander = Window.GetElementByName<MGExpander>("CustomIconExpander");
            CustomIconExpander.OnEndingDraw += (sender, e) =>
            {
                string TextureName = CustomIconExpander.IsExpanded ? "ArrowUpGreen" : "ArrowDownGreen";
                const int Size = 16;
                Point Position = CustomIconExpander.ExpanderToggleButton.LayoutBounds.Center - new Point(Size / 2, Size / 2) + e.DA.Offset;
                Desktop.Resources.TryDrawTexture(e.DA.DT, TextureName, Position, Size, Size);
            };
#endif
        }
    }
}
