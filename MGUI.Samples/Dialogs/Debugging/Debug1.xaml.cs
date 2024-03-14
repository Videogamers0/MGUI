using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.Containers.Grids;
using MGUI.Core.UI.Containers;
using MGUI.Core.UI.XAML;
using MGUI.Shared.Helpers;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using ColumnDefinition = MGUI.Core.UI.Containers.Grids.ColumnDefinition;
using RowDefinition = MGUI.Core.UI.Containers.Grids.RowDefinition;

namespace MGUI.Samples.Dialogs.Debugging
{
    public class Debug1 : SampleBase
    {
        private static void InitializeResources(ContentManager Content, MGDesktop Desktop)
        {
            MGTheme Theme = new(MGTheme.BuiltInTheme.Dark_Blue, Desktop.Theme.FontSettings.DefaultFontFamily);
            Theme.FontSettings.AdjustAllFontSizes(-2);
            Desktop.Resources.AddTheme("Debug1_Theme", Theme);
        }

        public MGOverlay Overlay1 { get; }

        public Debug1(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Dialogs)}.{nameof(Debugging)}", $"{nameof(Debug1)}.xaml", () => { InitializeResources(Content, Desktop); })
        {
            Window.Scale = 2f;

            //Window.WindowStyle = WindowStyle.None;

            MGToolTip Test1 = new(Window, Window, 100, 100);
            Test1.SetContent("Testing inlined tooltip");
            Window.AddNamedToolTip("Test1", Test1);
            MGToolTip Test2 = new(Window, Window, 200, 100);
            Test2.BackgroundBrush.NormalValue = MGSolidFillBrush.SemiBlack;
            Test2.SetContent("Testing inlined tooltip on an inlined image.\n\nAlso click here to reduce window opacity by 0.05", null, 14);
            Test2.ApplySizeToContent(SizeToContent.Height);
            Window.AddNamedToolTip("Test2", Test2);

            Action<MGElement> Debug1_TestAction = element =>
            {
                Window.Opacity -= 0.05f;
                //Window.BackgroundBrush.NormalValue = MGSolidFillBrush.SemiBlack;
            };
            Window.GetResources().AddCommand("Debug1_TestAction", Debug1_TestAction);

            if (Window.TryGetElementByName("Test_Presenter1", out MGContentPresenter ContentPresenter))
            {
                ContentPresenter.SetContent(new MGChatBox(Window));
            }

            if (Window.TryGetElementByName("Btn_1", out MGButton Btn1))
            {
                Btn1.RenderScale = new(1.1f, 1.1f);
            }

            if (Window.TryGetElementByName("SP_1", out MGStackPanel SP))
            {
                //SP.BackgroundBrush.NormalValue = new MGTextureFillBrush(Desktop, "SkullAndCrossbones");
            }

            if (Window.TryGetElementByName("LV1", out MGListView<double> LV))
            {
                LV.SetItemsSource(new List<double>() { 5, 25.5, 101.11, 0.0, -52.6, -18, 12345, -99.3, 0 });
                LV.Columns[1].CellTemplate = (double val) =>
                {
                    if (val > 0)
                        return new MGTextBlock(Window, "Positive", Color.Green);
                    else if (val == 0)
                        return new MGTextBlock(Window, "Zero", Color.Gray);
                    else
                        return new MGTextBlock(Window, "Negative", Color.Red);
                };
            }

            if (Window.TryGetElementByName("UG1", out MGUniformGrid UG))
            {
                //UG.Columns = 3;
                UG.Columns = 5;
            }

            if (Window.TryGetElementByName("GB1", out MGGroupBox GB1))
            {
                //GB1.Header = new MGTextBlock(XAMLWindow, "Foo Bar Long text to test layout updating correctly after initialization", Color.Red);
            }

            if (Window.TryGetElementByName("TestProgressBar", out MGProgressBar TestProgressBar))
            {
                MGProgressBarGradientBrush Brush = new(TestProgressBar);
                //TestProgressBar.CompletedBrush.NormalValue = Brush;
                int Counter = 0;
                TestProgressBar.OnEndUpdate += (sender, e) =>
                {
                    if (Counter % 6 == 0)
                        TestProgressBar.Value = (TestProgressBar.Value + 0.5f + TestProgressBar.Maximum) % TestProgressBar.Maximum;
                    Counter++;
                };
            }

            if (Window.TryGetElementByName("TestGrid", out MGGrid TestGrid))
            {
                TestGrid.OnLayoutUpdated += (sender, e) =>
                {
                    foreach (ColumnDefinition CD in TestGrid.Columns)
                    {
                        foreach (RowDefinition RD in TestGrid.Rows)
                        {
                            foreach (MGElement Element in TestGrid.GetCellContent(RD, CD))
                            {
                                if (Element is MGTextBlock TB)
                                {
                                    TB.SetText($"[b][color=White][shadow=black 1 2]W={CD.Width}, H={RD.Height}[/b][/shadow][/color]", true);
                                }
                            }
                        }
                    }
                };
            }

            if (Window.TryGetElementByName("CB", out MGComboBox<object> TestComboBox))
            {
                List<string> Items = Enumerable.Range(0, 26).Select(x => ((char)(x + 'a')).ToString()).ToList();
                TestComboBox.SetItemsSource(Items.Cast<object>().ToList());
            }

            if (Window.TryGetElementByName("CM1", out MGContextMenu CM))
            {

            }

            if (Window.TryGetElementByName("LB1", out MGListBox<string> LB))
            {

            }

            if (Window.TryGetElementByName("Overlay1", out MGOverlay Overlay))
            {
                Overlay1 = Overlay;
            }

            Window.WindowDataContext = this;
        }
    }
}
