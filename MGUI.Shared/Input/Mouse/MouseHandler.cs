using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Shared.Input.Mouse
{
    public enum DragStartCondition
    {
        /// <summary>The <see cref="MouseHandler.DragStart"/> event will be invoked immediately after the mouse has been pressed inside the viewport.</summary>
        MousePressed,
        /// <summary>The <see cref="MouseHandler.DragStart"/> event will be invoked after the mouse has moved by at least <see cref="MouseTracker.DragThreshold"/> number of pixels, after being pressed inside the viewport.</summary>
        MouseMovedAfterPress,
        /// <summary>The <see cref="MouseHandler.DragStart"/> event will be invoked immediately after the mouse has been pressed inside the viewport<br/>
        /// AND again after the mouse has moved by at least <see cref="MouseTracker.DragThreshold"/> number of pixels, after being pressed inside the viewport.<para/>
        /// Highly recommended to avoid using this value, only intended for specialized use-cases.</summary>
        Both
    }

    /// <summary>Exposes several mouse-related events that you can subscribe and respond to, such as <see cref="MouseHandler.Entered"/>, <see cref="MouseHandler.Dragged"/>, <see cref="MouseHandler.LMBPressedInside"/> etc.<para/>
    /// This class is instantiated via: <see cref="MouseTracker.CreateHandler{T}(T, double?, bool, bool, bool)"/></summary>
    public class MouseHandler
    {
        /// <summary>Only includes distinct values. Does not include combined values such as <see cref="DragStartCondition.Both"/></summary>
        public static readonly IReadOnlyList<DragStartCondition> DragStartConditions = Enum.GetValues(typeof(DragStartCondition)).Cast<DragStartCondition>().Where(x => x != DragStartCondition.Both).ToList();
        public static readonly ReadOnlyCollection<MouseButton> MouseButtons = Enum.GetValues(typeof(MouseButton)).Cast<MouseButton>().ToList().AsReadOnly();

        public MouseTracker Tracker { get; }
        public IMouseHandlerHost Owner { get; }
        public double? UpdatePriority { get; }
        public bool IsManualUpdate => !UpdatePriority.HasValue;

        /// <summary>If true, <see cref="HandledByEventArgs{THandlerType}.HandledBy"/> will always be set to <see cref="Owner"/> after invoking an event.</summary>
        private bool AlwaysHandlesEvents { get; }
        /// <summary>If true, events will still be invoked even if <see cref="HandledByEventArgs{THandlerType}.IsHandled"/> is true.</summary>
        private bool InvokeEvenIfHandled { get; }
        /// <summary>If true, events will still be invoked even if <see cref="HandledByEventArgs{THandlerType}.IsHandled"/> is true, 
        /// as long as the handler is the same as <see cref="Owner"/></summary>
        private bool InvokeIfHandledBySelf { get; }

        public DragStartCondition DragStartCondition { get; set; } = DragStartCondition.MouseMovedAfterPress;

        public override string ToString() => $"{nameof(MouseHandler)}: {nameof(Owner)} = {Owner}";

        internal MouseHandler(MouseTracker Tracker, IMouseHandlerHost Owner, double? UpdatePriority, bool AlwaysHandlesEvents, bool InvokeEvenIfHandled, bool InvokeIfHandledBySelf)
        {
            this.Tracker = Tracker;
            this.Owner = Owner;
            this.UpdatePriority = UpdatePriority;
            this.AlwaysHandlesEvents = AlwaysHandlesEvents;
            this.InvokeEvenIfHandled = InvokeEvenIfHandled;
            this.InvokeIfHandledBySelf = InvokeIfHandledBySelf;
        }

        private bool IsInside(Vector2 Position) => IsInside(Position, Owner.GetOffset());
        private bool IsInside(Point Position, Vector2 Offset) => IsInside(Position.ToVector2(), Offset);
        private bool IsInside(Vector2 Position, Vector2 Offset) => Owner.IsInside(Position + Offset);

        /// <summary>True if the mouse is currently inside the viewport</summary>
        public bool IsHovered => IsInside(Tracker.CurrentState.Position.ToVector2(), Owner.GetOffset());

        public bool IsButtonPressedInside(MouseButton Button) => Tracker.IsPressedInside(Button, Owner);

        #region Events
        /// <summary>Invoked when the mouse wheel is scrolled overtop of the <see cref="Owner"/>'s viewport.</summary>
        public event EventHandler<BaseMouseScrolledEventArgs> Scrolled;

        #region Movement
        /// <summary>Invoked when the mouse moves to a different position, and that position is inside of the <see cref="Owner"/>'s viewport.</summary>
        public event EventHandler<BaseMouseMovedEventArgs> MovedInside;
        /// <summary>Invoked when the mouse moves to a different position, and that position is outside of the <see cref="Owner"/>'s viewport.</summary>
        public event EventHandler<BaseMouseMovedEventArgs> MovedOutside;
        /// <summary>Invoked when the mouse enters the <see cref="Owner"/>'s viewport.</summary>
        public event EventHandler<BaseMouseMovedEventArgs> Entered;
        /// <summary>Invoked when the mouse exits the <see cref="Owner"/>'s viewport.</summary>
        public event EventHandler<BaseMouseMovedEventArgs> Exited;

        private IEnumerable<EventHandler<BaseMouseMovedEventArgs>> MovedEvents
        {
            get
            {
                yield return MovedInside;
                yield return MovedOutside;
                yield return Entered;
                yield return Exited;
            }
        }
        #endregion Movement

        #region Pressed
        /// <summary>Invoked when any <see cref="MouseButton"/> is pressed while the mouse is currently inside the <see cref="Owner"/>'s viewport.</summary>
        public event EventHandler<BaseMousePressedEventArgs> PressedInside;
        /// <summary>Invoked when <see cref="MouseButton.Left"/> is pressed while the mouse is currently inside the <see cref="Owner"/>'s viewport.</summary>
        public event EventHandler<BaseMousePressedEventArgs> LMBPressedInside;
        /// <summary>Invoked when <see cref="MouseButton.Middle"/> is pressed while the mouse is currently inside the <see cref="Owner"/>'s viewport.</summary>
        public event EventHandler<BaseMousePressedEventArgs> MMBPressedInside;
        /// <summary>Invoked when <see cref="MouseButton.Right"/> is pressed while the mouse is currently inside the <see cref="Owner"/>'s viewport.</summary>
        public event EventHandler<BaseMousePressedEventArgs> RMBPressedInside;

        /// <summary>Invoked when any <see cref="MouseButton"/> is pressed while the mouse is currently outside of the <see cref="Owner"/>'s viewport.</summary>
        public event EventHandler<BaseMousePressedEventArgs> PressedOutside;

        private IEnumerable<EventHandler<BaseMousePressedEventArgs>> PressedEvents
        {
            get
            {
                yield return PressedInside;
                yield return LMBPressedInside;
                yield return MMBPressedInside;
                yield return RMBPressedInside;
                yield return PressedOutside;
            }
        }
        #endregion Pressed

        #region Released
        /// <summary>Invoked when any <see cref="MouseButton"/> is released while the mouse is currently inside the <see cref="Owner"/>'s viewport.</summary>
        public event EventHandler<BaseMouseReleasedEventArgs> ReleasedInside;
        /// <summary>Invoked when <see cref="MouseButton.Left"/> is released while the mouse is currently inside the <see cref="Owner"/>'s viewport.</summary>
        public event EventHandler<BaseMouseReleasedEventArgs> LMBReleasedInside;
        /// <summary>Invoked when <see cref="MouseButton.Middle"/> is released while the mouse is currently inside the <see cref="Owner"/>'s viewport.</summary>
        public event EventHandler<BaseMouseReleasedEventArgs> MMBReleasedInside;
        /// <summary>Invoked when <see cref="MouseButton.Right"/> is released while the mouse is currently inside the <see cref="Owner"/>'s viewport.</summary>
        public event EventHandler<BaseMouseReleasedEventArgs> RMBReleasedInside;

        /// <summary>Invoked when any <see cref="MouseButton"/> is released while the mouse is currently outside of the <see cref="Owner"/>'s viewport.</summary>
        public event EventHandler<BaseMouseReleasedEventArgs> ReleasedOutside;

        private IEnumerable<EventHandler<BaseMouseReleasedEventArgs>> ReleasedEvents
        {
            get
            {
                yield return ReleasedInside;
                yield return LMBReleasedInside;
                yield return MMBReleasedInside;
                yield return RMBReleasedInside;
                yield return ReleasedOutside;
            }
        }
        #endregion Released

        #region Clicked
        /// <summary>Invoked when any <see cref="MouseButton"/> is clicked while the mouse is currently inside the <see cref="Owner"/>'s viewport.</summary>
        public event EventHandler<BaseMouseClickedEventArgs> ClickedInside;
        /// <summary>Invoked when <see cref="MouseButton.Left"/> is clicked while the mouse is currently inside the <see cref="Owner"/>'s viewport.</summary>
        public event EventHandler<BaseMouseClickedEventArgs> LMBClickedInside;
        /// <summary>Invoked when <see cref="MouseButton.Middle"/> is clicked while the mouse is currently inside the <see cref="Owner"/>'s viewport.</summary>
        public event EventHandler<BaseMouseClickedEventArgs> MMBClickedInside;
        /// <summary>Invoked when <see cref="MouseButton.Right"/> is clicked while the mouse is currently inside the <see cref="Owner"/>'s viewport.</summary>
        public event EventHandler<BaseMouseClickedEventArgs> RMBClickedInside;

        /// <summary>Invoked when any <see cref="MouseButton"/> is clicked while the mouse is currently outside of the <see cref="Owner"/>'s viewport.</summary>
        public event EventHandler<BaseMouseClickedEventArgs> ClickedOutside;

        private IEnumerable<EventHandler<BaseMouseClickedEventArgs>> ClickedEvents
        {
            get
            {
                yield return ClickedInside;
                yield return LMBClickedInside;
                yield return MMBClickedInside;
                yield return RMBClickedInside;
                yield return ClickedOutside;
            }
        }
        #endregion Clicked

        #region Drag
        /// <summary>Invoked when any <see cref="MouseButton"/> is pressed while the mouse is currently inside the <see cref="Owner"/>'s viewport.<br/>
        /// If <see cref="DragStartCondition"/> is <see cref="DragStartCondition.MouseMovedAfterPress"/>, then will also verify that the mouse has moved by at least <see cref="MouseTracker.DragThreshold"/> while pressed before invoking the event.</summary>
        public event EventHandler<BaseMouseDragStartEventArgs> DragStart;

        /// <summary>Invoked when any <see cref="MouseButton"/> is pressed while the mouse is currently outside the <see cref="Owner"/>'s viewport.<br/>
        /// If <see cref="DragStartCondition"/> is <see cref="DragStartCondition.MouseMovedAfterPress"/>, then will also verify that the mouse has moved by at least <see cref="MouseTracker.DragThreshold"/> while pressed before invoking the event.<para/>
        /// You should probably subscribe to <see cref="DragStart"/> instead.</summary>
        public event EventHandler<BaseMouseDragStartEventArgs> DragStartOutside;

        /// <summary>Invoked when <see cref="DragStart"/> is already in progress, and then the mouse moves.</summary>
        public event EventHandler<BaseMouseDraggedEventArgs> Dragged;

        /// <summary>Invoked when <see cref="DragStart"/> is currently in progress, and then the pressed <see cref="MouseButton"/> is released.</summary>
        public event EventHandler<BaseMouseDragEndEventArgs> DragEnd;
        #endregion Drag

        /// <summary>True if at least one event has been subscribed to.<para/>
        /// If false, the Update method may return early under the assumption that no further processing is needed.</summary>
        private bool HasSubscribedEvents => IsMonitoringScroll || IsMonitoringMovement || IsMonitoringClicks || IsMonitoringDrag;
        /// <summary>True if at least one scroll event has been subscribed to</summary>
        private bool IsMonitoringScroll => Scrolled != null;
        /// <summary>True if at least one move event has been subscribed to</summary>
        private bool IsMonitoringMovement => MovedEvents.Any(x => x != null) || IsMonitoringDrag;
        /// <summary>True if at least one press/release/click event has been subscribed to</summary>
        private bool IsMonitoringClicks => PressedEvents.Any(x => x != null) || ReleasedEvents.Any(x => x != null) || ClickedEvents.Any(x => x != null);
        /// <summary>True if at least one drag event has been subscribed to</summary>
        private bool IsMonitoringDrag => DragStart != null || Dragged != null || DragEnd != null;
        #endregion Events

        /// <summary>Should only be invoked via <see cref="MouseTracker.UpdateHandlers"/>></summary>
        internal void AutoUpdate()
        {
            if (IsManualUpdate)
                throw new InvalidOperationException($"{nameof(MouseHandler)}.{nameof(AutoUpdate)} should only be invoked on {nameof(MouseHandler)}s where {nameof(IsManualUpdate)} is false.");
            InvokeQueuedEvents();
        }

        /// <summary>Should be invoked exactly once per Update tick, but only on <see cref="MouseHandler"/>s where <see cref="IsManualUpdate"/> is true.<para/>
        /// This method will invoke any pending mouse events.</summary>
        public void ManualUpdate()
        {
            if (!IsManualUpdate)
                throw new InvalidOperationException($"{nameof(MouseHandler)}.{nameof(ManualUpdate)} should only be invoked on {nameof(MouseHandler)}s where {nameof(IsManualUpdate)} is true.");
            InvokeQueuedEvents();
        }

        private void InvokeQueuedEvents()
        {
            if (IsValid && HasSubscribedEvents && Owner.CanReceiveMouseInput())
            {
                Vector2 Offset = Owner.GetOffset();

                //  Invoke Scrolled event
                if (Tracker.CurrentScrollEvent != null && Scrolled != null && IsInside(Tracker.CurrentScrollEvent.Position, Offset))
                {
                    if (InvokeEvenIfHandled || !Tracker.CurrentScrollEvent.IsHandled)
                    {
                        Scrolled.Invoke(this, Tracker.CurrentScrollEvent);
                        if (AlwaysHandlesEvents)
                            Tracker.CurrentScrollEvent.SetHandledBy(Owner, false);
                    }
                }

                //  Invoke mouse moved events
                if (Tracker.CurrentMoveEvent != null && IsMonitoringMovement)
                {
                    bool WasHovering = IsInside(Tracker.CurrentMoveEvent.PreviousPosition, Offset);
                    bool IsHovering = IsInside(Tracker.CurrentMoveEvent.CurrentPosition, Offset);

                    if (IsHovering)
                        MovedInside?.Invoke(this, Tracker.CurrentMoveEvent);
                    if (!IsHovering)
                        MovedOutside?.Invoke(this, Tracker.CurrentMoveEvent);

                    if (!WasHovering && IsHovering)
                        Entered?.Invoke(this, Tracker.CurrentMoveEvent);
                    if (WasHovering && !IsHovering)
                        Exited?.Invoke(this, Tracker.CurrentMoveEvent);
                }

                if (Tracker.HasCurrentButtonEvents && IsMonitoringClicks)
                {
                    //  Invoke mouse pressed events
                    if (Tracker.HasCurrentButtonPressedEvents && PressedEvents.Any(x => x != null))
                    {
                        foreach (MouseButton Button in MouseButtons)
                        {
                            BaseMousePressedEventArgs Args = Tracker.CurrentButtonPressedEvents[Button];
                            if (Args != null)
                            {
                                bool IsPressedInside = IsInside(Args.Position, Offset);
                                if (IsPressedInside)
                                {
                                    if ((InvokeEvenIfHandled || !Args.IsHandled || (InvokeIfHandledBySelf && Args.HandledBy == Owner)) && PressedInside != null)
                                    {
                                        PressedInside.Invoke(this, Args);
                                        if (AlwaysHandlesEvents)
                                            Args.SetHandledBy(Owner, false);
                                    }

                                    if (InvokeEvenIfHandled || !Args.IsHandled || (InvokeIfHandledBySelf && Args.HandledBy == Owner))
                                    {
                                        switch (Button)
                                        {
                                            case MouseButton.Left:
                                                if (LMBPressedInside != null)
                                                {
                                                    LMBPressedInside.Invoke(this, Args);
                                                    if (AlwaysHandlesEvents)
                                                        Args.SetHandledBy(Owner, false);
                                                }
                                                break;
                                            case MouseButton.Middle:
                                                if (MMBPressedInside != null)
                                                {
                                                    MMBPressedInside.Invoke(this, Args);
                                                    if (AlwaysHandlesEvents)
                                                        Args.SetHandledBy(Owner, false);
                                                }
                                                break;
                                            case MouseButton.Right:
                                                if (RMBPressedInside != null)
                                                {
                                                    RMBPressedInside.Invoke(this, Args);
                                                    if (AlwaysHandlesEvents)
                                                        Args.SetHandledBy(Owner, false);
                                                }
                                                break;
                                        }
                                    }
                                }
                                else
                                {
                                    if ((InvokeEvenIfHandled || !Args.IsHandled || (InvokeIfHandledBySelf && Args.HandledBy == Owner)) && PressedOutside != null)
                                    {
                                        PressedOutside.Invoke(this, Args);
                                        if (AlwaysHandlesEvents)
                                            Args.SetHandledBy(Owner, false);
                                    }
                                }
                            }
                        }
                    }

                    //  Invoke mouse released events
                    if (Tracker.HasCurrentButtonReleasedEvents && ReleasedEvents.Any(x => x != null))
                    {
                        foreach (MouseButton Button in MouseButtons)
                        {
                            BaseMouseReleasedEventArgs Args = Tracker.CurrentButtonReleasedEvents[Button];
                            if (Args != null)
                            {
                                bool IsReleasedInside = IsInside(Args.Position, Offset);
                                if (IsReleasedInside)
                                {
                                    if ((InvokeEvenIfHandled || !Args.IsHandled || (InvokeIfHandledBySelf && Args.HandledBy == Owner)) && ReleasedInside != null)
                                    {
                                        ReleasedInside.Invoke(this, Args);
                                        if (AlwaysHandlesEvents)
                                            Args.SetHandledBy(Owner, false);
                                    }

                                    if (InvokeEvenIfHandled || !Args.IsHandled || (InvokeIfHandledBySelf && Args.HandledBy == Owner))
                                    {
                                        switch (Button)
                                        {
                                            case MouseButton.Left:
                                                if (LMBReleasedInside != null)
                                                {
                                                    LMBReleasedInside.Invoke(this, Args);
                                                    if (AlwaysHandlesEvents)
                                                        Args.SetHandledBy(Owner, false);
                                                }
                                                break;
                                            case MouseButton.Middle:
                                                if (MMBReleasedInside != null)
                                                {
                                                    MMBReleasedInside.Invoke(this, Args);
                                                    if (AlwaysHandlesEvents)
                                                        Args.SetHandledBy(Owner, false);
                                                }
                                                break;
                                            case MouseButton.Right:
                                                if (RMBReleasedInside != null)
                                                {
                                                    RMBReleasedInside.Invoke(this, Args);
                                                    if (AlwaysHandlesEvents)
                                                        Args.SetHandledBy(Owner, false);
                                                }
                                                break;
                                        }
                                    }
                                }
                                else
                                {
                                    if ((InvokeEvenIfHandled || !Args.IsHandled || (InvokeIfHandledBySelf && Args.HandledBy == Owner)) && ReleasedOutside != null)
                                    {
                                        ReleasedOutside.Invoke(this, Args);
                                        if (AlwaysHandlesEvents)
                                            Args.SetHandledBy(Owner, false);
                                    }
                                }
                            }
                        }
                    }

                    //  Invoke mouse clicked events
                    if (Tracker.HasCurrentButtonClickedEvents && ClickedEvents.Any(x => x != null))
                    {
                        foreach (MouseButton Button in MouseButtons)
                        {
                            BaseMouseClickedEventArgs Args = Tracker.CurrentButtonClickedEvents[Button];
                            if (Args != null)
                            {
                                bool IsClickedInside = IsInside(Args.Position, Offset);
                                if (IsClickedInside)
                                {
                                    if ((InvokeEvenIfHandled || !Args.IsHandled || (InvokeIfHandledBySelf && Args.HandledBy == Owner)) && (InvokeEvenIfHandled || !Args.ReleasedArgs.IsHandled || Args.ReleasedArgs.HandledBy == Owner) && ClickedInside != null)
                                    {
                                        ClickedInside.Invoke(this, Args);
                                        if (AlwaysHandlesEvents)
                                            Args.SetHandledBy(Owner, false);
                                    }

                                    if ((InvokeEvenIfHandled || !Args.IsHandled || (InvokeIfHandledBySelf && Args.HandledBy == Owner)) && (InvokeEvenIfHandled || !Args.ReleasedArgs.IsHandled || Args.ReleasedArgs.HandledBy == Owner))
                                    {
                                        switch (Button)
                                        {
                                            case MouseButton.Left:
                                                if (LMBClickedInside != null)
                                                {
                                                    LMBClickedInside.Invoke(this, Args);
                                                    if (AlwaysHandlesEvents)
                                                        Args.SetHandledBy(Owner, false);
                                                }
                                                break;
                                            case MouseButton.Middle:
                                                if (MMBClickedInside != null)
                                                {
                                                    MMBClickedInside.Invoke(this, Args);
                                                    if (AlwaysHandlesEvents)
                                                        Args.SetHandledBy(Owner, false);
                                                }
                                                break;
                                            case MouseButton.Right:
                                                if (RMBClickedInside != null)
                                                {
                                                    RMBClickedInside.Invoke(this, Args);
                                                    if (AlwaysHandlesEvents)
                                                        Args.SetHandledBy(Owner, false);
                                                }
                                                break;
                                        }
                                    }
                                }
                                else
                                {
                                    if ((InvokeEvenIfHandled || !Args.IsHandled || (InvokeIfHandledBySelf && Args.HandledBy == Owner)) && (InvokeEvenIfHandled || !Args.ReleasedArgs.IsHandled || Args.ReleasedArgs.HandledBy == Owner) && ClickedOutside != null)
                                    {
                                        ClickedOutside.Invoke(this, Args);
                                        if (AlwaysHandlesEvents)
                                            Args.SetHandledBy(Owner, false);
                                    }
                                }
                            }
                        }
                    }
                }

                //  Invoke mouse drag events
                if (Tracker.HasCurrentDragEvents && IsMonitoringDrag)
                {
                    List<DragStartCondition> Conditions = new();
                    if (DragStartCondition == DragStartCondition.Both)
                    {
                        Conditions.Add(DragStartCondition.MouseMovedAfterPress);
                        Conditions.Add(DragStartCondition.MousePressed);
                    }
                    else
                    {
                        Conditions.Add(DragStartCondition);
                    }

                    foreach (DragStartCondition Condition in Conditions)
                    {
                        foreach (MouseButton Button in MouseButtons)
                        {
                            //  Drag started
                            if (DragStart != null || DragStartOutside != null)
                            {
                                BaseMouseDragStartEventArgs DragStartPressed = Tracker.CurrentDragStartEvents[DragStartCondition.MousePressed][Button];
                                if (DragStartPressed != null && Condition == DragStartCondition.MousePressed && (InvokeEvenIfHandled || !DragStartPressed.IsHandled || (InvokeIfHandledBySelf && DragStartPressed.HandledBy == Owner)))
                                {
                                    bool IsMouseInsideViewport = IsInside(DragStartPressed.Position, Offset);

                                    if (IsMouseInsideViewport && DragStart != null)
                                    {
                                        DragStart.Invoke(this, DragStartPressed);
                                        if (AlwaysHandlesEvents)
                                            DragStartPressed.SetHandledBy(Owner, false);
                                    }
                                    else if (!IsMouseInsideViewport && DragStartOutside != null)
                                    {
                                        DragStartOutside.Invoke(this, DragStartPressed);
                                        if (AlwaysHandlesEvents)
                                            DragStartPressed.SetHandledBy(Owner, false);
                                    }
                                }

                                BaseMouseDragStartEventArgs DragStartMovedAfterPress = Tracker.CurrentDragStartEvents[DragStartCondition.MouseMovedAfterPress][Button];
                                if (DragStartMovedAfterPress != null && Condition == DragStartCondition.MouseMovedAfterPress && (InvokeEvenIfHandled || !DragStartMovedAfterPress.IsHandled || (InvokeIfHandledBySelf && DragStartMovedAfterPress.HandledBy == Owner)))
                                {
                                    bool IsMouseInsideViewport = IsInside(DragStartMovedAfterPress.Position, Offset);

                                    if (IsMouseInsideViewport && DragStart != null)
                                    {
                                        DragStart.Invoke(this, DragStartMovedAfterPress);
                                        if (AlwaysHandlesEvents)
                                            DragStartMovedAfterPress.SetHandledBy(Owner, false);
                                    }
                                    else if (!IsMouseInsideViewport && DragStartOutside != null)
                                    {
                                        DragStartOutside.Invoke(this, DragStartMovedAfterPress);
                                        if (AlwaysHandlesEvents)
                                            DragStartMovedAfterPress.SetHandledBy(Owner, false);
                                    }
                                }
                            }

                            //  Dragged
                            BaseMouseDraggedEventArgs DraggedArgs = Tracker.CurrentDraggedEvents[Condition][Button];
                            if (DraggedArgs != null && Dragged != null && (IsInside(DraggedArgs.StartPosition, Offset) || DraggedArgs.DragStartArgs.HandledBy == Owner)
                                && (InvokeEvenIfHandled || !DraggedArgs.DragStartArgs.IsHandled || DraggedArgs.DragStartArgs.HandledBy == Owner))
                            {
                                Dragged.Invoke(this, DraggedArgs);
                                if (AlwaysHandlesEvents)
                                    DraggedArgs.SetHandled(Owner, false);
                            }

                            //  Drag ended
                            BaseMouseDragEndEventArgs DragEndArgs = Tracker.CurrentDragEndEvents[Condition][Button];
                            if (DragEndArgs != null && DragEnd != null && (IsInside(DragEndArgs.StartPosition, Offset) || DragEndArgs.DragStartArgs.HandledBy == Owner)
                                && (InvokeEvenIfHandled || !DragEndArgs.DragStartArgs.IsHandled || DragEndArgs.DragStartArgs.HandledBy == Owner))
                            {
                                DragEnd.Invoke(this, DragEndArgs);
                                if (AlwaysHandlesEvents)
                                    DragEndArgs.SetHandled(Owner, false);
                            }
                        }
                    }
                }
            }
        }

        private bool IsValid = true;
        /// <summary>Permanently invalidates this <see cref="MouseHandler"/> so that it will not receive and invoke any further mouse-related events.</summary>
        public void Unsubscribe()
        {
            Tracker.RemoveHandler(this);
            IsValid = false;
        }
    }
}
