using MGUI.Core.UI;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Samples.Controls
{
    public class ContextualContentPresenterSamples : SampleBase
    {
        private MGToggleButton ToggleButton1 { get; }
        private MGTextBlock TextBlock1 { get; }
        private MGContextualContentPresenter CCP1 { get; }

        public ContextualContentPresenterSamples(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Controls)}", "ContextualContentPresenter.xaml")
        {
            ToggleButton1 = Window.GetElementByName<MGToggleButton>("ToggleButton1");
            TextBlock1 = Window.GetElementByName<MGTextBlock>("TextBlock1");
            CCP1 = Window.GetElementByName<MGContextualContentPresenter>("CCP1");

            CCP1.Value = ToggleButton1.IsChecked;
            UpdateTextBlock1Text();
            ToggleButton1.OnCheckStateChanged += (sender, e) =>
            {
                UpdateTextBlock1Text();
                CCP1.Value = ToggleButton1.IsChecked;
            };
        }

        private void UpdateTextBlock1Text() => TextBlock1.Text = $"[c=Turquoise][i]ContextualContentPresenter.Value[/i][/c] is currently set to [c=LightBlue][s=Blue]{ToggleButton1.IsChecked}[/s][/c]";
    }
}
