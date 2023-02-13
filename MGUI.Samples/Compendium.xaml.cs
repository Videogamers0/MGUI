using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using MGUI.Core.UI;
using MGUI.Core.UI.XAML;
using MGUI.Samples.Controls;
using MGUI.Samples.Dialogs;
using MGUI.Samples.Dialogs.Debugging;
using MGUI.Samples.Dialogs.FF7;
using MGUI.Samples.Dialogs.Stardew_Valley;
using MGUI.Samples.Features;
using MGUI.Shared.Helpers;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace MGUI.Samples
{
    public abstract class SampleBase
    {
        public ContentManager Content { get; }
        public MGDesktop Desktop { get; }
        public MGWindow Window { get; }

        private bool _IsVisible;
        public bool IsVisible
        {
            get => _IsVisible;
            private set
            {
                if (_IsVisible != value)
                {
                    _IsVisible = value;

                    if (IsVisible)
                        Desktop.Windows.Add(Window);
                    else
                        Desktop.Windows.Remove(Window);
                    VisibilityChanged?.Invoke(this, IsVisible);
                }
            }
        }

        public event EventHandler<bool> VisibilityChanged;

        public void Show() => IsVisible = true;
        public void Hide() => IsVisible = false;

        protected SampleBase(ContentManager Content, MGDesktop Desktop, string ProjectFolderName, string XAMLFilename, MGTheme Theme = null, Action Initialize = null)
        {
            this.Content = Content;
            this.Desktop = Desktop;
            string ResourceName = $"{nameof(MGUI)}.{nameof(Samples)}.{(ProjectFolderName == null ? "" : ProjectFolderName + ".")}{XAMLFilename}";
            string XAML = GeneralUtils.ReadEmbeddedResourceAsString(Assembly.GetExecutingAssembly(), ResourceName);
            Initialize?.Invoke();
            Window = XAMLParser.LoadRootWindow(Desktop, XAML, false, true, Theme);
            Window.WindowClosed += (sender, e) => IsVisible = false;
        }

        protected static void OpenURL(string URL)
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = URL,
                UseShellExecute = true
            });
        }
    }

    public class Compendium : SampleBase
    {
        public ListViewSamples ListViewSamples { get; }
        public TextBlockSamples TextBlockSamples { get; }

        public StylesSamples StylesSamples { get; }

        #region Dialogs
        public FF7Inventory FF7Inventory { get; }
        public Registration Registration { get; }
        public SDVInventory SDVInventory { get; }
        public XAMLDesignerWindow XAMLDesignerWindow { get; }
        public Debug1 Debug1 { get; }
        public Debug2 Debug2 { get; }
        #endregion Dialogs

        public Compendium(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, null, $"{nameof(Compendium)}.xaml")
        {
            Window.IsCloseButtonVisible = false;

            ListViewSamples = new(Content, Desktop);
            BindVisibility(ListViewSamples, "ListView_Toggle");
            TextBlockSamples = new(Content, Desktop);
            BindVisibility(TextBlockSamples, "TextBlock_Toggle");

            StylesSamples = new(Content, Desktop);
            BindVisibility(StylesSamples, "Styles_Toggle");

            FF7Inventory = new(Content, Desktop);
            BindVisibility(FF7Inventory, "FF7Inventory_Toggle", true);
            SDVInventory = new(Content, Desktop);
            BindVisibility(SDVInventory, "SDVInventory_Toggle", false);
            Registration = new(Content, Desktop);
            BindVisibility(Registration, "Registration_Toggle");
            XAMLDesignerWindow = new(Content, Desktop);
            BindVisibility(XAMLDesignerWindow, "XAMLDesigner_Toggle");
            Debug1 = new(Content, Desktop);
            BindVisibility(Debug1, "Debug1_Toggle");
            Debug2 = new(Content, Desktop);
            BindVisibility(Debug2, "Debug2_Toggle");

            Window.AddNamedAction("OpenTextBlockSamples", x => { TextBlockSamples.Show(); });
        }

        private void BindVisibility(SampleBase Sample, string ToggleButtonName, bool IsChecked = false)
        {
            MGToggleButton ToggleButton = Window.GetElementByName<MGToggleButton>(ToggleButtonName);

            Sample.VisibilityChanged += (sender, e) => { ToggleButton.IsChecked = e; };
            ToggleButton.OnCheckStateChanged += (sender, e) =>
            {
                if (e.NewValue)
                    Sample.Show();
                else
                    Sample.Hide();
            };

            ToggleButton.IsChecked = IsChecked;
        }
    }
}
