using FontStashSharp;
using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.FontStashSharp;
using MGUI.Shared.Helpers;
using MGUI.Shared.Input.Keyboard;
using MGUI.Shared.Rendering;
using MGUI.Shared.Text;
using MGUI.Shared.Text.Engines;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Diagnostics;
using System.IO;

namespace MGUI.Samples
{
    public class Game1 : Game, IObservableUpdate
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private MainRenderer MGUIRenderer { get; set; }
        private MGDesktop Desktop { get; set; }

        private KeyboardState _prevKeyboardState;

        //  IObservableUpdate implementation
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

            _spriteBatch = new SpriteBatch(GraphicsDevice);

            MGUIRenderer = new(new GameRenderHost<Game1>(this));
            Desktop = new(MGUIRenderer);

            InitializeTextEngines();

            //  This is a dialog with toggle buttons to launch other dialogs
            Compendium Compendium = new(Content, Desktop);
            Compendium.Show();

            base.Initialize();
        }

        /// <summary>Default SpriteFont backend (Press F1 to toggle the active text-rendering engine)</summary>
        private SpriteFontTextEngine SpriteFontEngine;
        /// <summary>Optional FontStashSharp backend. (Press F1 to toggle the active text-rendering engine)</summary>
        private FontStashSharpTextEngine FontStashSharpEngine;

        private void InitializeTextEngines()
        {
            //  You only need 1 textengine, but this sample project creates multiple engines
            //  that you can toggle between by pressing F1 for demonstration purposes

            SpriteFontEngine = new SpriteFontTextEngine(Desktop.FontManager);

            //  Initialize the FontStashSharp text engine
            try
            {
                string ttfDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"Content\Fonts\ttf"));

                FontStashSharpEngine = new FontStashSharpTextEngine();

                //  For each font you want to use:
                //  1. Read the bytes data from the .ttf file
                //  2. Create a FontSystem and add the data to the system
                //  3. Add the FontSystem to the engine (be sure to use the AddFontSystem method overload that takes in the byte[] data)

                const string FamilyName = "Arial";

                byte[] arialBytes = File.ReadAllBytes(Path.Combine(ttfDir, "arial.ttf"));
                FontSystem arialNormal = new FontSystem();
                arialNormal.AddFont(arialBytes);
                FontStashSharpEngine.AddFontSystem(FamilyName, CustomFontStyles.Normal, arialNormal, arialBytes);

                FontSystem arialBold = new FontSystem();
                arialBold.AddFont(File.ReadAllBytes(Path.Combine(ttfDir, "arialbd.ttf")));
                FontStashSharpEngine.AddFontSystem(FamilyName, CustomFontStyles.Bold, arialBold);

                FontSystem arialItalic = new FontSystem();
                arialItalic.AddFont(File.ReadAllBytes(Path.Combine(ttfDir, "ariali.ttf")));
                FontStashSharpEngine.AddFontSystem(FamilyName, CustomFontStyles.Italic, arialItalic);

                // Calibrate per-size advance widths to match SpriteFontTextEngine exactly.
                // Must be called after FontSizeScale is set (via AddFontSystem overload above).
                FontStashSharpEngine.MatchSpriteFontSizing(Desktop.FontManager);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FSS engine init failed: {ex.Message}");
                FontStashSharpEngine = null;
            }

            Desktop.TextEngine = SpriteFontEngine;
        }

        /// <summary>Toggles the active text rendering engine between <see cref="SpriteFontTextEngine"/> and <see cref="FontStashSharpTextEngine"/></summary>
        public void ToggleActiveTextEngine()
        {
            if (FontStashSharpEngine == null)
                return;

            ITextEngine CurrentEngine = Desktop.TextEngine;
            ITextEngine NewEngine = CurrentEngine is SpriteFontTextEngine ? FontStashSharpEngine : SpriteFontEngine;
            Desktop.TextEngine = NewEngine;
            Debug.WriteLine($"[TextEngine] switched to {Desktop.TextEngine.GetType().Name}");
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void Update(GameTime gameTime)
        {
            PreviewUpdate?.Invoke(this, gameTime.TotalGameTime);

            //  F1 toggles between SpriteFontTextEngine and FontStashSharpTextEngine
            KeyboardState ks = Keyboard.GetState();
            if (ks.IsKeyDown(Keys.F1) && !_prevKeyboardState.IsKeyDown(Keys.F1))
                ToggleActiveTextEngine();
            _prevKeyboardState = ks;

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