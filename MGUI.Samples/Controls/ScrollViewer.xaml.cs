using MGUI.Core.UI;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Samples.Controls
{
    public class ScrollViewerSamples : SampleBase
    {
        private MGScrollViewer ScrollViewer1 { get; }
        private MGTextBox TextBox_SV1VerticalOffset { get; }
        private MGTextBox TextBox_SV1HorizontalOffset { get; }

        public ScrollViewerSamples(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Controls)}", "ScrollViewer.xaml")
        {
            ScrollViewer1 = Window.GetElementByName<MGScrollViewer>("ScrollViewer1");

            TextBox_SV1VerticalOffset = Window.GetElementByName<MGTextBox>("TextBox_SV1VerticalOffset");
            Window.GetResources().AddCommand("ApplyScrollViewer1VerticalOffset", x => {
                if (float.TryParse(TextBox_SV1VerticalOffset.Text, out float DesiredOffset))
                    ScrollViewer1.VerticalOffset = DesiredOffset;
            });
            ScrollViewer1.VerticalOffsetChanged += (sender, e) => { TextBox_SV1VerticalOffset.SetText(e.NewValue.ToString("0.0")); };

            TextBox_SV1HorizontalOffset = Window.GetElementByName<MGTextBox>("TextBox_SV1HorizontalOffset");
            Window.GetResources().AddCommand("ApplyScrollViewer1HorizontalOffset", x => {
                if (float.TryParse(TextBox_SV1HorizontalOffset.Text, out float DesiredOffset))
                    ScrollViewer1.HorizontalOffset = DesiredOffset;
            });
            ScrollViewer1.HorizontalOffsetChanged += (sender, e) => { TextBox_SV1HorizontalOffset.SetText(e.NewValue.ToString("0.0")); };
        }
    }
}
