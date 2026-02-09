using MGUI.Core.UI;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Samples.Controls
{
    public class ContextMenuSamples : SampleBase
    {
        private MGContextMenu ContextMenu1 { get; }
        private MGTextBlock TextBlock1 { get; }

        public ContextMenuSamples(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Controls)}", "ContextMenu.xaml")
        {
            ContextMenu1 = Window.GetElementByName<MGContextMenu>("ContextMenu1");
            TextBlock1 = Window.GetElementByName<MGTextBlock>("TextBlock1");
            ContextMenu1.ItemSelected += (sender, e) =>
            {
                string CommandId = e.CommandId;
                string Result = CommandId switch
                {
                    "Cmd1" => "You clicked [b][Color=Gold]Menu Item #1[/color][/b]!",
                    "Cmd2" => "You clicked [b][Color=Silver]Menu Item #2[/c][/b]!",
                    "Cmd3" => "You clicked [b][Color=Brown]Menu Item #3[/c][/b]!",
                    _ => throw new NotImplementedException($"Unrecognized {nameof(MGContextMenuButton.CommandId)}: {CommandId}")
                };
                TextBlock1.Text = Result;
            };
        }
    }
}
