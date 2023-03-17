using MGUI.Shared.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Shared.Rendering
{
    [DebuggerStepThrough]
    public record struct DrawBaseArgs(TimeSpan TS, DrawTransaction DT, float Opacity)
    {
        public DrawBaseArgs SetOpacity(float Value) => new(TS, DT, Value);
        public DrawBaseArgs MultiplyOpacity(float Scalar) => new(TS, DT, this.Opacity * Scalar);
    }

    [DebuggerStepThrough]
    public class DrawBaseEventArgs : EventArgs
    {
        public readonly DrawBaseArgs BA;
        public TimeSpan TS => BA.TS;
        public DrawTransaction DT => BA.DT;
        public float Opacity => BA.Opacity;

        public DrawBaseEventArgs(DrawBaseArgs BA)
        {
            this.BA = BA;
        }
    }

#if true
    [DebuggerStepThrough]
    public record struct UpdateBaseArgs(TimeSpan TotalElapsed, TimeSpan FrameElapsed, MouseState MouseState, KeyboardState KeyboardState)
    {
        public UpdateBaseArgs GetTranslated(int MouseXOffset, int MouseYOffset)
        {
            MouseState MS = MouseState;
            MouseState Translated = new(MS.X + MouseXOffset, MS.Y + MouseYOffset, MS.ScrollWheelValue, MS.LeftButton, MS.MiddleButton, MS.RightButton, MS.XButton1, MS.XButton2);
            return new(TotalElapsed, FrameElapsed, Translated, KeyboardState);
        }
    }
#else
    public class UpdateBaseArgs
    {
        public readonly TimeSpan TotalElapsed;
        public readonly TimeSpan FrameElapsed;
        public readonly MouseState MouseState;
        public readonly KeyboardState KeyboardState;

        public UpdateBaseArgs(TimeSpan TotalElapsed, TimeSpan FrameElapsed, MouseState MouseState, KeyboardState KeyboardState)
        {
            this.TotalElapsed = TotalElapsed;
            this.FrameElapsed = FrameElapsed;
            this.MouseState = MouseState;
            this.KeyboardState = KeyboardState;
        }

        public UpdateBaseArgs GetTranslated(int MouseXOffset, int MouseYOffset)
        {
            MouseState MS = MouseState;
            MouseState Translated = new(MS.X + MouseXOffset, MS.Y + MouseYOffset, MS.ScrollWheelValue, MS.LeftButton, MS.MiddleButton, MS.RightButton, MS.XButton1, MS.XButton2);
            return new(TotalElapsed, FrameElapsed, Translated, KeyboardState);
        }
    }
#endif

    [DebuggerStepThrough]
    public class UpdateBaseEventArgs : EventArgs
    {
        public readonly UpdateBaseArgs BA;

        public TimeSpan TotalElapsed => BA.TotalElapsed;
        public TimeSpan FrameElapsed => BA.FrameElapsed;
        public MouseState MouseState => BA.MouseState;
        public KeyboardState KeyboardState => BA.KeyboardState;

        public UpdateBaseEventArgs(UpdateBaseArgs BA)
        {
            this.BA = BA;
        }
    }

    public abstract class View : ViewModelBase
    {
        public MainRenderer MainRenderer { get; }

        public GraphicsDevice GraphicsDevice => MainRenderer?.GraphicsDevice;
        public ObservableCollection<View> Children { get; }

        private bool _IsVisible;
        public bool IsVisible
        {
            get => _IsVisible;
            set
            {
                if (IsVisible != value)
                {
                    _IsVisible = value;
                    AutoNPC();
                }
            }
        }

        protected Rectangle _ScreenViewport;
        /// <summary>The entire screen bound's that this <see cref="View"/> occupies.</summary>
        public Rectangle ScreenViewport { get => _ScreenViewport; }

        /// <param name="NotifyChanged">True if <see cref="OnScreenViewportChanged"/> should be invoked.</param>
        /// <param name="BeforeNotify">Optional, can be null. If not null, this action will be invoked after <see cref="ScreenViewport"/> is set to the new <paramref name="Value"/>, but before <see cref="OnScreenViewportChanged"/> is invoked.</param>
        protected void SetScreenViewportBase(Rectangle Value, Action BeforeNotify, bool NotifyChanged = true)
        {
            if (ScreenViewport != Value)
            {
                Rectangle Previous = ScreenViewport;
                _ScreenViewport = Value;
                NPC(nameof(ScreenViewport));
                BeforeNotify?.Invoke();
                if (NotifyChanged)
                    OnScreenViewportChanged?.Invoke(this, new(Previous, ScreenViewport));
            }
        }

        public event EventHandler<EventArgs<Rectangle>> OnScreenViewportChanged;

        private enum TraversalType
        {
            Preorder,
            PostOrder
        }

        private IEnumerable<View> RecurseChildren(TraversalType Mode, bool IncludeSelf, bool IncludeHidden)
        {
            switch (Mode)
            {
                case TraversalType.Preorder:
                    if (IncludeSelf)
                        yield return this;
                    foreach (View Child in Children)
                    {
                        IEnumerable<View> RecursiveChildren = Child.RecurseChildren(Mode, true, IncludeHidden);
                        foreach (View Item in RecursiveChildren)
                            yield return Item;
                    }
                    break;
                case TraversalType.PostOrder:
                    foreach (View Child in Children)
                    {
                        IEnumerable<View> RecursiveChildren = Child.RecurseChildren(Mode, true, IncludeHidden);
                        foreach (View Item in RecursiveChildren)
                            yield return Item;
                    }
                    if (IncludeSelf)
                        yield return this;
                    break;
                default:
                    throw new NotImplementedException($"Unrecognized {nameof(TraversalType)}: {Mode}");
            }
        }

        public View(MainRenderer MainRenderer, int ScreenViewportMargin)
            : this(MainRenderer, MainRenderer.GetViewport(ScreenViewportMargin)) { }

        public View(MainRenderer MainRenderer, Rectangle ScreenViewport)
        {
            this.MainRenderer = MainRenderer;
            this.IsVisible = true;
            SetScreenViewportBase(ScreenViewport, null, true);
            this.Children = new();
        }

        /// <summary>Invoked at the beginning of the <see cref="Update(UpdateBaseArgs)"/> method.</summary>
        public event EventHandler<UpdateBaseEventArgs> OnBeginUpdate;
        /// <summary>Invoked at the end of the <see cref="Update(UpdateBaseArgs)"/> method.</summary>
        public event EventHandler<UpdateBaseEventArgs> OnEndUpdate;

        public void Update(UpdateBaseArgs BA)
        {
            OnBeginUpdate?.Invoke(this, new(BA));
            foreach (View Child in Children)
                Child.Update(BA);
            UpdateSelf(BA);
            OnEndUpdate?.Invoke(this, new(BA));
        }

        protected abstract void UpdateSelf(UpdateBaseArgs BA);

        public void Draw(DrawBaseArgs BA)
        {
            if (IsVisible)
            {
                DrawBackground(BA);
                foreach (View Child in Children)
                    Child.Draw(BA);
                DrawForeground(BA);
            }
        }

        protected abstract void DrawBackground(DrawBaseArgs BA);
        protected abstract void DrawForeground(DrawBaseArgs BA);
    }
}
