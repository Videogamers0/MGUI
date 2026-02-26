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

        /// <summary>Default SpriteFont backend.</summary>
        private SpriteFontTextEngine _sfEngine;
        /// <summary>Optional FontStashSharp backend. Press F1 to toggle between engines.</summary>
        private FontStashSharpTextEngine _fssEngine;
        private KeyboardState _prevKeyboardState;
        private bool _diagnosticRun;

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

            // Keep a reference to the default SF engine for diagnostics / toggling back.
            _sfEngine = new SpriteFontTextEngine(Desktop.FontManager);
            Desktop.TextEngine = _sfEngine;

            // ── FontStashSharp engine (F1 to toggle) ──────────────────────────────────
            try
            {
                string ttfDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"Content\Fonts\ttf"));

                _fssEngine = new FontStashSharpTextEngine();

                byte[] arialBytes = File.ReadAllBytes(Path.Combine(ttfDir, "arial.ttf"));
                var arialNormal = new FontSystem();
                arialNormal.AddFont(arialBytes);
                // Pass the raw TTF so FontSizeScale is auto-computed from the font metrics
                _fssEngine.AddFontSystem("Arial", CustomFontStyles.Normal, arialNormal, arialBytes);

                var arialBold = new FontSystem();
                arialBold.AddFont(File.ReadAllBytes(Path.Combine(ttfDir, "arialbd.ttf")));
                _fssEngine.AddFontSystem("Arial", CustomFontStyles.Bold, arialBold);

                var arialItalic = new FontSystem();
                arialItalic.AddFont(File.ReadAllBytes(Path.Combine(ttfDir, "ariali.ttf")));
                _fssEngine.AddFontSystem("Arial", CustomFontStyles.Italic, arialItalic);

                // Calibrate per-size advance widths to match SpriteFontTextEngine exactly.
                // Must be called after FontSizeScale is set (via AddFontSystem overload above).
                _fssEngine.MatchSpriteFontSizing(Desktop.FontManager);
            }
            catch (Exception ex) 
            { 
                Debug.WriteLine($"FSS engine init failed: {ex.Message}"); 
                _fssEngine = null; 
            }
            // ─────────────────────────────────────────────────────────────────────────

            //  This is a dialog with toggle buttons to launch other dialogs
            Compendium Compendium = new(Content, Desktop);
            Compendium.Show();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Run diagnostic once on first load so both engines are fully initialized.
            if (!_diagnosticRun && _sfEngine != null && _fssEngine != null)
            {
                TextEngineDiagnostic.Run(_sfEngine, _fssEngine);
                _diagnosticRun = true;
            }
        }

        protected override void Update(GameTime gameTime)
        {
            PreviewUpdate?.Invoke(this, gameTime.TotalGameTime);

            // F1 toggles between SpriteFontTextEngine and FontStashSharpTextEngine
            KeyboardState ks = Keyboard.GetState();
            if (_fssEngine != null &&
                ks.IsKeyDown(Keys.F1) && !_prevKeyboardState.IsKeyDown(Keys.F1))
            {
                Desktop.TextEngine = Desktop.TextEngine is SpriteFontTextEngine
                    ? (ITextEngine)_fssEngine
                    : (ITextEngine)_sfEngine;
                Debug.WriteLine($"[TextEngine] switched to {Desktop.TextEngine.GetType().Name}");
                // Re-resolve all MGTextBlock font handles from the new engine and clear
                // measurement caches so layouts update in the very next frame.
                Desktop.RecalculateTextLayouts();
            }
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