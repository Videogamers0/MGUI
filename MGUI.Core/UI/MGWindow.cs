using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MGUI.Shared.Helpers;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Diagnostics;
using MonoGame.Extended;
using MGUI.Core.UI.Containers;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Shared.Input.Mouse;
using MGUI.Shared.Input.Keyboard;
using MGUI.Shared.Rendering;

namespace MGUI.Core.UI
{
    public class CancelEventArgs<T> : CancelEventArgs
    {
        public T Data { get; }

        public CancelEventArgs(T Data)
            : base(false)
        {
            this.Data = Data;
        }
    }

    public class MGWindow : MGSingleContentHost
    {
        public MGDesktop Desktop { get; }

        #region Position / Size
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Point TopLeft
        {
            get => new(Left, Top);
            set
            {
                Left = value.X;
                Top = value.Y;
            }
        }

        public int Left { get; set; }
        public int Top { get; set; }

        /// <summary>Note: This event is not invoked immediately after <see cref="Left"/> or <see cref="Top"/> changes.<br/>
        /// It is invoked during the Update tick to improve performance by only allowing it to notify once per tick.</summary>
        public event EventHandler<EventArgs<(int Left, int Top)>> OnWindowPositionChanged;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _WindowWidth;
        public int WindowWidth
        {
            get => _WindowWidth;
            set
            {
                int ActualValue = Math.Clamp(value, MinWidth ?? 0, MaxWidth ?? int.MaxValue);
                if (_WindowWidth != ActualValue)
                {
                    _WindowWidth = ActualValue;
                    LayoutChanged(this, true);
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _WindowHeight;
        public int WindowHeight
        {
            get => _WindowHeight;
            set
            {
                int ActualValue = Math.Clamp(value, MinHeight ?? 0, MaxHeight ?? int.MaxValue);
                if (_WindowHeight != ActualValue)
                {
                    _WindowHeight = ActualValue;
                    LayoutChanged(this, true);
                }
            }
        }

        /// <summary>Note: This event is not invoked immediately after <see cref="WindowWidth"/> or <see cref="WindowHeight"/> changes.<br/>
        /// It is invoked during the Update tick to improve performance by only allowing it to notify once per tick.</summary>
        public event EventHandler<EventArgs<(int Width, int Height)>> OnWindowSizeChanged;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int PreviousLeft;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int PreviousTop;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int PreviousWidth;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int PreviousHeight;

        /// <summary>Fires the <see cref="OnWindowPositionChanged"/> and/or <see cref="OnWindowSizeChanged"/> events if necessary.<para/>
        /// This method is automatically invoked at the beginning of <see cref="MGElement.Update(ElementUpdateArgs)"/>,<br/>
        /// but in rare cases you may want to manually invoke this after changing <see cref="Left"/>, <see cref="Top"/>, <see cref="WindowWidth"/>, or <see cref="WindowHeight"/> to make changes take effect immediately.</summary>
        public void ValidateWindowSizeAndPosition()
        {
            if (PreviousLeft != Left || PreviousTop != Top)
            {
                try
                {
                    OnWindowPositionChanged?.Invoke(this, new((PreviousLeft, PreviousTop), (Left, Top)));
                }
                finally
                {
                    PreviousLeft = Left;
                    PreviousTop = Top;
                }
            }

            if (PreviousWidth != WindowWidth || PreviousHeight != WindowHeight)
            {
                try
                {
                    OnWindowSizeChanged?.Invoke(this, new((PreviousWidth, PreviousHeight), (WindowWidth, WindowHeight)));
                }
                finally
                {
                    PreviousWidth = WindowWidth;
                    PreviousHeight = WindowHeight;
                }
            }
        }

        public Size ComputeContentSize(int MinWidth = 100, int MinHeight = 100, int MaxWidth = 1920, int MaxHeight = 1080)
        {
            Size MinSize = new(MinWidth, MinHeight);
            Size MaxSize = new(Math.Min(GetDesktop().ValidScreenBounds.Width, MaxWidth), Math.Min(GetDesktop().ValidScreenBounds.Height, MaxHeight));
            UpdateMeasurement(MaxSize, out _, out Thickness FullSize, out _, out _);
            Size Size = FullSize.Size.Clamp(MinSize, MaxSize);
            return Size;
        }

        /// <summary>Resizes this <see cref="MGWindow"/> to satisfy the given constraints. Note: The layout of child content isn't refrshed until the next update tick.</summary>
        /// <returns>The computed size that this <see cref="MGWindow"/> will be changed to.</returns>
        public Size ApplySizeToContent(SizeToContent Value, int MinWidth = 100, int MinHeight = 100, int MaxWidth = 1920, int MaxHeight = 1080, bool UpdateLayoutImmediately = true)
        {
            Size MinSize = new(MinWidth, MinHeight);
            Size MaxSize = new(Math.Min(GetDesktop().ValidScreenBounds.Width, MaxWidth), Math.Min(GetDesktop().ValidScreenBounds.Height, MaxHeight));
            Size AvailableSize = GetActualAvailableSize(new Size(WindowWidth, WindowHeight), Value).Clamp(MinSize, MaxSize);
            UpdateMeasurement(AvailableSize, out _, out Thickness FullSize, out _, out _);
            Size Size = FullSize.Size.Clamp(MinSize, MaxSize);
            this.WindowWidth = Size.Width;
            this.WindowHeight = Size.Height;
            LayoutChanged(this, true);

            if (UpdateLayoutImmediately)
            {
                ValidateWindowSizeAndPosition();
                UpdateLayout(new Rectangle(Left, Top, WindowWidth, WindowHeight));
            }

            return Size;
        }

        protected static Size GetActualAvailableSize(Size Size, SizeToContent SizeToContent)
        {
            int ActualAvailableWidth = SizeToContent switch
            {
                SizeToContent.Manual => Size.Width,
                SizeToContent.Width => int.MaxValue,
                SizeToContent.Height => Size.Width,
                SizeToContent.WidthAndHeight => int.MaxValue,
                _ => throw new NotImplementedException($"Unrecognized {nameof(SizeToContent)}: {SizeToContent}")
            };

            int ActualAvailableHeight = SizeToContent switch
            {
                SizeToContent.Manual => Size.Height,
                SizeToContent.Width => Size.Height,
                SizeToContent.Height => int.MaxValue,
                SizeToContent.WidthAndHeight => int.MaxValue,
                _ => throw new NotImplementedException($"Unrecognized {nameof(SizeToContent)}: {SizeToContent}")
            };

            Size ActualAvailableSize = new(ActualAvailableWidth, ActualAvailableHeight);

            return ActualAvailableSize;
        }

        #region Resizing
        /// <summary>Provides direct access to the resizer grip that appears in the bottom-right corner of this textbox when <see cref="IsUserResizable"/> is true.</summary>
        public MGComponent<MGResizeGrip> ResizeGripComponent { get; }
        private MGResizeGrip ResizeGripElement { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _IsUserResizable;
        /// <summary>If true, a <see cref="MGResizeGrip"/> will be visible in the bottom-right corner of the window, allowing the user to click+drag it to adjust this <see cref="MGWindow"/>'s <see cref="WindowWidth"/>/<see cref="WindowHeight"/><para/>
        /// Default value: true (except in special-cases such as <see cref="MGComboBox{TItemType}.Dropdown"/> window or for <see cref="MGToolTip"/>s)</summary>
        public bool IsUserResizable
        {
            get => _IsUserResizable;
            set
            {
                _IsUserResizable = value;
                ResizeGripElement.Visibility = IsUserResizable ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        #endregion Resizing
        #endregion Position / Size

        #region Border
        /// <summary>Provides direct access to this element's border.</summary>
        public MGComponent<MGBorder> BorderComponent { get; }
        private MGBorder BorderElement { get; }

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

        #region Nested Windows
        /// <summary>A child <see cref="MGWindow"/> of this <see cref="MGWindow"/>, which blocks all input handling on this <see cref="MGWindow"/></summary>
        public MGWindow ModalWindow { get; set; }
        /// <summary>True if a modal window is being displayed overtop of this window.</summary>
        public bool HasModalWindow => ModalWindow != null;
        /// <summary>True if this window instance is the modal window of its parent window.</summary>
        public bool IsModalWindow => ParentWindow?.ModalWindow == this;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly List<MGWindow> _NestedWindows;
        /// <summary>The last element represents the <see cref="MGWindow"/> that will be drawn last (I.E., rendered overtop of everything else), and 
        /// updated first (I.E., has the first chance to handle inputs)<para/>
        /// This list does not include <see cref="ModalWindow"/>, which is always prioritized over all <see cref="NestedWindows"/></summary>
        public IReadOnlyList<MGWindow> NestedWindows => _NestedWindows;
        public void AddNestedWindow(MGWindow NestedWindow)
        {
            if (NestedWindow == null)
                throw new ArgumentNullException(nameof(NestedWindow));
            if (_NestedWindows.Contains(NestedWindow))
                throw new ArgumentException("Cannot add the same nested window to a parent window multiple times.");
            if (NestedWindow == this)
                throw new ArgumentException("Cannot add a window as a nested window to itself as this would create an infinite recursive dependency.");
            _NestedWindows.Add(NestedWindow);
        }
        public bool RemoveNestedWindow(MGWindow NestedWindow) => _NestedWindows.Remove(NestedWindow);

        /// <summary>Moves the given <paramref name="NestedWindow"/> to the end of <see cref="NestedWindows"/> list. It will be rendered overtop of all other <see cref="NestedWindows"/><para/>
        /// If there is a <see cref="ModalWindow"/>, then the <see cref="ModalWindow"/> will be rendered overtop of the front-most <paramref name="NestedWindow"/></summary>
        /// <param name="NestedWindow"></param>
        /// <returns>True if the <paramref name="NestedWindow"/> was brought to the front. False if it was not a valid element in <see cref="NestedWindows"/>.</returns>
        public bool BringToFront(MGWindow NestedWindow)
        {
            if (!_NestedWindows.Contains(NestedWindow))
            {
                return false;
            }
            else
            {
                if (_NestedWindows.IndexOf(NestedWindow) != _NestedWindows.Count - 1)
                {
                    _NestedWindows.Remove(NestedWindow);
                    _NestedWindows.Add(NestedWindow);
                }
                return true;
            }
        }

        /// <summary>Moves the given <paramref name="NestedWindow"/> to the start of <see cref="NestedWindows"/> list. It will be rendered underneath of all other <see cref="NestedWindows"/></summary>
        /// <param name="NestedWindow"></param>
        /// <returns>True if the <paramref name="NestedWindow"/> was moved to the back. False if it was not a valid element in <see cref="NestedWindows"/>.</returns>
        public bool BringToBack(MGWindow NestedWindow)
        {
            if (!_NestedWindows.Contains(NestedWindow))
            {
                return false;
            }
            else
            {
                if (_NestedWindows.IndexOf(NestedWindow) != 0)
                {
                    _NestedWindows.Remove(NestedWindow);
                    _NestedWindows.Insert(0, NestedWindow);
                }
                return true;
            }
        }

        public IEnumerable<MGWindow> RecurseNestedWindows(bool IncludeSelf, TreeTraversalMode TraversalMode = TreeTraversalMode.Postorder)
        {
            if (IncludeSelf && TraversalMode == TreeTraversalMode.Preorder)
                yield return this;

            foreach (MGWindow Nested in NestedWindows)
            {
                foreach (MGWindow Item in Nested.RecurseNestedWindows(true, TraversalMode))
                    yield return Item;
            }

            if (IncludeSelf && TraversalMode == TreeTraversalMode.Postorder)
                yield return this;
        }
        #endregion Nested Windows

        #region Title Bar
        /// <summary>Provides direct access to the dockpanel component that displays this window's title-bar content.<para/>
        /// See also: <see cref="IsTitleBarVisible"/>, <see cref="TitleBarTextBlockElement"/></summary>
        public MGComponent<MGDockPanel> TitleBarComponent { get; }
        private MGDockPanel TitleBarElement { get; }

        /// <summary>The textblock element that contains this window's <see cref="TitleText"/> in the title-bar.</summary>
        public MGTextBlock TitleBarTextBlockElement { get; }

        /// <summary>This property is functionally equivalent to <see cref="TitleBarTextBlockElement"/>'s <see cref="MGTextBlock.Text"/> property.<para/>
        /// See also: <see cref="IsTitleBarVisible"/></summary>
        public string TitleText
        {
            get => TitleBarTextBlockElement.Text;
            set { TitleBarTextBlockElement.Text = value; }
        }

        /// <summary>True if the title bar should be visible at the top of this <see cref="MGWindow"/><para/>
        /// Default value: true for most types of <see cref="MGWindow"/>, false for <see cref="MGToolTip"/></summary>
        public bool IsTitleBarVisible
        {
            get => TitleBarElement.Visibility == Visibility.Visible;
            set => TitleBarElement.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }

        #region Close
        public MGButton CloseButtonElement { get; }

        public bool IsCloseButtonVisible
        {
            get => CloseButtonElement.Visibility == Visibility.Visible;
            set => CloseButtonElement.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }

        public bool CanCloseWindow { get; set; } = true;

        public bool TryCloseWindow()
        {
            if (!CanCloseWindow)
                return false;

            if ((ParentWindow != null && (ParentWindow.NestedWindows.Contains(this) || ParentWindow.ModalWindow == this)) 
                || (ParentWindow == null && Desktop.Windows.Contains(this)))
            {
                if (WindowClosing != null)
                {
                    CancelEventArgs ClosingArgs = new();
                    WindowClosing.Invoke(this, ClosingArgs);
                    if (ClosingArgs.Cancel)
                        return false;
                }

                bool IsClosed = false;
                if (ParentWindow != null && ParentWindow.ModalWindow == this)
                {
                    ParentWindow.ModalWindow = null;
                    IsClosed = true;
                }    
                if (ParentWindow != null && ParentWindow.NestedWindows.Contains(this))
                    IsClosed = ParentWindow.RemoveNestedWindow(this);
                else if (ParentWindow == null && Desktop.Windows.Contains(this))
                    IsClosed = Desktop.Windows.Remove(this);

                if (IsClosed)
                    WindowClosed?.Invoke(this, EventArgs.Empty);
                return IsClosed;
            }

            return false;
        }

        public event EventHandler<CancelEventArgs> WindowClosing;
        public event EventHandler<EventArgs> WindowClosed;
        #endregion Close
        #endregion Title Bar

        #region RadioButton Groups
        private Dictionary<string, MGRadioButtonGroup> RadioButtonGroups { get; }
        public bool HasRadioButtonGroup(string Name) => RadioButtonGroups.ContainsKey(Name);
        public MGRadioButtonGroup GetOrCreateRadioButtonGroup(string Name)
        {
            if (RadioButtonGroups.TryGetValue(Name, out MGRadioButtonGroup ExistingGroup))
                return ExistingGroup;
            else
            {
                MGRadioButtonGroup NewGroup = new(this, Name);
                RadioButtonGroups.Add(Name, NewGroup);
                return NewGroup;
            }
        }
        #endregion RadioButton Groups

        /// <summary>A <see cref="MouseHandler"/> that is updated just before <see cref="MGElement.MouseHandler"/> is updated.<para/>
        /// This allows subscribing to mouse events that can get handled just before the <see cref="MGWindow"/>'s input handling can occur.</summary>
        public MouseHandler WindowMouseHandler { get; }
        /// <summary>A <see cref="KeyboardHandler"/> that is updated just before <see cref="MGElement.KeyboardHandler"/> is updated.<para/>
        /// This allows subscribing to keyboard events that can get handled just before the <see cref="MGWindow"/>'s input handling can occur.</summary>
        public KeyboardHandler WindowKeyboardHandler { get; }

        /// <summary>If true, mouse clicks overtop of this <see cref="MGWindow"/> that weren't handled by any child elements will remain unhandled,<br/>
        /// allowing content underneath this <see cref="MGWindow"/> to handle the mouse event.<para/>
        /// Default value: false<para/>
        /// This property is ignored if <see cref="IsModalWindow"/> is true.</summary>
        public bool AllowsClickThrough { get; set; } = false;

        /// <summary>True if this <see cref="MGWindow"/> can be moved by dragging the title bar.<para/>
        /// Warning: You may need to set <see cref="IsTitleBarVisible"/> to true to utilize this feature.</summary>
        public bool IsDraggable { get; set; } = true;

        #region Constructors
        /// <summary>Initializes a root-level window.</summary>
        public MGWindow(MGDesktop Desktop, int Left, int Top, int Width, int Height)
            : this(Desktop, null, MGElementType.Window, Left, Top, Width, Height)
        {

        }

        /// <summary>Initializes a nested window (such as a popup). You should still call <see cref="AddNestedWindow(MGWindow)"/> (or set <see cref="ModalWindow"/>) afterwards.</summary>
        public MGWindow(MGWindow Window, int Left, int Top, int Width, int Height)
            : this(Window.Desktop, Window, MGElementType.Window, Left, Top, Width, Height)
        {
            if (Window == null)
                throw new ArgumentNullException(nameof(Window));
        }

        /// <exception cref="InvalidOperationException">Thrown if you attempt to change <see cref="MGElement.HorizontalAlignment"/> or <see cref="MGElement.VerticalAlignment"/> on this <see cref="MGWindow"/></exception>
        protected MGWindow(MGDesktop Desktop, MGWindow ParentWindow, MGElementType ElementType, int Left, int Top, int Width, int Height)
            : base(Desktop, ParentWindow, ElementType)
        {
            if (ParentWindow == null && !MGElement.WindowElementTypes.Contains(ElementType))
                throw new InvalidOperationException($"All {nameof(MGElement)}s must either belong to an {nameof(MGWindow)} or be a root-level {nameof(MGWindow)} instance.");

            using (BeginInitializing())
            {
                this.Desktop = Desktop ?? throw new ArgumentNullException(nameof(Desktop));

                MGTheme Theme = GetTheme();

                this.WindowMouseHandler = InputTracker.Mouse.CreateHandler(this, null);
                this.WindowKeyboardHandler = InputTracker.Keyboard.CreateHandler(this, null);
                this.MouseHandler.DragStartCondition = DragStartCondition.Both;

                this.RadioButtonGroups = new();
                this._NestedWindows = new();
                this.ModalWindow = null;

                this.Left = Left;
                this.PreviousLeft = Left;
                this.Top = Top;
                this.PreviousTop = Top;

                this.WindowWidth = Width;
                this.PreviousWidth = Width;
                this.WindowHeight = Height;
                this.PreviousHeight = Height;

                this.MinWidth = 100;
                this.MinHeight = 100;
                this.MaxWidth = 4000;
                this.MaxHeight = 2000;

                this.Padding = new(5);

                this.BorderElement = new(this, new Thickness(2), MGUniformBorderBrush.Black);
                this.BorderComponent = MGComponentBase.Create(BorderElement);
                AddComponent(BorderComponent);

                this.TitleBarElement = new(this);
                TitleBarElement.Padding = new(2);
                TitleBarElement.MinHeight = 24;
                TitleBarElement.HorizontalAlignment = HorizontalAlignment.Stretch;
                TitleBarElement.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                TitleBarElement.VerticalAlignment = VerticalAlignment.Stretch;
                TitleBarElement.VerticalContentAlignment = VerticalAlignment.Stretch;
                TitleBarElement.BackgroundBrush = Theme.TitleBackground.GetValue(true);

                this.TitleBarComponent = new(TitleBarElement, true, false, true, true, false, false, false,
                    (AvailableBounds, ComponentSize) => ApplyAlignment(AvailableBounds, HorizontalAlignment.Stretch, VerticalAlignment.Top, ComponentSize.Size));
                AddComponent(TitleBarComponent);

                TitleBarElement.OnEndDraw += (sender, e) =>
                {
                    if (IsTitleBarVisible)
                    {
                        Rectangle TitleBottomEdge = new(TitleBarElement.LayoutBounds.Left, TitleBarElement.LayoutBounds.Bottom - BorderThickness.Top, TitleBarElement.LayoutBounds.Width, BorderThickness.Top);
                        BorderElement.BorderBrush.GetTop().Draw(e.DA, this, TitleBottomEdge);
                    }
                };

                this.CloseButtonElement = new(this, x => { TryCloseWindow(); });
                CloseButtonElement.MinWidth = 12;
                CloseButtonElement.MinHeight = 12;
                CloseButtonElement.BackgroundBrush = new(MGSolidFillBrush.White * 0.5f, Color.White * 0.18f, PressedModifierType.Darken, 0.06f);
                CloseButtonElement.BorderBrush = MGUniformBorderBrush.Black;
                CloseButtonElement.BorderThickness = new(1);
                CloseButtonElement.Margin = new(1, 1, 1, 1 + BorderElement.BorderThickness.Bottom);
                CloseButtonElement.Padding = new(4,-1);
                CloseButtonElement.VerticalAlignment = VerticalAlignment.Center;
                CloseButtonElement.VerticalContentAlignment = VerticalAlignment.Center;
                CloseButtonElement.HorizontalContentAlignment = HorizontalAlignment.Center;
                CloseButtonElement.SetContent(new MGTextBlock(this, "[b]x[/b]", Color.Crimson));
                //CloseButtonElement.CanChangeContent = false;

                this.TitleBarTextBlockElement = new(this, null, Color.White)
                {
                    Margin = new(4,0),
                    Padding = new(0),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = HorizontalAlignment.Left,
                    DefaultTextForeground = new VisualStateSetting<Color?>(Color.White, Color.White, Color.White)
                };
                TitleText = null;

                TitleBarElement.TryAddChild(CloseButtonElement, Dock.Right);
                TitleBarElement.TryAddChild(TitleBarTextBlockElement, Dock.Left);
                TitleBarElement.CanChangeContent = false;

                IsTitleBarVisible = true;

                this.ResizeGripElement = new(this);
                this.ResizeGripComponent = MGComponentBase.Create(ResizeGripElement);
                AddComponent(ResizeGripComponent);
                this.IsUserResizable = true;

                this.HorizontalAlignment = HorizontalAlignment.Stretch;
                this.VerticalAlignment = VerticalAlignment.Stretch;

                OnHorizontalAlignmentChanged += (sender, e) =>
                {
                    if (e.NewValue != HorizontalAlignment.Stretch)
                    {
                        string Error = $"The {nameof(HorizontalAlignment)} of a root-level window element must always be set to: " +
                            $"{nameof(HorizontalAlignment)}.{nameof(HorizontalAlignment.Stretch)}";
                        throw new InvalidOperationException(Error);
                    }
                };
                OnVerticalAlignmentChanged += (sender, e) =>
                {
                    if (e.NewValue != VerticalAlignment.Stretch)
                    {
                        string Error = $"The {nameof(VerticalAlignment)} of a root-level window element must always be set to: " +
                            $"{nameof(VerticalAlignment)}.{nameof(VerticalAlignment.Stretch)}";
                        throw new InvalidOperationException(Error);
                    }
                };

                ElementsByName = new();
                OnDirectOrNestedContentAdded += Element_Added;
                OnDirectOrNestedContentRemoved += Element_Removed;

                OnBeginUpdate += (sender, e) =>
                { 
                    ValidateWindowSizeAndPosition();

                    if (!IsLayoutValid)
                    {
                        InvalidateLayout();
                        UpdateLayout(new(this.Left, this.Top, this.WindowWidth, this.WindowHeight));
                    }
                };

                //  Ensure all mouse events that haven't already been handled by a child element of this window are handled, so that the mouse events won't fall-through to underneath this window
                MouseHandler.PressedInside += (sender, e) =>
                {
                    if (!AllowsClickThrough || IsModalWindow)
                        e.SetHandled(this, false);
                };
                MouseHandler.ReleasedInside += (sender, e) =>
                {
                    if (!AllowsClickThrough || IsModalWindow)
                        e.SetHandled(this, false);
                };
                MouseHandler.DragStart += (sender, e) =>
                {
                    if (!AllowsClickThrough || IsModalWindow)
                        e.SetHandled(this, false);
                };
#if true // Disallow scroll-wheel click-through
                //TODO maybe the window should never auto-handle scroll wheel events?
                MouseHandler.Scrolled += (sender, e) =>
                {
                    if (!AllowsClickThrough || IsModalWindow)
                        e.SetHandled(this, false);
                };
#endif

                MouseHandler.PressedOutside += (sender, e) =>
                {
                    if (IsModalWindow)
                        e.SetHandled(this, false);
                };
                MouseHandler.ReleasedOutside += (sender, e) =>
                {
                    if (IsModalWindow)
                        e.SetHandled(this, false);
                };
                MouseHandler.DragStartOutside += (sender, e) =>
                {
                    if (IsModalWindow)
                        e.SetHandled(this, false);
                };

                OnBeginUpdateContents += (sender, e) =>
                {
                    ElementUpdateArgs UpdateArgs = e.UA with { Offset = this.BoundsOffset };

                    //TODO does this order make sense?
                    //What if this window has both a ModalWindow and a NestedWindow, and the NestedWindow has a ModalWindow.
                    //Should we update the ModalWindow of the NestedWindow before we update the ModalWindow of this Window?
                    ModalWindow?.Update(UpdateArgs);

                    foreach (MGWindow Nested in _NestedWindows.Reverse<MGWindow>())
                        Nested.Update(UpdateArgs);
                };

                OnEndUpdateContents += (sender, e) =>
                {
                    WindowMouseHandler.ManualUpdate();
                    WindowKeyboardHandler.ManualUpdate();
                };

                OnEndDraw += (sender, e) =>
                {
                    //  Draw a transparent black overlay if there is a Modal window overtop of this window
                    if (ModalWindow != null)
                        e.DA.DT.FillRectangle(e.DA.Offset.ToVector2(), this.LayoutBounds, Color.Black * 0.5f);

                    foreach (MGWindow Nested in _NestedWindows)
                        Nested.Draw(e.DA);
                    ModalWindow?.Draw(e.DA);

                    if (!IsDrawingDraggedWindowPreview && IsDraggingWindowPosition && DragWindowPositionOffset.HasValue && DragWindowPositionOffset.Value != Point.Zero)
                    {
                        try
                        {
                            IsDrawingDraggedWindowPreview = true;
                            float TempOpacity = e.DA.Opacity * 0.25f;
                            Point TempOffset = e.DA.Offset + DragWindowPositionOffset.Value;
                            Draw(e.DA.SetOpacity(TempOpacity) with { Offset = TempOffset });
                        }
                        finally { IsDrawingDraggedWindowPreview = false; }
                    }
                };

                MakeDraggable();
            }
        }
        #endregion Constructors

        #region Drag Window Position
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool IsDraggingWindowPosition = false;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Point? DragWindowPositionOffset = null;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool IsDrawingDraggedWindowPreview = false;

        /// <summary>Intended to be called once during initialization. Subscribes to the appropriate events to allow the user to move this <see cref="MGWindow"/> by clicking and dragging the window's Title bar.</summary>
        private void MakeDraggable()
        {
            MouseHandler.DragStart += (sender, e) =>
            {
                if (e.IsLMB && IsDraggable && e.Condition == DragStartCondition.MouseMovedAfterPress && IsTitleBarVisible)
                {
                    Point MousePosition = e.AdjustedPosition(this).ToPoint();
                    if (TitleBarElement.LayoutBounds.ContainsInclusive(MousePosition) && !CloseButtonElement.LayoutBounds.ContainsInclusive(MousePosition))
                    {
                        IsDraggingWindowPosition = true;
                        DragWindowPositionOffset = Point.Zero;
                        e.SetHandled(this, false);
                        Debug.WriteLine($"{nameof(MGWindow)}: Drag Start at: {e.Position}");
                    }
                }
            };

            MouseHandler.Dragged += (sender, e) =>
            {
                if (e.IsLMB && IsDraggingWindowPosition)
                {
                    DragWindowPositionOffset = e.PositionDelta;
                }
            };

            MouseHandler.DragEnd += (sender, e) =>
            {
                try
                {
                    if (e.IsLMB && IsDraggingWindowPosition && DragWindowPositionOffset.HasValue)
                    {
                        this.Left += DragWindowPositionOffset.Value.X;
                        this.Top += DragWindowPositionOffset.Value.Y;

                        foreach (MGWindow Nested in this._NestedWindows)
                        {
                            Nested.Left += DragWindowPositionOffset.Value.X;
                            Nested.Top += DragWindowPositionOffset.Value.Y;
                        }

                        if (ModalWindow != null)
                        {
                            ModalWindow.Left += DragWindowPositionOffset.Value.X;
                            ModalWindow.Top += DragWindowPositionOffset.Value.Y;
                        }

                        MouseHandler.Tracker.CurrentButtonReleasedEvents[MouseButton.Left]?.SetHandled(this, false);

                        Debug.WriteLine($"{nameof(MGWindow)}: Drag End at: {e.EndPosition}");
                    }
                }
                finally
                {
                    IsDraggingWindowPosition = false;
                    DragWindowPositionOffset = null;
                }
            };
        }
        #endregion Drag Window Position

        #region Indexed Elements
        private Dictionary<string, MGElement> ElementsByName { get; }

        public MGElement GetElementByName(string Name) => ElementsByName[Name];
        public T GetElementByName<T>(string Name) where T : MGElement => ElementsByName[Name] as T;

        public bool TryGetElementByName(string Name, out MGElement Element) => ElementsByName.TryGetValue(Name, out Element);
        public bool TryGetElementByName<T>(string Name, out T Element) where T : MGElement
        {
            if (TryGetElementByName(Name, out MGElement Result))
            {
                Element = Result as T;
                return Element != null;
            }
            else
            {
                Element = default;
                return false;
            }
        }

        private void Element_Added(object sender, MGElement e)
        {
            if (e.Name != null)
                ElementsByName.Add(e.Name, e);

            if (e.ToolTip != null)
            {
                MGToolTip TT = e.ToolTip;
                foreach (MGElement Element in TT.TraverseVisualTree(true, false, TreeTraversalMode.Preorder))
                {
                    Element_Added(TT, Element);
                }
            }

            if (e.ContextMenu != null)
            {
                MGContextMenu CM = e.ContextMenu;
                foreach (MGElement Element in CM.TraverseVisualTree(true, false, TreeTraversalMode.Preorder))
                {
                    Element_Added(CM, Element);
                }
            }

            e.ContextMenuChanged += Element_ContextMenuChanged;
            e.OnNameChanged += Element_NameChanged;

        }

        private void Element_Removed(object sender, MGElement e)
        {
            if (e.Name != null)
                ElementsByName.Remove(e.Name);

            if (e.ToolTip != null)
            {
                MGToolTip TT = e.ToolTip;
                foreach (MGElement Element in TT.TraverseVisualTree(true, false, TreeTraversalMode.Preorder))
                {
                    Element_Removed(TT, Element);
                }
            }

            if (e.ContextMenu != null)
            {
                MGContextMenu CM = e.ContextMenu;
                foreach (MGElement Element in CM.TraverseVisualTree(true, false, TreeTraversalMode.Preorder))
                {
                    Element_Removed(CM, Element);
                }
            }

            e.ContextMenuChanged -= Element_ContextMenuChanged;
            e.OnNameChanged -= Element_NameChanged;
        }

        private void Element_NameChanged(object sender, EventArgs<string> e)
        {
            if (e.PreviousValue != null)
                ElementsByName.Remove(e.PreviousValue);
            if (e.NewValue != null)
                ElementsByName.Add(e.NewValue, sender as MGElement);
        }

        private void Element_ContextMenuChanged(object sender, EventArgs<MGContextMenu> e)
        {
            MGContextMenu Previous = e.PreviousValue;
            if (Previous != null)
            {
                Previous.OnDirectOrNestedContentAdded -= Element_Added;
                Previous.OnDirectOrNestedContentRemoved -= Element_Removed;

                foreach (MGElement Element in Previous.TraverseVisualTree(true, false, TreeTraversalMode.Preorder))
                {
                    Element_Removed(Previous, Element);
                }
            }

            MGContextMenu New = e.NewValue;
            if (New != null)
            {
                New.OnDirectOrNestedContentAdded += Element_Added;
                New.OnDirectOrNestedContentRemoved += Element_Removed;

                foreach (MGElement Element in New.TraverseVisualTree(true, false, TreeTraversalMode.Preorder))
                {
                    Element_Added(New, Element);
                }
            }
        }
        #endregion Indexed Elements

        public void Draw(DrawBaseArgs BA) => Draw(new ElementDrawArgs(BA, this.VisualState, Point.Zero));

        public override MGBorder GetBorder() => BorderElement;
    }
}
