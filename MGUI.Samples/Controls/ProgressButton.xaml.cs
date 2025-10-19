using MGUI.Core.UI;
using Microsoft.Xna.Framework.Content;

namespace MGUI.Samples.Controls
{
    public class ProgressButtonSamples : SampleBase
    {
        private int _WoodQty;
        public int WoodQty
        {
            get => _WoodQty;
            set
            {
                if (_WoodQty != value)
                {
                    _WoodQty = value;
                    NPC(nameof(WoodQty));
                    NPC(nameof(CanBuildHut));
                }
            }
        }

        private int _HutQty;
        public int HutQty
        {
            get => _HutQty;
            set
            {
                if (_HutQty != value)
                {
                    _HutQty = value;
                    NPC(nameof(HutQty));
                }
            }
        }

        const int WoodPerHut = 2;
        public bool CanBuildHut => WoodQty >= WoodPerHut;

        private int _StoneQty;
        public int StoneQty
        {
            get => _StoneQty;
            set
            {
                if (_StoneQty != value)
                {
                    _StoneQty = value;
                    NPC(nameof(StoneQty));
                    NPC(nameof(CanBuildQuarry));
                }
            }
        }

        private int _QuarryQty;
        public int QuarryQty
        {
            get => _QuarryQty;
            set
            {
                if (_QuarryQty != value)
                {
                    _QuarryQty = value;
                    NPC(nameof(QuarryQty));
                }
            }
        }

        const int StonePerQuarry = 3;
        public bool CanBuildQuarry => StoneQty >= StonePerQuarry;

        public ProgressButtonSamples(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Controls)}", "ProgressButton.xaml")
        {
            Desktop.Resources.AddCommand("Button1_Reset", x =>
            {
                if (Window.TryGetElementByName("ProgressButton_1", out MGProgressButton btn))
                    btn.ResetProgress();
            });

            ((MGProgressButton)Window.GetElementByName("WoodBtn")).OnCompleted += (sender, e) => WoodQty++;
            ((MGProgressButton)Window.GetElementByName("HutBtn")).OnStarted += (sender, e) => WoodQty -= WoodPerHut;
            ((MGProgressButton)Window.GetElementByName("HutBtn")).OnCompleted += (sender, e) => HutQty++;

            ((MGProgressButton)Window.GetElementByName("StoneBtn")).OnCompleted += (sender, e) => StoneQty++;
            ((MGProgressButton)Window.GetElementByName("QuarryBtn")).OnStarted += (sender, e) => StoneQty -= StonePerQuarry;
            ((MGProgressButton)Window.GetElementByName("QuarryBtn")).OnCompleted += (sender, e) => QuarryQty++;

            Window.WindowDataContext = this;
        }
    }
}
