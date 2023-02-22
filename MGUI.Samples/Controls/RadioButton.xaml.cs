using MGUI.Core.UI;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Samples.Controls
{
    public class RadioButtonSamples : SampleBase
    {
        private MGRadioButtonGroup WeaponType2 { get; }
        private MGTextBlock TextBlock1 { get; }

        public RadioButtonSamples(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Controls)}", "RadioButton.xaml")
        {
            WeaponType2 = Window.GetOrCreateRadioButtonGroup("WeaponType2");
            TextBlock1 = Window.GetElementByName<MGTextBlock>("TextBlock1");
            UpdateTextBlock1Text();
            WeaponType2.CheckedItemChanged += (sender, e) => { UpdateTextBlock1Text(); };

            MGRadioButtonGroup ProgrammingLanguageGroup = Window.GetOrCreateRadioButtonGroup("ProgrammingLanguage");
            ProgrammingLanguageGroup.AllowNullCheckedItem = true;
            ProgrammingLanguageGroup.AllowUnchecking = true;
            ProgrammingLanguageGroup.CheckedItem = null;
        }

        private void UpdateTextBlock1Text() => TextBlock1.Text = $"The current [c=Turquoise][i]CheckedItem[/i][/c] is [b][s=Black]{((MGTextBlock)WeaponType2.CheckedItem.Content).Text}[/s][/b]";
    }
}
