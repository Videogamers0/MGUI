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
using SpriteFontPlus;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace MGUI.Samples
{
    public class Game1 : Game, IObservableUpdate
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private MainRenderer MGUIRenderer { get; set; }
        private MGDesktop Desktop { get; set; }

        /// <summary>Optional FontStashSharp backend. Press F1 to toggle between engines.</summary>
        private FontStashSharpTextEngine _fssEngine;
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

            //  Optional: Use the SpriteFontPlus library (nuget pkg: https://www.nuget.org/packages/SpriteFontPlus) to dynamically create SpriteFonts at runtime
            //  and add them to MGDesktop.FontManager so they can be used by MGTextBlocks/MGTextBoxes etc
            try
            {
                const int FontBitmapWidth = 1024;
                const int FontBitmapHeight = 1024;

                ReadOnlyCollection<CharacterRange> FontCharacterRanges = new[]
                {
                    CharacterRange.BasicLatin,
                    CharacterRange.Latin1Supplement,
                    CharacterRange.LatinExtendedA,
                    CharacterRange.Cyrillic
                }.ToList().AsReadOnly();

                string FontsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

                //  Open a file stream for the regular, bold, and italic variants of the "Century Gothic" ttf font (you can also use the "Bold Italic" variant if you want)
                // Note: If unsure of the exact file name, search "Font settings" in the windows start menu, look for the desired font family, select the desired font face, and look at the font file value
                using (FileStream RegularStream = File.OpenRead(Path.Combine(FontsDirectory, "gothic.ttf")))
                {
                    using (FileStream BoldStream = File.OpenRead(Path.Combine(FontsDirectory, "gothicb.ttf")))
                    {
                        using (FileStream ItalicStream = File.OpenRead(Path.Combine(FontsDirectory, "gothici.ttf")))
                        {
                            Dictionary<CustomFontStyles, FileStream> FileStreamsLookup = new Dictionary<CustomFontStyles, FileStream>()
                            {
                                { CustomFontStyles.Normal, RegularStream },
                                { CustomFontStyles.Bold, BoldStream },
                                { CustomFontStyles.Italic, ItalicStream }
                            };

                            //  Use TtfFontBaker to make a SpriteFont for each font size + font style tuple
                            Dictionary<SpriteFont, FontMetadata> SpriteFonts = new();

                            //  Note: large font sizes may require a lot of memory for the spritefont file.
                            //      Even just this example of 5 sizes and 3 variants per size (15 SpriteFonts) uses over 50MB of memory!
                            //  SpriteFontPlus uses px heights. At 96 DPI, 1pt ≈ 1.333px.
                            //  We bake at ~pt×4/3 but register with the logical pt size so that
                            //  MGTextBlocks requesting FontSize=12 (pt) get a visually matching font.
                            //  Logical pt size → actual bake px size:
                            //    12 → 16,  14 → 19,  16 → 21,  20 → 27,  24 → 32
                            ReadOnlyCollection<int> DesiredSizes = new List<int>() { 12, 14, 16, 20, 24 }.AsReadOnly();
                            Dictionary<int, int> PtToPx = new() { { 12, 16 }, { 14, 19 }, { 16, 21 }, { 20, 27 }, { 24, 32 } };
                            ReadOnlyCollection<CustomFontStyles> DesiredStyles = new List<CustomFontStyles>() {
                                CustomFontStyles.Normal, CustomFontStyles.Bold, CustomFontStyles.Italic
                            }.AsReadOnly();

                            foreach (int FontSize in DesiredSizes)
                            {
                                foreach (CustomFontStyles FontStyle in DesiredStyles)
                                {
                                    int bakeSize = PtToPx.TryGetValue(FontSize, out int px) ? px : FontSize;
                                    FontMetadata Metadata = new FontMetadata(FontSize, FontStyle.HasFlag(CustomFontStyles.Bold), FontStyle.HasFlag(CustomFontStyles.Italic));
                                    SpriteFont SF = TtfFontBaker.Bake(FileStreamsLookup[FontStyle], bakeSize, FontBitmapWidth, FontBitmapHeight, FontCharacterRanges).CreateSpriteFont(GraphicsDevice);
                                    SpriteFonts.Add(SF, Metadata);
                                }
                            }

                            Desktop.FontManager.AddFontSet(new FontSet("Century Gothic", SpriteFonts));

                            //  Now that the Century Gothic font set has been added to the FontManager, we can use MGTextBlocks with FontFamily="Century Gothic"

                            // ── FontStashSharp engine (F1 to toggle) ──────────────────────────────────
                            //  Uses JetBrainsMono (bundled in MGUI.Core) so the size comparison
                            //  is purely engine-driven, independent of the Gothic font.
                            try
                            {
                                // AppContext.BaseDirectory = …/MGUI.Samples/bin/Debug/net6.0-windows/
                                string jbmDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
                                    @"..\..\..\..\MGUI.Core\Content\Fonts\JetBrainsMono\ttf"));

                                _fssEngine = new FontStashSharpTextEngine();

                                var jbmNormal = new FontSystem();
                                jbmNormal.AddFont(File.ReadAllBytes(Path.Combine(jbmDir, "JetBrainsMono-Regular.ttf")));
                                _fssEngine.AddFontSystem("JetBrainsMono", CustomFontStyles.Normal, jbmNormal);

                                var jbmBold = new FontSystem();
                                jbmBold.AddFont(File.ReadAllBytes(Path.Combine(jbmDir, "JetBrainsMono-Bold.ttf")));
                                _fssEngine.AddFontSystem("JetBrainsMono", CustomFontStyles.Bold, jbmBold);

                                var jbmItalic = new FontSystem();
                                jbmItalic.AddFont(File.ReadAllBytes(Path.Combine(jbmDir, "JetBrainsMono-Italic.ttf")));
                                _fssEngine.AddFontSystem("JetBrainsMono", CustomFontStyles.Italic, jbmItalic);
                            }
                            catch (Exception fssEx) { Debug.WriteLine($"FSS engine init failed: {fssEx.Message}"); _fssEngine = null; }
                            // ─────────────────────────────────────────────────────────────────────────
                        }
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine(ex); }

            //  This is a dialog with toggle buttons to launch other dialogs
            Compendium Compendium = new(Content, Desktop);
            Compendium.Show();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
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
                    : new SpriteFontTextEngine(Desktop.FontManager);
                Debug.WriteLine($"[TextEngine] switched to {Desktop.TextEngine.GetType().Name}");
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