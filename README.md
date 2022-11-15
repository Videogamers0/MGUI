# MGUI

MGUI is a UI framework for [MonoGame](https://www.monogame.net/) ([Src](https://github.com/MonoGame/MonoGame)) that features a powerful layout engine similar to WPF, and a robust set of controls to build your UI with. 

All control names are prefixed with 'MG' and have similar names and properties to what you might find in WPF. Currently supported controls:
- 'Container'-like Controls that define their own logic for arranging their children:
  - MGDockPanel
  - MGGrid
  - MGOverlayPanel
  - MGStackPanel
- Controls that can have child Content:
  - MGBorder
  - MGButton
  - MGCheckBox
  - MGComboBox
  - MGContextMenu
  - MGContextMenuItem
  - MGDesigner
  - MGExpander
  - MGGroupBox
  - MGListBox
  - MGListView
  - MGRadioButton
  - MGScrollViewer
  - MGTabControl
  - MGTabItem
  - MGToggleButton
  - MGToolTip
  - MGWindow
- Controls that cannot have child Content:
  - MGGridSplitter
  - MGImage
  - MGPasswordBox
  - MGProgressBar
  - MGRatingControl
  - MGResizeGrip
  - MGSeparator
  - MGSlider
  - MGStopwatch
  - MGTextBlock
  - MGTextBox
  - MGTimer
  
# Examples
  
A simple registration window created with MGUI:

![Register.png](assets/samples/Sample_Registration_Window.png)
<details>
  <summary>XAML Markup:</summary>
  
```xaml
<Window Left="50" Top="50" Width="300" Height="380">
    
    <!-- Replace the window's Title bar with custom content -->
    <Window.TitleBar>
        <!-- The root-element of the window's Title bar must always be an MGDockPanel, but we can add whatever we want to it -->
        <DockPanel Padding="5,2">
            <Rectangle Dock="Left" Width="16" Height="16" Stroke="Gray" StrokeThickness="1" Fill="Black" Margin="0,0,5,0">
                <Rectangle.ToolTip>
                    <ToolTip Padding="0" Width="240" Height="30" TextForeground="RoyalBlue" Background="White * 0.85">
                        <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center" Text="[b]Pretend this is a window icon[/b]" />
                    </ToolTip>
                </Rectangle.ToolTip>
            </Rectangle>
            
            <TextBlock FontSize="10" Foreground="White" Text="Registration Form" />
        </DockPanel>
    </Window.TitleBar>
    
    <DockPanel Margin="5">
        <!-- Window header -->
        <StackPanel Dock="Top" Orientation="Vertical">
            <TextBlock Dock="Top" HorizontalAlignment="Center" FontSize="14" Foreground="Orange" Text="Register for a new account" />
            <Separator Size="3" Orientation="Horizontal" Background="Orange * 0.5" />
        </StackPanel>
        
        <!-- Register -->
        <Button Name="Button_Register" Dock="Bottom" Background="Orange" HorizontalAlignment="Center" Padding="25,5">
            <TextBlock Text="[b]Register[/b]" FontSize="14" />
        </Button>
        
        <Border BorderBrush="Black" BorderThickness="1">
            <ScrollViewer VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Disabled" Padding="8" Margin="0,10,0,15" Background="White * 0.08">
                <StackPanel Orientation="Vertical">
                    <StackPanel.Styles>
                        <Style TargetType="HeaderedContentPresenter">
                            <Setter Property="HeaderPosition" Value="Top" />
                            <Setter Property="Spacing" Value="2" />
                        </Style>
                    </StackPanel.Styles>
                    
                    <!-- Email -->
                    <HeaderedContentPresenter>
                        <HeaderedContentPresenter.Header>
                            <TextBlock Text="Email:" Foreground="Gray" />
                        </HeaderedContentPresenter.Header>
                        <TextBox Name="TextBox_Email" PlaceholderText="[i][opacity=0.5]Email[/opacity][/i]" CharacterLimit="40" ShowCharacterCount="false" AcceptsReturn="false" AcceptsTab="false" />
                    </HeaderedContentPresenter>
                    <Spacer Height="8" />
                    
                    <!-- Username -->
                    <HeaderedContentPresenter>
                        <HeaderedContentPresenter.Header>
                            <TextBlock Text="Username:" Foreground="Gray" />
                        </HeaderedContentPresenter.Header>
                        <TextBox Name="TextBox_Username" PlaceholderText="[i][opacity=0.5]Username[/opacity][/i]" CharacterLimit="16" ShowCharacterCount="true" AcceptsReturn="false" AcceptsTab="false">
                            <TextBox.ToolTip>
                                <ToolTip Width="320" Height="64" TextForeground="Red" HorizontalContentAlignment="Center" VerticalContentAlignment="Center">
                                    <TextBlock WrapText="True" LinePadding="10" FontSize="10" Text="Must contain only alphanumeric characters.\nCannot exceed [b]16[/b] characters." />
                                </ToolTip>
                            </TextBox.ToolTip>
                        </TextBox>
                    </HeaderedContentPresenter>
                    <Spacer Height="8" />

                    <!-- Password -->
                    <HeaderedContentPresenter>
                        <HeaderedContentPresenter.Header>
                            <TextBlock Text="Password:" Foreground="Gray" />
                        </HeaderedContentPresenter.Header>
                        <PasswordBox Name="TextBox_Password" PasswordCharacter="*" Text="Hunter2" />
                    </HeaderedContentPresenter>
                    <Spacer Height="16" />
                    
                    <!-- Terms of service -->
                    <CheckBox Name="CheckBox_TOS" IsChecked="true">
                        <TextBlock Text="I agree to the [color=#3483eb][i][u]Terms of service[/u][/i][/color]" />
                    </CheckBox>
                </StackPanel>
            </ScrollViewer>
        </Border>
    </DockPanel>
</Window>
```
</details>

![Register.png](assets/samples/Sample_Inventory_Window.png)
<details>
  <summary>XAML Markup</summary>

```xaml
<Window Left="300" Top="400" Width="800" Height="500" IsUserResizable="False">
    <OverlayPanel TextForeground="Black" HorizontalAlignment="Center" VerticalAlignment="Center">
        <Button Name="Button_Close" HorizontalAlignment="Right" VerticalAlignment="Top" Content="[b]X" TextForeground="Red" Padding="3,2,3,0"
                BorderBrush="RGB(91,43,42)" BorderThickness="2" Background="rgb(242,191,114)" />
        
        <TabControl Name="Tabs" Background="RGB(255,203,123)" BorderBrush="RGB(177,78,5)" BorderThickness="0" MinWidth="300" MinHeight="300" Padding="0" Margin="0,12,12,0">
            <TabControl.HeadersPanel>
                <StackPanel Orientation="Horizontal" Margin="35,0,0,0" Spacing="0" />
            </TabControl.HeadersPanel>
            <TabItem Padding="0">
                <TabItem.Header>
                    <TextBlock Text="[b]1" Foreground="Black" Padding="5,2" />
                </TabItem.Header>

                <!-- Content of the first tab -->
                <Border BorderBrush="RGB(133,54,5)" BorderThickness="3">
                    <Border BorderBrush="RGB(250,147,5)" BorderThickness="3">
                        <Border BorderBrush="RGB(177,78,5)" BorderThickness="3">
                            <StackPanel Orientation="Vertical">
                                
                                <!-- 4x12 grid of 48x48 px slots -->
                                <UniformGrid Name="UniformGrid_Inventory" Rows="4" Columns="12" CellSize="48,48" Margin="16,8,16,12" HeaderRowHeight="15" RowSpacing="3">
                                    <UniformGrid.Styles>
                                        <Style TargetType="TextBlock">
                                            <Setter Property="Foreground" Value="Gray" />
                                            <Setter Property="FontSize" Value="8" />
                                            <Setter Property="HorizontalAlignment" Value="Center" />
                                            <Setter Property="VerticalAlignment" Value="Center" />
                                        </Style>
                                    </UniformGrid.Styles>
                                    <TextBlock Foreground="Red" Text="1" Row="0" Column="0" />
                                    <TextBlock Text="2" Row="0" Column="1" />
                                    <TextBlock Text="3" Row="0" Column="2" />
                                    <TextBlock Text="4" Row="0" Column="3" />
                                    <TextBlock Text="5" Row="0" Column="4" />
                                    <TextBlock Text="6" Row="0" Column="5" />
                                    <TextBlock Text="7" Row="0" Column="6" />
                                    <TextBlock Text="8" Row="0" Column="7" />
                                    <TextBlock Text="9" Row="0" Column="8" />
                                    <TextBlock Text="0" Row="0" Column="9" />
                                    <TextBlock Text="-" Row="0" Column="10" />
                                    <TextBlock Text="=" Row="0" Column="11" />
                                </UniformGrid>

                                <Separator Orientation="Horizontal" Size="3" Margin="0" Background="RGB(133,54,5)" />
                                <Separator Orientation="Horizontal" Size="3" Margin="0" Background="RGB(250,147,5)" />
                                <Separator Orientation="Horizontal" Size="3" Margin="0" Background="RGB(177,78,5)" />

                                <DockPanel Margin="12,10,12,12">
                                    <Grid Dock="Left" RowLengths="48,48,48,30" ColumnLengths="48,100,48">
                                        <!-- Note: If you specify 4 colors, delimited by a hyphen, it will create an MGDockedBorderBrush (Left, Top, Right, Bottom) -->
                                        <Border Row="0" Column="0" BorderBrush="RGB(255,228,161)-RGB(214,143,84)-RGB(214,143,84)-RGB(255,228,161)" BorderThickness="3" />
                                        <Border Row="1" Column="0" BorderBrush="RGB(255,228,161)-RGB(214,143,84)-RGB(214,143,84)-RGB(255,228,161)" BorderThickness="3" />
                                        <Border Row="2" Column="0" BorderBrush="RGB(255,228,161)-RGB(214,143,84)-RGB(214,143,84)-RGB(255,228,161)" BorderThickness="3" />

                                        <Border Row="0" Column="1" RowSpan="3" Margin="9,0"  BorderBrush="RGB(133,54,5)" BorderThickness="3">
                                            <Border BorderBrush="RGB(250,147,5)" BorderThickness="3">
                                                <Border BorderBrush="RGB(177,78,5)" BorderThickness="3" Background="RGB(56,170,255)">
                                                    <!--<Image Texture="texture_name..."></Image>-->
                                                </Border>
                                            </Border>
                                        </Border>

                                        <Border Row="0" Column="2" BorderBrush="RGB(255,228,161)-RGB(214,143,84)-RGB(214,143,84)-RGB(255,228,161)" BorderThickness="3" />
                                        <Border Row="1" Column="2" BorderBrush="RGB(255,228,161)-RGB(214,143,84)-RGB(214,143,84)-RGB(255,228,161)" BorderThickness="3" />
                                        <Border Row="2" Column="2" BorderBrush="RGB(255,228,161)-RGB(214,143,84)-RGB(214,143,84)-RGB(255,228,161)" BorderThickness="3" />

                                        <TextBlock Row="3" Column="0" ColumnSpan="3" Margin="0,12,0,0" Text="Character Name" HorizontalAlignment="Center" FontSize="14" />
                                    </Grid>

                                    <StackPanel Orientation="Vertical" HorizontalAlignment="Center" VerticalAlignment="Center" Spacing="12" Margin="15,15,15,65">
                                        <TextBlock Text="Farm Name" FontSize="20" HorizontalAlignment="Center" />
                                        <Grid HorizontalAlignment="Center" RowSpacing="12" ColumnSpacing="8" RowLengths="auto,auto" ColumnLengths="auto,auto">
                                            <TextBlock Row="0" Column="0" Text="Current Funds:" FontSize="18" HorizontalAlignment="Right" />
                                            <TextBlock Row="0" Column="1"  Text="999,999g" FontSize="18" HorizontalAlignment="Left" />

                                            <TextBlock Row="1" Column="0" Text="Total Earnings:" FontSize="18" HorizontalAlignment="Right" />
                                            <TextBlock Row="1" Column="1" Text="2,147,483,647g" FontSize="18" HorizontalAlignment="Left" />
                                        </Grid>
                                    </StackPanel>
                                </DockPanel>
                            </StackPanel>
                        </Border>
                    </Border>
                </Border>
            </TabItem>
            <TabItem>
                <TabItem.Header>
                    <TextBlock Text="[b]2" Foreground="Black" Padding="5,2" />
                </TabItem.Header>
            </TabItem>
            <TabItem>
                <TabItem.Header>
                    <TextBlock Text="[b]3" Foreground="Black" Padding="5,2" />
                </TabItem.Header>
            </TabItem>
            <TabItem>
                <TabItem.Header>
                    <TextBlock Text="[b]4" Foreground="Black" Padding="5,2" />
                </TabItem.Header>
            </TabItem>
            <TabItem>
                <TabItem.Header>
                    <TextBlock Text="[b]5" Foreground="Black" Padding="5,2" />
                </TabItem.Header>
            </TabItem>
            <TabItem>
                <TabItem.Header>
                    <TextBlock Text="[b]6" Foreground="Black" Padding="5,2" />
                </TabItem.Header>
            </TabItem>
            <TabItem>
                <TabItem.Header>
                    <TextBlock Text="[b]7" Foreground="Black" Padding="5,2" />
                </TabItem.Header>
            </TabItem>
            <TabItem>
                <TabItem.Header>
                    <TextBlock Text="[b]8" Foreground="Black" Padding="5,2" />
                </TabItem.Header>
            </TabItem>
        </TabControl>
    </OverlayPanel>
</Window>
```
</details>

![Register.png](assets/samples/Sample_Debug_Window.png)
  
MGUI can also parse and render your XAML markup at runtime using the MGDesigner control:
![XAML Designer](assets/samples/Sample_XAML_Designer_Window.gif)
  
# Getting Started:

1. Clone this repo
2. Use Visual Studio 2022 (since this project targets .NET 6.0, and makes use of some new-ish C# language features such as record structs)
3. In your MonoGame project:
   - In the Solution Explorer:
     - Right-click your Solution, *Add* -> *Existing Project*. Browse for `MGUI.Shared.csproj`, and `MGUI.Core.csproj`.
     - Right-click your Project, *Add* -> *Project Reference*. Add references to `MGUI.Shared and MGUI.Core`.
     - You may need to:
       - Right-click your game's *Content* folder, *Add* -> *Existing Item*. Browse for `MGUI\MGUI.Shared\Content\MGUI.Shared.Content.mgcb` and `MGUI\MGUI.Core\Content\MGUI.Core.Content.mgcb` and add them both as links (in the file browser dialog, click the dropdown arrow next to the *Add* button and choose *Add as link*). This is intended to ensure MGUI's content .xnb files are copied to your project's bin\Content folder. This step might not be necessary.
   - In your Game class:
     - In the Initialize method:
       - Instantiate `MGUI.Shared.Rendering.MainRenderer`
       - Instantiate `MGUI.Core.UI.MGDesktop`
     - Anywhere in your code, instantiate 1 or more `MGWindow` and add them to your `MGDesktop` instance via `MGDesktop.Windows`
     - In the Update method: Call `MGDesktop.Update()`
     - In the Draw method: Call `MGDesktop.Draw()`
      
<details>
  <summary>Example code for your Game class:</summary>

```c#
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
        this.MGUIRenderer = new(new GameRenderHost<Game1>(this));
        this.Desktop = new(MGUIRenderer);

        MGWindow Window1 = new(Desktop, 50, 50, 500, 200);
        Window1.TitleText = "Sample Window with a single [b]Button[/b]: [color=yellow]Click it![/color]";
        Window1.BackgroundBrush.NormalValue = new MGSolidFillBrush(Color.Orange);
        Window1.Padding = new(15);
        MGButton Button1 = new(Window1, button => { button.SetContent("I've been clicked!"); });
        Button1.SetContent("Click me!");
        Window1.SetContent(Button1);

        this.Desktop.Windows.Add(Window1);

        // TODO: Add your initialization logic here

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
```

![window1.png](assets/samples/window1.png)
</details>
  
# Multi-Platform

MGUI.Core targets `net6.0-windows` by default. If you wish to use MGUI on another OS, open `MGUI\MGUI.Core\MGUI.Core.csproj` and change `<TargetFramework>net6.0-windows</TargetFramework>` to `<TargetFramework>net6.0</TargetFramework>`. XAML parsing is only available if targeting `net6.0-windows` with `<UseWPF>True</UseWPF>`.

The following is valid if targeting `net6.0-windows` with `<UseWPF>True</UseWPF>`:
```c#
string XAMLWindow =
@"<Window Left=""50"" Top=""100"" Width=""150"" Height=""150"" Background=""Orange"" Padding=""10"">
    <Button Padding=""10,5"" HorizontalAlignment=""Center"" VerticalAlignment=""Center"" Content=""Hello World"" />
</Window>";
MGWindow Window1 = XAMLParser.LoadRootWindow(Desktop, XAMLWindow);
Desktop.Windows.Add(Window1);
```

If targeting `net6.0`, you would instead have to use c# code to define your UI:
```c#
MGWindow Window1 = new(Desktop, 50, 100, 150, 150);
Window1.BackgroundBrush.NormalValue = new MGSolidFillBrush(Color.Orange);
Window1.Padding = new(10);
MGButton Button = new(Window1);
Button.Padding = new(10, 5);
Button.HorizontalAlignment = HorizontalAlignment.Center;
Button.VerticalAlignment = VerticalAlignment.Center;
Button.SetContent("Hello World");
Window1.SetContent(Button);
Desktop.Windows.Add(Window1);
```
<sub>Everything that can be done in XAML can be also done with c# code, but not everything that can be done with c# can be done with XAML. XAML is generally more concise and convenient to use though.</sub>

# Input Handling

MGUI uses its own framework to detect and respond to inputs.

- `InputTracker`
  - The base-class for input-related logic
  - There is typically only 1 instance of InputTracker per program, automatically created when you create an instance of `MGUI.Shared.Rendering.MainRenderer`
  - Contains 2 child objects: `MouseTracker` and `KeyboardTracker`
- `MouseTracker`
  - Detects changes to the `MouseState` and stores information about those changes in EventArg objects. Most of the EventArgs have an `IsHandled` property.
  - Contains 0 to many child `MouseHandler` objects
  - `MouseHandler`
    - Each `MouseHandler` has an `IMouseHandlerHost` which defines things like the viewport that this handler detects events within.
    - Exposes several events that your code can subscribe to, to react to mouse events. Such as:
      - Scrolled, MovedInside, MovedOutside, Entered, Exited, PressedInside, PressedOutside, ReleasedInside, ReleasedOutside, DragStart, Dragged, DragEnd
    - Has logic that will prevent the same event from being invoked to several subscribers after one of the subscribers handles it (by setting `IsHandled` to `true`)
- `KeyboardTracker`
  - Detects changes to the `KeyboardState` and stores information about those changes in EventArg objects. Most of the EventArgs have an `IsHandled` property.
  - Contains 0 to many child `KeyboardHandler` objects
    - `KeyboardHandler`
      - Each `KeyboardHandler` has an `IKeyboardHandlerHost` which defines things like if the owner object currently has the keyboard focus.
      - Exposes several events that your code can subscribe to, to react to keyboard events. Such as:
        - Pressed, Clicked, Released
        - The EventArgs contain useful information such as `PrintableValue` which would be 'A' if you pressed 'a' with CAPSLOCK on, or pressed 'a' with CAPSLOCK off but Left or Right shift is held etc.
        
If you'd like to utilize this framework in your own code, get the `InputTracker` instance from your `MGUI.Shared.Rendering.MainRenderer` instance, then get the `MouseTracker` and/or `KeyboardTracker` instance from the `InputTracker`. Call `MouseTracker.CreateHandler(...)`/`KeyboardTracker.CreateHandler(...)`, and subscribe to the events in the handler instance.

<details>
  <summary>Example code:</summary>
  
Suppose you want to react to WASD keys to move your player, but you don't want to move the player if the WASD was handled by an `MGTextBox` on the UI:

```c#
public class Game1 : Game, IObservableUpdate, IKeyboardHandlerHost
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

    private KeyboardHandler PlayerMovementHandler;

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = 400;
        _graphics.PreferredBackBufferHeight = 300;
        _graphics.ApplyChanges();

        this.MGUIRenderer = new(new GameRenderHost<Game1>(this));
        this.Desktop = new(MGUIRenderer);

        // Create a simple UI that may need to handle keyboard events
        MGWindow Window1 = new(Desktop, 20, 20, 200, 100);
        Window1.Padding = new(10);
        MGTextBox TextBox = new(Window1);
        Window1.SetContent(TextBox);
        Desktop.Windows.Add(Window1);

        //  Create a KeyboardHandler instance that will respond to WASD key presses
        PlayerMovementHandler = MGUIRenderer.Input.Keyboard.CreateHandler(this, null);
        PlayerMovementHandler.Pressed += (sender, e) =>
        {
            if (e.Key is Keys.W or Keys.A or Keys.S or Keys.D)
            {
                //TODO Move the player
                e.SetHandled(this, false);
            }
        };

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

        // By updating this handler AFTER we've updated our UI, the handler won't receive events that were already handled by our UI's TextBox
        PlayerMovementHandler.ManualUpdate();

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
```

If you don't want to use MGUI's input framework, then you can just check if the EventArgs in `MouseTracker`/`KeyboardTracker` have already been handled before your code attempts to handle them:

```c#
BaseKeyPressedEventArgs W_PressEvent = MGUIRenderer.Input.Keyboard.CurrentKeyPressedEvents[Keys.W];
if (Keyboard.GetState().IsKeyDown(Keys.W) && (W_PressEvent == null || !W_PressEvent.IsHandled))
{
    //TODO do something
}
```
</details>
