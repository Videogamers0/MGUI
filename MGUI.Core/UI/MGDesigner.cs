using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.Containers;
using MGUI.Core.UI.Containers.Grids;
using MGUI.Core.UI.Text;
using MGUI.Core.UI.XAML;
using MGUI.Shared.Helpers;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI
{
    /// <summary>Represents a simple XAML designer that allows you to parse and render XAML markup on the fly.</summary>
    public class MGDesigner : MGElement
    {
        private MGComponent<MGDockPanel> MainContent { get; }
        public MGTextBox MarkupTextBoxComponent { get; }
        public MGButton RefreshButton { get; }
        public MGContentPresenter MarkupPresenter { get; }

        public MGDesigner(MGWindow ParentWindow)
            : base(ParentWindow, MGElementType.Designer)
        {
            using (BeginInitializing())
            {
                MGScrollViewer MarkupScrollViewer = new(ParentWindow);
                this.MarkupTextBoxComponent = new(ParentWindow, null);
                string SampleXAML = @"<Button Background=""Orange"" Padding=""8,5"" VerticalAlignment=""Center"" Content=""Hello World"" />";
                MarkupTextBoxComponent.SetText(SampleXAML);
                MarkupScrollViewer.SetContent(MarkupTextBoxComponent);
                MarkupScrollViewer.CanChangeContent = false;

                this.RefreshButton = new(ParentWindow);
                RefreshButton.HorizontalAlignment = HorizontalAlignment.Center;
                RefreshButton.Padding = new(10, 5);
                RefreshButton.Margin = new(0, 0, 15, 0);
                RefreshButton.SetContent("[b]Refresh[/b]");
                RefreshButton.CanChangeContent = false;
                RefreshButton.AddCommandHandler((btn, e) => { RefreshParsedContent(); });

                MGTabControl TabControlComponent = new(ParentWindow);
                TabControlComponent.Padding = new(8);
                TabControlComponent.AddTab("From File", new MGTextBlock(ParentWindow, "Placeholder"));
                TabControlComponent.AddTab("From String", MarkupScrollViewer);

                this.MarkupPresenter = new(ParentWindow);
                MarkupPresenter.BackgroundBrush.NormalValue = new MGBorderedFillBrush(new(2), MGUniformBorderBrush.Black, Color.Black.AsFillBrush() * 0.75f, true);

                MGHeaderedContentPresenter Tmp = new(ParentWindow, RefreshButton, TabControlComponent) { HeaderPosition = Dock.Bottom, Spacing = 2 };
                Tmp.CanChangeContent = false;

                MGGrid MainGrid = new(ParentWindow);
                MainGrid.AddRows(ConstrainedGridLength.ParseMultiple("*[150,],15,140[120,]"));
                MainGrid.AddColumn(GridLength.CreateWeightedLength(1.0));
                MainGrid.TryAddChild(0, 0, MarkupPresenter);
                MainGrid.TryAddChild(2, 0, Tmp);
                MainGrid.TryAddChild(1, 0, new MGGridSplitter(ParentWindow) { Margin = new(0,2) });
                MainGrid.CanChangeContent = false;

                MGDockPanel DockPanel = new(ParentWindow);
                DockPanel.TryAddChild(MainGrid, Dock.Right);
                DockPanel.CanChangeContent = false;
                DockPanel.SetParent(this);
                this.MainContent = new(DockPanel, false, false, true, true, false, false, true,
                    (AvailableBounds, ComponentSize) => AvailableBounds);
                AddComponent(MainContent);

                SelfOrParentWindow.OnWindowPositionChanged += (sender, e) =>
                {
                    if (MarkupPresenter.Content is MGWindow XAMLWindow)
                    {
                        Rectangle PreviousLayoutBounds = LayoutBounds;
                        Point Offset = new(e.NewValue.Left - e.PreviousValue.Left, e.NewValue.Top - e.PreviousValue.Top);
                        XAMLWindow.TopLeft += Offset;
                    }
                };

                RefreshParsedContent();
            }
        }

        private void RefreshParsedContent()
        {
            MGElement Result;
            try
            {
                string Markup = MarkupTextBoxComponent.Text;
                Result = XAMLParser.Load<MGElement>(SelfOrParentWindow, Markup, true, true);
            }
            catch (Exception ex)
            {
                MGScrollViewer SV = new MGScrollViewer(SelfOrParentWindow);
                SV.Padding = new(5);
                string Message = FTTokenizer.EscapeMarkdown($"{ex.Message}\n\n{ex}");
                SV.SetContent(Message);
                SV.CanChangeContent = false;
                Result = SV;
            }

            using (MarkupPresenter.AllowChangingContentTemporarily())
            {
                MarkupPresenter.SetContent(Result);
            }
        }
    }
}
