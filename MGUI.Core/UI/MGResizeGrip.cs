using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoGame.Extended;
using MGUI.Shared.Helpers;
using System.Diagnostics;
using MGUI.Shared.Input.Mouse;
using MGUI.Core.UI.Brushes.Fill_Brushes;

namespace MGUI.Core.UI
{
    /// <summary>Can be attached to another <see cref="MGElement"/> to allow resizing via dragging the mouse.</summary>
    public class MGResizeGrip : MGElement
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private VisualStateColorBrush _Foreground;
        /// <summary>The primary color to draw the resizer dots with.</summary>
        public VisualStateColorBrush Foreground
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

        /// <summary>Determines how much width and height this element takes up. This value is derived from <see cref="MaxDots"/>, <see cref="Spacing"/>, and <see cref="MGElement.Margin"/>.<br/>
        /// Formula: 1 + (<see cref="MaxDots"/> - 1) * <see cref="Spacing"/> + <see cref="MGElement.Margin"/>.Right</summary>
        public int Size => 1 + (MaxDots - 1) * Spacing + Margin.Right;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _MaxDots;
        /// <summary>Determines how many dots to attempt to display for this <see cref="MGResizeGrip"/>'s graphics.<para/>
        /// If this resizer allows resizing in either dimension, <see cref="MaxDots"/> determines how many dots will be drawn on either the bottom or right edge of this element.<para/>
        /// Default value: 4. Recommended value: 3-6</summary>
        public int MaxDots
        {
            get => _MaxDots;
            set
            {
                if (_MaxDots != value)
                {
                    _MaxDots = value;
                    LayoutChanged(this, true);
                    NPC(nameof(MaxDots));
                    NPC(nameof(Size));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _Spacing;
        /// <summary>The amount of pixels between each dot of this <see cref="MGResizeGrip"/>'s graphics.<para/>
        /// Default value: 3. Recommended value: 3-4</summary>
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
                    NPC(nameof(MaxDots));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private MGElement _Host;
        /// <summary>To set this value, use <see cref="TrySetHost(MGElement)"/>. This value cannot be modified if <see cref="MGElement.IsComponent"/> is true.<para/>
        /// See also: <see cref="ActualHost"/>, which accounts for <see cref="MGElement.IsComponent"/></summary>
        public MGElement Host { get => _Host; }
        public MGElement ActualHost => IsComponent ? Parent : Host;

        /// <returns>True if <see cref="Host"/> was set to the new value. False if unable to change the value, or if the new value was the same as the current value.</returns>
        public bool TrySetHost(MGElement Value)
        {
            if (!IsComponent && _Host != Value)
            {
                if (Host != null)
                {
                    Host.OnLayoutUpdated -= Host_LayoutUpdated;
                    Host.OnBeginUpdateContents -= Host_BeginUpdateContents;
                    Host.OnEndingDraw -= Host_EndingDraw;
                }

                _Host = Value;
                SetParent(Host);
                NPC(nameof(Host));
                NPC(nameof(ActualHost));

                if (Host != null)
                {
                    Host.OnLayoutUpdated += Host_LayoutUpdated;
                    Host.OnBeginUpdateContents += Host_BeginUpdateContents;
                    Host.OnEndingDraw += Host_EndingDraw;
                }

                return true;
            }
            else
                return false;
        }

        private void Host_LayoutUpdated(object sender, EventArgs e)
        {
            UpdateLayout(ApplyAlignment(Host.LayoutBounds, HorizontalAlignment.Right, VerticalAlignment.Bottom, new Size(Size, Size)));
        }

        private void Host_BeginUpdateContents(object sender, ElementUpdateEventArgs e)
        {
            Update(e.UA);
        }

        private void Host_EndingDraw(object sender, MGElementDrawEventArgs e)
        {
            if (!Host.RecentDrawWasClipped)
                DrawSelf(e.DA, this.LayoutBounds);
        }

        private int InitialWidth;
        private int InitialHeight;
        private bool IsDragging;

        //TODO options to allow resize in either direction or just 1 axis?

        /// <summary>Creates a <see cref="MGResizeGrip"/> that will be attached to the given <paramref name="HostElement"/></summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="HostElement"/> is null</exception>
        public MGResizeGrip(MGWindow Window, MGElement HostElement)
            : this(Window, false)
        {
            if (HostElement == null)
                throw new ArgumentNullException(nameof(HostElement));

            TrySetHost(HostElement);
        }

        /// <summary>Creates a <see cref="MGResizeGrip"/> which is a component of its parent element.</summary>
        internal MGResizeGrip(MGWindow Window)
            : this(Window, true)
        {

        }

        private MGResizeGrip(MGWindow ParentWindow, bool IsComponent)
            : base(ParentWindow, MGElementType.ResizeGrip)
        {
            using (BeginInitializing())
            {
                this.Foreground = GetTheme().ResizeGripForeground.GetValue(true);

                this.Margin = new(2);

                this.MaxDots = 4;
                this.Spacing = 3;
                OnMarginChanged += (sender, e) => { NPC(nameof(Size)); };

                MouseHandler.DragStartCondition = DragStartCondition.MousePressed;
                MouseHandler.DragStart += (sender, e) =>
                {
                    if (e.IsLMB && Parent != null)
                    {
                        IsDragging = true;
                        SpoofIsPressedWhileDrawingBackground = false;
                        InitialWidth = Parent is MGWindow ? ParentWindow.WindowWidth : Parent.ActualWidth;
                        InitialHeight = Parent is MGWindow ? ParentWindow.WindowHeight : Parent.ActualHeight;
                        e.SetHandledBy(this, false);
                    }
                };

                MouseHandler.Dragged += (sender, e) =>
                {
                    if (e.IsLMB && e.DragStartArgs.HandledBy == this && Parent != null)
                    {
                        float Scalar = 1.0f / Parent.SelfOrParentWindow.Scale;
                        Point Delta = new((int)(e.PositionDelta.X * Scalar), (int)(e.PositionDelta.Y * Scalar));
                        if (Parent is MGWindow TargetWindow)
                        {
                            TargetWindow.WindowWidth = Math.Max(0, InitialWidth + Delta.X);
                            TargetWindow.WindowHeight = Math.Max(0, InitialHeight + Delta.Y);
                        }
                        else
                        {
                            Parent.PreferredWidth = Math.Max(0, InitialWidth + Delta.X);
                            Parent.PreferredHeight = Math.Max(0, InitialHeight + Delta.Y);
                        }
                    }
                };

                MouseHandler.DragEnd += (sender, e) =>
                {
                    if (e.IsLMB && IsDragging)
                    {
                        IsDragging = false;
                        SpoofIsPressedWhileDrawingBackground = true;
                    }
                };
            }
        }

        public override Thickness MeasureSelfOverride(Size AvailableSize, out Thickness SharedSize)
        {
            SharedSize = new(0, 0, Size, Size);
            return new(0, 0, Size, Size);
        }

        public override void DrawSelf(ElementDrawArgs DA, Rectangle LayoutBounds)
        {
            Rectangle Region = ApplyAlignment(LayoutBounds, HorizontalAlignment.Right, VerticalAlignment.Bottom, new Size(Size, Size));
            Vector2 BottomRight = Region.BottomRight().ToVector2().Translate(-1, -1);

            //  Draw several small dots in the shape of a right-triangle
            List<Vector2> Points = new();
            for (int i = 0; i < MaxDots; i++)
            {
                for (int j = 0; j < MaxDots - i; j++)
                {
                    Vector2 Position = BottomRight.Translate(i * -Spacing, j * -Spacing);
                    Points.Add(Position);
                }
            }

            void FillPoints(Color Color)
            {
                foreach (Vector2 Point in Points)
                {
                    DA.DT.FillPoint(Point + DA.Offset.ToVector2(), Color, 1);
                }
            }

            FillPoints(Foreground.GetUnderlay(this.VisualState.Primary) * DA.Opacity);

            if (!ParentWindow.HasModalWindow)
            {
                Color? Overlay = Foreground.GetColorOverlay(VisualState.GetSecondaryState(IsDragging, false));
                if (Overlay != null)
                    FillPoints(Overlay.Value * DA.Opacity);
            }
        }
    }
}
