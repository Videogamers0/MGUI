using MGUI.Core.UI;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Samples.Controls
{
    public class TextBoxSamples : SampleBase
    {
        private MGTextBox TextBox1 { get; }
        private MGTextBlock TextBlock1 { get; }

        private MGTextBox TextBox2 { get; }
        private MGTextBlock TextBlock2 { get; }
        private MGTextBlock TextBlock3 { get; }

        private MGTextBox SelectionTextBox1 { get; }
        private MGTextBox SelectionTextBox2 { get; }

        public TextBoxSamples(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Controls)}", "TextBox.xaml")
        {
            TextBox1 = Window.GetElementByName<MGTextBox>("TextBox1");
            TextBox1.TrySelectText("text");
            TextBlock1 = Window.GetElementByName<MGTextBlock>("TextBlock1");
            UpdateTextBox1Labels();
            TextBox1.SelectionChanged += (sender, e) => UpdateTextBox1Labels();

            SelectionTextBox1 = Window.GetElementByName<MGTextBox>("SelectionTextBox1");
            SelectionTextBox1.TrySelectText("Hello World");
            SelectionTextBox2 = Window.GetElementByName<MGTextBox>("SelectionTextBox2");
            SelectionTextBox2.TrySelectText("Hello World");

            TextBox2 = Window.GetElementByName<MGTextBox>("TextBox2");
            TextBox2.TrySelectText("World");
            TextBlock2 = Window.GetElementByName<MGTextBlock>("TextBlock2");
            TextBlock3 = Window.GetElementByName<MGTextBlock>("TextBlock3");
            UpdateTextBox2Labels();
            TextBox2.TextChanged += (sender, e) => UpdateTextBox2Labels();
            TextBox2.SelectionChanged += (sender, e) => TextBlock3.Text = TextBox2.FormattedText;
        }

        private void UpdateTextBox1Labels() => 
            TextBlock1.Text = TextBox1.CurrentSelection == null ? "null" : $"{TextBox1.CurrentSelection.Value.Index1}-{TextBox1.CurrentSelection.Value.Index2}";

        private void UpdateTextBox2Labels()
        {
            TextBlock2.Text = TextBox2.Text;
            TextBlock3.Text = TextBox2.FormattedText;
        }
    }
}
