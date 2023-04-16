using MGUI.Core.UI.XAML;
using MGUI.Core.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MGUI.Shared.Helpers;
using Microsoft.Xna.Framework.Content;
using System.Diagnostics;

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
