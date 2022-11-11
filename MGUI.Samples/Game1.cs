using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Border_Brushes;
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
using System.IO;
using System.Linq;
using System.Reflection;
using static System.Net.Mime.MediaTypeNames;

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

            Assembly CurrentAssembly = Assembly.GetExecutingAssembly();
            Desktop.Windows.Add(LoadDebugWindow(CurrentAssembly, Desktop));
            Desktop.Windows.Add(LoadRegistrationWindow(CurrentAssembly, Desktop));
            Desktop.Windows.Add(LoadCharacterStatsWindow(CurrentAssembly, Desktop));
            Desktop.Windows.Add(LoadInventoryWindow(CurrentAssembly, Desktop));
            //Desktop.Windows.Add(LoadStylesTestWindow(CurrentAssembly, Desktop));

            base.Initialize();
        }

        private static string ReadEmbeddedResourceAsString(Assembly CurrentAssembly, string ResourceName)
        {
            using (Stream ResourceStream = CurrentAssembly.GetManifestResourceStream(ResourceName))
            using (StreamReader Reader = new StreamReader(ResourceStream))
            return Reader.ReadToEnd();
        }

        private static MGWindow LoadDebugWindow(Assembly CurrentAssembly, MGDesktop Desktop)
        {
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
        <!--<Button BG=""LightBlue"" BorderBrush=""Crimson"" BT=""2"" Padding=""20,4"" Dock=""Left"">
            <TextBlock Text=""Last Child of DockPanel"" />
        </Button>
        <OverlayPanel Dock=""Left"">
            <Button Offset=""10,10,5,5"" BG=""LightBlue"" MinWidth=""50"" />
            <Button Offset=""0,30"" BG=""Crimson"" MinWidth=""50"" />
        </OverlayPanel>
        <ListView Name=""TestLV"" Dock=""Right"" HA=""Right"" xmlns:System=""clr-namespace:System;assembly=mscorlib"" ItemType=""{x:Type System:Double}"">
            <ListViewColumn Width=""55px"">
                <ListViewColumn.Header>
                    <Button Content=""Test"" BG=""LightGray"" />
                </ListViewColumn.Header>
            </ListViewColumn>
            <ListViewColumn Header=""Test"" Width=""*"" />
        </ListView>
        <TabControl Name=""TC1"" Dock=""Left"" MinWidth=""150"" VA=""Top"">
            <TabItem Header=""Tab1"">
                <Button Content=""Test"" />
            </TabItem>
            <TabItem Header=""Tab2"" />
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
                    <StackPanel Orientation=""Vertical"" Dock=""Top"" Spacing=""5"">
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
                    <StackPanel Orientation=""Vertical"" Spacing=""5"">
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
                        <GroupBox Name=""GB1"" IsExpandable=""true"" Header=""Header of GroupBox"">
                            <StackPanel Orientation=""Vertical"" Spacing=""5"">
                                <Grid RowLengths=""auto"" ColumnLengths=""*,8,*"">
                                    <Stopwatch Column=""0"" IsRunning=""true"" />
                                    <Timer Column=""2"" IsPaused=""false"" />
                                </Grid>
                                <ToggleButton Name=""TB1"" Content=""Toggle me"" />
                            </StackPanel>
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

        <!--<Grid Padding=""10"" Name=""TestGrid2"" RowLengths=""30,50,40,50,60,30"" ColumnLengths=""40,50,60,70"" GridLinesVisibility=""All"" BG=""LightBlue""
                    HorizontalGridLineBrush = ""Red"" VerticalGridLineBrush=""Green"" RowSpacing=""5"" ColumnSpacing=""8"" GridLineMargin=""2"" SelectionMode=""None"">
            <Button BG=""Red"" GridRow=""0"" GridColumn=""0"" />
            <Button BG=""Orange"" GridRow=""1"" GridColumn=""1"" />
            <Button BG=""Purple"" GridRow=""2"" GridColumn=""2"" />
            <Button BG=""Yellow"" GridRow=""3"" GridColumn=""3"" />
            <Button BG=""Green"" GridRow=""2"" GridColumn=""0"" />
            <Button BG=""YellowGreen"" GridRow=""3"" GridColumn=""1"" />
            <Button BG=""GreenYellow"" GridRow=""4"" GridColumn=""2"" />
            <Button BG=""OrangeRed"" GridRow=""5"" GridColumn=""3"" />
        </Grid>-->

        <!--<ListView Name=""LV1"" xmlns:System=""clr-namespace:System;assembly=mscorlib"" ItemType=""{x:Type System:Double}"">
            <ListViewColumn Width=""80px"">
                <ListViewColumn.Header>
                    <Button Content=""Test"" />
                </ListViewColumn.Header>
            </ListViewColumn>
            <ListViewColumn Header=""Test"" Width=""*"" />
        </ListView>-->

        <ListBox IsTitleVisible=""True"" xmlns:System=""clr-namespace:System;assembly=mscorlib"" ItemType=""{x:Type System:String}"">
            <ListBox.Header>
                <HeaderedContentPresenter Spacing=""5"" HeaderPosition=""Left"">
                    <HeaderedContentPresenter.Header>
                        <Rectangle Width=""16"" Height=""16"" Fill=""RoyalBlue"" HA=""Center"" />
                    </HeaderedContentPresenter.Header>
                    <TextBlock Text=""Hello World"" />
                </HeaderedContentPresenter>
            </ListBox.Header>
            <System:String>Hello!</System:String>
            <System:String>Baz</System:String>
            <System:String>Foo</System:String>
            <System:Double>115.0</System:Double>
            <System:String>Bar</System:String>
            <System:String>Long string that may require textwrapping</System:String>
            <System:String>hello world</System:String>
            <System:String>string\n with linebreaks</System:String>
            <System:String>Bar2</System:String>
        </ListBox>

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

            MGWindow Window = XAMLParser.LoadRootWindow(Desktop, xaml);
            //XAMLWindow.MakeInvisible();

            if (Window.TryGetElementByName("LV1", out MGListView<double> LV))
            {
                LV.SetItemsSource(new List<double>() { 5, 25.5, 101.11, 0.0, -52.6, -18, 12345, -99.3, 0 });
                LV.Columns[1].DataTemplate = (double val) =>
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

            return Window;
        }

        private static MGWindow LoadRegistrationWindow(Assembly CurrentAssembly, MGDesktop Desktop)
        {
            //  Parse the XAML markup into an MGWindow instance
            string ResourceName = $"{nameof(MGUI)}.{nameof(Samples)}.Windows.Registration.xaml";
            string XAML = ReadEmbeddedResourceAsString(CurrentAssembly, ResourceName);
            MGWindow Window = XAMLParser.LoadRootWindow(Desktop, XAML);

            //  Retrieve named elements from the window
            MGTextBox TextBox_Email = Window.GetElementByName<MGTextBox>("TextBox_Email");
            MGTextBox TextBox_Username = Window.GetElementByName<MGTextBox>("TextBox_Username");
            MGPasswordBox TextBox_Password = Window.GetElementByName<MGPasswordBox>("TextBox_Password");
            MGCheckBox CheckBox_TOS = Window.GetElementByName<MGCheckBox>("CheckBox_TOS");
            MGButton Button_Register = Window.GetElementByName<MGButton>("Button_Register");

            //  React to the terms of service checkbox
            CheckBox_TOS.OnCheckStateChanged += (sender, e) =>
            {
                if (e.NewValue.Value)
                {
                    Button_Register.IsEnabled = true;
                    Button_Register.Opacity = 1f;
                }
                else
                {
                    Button_Register.IsEnabled = false;
                    Button_Register.Opacity = 0.5f;
                }
            };

            //  React to the register button
            Button_Register.AddCommandHandler((btn, e) =>
            {
                if (CheckBox_TOS.IsChecked.Value)
                {
                    string Email = TextBox_Email.Text;
                    string Username = TextBox_Username.Text;
                    string Password = TextBox_Password.Password;

                    //TODO
                    //Do something with email/username/password inputs
                    //
                }
            });

            return Window;
        }

        private static MGWindow LoadCharacterStatsWindow(Assembly CurrentAssembly, MGDesktop Desktop)
        {
            //  Parse the XAML markup into an MGWindow instance
            string ResourceName = $"{nameof(MGUI)}.{nameof(Samples)}.Windows.CharacterStats.xaml";
            string XAML = ReadEmbeddedResourceAsString(CurrentAssembly, ResourceName);
            MGWindow Window = XAMLParser.LoadRootWindow(Desktop, XAML);

            return Window;
        }

        private static MGWindow LoadInventoryWindow(Assembly CurrentAssembly, MGDesktop Desktop)
        {
            //  Parse the XAML markup into an MGWindow instance
            string ResourceName = $"{nameof(MGUI)}.{nameof(Samples)}.Windows.Inventory.xaml";
            string XAML = ReadEmbeddedResourceAsString(CurrentAssembly, ResourceName);
            MGWindow Window = XAMLParser.LoadRootWindow(Desktop, XAML);

            MGButton Button_Close = Window.GetElementByName<MGButton>("Button_Close");
            Button_Close.AddCommandHandler((Button, e) =>
            {
                _ = Window.TryCloseWindow();
            });

            MGTabControl TabControl = Window.GetElementByName<MGTabControl>("Tabs");
            TabControl.SelectedTabHeaderTemplate = (TabItem) =>
            {
                MGButton Button = new(Window, new(2, 2, 2, 0), new Color(177, 78, 5).AsFillBrush().AsUniformBorderBrush(), x => TabItem.IsTabSelected = true);
                Button.Padding = new(2,1);
                Button.BackgroundBrush.NormalValue = new Color(255, 210, 132).AsFillBrush();
                Button.VerticalAlignment = VerticalAlignment.Bottom;
                return Button;
            };

            TabControl.UnselectedTabHeaderTemplate = (TabItem) =>
            {
                MGButton Button = new(Window, new(2, 2, 2, 0), new Color(177, 78, 5).AsFillBrush().AsUniformBorderBrush(), x => TabItem.IsTabSelected = true);
                Button.Padding = new(2, 3);
                Button.BackgroundBrush.NormalValue = new Color(228, 174, 110).AsFillBrush();
                Button.VerticalAlignment = VerticalAlignment.Bottom;
                return Button;
            };

            MGUniformGrid UniformGrid_Inventory = Window.GetElementByName<MGUniformGrid>("UniformGrid_Inventory");

            IFillBrush Border1 = new Color(214, 143, 84).AsFillBrush();
            IFillBrush Border2 = new Color(255, 228, 161).AsFillBrush();
            IBorderBrush CellBorderBrush = new MGDockedBorderBrush(Border2, Border1, Border1, Border2);
            UniformGrid_Inventory.CellBackground.NormalValue = new MGBorderedFillBrush(new(3), CellBorderBrush, new Color(255, 195, 118).AsFillBrush(), true);

            Window.MakeInvisible();

            return Window;
        }

        private static MGWindow LoadStylesTestWindow(Assembly CurrentAssembly, MGDesktop Desktop)
        {
            //  Parse the XAML markup into an MGWindow instance
            string ResourceName = $"{nameof(MGUI)}.{nameof(Samples)}.Windows.StylesTest.xaml";
            string XAML = ReadEmbeddedResourceAsString(CurrentAssembly, ResourceName);
            MGWindow Window = XAMLParser.LoadRootWindow(Desktop, XAML);

            return Window;
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