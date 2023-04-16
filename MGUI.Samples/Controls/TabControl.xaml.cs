using MGUI.Core.UI;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Samples.Controls
{
    public class TabControlSamples : SampleBase
    {
        private MGTabControl TabControl1 { get; }

        public TabControlSamples(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Controls)}", "TabControl.xaml")
        {
            TabControl1 = Window.GetElementByName<MGTabControl>("TabControl1");

            Window.GetResources().AddCommand("CloseTab3", x =>
            {
                if (TabControl1.Tabs.Count == 3)
                {
                    TabControl1.RemoveTab(TabControl1.Tabs[2]);
                }
            });
        }
    }
}
