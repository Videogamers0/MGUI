using MGUI.Core.UI;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Samples.Controls
{
    public class CheckBoxSamples : SampleBase
    {
        private MGCheckBox CheckBox1 { get; }
        private MGTextBlock TextBlock1 { get; }

        private MGCheckBox CheckBox2 { get; }
        private MGTextBlock TextBlock2 { get; }

        public CheckBoxSamples(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Controls)}", "CheckBox.xaml")
        {
            CheckBox1 = Window.GetElementByName<MGCheckBox>("CheckBox1");
            TextBlock1 = Window.GetElementByName<MGTextBlock>("TextBlock1");
            UpdateTextBlock1Text();
            CheckBox1.OnCheckStateChanged += (sender, e) => UpdateTextBlock1Text();

            CheckBox2 = Window.GetElementByName<MGCheckBox>("CheckBox2");
            TextBlock2 = Window.GetElementByName<MGTextBlock>("TextBlock2");
            UpdateTextBlock2Text();
            CheckBox2.OnCheckStateChanged += (sender, e) => UpdateTextBlock2Text();

            MGCheckBox CheckBox3 = Window.GetElementByName<MGCheckBox>("CheckBox3");
            MGContextualContentPresenter CCP1 = Window.GetElementByName<MGContextualContentPresenter>("CCP1");
            CCP1.Value = CheckBox3.IsChecked.Value;
            CheckBox3.OnCheckStateChanged += (sender, e) => { CCP1.Value = CheckBox3.IsChecked.Value; };

            MGCheckBox CheckBox4 = Window.GetElementByName<MGCheckBox>("CheckBox4");
            MGContextualContentPresenter CCP2 = Window.GetElementByName<MGContextualContentPresenter>("CCP2");
            CCP2.Value = CheckBox4.IsChecked.Value;
            CheckBox4.OnCheckStateChanged += (sender, e) => { CCP2.Value = CheckBox4.IsChecked.Value; };
        }

        private void UpdateTextBlock1Text() => 
            TextBlock1.Text = $"This CheckBox's [c=Turquoise][i]IsChecked[/i][/c]=[c=LightBlue][s=Blue]{CheckBox1.IsChecked}[/s][/c]";
        private void UpdateTextBlock2Text() => 
            TextBlock2.Text = $"This 3-State CheckBox's [c=Turquoise][i]IsChecked[/i][/c]=[c=LightBlue][s=Blue]{(CheckBox2.IsChecked.HasValue ? CheckBox2.IsChecked : "null")}[/s][/c]";
    }
}
