using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MGUI.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MGUI.Shared.Rendering;

namespace MGUI.Core.UI
{
    public enum RatingItemShape
    {
        Star,
        Diamond,
        Circle,
        Rectangle,
        Triangle
    }

    /// <summary>Similar to a <see cref="MGSlider"/>, except this uses shapes instead of a continuous number line and thumb. 
    /// And this element always starts counting from zero instead of having a <see cref="MGSlider.Minimum"/> equivalent.<para/>
    /// Sample usage: Displaying a mission's difficulty, or allowing users to rate other user-generated content such as custom maps</summary>
    public class MGRatingControl : MGElement
    {
        private static readonly Rectangle Rect256 = new(0, 0, 256, 256);
        private static readonly Dictionary<RatingItemShape, ReadOnlyCollection<Vector2>> ShapeVertices256 = new()
        {
            { 
                RatingItemShape.Star,
                Get5PointStarVertices(Rect256).Reverse<Vector2>().ToList().AsReadOnly()
            },
            {
                RatingItemShape.Diamond,
                new List<Vector2>()
                {
                    new(Rect256.Center.X, 0),
                    new(Rect256.Right - Rect256.Width / 6, Rect256.Center.Y),
                    new(Rect256.Center.X, Rect256.Bottom),
                    new(Rect256.Left + Rect256.Width / 6, Rect256.Center.Y)
                }.AsReadOnly()
            },
            { 
                RatingItemShape.Circle,
                DrawTransaction.GetCircleVertices(Rect256.Center.ToVector2(), Math.Min(Rect256.Width / 2, Rect256.Height / 2), 32).ToList().AsReadOnly()
            },
            { 
                RatingItemShape.Rectangle, 
                Rect256.GetCorners().Select(x => x.ToVector2()).ToList().AsReadOnly()
            },
            {
                RatingItemShape.Triangle,
                new List<Vector2>()
                {
                    new(Rect256.Left, Rect256.Bottom - Rect256.Height / 8),
                    new(Rect256.Right, Rect256.Bottom - Rect256.Height / 8),
                    new(Rect256.Center.X, Rect256.Top + Rect256.Height / 8)
                }.AsReadOnly()
            }
        };

        public RatingItemShape ItemShape { get; set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _ItemSize;
        /// <summary>The dimensions, in pixels, of each item in this <see cref="MGRatingControl"/>.<para/>
        /// Default value: 16<br/>
        /// Recommended value: 16-96</summary>
        public int ItemSize
        {
            get => _ItemSize;
            set
            {
                if (_ItemSize != value)
                {
                    _ItemSize = value;
                    LayoutChanged(this, true);
                    NPC(nameof(ItemSize));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _Spacing;
        /// <summary>The spacing, in pixels, between each consecutive item.<para/>
        /// Default value: 3</summary>
        public int Spacing
        {
            get => _Spacing;
            set
            {
                if (_Spacing != value)
                {
                    _Spacing = value;
                    LayoutChanged(this, true);
                    NPC(nameof(Spacing));
                }
            }
        }

        #region Value
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private float _Minimum;
        /// <summary>The inclusive minimum that <see cref="Value"/> can be set to.<para/>
        /// To set this value, use <see cref="SetRange(float, float)"/><para/>
        /// Default value: 0<br/>
        /// Recommended value: 0, 0.5f, or 1.</summary>
        public float Minimum { get => _Minimum; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private float _Maximum;
        /// <summary>The inclusive maximum that <see cref="Value"/> can be set to.<para/>
        /// To set this value, use <see cref="SetRange(float, float)"/><para/>
        /// Default value: 5<br/>
        /// Recommended value: 5 or 10<br/>
        /// Max value: 100</summary>
        public float Maximum { get => _Maximum; }

        public int NumItems => (int)Math.Ceiling(Maximum);

        public void SetRange(float Minimum, float Maximum)
        {
            if (this.Minimum != Minimum || this.Maximum != Maximum)
            {
                float PreviousMaximum = this.Maximum;

                if (Minimum > Maximum)
                    throw new ArgumentException($"{nameof(MGSlider)}.{nameof(Minimum)} cannot be greater than {nameof(MGSlider)}.{nameof(Maximum)}");

                _Minimum = Minimum;
                _Maximum = Maximum;
                _ = SetValue(Value);

                if (PreviousMaximum != Maximum)
                    LayoutChanged(this, true);

                NPC(nameof(Minimum));
                NPC(nameof(Maximum));
                NPC(nameof(Interval));
            }
        }

        /// <summary>Convenience property that simply returns: <see cref="Maximum"/> - <see cref="Minimum"/></summary>
        public float Interval => Maximum - Minimum;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private float _Value;
        /// <summary>The current value that this <see cref="MGRatingControl"/> is set to.<para/>
        /// To set this value, use <see cref="SetValue(float)"/></summary>
        public float Value { get => _Value; }
        /// <summary>See also: <see cref="GetActualValue(float)"/></summary>
        public float SetValue(float DesiredValue)
        {
            float ActualValue = GetActualValue(DesiredValue);
            if (Value != ActualValue)
            {
                _Value = ActualValue;
                NPC(nameof(Value));
            }
            return ActualValue;
        }

        /// <summary>Returns a valid value for <see cref="Value"/>.<para/>
        /// The given <paramref name="DesiredValue"/> will be clamped to the range [<see cref="Minimum"/>, <see cref="Maximum"/>],<br/>
        /// and set to a valid multiple of <see cref="DiscreteValueInterval"/> if <see cref="UseDiscreteValues"/>==true</summary>
        public float GetActualValue(float DesiredValue)
        {
            float Result = Math.Clamp(DesiredValue, Minimum, Maximum);

            if (UseDiscreteValues && DiscreteValueInterval.HasValue)
            {
                if (!Result.IsAlmostEqual(Minimum) && !Result.IsAlmostEqual(Maximum))
                {
                    //  Round to the nearest multiple of the DiscreteValueInterval (starting from Minimum, rather than from 0)
                    //  EX: Minimum=2, Interval=0.35, Value=3.3         Round( (3.3-2)/.35 ) = 4            4 * .35 + 2 = 3.4
                    Result = (int)Math.Round((Result - Minimum) / DiscreteValueInterval.Value, MidpointRounding.ToEven) * DiscreteValueInterval.Value + Minimum;
                }
            }

            return Result;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _UseDiscreteValues;
        /// <summary>If true, <see cref="Value"/> will snap to the nearest multiple of the given <see cref="DiscreteValueInterval"/>.<para/>
        /// If false, <see cref="Value"/> is continuous and can be any numeric value in the inclusive range [<see cref="Minimum"/>,<see cref="Maximum"/>].</summary>
        public bool UseDiscreteValues
        {
            get => _UseDiscreteValues;
            set
            {
                if (_UseDiscreteValues != value)
                {
                    _UseDiscreteValues = value;
                    if (UseDiscreteValues)
                        _ = SetValue(Value);
                    NPC(nameof(UseDiscreteValues));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private float? _DiscreteValueInterval;
        /// <summary>Only relevant if <see cref="UseDiscreteValues"/> is true. Represents the interval that <see cref="Value"/> will snap to.</summary>
        public float? DiscreteValueInterval
        {
            get => _DiscreteValueInterval;
            set
            {
                if (_DiscreteValueInterval != value)
                {
                    _DiscreteValueInterval = value;
                    if (UseDiscreteValues)
                        _ = SetValue(Value);
                    NPC(nameof(DiscreteValueInterval));
                }
            }
        }
        #endregion Value

        #region Stroke / Fill Settings
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _UnfilledShapeStrokeThickness;
        public int UnfilledShapeStrokeThickness
        {
            get => _UnfilledShapeStrokeThickness;
            set
            {
                if (_UnfilledShapeStrokeThickness != value)
                {
                    _UnfilledShapeStrokeThickness = value;
                    NPC(nameof(UnfilledShapeStrokeThickness));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Color _UnfilledShapeStrokeColor;
        public Color UnfilledShapeStrokeColor
        {
            get => _UnfilledShapeStrokeColor;
            set
            {
                if (_UnfilledShapeStrokeColor != value)
                {
                    _UnfilledShapeStrokeColor = value;
                    NPC(nameof(UnfilledShapeStrokeColor));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Color _UnfilledShapeFillColor;
        public Color UnfilledShapeFillColor
        {
            get => _UnfilledShapeFillColor;
            set
            {
                if (_UnfilledShapeFillColor != value)
                {
                    _UnfilledShapeFillColor = value;
                    NPC(nameof(UnfilledShapeFillColor));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _FilledShapeStrokeThickness;
        public int FilledShapeStrokeThickness
        {
            get => _FilledShapeStrokeThickness;
            set
            {
                if (_FilledShapeStrokeThickness != value)
                {
                    _FilledShapeStrokeThickness = value;
                    NPC(nameof(FilledShapeStrokeThickness));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Color _FilledShapeStrokeColor;
        public Color FilledShapeStrokeColor
        {
            get => _FilledShapeStrokeColor;
            set
            {
                if (_FilledShapeStrokeColor != value)
                {
                    _FilledShapeStrokeColor = value;
                    NPC(nameof(FilledShapeStrokeColor));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Color _FilledShapeFillColor;
        public Color FilledShapeFillColor
        {
            get => _FilledShapeFillColor;
            set
            {
                if (_FilledShapeFillColor != value)
                {
                    _FilledShapeFillColor = value;
                    NPC(nameof(FilledShapeFillColor));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _PreviewShapeStrokeThickness;
        public int PreviewShapeStrokeThickness
        {
            get => _PreviewShapeStrokeThickness;
            set
            {
                if (_PreviewShapeStrokeThickness != value)
                {
                    _PreviewShapeStrokeThickness = value;
                    NPC(nameof(PreviewShapeStrokeThickness));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Color _PreviewShapeStrokeColor;
        public Color PreviewShapeStrokeColor
        {
            get => _PreviewShapeStrokeColor;
            set
            {
                if (_PreviewShapeStrokeColor != value)
                {
                    _PreviewShapeStrokeColor = value;
                    NPC(nameof(PreviewShapeStrokeColor));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Color _PreviewShapeFillColor;
        /// <summary>The fill color to use when drawing <see cref="PreviewValue"/>, if there is one.<para/>
        /// Recommended to use a transparent <see cref="Color"/> so that <see cref="UnfilledShapeFillColor"/> and/or <see cref="FilledShapeFillColor"/> will still be partially visible underneath.</summary>
        public Color PreviewShapeFillColor
        {
            get => _PreviewShapeFillColor;
            set
            {
                if (_PreviewShapeFillColor != value)
                {
                    _PreviewShapeFillColor = value;
                    NPC(nameof(PreviewShapeFillColor));
                }
            }
        }
        #endregion Stroke / Fill Settings

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private float? _PreviewValue;
        /// <summary>The value that the user's mouse cursor is hovering over, or null if not hovered (or if <see cref="IsReadonly"/> is true)</summary>
        public float? PreviewValue
        {
            get => _PreviewValue;
            private set
            {
                float? ActualValue = !value.HasValue ? null : GetActualValue(value.Value);
                if (PreviewValue != ActualValue)
                {
                    _PreviewValue = ActualValue;
                    NPC(nameof(PreviewValue));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _IsReadonly;
        /// <summary>If true, the user will be unable to modify <see cref="Value"/> via clicking or clicking+dragging within this <see cref="MGRatingControl"/>'s bounds.<para/>
        /// Default value: false</summary>
        public bool IsReadonly
        {
            get => _IsReadonly;
            set
            {
                if (_IsReadonly != value)
                {
                    _IsReadonly = value;
                    PreviewValue = null;
                    NPC(nameof(IsReadonly));
                }
            }
        }

        //TODO
        //Orientation? FlowDirection?

        public MGRatingControl(MGWindow ParentWindow, int MinimumValue = 0, int MaximumValue = 5, int Size = 16, 
            bool UseDiscreteValues = true, float? DiscreteValueInterval = 1, RatingItemShape Shape = RatingItemShape.Star)
            : base(ParentWindow, MGElementType.RatingControl)
        {
            using (BeginInitializing())
            {
                this.ItemShape = Shape;
                this.ItemSize = Size;
                this.Spacing = 3;
                this.UseDiscreteValues = UseDiscreteValues;
                this.DiscreteValueInterval = DiscreteValueInterval;
                SetRange(MinimumValue, MaximumValue);

                this.IsReadonly = false;

                UnfilledShapeStrokeThickness = 1;
                UnfilledShapeStrokeColor = Color.Black;
                UnfilledShapeFillColor = Color.Transparent;

                FilledShapeStrokeThickness = 1;
                FilledShapeStrokeColor = Color.Black;
                FilledShapeFillColor = Color.Yellow;

                PreviewShapeStrokeThickness = 0;
                PreviewShapeStrokeColor = Color.Transparent;
                PreviewShapeFillColor = Color.LightBlue * 0.35f;

                MouseHandler.MovedInside += (sender, e) =>
                {
                    if (ParentWindow.HasModalWindow)
                        PreviewValue = null;
                    else
                        UpdatePreviewValue(ConvertCoordinateSpace(CoordinateSpace.Screen, CoordinateSpace.Layout, e.CurrentPosition));
                };

                MouseHandler.MovedOutside += (sender, e) => { PreviewValue = null; };

                MouseHandler.LMBPressedInside += (sender, e) =>
                {
                    if (!IsReadonly && PreviewValue.HasValue)
                    {
                        SetValue(PreviewValue.Value);
                        PreviewValue = null;
                        e.SetHandledBy(this, false);
                    }
                };

                MouseHandler.DragStart += (sender, e) =>
                {
                    if (e.IsLMB && !IsReadonly)
                    {
                        e.SetHandledBy(this, false);
                    }
                };

                MouseHandler.Dragged += (sender, e) =>
                {
                    if (e.IsLMB && !IsReadonly)
                    {
                        Point LayoutSpacePosition = ConvertCoordinateSpace(CoordinateSpace.Screen, CoordinateSpace.Layout, e.Position);
                        UpdatePreviewValue(LayoutSpacePosition);
                        SetValue(PreviewValue.Value);
                        PreviewValue = null;
                        e.SetHandled(this, false);
                    }
                };

                MouseHandler.DragEnd += (sender, e) =>
                {
                    if (e.IsLMB)
                    {
                        if (!IsReadonly && PreviewValue.HasValue)
                        {
                            SetValue(PreviewValue.Value);
                            e.SetHandled(this, false);
                        }
                    }
                };
            }
        }

        private void UpdatePreviewValue(Point MousePosition)
        {
            if (IsReadonly)
            {
                PreviewValue = null;
                return;
            }

            Rectangle PaddedBounds = LayoutBounds.GetCompressed(Padding);
            int Position = MousePosition.X;

            if (Position <= PaddedBounds.Left)
                PreviewValue = Minimum;
            else if (Position >= PaddedBounds.Right)
                PreviewValue = Maximum;
            else
            {
                int RelativePosition = Position - PaddedBounds.Left;
                int PaddedItemSize = this.ItemSize + Spacing;
                int FilledValue = (RelativePosition + Spacing) / PaddedItemSize;
                float PartialValue = Math.Max(0, (RelativePosition - FilledValue * PaddedItemSize)) * 1.0f / this.ItemSize;
                this.PreviewValue = FilledValue + PartialValue;
            }
        }

        public override Thickness MeasureSelfOverride(Size AvailableSize, out Thickness SharedSize)
        {
            SharedSize = new(0);

            int Width = NumItems * ItemSize + (NumItems - 1) * Spacing;
            int Height = ItemSize;
            return new(Width, Height, 0, 0);
        }

        public override void DrawSelf(ElementDrawArgs DA, Rectangle LayoutBounds)
        {
            float Opacity = DA.Opacity;
            Rectangle PaddedBounds = LayoutBounds.GetCompressed(Padding);
            int CurrentX = PaddedBounds.Left;

            for (int i = 0; i < NumItems; i++)
            {
                DrawValue(DA, PaddedBounds, CurrentX, 1.0f, UnfilledShapeStrokeThickness, UnfilledShapeStrokeColor * Opacity, UnfilledShapeFillColor * Opacity);

                float FilledPercent = Math.Clamp(Value - i, 0, 1);
                DrawValue(DA, PaddedBounds, CurrentX, FilledPercent, FilledShapeStrokeThickness, FilledShapeStrokeColor * Opacity, FilledShapeFillColor * Opacity);

                if (PreviewValue.HasValue)
                {
                    float PreviewFilledPercent = Math.Clamp(PreviewValue.Value - i, 0, 1);
                    DrawValue(DA, PaddedBounds, CurrentX, PreviewFilledPercent, PreviewShapeStrokeThickness, PreviewShapeStrokeColor * Opacity, PreviewShapeFillColor * Opacity);
                }

                CurrentX += ItemSize + Spacing;
            }
        }

        private void DrawValue(ElementDrawArgs DA, Rectangle PaddedBounds, int CurrentX, float FilledPercent,
            int StrokeThickness, Color StrokeColor, Color FillColor)
        {
            Vector2 Offset = DA.Offset.ToVector2();
            float ScaleFactor = ItemSize / 256.0f;
            Rectangle Destination = new(CurrentX, PaddedBounds.Y, ItemSize, ItemSize);

            bool IsCompletelyFilled = FilledPercent.IsAlmostEqual(1);
            bool IsCompletelyUnfilled = FilledPercent.IsAlmostEqual(0);
            bool IsPartiallyFilled = !IsCompletelyFilled && !IsCompletelyUnfilled;

            if (IsCompletelyUnfilled)
                return;

            switch (this.ItemShape)
            {
                case RatingItemShape.Star:
                case RatingItemShape.Diamond:
                    Vector2 Origin = Offset + Destination.TopLeft().ToVector2();
                    List<Vector2> Vertices = ShapeVertices256[ItemShape].Select(x => x * ScaleFactor).ToList();

                    if (IsCompletelyFilled)
                    {
                        DA.DT.StrokeAndFillPolygon(Origin, Vertices, StrokeColor, FillColor, StrokeThickness);
                    }
                    else if (IsPartiallyFilled)
                    {
                        float MinVertexX = Vertices.Min(v => v.X);
                        float MaxVertexX = Vertices.Max(v => v.X);
                        float Width = MinVertexX + (MaxVertexX - MinVertexX) * FilledPercent;

                        Rectangle UnscaledClipTarget = new(Destination.Left, Destination.Top, (int)Width, Destination.Height);
                        Rectangle ClipTarget = ConvertCoordinateSpace(CoordinateSpace.UnscaledScreen, CoordinateSpace.Screen, UnscaledClipTarget);
                        using (DA.DT.SetClipTargetTemporary(ClipTarget, true))
                        {
                            DA.DT.StrokeAndFillPolygon(Origin, Vertices, StrokeColor, FillColor, StrokeThickness);
                        }
                    }
                    break;
                case RatingItemShape.Circle:
                    Vector2 Center = Offset + Destination.Center.ToVector2();
                    float Radius = Math.Min(Destination.Width / 2, Destination.Height / 2);

                    if (IsCompletelyFilled)
                    {
                        DA.DT.FillCircle(Center, FillColor, Radius - StrokeThickness);
                        DA.DT.StrokeCircle(Center, StrokeColor, Radius, StrokeThickness);
                        //DA.DT.StrokeAndFillCircle(Center, StrokeColor, FillColor, Radius, StrokeThickness);
                    }
                    else if (IsPartiallyFilled)
                    {
                        float MinVertexX = Center.X - Radius;
                        float MaxVertexX = Center.X + Radius;
                        float Width = (MaxVertexX - MinVertexX) * FilledPercent;

                        Rectangle UnscaledClipTarget = new(Destination.Left, Destination.Top, (int)Width, Destination.Height);
                        Rectangle ClipTarget = ConvertCoordinateSpace(CoordinateSpace.UnscaledScreen, CoordinateSpace.Screen, UnscaledClipTarget);
                        using (DA.DT.SetClipTargetTemporary(ClipTarget, true))
                        {
                            DA.DT.FillCircle(Center, FillColor, Radius - StrokeThickness);
                            DA.DT.StrokeCircle(Center, StrokeColor, Radius, StrokeThickness);
                            //DA.DT.StrokeAndFillCircle(Center, StrokeColor, FillColor, Radius, StrokeThickness);
                        }
                    }
                    break;
                case RatingItemShape.Rectangle:
                    if (IsCompletelyFilled)
                    {
                        DA.DT.StrokeAndFillRectangle(Offset, Destination, StrokeColor, FillColor, StrokeThickness);
                    }
                    else if (IsPartiallyFilled)
                    {
                        Rectangle UnscaledClipTarget = new(Destination.Left, Destination.Top, (int)(Destination.Width * FilledPercent), Destination.Height);
                        Rectangle ClipTarget = ConvertCoordinateSpace(CoordinateSpace.UnscaledScreen, CoordinateSpace.Screen, UnscaledClipTarget);
                        using (DA.DT.SetClipTargetTemporary(ClipTarget, true))
                        {
                            DA.DT.StrokeAndFillRectangle(Offset, Destination, StrokeColor, FillColor, StrokeThickness);
                        }
                    }
                    break;
                case RatingItemShape.Triangle:
                    Origin = Offset + Destination.TopLeft().ToVector2();
                    IList<Vector2> TriangleVertices = ShapeVertices256[RatingItemShape.Triangle].Select(x => x * ScaleFactor).ToList();

                    if (IsCompletelyFilled)
                    {
                        DA.DT.FillTriangle(Origin, TriangleVertices[0], FillColor, TriangleVertices[1], FillColor, TriangleVertices[2], FillColor);
                        if (StrokeThickness > 0)
                        {
                            foreach (var LineSegment in TriangleVertices.SelectConsecutivePairs(true))
                            {
                                DA.DT.StrokeLineSegment(Origin, LineSegment.Item1, LineSegment.Item2, StrokeColor, StrokeThickness);
                            }
                        }
                    }
                    else if (IsPartiallyFilled)
                    {
                        float MinVertexX = TriangleVertices.Min(v => v.X);
                        float MaxVertexX = TriangleVertices.Max(v => v.X);
                        float Width = MinVertexX + (MaxVertexX - MinVertexX) * FilledPercent;

                        Rectangle UnscaledClipTarget = new(Destination.Left, Destination.Top, (int)Width, Destination.Height);
                        Rectangle ClipTarget = ConvertCoordinateSpace(CoordinateSpace.UnscaledScreen, CoordinateSpace.Screen, UnscaledClipTarget);
                        using (DA.DT.SetClipTargetTemporary(ClipTarget, true))
                        {
                            DA.DT.FillTriangle(Origin, TriangleVertices[0], FillColor, TriangleVertices[1], FillColor, TriangleVertices[2], FillColor);
                            if (StrokeThickness > 0)
                            {
                                foreach (var LineSegment in TriangleVertices.SelectConsecutivePairs(true))
                                {
                                    DA.DT.StrokeLineSegment(Origin, LineSegment.Item1, LineSegment.Item2, StrokeColor, StrokeThickness);
                                }
                            }
                        }
                    }
                    break;
            }
        }

        private static List<Vector2> Get5PointStarVertices(Rectangle Region)
        {
            float InnerRadius = Region.Width / 5.0f;
            float OuterRadius = Region.Width / 2.0f;

            float Ang36 = (float)(Math.PI / 5.0);   // 36Â° x PI/180
            float Ang72 = 2.0f * Ang36;     // 72Â° x PI/180
            float Sin36 = (float)Math.Sin(Ang36);
            float Sin72 = (float)Math.Sin(Ang72);
            float Cos36 = (float)Math.Cos(Ang36);
            float Cos72 = (float)Math.Cos(Ang72);

            Vector2 Center = new Vector2((Region.Left + Region.Right) / 2.0f, (Region.Top + Region.Bottom) / 2.0f);

            List<Vector2> Vertices = new(10);

#if NEVER
            //Taken from: https://www.daniweb.com/programming/software-development/code/360165/draw-any-star-you-want

            //  Top of the star: 12:00 hours
            Vertices.Add(new Vector2(Center.X, Center.Y - OuterRadius));

            //  Right-side vertices
            Vertices.Add(new Vector2(Center.X + InnerRadius * Sin36, Center.Y - InnerRadius * Cos36));
            Vertices.Add(new Vector2(Center.X + OuterRadius * Sin72, Center.Y - OuterRadius * Cos72));
            Vertices.Add(new Vector2(Center.X + InnerRadius * Sin72, Center.Y + InnerRadius * Cos72));
            Vertices.Add(new Vector2(Center.X + OuterRadius * Sin36, Center.Y + OuterRadius * Cos36));

            //  Bottom of the star: 6:00 hours
            Vertices.Add(new Vector2(Center.X, Center.Y + InnerRadius));

            //  Left-side vertices
            Vertices.Add(new Vector2(Center.X - OuterRadius * Sin36, Center.Y + OuterRadius * Cos36));
            Vertices.Add(new Vector2(Center.X - InnerRadius * Sin72, Center.Y + InnerRadius * Cos72));
            Vertices.Add(new Vector2(Center.X - OuterRadius * Sin72, Center.Y - OuterRadius * Cos72));
            Vertices.Add(new Vector2(Center.X - InnerRadius * Sin36, Center.Y - InnerRadius * Cos36));
#else
            float YOffset = InnerRadius / 8;

            //  Top of the star: 12:00 hours
            Vertices.Add(new Vector2(Center.X, Center.Y - OuterRadius + YOffset));

            //  Right-side vertices
            Vertices.Add(new Vector2(Center.X + InnerRadius * Sin36, Center.Y - InnerRadius * Cos36 + YOffset));
            Vertices.Add(new Vector2(Region.Right, Center.Y - InnerRadius * Cos36 + YOffset));
            Vertices.Add(new Vector2(Center.X + InnerRadius * Sin72, Center.Y + InnerRadius * Cos72 + YOffset));
            Vertices.Add(new Vector2(Center.X + OuterRadius * Sin36 + InnerRadius / 2, Center.Y + OuterRadius * Cos36 + YOffset));

            //  Bottom of the star: 6:00 hours
            Vertices.Add(new Vector2(Center.X, Center.Y + InnerRadius + YOffset));

            //  Left-side vertices
            Vertices.Add(new Vector2(Center.X - OuterRadius * Sin36 - InnerRadius / 2, Center.Y + OuterRadius * Cos36 + YOffset));
            Vertices.Add(new Vector2(Center.X - InnerRadius * Sin72, Center.Y + InnerRadius * Cos72 + YOffset));
            Vertices.Add(new Vector2(Region.Left, Center.Y - InnerRadius * Cos36 + YOffset));
            Vertices.Add(new Vector2(Center.X - InnerRadius * Sin36, Center.Y - InnerRadius * Cos36 + YOffset));

#endif
            return Vertices;
        }
    }
}
