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

        private double _HeightInches;
        public double HeightInches
        {
            get => _HeightInches;
            set
            {
                if (_HeightInches != value)
                {
                    _HeightInches = value;
                    NPC(nameof(HeightInches));
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

            Resources.AddCommand("ReduceOpacity", x => x.Opacity -= 0.1f);

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

            Resources.AddCommand("OuterButtonCommand", x => TextBlock1Text = "Clicked the [b]outer[/b] button");
            Resources.AddCommand("InnerButtonCommand", x => TextBlock1Text = "Clicked the [b]inner[/b] button");

            int RepeatCounter1 = 0;
            Resources.AddCommand("RepeatButtonSample1", x =>
            {
                RepeatCounter1++;
                MGButton Button = (MGButton)x;
                MGTextBlock TextBlock = Button.Content as MGTextBlock;
                string NewText = $"Execution count: [b]{RepeatCounter1}[/b]";
                TextBlock.SetText(NewText, NewText.Length == TextBlock.Text.Length);
            });

            int RepeatCounter2 = 0;
            Resources.AddCommand("RepeatButtonSample2", x =>
            {
                RepeatCounter2++;
                MGButton Button = (MGButton)x;
                MGTextBlock TextBlock = Button.Content as MGTextBlock;
                string NewText = $"Execution count: [b]{RepeatCounter2}[/b]";
                TextBlock.SetText(NewText, NewText.Length == TextBlock.Text.Length);
            });

            HeightInches = 70;
            Resources.AddCommand("RepeatButtonSample_IncrementHeight", x => { HeightInches += 1; });
            Resources.AddCommand("RepeatButtonSample_DecrementHeight", x => { HeightInches -= 1; });

            //  Draw a triangle inside the IncrementHeight/DecrementHeight buttons to mimic up/down arrows
            const int ArrowWidth = 8;
            const int ArrowHeight = 5;
            Window.GetElementByName<MGButton>("Button_IncrementHeight").OnEndingDraw += (sender, e) =>
            {
                MGButton Element = sender as MGButton;
                Rectangle ArrowElementFullBounds = Element.LayoutBounds;
                Rectangle ArrowPartBounds = MGElement.ApplyAlignment(ArrowElementFullBounds, HorizontalAlignment.Center, VerticalAlignment.Center, new Size(ArrowWidth, ArrowHeight));
                List<Vector2> ArrowVertices = new() {
                    ArrowPartBounds.BottomLeft().ToVector2(), ArrowPartBounds.BottomRight().ToVector2(), new(ArrowPartBounds.Center.X, ArrowPartBounds.Top)
                };
                e.DA.DT.FillPolygon(e.DA.Offset.ToVector2(), ArrowVertices, Color.DarkGray * e.DA.Opacity);
            };
            Window.GetElementByName<MGButton>("Button_DecrementHeight").OnEndingDraw += (sender, e) =>
            {
                MGButton Element = sender as MGButton;
                Rectangle ArrowElementFullBounds = Element.LayoutBounds;
                Rectangle ArrowPartBounds = MGElement.ApplyAlignment(ArrowElementFullBounds, HorizontalAlignment.Center, VerticalAlignment.Center, new Size(ArrowWidth, ArrowHeight));
                List<Vector2> ArrowVertices = new() {
                    ArrowPartBounds.TopLeft().ToVector2(), ArrowPartBounds.TopRight().ToVector2(), new(ArrowPartBounds.Center.X, ArrowPartBounds.Bottom)
                };
                e.DA.DT.FillPolygon(e.DA.Offset.ToVector2(), ArrowVertices, Color.DarkGray * e.DA.Opacity);
            };

            Window.WindowDataContext = this;
        }
    }
}
