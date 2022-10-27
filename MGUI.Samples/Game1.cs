using MGUI.Core.UI;
using MGUI.Core.UI.XAML;
using MGUI.Shared.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Diagnostics;

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
                <Window Left=""500"" Top=""100"" Width=""400"" Height=""600"" BG=""Orange"" TitleText=""Window Title Goes here"">
                    <Window.TitleBar>
                        <DockPanel BG=""LightGreen"" Padding=""16,4"">
                            <TextBlock Text=""ABC"" Dock=""Left"" />
                            <CheckBox IsChecked=""true"" Dock=""Right"" HA=""Right"">
                                <TextBlock Text=""Checkable Content"" />
                            </CheckBox>
                        </DockPanel>
                    </Window.TitleBar>
                    <DockPanel Margin=""5"" BG=""White"">
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
                        </OverlayPanel>-->
                        
                        <ListView Name=""TestLV"" Dock=""Right"" HA=""Right"" xmlns:System=""clr-namespace:System;assembly=mscorlib"" ItemType=""{x:Type System:Double}"">
                            <ListViewColumn Width=""55px"">
                                <ListViewColumn.HeaderContent>
                                    <Button Content=""Test"" BG=""LightGray"" />
                                </ListViewColumn.HeaderContent>
                            </ListViewColumn>
                            <ListViewColumn HeaderContent=""Test"" Width=""*"" />
                        </ListView>
                        <Grid RowLengths=""Auto,1.5*,2*,60"" ColumnLengths=""Auto,*"">
                            <Button BG=""Red"" GridRow=""0"" GridColumn=""0"" MinWidth=""26"" MinHeight=""30"" />
                            <Button BG=""Orange"" GridRow=""1"" GridColumn=""0"" MinWidth=""36"" />
                            <Button BG=""Purple"" GridRow=""2"" GridColumn=""0"" MinWidth=""50"" />
                            <Button BG=""Yellow"" GridRow=""3"" GridColumn=""0"" MinWidth=""10"" />
                            <Button BG=""LightGreen"" GridRow=""0"" GridColumn=""1"" MinHeight=""75"" />
                            <Button BG=""Cyan"" GridRow=""1"" GridColumn=""1"" />
                            <Button BG=""Crimson"" GridRow=""2"" GridColumn=""1"" />
                            <Button BG=""Blue"" GridRow=""3"" GridColumn=""1"" />
                        </Grid>
                    </DockPanel>
                </Window>
            ";

            MGWindow XAMLWindow = XAMLParser.LoadRootWindow(Desktop, xaml);
            this.Desktop.Windows.Add(XAMLWindow);

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