using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Samples.Features
{
    public class IFillBrushSamples : SampleBase
    {
        #region MGHighlightFillBrush samples
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _HighlightBrushFocusCheckBox;
        public bool HighlightBrushFocusCheckBox
        {
            get => _HighlightBrushFocusCheckBox;
            set
            {
                if (_HighlightBrushFocusCheckBox != value)
                {
                    _HighlightBrushFocusCheckBox = value;
                    NPC(nameof(HighlightBrushFocusCheckBox));
                    UpdateHighlightBrushFocusedElements();
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _HighlightBrushFocusButton;
        public bool HighlightBrushFocusButton
        {
            get => _HighlightBrushFocusButton;
            set
            {
                if (_HighlightBrushFocusButton != value)
                {
                    _HighlightBrushFocusButton = value;
                    NPC(nameof(HighlightBrushFocusButton));
                    UpdateHighlightBrushFocusedElements();
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _HighlightBrushFocusRadioButtons;
        public bool HighlightBrushFocusRadioButtons
        {
            get => _HighlightBrushFocusRadioButtons;
            set
            {
                if (_HighlightBrushFocusRadioButtons != value)
                {
                    _HighlightBrushFocusRadioButtons = value;
                    NPC(nameof(HighlightBrushFocusRadioButtons));
                    UpdateHighlightBrushFocusedElements();
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IReadOnlyList<MGElement> _HighlightBrushFocusedElements;
        public IReadOnlyList<MGElement> HighlightBrushFocusedElements
        {
            get => _HighlightBrushFocusedElements;
            private set
            {
                if (_HighlightBrushFocusedElements != value)
                {
                    _HighlightBrushFocusedElements = value;
                    NPC(nameof(HighlightBrushFocusedElements));
                }
            }
        }

        private void UpdateHighlightBrushFocusedElements()
        {
            List<MGElement> Elements = new();
            if (HighlightBrushFocusCheckBox && Window.TryGetElementByName("HighlightBrushSampleCheckBox", out MGElement CheckBox))
                Elements.Add(CheckBox);
            if (HighlightBrushFocusButton && Window.TryGetElementByName("HighlightBrushSampleButton", out MGElement Button))
                Elements.Add(Button);
            if (HighlightBrushFocusRadioButtons && Window.TryGetElementByName("HighlightBrushSampleRadioButtons", out MGElement RadioButtons))
                Elements.Add(RadioButtons);
            HighlightBrushFocusedElements = Elements;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Color _HighlightBrushFocusedColor;
        public Color HighlightBrushFocusedColor
        {
            get => _HighlightBrushFocusedColor;
            set
            {
                if (_HighlightBrushFocusedColor != value)
                {
                    _HighlightBrushFocusedColor = value;
                    NPC(nameof(HighlightBrushFocusedColor));
                    NPC(nameof(HighlightBrushActualFocusedColor));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private float _HighlightBrushFocusedColorOpacity;
        public float HighlightBrushFocusedColorOpacity
        {
            get => _HighlightBrushFocusedColorOpacity;
            set
            {
                if (_HighlightBrushFocusedColorOpacity != value)
                {
                    _HighlightBrushFocusedColorOpacity = value;
                    NPC(nameof(HighlightBrushFocusedColorOpacity));
                    NPC(nameof(HighlightBrushActualFocusedColor));
                }
            }
        }

        public Color HighlightBrushActualFocusedColor => HighlightBrushFocusedColor * HighlightBrushFocusedColorOpacity;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Color _HighlightBrushUnfocusedColor;
        public Color HighlightBrushUnfocusedColor
        {
            get => _HighlightBrushUnfocusedColor;
            set
            {
                if (_HighlightBrushUnfocusedColor != value)
                {
                    _HighlightBrushUnfocusedColor = value;
                    NPC(nameof(HighlightBrushUnfocusedColor));
                    NPC(nameof(HighlightBrushActualUnfocusedColor));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private float _HighlightBrushUnfocusedColorOpacity;
        public float HighlightBrushUnfocusedColorOpacity
        {
            get => _HighlightBrushUnfocusedColorOpacity;
            set
            {
                if (_HighlightBrushUnfocusedColorOpacity != value)
                {
                    _HighlightBrushUnfocusedColorOpacity = value;
                    NPC(nameof(HighlightBrushUnfocusedColorOpacity));
                    NPC(nameof(HighlightBrushActualUnfocusedColor));
                }
            }
        }

        public Color HighlightBrushActualUnfocusedColor => HighlightBrushUnfocusedColor * HighlightBrushUnfocusedColorOpacity;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _HighlightBrushFocusedElementPadding;
        public int HighlightBrushFocusedElementPadding
        {
            get => _HighlightBrushFocusedElementPadding;
            set
            {
                if (_HighlightBrushFocusedElementPadding != value)
                {
                    _HighlightBrushFocusedElementPadding = value;
                    NPC(nameof(HighlightBrushFocusedElementPadding));
                }
            }
        }
        #endregion MGHighlightFillBrush samples

        public IFillBrushSamples(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Features)}", "IFillBrush.xaml")
        {
            HighlightBrushFocusCheckBox = false;
            HighlightBrushFocusButton = false;
            HighlightBrushFocusRadioButtons = true;
            HighlightBrushFocusedColor = Color.White;
            HighlightBrushFocusedColorOpacity = 0.35f;
            HighlightBrushUnfocusedColor = Color.Black;
            HighlightBrushUnfocusedColorOpacity = 0.35f;
            HighlightBrushFocusedElementPadding = 3;

            Window.GetElementByName<MGTextBox>("TB1").Text = @"<Button Background=""Red"" />";

            Window.GetElementByName<MGTextBox>("TB2").Text = @"<Button>
    <Button.Background>
        <SolidFillBrush Color=""Red"" />
    </Button.Background>
</Button>";

            Window.GetElementByName<MGTextBox>("TB3").Text = @"<Button Background=""Red|Black"" />";

            Window.GetElementByName<MGTextBox>("TB4").Text = @"<Button>
    <Button.Background>
        <DiagonalGradientFillBrush Color1=""Red"" Color2=""Black"" />
    </Button.Background>
</Button>";

            Window.GetElementByName<MGTextBox>("TB5").Text = @"<Button Background=""Brown|Brown|DarkGray|DarkGray"" />";

            Window.GetElementByName<MGTextBox>("TB6").Text = @"<Button>
    <Button.Background>
        <GradientFillBrush TopLeftColor=""Brown"" TopRightColor=""Brown""
                            BottomRightColor=""DarkGray"" BottomLeftColor=""DarkGray"" />
    </Button.Background>
</Button>";


            MGProgressBar ProgressBar1 = Window.GetElementByName<MGProgressBar>("ProgressBar1");
            ProgressBar1.CompletedBrush.NormalValue = new MGProgressBarGradientBrush(ProgressBar1, Color.Red, Color.Yellow, new Color(0, 255, 0));
            int Counter = 0;
            ProgressBar1.OnEndUpdate += (sender, e) =>
            {
                if (Counter % 3 == 0)
                    ProgressBar1.Value = (ProgressBar1.Value + 0.5f + ProgressBar1.Maximum) % ProgressBar1.Maximum;
                Counter++;
            };

            Window.WindowDataContext = this;
        }
    }
}
