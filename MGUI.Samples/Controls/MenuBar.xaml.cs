using MGUI.Core.UI;
using Microsoft.Xna.Framework.Content;

namespace MGUI.Samples.Controls
{
    public class MenuBarSamples : SampleBase
    {
        private MGMenuBar MainMenuBar { get; }
        private MGTextBlock StatusText { get; }

        public MenuBarSamples(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Controls)}", "MenuBar.xaml")
        {
            MainMenuBar = Window.GetElementByName<MGMenuBar>("MainMenuBar");
            StatusText  = Window.GetElementByName<MGTextBlock>("StatusText");

            MainMenuBar.ItemSelected += (sender, e) =>
                StatusText.Text = $"Selected: [b][c=Gold]{e.CommandId}[/c][/b]";

            MainMenuBar.ItemToggled += (sender, e) =>
                StatusText.Text = $"Toggled: [b][c=SkyBlue]{e.CommandId}[/c][/b]  →  {(e.IsChecked ? "[c=Lime]ON[/c]" : "[c=Red]OFF[/c]")}";

            MainMenuBar.ItemRadioSelected += (sender, e) =>
                StatusText.Text = $"Radio: [b][c=Violet]{e.CommandId}[/c][/b]  (group: [i]{e.GroupName}[/i])";
        }
    }
}
