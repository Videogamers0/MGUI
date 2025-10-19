using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Data_Binding;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;
using System.IO;

namespace MGUI.Samples.Features
{
    public class IBorderBrushSamples : SampleBase
    {
        public Compendium Compendium { get; }

        private static void InitializeResources(ContentManager Content, MGDesktop Desktop)
        {
            //  XAML uses named resources to reference Texture2D objects.
            //  Calling Desktop.Resources.AddTexture(...) allows us to initialize things like Images in XAML
            MGResources Resources = Desktop.Resources;
            Texture2D BorderEdgeTexture1 = Content.Load<Texture2D>(Path.Combine("Border Textures", "1_RightEdge"));
            Texture2D BorderCornerTexture1 = Content.Load<Texture2D>(Path.Combine("Border Textures", "1_BottomRightCorner"));
            Resources.AddTexture("BorderEdgeTexture1", new MGTextureData(BorderEdgeTexture1));
            Resources.AddTexture("BorderCornerTexture1", new MGTextureData(BorderCornerTexture1));
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _HighlightBorderSampleIsEnabled;
        public bool HighlightBorderSampleIsEnabled
        {
            get => _HighlightBorderSampleIsEnabled;
            set
            {
                if (_HighlightBorderSampleIsEnabled != value)
                {
                    _HighlightBorderSampleIsEnabled = value;
                    NPC(nameof(HighlightBorderSampleIsEnabled));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _HighlightBorderSampleStopOnClick;
        public bool HighlightBorderSampleStopOnClick
        {
            get => _HighlightBorderSampleStopOnClick;
            set
            {
                if (_HighlightBorderSampleStopOnClick != value)
                {
                    _HighlightBorderSampleStopOnClick = value;
                    NPC(nameof(HighlightBorderSampleStopOnClick));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _HighlightBorderSampleStopOnMouseOver;
        public bool HighlightBorderSampleStopOnMouseOver
        {
            get => _HighlightBorderSampleStopOnMouseOver;
            set
            {
                if (_HighlightBorderSampleStopOnMouseOver != value)
                {
                    _HighlightBorderSampleStopOnMouseOver = value;
                    NPC(nameof(HighlightBorderSampleStopOnMouseOver));
                }
            }
        }

        public MGHighlightBorderBrush HighlightBorderSample { get; }

        public IBorderBrushSamples(ContentManager Content, MGDesktop Desktop, Compendium Compendium)
            : base(Content, Desktop, $"{nameof(Features)}", "IBorderBrush.xaml", () => InitializeResources(Content, Desktop))
        {
            this.Compendium = Compendium;

            Resources.AddCommand("OpenAndActivateFillBrushSamples", x =>
            {
                Compendium.IFillBrushSamples.IsVisible = true;
                Desktop.BringToFront(Compendium.IFillBrushSamples.Window);
            });

            Window.GetElementByName<MGTextBox>("TB1").Text = @"<Button BorderThickness=""5"" BorderBrush=""Red"" />";

            Window.GetElementByName<MGTextBox>("TB2").Text = @"<Button BorderThickness=""5"">
    <Button.BorderBrush>
        <UniformBorderBrush Brush=""Red"" />
    </Button.BorderBrush>
</Button>";

            Window.GetElementByName<MGTextBox>("TB3").Text = @"<Button BorderThickness=""5"" BorderBrush=""Red-Orange-Yellow-Brown"" />";

            Window.GetElementByName<MGTextBox>("TB4").Text = @"<Button BorderThickness=""5"">
    <Button.BorderBrush>
        <DockedBorderBrush Left=""Red"" Top=""Orange"" Right=""Yellow"" Bottom=""Brown"" />
    </Button.BorderBrush>
</Button>";

            HighlightBorderSample = Window.GetElementByName<MGButton>("HighlightBorderSampleButton").BorderBrush as MGHighlightBorderBrush;

            //  Create databindings that synchronizes HighlightBorderSampleIsEnabled/HighlightBorderSampleStopOnClick/HighlightBorderSampleStopOnMouseOver to HighlightBorderSample.IsEnabled/StopOnClick/StopOnMouseOver
            BindingConfig IsEnabledBindingCfg = new BindingConfig(nameof(HighlightBorderSampleIsEnabled), $"{nameof(HighlightBorderSample)}.{nameof(MGHighlightBorderBrush.IsEnabled)}",
                DataBindingMode.TwoWay, ISourceObjectResolver.FromSelf(), DataContextResolver.Self);
            DataBindingManager.AddBinding(IsEnabledBindingCfg, this);
            BindingConfig StopOnClickBindingCfg = new BindingConfig(nameof(HighlightBorderSampleStopOnClick), $"{nameof(HighlightBorderSample)}.{nameof(MGHighlightBorderBrush.StopOnClick)}",
                DataBindingMode.TwoWay, ISourceObjectResolver.FromSelf(), DataContextResolver.Self);
            DataBindingManager.AddBinding(StopOnClickBindingCfg, this);
            BindingConfig StopOnMouseOverBindingCfg = new BindingConfig(nameof(HighlightBorderSampleStopOnMouseOver), $"{nameof(HighlightBorderSample)}.{nameof(MGHighlightBorderBrush.StopOnMouseOver)}",
                DataBindingMode.TwoWay, ISourceObjectResolver.FromSelf(), DataContextResolver.Self);
            DataBindingManager.AddBinding(StopOnMouseOverBindingCfg, this);

            HighlightBorderSampleIsEnabled = true;
            HighlightBorderSampleStopOnClick = true;
            HighlightBorderSampleStopOnMouseOver = true;


            Window.WindowDataContext = this;
        }
    }
}