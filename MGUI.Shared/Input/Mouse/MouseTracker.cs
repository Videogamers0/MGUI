using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoGame.Extended;
using System.ComponentModel;
using System.Collections.ObjectModel;
using MGUI.Shared.Helpers;
using MGUI.Shared.Rendering;

namespace MGUI.Shared.Input.Mouse
{
    /// <summary>This class is just intended to be a convenient way to create a concrete implementation of <see cref="IMouseHandlerHost"/></summary>
    public class MouseHandlerHost : IMouseHandlerHost
    {
        private readonly Func<Rectangle> ComputeBounds;
        private readonly Func<RectangleF> ComputeBoundsF;
        private readonly Func<Vector2> ComputeOffset;

        public MouseHandlerHost(Func<Rectangle> ComputeBounds)
            : this(ComputeBounds, null) { }

        public MouseHandlerHost(Func<Rectangle> ComputeBounds, Func<Vector2> ComputeOffset)
        {
            this.ComputeBounds = ComputeBounds;
            this.ComputeBoundsF = null;
            this.ComputeOffset = ComputeOffset;
        }

        public MouseHandlerHost(Func<RectangleF> ComputeBounds)
            : this(ComputeBounds, null) { }

        public MouseHandlerHost(Func<RectangleF> ComputeBounds, Func<Vector2> ComputeOffset)
        {
            this.ComputeBounds = null;
            this.ComputeBoundsF = ComputeBounds;
            this.ComputeOffset = ComputeOffset;
        }

        Vector2 IMouseViewport.GetOffset() => ComputeOffset?.Invoke() ?? Vector2.Zero;
        bool IMouseViewport.IsInside(Vector2 Position) => ComputeBounds != null ? ComputeBounds().ContainsInclusive(Position) : ComputeBoundsF().ContainsInclusive(Position);
    }

    public interface IMouseViewport
    {
        public bool IsInside(Vector2 Position);
        /// <summary>Retrives an offset that should be added to the real mouse screen position before checking if the position is inside the viewport.<para/>
        /// For example, if a TextBlock is inside a ScrollViewer that is scrolled to VerticalOffset=100,<br/>
        /// the viewport Rectangle remains unchanged, but the real mouse position is offseted by +100 along the Y-axis before checking if the position is inside the viewport.</summary>
        public Vector2 GetOffset();
    }

    public interface IMouseHandlerHost : IMouseViewport
    {
        public bool CanReceiveMouseInput() => true;
    }

    public enum MouseButton
    {
        Left,
        Middle,
        Right
    }

    /// <summary>Detects changes to the mouse state between the previous and current Update ticks, but does not invoke the events. Instead, events are subscribed to and fired by <see cref="MouseHandler"/>.<br/>
    /// See: <see cref="MouseTracker.CreateHandler{T}(T, double?, bool, bool, bool)"/>, <see cref="MouseTracker.CreateHandler{T}(T, InputUpdatePriority, bool, bool)"/><para/>
    /// This class is automatically instantiated when creating an instance of <see cref="InputTracker"/>. See: <see cref="InputTracker.Mouse"/></summary>
    public class MouseTracker
    {
        public static readonly ReadOnlyCollection<MouseButton> MouseButtons = Enum.GetValues(typeof(MouseButton)).Cast<MouseButton>().ToList().AsReadOnly();

        /// <summary>The maximum number of pixels that the mouse can move by (in either the X or Y direction) while pressed before releasing for a Click event to be invoked.</summary>
        public int ClickPositionThreshold { get; set; } = 2;
        /// <summary>The maximum amount of time that can pass between a mouse button press and mouse button release to still be registed as a Click event</summary>
        public TimeSpan ClickTimeThreshold { get; set; } = TimeSpan.FromMilliseconds(300);

        /// <summary>The minimum number of pixels that the mouse must move by (in either the X or Y direction) while pressed before the a mouse drag event begins.<para/>
        /// Only used if <see cref="DragStartCondition"/> == <see cref="DragStartCondition.MouseMovedAfterPress"/></summary>
        public int DragThreshold { get; set; } = 3;

        public InputTracker InputTracker { get; }

        private List<MouseHandler> _Handlers { get; }
        /// <summary>To create a <see cref="MouseHandler"/>, use <see cref="CreateHandler{T}(T, double?, bool, bool, bool)"/> or <see cref="CreateHandler{T}(T, InputUpdatePriority, bool, bool)"/></summary>
        public IReadOnlyList<MouseHandler> Handlers => _Handlers;

        internal void RemoveHandler(MouseHandler Handler) => _Handlers.Remove(Handler);

        public MouseState PreviousState { get; private set; }
        public MouseState CurrentState { get; private set; }

        /// <summary>True if the mouse left button was just pressed during the current update.<para/>
        /// See also: <see cref="MouseLeftButtonReleasedRecently"/></summary>
        public bool MouseLeftButtonPressedRecently => _CurrentButtonPressedEvents[MouseButton.Left] != null;
        /// <summary>True if the mouse left button was just released during the current update.<para/>
        /// See also: <see cref="MouseLeftButtonPressedRecently"/></summary>
        public bool MouseLeftButtonReleasedRecently => _CurrentButtonReleasedEvents[MouseButton.Left] != null;

        /// <summary>True if the mouse position changed between the previous update and the current update.</summary>
        public bool MouseMovedRecently => CurrentState.Position != PreviousState.Position;

        internal MouseTracker(InputTracker InputTracker, int ClickPositionThreshold = 2, int DragThreshold = 3)
        {
            this.InputTracker = InputTracker;
            this._Handlers = new();
            this.ClickPositionThreshold = ClickPositionThreshold;
            this.DragThreshold = DragThreshold;
        }

        /// <param name="UpdatePriority">The priority with which the handler receives mouse events. A higher priority means this handler will have the first chance to receive and handle events.</param>
        /// <param name="AlwaysHandlesEvents">If true, the handler will always set <see cref="HandledByEventArgs{THandlerType}.HandledBy"/> to <paramref name="Owner"/> when receiving the mouse event.</param>
        /// <param name="InvokeEvenIfHandled">If true, the handler will still receive the mouse event even if <see cref="HandledByEventArgs{THandlerType}.IsHandled"/> is true.</param>
        public MouseHandler CreateHandler<T>(T Owner, InputUpdatePriority UpdatePriority, bool AlwaysHandlesEvents = false, bool InvokeEvenIfHandled = false)
            where T : IMouseHandlerHost => CreateHandler(Owner, (int)UpdatePriority, AlwaysHandlesEvents, InvokeEvenIfHandled);

        /// <param name="UpdatePriority">The priority with which the handler receives mouse events. A higher priority means this handler will have the first chance to receive and handle events.<para/>
        /// If null, the caller is expected to manually call <see cref="MouseHandler.ManualUpdate"/> themselves.<br/>
        /// If not null, <see cref="MouseHandler.AutoUpdate"/> will automatically be invoked when updating this <see cref="MouseTracker"/><para/>
        /// Minimum Value = 0. Maximum Value = 100. Recommended to use preset values from <see cref="InputUpdatePriority"/>.</param>
        /// <param name="AlwaysHandlesEvents">If true, the handler will always set <see cref="HandledByEventArgs{THandlerType}.HandledBy"/> to <paramref name="Owner"/> when receiving the mouse event.</param>
        /// <param name="InvokeEvenIfHandled">If true, the handler will still receive the mouse event even if <see cref="HandledByEventArgs{THandlerType}.IsHandled"/> is true.</param>
        /// <param name="InvokeIfHandledBySelf">If true, the handler will still receive the mouse event even if <see cref="HandledByEventArgs{THandlerType}.IsHandled"/> is true, 
        /// as long as the handler is the same as <see cref="MouseHandler.Owner"/></param>
        public MouseHandler CreateHandler<T>(T Owner, double? UpdatePriority, bool AlwaysHandlesEvents = false, bool InvokeEvenIfHandled = false, bool InvokeIfHandledBySelf = true)
            where T : IMouseHandlerHost
        {
            MouseHandler Handler = new(this, Owner, UpdatePriority, AlwaysHandlesEvents, InvokeEvenIfHandled, InvokeIfHandledBySelf);
            _Handlers.Add(Handler);
            return Handler;
        }

        public bool IsPressedInside(MouseButton Button, RectangleF Viewport)
            => IsPressedInside(Button, Viewport, Vector2.Zero);
        public bool IsPressedInside(MouseButton Button, RectangleF Viewport, Vector2 Offset)
            => _RecentButtonPressedEvents[Button] != null && Viewport.ContainsInclusive(_RecentButtonPressedEvents[Button].Position.ToVector2() + Offset);
        public bool IsPressedInside(MouseButton Button, IMouseHandlerHost Owner)
            => _RecentButtonPressedEvents[Button] != null && Owner.IsInside(_RecentButtonPressedEvents[Button].Position.ToVector2() + Owner.GetOffset());

        #region Events
        /// <summary>The mouse scroll event that occurred on the current Update tick, or null if the mouse wheel didn't change between the previous Update tick and the current Update tick.</summary>
        public BaseMouseScrolledEventArgs CurrentScrollEvent { get; private set; } = null;

        /// <summary>The mouse move event that occurred on the current Update tick, or null if the mouse position didn't change between the previous Update tick and the current Update tick.</summary>
        public BaseMouseMovedEventArgs CurrentMoveEvent { get; private set; } = null;

        /// <summary>The most recent MouseButton Press events that have occurred, even if they occurred on a prior Update tick. These values are set back to null when the button is released.</summary>
        private readonly Dictionary<MouseButton, BaseMousePressedEventArgs> _RecentButtonPressedEvents = new()
        {
            { MouseButton.Left, null },
            { MouseButton.Middle, null },
            { MouseButton.Right, null }
        };
        /// <summary>The most recent MouseButton Press events that have occurred, even if they occurred on a prior Update tick. These values are set back to null when the button is released.</summary>
        public IReadOnlyDictionary<MouseButton, BaseMousePressedEventArgs> RecentButtonPressedEvents => _RecentButtonPressedEvents;

        private readonly Dictionary<MouseButton, BaseMousePressedEventArgs> _CurrentButtonPressedEvents = new()
        {
            { MouseButton.Left, null },
            { MouseButton.Middle, null },
            { MouseButton.Right, null }
        };
        /// <summary>The MouseButton Press events that occurred on the current Update tick, or null if the button's <see cref="ButtonState"/> wasn't just set to <see cref="ButtonState.Pressed"/> on the current Update tick.</summary>
        public IReadOnlyDictionary<MouseButton, BaseMousePressedEventArgs> CurrentButtonPressedEvents => _CurrentButtonPressedEvents;
        internal bool HasCurrentButtonPressedEvents;

        private readonly Dictionary<MouseButton, BaseMouseReleasedEventArgs> _CurrentButtonReleasedEvents = new()
        {
            { MouseButton.Left, null },
            { MouseButton.Middle, null },
            { MouseButton.Right, null }
        };
        /// <summary>The MouseButton Release events that occurred on the current Update tick, or null if the button's <see cref="ButtonState"/> wasn't just set to <see cref="ButtonState.Released"/> on the current Update tick.</summary>
        public IReadOnlyDictionary<MouseButton, BaseMouseReleasedEventArgs> CurrentButtonReleasedEvents => _CurrentButtonReleasedEvents;
        internal bool HasCurrentButtonReleasedEvents;

        /// <summary>The MouseButton Click events that occurred on the current Update tick, or null if the button didn't just finish clicking on the current Update tick.</summary>
        private readonly Dictionary<MouseButton, BaseMouseClickedEventArgs> _CurrentButtonClickedEvents = new()
        {
            { MouseButton.Left, null },
            { MouseButton.Middle, null },
            { MouseButton.Right, null }
        };
        /// <summary>The MouseButton Click events that occurred on the current Update tick, or null if the button didn't just finish clicking on the current Update tick.</summary>
        public IReadOnlyDictionary<MouseButton, BaseMouseClickedEventArgs> CurrentButtonClickedEvents => _CurrentButtonClickedEvents;
        internal bool HasCurrentButtonClickedEvents;

        /// <summary>The most recent DragStart events that have occurred, even if they occurred on a prior Update tick. These values are set back to null when the DragEnd event is detected.</summary>
        private readonly Dictionary<DragStartCondition, Dictionary<MouseButton, BaseMouseDragStartEventArgs>> RecentDragStartEvents = new()
        {
            {
                DragStartCondition.MouseMovedAfterPress,
                new()
                {
                    { MouseButton.Left, null },
                    { MouseButton.Middle, null },
                    { MouseButton.Right, null }
                }
            },
            {
                DragStartCondition.MousePressed,
                new()
                {
                    { MouseButton.Left, null },
                    { MouseButton.Middle, null },
                    { MouseButton.Right, null }
                }
            }
        };

        private readonly Dictionary<DragStartCondition, Dictionary<MouseButton, BaseMouseDragStartEventArgs>> _CurrentDragStartEvents = new()
        {
            {
                DragStartCondition.MouseMovedAfterPress,
                new()
                {
                    { MouseButton.Left, null },
                    { MouseButton.Middle, null },
                    { MouseButton.Right, null }
                }
            },
            {
                DragStartCondition.MousePressed,
                new()
                {
                    { MouseButton.Left, null },
                    { MouseButton.Middle, null },
                    { MouseButton.Right, null }
                }
            }
        };
        /// <summary>The Mouse DragStart events that occurred on the current Update tick, or null if a DragStart didn't just occur the current Update tick.</summary>
        public IReadOnlyDictionary<DragStartCondition, Dictionary<MouseButton, BaseMouseDragStartEventArgs>> CurrentDragStartEvents => _CurrentDragStartEvents;
        internal bool HasCurrentDragStartEvents;

        private readonly Dictionary<DragStartCondition, Dictionary<MouseButton, BaseMouseDraggedEventArgs>> _CurrentDraggedEvents = new()
        {
            {
                DragStartCondition.MouseMovedAfterPress,
                new()
                {
                    { MouseButton.Left, null },
                    { MouseButton.Middle, null },
                    { MouseButton.Right, null }
                }
            },
            {
                DragStartCondition.MousePressed,
                new()
                {
                    { MouseButton.Left, null },
                    { MouseButton.Middle, null },
                    { MouseButton.Right, null }
                }
            }
        };
        /// <summary>The Mouse Dragged events that occurred on the current Update tick, or null if a Dragged event didn't just occur the current Update tick.</summary>
        public IReadOnlyDictionary<DragStartCondition, Dictionary<MouseButton, BaseMouseDraggedEventArgs>> CurrentDraggedEvents => _CurrentDraggedEvents;
        internal bool HasCurrentDraggedEvents;

        private readonly Dictionary<DragStartCondition, Dictionary<MouseButton, BaseMouseDragEndEventArgs>> _CurrentDragEndEvents = new()
        {
            {
                DragStartCondition.MouseMovedAfterPress,
                new()
                {
                    { MouseButton.Left, null },
                    { MouseButton.Middle, null },
                    { MouseButton.Right, null }
                }
            },
            {
                DragStartCondition.MousePressed,
                new()
                {
                    { MouseButton.Left, null },
                    { MouseButton.Middle, null },
                    { MouseButton.Right, null }
                }
            }
        };
        /// <summary>The Mouse DragEnd events that occurred on the current Update tick, or null if a DragEnd didn't just occur the current Update tick.</summary>
        public IReadOnlyDictionary<DragStartCondition, Dictionary<MouseButton, BaseMouseDragEndEventArgs>> CurrentDragEndEvents => _CurrentDragEndEvents;
        internal bool HasCurrentDragEndEvents;

        internal bool HasCurrentButtonEvents;
        internal bool HasCurrentDragEvents;
        #endregion Events

        public Point CurrentPosition { get; private set; }

        internal void Update(UpdateBaseArgs BA)
        {
            PreviousState = CurrentState;
            CurrentState = BA.MouseState;

            CurrentPosition = CurrentState.Position;

            //  Detect changes to the mouse wheel
            CurrentScrollEvent = null;
            if (PreviousState.ScrollWheelValue != CurrentState.ScrollWheelValue)
            {
                CurrentScrollEvent = new(this, CurrentState.ScrollWheelValue - PreviousState.ScrollWheelValue, CurrentPosition);
                //Debug.WriteLine($"Scrolled: {CurrentScrollEvent.ScrollWheelDelta} - {CurrentPosition}");
            }

            //  Detect changes to the mouse position
            CurrentMoveEvent = null;
            if (PreviousState.Position != CurrentState.Position)
            {
                CurrentMoveEvent = new(this, PreviousState.Position, CurrentState.Position);
                //Debug.WriteLine($"Moved: {PreviousState.Position} - {CurrentState.Position}");
            }

            //  Detect button press/release/click events
            foreach (MouseButton Button in MouseButtons)
            {
                _CurrentButtonPressedEvents[Button] = null;
                _CurrentButtonReleasedEvents[Button] = null;
                _CurrentButtonClickedEvents[Button] = null;
                _CurrentDragStartEvents[DragStartCondition.MousePressed][Button] = null;

                //  Detect buttons that were just pressed
                if (GetButtonState(PreviousState, Button) == ButtonState.Released && GetButtonState(CurrentState, Button) == ButtonState.Pressed)
                {
                    //Debug.WriteLine($"Pressed: {Button} - {CurrentPosition}");
                    BaseMousePressedEventArgs Args = new(this, Button, CurrentPosition);
                    _RecentButtonPressedEvents[Button] = Args;
                    _CurrentButtonPressedEvents[Button] = Args;

                    //  Detect DragStart events when Condition=MousePressed
                    bool IsDragging = RecentDragStartEvents[DragStartCondition.MousePressed][Button] != null;
                    if (!IsDragging)
                    {
                        BaseMouseDragStartEventArgs DragStartArgs = new(this, Button, CurrentPosition, DragStartCondition.MousePressed);
                        RecentDragStartEvents[DragStartCondition.MousePressed][Button] = DragStartArgs;
                        _CurrentDragStartEvents[DragStartCondition.MousePressed][Button] = DragStartArgs;
                    }
                }

                //  Detect buttons that were just released
                if (GetButtonState(PreviousState, Button) == ButtonState.Pressed && GetButtonState(CurrentState, Button) == ButtonState.Released &&
                    _RecentButtonPressedEvents.TryGetValue(Button, out BaseMousePressedEventArgs PressedArgs) && PressedArgs != null)
                {
                    //Debug.WriteLine($"Released: {Button} - {CurrentPosition}");
                    BaseMouseReleasedEventArgs ReleasedArgs = new(this, PressedArgs, Button, CurrentPosition);
                    _CurrentButtonReleasedEvents[Button] = ReleasedArgs;

                    //  Detect buttons that were just clicked
                    if (Math.Abs(PressedArgs.Position.X - ReleasedArgs.Position.X) <= ClickPositionThreshold && Math.Abs(PressedArgs.Position.Y - ReleasedArgs.Position.Y) <= ClickPositionThreshold &&
                        ReleasedArgs.HeldDuration <= ClickTimeThreshold)
                    {
                        BaseMouseClickedEventArgs ClickedArgs = new(this, ReleasedArgs, Button, CurrentPosition);
                        _CurrentButtonClickedEvents[Button] = ClickedArgs;
                    }

                    _RecentButtonPressedEvents[Button] = null;
                }
            }

            //  Detect mouse drag events
            foreach (DragStartCondition Condition in MouseHandler.DragStartConditions)
            {
                foreach (MouseButton Button in MouseButtons)
                {
                    if (Condition != DragStartCondition.MousePressed)
                        _CurrentDragStartEvents[Condition][Button] = null;
                    _CurrentDraggedEvents[Condition][Button] = null;
                    _CurrentDragEndEvents[Condition][Button] = null;

                    bool IsDragging = RecentDragStartEvents[Condition][Button] != null;
                    bool IsButtonPressed = _RecentButtonPressedEvents[Button] != null;

                    //  Check if a drag just began
                    if (CurrentMoveEvent != null && !IsDragging && IsButtonPressed)
                    {
                        BaseMousePressedEventArgs PressedArgs = _RecentButtonPressedEvents[Button];
                        if (Condition == DragStartCondition.MouseMovedAfterPress &&
                            (Math.Abs(CurrentPosition.X - PressedArgs.Position.X) >= DragThreshold || Math.Abs(CurrentPosition.Y - PressedArgs.Position.Y) >= DragThreshold))
                        {
                            //Debug.WriteLine($"Drag Start: {Button} - {PressedArgs.Position}");
                            BaseMouseDragStartEventArgs DragStartArgs = new(this, Button, PressedArgs.Position, Condition);
                            RecentDragStartEvents[Condition][Button] = DragStartArgs;
                            _CurrentDragStartEvents[Condition][Button] = DragStartArgs;
                        }
                    }
                    else
                    {
                        //  Check if a drag is in progress
                        if (CurrentMoveEvent != null && IsDragging)
                        {
                            BaseMouseDragStartEventArgs StartArgs = RecentDragStartEvents[Condition][Button];
                            //Debug.WriteLine($"Dragged: {Button} - {StartArgs.Position} - {CurrentPosition}");
                            BaseMouseDraggedEventArgs DraggedArgs = new(this, StartArgs, Button, CurrentPosition);
                            _CurrentDraggedEvents[Condition][Button] = DraggedArgs;
                        }

                        //  Check if a drag just ended
                        if (IsDragging && _CurrentButtonReleasedEvents[Button] != null)
                        {
                            BaseMouseDragStartEventArgs StartArgs = RecentDragStartEvents[Condition][Button];
                            //Debug.WriteLine($"Drag End: {Button} - {StartArgs.Position} - {CurrentPosition}");
                            BaseMouseDragEndEventArgs DragEndArgs = new(this, StartArgs, Button, CurrentPosition);
                            _CurrentDragEndEvents[Condition][Button] = DragEndArgs;
                            RecentDragStartEvents[Condition][Button] = null;
                            RecentDragStartEvents[Condition][Button] = null;
                        }
                    }
                }
            }

            HasCurrentButtonPressedEvents = _CurrentButtonPressedEvents.Any(x => x.Value != null);
            HasCurrentButtonReleasedEvents = _CurrentButtonReleasedEvents.Any(x => x.Value != null);
            HasCurrentButtonClickedEvents = _CurrentButtonClickedEvents.Any(x => x.Value != null);
            HasCurrentButtonEvents = HasCurrentButtonPressedEvents || HasCurrentButtonReleasedEvents || HasCurrentButtonClickedEvents;
            HasCurrentDragStartEvents = _CurrentDragStartEvents.Any(x => x.Value != null);
            HasCurrentDraggedEvents = _CurrentDraggedEvents.Any(x => x.Value != null);
            HasCurrentDragEndEvents = _CurrentDragEndEvents.Any(x => x.Value != null);
            HasCurrentDragEvents = HasCurrentDragStartEvents || HasCurrentDraggedEvents || HasCurrentDragEndEvents;
        }

        /// <summary>Should be invoked exactly once per Update tick.<para/>
        /// This method will invoke any pending mouse events on its <see cref="Handlers"/> where <see cref="MouseHandler.IsManualUpdate"/> is false.</summary>
        public void UpdateHandlers()
        {
            IOrderedEnumerable<MouseHandler> SortedHandlers = Handlers.Where(x => !x.IsManualUpdate).OrderByDescending(x => x.UpdatePriority.Value);
            foreach (IGrouping<double, MouseHandler> Group in SortedHandlers.GroupBy(x => x.UpdatePriority.Value).OrderByDescending(x => x.Key))
            {
                foreach (MouseHandler Handler in Group)
                {
                    Handler.AutoUpdate();
                }
            }
        }

        public static ButtonState GetButtonState(MouseState State, MouseButton Button) => Button switch
        {
            MouseButton.Left => State.LeftButton,
            MouseButton.Middle => State.MiddleButton,
            MouseButton.Right => State.RightButton,
            _ => throw new NotImplementedException($"Unrecognized {nameof(MouseButton)}: {Button}")
        };
    }
}
