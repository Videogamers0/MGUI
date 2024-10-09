using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Shared.Helpers;
using MGUI.Shared.Input.Keyboard;
using MGUI.Shared.Rendering;
using MGUI.Shared.Text;
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

            this.MGUIRenderer = new(new GameRenderHost<Game1>(this));
            this.Desktop = new(MGUIRenderer);

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
                            //  Also note that SpriteFontPlus uses px heights when generating the sprite font which isn't the same as font pts.
                            //  So for example, using an MGTextBlock with FontSize=12 will be smaller text than 12pt font that you're probably used to
                            ReadOnlyCollection<int> DesiredSizes = new List<int>() { 12, 14, 16, 20, 24 }.AsReadOnly();
                            ReadOnlyCollection<CustomFontStyles> DesiredStyles = new List<CustomFontStyles>() {
                                CustomFontStyles.Normal, CustomFontStyles.Bold, CustomFontStyles.Italic
                            }.AsReadOnly();

                            foreach (int FontSize in DesiredSizes)
                            {
                                foreach (CustomFontStyles FontStyle in DesiredStyles)
                                {
                                    FontMetadata Metadata = new FontMetadata(FontSize, FontStyle.HasFlag(CustomFontStyles.Bold), FontStyle.HasFlag(CustomFontStyles.Italic));
                                    SpriteFont SF = TtfFontBaker.Bake(FileStreamsLookup[FontStyle], FontSize, FontBitmapWidth, FontBitmapHeight, FontCharacterRanges).CreateSpriteFont(GraphicsDevice);
                                    SpriteFonts.Add(SF, Metadata);
                                }
                            }

                            Desktop.FontManager.AddFontSet(new FontSet("Century Gothic", SpriteFonts));

                            //  Now that the Century Gothic font set has been added to the FontManager, we can use MGTextBlocks with FontFamily="Century Gothic"
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