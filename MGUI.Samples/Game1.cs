using MGUI.Core.UI;
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
<Window Left=""500"" Top=""100"" Width=""400"" Height=""600"" BG=""Orange"" TitleText=""[b]Window Title Text[/b]"">
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

        <Grid Name=""TestGrid"" RowLengths=""100[50,],16,*[80,]"" ColumnLengths=""1*[50,150],16,1.5*[50,60],1.2*"" Width=""296"" HA=""Center"">
            <TextBlock BG=""Red"" GridRow=""0"" GridColumn=""0"" />
            <TextBlock BG=""Yellow"" GridRow=""2"" GridColumn=""2"" />
            <TextBlock BG=""Purple"" GridRow=""0"" GridColumn=""3"" />
            <TextBlock BG=""Green"" GridRow=""2"" GridColumn=""0"" />
            <TextBlock BG=""Cyan"" GridRow=""0"" GridColumn=""2"" />
            <TextBlock BG=""Crimson"" GridRow=""2"" GridColumn=""3"" />

            <GridSplitter GridRow=""0"" GridColumn=""1"" GridRowSpan=""3""  />
            <GridSplitter GridRow=""1"" GridColumn=""0"" GridColumnSpan=""4"" />

            <TextBox GridRow=""2"" GridColumn=""3"" />
        </Grid>

        <!--<ScrollViewer>
            <StackPanel Orientation=""Vertical"">
                <Button Content=""Hello World"" />
                <Button Content=""Hello World"" />
                <Button Content=""Hello World"" />
                <Button Content=""Hello World"" />
                <Button Content=""Hello World"" />
                <Button Content=""Hello World"" />
                <Button Content=""Hello World"" />
                <Button Content=""Hello World"" />
                <ComboBox Name=""CB"" />
            </StackPanel>
        </ScrollViewer>-->
    </DockPanel>
</Window>
            ";

            MGWindow XAMLWindow = XAMLParser.LoadRootWindow(Desktop, xaml);
            this.Desktop.Windows.Add(XAMLWindow);

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

            PreviewUpdate?.Invoke(this, gameTime.ElapsedGameTime);

            Desktop.Update();
            // TODO: Add your update logic here

            base.Update(gameTime);

            EndUpdate?.Invoke(this, EventArgs.Empty);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            Desktop.Draw();
            // TODO: Add your drawing code here

            base.Draw(gameTime);
        }
    }
}