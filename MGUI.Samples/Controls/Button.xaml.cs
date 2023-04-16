using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Samples.Controls
{
    public class ButtonSamples : SampleBase
    {
        private string _TextBlock1Text;
        public string TextBlock1Text
        {
            get => _TextBlock1Text;
            set
            {
                if (_TextBlock1Text != value)
                {
                    _TextBlock1Text = value;
                    NPC(nameof(TextBlock1Text));
                }
            }
        }

        public ButtonSamples(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Controls)}", "Button.xaml")
        {
            int ClickCount = 0;
            Window.GetElementByName<MGButton>("Button1").Command = btn =>
            {
                ClickCount++;
                MGTextBlock TextBlock = btn.Content as MGTextBlock;
                string NewText = $"I've been clicked [b]{ClickCount}[/b] time(s)";
                TextBlock.SetText(NewText, NewText.Length == TextBlock.Text.Length);
                return true;
            };

            Window.GetResources().AddCommand("ReduceOpacity", x => x.Opacity -= 0.1f);

            List<Color> Colors = new()
            {
                Color.DarkBlue, Color.MediumBlue, Color.Navy, Color.MediumPurple, Color.Purple
            };

            int ColorIndex = 0;
            Window.GetElementByName<MGButton>("Button2").Command = btn =>
            {
                Color NewBackgroundColor = Colors[ColorIndex];
                ColorIndex = (ColorIndex + 1) % Colors.Count;
                btn.BackgroundBrush.NormalValue = NewBackgroundColor.AsFillBrush();
                return true;
            };

            Window.GetResources().AddCommand("OuterButtonCommand", x => TextBlock1Text = "Clicked the [b]outer[/b] button");
            Window.GetResources().AddCommand("InnerButtonCommand", x => TextBlock1Text = "Clicked the [b]inner[/b] button");

            int RepeatCounter1 = 0;
            Window.GetResources().AddCommand("RepeatButtonSample1", x =>
            {
                RepeatCounter1++;
                MGButton Button = (MGButton)x;
                MGTextBlock TextBlock = Button.Content as MGTextBlock;
                string NewText = $"Execution count: [b]{RepeatCounter1}[/b]";
                TextBlock.SetText(NewText, NewText.Length == TextBlock.Text.Length);
            });

            int RepeatCounter2 = 0;
            Window.GetResources().AddCommand("RepeatButtonSample2", x =>
            {
                RepeatCounter2++;
                MGButton Button = (MGButton)x;
                MGTextBlock TextBlock = Button.Content as MGTextBlock;
                string NewText = $"Execution count: [b]{RepeatCounter2}[/b]";
                TextBlock.SetText(NewText, NewText.Length == TextBlock.Text.Length);
            });

            Window.WindowDataContext = this;
        }
    }
}
