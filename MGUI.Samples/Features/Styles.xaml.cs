using MGUI.Core.UI;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Samples.Features
{
    public class StylesSamples : SampleBase
    {
        public StylesSamples(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Features)}", $"Styles.xaml")
        {
            MGTextBox ExampleTextBox = Window.GetElementByName<MGTextBox>("Example_TextBox");

            Window.GetResources().AddCommand("OpenStylesWiki", x =>
            {
                const string Url = @"https://github.com/Videogamers0/MGUI/wiki/XAML#styles";
                OpenURL(Url);
            });

            string XAML = @"<StackPanel Orientation=""Vertical"" Spacing=""5"">
    <StackPanel.Styles>
        <!-- This is an implicit style. Since it doesn't have a Name, it will affect ALL child TextBlocks 
        (unless the TextBlock explicitly opts out of styling by setting IsStyleable=""false"") -->
        <Style TargetType=""TextBlock"">
            <Setter Property=""HorizontalAlignment"" Value=""Right"" />
        </Style>
    </StackPanel.Styles>

    <!-- This textblock ignores all styles because IsStyleable=""false"" -->
    <!-- Its HorizontalAlignment remains at the default value of ""Stretch"" -->
    <TextBlock Background=""RGB(40,40,40)"" Text=""One"" IsStyleable=""False"" />

    <!-- This textblock inherits HorizontalAlignment=""Right"" from the implicit style defined above -->
    <TextBlock Background=""RGB(40,40,40)"" Text=""Two"" />

    <StackPanel Orientation=""Vertical"">
        <!-- These styles have an explicit name. They will only affect TextBlocks where StyleNames contains the name -->
        <StackPanel.Styles>
            <Style TargetType=""TextBlock"" Name=""Explicit1"">
                <Setter Property=""HorizontalAlignment"" Value=""Center"" />
                <Setter Property=""Foreground"" Value=""Red"" />
            </Style>
            <Style TargetType=""TextBlock"" Name=""Explicit2"">
                <Setter Property=""HorizontalAlignment"" Value=""Left"" />
            </Style>
        </StackPanel.Styles>

        <!-- This TextBlock is affected by the implicit style and the ""Explicit1"" style, in that order. -->
        <!-- So HorizontalAlignment=""Right"" is applied first, then HorizontalAlignment=""Center"" and Foreground=""Red"" are applied -->
        <TextBlock Background=""RGB(40,40,40)"" Text=""Three"" StyleNames=""Explicit1"" />

        <!-- This TextBlock uses a comma delimiter to specify multiple explicit style names that are applied to it. -->
        <!-- The ""Explicit1"" style sets Foreground=""Red"", but that value is overridden with Foreground=""Orange"" -->
        <TextBlock Background=""RGB(40,40,40)"" Text=""Four"" StyleNames=""Explicit1,Explicit2"" Foreground=""Orange"" />

        <!-- This TextBlock didn't opt in to either Explicit style, so it's only affected by the outer implicit style, setting HorizontalAlignment=""Right"" -->
        <TextBlock Background=""RGB(40,40,40)"" Text=""Five"" Foreground=""Blue"" />
    </StackPanel>
</StackPanel>";
            ExampleTextBox.SetText(XAML);
        }
    }
}
