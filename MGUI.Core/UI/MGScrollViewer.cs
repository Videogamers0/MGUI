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
                }
            }
        }

        /// <summary>This property is the same as <see cref="HSBVisibility"/></summary>
        public ScrollBarVisibility HorizontalScrollBarVisibility
        {
            get => HSBVisibility;
            set => HSBVisibility = value;
        }

        public Rectangle ContentViewport { get; private set; }
        public Rectangle? VSBBounds { get; private set; }
        /// <summary>The screen bounds that the vertical scrollbar will be rendered to, after applying the <see cref="ScrollBarPadding"/></summary>
        public Rectangle? PaddedVSBBounds => VSBBounds?.GetCompressed(ScrollBarPadding);
        public Rectangle? HSBBounds { get; private set; }
        /// <summary>The screen bounds that the horizontal scrollbar will be rendered to, after applying the <see cref="ScrollBarPadding"/></summary>
        public Rectangle? PaddedHSBBounds => HSBBounds?.GetCompressed(ScrollBarPadding);

        #region Offset
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
                    _VerticalOffset = ClampedValue;
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private float _MaxVerticalOffset;
        /// <summary>The maximum value that <see cref="VerticalOffset"/> can be set to.<br/>
        /// Setting <see cref="VerticalOffset"/> to this value will scroll to the bottom of the scrollable content.</summary>
        private float MaxVerticalOffset
        {
            get => _MaxVerticalOffset;
            set
            {
                if (_MaxVerticalOffset != value)
                {
                    _MaxVerticalOffset = value;
                    VerticalOffset = Math.Clamp(VerticalOffset, 0, MaxVerticalOffset);
                }
            }
        }

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
                    _HorizontalOffset = ClampedValue;
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private float _MaxHorizontalOffset;
        /// <summary>The maximum value that <see cref="HorizontalOffset"/> can be set to.<br/>
        /// Setting <see cref="HorizontalOffset"/> to this value will scroll to the right-most of the scrollable content.</summary>
        private float MaxHorizontalOffset
        {
            get => _MaxHorizontalOffset;
            set
            {
                if (_MaxHorizontalOffset != value)
                {
                    _MaxHorizontalOffset = value;
                    HorizontalOffset = Math.Clamp(HorizontalOffset, 0, MaxHorizontalOffset);
                }
            }
        }
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

        /// <summary>The brush to use for the outer portion of the scrollbars.<br/>
        /// Default value: <see cref="MGTheme.ScrollBarOuterBrush"/><para/>
        /// See also: <see cref="MGDesktop.Theme"/></summary>
        public VisualStateFillBrush ScrollBarOuterBrush { get; set; }
        /// <summary>The brush to use for the inner portion of the scrollbars.<br/>
        /// Default value: <see cref="MGTheme.ScrollBarInnerBrush"/><para/>
        /// See also: <see cref="MGDesktop.Theme"/></summary>
        public VisualStateFillBrush ScrollBarInnerBrush { get; set; }

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
                            int ActualAvailableWidth = HSBVisibility == ScrollBarVisibility.Disabled ? ActualContentBounds.Width : int.MaxValue;
                            int ActualAvailableHeight = VSBVisibility == ScrollBarVisibility.Disabled ? ActualContentBounds.Height : int.MaxValue;
                            Size ActualAvailableSize = new(ActualAvailableWidth, ActualAvailableHeight);
                            Content.UpdateMeasurement(ActualAvailableSize, out _, out RequestedContentSize, out _, out _);
                        }
                    }

                    int ActualVSBWidth = VSBVisibility switch
                    {
                        ScrollBarVisibility.Disabled => 0,
                        ScrollBarVisibility.Auto => RequestedContentSize.Height > ActualContentBounds.Height ? VSBWidth : 0,
                        ScrollBarVisibility.Hidden => VSBWidth,
                        ScrollBarVisibility.Visible => VSBWidth,
                        ScrollBarVisibility.Collapsed => 0,
                        _ => throw new NotImplementedException($"Unrecognized {nameof(ScrollBarVisibility)}: {VSBVisibility}"),
                    };

                    int ActualHSBHeight = HSBVisibility switch
                    {
                        ScrollBarVisibility.Disabled => 0,
                        ScrollBarVisibility.Auto => RequestedContentSize.Width > ActualContentBounds.Width ? HSBHeight : 0,
                        ScrollBarVisibility.Hidden => HSBHeight,
                        ScrollBarVisibility.Visible => HSBHeight,
                        ScrollBarVisibility.Collapsed => 0,
                        _ => throw new NotImplementedException($"Unrecognized {nameof(ScrollBarVisibility)}: {HSBVisibility}"),
                    };

                    Size ScrollBarsSize = new(ActualVSBWidth, ActualHSBHeight);
                    Size ContentSize = Content?.AllocatedBounds.Size.AsSize() ?? ActualContentBounds.Size.AsSize();
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
                    IsHoveringVSB = !ParentWindow.HasModalWindow && VSBBounds.HasValue && VSBBounds.Value.ContainsInclusive(e.AdjustedCurrentPosition(this));
                    IsHoveringHSB = !ParentWindow.HasModalWindow && HSBBounds.HasValue && HSBBounds.Value.ContainsInclusive(e.AdjustedCurrentPosition(this));
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
                        e.SetHandled(this, false);
                        HandleScrollBarInput(Orientation.Vertical, e.Button, (int)e.AdjustedPosition(this).Y);
                    }

                    if (IsHoveringHSB)
                    {
                        e.SetHandled(this, false);
                        HandleScrollBarInput(Orientation.Horizontal, e.Button, (int)e.AdjustedPosition(this).X);
                    }
                };

                MouseHandler.DragStartCondition = DragStartCondition.MousePressed;

                MouseHandler.DragStart += (sender, e) =>
                {
                    if (IsHoveringVSB)
                    {
                        e.SetHandled(this, false);
                        IsDraggingVSB = true;
                    }

                    if (IsHoveringHSB)
                    {
                        e.SetHandled(this, false);
                        IsDraggingHSB = true;
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
                        HandleScrollBarInput(Orientation.Vertical, e.Button, (int)e.AdjustedPosition(this).Y);
                    }

                    if (IsDraggingHSB)
                    {
                        e.SetHandled(this, false);
                        HandleScrollBarInput(Orientation.Horizontal, e.Button, (int)e.AdjustedPosition(this).X);
                    }
                };

                //  Pre-emptively handle mouse release events if user was dragging a scrollbar
                //  so that the child content doesn't also react to the mouse release
                OnBeginUpdateContents += (sender, e) => // TODO should probably apply similar logic to anything that handles dragging, such as MGSlider
                {
                    if (IsDraggingVSB || IsDraggingHSB)
                    {
                        MouseHandler.Tracker.CurrentButtonReleasedEvents[MouseButton.Left]?.SetHandled(this, false);
                    }
                };

                //  This probably isn't needed
                MouseHandler.ReleasedOutside += (sender, e) =>
                {
                    if (e.IsLMB && (IsDraggingVSB || IsDraggingHSB))
                        e.SetHandled(this, false);
                };

                MouseHandler.Scrolled += (sender, e) =>
                {
                    if (e.ScrollWheelDelta > 0 && VerticalOffset > 0)
                    {
                        e.SetHandled(this, false);
                        VerticalOffset -= VerticalScrollInterval;
                    }
                    else if (e.ScrollWheelDelta < 0 && VerticalOffset < MaxVerticalOffset)
                    {
                        e.SetHandled(this, false);
                        VerticalOffset += VerticalScrollInterval;
                    }
                };
            }
        }

        protected override void UpdateContents(ElementUpdateArgs UA)
        {
            Point ScrollOffset = new((int)HorizontalOffset, (int)VerticalOffset);
            Point NewOffset = UA.Offset + ScrollOffset;
            base.UpdateContents(UA with { Offset = NewOffset });
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
                        if (StartY < PaddedBounds.Left)
                        {
                            EndY += PaddedBounds.Left - StartY;
                            StartY = PaddedBounds.Left;
                        }
                        if (EndY > PaddedBounds.Right)
                            EndY = PaddedBounds.Right;
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
            using (DA.DT.SetClipTargetTemporary(ContentViewport.GetTranslated(DA.Offset), true))
            {
                Point NewOffset = DA.Offset - new Point((int)HorizontalOffset, (int)VerticalOffset);
                base.DrawContents(DA with { Offset = NewOffset });
            }
        }
    }
}
