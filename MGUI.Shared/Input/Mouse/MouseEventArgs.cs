using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Shared.Input.Mouse
{
    public class BaseMouseScrolledEventArgs : HandledByEventArgs<IMouseHandlerHost>
    {
        public MouseTracker Tracker { get; }

        public int ScrollWheelDelta { get; }
        public Point Position { get; }
        public Vector2 AdjustedPosition(IMouseHandlerHost Handler) => Position.ToVector2() + Handler.GetOffset();

        public BaseMouseScrolledEventArgs(MouseTracker Tracker, int ScrollWheelDelta, Point Position)
            : base()
        {
            this.Tracker = Tracker;
            this.ScrollWheelDelta = ScrollWheelDelta;
            this.Position = Position;
        }
    }

    public class BaseMouseMovedEventArgs : EventArgs
    {
        public MouseTracker Tracker { get; }

        public Point PreviousPosition { get; }
        public Point CurrentPosition { get; }
        public Point PositionDelta => CurrentPosition - PreviousPosition;

        public Vector2 AdjustedPreviousPosition(IMouseHandlerHost Handler) => PreviousPosition.ToVector2() + Handler.GetOffset();
        public Vector2 AdjustedCurrentPosition(IMouseHandlerHost Handler) => CurrentPosition.ToVector2() + Handler.GetOffset();

        public BaseMouseMovedEventArgs(MouseTracker Tracker, Point PreviousPosition, Point CurrentPosition)
        {
            this.Tracker = Tracker;
            this.PreviousPosition = PreviousPosition;
            this.CurrentPosition = CurrentPosition;
        }
    }

    #region Press / Release / Click
    public class BaseMousePressedEventArgs : HandledByEventArgs<IMouseHandlerHost>
    {
        public MouseTracker Tracker { get; }

        public DateTime PressedAt { get; }
        public MouseButton Button { get; }
        public Point Position { get; }
        public Vector2 AdjustedPosition(IMouseHandlerHost Handler) => Position.ToVector2() + Handler.GetOffset();

        /// <summary>True if the <see cref="Button"/> associated with this event is <see cref="MouseButton.Left"/></summary>
        public bool IsLMB => Button == MouseButton.Left;
        /// <summary>True if the <see cref="Button"/> associated with this event is <see cref="MouseButton.Right"/></summary>
        public bool IsRMB => Button == MouseButton.Right;
        /// <summary>True if the <see cref="Button"/> associated with this event is <see cref="MouseButton.Middle"/></summary>
        public bool IsMMB => Button == MouseButton.Middle;

        public BaseMousePressedEventArgs(MouseTracker Tracker, MouseButton Button, Point Position)
            : base()
        {
            this.Tracker = Tracker;
            this.PressedAt = DateTime.Now;
            this.Button = Button;
            this.Position = Position;
        }
    }

    public class BaseMouseReleasedEventArgs : HandledByEventArgs<IMouseHandlerHost>
    {
        public MouseTracker Tracker { get; }

        public BaseMousePressedEventArgs PressedArgs { get; }

        public DateTime ReleasedAt { get; }
        public MouseButton Button { get; }
        public Point Position { get; }
        public Vector2 AdjustedPosition(IMouseHandlerHost Handler) => Position.ToVector2() + Handler.GetOffset();

        public TimeSpan HeldDuration => ReleasedAt.Subtract(PressedArgs.PressedAt);

        /// <summary>True if the <see cref="Button"/> associated with this event is <see cref="MouseButton.Left"/></summary>
        public bool IsLMB => Button == MouseButton.Left;
        /// <summary>True if the <see cref="Button"/> associated with this event is <see cref="MouseButton.Right"/></summary>
        public bool IsRMB => Button == MouseButton.Right;
        /// <summary>True if the <see cref="Button"/> associated with this event is <see cref="MouseButton.Middle"/></summary>
        public bool IsMMB => Button == MouseButton.Middle;

        public BaseMouseReleasedEventArgs(MouseTracker Tracker, BaseMousePressedEventArgs PressedArgs, MouseButton Button, Point Position)
            : base()
        {
            this.Tracker = Tracker;
            this.ReleasedAt = DateTime.Now;
            this.PressedArgs = PressedArgs;
            this.Button = Button;
            this.Position = Position;
        }
    }

    public class BaseMouseClickedEventArgs : HandledByEventArgs<IMouseHandlerHost>
    {
        public MouseTracker Tracker { get; }

        public BaseMouseReleasedEventArgs ReleasedArgs { get; }
        public BaseMousePressedEventArgs PressedArgs => ReleasedArgs?.PressedArgs;

        public MouseButton Button { get; }
        public Point Position { get; }
        public Vector2 AdjustedPosition(IMouseHandlerHost Handler) => Position.ToVector2() + Handler.GetOffset();

        /// <summary>True if the <see cref="Button"/> associated with this event is <see cref="MouseButton.Left"/></summary>
        public bool IsLMB => Button == MouseButton.Left;
        /// <summary>True if the <see cref="Button"/> associated with this event is <see cref="MouseButton.Right"/></summary>
        public bool IsRMB => Button == MouseButton.Right;
        /// <summary>True if the <see cref="Button"/> associated with this event is <see cref="MouseButton.Middle"/></summary>
        public bool IsMMB => Button == MouseButton.Middle;

        public BaseMouseClickedEventArgs(MouseTracker Tracker, BaseMouseReleasedEventArgs ReleasedArgs, MouseButton Button, Point Position)
            : base()
        {
            this.Tracker = Tracker;
            this.ReleasedArgs = ReleasedArgs;
            this.Button = Button;
            this.Position = Position;
        }
    }
    #endregion Press / Release / Click

    #region Drag
    public class BaseMouseDragStartEventArgs : HandledByEventArgs<IMouseHandlerHost>
    {
        public MouseTracker Tracker { get; }

        public DateTime StartedAt { get; }
        public MouseButton Button { get; }
        public Point Position { get; }
        public Vector2 AdjustedPosition(IMouseHandlerHost Handler) => Position.ToVector2() + Handler.GetOffset();

        /// <summary>True if the <see cref="Button"/> associated with this event is <see cref="MouseButton.Left"/></summary>
        public bool IsLMB => Button == MouseButton.Left;
        /// <summary>True if the <see cref="Button"/> associated with this event is <see cref="MouseButton.Right"/></summary>
        public bool IsRMB => Button == MouseButton.Right;
        /// <summary>True if the <see cref="Button"/> associated with this event is <see cref="MouseButton.Middle"/></summary>
        public bool IsMMB => Button == MouseButton.Middle;

        public DragStartCondition Condition { get; }

        public BaseMouseDragStartEventArgs(MouseTracker Tracker, MouseButton Button, Point Position, DragStartCondition Condition)
        {
            this.Tracker = Tracker;
            this.StartedAt = DateTime.Now;
            this.Button = Button;
            this.Position = Position;
            this.Condition = Condition;
        }
    }

    public class BaseMouseDraggedEventArgs// : HandledByEventArgs<IMouseHandler>
    {
        public MouseTracker Tracker { get; }

        public BaseMouseDragStartEventArgs DragStartArgs { get; }

        public DateTime Timestamp { get; }
        public MouseButton Button { get; }
        public Point Position { get; }

        public Point StartPosition => DragStartArgs.Position;

        public Vector2 AdjustedPosition(IMouseHandlerHost Handler) => Position.ToVector2() + Handler.GetOffset();
        public Point PositionDelta => Position - DragStartArgs.Position;

        /// <summary>True if the <see cref="Button"/> associated with this event is <see cref="MouseButton.Left"/></summary>
        public bool IsLMB => Button == MouseButton.Left;
        /// <summary>True if the <see cref="Button"/> associated with this event is <see cref="MouseButton.Right"/></summary>
        public bool IsRMB => Button == MouseButton.Right;
        /// <summary>True if the <see cref="Button"/> associated with this event is <see cref="MouseButton.Middle"/></summary>
        public bool IsMMB => Button == MouseButton.Middle;

        public BaseMouseDraggedEventArgs(MouseTracker Tracker, BaseMouseDragStartEventArgs DragStartArgs, MouseButton Button, Point Position)
        {
            this.Tracker = Tracker;
            this.DragStartArgs = DragStartArgs;
            this.Timestamp = DateTime.Now;
            this.Button = Button;
            this.Position = Position;
        }

        /// <summary>Returns the distance the mouse has moved from the original position that the drag originated from, to its current position.</summary>
        /// <param name="SquaredDistance">True to retrieve squared distance, to improve performance by avoiding square root computation</param>
        public double GetTotalDistanceMoved(bool SquaredDistance = false)
        {
            double DeltaX = PositionDelta.X;
            double DeltaY = PositionDelta.Y;
            double DistanceSquared = DeltaX * DeltaX + DeltaY * DeltaY;
            if (SquaredDistance)
                return DistanceSquared;
            else
                return Math.Sqrt(DistanceSquared);
        }

        /// <summary>This is currently not implemented - it does nothing.</summary>
        public void SetHandled<T>(T HandledBy, bool OverwriteIfAlreadyHandled) { }
    }

    public class BaseMouseDragEndEventArgs// : HandledByEventArgs<IMouseHandler>
    {
        public MouseTracker Tracker { get; }

        public BaseMouseDragStartEventArgs DragStartArgs { get; }

        public DateTime Timestamp { get; }
        public MouseButton Button { get; }
        private Point Position { get; }

        public Point StartPosition => DragStartArgs.Position;
        public Vector2 AdjustedStartPosition(IMouseHandlerHost Handler) => Position.ToVector2() + Handler.GetOffset();
        public Point EndPosition => Position;
        public Vector2 AdjustedEndPosition(IMouseHandlerHost Handler) => Position.ToVector2() + Handler.GetOffset();
        public Point PositionDelta => EndPosition - StartPosition;

        /// <summary>True if the <see cref="Button"/> associated with this event is <see cref="MouseButton.Left"/></summary>
        public bool IsLMB => Button == MouseButton.Left;
        /// <summary>True if the <see cref="Button"/> associated with this event is <see cref="MouseButton.Right"/></summary>
        public bool IsRMB => Button == MouseButton.Right;
        /// <summary>True if the <see cref="Button"/> associated with this event is <see cref="MouseButton.Middle"/></summary>
        public bool IsMMB => Button == MouseButton.Middle;

        public BaseMouseDragEndEventArgs(MouseTracker Tracker, BaseMouseDragStartEventArgs DragStartArgs, MouseButton Button, Point Position)
        {
            this.Tracker = Tracker;
            this.DragStartArgs = DragStartArgs;
            this.Timestamp = DateTime.Now;
            this.Button = Button;
            this.Position = Position;
        }

        /// <summary>Returns the distance the mouse has moved from the original position that the drag originated from, to its current position.</summary>
        /// <param name="SquaredDistance">True to retrieve squared distance, to improve performance by avoiding square root computation</param>
        public double GetTotalDistanceMoved(bool SquaredDistance = false)
        {
            double DeltaX = PositionDelta.X;
            double DeltaY = PositionDelta.Y;
            double DistanceSquared = DeltaX * DeltaX + DeltaY * DeltaY;
            if (SquaredDistance)
                return DistanceSquared;
            else
                return Math.Sqrt(DistanceSquared);
        }

        /// <summary>This is currently not implemented - it does nothing.</summary>
        public void SetHandled<T>(T HandledBy, bool OverwriteIfAlreadyHandled) { }
    }
    #endregion Drag
}
