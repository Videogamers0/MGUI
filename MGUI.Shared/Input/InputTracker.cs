using MGUI.Shared.Input.Keyboard;
using MGUI.Shared.Input.Mouse;
using MGUI.Shared.Rendering;
using System;

namespace MGUI.Shared.Input
{
    public enum InputUpdatePriority
    {
        Minimum = 0,
        Low = 25,
        Medium = 50,
        High = 75,
        Maximum = 100
    }

    public class HandledByEventArgs<THandlerType> : EventArgs
    {
        public bool IsHandled { get; private set; }
        public THandlerType HandledBy { get; private set; }

        public bool IsHandledByType<T>() => IsHandled && HandledBy is T;

        public HandledByEventArgs()
        {
            IsHandled = false;
            HandledBy = default;
        }

        /// <summary>Deprecated - use <see cref="SetHandledBy{T}(T, bool)"/> instead.</summary>
        [Obsolete("Deprecated - use SetHandledBy{T}(T, bool) instead.")]
        public void SetHandled<T>(T HandledBy, bool OverwriteIfAlreadyHandled = false) where T : THandlerType
            => SetHandledBy(HandledBy, OverwriteIfAlreadyHandled);

        public void SetHandledBy<T>(T HandledBy, bool OverwriteIfAlreadyHandled = false)
            where T : THandlerType
        {
            if (!IsHandled || OverwriteIfAlreadyHandled)
            {
                IsHandled = true;
                this.HandledBy = HandledBy;
            }
        }

        /// <summary>Resets <see cref="IsHandled"/> and <see cref="HandledBy"/> back to their default values.<br/>
        /// (<see cref="IsHandled"/>=false, <see cref="HandledBy"/>=default)</summary>
        public void Reset()
        {
            IsHandled = false;
            HandledBy = default;
        }
    }

    public class InputTracker
    {
        public MouseTracker Mouse { get; }
        public KeyboardTracker Keyboard { get; }

        public InputTracker()
        {
            Mouse = new(this);
            Keyboard = new(this);
        }

        /// <summary>Should be invoked exactly once per Update tick, at the very start of your Game's Update method.<para/>
        /// This method will detect changes to the mouse/keyboard states but won't fire the events until <see cref="MouseTracker.UpdateHandlers"/> or <see cref="KeyboardTracker.UpdateHandlers"/> are called.</summary>
        public void Update(UpdateBaseArgs BA)
        {
            Mouse.Update(BA);
            Keyboard.Update(BA);
        }
    }
}
