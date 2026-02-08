using MGUI.Shared.Helpers;
using MGUI.Shared.Input;
using MGUI.Shared.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.VectorDraw;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MGUI.Shared.Rendering
{
    public interface IRenderViewport
    {
        /// <summary>The valid bounds of this viewport, relative to the topleft corner of the <see cref="GameWindow"/>.<para/>
        /// To allow rendering content to the entire <see cref="GameWindow"/>, 
        /// use a <see cref="Rectangle"/> with Left=0, Top=0, Width=<see cref="GameWindow.ClientBounds"/>.Width, Height=<see cref="GameWindow.ClientBounds"/>.Height</summary>
        public Rectangle GetBounds();
    }

    public interface IObservableUpdate
    {
        /// <summary>The <see cref="TimeSpan"/> represents the total elapsed time.<para/>
        /// See also: <see cref="GameTime.TotalGameTime"/></summary>
        public event EventHandler<TimeSpan> PreviewUpdate;
        public event EventHandler<EventArgs> EndUpdate;
    }

    /// <summary>For a concrete implementation, consider using <see cref="GameRenderHost{TObservableGame}"/></summary>
    public interface IRenderHost : IRenderViewport, IObservableUpdate, IServiceProvider
    {
        public GraphicsDevice GraphicsDevice { get; }
        public MouseState GetMouseState();
        public KeyboardState GetKeyboardState();
    }

    public class GameRenderHost<TObservableGame> : IRenderHost
        where TObservableGame : Game, IObservableUpdate
    {
        public TObservableGame Game { get; }

        public Rectangle GetBounds() => new(0, 0, Game.Window.ClientBounds.Width, Game.Window.ClientBounds.Height);

        public GraphicsDevice GraphicsDevice => Game.GraphicsDevice;

        public MouseState GetMouseState() => Mouse.GetState();
        public KeyboardState GetKeyboardState() => Keyboard.GetState();

        public object GetService(Type serviceType) => Game.Services.GetService(serviceType);

        public event EventHandler<TimeSpan> PreviewUpdate;
        public event EventHandler<EventArgs> EndUpdate;

        private Rectangle PreviousClientBounds;

        public GameRenderHost(TObservableGame Game)
        {
            this.Game = Game;
            this.Game.PreviewUpdate += (sender, e) => this.PreviewUpdate?.Invoke(Game, e);
            this.Game.EndUpdate += (sender, e) => this.EndUpdate?.Invoke(Game, e);

            PreviousClientBounds = GetBounds();
            Game.Window.ClientSizeChanged += (sender, e) =>
            {
                if (GraphicsDevice.ScissorRectangle == PreviousClientBounds)
                    GraphicsDevice.ScissorRectangle = GetBounds();
                PreviousClientBounds = GetBounds();
            };
        }
    }

    public class MainRenderer
    {
        public IRenderHost Host { get; }

        public GraphicsDevice GraphicsDevice => Host.GraphicsDevice;
        public GraphicsDevice GD => GraphicsDevice;
        public SpriteBatch SpriteBatch { get; }
        public SpriteBatch SB => SpriteBatch;
        public PrimitiveBatch PrimitiveBatch { get; }
        public PrimitiveBatch PB => PrimitiveBatch;

        public ContentManager Content { get; }

        public FontManager FontManager { get; }

        public InputTracker Input { get; }
        public UpdateBaseArgs UpdateArgs { get; private set; }

        private TimeSpan PreviousUpdateTimeSpan = TimeSpan.Zero;

        public Rectangle GetViewport(int Margin) => Host.GetBounds().GetCompressed(Margin);

        /// <param name="Host">Consider using an instance of <see cref="GameRenderHost{TObservableGame}"/> to quickly create an implementation of <see cref="IRenderHost"/></param>
        public MainRenderer(IRenderHost Host)
        {
            this.Host = Host;
            this.SpriteBatch = new(GraphicsDevice);
            this.PrimitiveBatch = new(GraphicsDevice, 1024);
            this.Content = new(Host, "Content");
            this.FontManager = new(Content, "Arial");
            this.Input = new();

            this.ScrollMarker = Content.Load<Texture2D>(Path.Combine("Icons", "ScrollMarker"));

            Host.PreviewUpdate += (sender, e) =>
            {
                this.UpdateArgs = new(e, e.Subtract(PreviousUpdateTimeSpan), this.Host.GetMouseState(), this.Host.GetKeyboardState());
                PreviousUpdateTimeSpan = e;
                this.Input.Update(UpdateArgs);
            };

            Host.EndUpdate += (sender, e) =>
            {
                this.Input.Mouse.UpdateHandlers();
                this.Input.Keyboard.UpdateHandlers();
            };
        }

        #region Textures
        public readonly Texture2D ScrollMarker;

        #region Solid Color
        private readonly Dictionary<Color, SolidColorTexture> SolidColorTextures = new();

        public SolidColorTexture GetOrCreateSolidColorTexture(Color Color)
        {
            if (!SolidColorTextures.TryGetValue(Color, out SolidColorTexture Result))
            {
                Result = new(GD, Color);
                SolidColorTextures.Add(Color, Result);
            }

            return Result;
        }
        #endregion Solid Color

        #region Circles
        private const int MinimumCircleTextureRadius = 32;
        private const int MaximumCircleTextureRadius = 1024;

        private readonly Dictionary<int, Texture2D> CircleTextures = new();

        /// <summary>Returns a circle texture whose radius is at least as big as <paramref name="DesiredRadius"/>.<para/>
        /// Attempts to round <paramref name="DesiredRadius"/> up to the nearest power of 2 to prevent generating too many textures.</summary>
        /// <param name="MinimumRadius">If null, uses <see cref="Math.Floor(double)"/> of <paramref name="DesiredRadius"/>.<para/>
        /// The absolute minimum radius used is <see cref="MinimumCircleTextureRadius"/></param>
        /// <param name="MaximumRadius">If null, uses <see cref="GeneralUtils.NextPowerOf2(float)"/> of <paramref name="DesiredRadius"/>.<para/>
        /// The absolute maximum radius used is <see cref="MaximumCircleTextureRadius"/></param>
        public Texture2D GetOrCreateWhiteCircleTexture(float DesiredRadius, int? MinimumRadius = null, int? MaximumRadius = null)
        {
            MaximumRadius = Math.Clamp(MaximumRadius ?? GeneralUtils.NextPowerOf2(DesiredRadius), MinimumCircleTextureRadius, MaximumCircleTextureRadius);
            MinimumRadius = Math.Clamp(MinimumRadius ?? (int)Math.Floor(DesiredRadius), MinimumCircleTextureRadius, MaximumRadius.Value);

            IEnumerable<KeyValuePair<int, Texture2D>> Matches = CircleTextures
                .Where(x => x.Value != null && !x.Value.IsDisposed && x.Key >= MinimumRadius && x.Key <= MaximumRadius)
                .OrderBy(x => Math.Abs(DesiredRadius - x.Key));

            if (Matches.Any())
                return Matches.First().Value;
            else
            {
                DesiredRadius = Math.Min(DesiredRadius, MaximumRadius.Value);
                int ActualRadius = Math.Clamp(GeneralUtils.NextPowerOf2(DesiredRadius), MinimumRadius.Value, MaximumRadius.Value);
                Texture2D Circle = TextureUtils.CreateCircleTexture(this.SB, ActualRadius, Color.White, true);
                CircleTextures[ActualRadius] = Circle;
                return Circle;
            }
        }

        private void ClearDisposedCircleTextures()
        {
            IEnumerable<int> InvalidKeys = CircleTextures.Where(x => x.Value == null || x.Value.IsDisposed).Select(x => x.Key);
            if (InvalidKeys.Any())
            {
                foreach (int Key in InvalidKeys)
                    CircleTextures.Remove(Key);
            }
        }
        #endregion Circles
        #endregion Textures
    }
}
