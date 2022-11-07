using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.Containers.Grids;
using MGUI.Core.UI.XAML;
using MGUI.Shared.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MGUI.Samples
{
    public class Game1 : Game, IObservableUpdate
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private MainRenderer MGUIRenderer { get; set; }
        private MGDesktop Desktop { get; set; }

        public event EventHandler<TimeSpan> PreviewUpdate;
        public event EventHandler<EventArgs> EndUpdate;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.AllowUserResizing = true;
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = 1600;
            _graphics.PreferredBackBufferHeight = 900;
            _graphics.ApplyChanges();

            this.MGUIRenderer = new(new GameRenderHost<Game1>(this));
            this.Desktop = new(MGUIRenderer);

            string xaml =
            @"
<Window Left=""500"" Top=""100"" Width=""400"" Height=""600"" TitleText=""[b]Window Title Text[/b]"">
    <!--<Window.TitleBar>
        <DockPanel BG=""LightGreen"" Padding=""16,4"">
            <TextBlock Text=""ABC"" Dock=""Left"" />
            <CheckBox IsChecked=""true"" Dock=""Right"" HA=""Right"">
                <TextBlock Text=""Checkable Content"" />
            </CheckBox>
        </DockPanel>
    </Window.TitleBar>-->
    <DockPanel Margin=""5"">
        <!--<Button HA=""Center"" BG=""LightGray"" BorderBrush=""Red"" BT=""2"" Padding=""8,2"" Dock=""Top"">
            <Button.ContextMenu>
                <ContextMenu Name=""CM1"">
                    <ContextMenuButton Name=""CMB1"" Content=""ABC"" />
                    <ContextMenuButton>
                        <ContextMenuButton.Submenu>
                            <ContextMenu>
                                <ContextMenuToggle Content=""Toggle Me"" />
                            </ContextMenu>
                        </ContextMenuButton.Submenu>
                        <Rectangle Width=""16"" VA=""Stretch"" Fill=""Blue"" />
                    </ContextMenuButton>
                </ContextMenu>
            </Button.ContextMenu>
            <TextBlock Text=""Hello World!"" />
        </Button>
        <Button BG=""LightBlue"" BorderBrush=""Crimson"" BT=""2"" Padding=""20,4"" Dock=""Left"">
            <TextBlock Text=""Last Child of DockPanel"" />
        </Button>
        <OverlayPanel Dock=""Left"">
            <Button Offset=""10,10,5,5"" BG=""LightBlue"" MinWidth=""50"" />
            <Button Offset=""0,30"" BG=""Crimson"" MinWidth=""50"" />
        </OverlayPanel>
        <ListView Name=""TestLV"" Dock=""Right"" HA=""Right"" xmlns:System=""clr-namespace:System;assembly=mscorlib"" ItemType=""{x:Type System:Double}"">
            <ListViewColumn Width=""55px"">
                <ListViewColumn.HeaderContent>
                    <Button Content=""Test"" BG=""LightGray"" />
                </ListViewColumn.HeaderContent>
            </ListViewColumn>
            <ListViewColumn HeaderContent=""Test"" Width=""*"" />
        </ListView>
        <TabControl Name=""TC1"" Dock=""Left"" MinWidth=""150"" VA=""Top"">
            <TabItem HeaderContent=""Tab1"">
                <Button Content=""Test"" />
            </TabItem>
            <TabItem HeaderContent=""Tab2"" />
        </TabControl>-->

        <!--<Grid RowLengths=""Auto,1.5*,2*,60"" ColumnLengths=""Auto,15,*"">
            <Button BG=""Red"" GridRow=""0"" GridColumn=""0"" MinWidth=""26"" MinHeight=""30"" />
            <Button BG=""Orange"" GridRow=""1"" GridColumn=""0"" MinWidth=""36"" />
            <Button BG=""Purple"" GridRow=""2"" GridColumn=""0"" MinWidth=""50"" />
            <Button BG=""Yellow"" GridRow=""3"" GridColumn=""0"" MinWidth=""10"" />
            <Button BG=""LightGreen"" GridRow=""0"" GridColumn=""2"" MinHeight=""75"" />
            <Button BG=""Cyan"" GridRow=""1"" GridColumn=""2"" />
            <Button BG=""Crimson"" GridRow=""2"" GridColumn=""2"" />
            <Button BG=""Blue"" GridRow=""3"" GridColumn=""2"" />
            <GridSplitter GridRow=""0"" GridColumn=""1"" />
        </Grid>-->

        <TabControl Dock=""Bottom"">
            <TabItem>
                <TabItem.Header>
                    <StackPanel Orientation=""Horizontal"">
                        <Rectangle Width=""16"" Height=""16"" Fill=""LightBlue"" />
                        <Spacer Width=""5"" />
                        <TextBlock Text=""Tab #1"" />
                    </StackPanel>
                </TabItem.Header>

                <DockPanel>
                    <StackPanel Orientation=""Vertical"" Dock=""Top"">
                        <CheckBox Content=""CheckBox #1"" />
                        <RadioButton Content=""RadioButton #1"" />
                        <HeaderedContentPresenter Spacing=""5"" HeaderPosition=""Left"">
                            <HeaderedContentPresenter.Header>
                                <Rectangle Width=""16"" Height=""16"" Fill=""RoyalBlue"" HA=""Center"" />
                            </HeaderedContentPresenter.Header>
                            <TextBlock Text=""Hello World"" />
                        </HeaderedContentPresenter>
                    </StackPanel>
                </DockPanel>
            </TabItem>
            <TabItem Header=""Tab #2"" Name=""Tab#2"">
                <ScrollViewer Dock=""Top"">
                    <StackPanel Orientation=""Vertical"">
                        <Button Content=""Hello World"" />
                        <Slider UseDiscreteValues=""true"" DiscreteValueInterval=""5"" TickFrequency=""20"" DrawTicks=""true"" />
                        <ComboBox Name=""CB"" />
                        <ProgressBar Name=""TestProgressBar"" ShowValue=""true"">
                            <ProgressBar.ContextMenu>
                                <ContextMenu Name=""CM1"">
                                    <ContextMenuButton Name=""CMB1"" Content=""ABC"" />
                                    <ContextMenuButton>
                                        <ContextMenuButton.Submenu>
                                            <ContextMenu>
                                                <ContextMenuToggle Content=""Toggle Me"" />
                                            </ContextMenu>
                                        </ContextMenuButton.Submenu>
                                        <Rectangle Width=""16"" VA=""Stretch"" Fill=""Blue"" />
                                    </ContextMenuButton>
                                </ContextMenu>
                            </ProgressBar.ContextMenu>
                        </ProgressBar>
                        <GroupBox Name=""GB1"">
                            <GroupBox.Header>
                                <Expander Name=""GBExpander"">
                                    <Expander.Header>
                                        <TextBlock Text=""Expander Header"" Name=""TestHeader"" />
                                    </Expander.Header>
                                </Expander>
                            </GroupBox.Header>
                            <Stopwatch IsRunning=""true"" />
                        </GroupBox>
                    </StackPanel>
                </ScrollViewer>
            </TabItem>
        </TabControl>

        <!--<UniformGrid Name=""UG1"" Rows=""6"" Columns=""4"" CellSize=""32,40"" HeaderRowHeight=""20"" HeaderColumnWidth=""16"" GridLinesVisibility=""All"" BG=""LightBlue""
                    HorizontalGridLineBrush = ""Red"" VerticalGridLineBrush=""Green"" RowSpacing=""5"" ColumnSpacing=""8"" GridLineMargin=""2"" SelectionMode=""None"">
            <Button BG=""Red"" GridRow=""0"" GridColumn=""0"" />
            <Button BG=""Orange"" GridRow=""1"" GridColumn=""1"" />
            <Button BG=""Purple"" GridRow=""2"" GridColumn=""2"" />
            <Button BG=""Yellow"" GridRow=""3"" GridColumn=""3"" />
            <Button BG=""Green"" GridRow=""2"" GridColumn=""0"" />
            <Button BG=""YellowGreen"" GridRow=""3"" GridColumn=""1"" />
            <Button BG=""GreenYellow"" GridRow=""4"" GridColumn=""2"" />
            <Button BG=""OrangeRed"" GridRow=""5"" GridColumn=""3"" />
        </UniformGrid>-->

        <Grid Padding=""10"" Name=""TestGrid2"" RowLengths=""30,50,40,50,60,30"" ColumnLengths=""40,50,60,70"" GridLinesVisibility=""All"" BG=""LightBlue""
                    HorizontalGridLineBrush = ""Red"" VerticalGridLineBrush=""Green"" RowSpacing=""5"" ColumnSpacing=""8"" GridLineMargin=""2"" SelectionMode=""None"">
            <Button BG=""Red"" GridRow=""0"" GridColumn=""0"" />
            <Button BG=""Orange"" GridRow=""1"" GridColumn=""1"" />
            <Button BG=""Purple"" GridRow=""2"" GridColumn=""2"" />
            <Button BG=""Yellow"" GridRow=""3"" GridColumn=""3"" />
            <Button BG=""Green"" GridRow=""2"" GridColumn=""0"" />
            <Button BG=""YellowGreen"" GridRow=""3"" GridColumn=""1"" />
            <Button BG=""GreenYellow"" GridRow=""4"" GridColumn=""2"" />
            <Button BG=""OrangeRed"" GridRow=""5"" GridColumn=""3"" />
        </Grid>

        <!--<Grid Name=""TestGrid"" RowLengths=""100[50,],16,*[80,]"" ColumnLengths=""1*[50,150],16,1.5*[50,60],1.2*"">
            <TextBlock BG=""Red"" GridRow=""0"" GridColumn=""0"" />
            <TextBlock BG=""Yellow"" GridRow=""2"" GridColumn=""2"" />
            <TextBlock BG=""Purple"" GridRow=""0"" GridColumn=""3"" />
            <TextBlock BG=""Green"" GridRow=""2"" GridColumn=""0"" />
            <TextBlock BG=""Cyan"" GridRow=""0"" GridColumn=""2"" />
            <TextBlock BG=""Crimson"" GridRow=""2"" GridColumn=""3"" />

            <GridSplitter GridRow=""0"" GridColumn=""1"" GridRowSpan=""3""  />
            <GridSplitter GridRow=""1"" GridColumn=""0"" GridColumnSpan=""4"" />

            <TextBox GridRow=""2"" GridColumn=""3"" />
        </Grid>-->
    </DockPanel>
</Window>
            ";

            MGWindow XAMLWindow = XAMLParser.LoadRootWindow(Desktop, xaml);
            //XAMLWindow.MakeInvisible();
            this.Desktop.Windows.Add(XAMLWindow);

            if (XAMLWindow.TryGetElementByName("UG1", out MGUniformGrid UG))
            {
                //UG.Columns = 3;
                UG.Columns = 5;
            }

            if (XAMLWindow.TryGetElementByName("GB1", out MGGroupBox GB))
            {
                if (XAMLWindow.TryGetElementByName("GBExpander", out MGExpander Expander))
                {
                    if (XAMLWindow.TryGetElementByName("TestHeader", out MGTextBlock TBHeader))
                    {
                        Expander.BindVisibility(GB.Content);
                    }
                }
            }

            if (XAMLWindow.TryGetElementByName("TestProgressBar", out MGProgressBar TestProgressBar))
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

            if (XAMLWindow.TryGetElementByName("TestGrid", out MGGrid TestGrid))
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

            if (XAMLWindow.TryGetElementByName("CB", out MGComboBox<string> TestComboBox))
            {
                List<string> Items = Enumerable.Range(0, 26).Select(x => ((char)(x + 'a')).ToString()).ToList();
                TestComboBox.SetItemsSource(Items);
            }

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            PreviewUpdate?.Invoke(this, gameTime.TotalGameTime);

            Desktop.Update();

            // TODO: Add your update logic here

            base.Update(gameTime);

            EndUpdate?.Invoke(this, EventArgs.Empty);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here

            Desktop.Draw();

            base.Draw(gameTime);
        }
    }
}