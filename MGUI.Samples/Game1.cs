using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.Containers.Grids;
using MGUI.Core.UI.Text;
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
    public readonly record struct Person(int Id, string FirstName, string LastName, bool IsMale);

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

            //  Note: If reading XAML markup from .xaml files, make sure they're set to BuildAction=EmbeddedResource
            Assembly CurrentAssembly = Assembly.GetExecutingAssembly();
            MGWindow XAMLDesigner = LoadDesignerWindow(CurrentAssembly, Desktop);
            Desktop.Windows.Add(XAMLDesigner);
            Desktop.Windows.Add(LoadDebugWindow(CurrentAssembly, Desktop));
            Desktop.Windows.Add(LoadRegistrationWindow(CurrentAssembly, Desktop));
            Desktop.Windows.Add(LoadCharacterStatsWindow(CurrentAssembly, Desktop));
            Desktop.Windows.Add(LoadInventoryWindow(CurrentAssembly, Desktop));
            //Desktop.Windows.Add(LoadStylesTestWindow(CurrentAssembly, Desktop));
            //Desktop.Windows.Add(LoadNamespaceTestWindow(CurrentAssembly, Desktop));
            Desktop.Windows.Add(LoadListViewSampleWindow(CurrentAssembly, Desktop));

            Desktop.BringToFront(XAMLDesigner);

            base.Initialize();
        }

        private static string ReadEmbeddedResourceAsString(Assembly CurrentAssembly, string ResourceName)
        {
            using (Stream ResourceStream = CurrentAssembly.GetManifestResourceStream(ResourceName))
            using (StreamReader Reader = new StreamReader(ResourceStream))
            return Reader.ReadToEnd();
        }

        private static MGWindow LoadDesignerWindow(Assembly CurrentAssembly, MGDesktop Desktop)
        {
            MGWindow Window = new(Desktop, 150, 20, 500, 500);
            Window.TitleText = "XAML Designer";
            Window.SetContent(new MGDesigner(Window));
            if (Window.BackgroundBrush.NormalValue is MGSolidFillBrush SolidFill)
                Window.BackgroundBrush.NormalValue = SolidFill * 0.5f;
            return Window;
        }

        private static MGWindow LoadDebugWindow(Assembly CurrentAssembly, MGDesktop Desktop)
        {
            //  Parse the XAML markup into an MGWindow instance
            string ResourceName = $"{nameof(MGUI)}.{nameof(Samples)}.Windows.Debug.xaml";
            string XAML = ReadEmbeddedResourceAsString(CurrentAssembly, ResourceName);
            MGWindow Window = XAMLParser.LoadRootWindow(Desktop, XAML, false);

            //Window.MakeInvisible();

            if (Window.TryGetElementByName("LV1", out MGListView<double> LV))
            {
                LV.SetItemsSource(new List<double>() { 5, 25.5, 101.11, 0.0, -52.6, -18, 12345, -99.3, 0 });
                LV.Columns[1].ItemTemplate = (double val) =>
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
            MGWindow Window = XAMLParser.LoadRootWindow(Desktop, XAML, true);

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
            MGWindow Window = XAMLParser.LoadRootWindow(Desktop, XAML, true);

            return Window;
        }

        private static MGWindow LoadInventoryWindow(Assembly CurrentAssembly, MGDesktop Desktop)
        {
            //  Parse the XAML markup into an MGWindow instance
            string ResourceName = $"{nameof(MGUI)}.{nameof(Samples)}.Windows.Inventory.xaml";
            string XAML = ReadEmbeddedResourceAsString(CurrentAssembly, ResourceName);
            MGWindow Window = XAMLParser.LoadRootWindow(Desktop, XAML, true);

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
            Window.ApplySizeToContent(SizeToContent.WidthAndHeight, 100, 100, null, null, false);

            return Window;
        }

        private static MGWindow LoadStylesTestWindow(Assembly CurrentAssembly, MGDesktop Desktop)
        {
            //  Parse the XAML markup into an MGWindow instance
            string ResourceName = $"{nameof(MGUI)}.{nameof(Samples)}.Windows.StylesTest.xaml";
            string XAML = ReadEmbeddedResourceAsString(CurrentAssembly, ResourceName);
            MGWindow Window = XAMLParser.LoadRootWindow(Desktop, XAML, false);

            return Window;
        }

        private static MGWindow LoadNamespaceTestWindow(Assembly CurrentAssembly, MGDesktop Desktop)
        {
            //  Parse the XAML markup into an MGWindow instance
            string ResourceName = $"{nameof(MGUI)}.{nameof(Samples)}.Windows.NamespaceTest.xaml";
            string XAML = ReadEmbeddedResourceAsString(CurrentAssembly, ResourceName);
            MGWindow Window = XAMLParser.LoadRootWindow(Desktop, XAML, false);
            return Window;
        }

        private static MGWindow LoadListViewSampleWindow(Assembly CurrentAssembly, MGDesktop Desktop)
        {
            //  Parse the XAML markup into an MGWindow instance
            string ResourceName = $"{nameof(MGUI)}.{nameof(Samples)}.Windows.ListViewSample.xaml";
            string XAML = ReadEmbeddedResourceAsString(CurrentAssembly, ResourceName);
            MGWindow Window1 = XAMLParser.LoadRootWindow(Desktop, XAML, false);

            //  Get the ListView
            MGListView<Person> ListView_Sample1 = Window1.GetElementByName<MGListView<Person>>("ListView_Sample1");

            //  Set the ItemTemplate of each column
            ListView_Sample1.Columns[0].ItemTemplate = (person) => new MGTextBlock(Window1, person.Id.ToString()) { HorizontalAlignment = HorizontalAlignment.Center };
            ListView_Sample1.Columns[1].ItemTemplate = (person) => new MGTextBlock(Window1, person.FirstName, person.IsMale ? Color.Blue : Color.Pink);
            ListView_Sample1.Columns[2].ItemTemplate = (person) => new MGTextBlock(Window1, person.LastName, person.IsMale ? Color.Blue : Color.Pink);

            //  Set the row data of the ListView
            List<Person> People = new()
            {
                new(1, "John", "Smith", true),
                new(2, "James", "Johnson", true),
                new(3, "Emily", "Doe", false),
                new(4, "Chris", "Brown", true),
                new(5, "Melissa", "Wilson", false),
                new(6, "Richard", "Anderson", true),
                new(7, "Taylor", "Moore", false),
                new(8, "Tom", "Lee", true),
                new(9, "Joe", "White", true),
                new(10, "Alice", "Wright", false)
            };
            ListView_Sample1.SetItemsSource(People);

            return Window1;
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