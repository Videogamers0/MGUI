using Microsoft.Xna.Framework;
using MGUI.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoGame.Extended;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.Brushes.Border_Brushes;

namespace MGUI.Core.UI
{
    public class MGSlider : MGElement
    {
        #region Focus Visual Style
        private Color _HoveredHighlightColor;
        /// <summary>An overlay color that is drawn overtop of this <see cref="MGSlider"/>'s graphics if the mouse is currently hovering it.<br/>
        /// Recommended to use a transparent color.<para/>
        /// Default Value: <see cref="MGTheme.ClickableHoveredColor"/></summary>
        public Color HoveredHighlightColor
        {
            get => _HoveredHighlightColor;
            set
            {
                if (_HoveredHighlightColor != value)
                {
                    _HoveredHighlightColor = value;
                    NPC(nameof(HoveredHighlightColor));
                    HoveredOverlay = new(HoveredHighlightColor);
                    PressedOverlay = new(HoveredHighlightColor.Darken(PressedDarkenIntensity));
                    HoveredBorderOverlay = new(HoveredOverlay);
                    PressedBorderOverlay = new(PressedOverlay);
                }
            }
        }

        private float _PressedDarkenIntensity;
        /// <summary>A percentage to darken this <see cref="MGSlider"/>'s graphics by when the mouse is currently pressed, but not yet released, overtop of this <see cref="MGElement"/>.<br/>
        /// Use a larger value to apply a more obvious background overlay while this <see cref="MGElement"/> is pressed. Use a smaller value for a more subtle change.<para/>
        /// Default value: <see cref="MGTheme.ClickablePressedModifier"/></summary>
        public float PressedDarkenIntensity
        {
            get => _PressedDarkenIntensity;
            set
            {
                if (_PressedDarkenIntensity != value)
                {
                    _PressedDarkenIntensity = value;
                    NPC(nameof(PressedDarkenIntensity));
                    PressedOverlay = new(HoveredHighlightColor.Darken(PressedDarkenIntensity));
                    PressedBorderOverlay = new(PressedOverlay);
                }
            }
        }

        private MGSolidFillBrush HoveredOverlay { get; set; }
        private MGSolidFillBrush PressedOverlay { get; set; }
        private MGUniformBorderBrush HoveredBorderOverlay { get; set; }
        private MGUniformBorderBrush PressedBorderOverlay { get; set; }
        #endregion FocusVisualStyle

        #region Value
        private float _Minimum;
        /// <summary>The inclusive minimum that <see cref="Value"/> can be set to.<para/>
        /// To set this value, use <see cref="SetRange(float, float)"/></summary>
        public float Minimum { get => _Minimum; }
        private float _Maximum;
        /// <summary>The inclusive maximum that <see cref="Value"/> can be set to.<para/>
        /// To set this value, use <see cref="SetRange(float, float)"/></summary>
        public float Maximum { get => _Maximum; }

        public void SetRange(float Minimum, float Maximum)
        {
            if (this.Minimum != Minimum || this.Maximum != Maximum)
            {
                if (Minimum > Maximum)
                    throw new ArgumentException($"{nameof(MGSlider)}.{nameof(Minimum)} cannot be greater than {nameof(MGSlider)}.{nameof(Maximum)}");

                _Minimum = Minimum;
                _Maximum = Maximum;
                _ = SetValue(Value);
            }
        }

        /// <summary>Convenience property that simply returns: <see cref="Maximum"/> - <see cref="Minimum"/></summary>
        public float Interval => Maximum - Minimum;

        private float _Value;
        /// <summary>The current value that this <see cref="MGSlider"/> is set to.<para/>
        /// To set this value, use <see cref="SetValue(float)"/></summary>
        public float Value { get => _Value; }
        /// <summary>See also: <see cref="GetActualValue(float)"/></summary>
        public float SetValue(float DesiredValue)
        {
            float ActualValue = GetActualValue(DesiredValue);
            if (Value != ActualValue)
                _Value = ActualValue;
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
                    NPC(nameof(UseDiscreteValues));
                    if (UseDiscreteValues)
                        _ = SetValue(Value);
                }
            }
        }

        private float? _DiscreteValueInterval;
        /// <summary>Only relevant if <see cref="UseDiscreteValues"/>==true. Represents the interval that <see cref="Value"/> will snap to.</summary>
        public float? DiscreteValueInterval
        {
            get => _DiscreteValueInterval;
            set
            {
                if (_DiscreteValueInterval != value)
                {
                    _DiscreteValueInterval = value;
                    NPC(nameof(DiscreteValueInterval));
                    if (UseDiscreteValues)
                        _ = SetValue(Value);
                }
            }
        }
        #endregion Value

        #region Number Line
        public bool IsHoveringNumberLine { get; private set; } = false;

        private int _NumberLineSize;
        /// <summary>If <see cref="Orientation"/>==<see cref="Orientation.Horizontal"/>, this represents the height of the number line. Else it is the width.</summary>
        public int NumberLineSize
        {
            get => _NumberLineSize;
            set
            {
                if (_NumberLineSize != value)
                {
                    _NumberLineSize = value;
                    NPC(nameof(NumberLineSize));
                    LayoutChanged(this, true);
                }
            }
        }

        private Thickness _NumberLineBorderThickness;
        public Thickness NumberLineBorderThickness
        {
            get => _NumberLineBorderThickness;
            set
            {
                if (!_NumberLineBorderThickness.Equals(value))
                {
                    _NumberLineBorderThickness = value;
                    NPC(nameof(NumberLineBorderThickness));
                }
            }
        }

        private IBorderBrush _NumberLineBorderBrush;
        public IBorderBrush NumberLineBorderBrush
        {
            get => _NumberLineBorderBrush;
            set
            {
                if (_NumberLineBorderBrush != value)
                {
                    _NumberLineBorderBrush = value;
                    NPC(nameof(NumberLineBorderBrush));
                }
            }
        }

        private IFillBrush _NumberLineFillBrush;
        public IFillBrush NumberLineFillBrush
        {
            get => _NumberLineFillBrush;
            set
            {
                if (_NumberLineFillBrush != value)
                {
                    _NumberLineFillBrush = value;
                    NPC(nameof(NumberLineFillBrush));
                }
            }
        }

        public IFillBrush ActualNumberLineFillBrush => NumberLineFillBrush ?? Foreground;
        #endregion Number Line

        #region Ticks
        /// <summary>The default size of the tick marks along the direction that the slider is oriented.<br/>
        /// (Represents the Width if <see cref="Orientation"/>==<see cref="Orientation.Horizontal"/>. Else represents the Height)</summary>
        private const int DefaultTickPrimarySize = 2;
        /// <summary>The default size of the tick marks along the opposite direction that the slider is oriented.<br/>
        /// (Represents the Height if <see cref="Orientation"/>==<see cref="Orientation.Vertical"/>. Else represents the Width)</summary>
        private const int DefaultTickSecondarySize = 18;

        private float? _TickFrequency;
        public float? TickFrequency
        {
            get => _TickFrequency;
            set
            {
                if (_TickFrequency != value)
                {
                    _TickFrequency = value;
                    NPC(nameof(TickFrequency));
                }
            }
        }

        private bool _DrawTicks;
        /// <summary>True if tick marks should be drawn at the given <see cref="TickFrequency"/> interval along the number line.<para/>
        /// Only relevant if <see cref="TickFrequency"/> is specified.<para/>
        /// See also: <see cref="CanDrawTickMarks"/>, <see cref="TickWidth"/></summary>
        public bool DrawTicks
        {
            get => _DrawTicks;
            set
            {
                if (_DrawTicks != value)
                {
                    _DrawTicks = value;
                    NPC(nameof(DrawTicks));
                    LayoutChanged(this, true);
                }
            }
        }

        private const int MaxTickCount = 100;
        public bool CanDrawTickMarks => TickFrequency.HasValue && TickFrequency.Value > 0 && Interval / TickFrequency.Value <= MaxTickCount;

        private int? _TickWidth;
        /// <summary>The width of each tick mark. Only relevant if <see cref="DrawTicks"/>==true.<para/>
        /// If null, uses <see cref="DefaultTickPrimarySize"/> or <see cref="DefaultTickSecondarySize"/>, depending on <see cref="Orientation"/>.<para/>
        /// See also: <see cref="ActualTickWidth"/></summary>
        public int? TickWidth
        {
            get => _TickWidth;
            set
            {
                if (_TickWidth != value)
                {
                    _TickWidth = value;
                    NPC(nameof(TickWidth));
                    if (DrawTicks)
                        LayoutChanged(this, true);
                }
            }
        }

        private int? _TickHeight;
        /// <summary>The height of each tick mark. Only relevant if <see cref="DrawTicks"/>==true.<para/>
        /// If null, uses <see cref="DefaultTickPrimarySize"/> or <see cref="DefaultTickSecondarySize"/>, depending on <see cref="Orientation"/>.<para/>
        /// See also: <see cref="ActualTickHeight"/></summary>
        public int? TickHeight
        {
            get => _TickHeight;
            set
            {
                if (_TickHeight != value)
                {
                    _TickHeight = value;
                    NPC(nameof(TickHeight));
                    if (DrawTicks)
                        LayoutChanged(this, true);
                }
            }
        }

        public int ActualTickWidth => TickWidth ?? (IsHorizontal ? DefaultTickPrimarySize : IsVertical ? DefaultTickSecondarySize : throw new NotImplementedException());
        public int ActualTickHeight => TickHeight ?? (IsHorizontal ? DefaultTickSecondarySize : IsVertical ? DefaultTickPrimarySize : throw new NotImplementedException());

        /*private bool _DrawTickValues;
        /// <summary>True if the numeric values should be drawn at the given <see cref="TickFrequency"/> interval under the number line.<para/>
        /// Only relevant if <see cref="TickFrequency"/> is specified.</summary>
        public bool DrawTickValues
        {
            get => _DrawTickValues;
            set
            {
                if (_DrawTickValues != value)
                {
                    _DrawTickValues = value;
                    NPC(nameof(DrawTickValues));
                }
            }
        }*/

        private Thickness _TickBorderThickness;
        public Thickness TickBorderThickness
        {
            get => _TickBorderThickness;
            set
            {
                if (!_TickBorderThickness.Equals(value))
                {
                    _TickBorderThickness = value;
                    NPC(nameof(TickBorderThickness));
                }
            }
        }

        private IBorderBrush _TickBorderBrush;
        public IBorderBrush TickBorderBrush
        {
            get => _TickBorderBrush;
            set
            {
                if (_TickBorderBrush != value)
                {
                    _TickBorderBrush = value;
                    NPC(nameof(TickBorderBrush));
                }
            }
        }

        private IFillBrush _TickFillBrush;
        public IFillBrush TickFillBrush
        {
            get => _TickFillBrush;
            set
            {
                if (_TickFillBrush != value)
                {
                    _TickFillBrush = value;
                    NPC(nameof(TickFillBrush));
                }
            }
        }

        public IFillBrush ActualTickFillBrush => TickFillBrush ?? Foreground;
        #endregion Ticks

        #region Thumb
        /// <summary>The default size of the thumb along the direction that the slider is oriented.<br/>
        /// (Represents the Width if <see cref="Orientation"/>==<see cref="Orientation.Horizontal"/>. Else represents the Height)</summary>
        private const int DefaultThumbPrimarySize = 12;
        /// <summary>The default size of the thumb along the opposite direction that the slider is oriented.<br/>
        /// (Represents the Height if <see cref="Orientation"/>==<see cref="Orientation.Vertical"/>. Else represents the Width)</summary>
        private const int DefaultThumbSecondarySize = 24;

        public bool IsHoveringThumb { get; private set; } = false;
        public bool IsDraggingThumb { get; private set; } = false;

        private int? _ThumbWidth;
        /// <summary>The width of the thumb.<para/>
        /// If null, uses <see cref="DefaultThumbPrimarySize"/> or <see cref="DefaultThumbPrimarySize"/>, depending on <see cref="Orientation"/>.<para/>
        /// See also: <see cref="ActualThumbWidth"/></summary>
        public int? ThumbWidth
        {
            get => _ThumbWidth;
            set
            {
                if (_ThumbWidth != value)
                {
                    _ThumbWidth = value;
                    NPC(nameof(ThumbWidth));
                    LayoutChanged(this, true);
                }
            }
        }

        private int? _ThumbHeight;
        /// <summary>The height of the thumb.<para/>
        /// If null, uses <see cref="DefaultThumbPrimarySize"/> or <see cref="DefaultThumbPrimarySize"/>, depending on <see cref="Orientation"/>.<para/>
        /// See also: <see cref="ActualThumbHeight"/></summary>
        public int? ThumbHeight
        {
            get => _ThumbHeight;
            set
            {
                if (_ThumbHeight != value)
                {
                    _ThumbHeight = value;
                    NPC(nameof(ThumbHeight));
                    LayoutChanged(this, true);
                }
            }
        }

        public int ActualThumbWidth => ThumbWidth ?? (IsHorizontal ? DefaultThumbPrimarySize : IsVertical ? DefaultThumbSecondarySize : throw new NotImplementedException());
        public int ActualThumbHeight => ThumbHeight ?? (IsHorizontal ? DefaultThumbSecondarySize : IsVertical ? DefaultThumbPrimarySize : throw new NotImplementedException());

        private Thickness _ThumbBorderThickness;
        public Thickness ThumbBorderThickness
        {
            get => _ThumbBorderThickness;
            set
            {
                if (!_ThumbBorderThickness.Equals(value))
                {
                    _ThumbBorderThickness = value;
                    NPC(nameof(ThumbBorderThickness));
                }
            }
        }

        private IBorderBrush _ThumbBorderBrush;
        public IBorderBrush ThumbBorderBrush
        {
            get => _ThumbBorderBrush;
            set
            {
                if (_ThumbBorderBrush != value)
                {
                    _ThumbBorderBrush = value;
                    NPC(nameof(ThumbBorderBrush));
                }
            }
        }

        private IFillBrush _ThumbFillBrush;
        public IFillBrush ThumbFillBrush
        {
            get => _ThumbFillBrush;
            set
            {
                if (_ThumbFillBrush != value)
                {
                    _ThumbFillBrush = value;
                    NPC(nameof(ThumbFillBrush));
                }
            }
        }

        public IFillBrush ActualThumbFillBrush => ThumbFillBrush ?? Foreground;
        #endregion Thumb

        #region Orientation
        private Orientation _Orientation;
        public Orientation Orientation
        {
            get => _Orientation;
            set
            {
                if (_Orientation != value)
                {
                    _Orientation = value;
                    NPC(nameof(Orientation));
                    NPC(nameof(IsHorizontal));
                    NPC(nameof(IsVertical));
                    LayoutChanged(this, true);
                }
            }
        }

        public bool IsHorizontal => Orientation == Orientation.Horizontal;
        public bool IsVertical => Orientation == Orientation.Vertical;
        #endregion Orientation

        private IFillBrush _Foreground;
        /// <summary>The fallback <see cref="IFillBrush"/> for drawing the number line, ticks, or the thumb if they don't have an explicit <see cref="IFillBrush"/> specified.<para/>
        /// See also: <see cref="NumberLineFillBrush"/>, <see cref="TickFillBrush"/>, <see cref="ThumbFillBrush"/></summary>
        public IFillBrush Foreground
        {
            get => _Foreground;
            set
            {
                if (_Foreground != value)
                {
                    _Foreground = value;
                    NPC(nameof(Foreground));
                }
            }
        }

        /// <summary>If true, the slider value can be modified by using the mouse scroll wheel while hovering the number line portion.<para/>
        /// Default value: false</summary>
        public bool AcceptsMouseScrollWheel { get; set; }

        //TODO maybe add an option to show the value in a bordered textblock to the right of the slider portion?

        public MGSlider(MGWindow Window, float Minimum, float Maximum, float Value)
            : this(Window, Minimum, Maximum, Value, false, null, MGUniformBorderBrush.Black, Orientation.Horizontal) { }

        public MGSlider(MGWindow Window, float Minimum, float Maximum, float Value, bool DrawTicks, float? TickFrequency,
            IBorderBrush BorderBrush, Orientation Orientation)
            : base(Window, MGElementType.Slider)
        {
            using (BeginInitializing())
            {
                MGTheme Theme = GetTheme();

                this.HoveredHighlightColor = Theme.ClickableHoveredColor;
                this.PressedDarkenIntensity = Theme.ClickablePressedModifier;

                SetRange(Minimum, Maximum);
                SetValue(Value);

                this.Foreground = Theme.SliderForeground.GetValue(true);

                this.NumberLineSize = 8;
                this.NumberLineFillBrush = null;
                this.NumberLineBorderBrush = BorderBrush;
                this.NumberLineBorderThickness = new(1);

                this.DrawTicks = DrawTicks;
                this.TickFrequency = TickFrequency;
                this.UseDiscreteValues = DrawTicks;
                this.DiscreteValueInterval = TickFrequency;

                this.TickFillBrush = null;
                this.TickBorderBrush = BorderBrush;
                this.TickBorderThickness = new(0);

                this.ThumbFillBrush = Theme.SliderThumbFillBrush.GetValue(true);
                this.ThumbBorderBrush = BorderBrush;
                this.ThumbBorderThickness = new(1);

                this.Orientation = Orientation;
                if (Orientation == Orientation.Horizontal)
                {
                    this.MinWidth = DefaultThumbPrimarySize * 4;
                    this.VerticalAlignment = VerticalAlignment.Top;
                }
                else if (Orientation == Orientation.Vertical)
                {
                    this.MinHeight = DefaultThumbPrimarySize * 4;
                    this.HorizontalAlignment = HorizontalAlignment.Left;
                }
                else
                    throw new NotImplementedException($"Unrecognized {nameof(Orientation)}: {Orientation}");

                this.AcceptsMouseScrollWheel = false;

                MouseHandler.LMBReleasedInside += (sender, e) =>
                {
                    if (IsHoveringNumberLine)
                    {
                        e.SetHandled(this, false);
                        HandleSliderInput(e.AdjustedPosition(this));
                    }
                };

                MouseHandler.MovedOutside += (sender, e) =>
                {
                    IsHoveringNumberLine = false;
                    IsHoveringThumb = false;
                };

                MouseHandler.DragStart += (sender, e) =>
                {
                    if (e.IsLMB && IsHoveringThumb)
                    {
                        IsDraggingThumb = true;
                        e.SetHandled(this, false);
                    }
                };

                MouseHandler.DragEnd += (sender, e) =>
                {
                    if (e.IsLMB)
                        IsDraggingThumb = false;
                };

                MouseHandler.Dragged += (sender, e) =>
                {
                    if (e.IsLMB && IsDraggingThumb)
                        HandleSliderInput(e.AdjustedPosition(this));
                };

                MouseHandler.Scrolled += (sender, e) =>
                {
                    if (AcceptsMouseScrollWheel && IsHoveringNumberLine && DiscreteValueInterval.HasValue)
                    {
                        if (e.ScrollWheelDelta < 0 && !this.Value.IsAlmostEqual(Minimum))
                        {
                            SetValue(this.Value - DiscreteValueInterval.Value);
                            e.SetHandled(this, false);
                        }
                        else if (e.ScrollWheelDelta > 0 && !this.Value.IsAlmostEqual(Maximum))
                        {
                            SetValue(this.Value + DiscreteValueInterval.Value);
                            e.SetHandled(this, false);
                        }
                    }
                };
            }
        }

        public override void UpdateSelf(ElementUpdateArgs UA)
        {
            base.UpdateSelf(UA);
            if (IsHitTestVisible && !RecentDrawWasClipped)
            {
                Point AdjustedMousePosition = AdjustMousePosition(MouseHandler.Tracker.CurrentPosition).ToPoint();
                IsHoveringNumberLine = RecentStretchedNumberLineBounds.ContainsInclusive(AdjustedMousePosition);
                IsHoveringThumb = RecentThumbBounds.GetExpanded(5).ContainsInclusive(AdjustedMousePosition);
            }
        }

        private void HandleSliderInput(Vector2 CursorPosition)
        {
            Rectangle NumberLineBounds = ComputeNumberLineBounds(this.LayoutBounds);
            if (Orientation == Orientation.Horizontal)
            {
                float Percent = (CursorPosition.X - NumberLineBounds.Left) / NumberLineBounds.Width;
                SetValue(Minimum + Percent * Interval);
            }
            else if (Orientation == Orientation.Vertical)
            {
                float Percent = (CursorPosition.Y - NumberLineBounds.Top) / NumberLineBounds.Height;
                SetValue(Minimum + Percent * Interval);
            }
            else
                throw new NotImplementedException($"Unrecognized {nameof(Orientation)}: {Orientation}");
        }

        public override Thickness MeasureSelfOverride(Size AvailableSize, out Thickness SharedSize)
        {
            SharedSize = new(0);

            int Width;
            int Height;
            if (Orientation == Orientation.Horizontal)
            {
                Width = ActualThumbWidth * 2;
                Height = Math.Max(ActualThumbHeight, NumberLineSize);
                if (CanDrawTickMarks && DrawTicks)
                {
                    Width = Math.Max(Width, ActualTickWidth * 2);
                    Height = Math.Max(Height, ActualTickHeight);
                }
            }
            else if (Orientation == Orientation.Vertical)
            {
                Width = Math.Max(ActualThumbWidth, NumberLineSize);
                Height = ActualThumbHeight * 2;
                if (CanDrawTickMarks && DrawTicks)
                {
                    Width = Math.Max(Width, ActualTickWidth);
                    Height = Math.Max(Height, ActualTickHeight * 2);
                }
            }
            else
                throw new NotImplementedException($"Unrecognized {nameof(Orientation)}: {Orientation}");

            return new(Width, Height, 0, 0);
        }

        private Rectangle ComputeNumberLineBounds(Rectangle LayoutBounds)
        {
            //  Apply padding
            LayoutBounds = new(LayoutBounds.Left + Padding.Left, LayoutBounds.Top + Padding.Top, LayoutBounds.Width - Padding.Width, LayoutBounds.Height - Padding.Height);

            if (Orientation == Orientation.Horizontal)
            {
                //  Compute number line position
                int XPadding = Math.Max(ActualThumbWidth / 2, DrawTicks ? ActualTickWidth / 2 : 0);
                int NumberLineStartPosition = (int)Math.Clamp(LayoutBounds.Left + XPadding, LayoutBounds.Left, LayoutBounds.Right);
                int NumberLineEndPosition = (int)Math.Clamp(LayoutBounds.Right - XPadding, LayoutBounds.Left, LayoutBounds.Right);
                if (NumberLineStartPosition == NumberLineEndPosition)
                {
                    NumberLineStartPosition = LayoutBounds.Center.X;
                    NumberLineEndPosition = NumberLineStartPosition;
                }
                int NumberLineWidth = NumberLineEndPosition - NumberLineStartPosition;

                Rectangle NumberLineBounds = ApplyAlignment(LayoutBounds, HorizontalAlignment.Center, VerticalAlignment.Center, new Size(NumberLineWidth, NumberLineSize));
                return NumberLineBounds;
            }
            else if (Orientation == Orientation.Vertical)
            {
                //  Compute number line position
                int YPadding = Math.Max(ActualThumbHeight / 2, DrawTicks ? ActualTickHeight / 2 : 0);
                int NumberLineStartPosition = (int)Math.Clamp(LayoutBounds.Top + YPadding, LayoutBounds.Top, LayoutBounds.Bottom);
                int NumberLineEndPosition = (int)Math.Clamp(LayoutBounds.Bottom - YPadding, LayoutBounds.Top, LayoutBounds.Bottom);
                if (NumberLineStartPosition == NumberLineEndPosition)
                {
                    NumberLineStartPosition = LayoutBounds.Center.Y;
                    NumberLineEndPosition = NumberLineStartPosition;
                }
                int NumberLineHeight = NumberLineEndPosition - NumberLineStartPosition;

                Rectangle NumberLineBounds = ApplyAlignment(LayoutBounds, HorizontalAlignment.Center, VerticalAlignment.Center, new Size(NumberLineSize, NumberLineHeight));
                return NumberLineBounds;
            }
            else
                throw new NotImplementedException($"Unrecognized {nameof(Orientation)}: {Orientation}");
        }

        /// <summary>The most-recently rendered bounds of the number line, before accounting for any offset (DrawSelf.Offset)</summary>
        private Rectangle RecentNumberLineBounds = Rectangle.Empty;
        /// <summary>Same as <see cref="RecentNumberLineBounds"/>, except it is:<para/>
        /// - stretched vertically to fill the <see cref="MGElement.LayoutBounds"/> if <see cref="Orientation"/>==<see cref="Orientation.Horizontal"/><br/>
        /// - stretched horizontally to fill the <see cref="MGElement.LayoutBounds"/> if <see cref="Orientation"/>==<see cref="Orientation.Vertical"/></summary>
        private Rectangle RecentStretchedNumberLineBounds = Rectangle.Empty;
        private Rectangle RecentThumbBounds = Rectangle.Empty;

        public override void DrawSelf(ElementDrawArgs DA, Rectangle LayoutBounds)
        {
            //  Apply padding
            LayoutBounds = new(LayoutBounds.Left + Padding.Left, LayoutBounds.Top + Padding.Top, LayoutBounds.Width - Padding.Width, LayoutBounds.Height - Padding.Height);

            Rectangle NumberLineBounds = ComputeNumberLineBounds(LayoutBounds);
            this.RecentNumberLineBounds = NumberLineBounds;

            bool DrawTicks = CanDrawTickMarks && this.DrawTicks;
            if (Orientation == Orientation.Horizontal)
            {
                RecentStretchedNumberLineBounds = new(NumberLineBounds.Left, LayoutBounds.Top, NumberLineBounds.Width, LayoutBounds.Height);

                //  Draw the number line
                ActualNumberLineFillBrush.Draw(DA, this, NumberLineBounds);
                NumberLineBorderBrush?.Draw(DA, this, NumberLineBounds, NumberLineBorderThickness);

                if (DrawTicks && NumberLineBounds.Width > 0)
                {
                    Size TickSize = new(ActualTickWidth, ActualTickHeight);
                    float IntervalPercent = TickFrequency.Value / Interval;
                    float IntervalPixels = NumberLineBounds.Width * IntervalPercent;

                    //  Draw the ticks between the min and max
                    float CurrentXPosition = NumberLineBounds.Left + IntervalPixels;
                    while (CurrentXPosition < NumberLineBounds.Right)
                    {
                        Rectangle TickBounds = ApplyAlignment(new((int)CurrentXPosition, LayoutBounds.Top, 0, LayoutBounds.Height), HorizontalAlignment.Center, VerticalAlignment.Center, TickSize);
                        ActualTickFillBrush.Draw(DA, this, TickBounds);
                        TickBorderBrush?.Draw(DA, this, TickBounds, TickBorderThickness);
                        CurrentXPosition += IntervalPixels;
                    }

                    //  Draw the ticks at the min and max
                    Rectangle MinTickBounds = ApplyAlignment(new(NumberLineBounds.Left, LayoutBounds.Top, 0, LayoutBounds.Height), HorizontalAlignment.Center, VerticalAlignment.Center, TickSize);
                    ActualTickFillBrush.Draw(DA, this, MinTickBounds);
                    TickBorderBrush?.Draw(DA, this, MinTickBounds, TickBorderThickness);
                    Rectangle MaxTickBounds = ApplyAlignment(new(NumberLineBounds.Right, LayoutBounds.Top, 0, LayoutBounds.Height), HorizontalAlignment.Center, VerticalAlignment.Center, TickSize);
                    ActualTickFillBrush.Draw(DA, this, MaxTickBounds);
                    TickBorderBrush?.Draw(DA, this, MaxTickBounds, TickBorderThickness);
                }

                //  Compute thumb position
                float ThumbXPosition;
                if (Minimum.IsAlmostEqual(Maximum))
                    ThumbXPosition = NumberLineBounds.Center.X;
                else
                    ThumbXPosition = NumberLineBounds.Left + NumberLineBounds.Width * (Value - Minimum) / Interval;

                //  Draw the thumb
                Size ThumbSize = new(ActualThumbWidth, ActualThumbHeight);
                Rectangle ThumbBounds = ApplyAlignment(new((int)ThumbXPosition, LayoutBounds.Top, 0, LayoutBounds.Height), HorizontalAlignment.Center, VerticalAlignment.Center, ThumbSize);
                RecentThumbBounds = ThumbBounds;
                ActualThumbFillBrush.Draw(DA, this, ThumbBounds);
                ThumbBorderBrush?.Draw(DA, this, ThumbBounds, ThumbBorderThickness);

                if (!ParentWindow.HasModalWindow && (IsLMBPressed || IsHovered || IsDraggingThumb || IsHoveringThumb))
                {
                    //  Divide the number line into 2 pieces, the piece left of the thumb, and the piece right of the thumb
                    //  so that we don't draw overtop of the thumb with the number line overlays
                    Rectangle LeftNumberLine = Rectangle.Empty;
                    if (NumberLineBounds.Left < ThumbBounds.Left)
                        LeftNumberLine = new(NumberLineBounds.Left, NumberLineBounds.Top, ThumbBounds.Left - NumberLineBounds.Left, NumberLineBounds.Height);
                    Rectangle RightNumberLine = Rectangle.Empty;
                    if (NumberLineBounds.Right > ThumbBounds.Right)
                        RightNumberLine = new(ThumbBounds.Right, NumberLineBounds.Top, NumberLineBounds.Right - ThumbBounds.Right, NumberLineBounds.Height);
                    List<Rectangle> NumberLineChunks = new List<Rectangle>() { LeftNumberLine, RightNumberLine }.Where(x => x != Rectangle.Empty).ToList();

                    if (IsLMBPressed || IsDraggingThumb)
                    {
                        foreach (Rectangle Bounds in NumberLineChunks)
                        {
                            PressedOverlay.Draw(DA, this, Bounds);
                            PressedBorderOverlay.Draw(DA, this, Bounds, NumberLineBorderThickness);
                        }
                        PressedOverlay.Draw(DA, this, ThumbBounds);
                        PressedBorderOverlay.Draw(DA, this, ThumbBounds, ThumbBorderThickness);
                    }
                    else if (IsHovered || IsHoveringThumb)
                    {
                        foreach (Rectangle Bounds in NumberLineChunks)
                        {
                            HoveredOverlay.Draw(DA, this, Bounds);
                            HoveredBorderOverlay.Draw(DA, this, Bounds, NumberLineBorderThickness);
                        }
                        HoveredOverlay.Draw(DA, this, ThumbBounds);
                        HoveredBorderOverlay.Draw(DA, this, ThumbBounds, ThumbBorderThickness);
                    }
                }
            }
            else if (Orientation == Orientation.Vertical)
            {
                RecentStretchedNumberLineBounds = new(LayoutBounds.Left, NumberLineBounds.Top, LayoutBounds.Width, NumberLineBounds.Height);

                //  Draw the number line
                ActualNumberLineFillBrush.Draw(DA, this, NumberLineBounds);
                NumberLineBorderBrush?.Draw(DA, this, NumberLineBounds, NumberLineBorderThickness);

                if (DrawTicks && NumberLineBounds.Height > 0)
                {
                    Size TickSize = new(ActualTickWidth, ActualTickHeight);
                    float IntervalPercent = TickFrequency.Value / Interval;
                    float IntervalPixels = NumberLineBounds.Height * IntervalPercent;

                    //  Draw the ticks between the min and max
                    float CurrentYPosition = NumberLineBounds.Top + IntervalPixels;
                    while (CurrentYPosition < NumberLineBounds.Bottom)
                    {
                        Rectangle TickBounds = ApplyAlignment(new(LayoutBounds.Left, (int)CurrentYPosition, LayoutBounds.Width, 0), HorizontalAlignment.Center, VerticalAlignment.Center, TickSize);
                        ActualTickFillBrush.Draw(DA, this, TickBounds);
                        TickBorderBrush?.Draw(DA, this, TickBounds, TickBorderThickness);
                        CurrentYPosition += IntervalPixels;
                    }

                    //  Draw the ticks at the min and max
                    Rectangle MinTickBounds = ApplyAlignment(new(LayoutBounds.Left, NumberLineBounds.Top, LayoutBounds.Width, 0), HorizontalAlignment.Center, VerticalAlignment.Center, TickSize);
                    ActualTickFillBrush.Draw(DA, this, MinTickBounds);
                    TickBorderBrush?.Draw(DA, this, MinTickBounds, TickBorderThickness);
                    Rectangle MaxTickBounds = ApplyAlignment(new(LayoutBounds.Right, NumberLineBounds.Top, LayoutBounds.Width, 0), HorizontalAlignment.Center, VerticalAlignment.Center, TickSize);
                    ActualTickFillBrush.Draw(DA, this, MaxTickBounds);
                    TickBorderBrush?.Draw(DA, this, MaxTickBounds, TickBorderThickness);
                }

                //  Compute thumb position
                float ThumbYPosition;
                if (Minimum.IsAlmostEqual(Maximum))
                    ThumbYPosition = NumberLineBounds.Center.Y;
                else
                    ThumbYPosition = NumberLineBounds.Top + NumberLineBounds.Height * (Value - Minimum) / Interval;

                //  Draw the thumb
                Size ThumbSize = new(ActualThumbWidth, ActualThumbHeight);
                Rectangle ThumbBounds = ApplyAlignment(new(LayoutBounds.Left, (int)ThumbYPosition, LayoutBounds.Width, 0), HorizontalAlignment.Center, VerticalAlignment.Center, ThumbSize);
                RecentThumbBounds = ThumbBounds;
                ActualThumbFillBrush.Draw(DA, this, ThumbBounds);
                ThumbBorderBrush?.Draw(DA, this, ThumbBounds, ThumbBorderThickness);

                if (!ParentWindow.HasModalWindow && (IsLMBPressed || IsHovered || IsDraggingThumb || IsHoveringThumb))
                {
                    //  Divide the number line into 2 pieces, the piece above the thumb, and the piece below the thumb
                    //  so that we don't draw overtop of the thumb with the number line overlays
                    Rectangle TopNumberLine = Rectangle.Empty;
                    if (NumberLineBounds.Top < ThumbBounds.Top)
                        TopNumberLine = new(NumberLineBounds.Left, NumberLineBounds.Top, NumberLineBounds.Width, ThumbBounds.Top - NumberLineBounds.Top);
                    Rectangle BottomNumberLine = Rectangle.Empty;
                    if (NumberLineBounds.Bottom > ThumbBounds.Bottom)
                        BottomNumberLine = new(NumberLineBounds.Left, ThumbBounds.Bottom, NumberLineBounds.Width, NumberLineBounds.Bottom - ThumbBounds.Bottom);
                    List<Rectangle> NumberLineChunks = new List<Rectangle>() { TopNumberLine, BottomNumberLine }.Where(x => x != Rectangle.Empty).ToList();

                    if (IsLMBPressed || IsDraggingThumb)
                    {
                        foreach (Rectangle Bounds in NumberLineChunks)
                        {
                            PressedOverlay.Draw(DA, this, Bounds);
                            PressedBorderOverlay.Draw(DA, this, Bounds, NumberLineBorderThickness);
                        }
                        PressedOverlay.Draw(DA, this, ThumbBounds);
                        PressedBorderOverlay.Draw(DA, this, ThumbBounds, ThumbBorderThickness);
                    }
                    else if (IsHovered || IsHoveringThumb)
                    {
                        foreach (Rectangle Bounds in NumberLineChunks)
                        {
                            HoveredOverlay.Draw(DA, this, Bounds);
                            HoveredBorderOverlay.Draw(DA, this, Bounds, NumberLineBorderThickness);
                        }
                        HoveredOverlay.Draw(DA, this, ThumbBounds);
                        HoveredBorderOverlay.Draw(DA, this, ThumbBounds, ThumbBorderThickness);
                    }
                }
            }
            else
                throw new NotImplementedException($"Unrecognized {nameof(Orientation)}: {Orientation}");
        }
    }
}
