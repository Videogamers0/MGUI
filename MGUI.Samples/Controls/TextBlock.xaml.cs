using MGUI.Core.UI;
using Microsoft.Xna.Framework.Content;

namespace MGUI.Samples.Controls
{
    public class TextBlockSamples : SampleBase
    {
        public TextBlockSamples(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, nameof(Controls), "TextBlock.xaml")
        {
            Window.GetResources().AddCommand("OpenTextBlockFormattingWiki", x =>
            {
                const string Url = @"https://github.com/Videogamers0/MGUI/wiki/Text-Formatting";
                OpenURL(Url);
            });
        }
    }
}
