using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public abstract class SampleBase : ViewModelBase
    {
        public ContentManager Content { get; }
        public MGDesktop Desktop { get; }
        public MGWindow Window { get; }

        private bool _IsVisible;
        public bool IsVisible
        {
            get => _IsVisible;
            set
            {
                if (_IsVisible != value)
                {
                    _IsVisible = value;
                    NPC(nameof(IsVisible));

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

    public class DataContextTest : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public virtual void NotifyPropertyChanged(string PropertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        /// <summary>Notify Property Changed for the given <paramref name="PropertyName"/></summary>
        public void NPC(string PropertyName) => NotifyPropertyChanged(PropertyName);

        private string _TestString;
        public string TestString
        {
            get => _TestString;
            set
            {
                if (_TestString != value)
                {
                    _TestString = value;
                    NPC(nameof(TestString));
                }
            }
        }

        public DataContextTest()
        {
            this.TestString = "Hello World";
        }
    }

    public class Compendium : SampleBase
    {
        #region Controls
        public ButtonSamples ButtonSamples { get; }
        public CheckBoxSamples CheckBoxSamples { get; }
        public ComboBoxSamples ComboBoxSamples { get; }
        public ContextMenuSamples ContextMenuSamples { get; }
        public ContextualContentPresenterSamples ContextualContentPresenterSamples { get; }
        public DockPanelSamples DockPanelSamples { get; }
        public ExpanderSamples ExpanderSamples { get; }
        public GridSamples GridSamples { get; }
        public GroupBoxSamples GroupBoxSamples { get; }
        public ImageSamples ImageSamples { get; }
        public ListBoxSamples ListBoxSamples { get; }
        public ListViewSamples ListViewSamples { get; }
        public PasswordBoxSamples PasswordBoxSamples { get; }
        public ProgressBarSamples ProgressBarSamples { get; }
        public RadioButtonSamples RadioButtonSamples { get; }
        public ScrollViewerSamples ScrollViewerSamples { get; }
        public SliderSamples SliderSamples { get; }
        public StackPanelSamples StackPanelSamples { get; }
        public TabControlSamples TabControlSamples { get; }
        public TextBlockSamples TextBlockSamples { get; }
        public TextBoxSamples TextBoxSamples { get; }
        public ToolTipSamples ToolTipSamples { get; }
        public UniformGridSamples UniformGridSamples { get; }
        public WindowSamples WindowSamples { get; }
        #endregion Controls

        #region Features
        public StylesSamples StylesSamples { get; }
        public DataBindingSamples DataBindingSamples { get; }
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
            ButtonSamples = new(Content, Desktop);
            CheckBoxSamples = new(Content, Desktop);
            ComboBoxSamples = new(Content, Desktop);
            ContextMenuSamples = new(Content, Desktop);
            ContextualContentPresenterSamples = new(Content, Desktop);
            DockPanelSamples = new(Content, Desktop);
            ExpanderSamples = new(Content, Desktop);
            GridSamples = new(Content, Desktop);
            GroupBoxSamples = new(Content, Desktop);
            ImageSamples = new(Content, Desktop);
            ListBoxSamples = new(Content, Desktop);
            ListViewSamples = new(Content, Desktop);
            PasswordBoxSamples = new(Content, Desktop);
            ProgressBarSamples = new(Content, Desktop);
            RadioButtonSamples = new(Content, Desktop);
            ScrollViewerSamples = new(Content, Desktop);
            SliderSamples = new(Content, Desktop);
            StackPanelSamples = new(Content, Desktop);
            TabControlSamples = new(Content, Desktop);
            TextBlockSamples = new(Content, Desktop);
            TextBoxSamples = new(Content, Desktop);
            ToolTipSamples = new(Content, Desktop);
            UniformGridSamples = new(Content, Desktop);
            WindowSamples = new(Content, Desktop);
            #endregion Controls

            #region Features
            StylesSamples = new(Content, Desktop);
            BindVisibility(StylesSamples, "Styles_Toggle");
            DataBindingSamples = new(Content, Desktop);
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
            DataBindingSamples.Show();

            //  DataContext and simple data bindings
            //Debug2.Show();
            //DataContextTest DC = new();
            //Debug2.Window.WindowDataContext = DC;
            //Debug2.Window.AddNamedAction("Test1", x => { DC.TestString = "Clicked"; });
#endif

            Window.WindowDataContext = this;
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
