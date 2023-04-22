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
using Microsoft.Xna.Framework.Graphics;
using MGUI.Core.UI.Containers.Grids;

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

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _Left;
        public int Left
        {
            get => _Left;
            set
            {
                if (_Left != value)
                {
                    _Left = value;
                    NPC(nameof(Left));
                    NPC(nameof(TopLeft));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _Top;
        public int Top
        {
            get => _Top;
            set
            {
                if (_Top != value)
                {
                    _Top = value;
                    NPC(nameof(Top));
                    NPC(nameof(TopLeft));
                }
            }
        }

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
                    UpdateScaleTransforms();
                    RecentSizeToContentSettings = null;
                    NPC(nameof(WindowWidth));
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
                    UpdateScaleTransforms();
                    RecentSizeToContentSettings = null;
                    NPC(nameof(WindowHeight));
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

        protected void InvokeWindowPositionChanged(Point PreviousPosition, Point NewPosition)
            => OnWindowPositionChanged?.Invoke(this, new((PreviousPosition.X, PreviousPosition.Y), (NewPosition.X, NewPosition.Y)));

        /// <summary>Fires the <see cref="OnWindowPositionChanged"/> and/or <see cref="OnWindowSizeChanged"/> events if necessary.<para/>
        /// This method is automatically invoked at the beginning of <see cref="MGElement.Update(ElementUpdateArgs)"/>,<br/>
        /// but in rare cases you may want to manually invoke this after changing <see cref="Left"/>, <see cref="Top"/>, <see cref="WindowWidth"/>, or <see cref="WindowHeight"/> to make changes take effect immediately.</summary>
        public void ValidateWindowSizeAndPosition()
        {
            if (PreviousLeft != Left || PreviousTop != Top)
            {
                try
                {
                    UpdateScaleTransforms();
                    InvokeWindowPositionChanged(new(PreviousLeft, PreviousTop), new(Left, Top));
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
                    UpdateScaleTransforms();
#if NEVER
                    UpdateRenderTarget();
#endif
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

        private readonly record struct SizeToContentSettings(SizeToContent Type, int MinWidth, int MinHeight, int? MaxWidth, int? MaxHeight);
        private SizeToContentSettings? RecentSizeToContentSettings = null;

        /// <summary>Resizes this <see cref="MGWindow"/> to satisfy the given constraints.</summary>
        /// <param name="UpdateLayoutImmediately">If true, the layout of child content is refreshed immediately rather than waiting until the next update tick.</param>
        /// <returns>The computed size that this <see cref="MGWindow"/> will be changed to.</returns>
        public Size ApplySizeToContent(SizeToContent Value, int MinWidth = 50, int MinHeight = 50, int? MaxWidth = 1920, int? MaxHeight = 1080, bool UpdateLayoutImmediately = true)
        {
            Size MinSize = new(MinWidth, MinHeight);
            Size MaxSize = new(Math.Min(GetDesktop().ValidScreenBounds.Width - Left, MaxWidth ?? int.MaxValue), Math.Min(GetDesktop().ValidScreenBounds.Height - Top, MaxHeight ?? int.MaxValue));
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

            RecentSizeToContentSettings = new(Value, MinWidth, MinHeight, MaxWidth, MaxHeight);

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

        #region Scale
        private float _Scale = 1.0f;
        /// <summary>Scales this <see cref="MGWindow"/> from the window's center point.<para/>
        /// Default value: 1.0f</summary>
        public float Scale
        {
            get => _Scale;
            set
            {
                if (_Scale != value)
                {
                    float Previous = Scale;
                    _Scale = value;
                    UpdateScaleTransforms();
#if NEVER
                    UpdateRenderTarget();
#endif
                    NPC(nameof(Scale));
                    NPC(nameof(IsWindowScaled));
                    ScaleChanged?.Invoke(this, new(Previous, Scale));
                }
            }
        }

        /// <summary>Invoked when <see cref="Scale"/> changes.</summary>
        public event EventHandler<EventArgs<float>> ScaleChanged;

        public bool IsWindowScaled => !Scale.IsAlmostEqual(1.0f);

        private Matrix _UnscaledScreenSpaceToScaledScreenSpace;
        /// <summary>A <see cref="Matrix"/> that converts coordinates that haven't accounted for <see cref="Scale"/> to coordinates that have.<para/>
        /// If <see cref="Scale"/> is 1.0f, this value is <see cref="Matrix.Identity"/><para/>
        /// See also: <see cref="ScaledScreenSpaceToUnscaledScreenSpace"/></summary>
        protected internal Matrix UnscaledScreenSpaceToScaledScreenSpace { get => _UnscaledScreenSpaceToScaledScreenSpace; }

        private Matrix _ScaledScreenSpaceToUnscaledScreenSpace;
        /// <summary>A <see cref="Matrix"/> that converts coordinates in screen space to coordinates that haven't accounted for <see cref="Scale"/>.<para/>
        /// If <see cref="Scale"/> is 1.0f, this value is <see cref="Matrix.Identity"/><para/>
        /// See also: <see cref="UnscaledScreenSpaceToScaledScreenSpace"/></summary>
        protected internal Matrix ScaledScreenSpaceToUnscaledScreenSpace { get => _ScaledScreenSpaceToUnscaledScreenSpace; }

        private void UpdateScaleTransforms()
        {
            if (IsWindowScaled)
            {
                Vector2 ScaleOrigin = TopLeft.ToVector2(); //TopLeft.ToVector2() + new Vector2(WindowWidth / 2, WindowHeight / 2); // Center of window
                _UnscaledScreenSpaceToScaledScreenSpace =
                    Matrix.CreateTranslation(new Vector3(-ScaleOrigin, 0)) *
                    Matrix.CreateScale(Scale) *
                    Matrix.CreateTranslation(new Vector3(ScaleOrigin, 0));
                _ScaledScreenSpaceToUnscaledScreenSpace = Matrix.Invert(UnscaledScreenSpaceToScaledScreenSpace);
            }
            else
            {
                _UnscaledScreenSpaceToScaledScreenSpace = Matrix.Identity;
                _ScaledScreenSpaceToUnscaledScreenSpace = Matrix.Identity;
            }
        }

#if NEVER
        private void UpdateRenderTarget()
        {
            if (IsWindowScaled)
                RenderTarget = RenderUtils.CreateRenderTarget(GetDesktop().Renderer.GraphicsDevice, WindowWidth, WindowHeight, true);
            else
                RenderTarget = null;
        }

        private RenderTarget2D _RenderTarget;
        private RenderTarget2D RenderTarget
        {
            get => _RenderTarget;
            set
            {
                if (_RenderTarget != value)
                {
                    _RenderTarget?.Dispose();
                    _RenderTarget = value;
                }
            }
        }
#endif
        #endregion Scale

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
                NPC(nameof(IsUserResizable));
            }
        }
        #endregion Resizing
        #endregion Position / Size

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

        #region Nested Windows
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private MGWindow _ModalWindow;
        /// <summary>A child <see cref="MGWindow"/> of this <see cref="MGWindow"/>, which blocks all input handling on this <see cref="MGWindow"/></summary>
        public MGWindow ModalWindow
        {
            get => _ModalWindow;
            set
            {
                if (_ModalWindow != value)
                {
                    MGWindow Previous = ModalWindow;
                    _ModalWindow = value;
                    NPC(nameof(ModalWindow));
                    NPC(nameof(HasModalWindow));
                    Previous?.NPC(nameof(MGWindow.IsModalWindow));
                    ModalWindow?.NPC(nameof(MGWindow.IsModalWindow));
                }
            }
        }
        /// <summary>True if a modal window is being displayed overtop of this window.</summary>
        public bool HasModalWindow => ModalWindow != null;
        /// <summary>True if this window instance is the modal window of its parent window.</summary>
        public bool IsModalWindow => ParentWindow?.ModalWindow == this;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly List<MGWindow> _NestedWindows;
        /// <summary>The last element represents the <see cref="MGWindow"/> that will be 
        /// drawn last (I.E., rendered overtop of everything else), and updated first (I.E., has the first chance to handle inputs)<br/>
        /// except in cases where a Topmost window is prioritized (See: <see cref="IsTopmost"/>)<para/>
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

        /// <summary>Moves the given <paramref name="NestedWindow"/> to the end of <see cref="NestedWindows"/> list.<br/>
        /// It will typically be rendered overtop of all other <see cref="NestedWindows"/>, unless another nested window is Topmost (See: <see cref="IsTopmost"/>).<para/>
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

        /// <summary>Moves the given <paramref name="NestedWindow"/> to the start of <see cref="NestedWindows"/> list.<br/>
        /// It will typically be rendered underneath of all other <see cref="NestedWindows"/>, unless it is Topmost (See: <see cref="IsTopmost"/>)</summary>
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
            set
            {
                if (TitleBarTextBlockElement.Text != value)
                {
                    TitleBarTextBlockElement.Text = value;
                    NPC(nameof(TitleText));
                }
            }
        }

        /// <summary>True if the title bar should be visible at the top of this <see cref="MGWindow"/><para/>
        /// Default value: true for most types of <see cref="MGWindow"/>, false for <see cref="MGToolTip"/></summary>
        public bool IsTitleBarVisible
        {
            get => TitleBarElement.Visibility == Visibility.Visible;
            set
            {
                Visibility ActualValue = value ? Visibility.Visible : Visibility.Collapsed;
                if (TitleBarElement.Visibility != ActualValue)
                {
                    TitleBarElement.Visibility = ActualValue;
                    NPC(nameof(IsTitleBarVisible));
                }
            }
        }

        #region Close
        public MGButton CloseButtonElement { get; }

        public bool IsCloseButtonVisible
        {
            get => CloseButtonElement.Visibility == Visibility.Visible;
            set
            {
                Visibility ActualValue = value ? Visibility.Visible : Visibility.Collapsed;
                if (CloseButtonElement.Visibility != ActualValue)
                {
                    CloseButtonElement.Visibility = ActualValue;
                    NPC(nameof(IsCloseButtonVisible));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _CanCloseWindow = true;
        public bool CanCloseWindow
        {
            get => _CanCloseWindow;
            set
            {
                if (_CanCloseWindow != value)
                {
                    _CanCloseWindow = value;
                    NPC(nameof(CanCloseWindow));
                }
            }
        }

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

        #region Named ToolTips
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Dictionary<string, MGToolTip> _NamedToolTips { get; }
        /// <summary>This dictionary is commonly used by <see cref="MGTextBlock"/> to reference <see cref="MGToolTip"/>s by a string key value.<para/>
        /// See also:<br/><see cref="AddNamedToolTip(string, MGToolTip)"/><br/><see cref="RemoveNamedToolTip(string)"/><para/>
        /// EX: If you create an <see cref="MGTextBlock"/> and set its text to:
        /// <code>[ToolTip=ABC]This text has a ToolTip[/ToolTip] but this text doesn't</code>
        /// then the ToolTip with the name "ABC" will be shown when hovering over the substring "This text has a ToolTip"</summary>
        public IReadOnlyDictionary<string, MGToolTip> NamedToolTips => _NamedToolTips;

        public void AddNamedToolTip(string Name, MGToolTip ToolTip) => _NamedToolTips.Add(Name, ToolTip);
        public void RemoveNamedToolTip(string Name) => _NamedToolTips.Remove(Name);
        #endregion Named ToolTips

        /// <summary>A <see cref="MouseHandler"/> that is updated just before <see cref="MGElement.MouseHandler"/> is updated.<para/>
        /// This allows subscribing to mouse events that can get handled just before the <see cref="MGWindow"/>'s input handling can occur.</summary>
        public MouseHandler WindowMouseHandler { get; }
        /// <summary>A <see cref="KeyboardHandler"/> that is updated just before <see cref="MGElement.KeyboardHandler"/> is updated.<para/>
        /// This allows subscribing to keyboard events that can get handled just before the <see cref="MGWindow"/>'s input handling can occur.</summary>
        public KeyboardHandler WindowKeyboardHandler { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _AllowsClickThrough = false;
        /// <summary>If true, mouse clicks overtop of this <see cref="MGWindow"/> that weren't handled by any child elements will remain unhandled,<br/>
        /// allowing content underneath this <see cref="MGWindow"/> to handle the mouse event.<para/>
        /// Default value: false<para/>
        /// This property is ignored if <see cref="IsModalWindow"/> is true.</summary>
        public bool AllowsClickThrough
        {
            get => _AllowsClickThrough;
            set
            {
                if (_AllowsClickThrough != value)
                {
                    _AllowsClickThrough = value;
                    NPC(nameof(AllowsClickThrough));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _IsDraggable = true;
        /// <summary>True if this <see cref="MGWindow"/> can be moved by dragging the title bar.<para/>
        /// Warning: You may need to set <see cref="IsTitleBarVisible"/> to true to utilize this feature.</summary>
        public bool IsDraggable
        {
            get => _IsDraggable;
            set
            {
                if (_IsDraggable != value)
                {
                    _IsDraggable = value;
                    NPC(nameof(IsDraggable));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private MGTheme _Theme;
        /// <summary>If null, uses <see cref="MGDesktop.Theme"/> instead.<para/>
        /// Default value: null<para/>
        /// See also:<br/><see cref="MGElement.GetTheme()"/><br/><see cref="MGDesktop.Theme"/></summary>
        public MGTheme Theme
        {
            get => _Theme;
            set
            {
                if (_Theme != value)
                {
                    _Theme = value;
                    NPC(nameof(Theme));
                }
            }
        }

        private bool _IsTopmost;
        /// <summary>If true, this <see cref="MGWindow"/> will always be updated first (so that it has first-chance to receive and handle inputs)
        /// and drawn last (so that it appears overtop of all other <see cref="MGWindow"/>s).<para/>
        /// This property is only respected within the window's scope.<br/>
        /// If this window is topmost, but is also a nested window of a non-topmost window, it will only be on top of it's siblings (the other nested windows of its parent)<para/>
        /// If multiple windows are topmost, their draw/update priority depends on their index in <see cref="MGDesktop.Windows"/> and <see cref="NestedWindows"/><para/>
        /// <see cref="ModalWindow"/>s will still take priority even over topmost windows.</summary>
        public bool IsTopmost
        {
            get => _IsTopmost;
            set
            {
                if (_IsTopmost != value)
                {
                    _IsTopmost = value;
                    NPC(nameof(IsTopmost));
                }
            }
        }

        private MGElement PressedElementAtBeginUpdate { get; set; }
        private MGElement HoveredElementAtBeginUpdate { get; set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private MGElement _PressedElement;
        /// <summary>The inner-most element of the visual tree that the mouse was hovering at the moment that the left mouse button was pressed.<br/>
        /// Null if the left mouse button is currently released or the mouse wasn't hovering an element when the button was pressed down.<para/>
        /// See also: <see cref="HoveredElement"/>, <see cref="PressedElementChanged"/></summary>
        public MGElement PressedElement
        {
            get => _PressedElement;
            private set
            {
                if (_PressedElement != value)
                {
                    _PressedElement = value;
                    NPC(nameof(PressedElement));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private MGElement _HoveredElement;
        /// <summary>The inner-most element of the visual tree that the mouse is currently hovering, if any.<para/>
        /// If the mouse is hovering several sibling elements (such as children of an <see cref="MGOverlayPanel"/>, or elements placed inside the same cell of an <see cref="MGGrid"/>)<br/>
        /// then this property prioritizes the topmost element.<para/>
        /// See also: <see cref="PressedElement"/>, <see cref="HoveredElementChanged"/></summary>
        public MGElement HoveredElement
        {
            get => _HoveredElement;
            private set
            {
                if (_HoveredElement != value)
                {
                    _HoveredElement = value;
                    NPC(nameof(HoveredElement));
                }
            }
        }

        /// <summary>Invoked after <see cref="PressedElement"/> changes, but is intentionally deferred until the end of the current update tick so that <see cref="MGElement.VisualState"/> 
        /// values are properly synced with the <see cref="PressedElement"/></summary>
        public event EventHandler<EventArgs<MGElement>> PressedElementChanged;
        /// <summary>Invoked after <see cref="HoveredElement"/> changes, but is intentionally deferred until the end of the current update tick so that <see cref="MGElement.VisualState"/> 
        /// values are properly synced with the <see cref="HoveredElement"/></summary>
        public event EventHandler<EventArgs<MGElement>> HoveredElementChanged;

        internal bool InvalidatePressedAndHoveredElements { get; set; } = false;

        /// <summary>If true, this <see cref="MGWindow"/>'s layout will be recomputed at the start of the next update tick.</summary>
        public bool QueueLayoutRefresh { get; set; }

        private static readonly Thickness DefaultWindowPadding = new(5);
        private static readonly Thickness DefaultWindowBorderThickness = new(2);

        #region Data Context
        private object _WindowDataContext;
        /// <summary>The default <see cref="MGElement.DataContext"/> for all elements that do not explicitly define a <see cref="MGElement.DataContextOverride"/>.<para/>
        /// If not specified, this value is automatically inherited from the <see cref="MGElement.ParentWindow"/> if there is one.</summary>
        public object WindowDataContext
        {
            get => _WindowDataContext ?? ParentWindow?.WindowDataContext;
            set
            {
                if (_WindowDataContext != value)
                {
                    _WindowDataContext = value;
                    NPC(nameof(WindowDataContext));
                    NPC(nameof(DataContextOverride));
                    NPC(nameof(DataContext));
                    WindowDataContextChanged?.Invoke(this, WindowDataContext);
                    //  Changing the DataContext may have changed the sizes of elements on this window, so attempt to resize the window to its contents
                    //RevalidateSizeToContent(false);
                }
            }
        }

        public override object DataContextOverride
        { 
            get => WindowDataContext;
            set => WindowDataContext = value;
        }

        public event EventHandler<object> WindowDataContextChanged;

        private void RevalidateSizeToContent(bool UpdateImmediately)
        {
            if (RecentSizeToContentSettings.HasValue)
            {
                ApplySizeToContent(RecentSizeToContentSettings.Value.Type, RecentSizeToContentSettings.Value.MinWidth, RecentSizeToContentSettings.Value.MinHeight,
                    RecentSizeToContentSettings.Value.MaxWidth, RecentSizeToContentSettings.Value.MaxHeight, UpdateImmediately);
            }
        }
        #endregion Data Context

        #region Constructors
        /// <summary>Initializes a root-level window.</summary>
        public MGWindow(MGDesktop Desktop, int Left, int Top, int Width, int Height, MGTheme Theme = null)
            : this(Desktop, Theme, null, MGElementType.Window, Left, Top, Width, Height)
        {

        }

        /// <summary>Initializes a nested window (such as a popup). You should still call <see cref="AddNestedWindow(MGWindow)"/> (or set <see cref="ModalWindow"/>) afterwards.</summary>
        public MGWindow(MGWindow Window, int Left, int Top, int Width, int Height, MGTheme Theme = null)
            : this(Window.Desktop, Theme, Window, MGElementType.Window, Left, Top, Width, Height)
        {
            if (Window == null)
                throw new ArgumentNullException(nameof(Window));
        }

        /// <exception cref="InvalidOperationException">Thrown if you attempt to change <see cref="MGElement.HorizontalAlignment"/> or <see cref="MGElement.VerticalAlignment"/> on this <see cref="MGWindow"/></exception>
        protected MGWindow(MGDesktop Desktop, MGTheme WindowTheme, MGWindow ParentWindow, MGElementType ElementType, int Left, int Top, int Width, int Height)
            : base(Desktop, WindowTheme, ParentWindow, ElementType)
        {
            if (ParentWindow == null && !WindowElementTypes.Contains(ElementType))
                throw new InvalidOperationException($"All {nameof(MGElement)}s must either belong to an {nameof(MGWindow)} or be a root-level {nameof(MGWindow)} instance.");

            using (BeginInitializing())
            {
                this.Desktop = Desktop ?? throw new ArgumentNullException(nameof(Desktop));
                this.Theme = WindowTheme;

                MGTheme ActualTheme = GetTheme();

                this.WindowMouseHandler = InputTracker.Mouse.CreateHandler(this, null);
                this.WindowKeyboardHandler = InputTracker.Keyboard.CreateHandler(this, null);
                this.MouseHandler.DragStartCondition = DragStartCondition.Both;

                this.RadioButtonGroups = new();
                this._NestedWindows = new();
                this._NamedToolTips = new();
                this.ModalWindow = null;

                this.Left = Left;
                this.PreviousLeft = Left;
                this.Top = Top;
                this.PreviousTop = Top;

                this.WindowWidth = Width;
                this.PreviousWidth = Width;
                this.WindowHeight = Height;
                this.PreviousHeight = Height;

                this.MinWidth = 50;
                this.MinHeight = 50;
                this.MaxWidth = 4000;
                this.MaxHeight = 2000;

                this.Padding = DefaultWindowPadding;

                this.BorderElement = new(this, DefaultWindowBorderThickness, MGUniformBorderBrush.Black);
                this.BorderComponent = MGComponentBase.Create(BorderElement);
                AddComponent(BorderComponent);
                BorderElement.OnBorderBrushChanged += (sender, e) => { NPC(nameof(BorderBrush)); };
                BorderElement.OnBorderThicknessChanged += (sender, e) => { NPC(nameof(BorderThickness)); };

                this.TitleBarElement = new(this);
                TitleBarElement.Padding = new(2);
                TitleBarElement.MinHeight = 24;
                TitleBarElement.HorizontalAlignment = HorizontalAlignment.Stretch;
                TitleBarElement.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                TitleBarElement.VerticalAlignment = VerticalAlignment.Stretch;
                TitleBarElement.VerticalContentAlignment = VerticalAlignment.Stretch;
                TitleBarElement.BackgroundBrush = ActualTheme.TitleBackground.GetValue(true);

                this.TitleBarComponent = new(TitleBarElement, true, false, true, true, false, false, false,
                    (AvailableBounds, ComponentSize) => ApplyAlignment(AvailableBounds, HorizontalAlignment.Stretch, VerticalAlignment.Top, ComponentSize.Size));
                AddComponent(TitleBarComponent);
#if NEVER
                TitleBarElement.OnEndDraw += (sender, e) =>
                {
                    //  Attempt to draw the window's border around just the titlebar, so that the border is also drawn just underneath the title bar
                    if (IsTitleBarVisible)
                    {
                        Rectangle TargetBounds = new(TitleBarElement.LayoutBounds.Left - BorderThickness.Left, TitleBarElement.LayoutBounds.Top - BorderThickness.Top, 
                            TitleBarElement.LayoutBounds.Width + BorderThickness.Width, TitleBarElement.LayoutBounds.Height + BorderThickness.Top + BorderThickness.Bottom / 2);
                        BorderElement.BorderBrush.Draw(e.DA, this, TargetBounds, BorderThickness);
                    }
                };
#endif

                this.CloseButtonElement = new(this, x => { TryCloseWindow(); });
                CloseButtonElement.MinWidth = 12;
                CloseButtonElement.MinHeight = 12;
                CloseButtonElement.BackgroundBrush = new(Color.Crimson.AsFillBrush() * 0.5f, Color.White * 0.18f, PressedModifierType.Darken, 0.06f);
                CloseButtonElement.BorderBrush = MGUniformBorderBrush.Black;
                CloseButtonElement.BorderThickness = new(1);
                CloseButtonElement.Margin = new(1, 1, 1, 1 + BorderElement.BorderThickness.Bottom);
                CloseButtonElement.Padding = new(4,-1);
                CloseButtonElement.VerticalAlignment = VerticalAlignment.Center;
                CloseButtonElement.VerticalContentAlignment = VerticalAlignment.Center;
                CloseButtonElement.HorizontalContentAlignment = HorizontalAlignment.Center;
                CloseButtonElement.SetContent(new MGTextBlock(this, "[b][shadow=Black 1 1]x[/shadow][/b]", Color.White));
                //CloseButtonElement.CanChangeContent = false;

                this.TitleBarTextBlockElement = new(this, null, Color.White, ActualTheme.FontSettings.SmallFontSize)
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
                    if (ElementType == MGElementType.Window && e.NewValue != HorizontalAlignment.Stretch)
                    {
                        string Error = $"The {nameof(HorizontalAlignment)} of a root-level window element must always be set to: " +
                            $"{nameof(HorizontalAlignment)}.{nameof(HorizontalAlignment.Stretch)}";
                        throw new InvalidOperationException(Error);
                    }
                };
                OnVerticalAlignmentChanged += (sender, e) =>
                {
                    if (ElementType == MGElementType.Window && e.NewValue != VerticalAlignment.Stretch)
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
                    PressedElementAtBeginUpdate = PressedElement;
                    HoveredElementAtBeginUpdate = HoveredElement;

                    bool ShouldUpdateHoveredElement = MouseHandler.Tracker.MouseMovedRecently || !IsLayoutValid || QueueLayoutRefresh || InvalidatePressedAndHoveredElements;

                    ValidateWindowSizeAndPosition();

                    if (!IsLayoutValid || QueueLayoutRefresh)
                    {
                        QueueLayoutRefresh = false;
                        if (RecentSizeToContentSettings.HasValue)
                            RevalidateSizeToContent(true);
                        else
                            UpdateLayout(new(this.Left, this.Top, this.WindowWidth, this.WindowHeight));
                    }

                    if (ShouldUpdateHoveredElement)
                        HoveredElement = GetTopmostHoveredElement(e.UA);

                    if (MouseHandler.Tracker.MouseLeftButtonPressedRecently)
                        PressedElement = GetTopmostHoveredElement(e.UA);
                    else if (MouseHandler.Tracker.MouseLeftButtonReleasedRecently)
                        PressedElement = null;
                };

                OnEndUpdate += (sender, e) =>
                {
                    //  These 2 events are intentionally deferred because they affect MGElement.VisualState,
                    //  and subscribing code probably wants to access the most up-to-date MGElement.VisualState values.
                    if (PressedElementAtBeginUpdate != PressedElement)
                        PressedElementChanged?.Invoke(this, new(PressedElementAtBeginUpdate, PressedElement));
                    if (HoveredElementAtBeginUpdate != HoveredElement)
                        HoveredElementChanged?.Invoke(this, new(HoveredElementAtBeginUpdate, HoveredElement));
                };

                //  Ensure all mouse events that haven't already been handled by a child element of this window are handled, so that the mouse events won't fall-through to underneath this window
                MouseHandler.PressedInside += (sender, e) =>
                {
                    if (!AllowsClickThrough || IsModalWindow)
                        e.SetHandledBy(this, false);
                };
                MouseHandler.ReleasedInside += (sender, e) =>
                {
                    if (!AllowsClickThrough || IsModalWindow)
                        e.SetHandledBy(this, false);
                };
                MouseHandler.DragStart += (sender, e) =>
                {
                    if (!AllowsClickThrough || IsModalWindow)
                        e.SetHandledBy(this, false);
                };
                MouseHandler.Scrolled += (sender, e) =>
                {
                    if (!AllowsClickThrough || IsModalWindow)
                        e.SetHandledBy(this, false);
                };
                MouseHandler.PressedOutside += (sender, e) =>
                {
                    if (IsModalWindow)
                        e.SetHandledBy(this, false);
                };
                MouseHandler.ReleasedOutside += (sender, e) =>
                {
                    if (IsModalWindow)
                        e.SetHandledBy(this, false);
                };
                MouseHandler.DragStartOutside += (sender, e) =>
                {
                    if (IsModalWindow)
                        e.SetHandledBy(this, false);
                };

                OnBeginUpdateContents += (sender, e) =>
                {
                    ElementUpdateArgs UpdateArgs = e.UA.ChangeOffset(this.Origin);

                    //TODO does this order make sense?
                    //What if this window has both a ModalWindow and a NestedWindow, and the NestedWindow has a ModalWindow.
                    //Should we update the ModalWindow of the NestedWindow before we update the ModalWindow of this Window?
                    ModalWindow?.Update(UpdateArgs);

                    foreach (MGWindow Nested in _NestedWindows.Reverse<MGWindow>().OrderByDescending(x => x.IsTopmost))
                        Nested.Update(UpdateArgs);
                };

                OnEndUpdateContents += (sender, e) =>
                {
                    WindowMouseHandler.ManualUpdate();
                    WindowKeyboardHandler.ManualUpdate();
                };

                //  Nested windows inherit their WindowDataContext from the parent if they don't have their own explicit value
                if (ParentWindow != null)
                {
                    ParentWindow.WindowDataContextChanged += (sender, e) =>
                    {
                        if (_WindowDataContext == null)
                        {
                            NPC(nameof(WindowDataContext));
                            NPC(nameof(DataContextOverride));
                            NPC(nameof(DataContext));
                            WindowDataContextChanged?.Invoke(this, WindowDataContext);
                            RevalidateSizeToContent(false);
                        }
                    };
                }

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
                    Point LayoutSpacePosition = ConvertCoordinateSpace(CoordinateSpace.Screen, CoordinateSpace.Layout, e.Position);
                    if (TitleBarElement.LayoutBounds.ContainsInclusive(LayoutSpacePosition) && !CloseButtonElement.LayoutBounds.ContainsInclusive(LayoutSpacePosition))
                    {
                        IsDraggingWindowPosition = true;
                        DragWindowPositionOffset = Point.Zero;
                        e.SetHandledBy(this, false);
                        //Debug.WriteLine($"{nameof(MGWindow)}: Drag Start at: {e.Position}");
                    }
                }
            };

            MouseHandler.Dragged += (sender, e) =>
            {
                if (e.IsLMB && IsDraggingWindowPosition)
                {
                    float Scalar = 1.0f / Scale;
                    Point Delta = new((int)(e.PositionDelta.X * Scalar), (int)(e.PositionDelta.Y * Scalar));
                    DragWindowPositionOffset = Delta;
                }
            };

            MouseHandler.DragEnd += (sender, e) =>
            {
                try
                {
                    if (e.IsLMB && IsDraggingWindowPosition && DragWindowPositionOffset.HasValue)
                    {
                        Point Delta = new((int)(DragWindowPositionOffset.Value.X * Scale), (int)(DragWindowPositionOffset.Value.Y * Scale));

                        this.Left += Delta.X;
                        this.Top += Delta.Y;

                        foreach (MGWindow Nested in this._NestedWindows)
                        {
                            Nested.Left += Delta.X;
                            Nested.Top += Delta.Y;
                        }

                        if (ModalWindow != null)
                        {
                            ModalWindow.Left += Delta.X;
                            ModalWindow.Top += Delta.Y;
                        }

                        MouseHandler.Tracker.CurrentButtonReleasedEvents[MouseButton.Left]?.SetHandledBy(this, false);

                        //Debug.WriteLine($"{nameof(MGWindow)}: Drag End at: {e.EndPosition}");
                    }
                }
                finally
                {
                    IsDraggingWindowPosition = false;
                    DragWindowPositionOffset = null;
                }
            };

            //  Pre-emptively handle mouse release events if user was dragging this window
            //  so that the child content doesn't also react to the mouse release
            //  (such as if you end the mouse drag by releasing the mouse overtop of a button)
            OnBeginUpdateContents += (sender, e) =>
            {
                if (IsDraggingWindowPosition)
                {
                    MouseHandler.Tracker.CurrentButtonReleasedEvents[MouseButton.Left]?.SetHandledBy(this, false);
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
                foreach (MGElement Element in TT.TraverseVisualTree(true, false, false, false, TreeTraversalMode.Preorder))
                {
                    Element_Added(TT, Element);
                }
            }

            if (e.ContextMenu != null)
            {
                MGContextMenu CM = e.ContextMenu;
                foreach (MGElement Element in CM.TraverseVisualTree(true, false, false, false, TreeTraversalMode.Preorder))
                {
                    Element_Added(CM, Element);
                }
            }

            e.ToolTipChanged += Element_ToolTipChanged;
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
                foreach (MGElement Element in TT.TraverseVisualTree(true, false, false, false, TreeTraversalMode.Preorder))
                {
                    Element_Removed(TT, Element);
                }
            }

            if (e.ContextMenu != null)
            {
                MGContextMenu CM = e.ContextMenu;
                foreach (MGElement Element in CM.TraverseVisualTree(true, false, false, false, TreeTraversalMode.Preorder))
                {
                    Element_Removed(CM, Element);
                }
            }

            e.ToolTipChanged -= Element_ToolTipChanged;
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

        private void Element_ToolTipChanged(object sender, EventArgs<MGToolTip> e) => Element_NestedElementChanged(e.PreviousValue, e.NewValue);
        private void Element_ContextMenuChanged(object sender, EventArgs<MGContextMenu> e) => Element_NestedElementChanged(e.PreviousValue, e.NewValue);

        private void Element_NestedElementChanged(MGSingleContentHost Previous, MGSingleContentHost New)
        {
            if (Previous != null)
            {
                Previous.OnDirectOrNestedContentAdded -= Element_Added;
                Previous.OnDirectOrNestedContentRemoved -= Element_Removed;

                foreach (MGElement Element in Previous.TraverseVisualTree(true, false, false, false, TreeTraversalMode.Preorder))
                {
                    Element_Removed(Previous, Element);
                }
            }

            if (New != null)
            {
                New.OnDirectOrNestedContentAdded += Element_Added;
                New.OnDirectOrNestedContentRemoved += Element_Removed;

                foreach (MGElement Element in New.TraverseVisualTree(true, false, false, false, TreeTraversalMode.Preorder))
                {
                    Element_Added(New, Element);
                }
            }
        }
#endregion Indexed Elements

        private VisualStateFillBrush PreviousBackgroundBrush = null;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private WindowStyle _WindowStyle = WindowStyle.Default;
        public WindowStyle WindowStyle
        {
            get => _WindowStyle;
            set
            {
                if (WindowStyle != value)
                {
                    _WindowStyle = value;

                    switch (_WindowStyle)
                    {
                        case WindowStyle.Default:
                            this.IsTitleBarVisible = true;
                            this.IsCloseButtonVisible = true;
                            this.IsUserResizable = true;
                            this.Padding = DefaultWindowPadding;
                            this.BorderThickness = DefaultWindowBorderThickness;
                            this.BackgroundBrush = PreviousBackgroundBrush ?? BackgroundBrush;
                            break;
                        case WindowStyle.None:
                            this.IsTitleBarVisible = false;
                            this.IsCloseButtonVisible = false;
                            this.IsUserResizable = false;
                            this.Padding = new(0);
                            this.BorderThickness = new(0);
                            this.PreviousBackgroundBrush = BackgroundBrush.Copy();
                            this.BackgroundBrush.SetAll(MGSolidFillBrush.Transparent); // Set this to MGSolidFillBrush.White * 0.2f while testing the AllowsClickThrough issue below
                                                                                       //this.AllowsClickThrough = true;   //TODO we probably want AllowsClickThrough=false, but to then handle any unhandled events that occurred overtop of this window's content.
                                                                                       //That way, an invisible window with margin around the content (such as horizontally-centered content) won't auto-handle clicks within the
                                                                                       //window that are outside the content.
                                                                                       //For Example, make an invisible window at topleft=0,0, size=500,500
                                                                                       //Add content with size=200,200, centered in the window
                                                                                       //clicking at position=100,100 overlaps the window, but doesn't overlap the content of the window
                                                                                       //so the click should fall-through to whatever's under the window
                            break;
                        default: throw new NotImplementedException($"Unrecognized {nameof(WindowStyle)}: {value}");
                    }

                    NPC(nameof(WindowStyle));
                }
            }
        }

        public void Draw(DrawBaseArgs BA) => Draw(new ElementDrawArgs(BA, this.VisualState, Point.Zero));

        public event EventHandler<ElementDrawArgs> OnBeginDrawNestedWindows;
        public event EventHandler<ElementDrawArgs> OnEndDrawNestedWindows;

        private IEnumerable<MGWindow> ParentWindows
        {
            get
            {
                MGWindow Current = ParentWindow;
                while (Current != null)
                {
                    yield return Current;
                    Current = Current.ParentWindow;
                }
            }
        }

        public override void Draw(ElementDrawArgs DA)
        {
            if (!IsWindowScaled && !ParentWindows.Any(x => x.IsWindowScaled))
                base.Draw(DA);
            else
            {
#if true
                using (DA.DT.SetTransformTemporary(UnscaledScreenSpaceToScaledScreenSpace))
                {
                    base.Draw(DA);
                }
#else
                using (DA.DT.SetRenderTargetTemporary(RenderTarget, Color.Transparent))
                {
                    ElementDrawArgs Translated = DA with { Offset = DA.Offset - TopLeft };
                    base.Draw(Translated);
                }

                Rectangle LayoutSpaceBounds = new(Left, Top, WindowWidth, WindowHeight);
                Rectangle Destination = ConvertCoordinateSpace(CoordinateSpace.Layout, CoordinateSpace.Screen, LayoutSpaceBounds);
                DA.DT.DrawTextureTo(RenderTarget, null, Destination);
#endif
            }

            OnBeginDrawNestedWindows?.Invoke(this, DA);

            //  Draw a transparent black overlay if there is a Modal window overtop of this window
            if (ModalWindow != null)
                DA.DT.FillRectangle(DA.Offset.ToVector2(), this.LayoutBounds, Color.Black * 0.5f);

            foreach (MGWindow Nested in _NestedWindows.OrderBy(x => x.IsTopmost))
                Nested.Draw(DA);
            ModalWindow?.Draw(DA);

            if (!IsDrawingDraggedWindowPreview && IsDraggingWindowPosition && DragWindowPositionOffset.HasValue && DragWindowPositionOffset.Value != Point.Zero)
            {
                try
                {
                    IsDrawingDraggedWindowPreview = true;
                    float TempOpacity = DA.Opacity * 0.25f;
                    Point TempOffset = DA.Offset + DragWindowPositionOffset.Value;
                    Draw(DA.SetOpacity(TempOpacity) with { Offset = TempOffset });
                }
                finally { IsDrawingDraggedWindowPreview = false; }
            }

            OnEndDrawNestedWindows?.Invoke(this, DA);
        }
    }
}
