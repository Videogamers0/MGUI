using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.Containers;
using MGUI.Shared.Helpers;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI
{
    public enum ProgressButtonActionType
    {
        /// <summary>No action will be taken.</summary>
        None,
        /// <summary>The progress bar will be reset.</summary>
        Reset,
        /// <summary>The progress bar will be reset, and processing will be paused.</summary>
        ResetAndPause,
        /// <summary>The progress bar will be reset, and processing will be resumed.</summary>
        ResetAndResume,
        /// <summary>Processing will be paused.</summary>
        Pause,
        /// <summary>Processing will be resumed.</summary>
        Resume,
        /// <summary>Processing state will be toggled between paused and resumed. (If paused, it will be resumed. If not paused, it will be paused.)</summary>
        Toggle
    }

    public enum ProgressBarAlignment
    {
        /// <summary>If <see cref="MGProgressButton.IsHorizontal"/>, the progress bar's graphics will vertically stretch the <see cref="MGProgressButton"/>'s bounds.<br/>
        /// If <see cref="MGProgressButton.IsVertical"/>, the progress bar's graphics will horizontally stretch the <see cref="MGProgressButton"/>'s bounds.</summary>
        Stretch,
        /// <summary>If <see cref="MGProgressButton.IsHorizontal"/>, the progress bar's graphics will be vertically centered within the <see cref="MGProgressButton"/>'s bounds.<br/>
        /// If <see cref="MGProgressButton.IsVertical"/>, the progress bar's graphics will be horizontally centered the <see cref="MGProgressButton"/>'s bounds.</summary>
        Center,
        /// <summary>If <see cref="MGProgressButton.IsHorizontal"/>, the progress bar's graphics will be aligned to the top edge of the <see cref="MGProgressButton"/>'s bounds.<br/>
        /// If <see cref="MGProgressButton.IsVertical"/>, the progress bar's graphics will be aligned to the left edge of the <see cref="MGProgressButton"/>'s bounds.</summary>
        LeftOrTop,
        /// <summary>If <see cref="MGProgressButton.IsHorizontal"/>, the progress bar's graphics will be aligned to the right edge of the <see cref="MGProgressButton"/>'s bounds.<br/>
        /// If <see cref="MGProgressButton.IsVertical"/>, the progress bar's graphics will be aligned to the bottom edge of the <see cref="MGProgressButton"/>'s bounds.</summary>
        RightOrBottom
    }

    /// <summary>Represents a combination of an <see cref="MGButton"/> and <see cref="MGProgressBar"/>. 
    /// <see cref="MGProgressButton"/>s are buttons that, when clicked, start a progress bar (rendered underneath the <see cref="MGSingleContentHost.Content"/>) and temporarily disable the button until the progress bar completes.<para/>
    /// They are commonly used in clicker/incremental games (such as clicking a button, "Gather Wood" to obtain a resource, and waiting a few seconds before you can click it again), or for things like abilities with a cooldown period.</summary>
    public class MGProgressButton : MGSingleContentHost
    {
        #region Border
        /// <summary>Provides direct access to this element's border.</summary>
        public MGComponent<MGBorder> BorderComponent { get; }
        private MGBorder BorderElement { get; }
        public override MGBorder GetBorder() => BorderElement;

        public IBorderBrush BorderBrush
        {
            get => BorderElement.BorderBrush;
            set => BorderElement.BorderBrush = value;
        }

        public Thickness BorderThickness
        {
            get => BorderElement.BorderThickness;
            set => BorderElement.BorderThickness = value;
        }
        #endregion Border

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ProgressButtonActionType _ActionWhenPaused;
        /// <summary>Determines what action should be taken when the button is clicked when <see cref="IsPaused"/> is <see langword="true"/>.<para/>
        /// Default value: <see cref="ProgressButtonActionType.ResetAndResume"/></summary>
        public ProgressButtonActionType ActionWhenPaused
        {
            get => _ActionWhenPaused;
            set
            {
                if (_ActionWhenPaused != value)
                {
                    _ActionWhenPaused = value;
                    NPC(nameof(ActionWhenPaused));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ProgressButtonActionType _ActionWhenProcessing;
        /// <summary>Determines what action should be taken when the button is clicked when <see cref="IsPaused"/> is <see langword="false"/> AND <see cref="IsCompleted"/> is <see langword="false"/>.<para/>
        /// Default value: <see cref="ProgressButtonActionType.None"/></summary>
        public ProgressButtonActionType ActionWhenProcessing
        {
            get => _ActionWhenProcessing;
            set
            {
                if (_ActionWhenProcessing != value)
                {
                    _ActionWhenProcessing = value;
                    NPC(nameof(ActionWhenProcessing));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ProgressButtonActionType _ActionWhenCompleted;
        /// <summary>Determines what action should be taken when the button is clicked when <see cref="IsPaused"/> is <see langword="false"/> AND <see cref="IsCompleted"/> is <see langword="true"/>.<para/>
        /// Default value: <see cref="ProgressButtonActionType.None"/><para/>
        /// Note: This property is for when the button is clicked, whereas <see cref="ActionOnCompleted"/> is executed automatically when completion occurs.</summary>
        public ProgressButtonActionType ActionWhenCompleted
        {
            get => _ActionWhenCompleted;
            set
            {
                if (_ActionWhenCompleted != value)
                {
                    _ActionWhenCompleted = value;
                    NPC(nameof(ActionWhenCompleted));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ProgressButtonActionType _ActionOnCompleted;
        /// <summary>Determines what action should be taken when <see cref="Value"/> changes from being less than <see cref="Maximum"/> to being greater than or equal to <see cref="Maximum"/>.<para/>
        /// Default value: <see cref="ProgressButtonActionType.Pause"/><para/>
        /// To execute your own custom logic on completion, subscribe to: <see cref="OnCompleted"/><para/>
        /// Note: This property is for when completion occurs, whereas <see cref="ActionWhenCompleted"/> is executed when the button is clicked.</summary>
        public ProgressButtonActionType ActionOnCompleted
        {
            get => _ActionOnCompleted;
            set
            {
                if (_ActionOnCompleted != value)
                {
                    _ActionOnCompleted = value;
                    NPC(nameof(ActionOnCompleted));
                }
            }
        }

        /// <summary>Invoked when <see cref="Value"/> is set to <see cref="Minimum"/> and <see cref="IsPaused"/> is <see langword="false" /></summary>
        public event EventHandler<EventArgs> OnStarted;
        /// <summary>Invoked when <see cref="IsPaused"/> is set to <see langword="true"/></summary>
        public event EventHandler<EventArgs> OnPaused;
        /// <summary>Invoked when <see cref="IsPaused"/> is set to <see langword="false"/></summary>
        public event EventHandler<EventArgs> OnResumed;
        /// <summary>Invoked when <see cref="Value"/> changes from being less than <see cref="Maximum"/> to being greater than or equal to <see cref="Maximum"/>.<para/>
        /// If <see cref="Value"/> continues to increase beyond <see cref="Maximum"/>, this event will not be continually invoked.<para/>
        ///  This event may be invoked even if <see cref="IsPaused"/> is <see langword="true"/>.</summary>
        public event EventHandler<EventArgs> OnCompleted;
        /// <summary>Invoked when <see cref="Value"/> is reset back down to <see cref="Minimum"/>. This event may be invoked even if <see cref="IsPaused"/> is <see langword="true"/>.</summary>
        public event EventHandler<EventArgs> OnReset;
        /// <summary>Invoked when <see cref="Value"/> changes. Parameter is the before and after value of <see cref="Value"/>. This event may be invoked even if <see cref="IsPaused"/> is <see langword="true"/>.</summary>
        public event EventHandler<EventArgs<double>> OnProgressChanged;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _HideWhenPaused;
        /// <summary>If <see langword="true"/>, the progress bar will not be rendered when <see cref="IsPaused"/> is <see langword="true" />.<para/>
        /// Default value: <see langword="false" /></summary>
        public bool HideWhenPaused
        {
            get => _HideWhenPaused;
            set
            {
                if (_HideWhenPaused != value)
                {
                    _HideWhenPaused = value;
                    NPC(nameof(HideWhenPaused));
                    NPC(nameof(IsProgressBarVisible));
                    if (IsPaused)
                        LayoutChanged(this, true);
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _IsPaused;
        /// <summary>If <see langword="true" />, the progress bar will not automatically progress even if <see cref="Duration"/> is not <see langword="null" />.</summary>
        public bool IsPaused
        {
            get => _IsPaused;
            set
            {
                if (_IsPaused != value)
                {
                    _IsPaused = value;
                    NPC(nameof(IsPaused));
                    NPC(nameof(IsProgressBarVisible));
                    if (IsPaused)
                        OnPaused?.Invoke(this, EventArgs.Empty);
                    else
                    {
                        OnResumed?.Invoke(this, EventArgs.Empty);
                        if (Value.IsAlmostEqual(Minimum))
                            OnStarted?.Invoke(this, EventArgs.Empty);
                    }

                    if (HideWhenPaused)
                        LayoutChanged(this, true);
                }
            }
        }

        /// <summary>See also: <see cref="HideWhenPaused"/>, <see cref="IsPaused"/></summary>
        public bool IsProgressBarVisible => !IsPaused || !HideWhenPaused;

        #region Value
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private float _Minimum;
        /// <summary>The minimum value of the progress bar which represents 0% completion.</summary>
        public float Minimum
        {
            get => _Minimum;
            set
            {
                if (_Minimum != value)
                {
                    _Minimum = value;
                    NPC(nameof(Minimum));
                    NPC(nameof(ActualValue));
                    NPC(nameof(ValuePercent));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private float _Maximum;
        /// <summary>The maximum value of the progress bar which represents 100% completion.</summary>
        public float Maximum
        {
            get => _Maximum;
            set
            {
                if (_Maximum != value)
                {
                    _Maximum = value;
                    NPC(nameof(Maximum));
                    NPC(nameof(ActualValue));
                    NPC(nameof(ValuePercent));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private float _Value;
        /// <summary>The current value of the progress bar.</summary>
        public float Value
        {
            get => _Value;
            set
            {
                if (_Value != value)
                {
                    bool WasCompleted = IsCompleted;
                    float Previous = Value;
                    _Value = value;
                    NPC(nameof(Value));
                    NPC(nameof(ActualValue));
                    NPC(nameof(ValuePercent));
                    OnProgressChanged?.Invoke(this, new EventArgs<double>(Previous, Value));

                    if (!WasCompleted && IsCompleted)
                    {
                        OnCompleted?.Invoke(this, EventArgs.Empty);
                        PerformAction(ActionOnCompleted);
                    }
                    if (Value.IsAlmostEqual(Minimum) && !Previous.IsAlmostEqual(Minimum))
                    {
                        OnReset?.Invoke(this, EventArgs.Empty);
                        if (!IsPaused)
                            OnStarted?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }
        
        /// <summary><see langword="true" /> if <see cref="Value"/> is >= <see cref="Maximum"/></summary>
        public bool IsCompleted => Value.IsGreaterThanOrEqual(Maximum);

        /// <summary>The <see cref="Value"/>, clamped to the range given by [<see cref="Minimum"/>, <see cref="Maximum"/>]</summary>
        public float ActualValue => Maximum.IsLessThan(Minimum) ? Minimum : Math.Clamp(Value, Minimum, Maximum);

        /// <summary>Completed percent, between 0.0 and 100.0</summary>
        public float ValuePercent => (Maximum.IsAlmostEqual(Minimum) ? 1.0f : (ActualValue - Minimum) / (Maximum - Minimum)) * 100.0f;

        public void SetRange(float Minimum, float Maximum)
        {
            if (Minimum > Maximum)
                throw new ArgumentOutOfRangeException(nameof(Minimum), Minimum, $"Value of {nameof(Minimum)} cannot be greater than value of {nameof(Maximum)}.");
            this.Minimum = Minimum;
            this.Maximum = Maximum;
        }

        /// <summary>Sets <see cref="Value"/> to the given completion percent.<para/>
        /// EX: If <see cref="Minimum"/>=20, <see cref="Maximum"/>=50, and <paramref name="ValuePercent"/>=25.0, then <see cref="Value"/> would be set to: 20 + 0.25*(50-20) = 27.5</summary>
        /// <param name="ValuePercent">A value between 0.0 and 100.0</param>
        public void SetValuePercent(double ValuePercent)
        {
            if (ValuePercent.IsLessThan(0.0) || ValuePercent.IsGreaterThan(100.0))
                throw new ArgumentOutOfRangeException(nameof(ValuePercent), ValuePercent, $"{nameof(ValuePercent)} must be a value between 0.0 and 100.0 inclusive.");
            Value = (float)(Minimum + ValuePercent / 100.0 * (Maximum - Minimum));
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private TimeSpan? _Duration;
        /// <summary>Determines how long it should take for the progress bar to go from 0% to 100%.<br/>
        /// If <see langword="null"/>, then the progress bar will NOT be automatically incremented during a game update tick.<para/>
        /// Default value: <see langword="null" /></summary>
        public TimeSpan? Duration
        {
            get => _Duration;
            set
            {
                if (_Duration != value)
                {
                    _Duration = value;
                    NPC(nameof(Duration));
                }
            }
        }

        /// <summary>Only relevant if <see cref="Duration"/> is not <see langword="null" />,<br/>
        /// Determines how long until the progress bar will reach 100% completion from its current value.<br/>
        /// (This property is computed under the assumption that <see cref="IsPaused"/> is <see langword="false" />)<para/>
        /// See also: <see cref="Duration"/>, <see cref="Minimum"/>, <see cref="Maximum"/>, <see cref="Value"/>, <see cref="ValuePercent"/></summary>
        public TimeSpan? RemainingDuration
        {
            get
            {
                if (!Duration.HasValue)
                    return null;
                else if (Value >= Maximum)
                    return TimeSpan.Zero;
                else
                    return Duration.Value * (Value - Minimum) / (Maximum - Minimum);
            }
        }
        #endregion Value

        #region Progress Bar Bounds
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Orientation _Orientation;
        /// <summary>If <see cref="Orientation.Horizontal"/>, the bar grows from left to right.<br/>
        /// If <see cref="Orientation.Vertical"/>, the bar grows from bottom to top.<para/>
        /// Default value: <see cref="Orientation.Horizontal"/><para/>
        /// See also: <see cref="IsReversed"/></summary>
        public Orientation Orientation
        {
            get => _Orientation;
            set
            {
                if (_Orientation != value)
                {
                    _Orientation = value;
                    LayoutChanged(this, true);
                    NPC(nameof(Orientation));
                    NPC(nameof(IsHorizontal));
                    NPC(nameof(IsVertical));
                }
            }
        }

        /// <summary><see langword="true"/> if the progress bar is horizontally-oriented (See: <see cref="Orientation"/>)</summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsHorizontal => Orientation == Orientation.Horizontal;
        /// <summary><see langword="true"/> if the progress bar is vertically-oriented (See: <see cref="Orientation"/>)</summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsVertical => Orientation == Orientation.Vertical;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _IsReversed;
        /// <summary>If true, the bar will flow in the opposite direction.<para/>
        /// If <see cref="Orientation"/> is <see cref="Orientation.Horizontal"/>, progress bar starts on the right, and completes on the left.<br/>
        /// If <see cref="Orientation"/> is <see cref="Orientation.Vertical"/>, progress bar starts on the top, and completes on the bottom.<para/>
        /// Default value: <see langword="false"/></summary>
        public bool IsReversed
        {
            get => _IsReversed;
            set
            {
                if (_IsReversed != value)
                {
                    _IsReversed = value;
                    NPC(nameof(IsReversed));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ProgressBarAlignment _ProgressBarAlignment;
        /// <summary>Only relevant if <see cref="ProgressBarSize"/> has a non-null and positive value. Determines how the graphics of the progress bar will be aligned within this element's bounds.<para/>
        /// Default value: <see cref="ProgressBarAlignment.Stretch"/></summary>
        public ProgressBarAlignment ProgressBarAlignment
        {
            get => _ProgressBarAlignment;
            set
            {
                if (_ProgressBarAlignment != value)
                {
                    _ProgressBarAlignment = value;
                    LayoutChanged(this, true);
                    NPC(nameof(ProgressBarAlignment));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int? _ProgressBarSize;
        /// <summary>Only relevant if <see cref="ProgressBarAlignment"/> is not set to <see cref="ProgressBarAlignment.Stretch"/>.<para/>
        /// If <see cref="IsHorizontal"/> is <see langword="true" />, this property determines the height of the progress bar.<br/>
        /// If <see cref="IsVertical"/> is <see langword="true" />, this property determines the width of the progress bar.<para/>
        /// If set to <see langword="null" />, then the progress bar will stretch the entire dimension of this element's bounds.
        /// Default value: <see langword="null"/>.<para/>
        /// Note: If not set to <see langword="null" />, then the value will include any border thickness from <see cref="ProgressBarBorderThickness"/>.</summary>
        public int? ProgressBarSize
        {
            get => _ProgressBarSize;
            set
            {
                if (_ProgressBarSize != value)
                {
                    _ProgressBarSize = value;
                    LayoutChanged(this, true);
                    NPC(nameof(ProgressBarSize));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Thickness _ProgressBarMargin;
        /// <summary>Empty space reserved around the progress bar. Using a non-zero margin can reposition the progress bar so it does not align right up against an edge of this element's bounds.<para/>
        /// Default value: an empty <see cref="Thickness"/> (all sides equal to zero)</summary>
        public Thickness ProgressBarMargin
        {
            get => _ProgressBarMargin;
            set
            {
                if (!_ProgressBarMargin.Equals(value))
                {
                    _ProgressBarMargin = value;
                    LayoutChanged(this, true);
                    NPC(nameof(ProgressBarMargin));
                }
            }
        }

        private Rectangle GetProgressBarBounds(Rectangle ElementBounds, bool IncludeProgressBarBorder)
        {
            Rectangle PaddedBounds = ElementBounds.GetCompressed(ProgressBarMargin);
            if (!IncludeProgressBarBorder && ProgressBarBorderBrush != null)
                PaddedBounds = PaddedBounds.GetCompressed(ProgressBarBorderThickness);

            if (ProgressBarSize.HasValue)
            {
                Size BarSize = new(ProgressBarSize.Value, ProgressBarSize.Value);
                if (!IncludeProgressBarBorder && ProgressBarBorderBrush != null)
                    BarSize = BarSize.Subtract(ProgressBarBorderThickness.Size, 0, 0);

                return ProgressBarAlignment switch
                {
                    ProgressBarAlignment.Stretch => PaddedBounds,
                    ProgressBarAlignment.Center =>
                        Orientation switch
                        {
                            Orientation.Horizontal => ApplyAlignment(PaddedBounds, HorizontalAlignment.Stretch, VerticalAlignment.Center, BarSize),
                            Orientation.Vertical => ApplyAlignment(PaddedBounds, HorizontalAlignment.Center, VerticalAlignment.Stretch, BarSize),
                            _ => throw new NotImplementedException($"Unrecognized {nameof(UI.Orientation)}: {Orientation}"),
                        },
                    ProgressBarAlignment.LeftOrTop => 
                        Orientation switch
                        {
                            Orientation.Horizontal => ApplyAlignment(PaddedBounds, HorizontalAlignment.Stretch, VerticalAlignment.Top, BarSize),
                            Orientation.Vertical => ApplyAlignment(PaddedBounds, HorizontalAlignment.Left, VerticalAlignment.Stretch, BarSize),
                            _ => throw new NotImplementedException($"Unrecognized {nameof(UI.Orientation)}: {Orientation}"),
                        },
                    ProgressBarAlignment.RightOrBottom =>
                        Orientation switch
                        {
                            Orientation.Horizontal => ApplyAlignment(PaddedBounds, HorizontalAlignment.Stretch, VerticalAlignment.Bottom, BarSize),
                            Orientation.Vertical => ApplyAlignment(PaddedBounds, HorizontalAlignment.Right, VerticalAlignment.Stretch, BarSize),
                            _ => throw new NotImplementedException($"Unrecognized {nameof(UI.Orientation)}: {Orientation}"),
                        },
                    _ => throw new NotImplementedException($"Unrecognized {nameof(UI.ProgressBarAlignment)}: {ProgressBarAlignment}"),
                };
            }
            else
                return PaddedBounds;
        }
        #endregion Progress Bar Bounds

        #region Progress Bar Graphics
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Thickness _ProgressBarBorderThickness;
        /// <summary>Only relevant if <see cref="ProgressBarBorderBrush"/> is not <see langword="null" />.<para/>
        /// The border thickness for the progress bar's graphics.<para/>
        /// Default value: an empty <see cref="Thickness"/> (all sides equal to zero)<para/>
        /// Note: if <see cref="ProgressBarSize"/> is not <see langword="null" />, then the <see cref="ProgressBarSize"/> will include the border.</summary>
        public Thickness ProgressBarBorderThickness
        {
            get => _ProgressBarBorderThickness;
            set
            {
                if (!_ProgressBarBorderThickness.Equals(value))
                {
                    _ProgressBarBorderThickness = value;
                    NPC(nameof(ProgressBarBorderThickness));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IBorderBrush _ProgressBarBorderBrush;
        /// <summary>Only relevant if <see cref="ProgressBarBorderThickness"/> is non-empty.<para/>
        /// The border brush to use around the progress bar.<para/>
        /// Default value: <see langword="null" /></summary>
        public IBorderBrush ProgressBarBorderBrush
        {
            get => _ProgressBarBorderBrush;
            set
            {
                if (_ProgressBarBorderBrush != value)
                {
                    _ProgressBarBorderBrush = value;
                    NPC(nameof(ProgressBarBorderBrush));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IFillBrush _ProgressBarBackground;
        /// <summary>The brush that is used to draw the background portion of the progress bar. Default value: <see langword="null"/>.<para/>
        /// If your progress bar does not span the entire bounds of this <see cref="MGProgressButton"/>,<br/>
        /// (such as if <see cref="ProgressBarAlignment"/> is not set to <see cref="ProgressBarAlignment.Stretch"/>, or if <see cref="ProgressBarMargin"/> is non-zero)<br/>
        /// then you should probably set this to a non-transparent value.<para/>
        /// Note: The progress bar is rendered overtop of this element's <see cref="MGElement.BackgroundBrush"/>, but underneath this element's <see cref="MGSingleContentHost.Content"/>.</summary>
        public IFillBrush ProgressBarBackground
        {
            get => _ProgressBarBackground;
            set
            {
                if (_ProgressBarBackground != value)
                {
                    _ProgressBarBackground = value;
                    NPC(nameof(ProgressBarBackground));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IFillBrush _ProgressBarForeground;
        /// <summary>The brush that is used to draw the completed portion of the progress bar.<para/>
        /// Default value: <see cref="MGTheme.ProgressButtonForeground"/>.<para/>
        /// Note: The progress bar is rendered overtop of this element's <see cref="MGElement.BackgroundBrush"/>, but underneath this element's <see cref="MGSingleContentHost.Content"/>.</summary>
        public IFillBrush ProgressBarForeground
        {
            get => _ProgressBarForeground;
            set
            {
                if (_ProgressBarForeground != value)
                {
                    _ProgressBarForeground = value;
                    NPC(nameof(ProgressBarForeground));
                }
            }
        }
        #endregion Progress Bar Graphics

        public MGProgressButton(MGWindow Window, Thickness BorderThickness, IBorderBrush BorderBrush, 
            float Minimum = 0, float Maximum = 100, float Value = 0, Orientation Orientation = Orientation.Horizontal)
            : base(Window, MGElementType.ProgressButton)
        {
            using (BeginInitializing())
            {
                this.MinWidth = 16;
                this.MinHeight = 16;

                this.BorderElement = new(Window, BorderThickness, BorderBrush);
                this.BorderComponent = MGComponentBase.Create(BorderElement);
                AddComponent(BorderComponent);
                BorderElement.OnBorderBrushChanged += (sender, e) => { NPC(nameof(BorderBrush)); };
                BorderElement.OnBorderThicknessChanged += (sender, e) => { NPC(nameof(BorderThickness)); };

                this.HorizontalContentAlignment = HorizontalAlignment.Center;
                this.VerticalContentAlignment = VerticalAlignment.Center;
                this.Padding = new(4, 2, 4, 2);

                SetRange(Minimum, Maximum);
                this.Value = Value;

                ActionWhenProcessing = ProgressButtonActionType.None;
                ActionWhenPaused = ProgressButtonActionType.ResetAndResume;
                ActionWhenCompleted = ProgressButtonActionType.None;
                ActionOnCompleted = ProgressButtonActionType.Pause;

                HideWhenPaused = false;
                IsPaused = true;

                Duration = null;

                Orientation = Orientation.Horizontal;
                IsReversed = false;
                ProgressBarAlignment = ProgressBarAlignment.Stretch;
                ProgressBarSize = null;
                ProgressBarMargin = new(0);
                ProgressBarBorderThickness = new(0);
                ProgressBarBorderBrush = null;
                ProgressBarBackground = null;
                ProgressBarForeground = GetTheme().ProgressButtonForeground.GetValue(true);

                MouseHandler.ReleasedInside += (sender, e) =>
                {
                    if (e.IsLMB)
                    {
                        PerformAction(IsPaused ? ActionWhenPaused : ActionWhenProcessing);
                    }
                };
            }
        }

        public override Thickness MeasureSelfOverride(Size AvailableSize, out Thickness SharedSize)
        {
            if (!IsProgressBarVisible || !ProgressBarSize.HasValue || ProgressBarAlignment == ProgressBarAlignment.Stretch)
                return base.MeasureSelfOverride(AvailableSize, out SharedSize);

            //  Compute the actual dimensions of the progress bar and its margin
            int Size = ProgressBarSize.Value;
            Thickness ProgressBarThickness = Orientation switch
            {
                Orientation.Horizontal => new(Size + ProgressBarMargin.Width, Size / 2 + ProgressBarMargin.Height, 0, 0),
                Orientation.Vertical => new(Size / 2 + ProgressBarMargin.Width, Size + ProgressBarMargin.Height, 0, 0),
                _ => throw new NotImplementedException($"Unrecognized {nameof(Orientation)}: {Orientation}")
            };

            Thickness Result = base.MeasureSelfOverride(AvailableSize, out SharedSize);

            //  Ensure the requested size is at least as large as the progress bar
            if (Result.Width < ProgressBarThickness.Width)
                Result = Result.ChangeLeft(ProgressBarThickness.Width);
            if (Result.Height < ProgressBarThickness.Height)
                Result = Result.ChangeTop(ProgressBarThickness.Height);

            //  Allow the progress bar to share it's space with the button's Content since the progress bar will be rendered underneath the Content
            SharedSize = SharedSize.Add(ProgressBarThickness);

            return Result;
        }

        public override void UpdateSelf(ElementUpdateArgs UA)
        {
            if (!IsPaused && Duration.HasValue && !IsCompleted)
            {
                Value += (float)(UA.BA.FrameElapsed / Duration.Value * (Maximum - Minimum));
            }
            base.UpdateSelf(UA);
        }

        public override void DrawSelf(ElementDrawArgs DA, Rectangle LayoutBounds)
        {
            if (IsProgressBarVisible)
            {
                Rectangle BorderlessBounds = LayoutBounds.GetCompressed(BorderThickness);
                Rectangle ProgressBarBorderedBounds = GetProgressBarBounds(BorderlessBounds, true);
                Rectangle ProgressBarBorderlessBounds = GetProgressBarBounds(BorderlessBounds, false);

                float CompletedScalar = ValuePercent / 100.0f;
                Rectangle CompletedBounds;
                if (Orientation == Orientation.Horizontal)
                {
                    HorizontalAlignment HA = IsReversed ? HorizontalAlignment.Right : HorizontalAlignment.Left;
                    CompletedBounds = ApplyAlignment(ProgressBarBorderlessBounds, HA, VerticalAlignment.Stretch, new((int)(ProgressBarBorderlessBounds.Width * CompletedScalar), 0));
                }
                else if (Orientation == Orientation.Vertical)
                {
                    VerticalAlignment VA = IsReversed ? VerticalAlignment.Top : VerticalAlignment.Bottom;
                    CompletedBounds = ApplyAlignment(ProgressBarBorderlessBounds, HorizontalAlignment.Stretch, VA, new(0, (int)(ProgressBarBorderlessBounds.Height * CompletedScalar)));
                }
                else
                    throw new NotImplementedException($"Unrecognized {nameof(Orientation)}: {Orientation}");

                //  Draw the progress bar
                if (!ProgressBarBorderThickness.IsEmpty())
                    ProgressBarBorderBrush?.Draw(DA, this, ProgressBarBorderedBounds, ProgressBarBorderThickness);
                ProgressBarBackground?.Draw(DA, this, ProgressBarBorderlessBounds);
                if (CompletedBounds.Width > 0 && CompletedBounds.Height > 0)
                    ProgressBarForeground?.Draw(DA, this, CompletedBounds);
            }

            base.DrawSelf(DA, LayoutBounds);
        }

        public void PerformAction(ProgressButtonActionType Action)
        {
            switch (Action)
            {
                case ProgressButtonActionType.None: return;
                case ProgressButtonActionType.Reset:
                    Value = Minimum;
                    return;
                case ProgressButtonActionType.ResetAndPause:
                    IsPaused = true;
                    Value = Minimum;
                    return;
                case ProgressButtonActionType.ResetAndResume:
                    IsPaused = false;
                    Value = Minimum;
                    return;
                case ProgressButtonActionType.Pause:
                    IsPaused = true;
                    return;
                case ProgressButtonActionType.Resume:
                    IsPaused = false;
                    return;
                case ProgressButtonActionType.Toggle:
                    IsPaused = !IsPaused;
                    return;
                default: throw new NotImplementedException($"Unrecognized {nameof(ProgressButtonActionType)}: {Action}");
            }
        }

        public void Pause() => PerformAction(ProgressButtonActionType.Pause);
        public void Resume() => PerformAction(ProgressButtonActionType.Resume);
        public void ResetProgress() => PerformAction(ProgressButtonActionType.Reset);
    }
}