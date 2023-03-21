using Microsoft.Xna.Framework;
using MGUI.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoGame.Extended;
using MGUI.Core.UI.Containers;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Shared.Input.Mouse;
using System.Diagnostics;

namespace MGUI.Core.UI
{
    public class MGScrollViewer : MGSingleContentHost
    {
        public const int VSBWidth = 16;
        public const int HSBHeight = 16;

        private const int ScrollBarPadding = 2;

        //TODO: bool AllowClickDragScrolling
        //      if true, this ScrollViewer attempts to handle DragStart, Dragged, and DragEnd events when clicking anywhere within the Viewport's bounds
        //      (assuming a child element didn't already handle the event.
        //      maybe only detects it if the start position of the drag wasn't overtop of any child content that handles input (like MGButton, MGRadioButton, MGCheckBox, MGTextBox etc)

        /// <summary>Represents how much <see cref="VerticalOffset"/> will be changed when using the mouse scroll wheel.<para/>
        /// Recommended value: Anywhere from 20 to 80 (Scrolling on reddit.com seems to scroll by about 84px? Might be percentage-based)</summary>
        public const int VerticalScrollInterval = 40;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ScrollBarVisibility _VSBVisibility;
        public ScrollBarVisibility VSBVisibility
        {
            get => _VSBVisibility;
            set
            {
                if (_VSBVisibility != value)
                {
                    _VSBVisibility = value;
                    LayoutChanged(this, true);
                    NPC(nameof(VSBVisibility));
                    NPC(nameof(VerticalScrollBarVisibility));
                }
            }
        }

        /// <summary>This property is the same as <see cref="VSBVisibility"/></summary>
        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get => VSBVisibility;
            set => VSBVisibility = value;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ScrollBarVisibility _HSBVisibility;
        public ScrollBarVisibility HSBVisibility
        {
            get => _HSBVisibility;
            set
            {
                if (_HSBVisibility != value)
                {
                    _HSBVisibility = value;
                    LayoutChanged(this, true);
                    NPC(nameof(HSBVisibility));
                    NPC(nameof(HorizontalScrollBarVisibility));
                }
            }
        }

        /// <summary>This property is the same as <see cref="HSBVisibility"/></summary>
        public ScrollBarVisibility HorizontalScrollBarVisibility
        {
            get => HSBVisibility;
            set => HSBVisibility = value;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Rectangle _ContentViewport;
        /// <summary>The bounds that this <see cref="MGScrollViewer"/>'s Content can draw itself to, 
        /// after accounting for <see cref="MGElement.Padding"/> and the width/height that the scrollbars reserved, if any.</summary>
        public Rectangle ContentViewport
        {
            get => _ContentViewport;
            private set
            {
                if (_ContentViewport != value)
                {
                    _ContentViewport = value;
                    NPC(nameof(ContentViewport));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Rectangle? _VSBBounds;
        public Rectangle? VSBBounds
        {
            get => _VSBBounds;
            private set
            {
                if (_VSBBounds != value)
                {
                    Rectangle? Previous = VSBBounds;
                    _VSBBounds = value;
                    NPC(nameof(VSBBounds));
                    NPC(nameof(PaddedVSBBounds));
                    VerticalScrollBarBoundsChanged?.Invoke(Previous, VSBBounds);
                }
            }
        }
        public event EventHandler<Rectangle?> VerticalScrollBarBoundsChanged;

        /// <summary>The screen bounds that the vertical scrollbar will be rendered to, after applying the <see cref="ScrollBarPadding"/></summary>
        public Rectangle? PaddedVSBBounds => VSBBounds?.GetCompressed(ScrollBarPadding);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Rectangle? _HSBBounds;
        public Rectangle? HSBBounds
        {
            get => _HSBBounds;
            private set
            {
                if (_HSBBounds != value)
                {
                    Rectangle? Previous = HSBBounds;
                    _HSBBounds = value;
                    NPC(nameof(HSBBounds));
                    NPC(nameof(PaddedHSBBounds));
                    HorizontalScrollBarBoundsChanged?.Invoke(Previous, HSBBounds);
                }
            }
        }

        public event EventHandler<Rectangle?> HorizontalScrollBarBoundsChanged;

        /// <summary>The screen bounds that the horizontal scrollbar will be rendered to, after applying the <see cref="ScrollBarPadding"/></summary>
        public Rectangle? PaddedHSBBounds => HSBBounds?.GetCompressed(ScrollBarPadding);

        #region Offset
        /// <summary>Invoked when either <see cref="HorizontalOffset"/> or <see cref="VerticalOffset"/> changes.<para/>
        /// See also: <see cref="HorizontalOffsetChanged"/>, <see cref="VerticalOffsetChanged"/></summary>
        public event EventHandler<EventArgs> OffsetChanged;
        public event EventHandler<EventArgs<float>> VerticalOffsetChanged;
        public event EventHandler<EventArgs<float>> HorizontalOffsetChanged;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private float _VerticalOffset;
        /// <summary>See also: <see cref="MaxVerticalOffset"/></summary>
        public float VerticalOffset
        {
            get => _VerticalOffset;
            set
            {
                float ClampedValue = Math.Clamp(value, 0, MaxVerticalOffset);
                if (_VerticalOffset != ClampedValue)
                {
                    float Previous = VerticalOffset;
                    _VerticalOffset = ClampedValue;
                    ParentWindow.InvalidatePressedAndHoveredElements = true;
                    NPC(nameof(VerticalOffset));
                    VerticalOffsetChanged?.Invoke(this, new(Previous, VerticalOffset));
                    OffsetChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private float _MaxVerticalOffset;
        /// <summary>The maximum value that <see cref="VerticalOffset"/> can be set to.<br/>
        /// Setting <see cref="VerticalOffset"/> to this value will scroll to the bottom of the scrollable content.</summary>
        public float MaxVerticalOffset
        {
            get => _MaxVerticalOffset;
            private set
            {
                if (_MaxVerticalOffset != value)
                {
                    float Previous = MaxVerticalOffset;
                    _MaxVerticalOffset = value;
                    VerticalOffset = Math.Clamp(VerticalOffset, 0, MaxVerticalOffset);
                    NPC(nameof(MaxVerticalOffset));
                    MaxVerticalOffsetChanged?.Invoke(this, new(Previous, MaxVerticalOffset));
                }
            }
        }

        public event EventHandler<EventArgs<float>> MaxVerticalOffsetChanged;

        /// <summary>Attempts to immediately invoke <see cref="ScrollToBottom"/> if <see cref="MGElement.IsLayoutValid"/> is true.<br/>
        /// Else queues an action that will invoke <see cref="ScrollToBottom"/> the next time the <see cref="MaxVerticalOffset"/> value changes.</summary>
        public void QueueScrollToBottom()
        {
            if (IsLayoutValid)
                ScrollToBottom();
            else
                MaxVerticalOffsetChanged += MaxVerticalOffsetChanged_ScrollToBottom;
        }

        private void MaxVerticalOffsetChanged_ScrollToBottom(object sender, EventArgs<float> e)
        {
            ScrollToBottom();
            MaxVerticalOffsetChanged -= MaxVerticalOffsetChanged_ScrollToBottom;
        }

        public void ScrollToTop() => VerticalOffset = 0;
        public void ScrollToBottom() => VerticalOffset = MaxVerticalOffset;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private float _HorizontalOffset;
        /// <summary>See also: <see cref="MaxHorizontalOffset"/></summary>
        public float HorizontalOffset
        {
            get => _HorizontalOffset;
            set
            {
                float ClampedValue = Math.Clamp(value, 0, MaxHorizontalOffset);
                if (_HorizontalOffset != ClampedValue)
                {
                    float Previous = HorizontalOffset;
                    _HorizontalOffset = ClampedValue;
                    ParentWindow.InvalidatePressedAndHoveredElements = true;
                    NPC(nameof(HorizontalOffset));
                    HorizontalOffsetChanged?.Invoke(this, new(Previous, HorizontalOffset));
                    OffsetChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private float _MaxHorizontalOffset;
        /// <summary>The maximum value that <see cref="HorizontalOffset"/> can be set to.<br/>
        /// Setting <see cref="HorizontalOffset"/> to this value will scroll to the right-most of the scrollable content.</summary>
        public float MaxHorizontalOffset
        {
            get => _MaxHorizontalOffset;
            private set
            {
                if (_MaxHorizontalOffset != value)
                {
                    float Previous = MaxHorizontalOffset;
                    _MaxHorizontalOffset = value;
                    NPC(nameof(MaxHorizontalOffset));
                    HorizontalOffset = Math.Clamp(HorizontalOffset, 0, MaxHorizontalOffset);
                    MaxHorizontalOffsetChanged?.Invoke(this, new(Previous, MaxHorizontalOffset));
                }
            }
        }

        public event EventHandler<EventArgs<float>> MaxHorizontalOffsetChanged;
        #endregion Offset

        private bool IsHoveringVSB { get; set; }
        private bool IsDraggingVSB { get; set; }
        private bool IsVSBFocused => !VisualState.IsDisabled && (IsHoveringVSB || IsDraggingVSB);

        private bool IsHoveringHSB { get; set; }
        private bool IsDraggingHSB { get; set; }
        private bool IsHSBFocused => !VisualState.IsDisabled && (IsHoveringHSB || IsDraggingHSB);

        /// <summary>Must always be true for <see cref="MGScrollViewer"/> to avoid content that is out of the <see cref="ContentViewport"/>'s bounds from being visible.</summary>
        public override bool ClipToBounds
        { 
            get => base.ClipToBounds;
            set
            {
                if (value != true)
                {
                    //throw new InvalidOperationException($"{nameof(MGScrollViewer)}.{nameof(ClipToBounds)} must always be true for scrollable content.");
                    return;
                }
                base.ClipToBounds = value;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private VisualStateFillBrush _ScrollBarOuterBrush;
        /// <summary>The brush to use for the outer portion of the scrollbars.<br/>
        /// Default value: <see cref="MGTheme.ScrollBarOuterBrush"/><para/>
        /// See also:<br/><see cref="MGWindow.Theme"/><br/><see cref="MGDesktop.Theme"/></summary>
        public VisualStateFillBrush ScrollBarOuterBrush
        {
            get => _ScrollBarOuterBrush;
            set
            {
                if (_ScrollBarOuterBrush != value)
                {
                    _ScrollBarOuterBrush = value;
                    NPC(nameof(ScrollBarOuterBrush));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private VisualStateFillBrush _ScrollBarInnerBrush;
        /// <summary>The brush to use for the inner portion of the scrollbars.<br/>
        /// Default value: <see cref="MGTheme.ScrollBarInnerBrush"/><para/>
        /// See also:<br/><see cref="MGWindow.Theme"/><br/><see cref="MGDesktop.Theme"/></summary>
        public VisualStateFillBrush ScrollBarInnerBrush
        {
            get => _ScrollBarInnerBrush;
            set
            {
                if (_ScrollBarInnerBrush != value)
                {
                    _ScrollBarInnerBrush = value;
                    NPC(nameof(ScrollBarInnerBrush));
                }
            }
        }

        protected override bool CanCacheSelfMeasurement => false; // The self measurement depends on the measurement of the children, so it must be re-calculated each time it's requested

        public MGScrollViewer(MGWindow Window, ScrollBarVisibility VerticalScrollBarVisibility = ScrollBarVisibility.Auto, ScrollBarVisibility HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled) 
            : base(Window, MGElementType.ScrollViewer)
        {
            using (BeginInitializing())
            {
                MGTheme Theme = GetTheme();

                this.VSBVisibility = VerticalScrollBarVisibility;
                this.HSBVisibility = HorizontalScrollBarVisibility;

                //Padding = new(0, 0, 5, 5);
                Padding = new(0);

                this.ScrollBarOuterBrush = Theme.ScrollBarOuterBrush.GetValue(true);
                this.ScrollBarInnerBrush = Theme.ScrollBarInnerBrush.GetValue(true);

                OnLayoutBoundsChanged += (sender, e) =>
                {
#if true
                    Thickness RequestedContentSize = new(0);
                    if (VSBVisibility == ScrollBarVisibility.Auto || HSBVisibility == ScrollBarVisibility.Auto)
                    {
                        if (HasContent)
                        {
                            int ActualAvailableWidth = HSBVisibility == ScrollBarVisibility.Disabled ? AlignedContentBounds.Width : int.MaxValue;
                            int ActualAvailableHeight = VSBVisibility == ScrollBarVisibility.Disabled ? AlignedContentBounds.Height : int.MaxValue;
                            Size ActualAvailableSize = new(ActualAvailableWidth, ActualAvailableHeight);
                            Content.UpdateMeasurement(ActualAvailableSize, out _, out RequestedContentSize, out _, out _);
                        }
                    }

                    int ActualVSBWidth = VSBVisibility switch
                    {
                        ScrollBarVisibility.Disabled => 0,
                        ScrollBarVisibility.Auto => RequestedContentSize.Height > AlignedContentBounds.Height ? VSBWidth : 0,
                        ScrollBarVisibility.Hidden => VSBWidth,
                        ScrollBarVisibility.Visible => VSBWidth,
                        ScrollBarVisibility.Collapsed => 0,
                        _ => throw new NotImplementedException($"Unrecognized {nameof(ScrollBarVisibility)}: {VSBVisibility}"),
                    };

                    int ActualHSBHeight = HSBVisibility switch
                    {
                        ScrollBarVisibility.Disabled => 0,
                        ScrollBarVisibility.Auto => RequestedContentSize.Width > AlignedContentBounds.Width ? HSBHeight : 0,
                        ScrollBarVisibility.Hidden => HSBHeight,
                        ScrollBarVisibility.Visible => HSBHeight,
                        ScrollBarVisibility.Collapsed => 0,
                        _ => throw new NotImplementedException($"Unrecognized {nameof(ScrollBarVisibility)}: {HSBVisibility}"),
                    };

                    Size ScrollBarsSize = new(ActualVSBWidth, ActualHSBHeight);
                    Size ContentSize = Content?.AllocatedBounds.Size.AsSize() ?? AlignedContentBounds.Size.AsSize();
                    Size ViewportSize = LayoutBounds.Size.AsSize().Subtract(ScrollBarsSize, 0, 0).Subtract(PaddingSize, 0, 0);
                    ContentViewport = new(LayoutBounds.Left + Padding.Left, LayoutBounds.Top + Padding.Top, ViewportSize.Width, ViewportSize.Height);

                    VSBBounds = ActualVSBWidth == 0 ? null : new(LayoutBounds.Right - ActualVSBWidth, LayoutBounds.Top, ActualVSBWidth, LayoutBounds.Height - ActualHSBHeight);
                    HSBBounds = ActualHSBHeight == 0 ? null : new(LayoutBounds.Left, LayoutBounds.Bottom - ActualHSBHeight, LayoutBounds.Width - ActualVSBWidth, ActualHSBHeight);

                    MaxVerticalOffset = Math.Max(0, ContentSize.Height - ContentViewport.Height);
                    MaxHorizontalOffset = Math.Max(0, ContentSize.Width - ContentViewport.Width);
#else
                    Size ScrollBarsSize = new(RecentVSBWidth, RecentHSBHeight);
                    Size ContentSize = RecentContentSize.Size; //Content?.AllocatedBounds.Size.AsSize() ?? ActualContentBounds.Size.AsSize();
                    Size ViewportSize = LayoutBounds.Size.AsSize().Subtract(ScrollBarsSize, 0, 0).Subtract(PaddingSize, 0, 0);
                    ContentViewport = new(LayoutBounds.Left + Padding.Left, LayoutBounds.Top + Padding.Top, ViewportSize.Width, ViewportSize.Height);

                    VSBBounds = RecentVSBWidth == 0 ? null : new(LayoutBounds.Right - RecentVSBWidth, LayoutBounds.Top, RecentVSBWidth, LayoutBounds.Height - RecentHSBHeight);
                    HSBBounds = RecentHSBHeight == 0 ? null : new(LayoutBounds.Left, LayoutBounds.Bottom - RecentHSBHeight, LayoutBounds.Width - RecentVSBWidth, RecentHSBHeight);

                    MaxVerticalOffset = Math.Max(0, ContentSize.Height - ContentViewport.Height);
                    MaxHorizontalOffset = Math.Max(0, ContentSize.Width - ContentViewport.Width);
#endif
                };

                MouseHandler.MovedInside += (sender, e) =>
                {
                    Point LayoutSpacePosition = ConvertCoordinateSpace(CoordinateSpace.Screen, CoordinateSpace.Layout, e.CurrentPosition);
                    IsHoveringVSB = !ParentWindow.HasModalWindow && VSBBounds.HasValue && VSBBounds.Value.ContainsInclusive(LayoutSpacePosition);
                    IsHoveringHSB = !ParentWindow.HasModalWindow && HSBBounds.HasValue && HSBBounds.Value.ContainsInclusive(LayoutSpacePosition);
                };

                MouseHandler.MovedOutside += (sender, e) =>
                {
                    IsHoveringVSB = false;
                    IsHoveringHSB = false;
                };

                MouseHandler.LMBPressedInside += (sender, e) =>
                {
                    if (IsHoveringVSB)
                    {
                        e.SetHandledBy(this, false);
                        Point LayoutSpacePosition = ConvertCoordinateSpace(CoordinateSpace.Screen, CoordinateSpace.Layout, e.Position);
                        HandleScrollBarInput(Orientation.Vertical, e.Button, LayoutSpacePosition.Y);
                    }

                    if (IsHoveringHSB)
                    {
                        e.SetHandledBy(this, false);
                        Point LayoutSpacePosition = ConvertCoordinateSpace(CoordinateSpace.Screen, CoordinateSpace.Layout, e.Position);
                        HandleScrollBarInput(Orientation.Horizontal, e.Button, LayoutSpacePosition.X);
                    }
                };

                MouseHandler.DragStartCondition = DragStartCondition.MousePressed;

                MouseHandler.DragStart += (sender, e) =>
                {
                    if (e.IsLMB)
                    {
                        if (IsHoveringVSB)
                        {
                            e.SetHandledBy(this, false);
                            IsDraggingVSB = true;
                        }

                        if (IsHoveringHSB)
                        {
                            e.SetHandledBy(this, false);
                            IsDraggingHSB = true;
                        }
                    }
                };

                MouseHandler.DragEnd += (sender, e) =>
                {
                    IsDraggingVSB = false;
                    IsDraggingHSB = false;
                };

                MouseHandler.Dragged += (sender, e) =>
                {
                    if (IsDraggingVSB)
                    {
                        e.SetHandled(this, false);
                        Point LayoutSpacePosition = ConvertCoordinateSpace(CoordinateSpace.Screen, CoordinateSpace.Layout, e.Position);
                        HandleScrollBarInput(Orientation.Vertical, e.Button, LayoutSpacePosition.Y);
                    }

                    if (IsDraggingHSB)
                    {
                        e.SetHandled(this, false);
                        Point LayoutSpacePosition = ConvertCoordinateSpace(CoordinateSpace.Screen, CoordinateSpace.Layout, e.Position);
                        HandleScrollBarInput(Orientation.Horizontal, e.Button, LayoutSpacePosition.X);
                    }
                };

                //  Pre-emptively handle mouse events if user was dragging a scrollbar so that the child content doesn't also react to the mouse event
                OnBeginUpdateContents += (sender, e) =>
                {
                    if (IsDraggingVSB || IsDraggingHSB)
                    {
                        MouseHandler.Tracker.CurrentButtonReleasedEvents[MouseButton.Left]?.SetHandledBy(this, false);
                        foreach (DragStartCondition StartCondition in MouseHandler.DragStartConditions)
                            MouseHandler.Tracker.CurrentDragStartEvents[StartCondition][MouseButton.Left]?.SetHandledBy(this, false);
                    }

                    if (IsHoveringVSB || IsHoveringHSB)
                    {
                        MouseHandler.Tracker.CurrentButtonPressedEvents[MouseButton.Left]?.SetHandledBy(this, false);
                    }
                };

                //  This probably isn't needed
                MouseHandler.ReleasedOutside += (sender, e) =>
                {
                    if (e.IsLMB && (IsDraggingVSB || IsDraggingHSB))
                        e.SetHandledBy(this, false);
                };

                MouseHandler.Scrolled += (sender, e) =>
                {
                    //  Attempt to scroll vertically
                    if (VSBBounds.HasValue)
                    {
                        if (e.ScrollWheelDelta > 0 && VerticalOffset > 0)
                        {
                            e.SetHandledBy(this, false);
                            VerticalOffset -= VerticalScrollInterval;
                        }
                        else if (e.ScrollWheelDelta < 0 && VerticalOffset < MaxVerticalOffset)
                        {
                            e.SetHandledBy(this, false);
                            VerticalOffset += VerticalScrollInterval;
                        }
                    }
                    //  Scroll horizontally if there is only a horizontal scrollbar but no vertical scrollbar
                    else if (HSBBounds.HasValue)
                    {
                        if (e.ScrollWheelDelta > 0 && HorizontalOffset > 0)
                        {
                            e.SetHandledBy(this, false);
                            HorizontalOffset -= VerticalScrollInterval;
                        }
                        else if (e.ScrollWheelDelta < 0 && HorizontalOffset < MaxHorizontalOffset)
                        {
                            e.SetHandledBy(this, false);
                            HorizontalOffset += VerticalScrollInterval;
                        }
                    }
                };
            }
        }

        protected override void UpdateContents(ElementUpdateArgs UA)
        {
            Point ScrollOffset = new((int)HorizontalOffset, (int)VerticalOffset);
            Point NewOffset = UA.Offset + ScrollOffset;
            base.UpdateContents(UA.ChangeOffset(NewOffset));
        }

        private void HandleScrollBarInput(Orientation ScrollBar, MouseButton Button, int CursorPosition)
        {
            if (Button == MouseButton.Left && HasContent)
            {
                Size ContentSize = Content.AllocatedBounds.Size.AsSize();

                if (ScrollBar == Orientation.Vertical && VSBVisibility != ScrollBarVisibility.Disabled)
                {
                    Rectangle ScrollBarBounds = PaddedVSBBounds.Value;

                    float PercentInCurrentViewport = ContentViewport.Height * 1.0f / ContentSize.Height;
                    float ScrollBarForegroundHeight = PercentInCurrentViewport * ScrollBarBounds.Height;

                    float MinValue = 0;
                    float MaxValue = ScrollBarBounds.Height - ScrollBarForegroundHeight;
                    float AdjustedPosition = Math.Clamp(CursorPosition - ScrollBarBounds.Top - ScrollBarForegroundHeight / 2, MinValue, MaxValue);

                    if (MaxValue == MinValue)
                        VerticalOffset = MaxValue;
                    else
                        VerticalOffset = (AdjustedPosition - MinValue) / (MaxValue - MinValue) * MaxVerticalOffset;
                }
                else if (ScrollBar == Orientation.Horizontal && HSBVisibility != ScrollBarVisibility.Disabled)
                {
                    Rectangle ScrollBarBounds = PaddedHSBBounds.Value;

                    float PercentInCurrentViewport = ContentViewport.Width * 1.0f / ContentSize.Width;
                    float ScrollBarForegroundWidth = PercentInCurrentViewport * ScrollBarBounds.Width;

                    float MinValue = 0;
                    float MaxValue = ScrollBarBounds.Width - ScrollBarForegroundWidth;
                    float AdjustedPosition = Math.Clamp(CursorPosition - ScrollBarBounds.Left - ScrollBarForegroundWidth / 2, MinValue, MaxValue);

                    if (MaxValue == MinValue)
                        VerticalOffset = MaxValue;
                    else
                        HorizontalOffset = (AdjustedPosition - MinValue) / (MaxValue - MinValue) * MaxHorizontalOffset;
                }
            }
        }

        protected override Thickness UpdateContentMeasurement(Size AvailableSize)
        {
            int ActualAvailableWidth = HSBVisibility == ScrollBarVisibility.Disabled ? AvailableSize.Width : int.MaxValue;
            int ActualAvailableHeight = VSBVisibility == ScrollBarVisibility.Disabled ? AvailableSize.Height : int.MaxValue;
            Size ActualAvailableSize = new(ActualAvailableWidth, ActualAvailableHeight);
            return base.UpdateContentMeasurement(ActualAvailableSize);
        }

        protected override void UpdateContentLayout(Rectangle Bounds)
        {
            if (Content != null)
            {
                int ActualAvailableWidth = HSBVisibility == ScrollBarVisibility.Disabled ? Bounds.Width : int.MaxValue;
                int ActualAvailableHeight = VSBVisibility == ScrollBarVisibility.Disabled ? Bounds.Height : int.MaxValue;
                Size ActualAvailableSize = new(ActualAvailableWidth, ActualAvailableHeight);

                int ActualContentWidth = Bounds.Width;
                int ActualContentHeight = Bounds.Height;
                if (HSBVisibility != ScrollBarVisibility.Disabled || VSBVisibility != ScrollBarVisibility.Disabled)
                {
                    Content.UpdateMeasurement(ActualAvailableSize, out _, out Thickness ContentSize, out _, out _);
                    ActualContentWidth = Math.Max(ActualContentWidth, ContentSize.Width);
                    ActualContentHeight = Math.Max(ActualContentHeight, ContentSize.Height);
                }

                Rectangle ActualBounds = new(Bounds.Left, Bounds.Top, ActualContentWidth, ActualContentHeight);
                Content.UpdateLayout(ActualBounds);
            }
            else
                base.UpdateContentLayout(Bounds);
        }

        public override Thickness MeasureSelfOverride(Size AvailableSize, out Thickness SharedSize)
        {
            //  Measure the content to see if we need to display scrollbars that are using ScrollBarVisibility.Auto
            Thickness RequestedContentSize = new(0);
            if (VSBVisibility == ScrollBarVisibility.Auto || HSBVisibility == ScrollBarVisibility.Auto)
            {
                if (HasContent)
                {
                    int ActualAvailableWidth = HSBVisibility == ScrollBarVisibility.Disabled ? AvailableSize.Width - HorizontalPadding : int.MaxValue;
                    int ActualAvailableHeight = VSBVisibility == ScrollBarVisibility.Disabled ? AvailableSize.Height - VerticalPadding : int.MaxValue;
                    Size ActualAvailableSize = new(ActualAvailableWidth, ActualAvailableHeight);
                    Content.UpdateMeasurement(ActualAvailableSize, out _, out RequestedContentSize, out _, out _);
                }
            }

            int ActualVSBWidth = VSBVisibility switch
            {
                ScrollBarVisibility.Disabled => 0,
                ScrollBarVisibility.Auto => RequestedContentSize.Height > AvailableSize.Height - VerticalPadding ? VSBWidth : 0,
                ScrollBarVisibility.Hidden => VSBWidth,
                ScrollBarVisibility.Visible => VSBWidth,
                ScrollBarVisibility.Collapsed => 0,
                _ => throw new NotImplementedException($"Unrecognized {nameof(ScrollBarVisibility)}: {VSBVisibility}"),
            };

            int ActualHSBHeight = HSBVisibility switch
            {
                ScrollBarVisibility.Disabled => 0,
                ScrollBarVisibility.Auto => RequestedContentSize.Width > AvailableSize.Width - HorizontalPadding ? HSBHeight : 0,
                ScrollBarVisibility.Hidden => HSBHeight,
                ScrollBarVisibility.Visible => HSBHeight,
                ScrollBarVisibility.Collapsed => 0,
                _ => throw new NotImplementedException($"Unrecognized {nameof(ScrollBarVisibility)}: {HSBVisibility}"),
            };

            SharedSize = new(0);
            return new(0, 0, ActualVSBWidth, ActualHSBHeight);
        }

        public override void DrawSelf(ElementDrawArgs DA, Rectangle LayoutBounds)
        {
            if (HasContent)
            {
                bool IsVSBRendered = VSBBounds.HasValue && VSBVisibility != ScrollBarVisibility.Hidden && VSBVisibility != ScrollBarVisibility.Collapsed;
                bool IsHSBRendered = HSBBounds.HasValue && HSBVisibility != ScrollBarVisibility.Hidden && HSBVisibility != ScrollBarVisibility.Collapsed;

                int MinSize = 8; // Minimum size of the inner rectangle of a ScrollBar
                Size ContentSize = Content.AllocatedBounds.Size.AsSize();

                if (IsVSBRendered)
                {
                    //  Calculate the coordinates of the inner rectangle of the scrollbar
                    Rectangle PaddedBounds = PaddedVSBBounds.Value;
                    float PercentInCurrentViewport = ContentViewport.Height * 1.0f / ContentSize.Height;
                    float PercentOutsideCurrentViewport = 1 - PercentInCurrentViewport;

                    float StartY;
                    if (MaxVerticalOffset.IsAlmostZero())
                        StartY = PaddedBounds.Top;
                    else
                        StartY = PaddedBounds.Top + PaddedBounds.Height * PercentOutsideCurrentViewport * VerticalOffset / MaxVerticalOffset;
                    float EndY = StartY + PercentInCurrentViewport * PaddedBounds.Height;

                    //  Validate that the inner rectangle is at least 8 pixels big
                    float Height = EndY - StartY + 1;
                    if (Height < MinSize)
                    {
                        //  Expand both ends by half of the difference
                        StartY -= (MinSize - Height) / 2;
                        EndY += (MinSize - Height) / 2;
                        Height = MinSize;

                        //  Ensure the Start/End are still within the outer bounds
                        if (StartY < PaddedBounds.Top)
                        {
                            EndY += PaddedBounds.Top - StartY;
                            StartY = PaddedBounds.Top;
                        }
                        if (EndY > PaddedBounds.Bottom)
                            EndY = PaddedBounds.Bottom;
                    }

                    PrimaryVisualState PrimaryState = IsVSBFocused ? PrimaryVisualState.Selected : VisualState.Primary;
                    SecondaryVisualState SecondaryState = IsDraggingVSB ? SecondaryVisualState.Pressed : IsHoveringVSB ? SecondaryVisualState.Hovered : SecondaryVisualState.None;

                    //  Draw the outer rectangle of the scrollbar
                    ScrollBarOuterBrush.GetUnderlay(PrimaryState)?.Draw(DA, this, VSBBounds.Value);
                    ScrollBarOuterBrush.GetFillOverlay(SecondaryState)?.Draw(DA, this, VSBBounds.Value);

                    //  Draw the inner rectangle of the scrollbar
                    Rectangle ScrollBarVisibleBounds = new(PaddedBounds.Left, (int)StartY, (PaddedBounds.Width), (int)(EndY - StartY + 1));
                    ScrollBarInnerBrush.GetUnderlay(PrimaryState)?.Draw(DA, this, ScrollBarVisibleBounds);
                    ScrollBarInnerBrush.GetFillOverlay(SecondaryState)?.Draw(DA, this, ScrollBarVisibleBounds);
                }

                if (IsHSBRendered)
                {
                    //  Calculate the coordinates of the inner rectangle of the scrollbar
                    Rectangle PaddedBounds = PaddedHSBBounds.Value;
                    float PercentInCurrentViewport = ContentViewport.Width * 1.0f / ContentSize.Width;
                    float PercentOutsideCurrentViewport = 1 - PercentInCurrentViewport;

                    float StartX;
                    if (MaxHorizontalOffset.IsAlmostZero())
                        StartX = PaddedBounds.Left;
                    else
                        StartX = PaddedBounds.Left + PaddedBounds.Width * PercentOutsideCurrentViewport * HorizontalOffset / MaxHorizontalOffset;
                    float EndX = StartX + PercentInCurrentViewport * PaddedBounds.Width;

                    //  Validate that the inner rectangle is at least 8 pixels big
                    float Width = EndX - StartX + 1;
                    if (Width < MinSize)
                    {
                        //  Expand both ends by half of the difference
                        StartX -= (MinSize - Width) / 2;
                        EndX += (MinSize - Width) / 2;
                        Width = MinSize;

                        //  Ensure the Start/End are still within the outer bounds
                        if (StartX < PaddedBounds.Left)
                        {
                            EndX += PaddedBounds.Left - StartX;
                            StartX = PaddedBounds.Left;
                        }
                        if (EndX > PaddedBounds.Right)
                            EndX = PaddedBounds.Right;
                    }

                    PrimaryVisualState PrimaryState = IsHSBFocused ? PrimaryVisualState.Selected : VisualState.Primary;
                    SecondaryVisualState SecondaryState = IsDraggingHSB ? SecondaryVisualState.Pressed : IsHoveringHSB ? SecondaryVisualState.Hovered : SecondaryVisualState.None;

                    //  Draw the outer rectangle of the scrollbar
                    ScrollBarOuterBrush.GetUnderlay(PrimaryState)?.Draw(DA, this, HSBBounds.Value);
                    ScrollBarOuterBrush.GetFillOverlay(SecondaryState)?.Draw(DA, this, HSBBounds.Value);

                    //  Draw the inner rectangle of the scrollbar
                    Rectangle ScrollBarVisibleBounds = new((int)StartX, PaddedBounds.Top, (int)(EndX - StartX + 1), PaddedBounds.Height);
                    ScrollBarInnerBrush.GetUnderlay(PrimaryState)?.Draw(DA, this, ScrollBarVisibleBounds);
                    ScrollBarInnerBrush.GetFillOverlay(SecondaryState)?.Draw(DA, this, ScrollBarVisibleBounds);
                }
            }
        }

        protected override void DrawContents(ElementDrawArgs DA)
        {
            Rectangle ScreenBounds = ConvertCoordinateSpace(CoordinateSpace.UnscaledScreen, CoordinateSpace.Screen, ContentViewport.GetTranslated(DA.Offset));
            using (DA.DT.SetClipTargetTemporary(ScreenBounds, true))
            {
                Point NewOffset = DA.Offset - new Point((int)HorizontalOffset, (int)VerticalOffset);
                base.DrawContents(DA with { Offset = NewOffset });
            }
        }
    }
}
