using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Thickness = MonoGame.Extended.Thickness;

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

        #region MGNineSliceFillBrush samples
        private MGBorder NineSliceResult { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _NineSliceSourceName;
        public string NineSliceSourceName
        {
            get => _NineSliceSourceName;
            set
            {
                if (_NineSliceSourceName != value)
                {
                    _NineSliceSourceName = value;
                    NPC(nameof(NineSliceSourceName));
                    NPC(nameof(NineSlicePlaceholderTextMargin));
                    NPC(nameof(NineSlicePlaceholderTextColor));
                    UpdateNineSliceBrush();
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _NineSliceSourceSample1;
        public bool NineSliceSourceSample1
        {
            get => _NineSliceSourceSample1;
            set
            {
                if (_NineSliceSourceSample1 != value)
                {
                    _NineSliceSourceSample1 = value;
                    NPC(nameof(NineSliceSourceSample1));
                    NineSliceSourceName = "Samples_9SliceTexture1";
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _NineSliceSourceSample2;
        public bool NineSliceSourceSample2
        {
            get => _NineSliceSourceSample2;
            set
            {
                if (_NineSliceSourceSample2 != value)
                {
                    _NineSliceSourceSample2 = value;
                    NPC(nameof(NineSliceSourceSample2));
                    NineSliceSourceName = "Samples_9SliceTexture2";
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _NineSliceSourceSample3;
        public bool NineSliceSourceSample3
        {
            get => _NineSliceSourceSample3;
            set
            {
                if (_NineSliceSourceSample3 != value)
                {
                    _NineSliceSourceSample3 = value;
                    NPC(nameof(NineSliceSourceSample3));
                    NineSliceSourceName = "Samples_9SliceTexture3";
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _NineSliceTargetMargin;
        public int NineSliceTargetMargin
        {
            get => _NineSliceTargetMargin;
            set
            {
                if (_NineSliceTargetMargin != value)
                {
                    _NineSliceTargetMargin = value;
                    NPC(nameof(NineSliceTargetMargin));
                    NPC(nameof(NineSlicePlaceholderTextMargin));
                    UpdateNineSliceBrush();
                }
            }
        }

        public Thickness NineSlicePlaceholderTextMargin => NineSliceSourceSample1 ? new(NineSliceTargetMargin + 5) : new(NineSliceTargetMargin / 2 + 2);
        public Color NineSlicePlaceholderTextColor => NineSliceSourceSample3 ? Color.Black : Color.White;

        private static readonly IReadOnlyDictionary<string, int> NineSliceSourceMargins = new Dictionary<string, int>()
        {
            { "Samples_9SliceTexture1", 52 },
            { "Samples_9SliceTexture2", 40 },
            { "Samples_9SliceTexture3", 40 }
        };

        private void UpdateNineSliceBrush()
        {
            MGNineSliceFillBrush NineSliceBrush = new MGNineSliceFillBrush(new Thickness(NineSliceTargetMargin), Resources.Textures[NineSliceSourceName], NineSliceSourceMargins[NineSliceSourceName]);
            NineSliceResult.BackgroundBrush.SetAll(NineSliceBrush);
        }
        #endregion MGNineSliceFillBrush samples

        private static void InitializeResources(ContentManager Content, MGDesktop Desktop)
        {
            MGResources Resources = Desktop.Resources;

            //  SourceMargin=52
            Resources.AddTexture("Samples_9SliceTexture1", new MGTextureData(Content.Load<Texture2D>(Path.Combine("Brush Textures", "9SliceTexture-1"))));

            //  SourceMargin=40
            Texture2D NineSliceTextureAtlas = Content.Load<Texture2D>(Path.Combine("Brush Textures", "9SliceTextures-2"));
            Resources.AddTexture("Samples_9SliceTexture2", new MGTextureData(NineSliceTextureAtlas, new Rectangle(136, 532, 128, 128)));
            Resources.AddTexture("Samples_9SliceTexture3", new MGTextureData(NineSliceTextureAtlas, new Rectangle(4, 400, 128, 128)));
            //Resources.AddTexture("9SliceTexture3", new MGTextureData(NineSliceTextureAtlas, new Rectangle(136, 532, 128, 128)));
        }

        public IFillBrushSamples(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Features)}", "IFillBrush.xaml", () => InitializeResources(Content, Desktop))
        {
            HighlightBrushFocusCheckBox = false;
            HighlightBrushFocusButton = false;
            HighlightBrushFocusRadioButtons = true;
            HighlightBrushFocusedColor = Color.White;
            HighlightBrushFocusedColorOpacity = 0.35f;
            HighlightBrushUnfocusedColor = Color.Black;
            HighlightBrushUnfocusedColorOpacity = 0.35f;
            HighlightBrushFocusedElementPadding = 3;

            NineSliceResult = Window.GetElementByName<MGBorder>("NineSliceResult");
            NineSliceSourceSample1 = true;
            MGResizeGrip NineSliceResizeGrip = new(Window, NineSliceResult);
            NineSliceTargetMargin = 26;

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
