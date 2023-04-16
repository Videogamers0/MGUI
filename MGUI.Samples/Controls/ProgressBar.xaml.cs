using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Shared.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Samples.Controls
{
    public class ProgressBarSamples : SampleBase
    {
        private static void Initialize(MGDesktop Desktop)
        {
            MGResources Resources = Desktop.Resources;
            if (!Resources.Textures.ContainsKey("BorderlessSteelFloor"))
            {
                MGTextureData SteelFloor = Resources.Textures["SteelFloor"];
                Rectangle SteelFloorSourceRect = SteelFloor.SourceRect.Value;
                Resources.AddTexture("BorderlessSteelFloor", new(SteelFloor.Texture, SteelFloorSourceRect.GetCompressed(1)));
            }
        }

        private MGProgressBar ProgressBar1 { get; }
        private MGToggleButton ToggleButton1 { get; }
        private MGSlider Slider1 { get; }

        private MGProgressBar ProgressBar2 { get; }
        private MGToggleButton ToggleButton2 { get; }
        private MGSlider Slider2 { get; }

        private MGProgressBar ProgressBar3 { get; }
        private MGToggleButton ToggleButton3 { get; }
        private MGSlider Slider3 { get; }

        private MGProgressBar ProgressBar4 { get; }
        private MGToggleButton ToggleButton4 { get; }
        private MGSlider Slider4 { get; }

        private MGProgressBar ProgressBar5 { get; }
        private MGToggleButton ToggleButton5 { get; }
        private MGSlider Slider5 { get; }

        private MGProgressBar ProgressBar6 { get; }
        private MGToggleButton ToggleButton6 { get; }
        private MGSlider Slider6 { get; }

        private MGProgressBar ProgressBar7 { get; }
        private MGToggleButton ToggleButton7 { get; }
        private MGSlider Slider7 { get; }

        private MGProgressBar ProgressBar8 { get; }
        private MGToggleButton ToggleButton8 { get; }
        private MGSlider Slider8 { get; }

        public ProgressBarSamples(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Controls)}", "ProgressBar.xaml", () => { Initialize(Desktop); })
        {
            ProgressBar1 = Window.GetElementByName<MGProgressBar>("ProgressBar1");
            ToggleButton1 = Window.GetElementByName<MGToggleButton>("ToggleButton1");
            Slider1 = Window.GetElementByName<MGSlider>("Slider1");
            Window.GetResources().AddCommand("ResetProgressBar1Progress", x => { ProgressBar1.Value = 0; });
            ProgressBar1.OnBeginUpdate += (sender, e) =>
            { 
                ProgressBar1.Value += Slider1.Value;
                if (ToggleButton1.IsChecked && ProgressBar1.Value > ProgressBar1.Maximum)
                    ProgressBar1.Value = ProgressBar1.Minimum;
            };

            ProgressBar2 = Window.GetElementByName<MGProgressBar>("ProgressBar2");
            ToggleButton2 = Window.GetElementByName<MGToggleButton>("ToggleButton2");
            Slider2 = Window.GetElementByName<MGSlider>("Slider2");
            Window.GetResources().AddCommand("ResetProgressBar2Progress", x => { ProgressBar2.Value = 0; });
            ProgressBar2.OnBeginUpdate += (sender, e) =>
            {
                ProgressBar2.Value += Slider2.Value;
                if (ToggleButton2.IsChecked && ProgressBar2.Value > ProgressBar2.Maximum)
                    ProgressBar2.Value = ProgressBar2.Minimum;
            };

            ProgressBar3 = Window.GetElementByName<MGProgressBar>("ProgressBar3");
            ToggleButton3 = Window.GetElementByName<MGToggleButton>("ToggleButton3");
            Slider3 = Window.GetElementByName<MGSlider>("Slider3");
            Window.GetResources().AddCommand("ResetProgressBar3Progress", x => { ProgressBar3.Value = 0; });
            ProgressBar3.OnBeginUpdate += (sender, e) =>
            {
                ProgressBar3.Value += Slider3.Value;
                if (ToggleButton3.IsChecked && ProgressBar3.Value > ProgressBar3.Maximum)
                    ProgressBar3.Value = ProgressBar3.Minimum;
            };

            ProgressBar4 = Window.GetElementByName<MGProgressBar>("ProgressBar4");
            ToggleButton4 = Window.GetElementByName<MGToggleButton>("ToggleButton4");
            Slider4 = Window.GetElementByName<MGSlider>("Slider4");
            Window.GetResources().AddCommand("ResetProgressBar4Progress", x => { ProgressBar4.Value = 0; });
            ProgressBar4.OnBeginUpdate += (sender, e) =>
            {
                ProgressBar4.Value += Slider4.Value;
                if (ToggleButton4.IsChecked && ProgressBar4.Value > ProgressBar4.Maximum)
                    ProgressBar4.Value = ProgressBar4.Minimum;
            };

            ProgressBar5 = Window.GetElementByName<MGProgressBar>("ProgressBar5");
            ToggleButton5 = Window.GetElementByName<MGToggleButton>("ToggleButton5");
            Slider5 = Window.GetElementByName<MGSlider>("Slider5");
            Window.GetResources().AddCommand("ResetProgressBar5Progress", x => { ProgressBar5.Value = 0; });
            ProgressBar5.OnBeginUpdate += (sender, e) =>
            {
                ProgressBar5.Value += Slider5.Value;
                if (ToggleButton5.IsChecked && ProgressBar5.Value > ProgressBar5.Maximum)
                    ProgressBar5.Value = ProgressBar5.Minimum;
            };

            ProgressBar6 = Window.GetElementByName<MGProgressBar>("ProgressBar6");
            ToggleButton6 = Window.GetElementByName<MGToggleButton>("ToggleButton6");
            Slider6 = Window.GetElementByName<MGSlider>("Slider6");
            Window.GetResources().AddCommand("ResetProgressBar6Progress", x => { ProgressBar6.Value = 0; });
            ProgressBar6.OnBeginUpdate += (sender, e) =>
            {
                ProgressBar6.Value += Slider6.Value;
                if (ToggleButton6.IsChecked && ProgressBar6.Value > ProgressBar6.Maximum)
                    ProgressBar6.Value = ProgressBar6.Minimum;
            };

            Window.GetOrCreateRadioButtonGroup("ValueDisplayMode").CheckedItemChanged += (sender, e) => {
                UpdateValueDisplayFormat(e.NewValue.Name);
            };
            UpdateValueDisplayFormat("RBExact");

            ProgressBar7 = Window.GetElementByName<MGProgressBar>("ProgressBar7");
            ToggleButton7 = Window.GetElementByName<MGToggleButton>("ToggleButton7");
            Slider7 = Window.GetElementByName<MGSlider>("Slider7");
            Window.GetResources().AddCommand("ResetProgressBar7Progress", x => { ProgressBar7.Value = 0; });
            ProgressBar7.OnBeginUpdate += (sender, e) =>
            {
                ProgressBar7.Value += Slider7.Value;
                if (ToggleButton7.IsChecked && ProgressBar7.Value > ProgressBar7.Maximum)
                    ProgressBar7.Value = ProgressBar7.Minimum;
            };

            ProgressBar8 = Window.GetElementByName<MGProgressBar>("ProgressBar8");
            ToggleButton8 = Window.GetElementByName<MGToggleButton>("ToggleButton8");
            Slider8 = Window.GetElementByName<MGSlider>("Slider8");
            Window.GetResources().AddCommand("ResetProgressBar8Progress", x => { ProgressBar8.Value = 0; });
            ProgressBar8.OnBeginUpdate += (sender, e) =>
            {
                ProgressBar8.Value += Slider8.Value;
                if (ToggleButton8.IsChecked && ProgressBar8.Value > ProgressBar8.Maximum)
                    ProgressBar8.Value = ProgressBar8.Minimum;
            };

            List<string> Names = new() { "ProgressBar9", "ProgressBar10", "ProgressBar11", "ProgressBar12" };
            foreach (string Name in Names)
            {
                MGProgressBar ProgressBar = Window.GetElementByName<MGProgressBar>(Name);
                ProgressBar.OnBeginUpdate += (sender, e) =>
                {
                    ProgressBar.Value += 0.16f;
                    if (ProgressBar.Value > ProgressBar.Maximum)
                        ProgressBar.Value = ProgressBar.Minimum;
                };
            }
            MGProgressBar ProgressBar12 = Window.GetElementByName<MGProgressBar>("ProgressBar12");
            ProgressBar12.CompletedBrush.NormalValue = new MGProgressBarGradientBrush(ProgressBar12, Color.Red, Color.Yellow, new Color(0, 255, 0));
        }

        private void UpdateValueDisplayFormat(string CheckedRadioButtonName)
        {
            if (CheckedRadioButtonName == "RBExact")
            {
                ProgressBar6.ValueDisplayFormat = "[b]{{Value}}";
                ProgressBar6.NumberFormat = "0.0";
            }
            else if (CheckedRadioButtonName == "RBExactAndTotal")
            {
                ProgressBar6.ValueDisplayFormat = "[b]{{Value}}[/b] / [b]{{Maximum}}";
                ProgressBar6.NumberFormat = "0.0";
            }
            else if (CheckedRadioButtonName == "RBIntegerPercent")
            {
                ProgressBar6.ValueDisplayFormat = "[b]{{ValuePercent}}%";
                ProgressBar6.NumberFormat = "0";
            }
            else if (CheckedRadioButtonName == "RBDecimalPercent")
            {
                ProgressBar6.ValueDisplayFormat = "[b]{{ValuePercent}}%";
                ProgressBar6.NumberFormat = "0.0";
            }
        }
    }
}
