using MGUI.Shared.Helpers;
using MGUI.Shared.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Triangulation;
using MonoGame.Extended.VectorDraw;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using XNAColor = Microsoft.Xna.Framework.Color;
using WindingOrder = MonoGame.Extended.Triangulation.WindingOrder;

namespace MGUI.Shared.Rendering
{
    //TODO: Maybe a 'SetEffect'/'SetEffectTemporary'? Could put the Effect in DrawSettings, default value = null
    //Anytime the effect is being changed, must call SetDrawSettings/SetDrawSettingsTemporary just like SetTransform/SetTransformTemporary do

    public enum DrawContext
    {
        None,
        Sprites,
        Primitives
    }

    public class DrawTransaction : IDisposable
    {
        public MainRenderer Renderer { get; }
        public FontManager FontManager => Renderer.FontManager;
        public GraphicsDevice GD => Renderer.GD;
        public SpriteBatch SB => Renderer.SB;
        private PrimitiveBatch PB => Renderer.PB;
        public PrimitiveDrawing PD { get; }

        public SolidColorTexture BlackPixel => Renderer.GetOrCreateSolidColorTexture(Color.Black);
        /// <summary>A solid white, 1 pixel wide/tall Texture. Useful for drawing Colored squares. 
        /// (Color can be specified in the 'Color' mask parameter of SpriteBatch.Draw(...))</summary>
        public SolidColorTexture WhitePixel => Renderer.GetOrCreateSolidColorTexture(Color.White);

        private DrawContext CurrentContext = DrawContext.None;

        private Matrix PrimitiveProjectionMatrix { get; }

        public DrawSettings CurrentSettings { get; private set; }
        public DrawSettings PreviousSettings { get; private set; }

        /// <param name="Settings">See also: <see cref="DrawSettings.Default"/></param>
        /// <param name="DeferBegin">If true, <see cref="SpriteBatch.Begin(SpriteSortMode, BlendState, SamplerState, DepthStencilState, RasterizerState, Effect, Matrix?)"/> or <see cref="PrimitiveBatch.Begin(ref Matrix, ref Matrix)"/><para/>
        /// will not be invoked until you call a draw-related function within <see cref="DrawTransaction"/>, such as <see cref="DrawTextureTo(Texture2D, Rectangle?, Rectangle)"/></param>
        /// <param name="DefaultContext">Only relevant if <paramref name="DeferBegin"/>==false. The default drawing context to immediately start.</param>
        public DrawTransaction(MainRenderer Renderer, DrawSettings Settings, bool DeferBegin, DrawContext DefaultContext = DrawContext.Sprites)
        {
            this.Renderer = Renderer ?? throw new ArgumentNullException(nameof(Renderer));
            this.PD = new PrimitiveDrawing(PB);
            this.CurrentSettings = Settings ?? throw new ArgumentNullException(nameof(Settings));

            this.PrimitiveProjectionMatrix = Matrix.CreateOrthographicOffCenter(0, GD.Viewport.Width, GD.Viewport.Height, 0, 0, 1);

            if (!DeferBegin)
            {
                BeginDraw(DefaultContext);
            }
        }

        /// <summary>Should only be invoked if you intend to make a draw-related call that isn't already funneled through <see cref="DrawTransaction"/>.<para/>
        /// Draw-related calls that are funneled through <see cref="DrawTransaction"/> (such as <see cref="DrawTransaction.StrokeRectangle(Vector2, RectangleF, Color, Thickness, DrawContext?)"/>) will already call the begin methods if necessary.</summary>
        public void ForceBeginDraw(DrawContext Context) => BeginDraw(Context);
        public void ForceEndDraw(DrawContext Context) => EndDraw(Context);

        private void BeginDraw(DrawContext Context)
        {
            if (Context == DrawContext.None)
            {
                EndDraw(CurrentContext);
            }
            else if (CurrentContext != Context)
            {
                if (CurrentContext != DrawContext.None)
                    EndDraw(CurrentContext);

                switch (Context)
                {
                    case DrawContext.Sprites: CurrentSettings.BeginDraw(SB); break;
                    case DrawContext.Primitives: PB.Begin(PrimitiveProjectionMatrix, CurrentSettings.Transform); break;
                    default: throw new NotImplementedException($"Unrecognized {nameof(DrawContext)}: {Context}");
                }

                CurrentContext = Context;
            }
        }

        private void EndDraw(DrawContext Context)
        {
            if (Context != DrawContext.None && CurrentContext == Context)
            {
                switch (CurrentContext)
                {
                    case DrawContext.Sprites: SB.End(); break;
                    case DrawContext.Primitives: PB.End(); break;
                    default: throw new NotImplementedException($"Unrecognized {nameof(DrawContext)}: {CurrentContext}");
                }

                CurrentContext = DrawContext.None;
            }
        }

        #region Draw
        #region Draw Texture
        /// <summary>Draw a texture to a given <paramref name="Destination"/> <see cref="Rectangle"/></summary>
        public void DrawTextureTo(Texture2D Texture, Rectangle? Source, Rectangle Destination)
        {
            DrawTextureTo(Texture, Source, Destination, Color.White);
        }

        /// <summary>Draw a texture to a given <paramref name="Destination"/> <see cref="Rectangle"/></summary>
        public void DrawTextureTo(Texture2D Texture, Rectangle? Source, Rectangle Destination, Color ColorMask)
        {
            if (Destination.Width < 1 || Destination.Height < 1)
                return;

            BeginDraw(DrawContext.Sprites);
            SB.Draw(Texture, Destination, Source, ColorMask);
        }

        /// <summary>Draw a texture to a given <paramref name="Destination"/> <see cref="Rectangle"/></summary>
        public void DrawTextureTo(Texture2D Texture, Rectangle? Source, Rectangle Destination, Color ColorMask,
            Vector2 Origin, float Rotation = 0f, float Depth = 0f, SpriteEffects Effects = SpriteEffects.None)
        {
            if (Destination.Width < 1 || Destination.Height < 1)
                return;

            BeginDraw(DrawContext.Sprites);
            SB.Draw(Texture, Destination, Source, ColorMask, Rotation, Origin, Effects, Depth);
        }

        /// <summary>Draw a texture at a given <paramref name="Destination"/> point</summary>
        public void DrawTextureAt(Texture2D Texture, Rectangle? Source, Vector2 Destination)
        {
            DrawTextureAt(Texture, Source, Destination, Color.White);
        }

        /// <summary>Draw a texture at a given <paramref name="Destination"/> point</summary>
        public void DrawTextureAt(Texture2D Texture, Rectangle? Source, Vector2 Destination, Color ColorMask)
        {
            BeginDraw(DrawContext.Sprites);
            SB.Draw(Texture, Destination, Source, ColorMask);
        }

        /// <summary>Draw a texture at a given <paramref name="Destination"/> point</summary>
        public void DrawTextureAt(Texture2D Texture, Rectangle? Source, Vector2 Destination, Color ColorMask,
            Vector2 Origin, float Rotation = 0f, float ScaleX = 1f, float ScaleY = 1f, float Depth = 0f, SpriteEffects Effects = SpriteEffects.None)
        {
            BeginDraw(DrawContext.Sprites);
            if (ScaleX == ScaleY)
                SB.Draw(Texture, Destination, Source, ColorMask, Rotation, Origin, ScaleX, Effects, Depth);
            else
                SB.Draw(Texture, Destination, Source, ColorMask, Rotation, Origin, new Vector2(ScaleX, ScaleY), Effects, Depth);
        }
        #endregion Draw Texture

        #region Draw Text
        public void DrawSpriteFontText(SpriteFont Font, string Text, Vector2 Position, Color Color, 
            Vector2 Origin, float ScaleX = 1f, float ScaleY = 1f, float Rotation = 0f, float Depth = 0f, SpriteEffects Effects = SpriteEffects.None)
        {
            BeginDraw(DrawContext.Sprites);
            if (ScaleX == ScaleY)
                SB.DrawString(Font, Text, Position, Color, Rotation, Origin, ScaleX, Effects, Depth);
            else
                SB.DrawString(Font, Text, Position, Color, Rotation, Origin, new Vector2(ScaleX, ScaleY), Effects, Depth);
        }

        /// <param name="Family">The font to use</param>
        /// <param name="DesiredFontSize">The desired size of the <see cref="SpriteFont"/>, in points.</param>
        /// <param name="Exact">If true, will attempt to render the text at exactly the given <paramref name="DesiredFontSize"/>.<br/>
        /// If false, treats <paramref name="DesiredFontSize"/> as an approximation, and may render the text slightly larger or smaller to avoid blurriness</param>
        public Vector2 DrawShadowedText(string Family, string Text, Vector2 Position, Color TextColor, Color ShadowColor,
            int DesiredFontSize, float XOffset = 1, float YOffset = 1, bool Exact = false)
            => DrawShadowedText(Family, CustomFontStyles.Normal, Text, Position, TextColor, ShadowColor, DesiredFontSize, XOffset, YOffset, Exact);

        /// <param name="Family">The font to use</param>
        /// <param name="DesiredFontSize">The desired size of the <see cref="SpriteFont"/>, in points.</param>
        /// <param name="Exact">If true, will attempt to render the text at exactly the given <paramref name="DesiredFontSize"/>.<br/>
        /// If false, treats <paramref name="DesiredFontSize"/> as an approximation, and may render the text slightly larger or smaller to avoid blurriness</param>
        public Vector2 DrawShadowedText(string Family, CustomFontStyles Style, string Text, Vector2 Position, Color TextColor, Color ShadowColor, 
            int DesiredFontSize, float XOffset = 1, float YOffset = 1, bool Exact = false)
        {
            DrawText(Family, Style, Text, Position + new Vector2(XOffset, YOffset), ShadowColor, DesiredFontSize, Exact);
            return DrawText(Family, Style, Text, Position, TextColor, DesiredFontSize, Exact);
        }

        public Vector2 MeasureText(string Family, CustomFontStyles Style, string Text, int DesiredFontSize, bool Exact = false)
        {
            if (FontManager.TryGetFont(Family, Style, DesiredFontSize, true, out FontSet FS, out SpriteFont SF, out int Size, out float ExactScale, out float SuggestedScale))
            {
                float Scale = Exact ? ExactScale : SuggestedScale;
                Vector2 TextSize = new Vector2(SF.MeasureString(Text).X, FS.Heights[Size]) * Scale;
                return TextSize;
            }
            else
            {
                return Vector2.Zero;
            }
        }

        /// <summary>Renders the given <paramref name="Text"/> using <see cref="CustomFontStyles.Normal"/> style.</summary>
        /// <param name="Family">The font to use</param>
        /// <param name="DesiredFontSize">The desired size of the <see cref="SpriteFont"/>, in points.</param>
        /// <param name="Exact">If true, will attempt to render the text at exactly the given <paramref name="DesiredFontSize"/>.<br/>
        /// If false, treats <paramref name="DesiredFontSize"/> as an approximation, and may render the text slightly larger or smaller to avoid blurriness</param>
        public Vector2 DrawText(string Family, string Text, Vector2 Position, Color Color, int DesiredFontSize, bool Exact = false)
            => DrawText(Family, CustomFontStyles.Normal, Text, Position, Color, DesiredFontSize, Exact);

        /// <param name="Family">The font to use</param>
        /// <param name="DesiredFontSize">The desired size of the <see cref="SpriteFont"/>, in points.</param>
        /// <param name="Exact">If true, will attempt to render the text at exactly the given <paramref name="DesiredFontSize"/>.<br/>
        /// If false, treats <paramref name="DesiredFontSize"/> as an approximation, and may render the text slightly larger or smaller to avoid blurriness</param>
        public Vector2 DrawText(string Family, CustomFontStyles Style, string Text, Vector2 Position, Color Color, int DesiredFontSize, bool Exact = false)
        {
            if (FontManager.TryGetFont(Family, Style, DesiredFontSize, true, out FontSet FS, out SpriteFont SF, out int Size, out float ExactScale, out float SuggestedScale))
            {
                BeginDraw(DrawContext.Sprites);
                float Scale = Exact ? ExactScale : SuggestedScale;

                Vector2 Origin = FS.Origins[Size];
                int TextHeight = FS.Heights[Size];
                Vector2 TextSize = new Vector2(SF.MeasureString(Text).X, FS.Heights[Size]) * Scale;
                SB.DrawString(SF, Text, Position, Color, 0f, Origin, Scale, SpriteEffects.None, 0);
                return TextSize;
            }
            else
            {
#if DEBUG
                throw new KeyNotFoundException($"No font found for {nameof(Family)}={Family} and {nameof(Style)}={Style}");
#else
                return Vector2.Zero;
#endif
            }
        }
        #endregion Draw Text

        #region Draw Geometry
        /// <summary>Returns the first non-null, non-None <see cref="DrawContext"/> from given <paramref name="Values"/></summary>
        private static DrawContext GetFirstValidDrawContext(params DrawContext?[] Values)
            => Values == null ? DrawContext.Sprites : Values.First(x => x.HasValue && x != DrawContext.None).Value;

        #region Rectangles
        public void StrokeAndFillRectangle(Vector2 Origin, RectangleF Destination, Color StrokeColor, Color FillColor, int StrokeThickness, DrawContext? PreferredContext = null)
            => StrokeAndFillRectangle(Origin, Destination, StrokeColor, FillColor, new Thickness(StrokeThickness), PreferredContext);
        public void StrokeAndFillRectangle(Vector2 Origin, RectangleF Destination, Color StrokeColor, Color FillColor, Thickness StrokeThickness, DrawContext? PreferredContext = null)
        {
            FillRectangle(Origin, Destination.GetCompressed(StrokeThickness), FillColor, PreferredContext);
            StrokeRectangle(Origin, Destination, StrokeColor, StrokeThickness, PreferredContext);
        }

        public void StrokeRectangle(Vector2 Origin, RectangleF Destination, Color Color, Thickness Thickness, DrawContext? PreferredContext = null)
        {
            if (Destination.Width.IsAlmostZero() || Destination.Height.IsAlmostZero() || Thickness.IsEmpty())
                return;

            DrawContext Ctx = GetFirstValidDrawContext(PreferredContext, CurrentContext, DrawContext.Sprites);
            BeginDraw(Ctx);

            float LeftEdgeWidth = Math.Min(Thickness.Left, Destination.Width);
            float TopEdgeHeight = Math.Min(Thickness.Top, Destination.Height);
            float RightEdgeWidth = Math.Min(Thickness.Right, Destination.Width);
            float BottomEdgeHeight = Math.Min(Thickness.Bottom, Destination.Height);

            if (LeftEdgeWidth > 0.0f && !LeftEdgeWidth.IsAlmostZero())
            {
                RectangleF LeftEdge = new(Destination.Left, Destination.Top + TopEdgeHeight, LeftEdgeWidth, Destination.Height - TopEdgeHeight - BottomEdgeHeight);
                FillRectangle(Origin, LeftEdge, Color, Ctx);
            }

            if (TopEdgeHeight > 0.0f && !TopEdgeHeight.IsAlmostZero())
            {
                RectangleF TopEdge = new(Destination.Left, Destination.Top, Destination.Width, TopEdgeHeight);
                FillRectangle(Origin, TopEdge, Color, Ctx);
            }

            if (RightEdgeWidth > 0.0f && !RightEdgeWidth.IsAlmostZero())
            {
                RectangleF RightEdge = new(Destination.Right - RightEdgeWidth, Destination.Top + TopEdgeHeight, RightEdgeWidth, Destination.Height - TopEdgeHeight - BottomEdgeHeight);
                FillRectangle(Origin, RightEdge, Color, Ctx);
            }

            if (BottomEdgeHeight > 0.0f && !BottomEdgeHeight.IsAlmostZero())
            {
                RectangleF BottomEdge = new(Destination.Left, Destination.Bottom - BottomEdgeHeight, Destination.Width, BottomEdgeHeight);
                FillRectangle(Origin, BottomEdge, Color, Ctx);
            }
        }

        public void FillRectangle(Vector2 Origin, RectangleF Destination, Color Color, DrawContext? PreferredContext = null)
        {
            if (Destination.Width.IsAlmostZero() || Destination.Height.IsAlmostZero() || Color.A == 0)
                return;

            DrawContext Ctx = GetFirstValidDrawContext(PreferredContext, CurrentContext, DrawContext.Sprites);
            BeginDraw(Ctx);

            switch (Ctx)
            {
                case DrawContext.Sprites:
                    SB.FillRectangle(Destination.GetTranslated(Origin), Color);
                    break;
                case DrawContext.Primitives:
                    Vector2[] RectangleVertices = new Vector2[4]
                    {
                        new Vector2(0, 0),
                        new Vector2(Destination.Width, 0),
                        new Vector2(Destination.Width, Destination.Height),
                        new Vector2(0, Destination.Height)
                    };
                    PD.DrawSolidPolygon(Destination.Position + Origin, RectangleVertices, Color, false);
                    //PD.DrawSolidRectangle(Origin + Destination.Position, Destination.Width, Destination.Height, Color); // This defaults to DrawSolidPolygon(outline: true) which renders differently
                    //https://github.com/craftworkgames/MonoGame.Extended/blob/a3373ac26d90c9801b71b55679f1199fa4ec22a6/src/cs/MonoGame.Extended/VectorDraw/PrimitiveDrawing.cs
                    break;
                default: throw new NotImplementedException($"Unrecognized {nameof(DrawContext)}: {Ctx}");
            }
        }
        #endregion Rectangles

        #region Circle / Ellipse
        /// <summary>The maximum number of edges to use when approximating the geometry of a circle.<para/>
        /// This value is arbitrarily chosen to maintain reasonable performance and it's unlikely that a circle will ever be drawn with this many sides.</summary>
        private const int CircleMaxSides = 256;

        /// <param name="NumSides">How many sides to use when approximating the geometry of the circle. Recommended: 16-32. Max value = <see cref="CircleMaxSides"/></param>
        public void StrokeAndFillCircle(Vector2 Center, Color StrokeColor, Color FillColor, float Radius, float StrokeThickness = 1.0f, int NumSides = 32, DrawContext? PreferredContext = null)
        {
            if (Radius <= 0.0f || Radius.IsAlmostZero())
                return;

            if (Radius.IsAlmostEqual(StrokeThickness))
                FillCircle(Center, StrokeColor, Radius, NumSides, PreferredContext);
            else
            {
                FillCircle(Center, FillColor, Radius, NumSides, PreferredContext);
                StrokeCircle(Center, StrokeColor, Radius, StrokeThickness, NumSides, PreferredContext);
            }
        }

        /// <param name="NumSides">How many sides to use when approximating the geometry of the circle. Recommended: 16-32. Max value = <see cref="CircleMaxSides"/></param>
        public void StrokeCircle(Vector2 Center, Color Color, float Radius, float Thickness = 1.0f, int NumSides = 32, DrawContext? PreferredContext = null)
        {
            if (Radius <= 0.0f || Radius.IsAlmostZero() || Thickness <= 0.0f || Thickness.IsAlmostZero())
                return;
            if (NumSides > CircleMaxSides)
                throw new ArgumentException($"{nameof(StrokeCircle)}.{nameof(NumSides)} cannot exceed {CircleMaxSides}.");

            DrawContext Ctx = GetFirstValidDrawContext(PreferredContext, CurrentContext, DrawContext.Sprites);
            BeginDraw(Ctx);

            switch (Ctx)
            {
                case DrawContext.Sprites:
                    SB.DrawCircle(Center, Radius, NumSides, Color, Thickness, 0.0f);
                    break;
                case DrawContext.Primitives:
                    List<Vector2> Vertices = GetCircleVertices(Center, Radius, NumSides)
                        .Select(x => x + (Center - x).NormalizedCopy().Scale(Thickness / 2f)) // Translate the vertex towards the center by half of the thickness
                        .ToList();

                    List<(Vector2 Start, Vector2 End)> Edges = Vertices.SelectConsecutivePairs(true).ToList();
                    for (int i = 0; i < Edges.Count; i++)
                    {
                        (Vector2 Start, Vector2 End) CurrentEdge = Edges[i];
                        (Vector2 Start, Vector2 End) PreviousEdge = Edges[(i - 1 + Edges.Count) % Edges.Count];

                        StrokeLineSegment(Vector2.Zero, CurrentEdge.Start, CurrentEdge.End, Color, Thickness, Ctx);

                        //  Draw a small triangle to fill in the empty space on the outer portion of the circle
                        Vector2 v0 = CurrentEdge.Start;
                        Vector2 v1 = v0 + (CurrentEdge.End - CurrentEdge.Start).RightNormal().NormalizedCopy().Scale(Thickness / 2f);
                        Vector2 v2 = v0 + (PreviousEdge.Start - CurrentEdge.Start).LeftNormal().NormalizedCopy().Scale(Thickness / 2f);
                        FillTrianglePrimitive(Vector2.Zero, v0, v1, v2, Color);
                    }
                    break;
                default: throw new NotImplementedException($"Unrecognized {nameof(DrawContext)}: {Ctx}");
            }
        }

        /// <param name="NumSides">How many sides to use when approximating the geometry of the circle. Recommended: 16-32. Max value = <see cref="CircleMaxSides"/></param>
        public void FillCircle(Vector2 Center, Color Color, float Radius, int NumSides = 32, DrawContext? PreferredContext = null)
        {
            if (Radius <= 0.0f || Radius.IsAlmostZero())
                return;
            if (NumSides > CircleMaxSides)
                throw new ArgumentException($"{nameof(FillCircle)}.{nameof(NumSides)} cannot exceed {CircleMaxSides}.");

            DrawContext Ctx = GetFirstValidDrawContext(PreferredContext, CurrentContext, DrawContext.Sprites);
            BeginDraw(Ctx);

            switch (Ctx)
            {
                case DrawContext.Sprites:
                    Texture2D CircleTexture = Renderer.GetOrCreateWhiteCircleTexture(Radius, null, null);
                    float Scale = Radius * 2 / CircleTexture.Width;
                    DrawTextureAt(CircleTexture, null, Center - new Vector2(Radius), Color, Vector2.Zero, 0, Scale, Scale);
                    break;
                case DrawContext.Primitives:
                    PD.DrawSolidEllipse(Center, new Vector2(Radius), NumSides, Color, false);
                    break;
                default: throw new NotImplementedException($"Unrecognized {nameof(DrawContext)}: {Ctx}");
            }
        }

        //TODO: StrokeAndFillEllipse StrokeEllipse FillEllipse

        //Adapted from:
        //https://github.com/craftworkgames/MonoGame.Extended/blob/a3373ac26d90c9801b71b55679f1199fa4ec22a6/src/cs/MonoGame.Extended/Math/ShapeExtensions.cs
        //https://github.com/craftworkgames/MonoGame.Extended/blob/a3373ac26d90c9801b71b55679f1199fa4ec22a6/src/cs/MonoGame.Extended/VectorDraw/PrimitiveDrawing.cs
        public static Vector2[] GetCircleVertices(Vector2 origin, double radius, int sides, double angleOffset = 0.0)
        {
            const double max = 2.0 * Math.PI;
            var points = new Vector2[sides];
            var step = max / sides;
            var theta = 0.0 + angleOffset;

            for (var i = 0; i < sides; i++)
            {
                points[i] = origin + new Vector2((float)(radius * Math.Cos(theta)), (float)(radius * Math.Sin(theta)));
                theta += step;
            }

            return points;
        }

        //Adapted from:
        //https://github.com/craftworkgames/MonoGame.Extended/blob/a3373ac26d90c9801b71b55679f1199fa4ec22a6/src/cs/MonoGame.Extended/Math/ShapeExtensions.cs
        //https://github.com/craftworkgames/MonoGame.Extended/blob/a3373ac26d90c9801b71b55679f1199fa4ec22a6/src/cs/MonoGame.Extended/VectorDraw/PrimitiveDrawing.cs
        private static Vector2[] GetEllipseVertices(Vector2 origin, float radiusX, float radiusY, int sides)
        {
            var vertices = new Vector2[sides];

            var t = 0.0;
            var dt = 2.0 * Math.PI / sides;
            for (var i = 0; i < sides; i++, t += dt)
            {
                var x = (float)(radiusX * Math.Cos(t));
                var y = (float)(radiusY * Math.Sin(t));
                vertices[i] = origin + new Vector2(x, y);
            }
            return vertices;
        }
        #endregion Circle / Ellipse

        #region Polygons
        public void StrokeAndFillPolygon(Vector2 Origin, IReadOnlyList<Vector2> Vertices, Color StrokeColor, Color FillColor, float StrokeThickness = 1.0f,
            bool CenterLinesOnVertices = true, WindingOrder? Order = null)
        {
            if (Vertices == null || !Vertices.Any())
                throw new ArgumentException(null, $"{nameof(Vertices)}");

            if (!CenterLinesOnVertices && StrokeColor == FillColor)
                FillPolygon(Origin, Vertices, StrokeColor);
            else
            {
                FillPolygon(Origin, Vertices, FillColor);
                StrokePolygon(Origin, Vertices, StrokeColor, StrokeThickness, CenterLinesOnVertices, null, DrawContext.Primitives);
            }
        }

        /// <param name="CenterLinesOnVertices">If true, the <paramref name="Vertices"/> will represent the center of the line strokes. (I.E. the stroke will extend left of the vertex by half the <paramref name="Thickness"/>, and right of the vertex by half the <paramref name="Thickness"/>)<br/>
        /// If false, the <paramref name="Vertices"/> will represent the outer portion of the line strokes, meaning that the line stroke will extend inwards towards the center of the polygon by the <paramref name="Thickness"/> amount.</param>
        /// <param name="Order">The <see cref="WindingOrder"/> of the <paramref name="Vertices"/>.<para/>
        /// Will be dynamically computed if null. (Only used if <paramref name="CenterLinesOnVertices"/> is false)</param>
        public void StrokePolygon(Vector2 Origin, IReadOnlyList<Vector2> Vertices, Color Color, float Thickness = 1.0f, bool CenterLinesOnVertices = true, WindingOrder? Order = null, DrawContext? PreferredContext = null)
        {
            if (Thickness <= 0.0f || Thickness.IsAlmostZero())
                return;
            else if (Vertices == null || !Vertices.Any())
                throw new ArgumentException(null, $"{nameof(Vertices)}");

            DrawContext Ctx = GetFirstValidDrawContext(PreferredContext, CurrentContext, DrawContext.Sprites);
            BeginDraw(Ctx);

            if (CenterLinesOnVertices)
            {
                var Edges = Vertices.SelectConsecutivePairs(true);
                foreach ((Vector2 Start, Vector2 End) in Edges)
                    StrokeLineSegment(Origin, Start, End, Color, Thickness, Ctx);
            }
            else
            {
                Order = Order ?? Triangulator.DetermineWindingOrder(Vertices.Append(Vertices[0]).ToArray());

                switch (Ctx)
                {
                    case DrawContext.Sprites:
                        SB.DrawPolygon(Origin, Triangulator.EnsureWindingOrder(Vertices.ToArray(), WindingOrder.CounterClockwise), Color, Thickness, 0.0f);
                        break;
                    case DrawContext.Primitives:
                        var Edges = Vertices.SelectConsecutivePairs(true);
                        switch (Order.Value)
                        {
                            //  Note: We offset the vertices towards the center by the thickness amount when drawing the triangles,
                            //  so that the vertices represent the outer bounds of the shape and the line is drawn inwards
                            case WindingOrder.Clockwise:
                                foreach ((Vector2 Start, Vector2 End) in Edges)
                                {
                                    Vector2 Offset = (End - Start).RightNormal().NormalizedCopy().Scale(Thickness);
                                    FillTrianglePrimitive(Origin, Start, End, End + Offset, Color);
                                    FillTrianglePrimitive(Origin, End + Offset, Start + Offset, Start, Color);
                                }
                                break;
                            case WindingOrder.CounterClockwise:
                                foreach ((Vector2 Start, Vector2 End) in Edges)
                                {
                                    Vector2 Offset = (End - Start).LeftNormal().NormalizedCopy().Scale(Thickness);
                                    FillTrianglePrimitive(Origin, Start, End, End + Offset, Color);
                                    FillTrianglePrimitive(Origin, End + Offset, Start + Offset, Start, Color);
                                }
                                break;
                            default: throw new NotImplementedException($"Unrecognized {nameof(WindingOrder)}: {Order}");
                        }
                        break;
                    default: throw new NotImplementedException($"Unrecognized {nameof(DrawContext)}: {Ctx}");
                }
            }
        }

        public void FillPolygon(Vector2 Origin, IEnumerable<Vector2> Vertices, Color Color)
        {
            if (Vertices == null || !Vertices.Any())
                throw new ArgumentException(null, $"{nameof(Vertices)}");

            BeginDraw(DrawContext.Primitives);
            //Vector2[] CCWVertices = Triangulator.EnsureWindingOrder(Vertices.ToArray(), WindingOrder.CounterClockwise);
            //PD.DrawSolidPolygon(Origin, CCWVertices, Color, false);
            PD.DrawSolidPolygon(Origin, Vertices.ToArray(), Color, false);
        }
        #endregion Polygons

        #region Points
        public enum PointShape
        {
            Square,
            Circle
        }

        public void StrokeAndFillPoint(Vector2 Position, Color StrokeColor, Color FillColor, float Radius = 3.0f, int StrokeThickness = 1, PointShape Shape = PointShape.Circle, DrawContext? PreferredContext = null)
        {
            if (Radius.IsAlmostEqual(StrokeThickness))
                FillPoint(Position, StrokeColor, Radius, Shape, PreferredContext);
            else
            {
                FillPoint(Position, FillColor, Radius, Shape, PreferredContext);
                StrokePoint(Position, StrokeColor, Radius, StrokeThickness, Shape, PreferredContext);
            }
        }

        public void StrokePoint(Vector2 Position, Color Color, float Radius = 1.0f, int Thickness = 1, PointShape Shape = PointShape.Circle, DrawContext? PreferredContext = null)
        {
            if (Radius <= 0.0f || Radius.IsAlmostZero() || Thickness <= 0)
                return;

            if (Radius.IsAlmostEqual(Thickness))
            {
                FillPoint(Position, Color, Radius, Shape, PreferredContext);
            }
            else
            {
                switch (Shape)
                {
                    case PointShape.Circle:
                        StrokeCircle(Position, Color, Radius, Thickness, 32, PreferredContext);
                        break;
                    case PointShape.Square:
                        StrokeRectangle(Vector2.Zero, new RectangleF(Position.X - Radius, Position.Y - Radius, Radius * 2, Radius * 2), Color, new Thickness(Thickness), PreferredContext);
                        break;
                    default: throw new NotImplementedException($"Unrecognized {nameof(PointShape)}: {Shape}");
                }
            }
        }

        public void FillPoint(Vector2 Position, Color Color, float Radius = 1.0f, PointShape Shape = PointShape.Circle, DrawContext? PreferredContext = null)
        {
            if (Radius <= 0.0f || Radius.IsAlmostZero())
                return;

            switch (Shape)
            {
                case PointShape.Circle:
                    FillCircle(Position, Color, Radius, 32, PreferredContext);
                    break;
                case PointShape.Square:
                    FillRectangle(Vector2.Zero, new RectangleF(Position.X - Radius, Position.Y - Radius, Radius * 2, Radius * 2), Color, PreferredContext);
                    break;
                default: throw new NotImplementedException($"Unrecognized {nameof(PointShape)}: {Shape}");
            }
        }
        #endregion Points

        public void StrokeLineSegment(Vector2 Origin, Vector2 Start, Vector2 End, Color Color, float Thickness = 1.0f, DrawContext? PreferredContext = null)
        {
            if (Start.IsAlmostEqualTo(End) || Thickness <= 0.0f || Thickness.IsAlmostZero())
                return;

            DrawContext Ctx = GetFirstValidDrawContext(PreferredContext, CurrentContext, DrawContext.Sprites);
            BeginDraw(Ctx);

            switch (Ctx)
            {
                case DrawContext.Sprites:
                    SB.DrawLine(Origin + Start, Origin + End, Color, Thickness, 0.0f);
                    break;
                case DrawContext.Primitives:
                    Vector2 Offset = (End - Start).LeftNormal().NormalizedCopy().Scale(Thickness / 2f);
                    FillTrianglePrimitive(Origin, Start - Offset, End + Offset, End - Offset, Color);
                    FillTrianglePrimitive(Origin, Start + Offset, End + Offset, Start - Offset, Color);
                    break;
                default: throw new NotImplementedException($"Unrecognized {nameof(DrawContext)}: {Ctx}");
            }
        }

        private void FillTrianglePrimitive(Vector2 Origin, Vector2 v0, Vector2 v1, Vector2 v2, Color Color)
        {
            if (!PB.IsReady() || CurrentContext != DrawContext.Primitives)
                throw new InvalidOperationException($"{nameof(PrimitiveBatch)}.{nameof(PrimitiveBatch.Begin)} must be called before drawing anything.");
            else if (CurrentSettings.RasterizerState.CullMode != CullMode.None)
            {
                string ErrorMessage = $"{nameof(DrawTransaction)}.{nameof(FillTrianglePrimitive)} does not account for the winding order of the vertices and may not work correctly " +
                    $"if the {nameof(RasterizerState)}'s {nameof(CullMode)} is not set to '{nameof(CullMode.None)}'.";
                throw new NotImplementedException(ErrorMessage);
            }

            PB.AddVertex(v0 + Origin, Color, PrimitiveType.TriangleList);
            PB.AddVertex(v1 + Origin, Color, PrimitiveType.TriangleList);
            PB.AddVertex(v2 + Origin, Color, PrimitiveType.TriangleList);
        }

        public void FillTriangle(Vector2 Origin, Vector2 v0, Color c0, Vector2 v1, Color c1, Vector2 v2, Color c2)
        {
            if (CurrentSettings.RasterizerState.CullMode != CullMode.None)
            {
                string ErrorMessage = $"{nameof(DrawTransaction)}.{nameof(FillTriangle)} does not account for the winding order of the vertices and may not work correctly " +
                    $"if the {nameof(RasterizerState)}'s {nameof(CullMode)} is not set to '{nameof(CullMode.None)}'.";
                throw new NotImplementedException(ErrorMessage);
            }

            BeginDraw(DrawContext.Primitives);
            PB.AddVertex(v0 + Origin, c0, PrimitiveType.TriangleList);
            PB.AddVertex(v1 + Origin, c1, PrimitiveType.TriangleList);
            PB.AddVertex(v2 + Origin, c2, PrimitiveType.TriangleList);
        }

        /// <summary>Fills the given quadrilateral using <see cref="SamplerType.LinearClamp"/> to produce gradient color interpolation.</summary>
        public void FillQuadrilateralLinearClamp(Vector2 Origin, Vector2 v0, Color c0, Vector2 v1, Color c1, Vector2 v2, Color c2, Vector2 v3, Color c3)
        {
            if (CurrentSettings.RasterizerState.CullMode != CullMode.None)
            {
                string ErrorMessage = $"{nameof(DrawTransaction)}.{nameof(FillQuadrilateralLinearClamp)} does not account for the winding order of the vertices and may not work correctly " +
                    $"if the {nameof(RasterizerState)}'s {nameof(CullMode)} is not set to '{nameof(CullMode.None)}'.";
                throw new NotImplementedException(ErrorMessage);
            }

            using (SetDrawSettingsTemporary(CurrentSettings with { SamplerType = SamplerType.LinearClamp }))
            {
                BeginDraw(DrawContext.Primitives);

                PB.AddVertex(v0 + Origin, c0, PrimitiveType.TriangleList);
                PB.AddVertex(v1 + Origin, c1, PrimitiveType.TriangleList);
                PB.AddVertex(v2 + Origin, c2, PrimitiveType.TriangleList);

                PB.AddVertex(v2 + Origin, c2, PrimitiveType.TriangleList);
                PB.AddVertex(v3 + Origin, c3, PrimitiveType.TriangleList);
                PB.AddVertex(v0 + Origin, c0, PrimitiveType.TriangleList);
            }
        }

        /*
        #if DEBUG
                    if (DateTime.Now.Second % 2 == 0)
                        Ctx = DrawContext.Primitives;
                    else
                    {
                        Ctx = DrawContext.Sprites;
                        Color = Color * 0.9f;
                    }
        #endif
        */

        //Assume Transform.M11/Transform.M12 are the scale
        //if both values are the same, then you can convert a scalar to another scaled/unscaled scalar value
        #endregion Draw Geometry
        #endregion Draw

        /// <param name="ClearColor">Optional. If not null, the new render target will be immediately cleared with this color. Only used if the render target actually changed.</param>
        public void SetRenderTarget(RenderTarget2D New, Color? ClearColor)
        {
            RenderTargetBinding[] RTBs = GD.GetRenderTargets();
            RenderTarget2D CurrentTarget = RTBs.Length == 0 ? null : RTBs[0].RenderTarget as RenderTarget2D;
            if (New != CurrentTarget)
            {
                EndDraw(CurrentContext);
                GD.SetRenderTarget(New);
                if (ClearColor.HasValue)
                    GD.Clear(ClearColor.Value);
            }
        }

        /// <summary>
        /// Drawing to a temporary render target example:<para/>
        /// <code xml:space="preserve">
        /// <see cref="RenderTarget2D"/> Temp = <see cref="RenderUtils.CreateRenderTarget(GraphicsDevice, Rectangle, bool)"/>;<br/>
        /// using (Temp)<br/>
        /// {<br/>
        /// ⠀⠀⠀⠀using (<see cref="SetRenderTargetTemporary(RenderTarget2D, Color?)"/>)<br/>
        /// ⠀⠀⠀⠀{<br/>
        /// ⠀⠀⠀⠀⠀⠀⠀⠀//Draw something<br/>
        /// ⠀⠀⠀⠀}<para/>
        /// ⠀⠀⠀⠀//  Optionally, draw the temporary render target to the backbuffer<br/>
        /// ⠀⠀⠀⠀<see cref="DrawTextureAt(Texture2D, Rectangle?, Vector2)"/>;<br/>
        /// }</code>
        /// </summary>
        /// <param name="ClearColor">Optional. If not null, the new render target will be immediately cleared with this color. Only used if the render target actually changed.</param>
        public IDisposable SetRenderTargetTemporary(RenderTarget2D New, Color? ClearColor)
        {
            RenderTargetBinding[] RTBs = GD.GetRenderTargets();
            RenderTarget2D CurrentTarget = RTBs.Length == 0 ? null : RTBs[0].RenderTarget as RenderTarget2D;

            return new TemporaryChange<RenderTarget2D, Color?>(CurrentTarget, New, null, ClearColor, SetRenderTarget);
        }

        /// <param name="New">To change current settings, consider using '<see cref="CurrentSettings"/> with { ... }' record syntax.</param>
        public void SetDrawSettings(DrawSettings New)
        {
            if (New != null && New != CurrentSettings)
            {
                PreviousSettings = CurrentSettings;
                CurrentSettings = New;
                EndDraw(CurrentContext);
            }
        }

        /// <param name="New">To change current settings, consider using '<see cref="CurrentSettings"/> with { ... }' record syntax.</param>
        public IDisposable SetDrawSettingsTemporary(DrawSettings New)
        {
            return new TemporaryChange<DrawSettings>(CurrentSettings, New, SetDrawSettings);
        }

        public IDisposable SetTransformTemporary(Matrix Transform)
        {
            return SetDrawSettingsTemporary(CurrentSettings with { Transform = Transform });
        }

        /// <param name="IntersectWithCurrentClipTarget">If true, rather than replacing the clip target with the given <paramref name="Bounds"/>,<br/>
        /// the clip target will be the intersection of the current clip target and the given <paramref name="Bounds"/></param>
        public void SetClipTarget(Rectangle? Bounds, bool IntersectWithCurrentClipTarget)
        {
            Rectangle CurrentBounds = SB.GraphicsDevice.ScissorRectangle;

            bool IsScissorTesting = CurrentSettings.RasterizerState.ScissorTestEnable;
            bool ShouldScissorTest = Bounds.HasValue;

            if (IsScissorTesting && Bounds.HasValue && IntersectWithCurrentClipTarget)
            {
                Bounds = Rectangle.Intersect(Bounds.Value, CurrentBounds);
            }

            if (Bounds != CurrentBounds || IsScissorTesting != ShouldScissorTest)
            {
                EndDraw(CurrentContext);
                SB.GraphicsDevice.ScissorRectangle = Bounds ?? Renderer.GetViewport(0);
                if (ShouldScissorTest && !IsScissorTesting)
                    SetDrawSettings(CurrentSettings with { RasterizerType = RasterizerType.SolidScissorTest });
                else if (!ShouldScissorTest && IsScissorTesting)
                    SetDrawSettings(CurrentSettings with { RasterizerType = RasterizerType.Solid });
            }
        }

        /// <param name="IntersectWithCurrentClipTarget">If true, rather than replacing the clip target with the given <paramref name="Bounds"/>,<br/>
        /// the clip target will be the intersection of the current clip target and the given <paramref name="Bounds"/></param>
        public IDisposable SetClipTargetTemporary(Rectangle? Bounds, bool IntersectWithCurrentClipTarget)
        {
            return new TemporaryChange<Rectangle?>(SB.GraphicsDevice.ScissorRectangle, Bounds, 
                x => SetClipTarget(x, IntersectWithCurrentClipTarget), x => SetClipTarget(x, false));
        }

        public void DisableClipTarget()
        {
            if (CurrentSettings.RasterizerState.ScissorTestEnable)
            {
                EndDraw(CurrentContext);
                SetDrawSettings(CurrentSettings with { RasterizerType = RasterizerType.Solid });
            }
        }

        public void Dispose()
        {
            EndDraw(CurrentContext);
        }
    }
}
