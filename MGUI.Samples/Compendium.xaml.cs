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

        /// <param name="Initialize">Optional. This delegate is invoked before the XAML content is parsed, 
        /// so you may wish to use this delegate to add resources to the <paramref name="Desktop"/> that may be required in order to parse the XAML.</param>
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
        #region Controls
        public ComboBoxSamples ComboBoxSamples { get; }
        public ContextMenuSamples ContextMenuSamples { get; }
        public ContextualContentPresenterSamples ContextualContentPresenterSamples { get; }
        public DockPanelSamples DockPanelSamples { get; }
        public ExpanderSamples ExpanderSamples { get; }
        public GridSamples GridSamples { get; }
        public ListBoxSamples ListBoxSamples { get; }
        public ListViewSamples ListViewSamples { get; }
        public ProgressBarSamples ProgressBarSamples { get; }
        public RadioButtonSamples RadioButtonSamples { get; }
        public ScrollViewerSamples ScrollViewerSamples { get; }
        public SliderSamples SliderSamples { get; }
        public StackPanelSamples StackPanelSamples { get; }
        public TabControlSamples TabControlSamples { get; }
        public TextBlockSamples TextBlockSamples { get; }
        public TextBoxSamples TextBoxSamples { get; }
        public ToolTipSamples ToolTipSamples { get; }
        public WindowSamples WindowSamples { get; }
        #endregion Controls

        #region Features
        public StylesSamples StylesSamples { get; }
        #endregion Features

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

            #region Controls
            ComboBoxSamples = new(Content, Desktop);
            BindVisibility(ComboBoxSamples, "ComboBox_Toggle");
            ContextMenuSamples = new(Content, Desktop);
            BindVisibility(ContextMenuSamples, "ContextMenu_Toggle");
            ContextualContentPresenterSamples = new(Content, Desktop);
            BindVisibility(ContextualContentPresenterSamples, "ContextualContentPresenter_Toggle");
            DockPanelSamples = new(Content, Desktop);
            BindVisibility(DockPanelSamples, "DockPanel_Toggle");
            ExpanderSamples = new(Content, Desktop);
            BindVisibility(ExpanderSamples, "Expander_Toggle");
            GridSamples = new(Content, Desktop);
            BindVisibility(GridSamples, "Grid_Toggle");
            ListBoxSamples = new(Content, Desktop);
            BindVisibility(ListBoxSamples, "ListBox_Toggle");
            ListViewSamples = new(Content, Desktop);
            BindVisibility(ListViewSamples, "ListView_Toggle");
            ProgressBarSamples = new(Content, Desktop);
            BindVisibility(ProgressBarSamples, "ProgressBar_Toggle");
            RadioButtonSamples = new(Content, Desktop);
            BindVisibility(RadioButtonSamples, "RadioButton_Toggle");
            ScrollViewerSamples = new(Content, Desktop);
            BindVisibility(ScrollViewerSamples, "ScrollViewer_Toggle");
            SliderSamples = new(Content, Desktop);
            BindVisibility(SliderSamples, "Slider_Toggle");
            StackPanelSamples = new(Content, Desktop);
            BindVisibility(StackPanelSamples, "StackPanel_Toggle");
            TabControlSamples = new(Content, Desktop);
            BindVisibility(TabControlSamples, "TabControl_Toggle");
            TextBlockSamples = new(Content, Desktop);
            BindVisibility(TextBlockSamples, "TextBlock_Toggle");
            TextBoxSamples = new(Content, Desktop);
            BindVisibility(TextBoxSamples, "TextBox_Toggle");
            ToolTipSamples = new(Content, Desktop);
            BindVisibility(ToolTipSamples, "ToolTip_Toggle");
            WindowSamples = new(Content, Desktop);
            BindVisibility(WindowSamples, "Window_Toggle");
            #endregion Controls

            #region Features
            StylesSamples = new(Content, Desktop);
            BindVisibility(StylesSamples, "Styles_Toggle");
            #endregion Features

            #region Dialogs
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
            #endregion Dialogs

#if DEBUG
            StackPanelSamples.Show();
#endif
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
