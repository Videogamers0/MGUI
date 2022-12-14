using MGUI.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using MonoGame.Extended;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.ObjectModel;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Shared.Rendering;
using MGUI.Shared.Input.Mouse;
using MGUI.Shared.Input.Keyboard;
using MGUI.Shared.Input;
using MGUI.Core.UI.Containers;
using MGUI.Core.UI.Containers.Grids;

namespace MGUI.Core.UI
{
    public readonly record struct ElementUpdateArgs(UpdateBaseArgs BA, bool IsEnabled, bool IsSelected, bool IsHitTestVisible, Point Offset, Rectangle ActualLayoutBounds)
    {
        public TimeSpan TotalElapsed => BA.TotalElapsed;
        public TimeSpan FrameElapsed => BA.FrameElapsed;
        public MouseState MouseState => BA.MouseState;
        public KeyboardState KeyboardState => BA.KeyboardState;

        public ElementUpdateArgs AsZeroOffset() => this with { Offset = Point.Zero };
    };

    public readonly record struct ElementDrawArgs(DrawBaseArgs BA, VisualState VisualState, Point Offset)
    {
        public TimeSpan TS => BA.TS;
        public DrawTransaction DT => BA.DT;
        public float Opacity => BA.Opacity;
        public bool IsEnabled => !VisualState.IsDisabled;
        public bool IsSelected => VisualState.IsSelected;

        public ElementDrawArgs SetOpacity(float Value) => this with { BA = BA.SetOpacity(Value) };
        public ElementDrawArgs SetOffset(Point Value) => this with { Offset = Value };

        public ElementDrawArgs AsZeroOffset() => this with { Offset = Point.Zero };
    }

    public readonly record struct ElementMeasurement(Size AvailableSize, Thickness RequestedSize, Thickness SharedSize, Thickness ContentSize)
	{
		public bool IsAvailableSizeGreaterThan(Size Other) => AvailableSize.Width > Other.Width && AvailableSize.Height > Other.Height;
        public bool IsAvailableSizeGreaterThanOrEqual(Size Other) => AvailableSize.Width >= Other.Width && AvailableSize.Height >= Other.Height;
        public bool IsAvailableSizeLessThan(Size Other) => AvailableSize.Width < Other.Width && AvailableSize.Height < Other.Height;
        public bool IsAvailableSizeLessThanOrEqual(Size Other) => AvailableSize.Width <= Other.Width && AvailableSize.Height <= Other.Height;

        public bool IsRequestedSizeGreaterThan(Size Other) => RequestedSize.Width > Other.Width && RequestedSize.Height > Other.Height;
        public bool IsRequestedSizeGreaterThanOrEqual(Size Other) => RequestedSize.Width >= Other.Width && RequestedSize.Height >= Other.Height;
        public bool IsRequestedSizeLessThan(Size Other) => RequestedSize.Width < Other.Width && RequestedSize.Height < Other.Height;
        public bool IsRequestedSizeLessThanOrEqual(Size Other) => RequestedSize.Width <= Other.Width && RequestedSize.Height <= Other.Height;
    }

    /// <param name="PressedScale">A scale to apply to the target element when <see cref="MGElement.IsLMBPressed"/> is true</param>
    /// <param name="HoveredScale">A scale to apply to the target element when <see cref="MGElement.IsHovered"/> is true</param>
    public readonly record struct ConditionalScaleTransform(float PressedScale, float HoveredScale)
    {
        public bool TryGetScale(VisualState VS, out float Scale)
        {
            if (VS.Secondary == SecondaryVisualState.Pressed)
            {
                Scale = PressedScale;
                return true;
            }
            else if (VS.Secondary == SecondaryVisualState.Hovered)
            {
                Scale = HoveredScale;
                return true;
            }
            else
            {
                Scale = 1.0f;
                return false;
            }
        }
    }

    //TODO:
	//tabcontrol - a separate default tabheader style that has like 'floating' tab headers. Just Underlined ContentPresenters instead of a full border. not touching the tabitem content's top border etc
	//		should be easily achieved by just restyling the buttons to only have a bottom border, and have a bottom padding. but we probably also want the selected background to be null, and instead
	//		have a brighter font color when hovered?
	//mgdockpanel - lastchildfill property, default=true. if false, then the actualdockedchildren shouldn't  be different than dockedchildren. when changed, calls layoutchanged
	//something for mouse cursors?
	//		maybe an enum MouseCursorType
	//		and MGElement would have MouseCursorType Cursor property
	//		then MGDesktop defines some textures (and drawing offsets) associated with each MouseCursorType, and bool UseHardwareCursor
	//		If usehardwarecursor=false, then after drawing the desktop, get the hovered element, and draw the texture associated with its MGElement.Cursor
	//something really basic for Gamepads? Maybe just a simple way to 'spoof' a mousestate that's hovering a particular MGElement
	//		like MGElement.NavigateTo, Dictionary<Direction, MGElement> Neighbors. So if you press GamePad Left, it would basically just 
	//		get the hovered element, and call hovered.Neighbors[Left]?.NavigateTo() which returns a new MouseState to use for next update tick? idk
    //textblock inline formatting:
    //      inlined images should have option for render size AND layout size, so you could, for example, have a zero-width image underneath specific text in the textblock
    //      underline should allow specifying a color and stroke thickness, maybe also an offset
    //          [Underline={Color} {Height} {YOffset}]
    //          [Underline=Red 2 3] means to draw a Red underline that's 2px tall, and is moved down by 3px from the default Y position
    //Improve Grid/UniformGrid's default selection graphics
    //Bugfix MGTextBox's Caret positioning after moving to new line such as when inserting a linebreak
    //      only seems incorrect if the textbox's height changes? (I.E. it doesnt have a PreferredHeight and its not inside a ScrollViewer)
    //      it's just the screen position though, the indices in the text seem correct
    //statusbar, menubar/menuitems
    //      messagebox
    //          has icon docked left
    //          button choices like YesNoCancel, OKCancel, or even custom where you can call AddButton(mgelement content) and they are appended in order etc
    //          also 'commandlinks' like taskdialog
    //dialoguebox
    //      subclass of window, but mostly invisible
    //      left is an optional image to display character portrait
    //      rest is a border with a gradientfill background that displays a dockpanel
    //      dockpanel has user choices on the bottom (optional)
    //      rest is a textblock for the character's dialogue
    //maybe a subclass of MGImage for showing animations? Automatically cycles through a set list of textures/sourcerects without invoking LayoutChanged each time
    //		under the assumption each frame of the animation is same size. MGAnimatedImage(bool IsUniform) (if !IsUniform, has to invoke LayoutChanged)
    //maybe MGElement should have a: List<MGElement> AttachedElements { get; }
    //		This would specifically be for elements where the parent doesn't normally have a reference to the child, such as MGResizeGrip when using MGResizeGrip.Host to attach to
    //		The Visual Tree traversal logic should have an additional parameter, IncludeAttached

    /// <summary>Base class for all UI elements.</summary>
    public abstract class MGElement : IMouseHandlerHost, IKeyboardHandlerHost
    {
        public string UniqueId { get; }

		public MGDesktop GetDesktop() => SelfOrParentWindow.Desktop;
        /// <summary>Prioritizes <see cref="MGWindow.Theme"/>. If null, falls back to <see cref="MGDesktop.Theme"/></summary>
        public MGTheme GetTheme() => SelfOrParentWindow.Theme ?? GetDesktop().Theme;

		/// <summary>The <see cref="MGWindow"/> that this <see cref="MGElement"/> belongs to. This value is only null if this <see cref="MGElement"/> is an <see cref="MGWindow"/> with no parent.</summary>
		public MGWindow ParentWindow { get; }
        /// <summary>Returns a reference to 'this' if this is an instance of <see cref="MGWindow"/>. Else returns <see cref="ParentWindow"/></summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public MGWindow SelfOrParentWindow => IsWindow ? this as MGWindow : ParentWindow;

		public static readonly ReadOnlyCollection<MGElementType> WindowElementTypes = new List<MGElementType>() { MGElementType.Window, MGElementType.ToolTip, MGElementType.ContextMenu }.AsReadOnly();
        public MGElementType ElementType { get; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool IsWindow => WindowElementTypes.Contains(ElementType);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private MGElement _Parent;
		public MGElement Parent { get => _Parent; }
		protected internal void SetParent(MGElement Value)
		{
            if (_Parent != Value)
            {
                MGElement Previous = Parent;
                _Parent = Value;
                OnParentChanged?.Invoke(this, new(Previous, Parent));
            }
        }

		public event EventHandler<EventArgs<MGElement>> OnParentChanged;

        /// <summary>The parent <see cref="MGElement"/> that micromanages this <see cref="MGElement"/>, or null if this <see cref="MGElement"/> is not tightly coupled with its creator.<para/>
        /// For example, an <see cref="MGListView{TItemType}"/> always contains an <see cref="MGGrid"/> to display the column headers, and another <see cref="MGGrid"/> to display the rows<br/>
        /// Those 2 <see cref="MGGrid"/>s are specially-created elements whose <see cref="ManagedParent"/> is the <see cref="MGListView{TItemType}"/></summary>
        public MGElement ManagedParent { get; protected internal set; }
        /// <summary>True if this <see cref="MGElement"/> is automatically created and managed by its parent,<br/>
        /// such as a ComboBox's <see cref="MGComboBox{TItemType}.Dropdown"/> or a ContextMenu's <see cref="MGContextMenu.ItemsPanel"/></summary>
        public bool IsManagedElement => ManagedParent != null;

        /// <summary>The parent <see cref="MGElement"/> that this <see cref="MGComponent{TElementType}"/> belongs to, or null if this <see cref="MGElement"/> is not a <see cref="MGComponent{TElementType}"/>.</summary>
        public MGElement ComponentParent { get; private set; }
        /// <summary>True if this <see cref="MGElement"/> is a <see cref="MGComponent{TElementType}"/> of its parent,
        /// such as the <see cref="MGCheckBox.ButtonComponent"/> of an <see cref="MGCheckBox"/>, or the <see cref="MGButton.BorderComponent"/> of an <see cref="MGButton"/></summary>
        public bool IsComponent => ComponentParent != null;

        protected List<MGComponentBase> Components { get; } = new();
		protected virtual void AddComponent(MGComponentBase Component)
		{
			Component.BaseElement.SetParent(this);
            Component.BaseElement.ComponentParent = this;
			Components.Add(Component);
		}

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _Name;
		/// <summary>Optional - can be null. If not null, the <see cref="SelfOrParentWindow"/> will index all child elements by their <see cref="Name"/>, so <see cref="Name"/>s must be unique.</summary>
		public string Name
		{
			get => _Name;
			set
			{
				if (_Name != value)
				{
					string Previous = Name;
					_Name = value;
					OnNameChanged?.Invoke(this, new(Previous, Name));
				}
			}
		}

		public event EventHandler<EventArgs<string>> OnNameChanged;

        #region Margin / Padding
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Thickness _Margin;
		public Thickness Margin
		{
			get => _Margin;
			set
			{
				if (!_Margin.Equals(value))
				{
					_Margin = value;
                    LayoutChanged(this, true);
                }
			}
		}

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Thickness _Padding;
		public Thickness Padding
		{
			get => _Padding;
			set
			{
				if (!_Padding.Equals(value))
				{
					_Padding = value;
                    LayoutChanged(this, true);
                }
			}
		}

        /// <summary>Total width of <see cref="Margin"/> (<see cref="Thickness.Left"/> + <see cref="Thickness.Right"/>)</summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int HorizontalMargin => Margin.Width;
        /// <summary>Total height of <see cref="Margin"/> (<see cref="Thickness.Top"/> + <see cref="Thickness.Bottom"/>)</summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int VerticalMargin => Margin.Height;
        /// <summary>Total size of <see cref="Margin"/><para/>
        /// Width = <see cref="Thickness.Left"/> + <see cref="Thickness.Right"/>;<br/>
        /// Height = <see cref="Thickness.Top"/> + <see cref="Thickness.Bottom"/>;</summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Size MarginSize => Margin.Size;

        /// <summary>Total width of <see cref="Padding"/> (<see cref="Thickness.Left"/> + <see cref="Thickness.Right"/>)</summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int HorizontalPadding => Padding.Width;
        /// <summary>Total height of <see cref="Padding"/> (<see cref="Thickness.Top"/> + <see cref="Thickness.Bottom"/>)</summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int VerticalPadding => Padding.Height;
        /// <summary>Total size of <see cref="Padding"/><para/>
        /// Width = <see cref="Thickness.Left"/> + <see cref="Thickness.Right"/>;<br/>
        /// Height = <see cref="Thickness.Top"/> + <see cref="Thickness.Bottom"/>;</summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Size PaddingSize => Padding.Size;

        /// <summary>Total width of <see cref="Margin"/> + <see cref="Padding"/> (<see cref="Thickness.Left"/> + <see cref="Thickness.Right"/>)</summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int HorizontalMarginAndPadding => HorizontalMargin + HorizontalPadding;
        /// <summary>Total height of <see cref="Margin"/> + <see cref="Padding"/> (<see cref="Thickness.Top"/> + <see cref="Thickness.Bottom"/>)</summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int VerticalMarginAndPadding => VerticalMargin + VerticalPadding;
        /// <summary>Total size of <see cref="Margin"/> + <see cref="Padding"/><para/>
        /// Width = <see cref="Thickness.Left"/> + <see cref="Thickness.Right"/>;<br/>
        /// Height = <see cref="Thickness.Top"/> + <see cref="Thickness.Bottom"/>;</summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Size MarginAndPaddingSize => new(HorizontalMarginAndPadding, VerticalMarginAndPadding);
        #endregion Margin / Padding

        #region Alignment
        public static Rectangle ApplyAlignment(Rectangle Bounds, HorizontalAlignment HA, VerticalAlignment VA, Size Size)
        {
            int StartX = HA switch
            {
                HorizontalAlignment.Left => Bounds.Left,
                HorizontalAlignment.Center => Bounds.Left + (Bounds.Width - Size.Width) / 2,
                HorizontalAlignment.Right => Bounds.Right - Size.Width,
                HorizontalAlignment.Stretch => Bounds.Left,
                _ => throw new NotImplementedException($"Unrecognized {nameof(HorizontalAlignment)}: {HA}"),
            };

            int StartY = VA switch
            {
                VerticalAlignment.Top => Bounds.Top,
                VerticalAlignment.Center => Bounds.Top + (Bounds.Height - Size.Height) / 2,
                VerticalAlignment.Bottom => Bounds.Bottom - Size.Height,
                VerticalAlignment.Stretch => Bounds.Top,
                _ => throw new NotImplementedException($"Unrecognized {nameof(VerticalAlignment)}: {VA}"),
            };

            return new(StartX, StartY, HA == HorizontalAlignment.Stretch ? Bounds.Width : Size.Width, VA == VerticalAlignment.Stretch ? Bounds.Height : Size.Height);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private HorizontalAlignment _HorizontalAlignment;
		public HorizontalAlignment HorizontalAlignment
		{
			get => _HorizontalAlignment;
			set
			{
				if (_HorizontalAlignment != value)
				{
					HorizontalAlignment Previous = HorizontalAlignment;
					_HorizontalAlignment = value;
					OnHorizontalAlignmentChanged?.Invoke(this, new(Previous, HorizontalAlignment));
                    LayoutChanged(this, true);
                }
			}
		}

		protected event EventHandler<EventArgs<HorizontalAlignment>> OnHorizontalAlignmentChanged;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private VerticalAlignment _VerticalAlignment;
		public VerticalAlignment VerticalAlignment
		{
			get => _VerticalAlignment;
			set
			{
				if (_VerticalAlignment != value)
				{
					VerticalAlignment Previous = VerticalAlignment;
                    _VerticalAlignment = value;
                    OnVerticalAlignmentChanged?.Invoke(this, new(Previous, VerticalAlignment));
                    LayoutChanged(this, true);
                }
			}
		}

        protected event EventHandler<EventArgs<VerticalAlignment>> OnVerticalAlignmentChanged;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private HorizontalAlignment _HorizontalContentAlignment;
		public HorizontalAlignment HorizontalContentAlignment
		{
			get => _HorizontalContentAlignment;
			set
			{
				if (_HorizontalContentAlignment != value)
				{
					_HorizontalContentAlignment = value;
                    LayoutChanged(this, true);
                }
			}
		}

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private VerticalAlignment _VerticalContentAlignment;
		public VerticalAlignment VerticalContentAlignment
		{
			get => _VerticalContentAlignment;
			set
			{
				if (_VerticalContentAlignment != value)
				{
					_VerticalContentAlignment = value;
                    LayoutChanged(this, true);
                }
			}
		}
        #endregion Alignment

        #region Size
        #region Min / Max Size
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int? _MinWidth;
        /// <summary>The minimum width of this <see cref="MGElement"/>, not including <see cref="HorizontalMargin"/>, or 0 if null.</summary>
		public int? MinWidth
		{
			get => _MinWidth;
			set
			{
				if (_MinWidth != value)
				{
					_MinWidth = value;
                    LayoutChanged(this, true);
                }
			}
		}

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int? _MinHeight;
        /// <summary>The minimum height of this <see cref="MGElement"/>, not including <see cref="VerticalMargin"/>, or 0 if null.</summary>
        public int? MinHeight
        {
            get => _MinHeight;
            set
            {
                if (_MinHeight != value)
                {
                    _MinHeight = value;
                    LayoutChanged(this, true);
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int? _MaxWidth;
        /// <summary>The maximum width of this <see cref="MGElement"/>, not including <see cref="HorizontalMargin"/>, or <see cref="int.MaxValue"/> if null.</summary>
        public int? MaxWidth
        {
            get => _MaxWidth;
            set
            {
                if (_MaxWidth != value)
                {
                    _MaxWidth = value;
                    LayoutChanged(this, true);
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int? _MaxHeight;
        /// <summary>The maximum height of this <see cref="MGElement"/>, not including <see cref="VerticalMargin"/>, or <see cref="int.MaxValue"/> if null.</summary>
		public int? MaxHeight
		{
			get => _MaxHeight;
			set
			{
				if (_MaxHeight != value)
				{
					_MaxHeight = value;
                    LayoutChanged(this, true);
                }
			}
		}

        /// <summary>Combination of <see cref="MinWidth"/> and <see cref="MinHeight"/>. Uses 0 if value is not specified.<para/>
        /// This value does not include <see cref="Margin"/>.<para/>
        /// See also: <see cref="MinSizeIncludingMargin"/>, <see cref="MaxSize"/>, <see cref="MaxSizeIncludingMargin"/></summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Size MinSize => new(MinWidth ?? 0, MinHeight ?? 0);
        /// <summary>Combination of <see cref="MinWidth"/> and <see cref="MinHeight"/>. Uses 0 if value is not specified.<para/>
        /// This value includes <see cref="Margin"/>.<para/>
        /// See also: <see cref="MinSize"/>, <see cref="MaxSize"/>, <see cref="MaxSizeIncludingMargin"/></summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Size MinSizeIncludingMargin => new(MinWidth.HasValue ? MinWidth.Value + HorizontalMargin : 0, MinHeight.HasValue ? MinHeight.Value + VerticalMargin : 0);

        /// <summary>Combination of <see cref="MinWidth"/> and <see cref="MinHeight"/>. Uses <see cref="int.MaxValue"/> if value is not specified.<para/>
        /// This value does not include <see cref="Margin"/>.<para/>
        /// See also: <see cref="MaxSizeIncludingMargin"/>, <see cref="MinSize"/>, <see cref="MinSizeIncludingMargin"/></summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Size MaxSize => new(MaxWidth ?? int.MaxValue, MaxHeight ?? int.MaxValue);

        /// <summary>Combination of <see cref="MinWidth"/> and <see cref="MinHeight"/>. Uses <see cref="int.MaxValue"/> if value is not specified.<para/>
        /// This value includes <see cref="Margin"/>.<para/>
        /// See also: <see cref="MaxSize"/>, <see cref="MinSize"/>, <see cref="MinSizeIncludingMargin"/></summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Size MaxSizeIncludingMargin => new(MaxWidth.HasValue ? MaxWidth.Value + HorizontalMargin : int.MaxValue, MaxHeight.HasValue ? MaxHeight.Value + VerticalMargin : int.MaxValue);
        #endregion Min / Max Size

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int? _PreferredWidth;
		/// <summary>The requested layout width of this <see cref="MGElement"/>, not including <see cref="HorizontalMargin"/>.<br/>
		/// For the value that includes <see cref="HorizontalMargin"/>, use <see cref="ActualPreferredWidth"/>.<para/>
		/// Not the same as the rendered width if the parent <see cref="MGElement"/> did not have enough available space to allocate to this <see cref="MGElement"/> or if there is a non-zero <see cref="HorizontalMargin"/>.<para/>
		/// See also: <see cref="ActualWidth"/>, <see cref="ActualPreferredWidth"/></summary>
		public int? PreferredWidth
		{
			get => _PreferredWidth;
			set
			{
				if (_PreferredWidth != value)
				{
					if (PreferredWidth.HasValue && PreferredWidth.Value < 0)
						throw new ArgumentOutOfRangeException($"{nameof(MGElement)}.{nameof(PreferredWidth)} cannot be negative.");

					_PreferredWidth = value;
                    LayoutChanged(this, true);
                }
			}
		}

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int? _PreferredHeight;
        /// <summary>The requested layout height of this <see cref="MGElement"/>, not including <see cref="VerticalMargin"/>.<br/>
        /// For the value that includes <see cref="VerticalMargin"/>, use <see cref="ActualPreferredHeight"/>.<para/>
        /// Not the same as the rendered height if the parent <see cref="MGElement"/> did not have enough available space to allocate to this <see cref="MGElement"/> or if there is a non-zero <see cref="VerticalMargin"/>.<para/>
        /// See also: <see cref="ActualHeight"/>, <see cref="ActualPreferredHeight"/></summary>
        public int? PreferredHeight
		{
			get => _PreferredHeight;
			set
			{
				if (_PreferredHeight != value)
				{
                    if (PreferredHeight.HasValue && PreferredHeight.Value < 0)
                        throw new ArgumentOutOfRangeException($"{nameof(MGElement)}.{nameof(PreferredHeight)} cannot be negative.");

                    _PreferredHeight = value;
                    LayoutChanged(this, true);
                }
			}
		}

        /// <summary>Same as <see cref="PreferredWidth"/>, except this includes the <see cref="HorizontalMargin"/></summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int? ActualPreferredWidth => PreferredWidth.HasValue ? Math.Max(0, PreferredWidth.Value + HorizontalMargin) : PreferredWidth;
        /// <summary>Same as <see cref="PreferredHeight"/>, except this includes the <see cref="VerticalMargin"/></summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int? ActualPreferredHeight => PreferredHeight.HasValue ? Math.Max(0, PreferredHeight.Value + VerticalMargin) : PreferredHeight;
        #endregion Size

        #region ToolTip
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private MGToolTip _ToolTip;
		public MGToolTip ToolTip
		{
			get => _ToolTip;
			set
			{
				if (_ToolTip != value)
				{
					MGToolTip Previous = ToolTip;
					_ToolTip = value;
					ToolTipChanged?.Invoke(this, new(Previous, ToolTip));
				}
			}
		}

		public event EventHandler<EventArgs<MGToolTip>> ToolTipChanged;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsToolTipShowing => GetDesktop().ActiveToolTip == ToolTip;
        #endregion ToolTip

        #region ContextMenu
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private MGContextMenu _ContextMenu;
		public MGContextMenu ContextMenu
		{
			get => _ContextMenu;
			set
			{
				if (_ContextMenu != value)
				{
					MGContextMenu Previous = ContextMenu;
					_ContextMenu = value;
					ContextMenuChanged?.Invoke(this, new(Previous, ContextMenu));
				}
			}
		}

		/// <summary>Invoked when <see cref="ContextMenu"/> is set to a new value. (Not invoked when the content within <see cref="ContextMenu"/> is modified)<para/>
		/// See also: <see cref="MGDesktop.ActiveContextMenu"/>, <see cref="MGDesktop.ContextMenuClosing"/>, <see cref="MGDesktop.ContextMenuClosed"/>, <see cref="MGDesktop.ContextMenuOpening"/>, <see cref="MGDesktop.ContextMenuOpened"/></summary>
        public event EventHandler<EventArgs<MGContextMenu>> ContextMenuChanged;
        #endregion ContextMenu

        #region Input
        protected InputTracker InputTracker => GetDesktop().InputTracker;
		public MouseHandler MouseHandler { get; }
        public KeyboardHandler KeyboardHandler { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsHovered => MouseHandler.IsHovered;
        /// <summary>True if <see cref="MouseButton.Left"/> is currently pressed overtop of this <see cref="MGElement"/></summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsLMBPressed => MouseHandler.IsButtonPressedInside(MouseButton.Left);

        protected IMouseViewport AsIViewport() => this;
        protected IMouseHandlerHost AsIMouseHandlerHost() => this;
        bool IMouseViewport.IsInside(Vector2 Position) => 
            ActualLayoutBounds.ContainsInclusive(ConvertCoordinateSpace(CoordinateSpace.Screen, CoordinateSpace.UnscaledScreen, Position)) &&
            GetDesktop().ValidScreenBounds.ContainsInclusive(Position);

        Vector2 IMouseViewport.GetOffset() => Vector2.Zero;

		protected bool _CanReceiveMouseInput { get; private set; }
		bool IMouseHandlerHost.CanReceiveMouseInput() => _CanReceiveMouseInput;

        public virtual bool CanHandleKeyboardInput { get => false; }
        protected bool _CanReceiveKeyboardInput { get; private set; }
		bool IKeyboardHandlerHost.CanReceiveKeyboardInput() => CanHandleKeyboardInput && _CanReceiveKeyboardInput;

		bool IKeyboardHandlerHost.HasKeyboardFocus() => GetDesktop().FocusedKeyboardHandler == this;

        public bool CanHandleInputsWhileHidden { get; internal protected set; } = false;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _IsHitTestVisible;
        /// <summary>True if this <see cref="MGElement"/> should be allowed to detect and handle mouse and keyboard inputs.<para/>
        /// This property does not account for the parent's <see cref="IsHitTestVisible"/>. Consider using <see cref="DerivedIsHitTestVisible"/> instead.</summary>
        public bool IsHitTestVisible
        {
            get => _IsHitTestVisible && (Visibility == Visibility.Visible || (CanHandleInputsWhileHidden && Visibility == Visibility.Hidden));
            set => _IsHitTestVisible = value;
        }
        /// <summary>True if this <see cref="MGElement"/> should be allowed to detect and handle mouse and keyboard inputs.<para/>
        /// This property is only true if both this <see cref="MGElement"/> and every parent along the visual tree have <see cref="IsHitTestVisible"/>==true.</summary>
        public bool DerivedIsHitTestVisible => IsHitTestVisible && (Parent?.IsHitTestVisible ?? IsHitTestVisible);

        /// <summary>If true, <see cref="IsLMBPressed"/> will be evaluated as true while drawing this element's background, regardless of the real MouseState.</summary>
        internal protected bool SpoofIsPressedWhileDrawingBackground { get; set; } = false;
        /// <summary>If true, <see cref="IsHovered"/> will be evaluated as true while drawing this element's background, regardless of the real MouseState.</summary>
        internal protected bool SpoofIsHoveredWhileDrawingBackground { get; set; } = false;
        #endregion Input

        private VisualState _VisualState = new(PrimaryVisualState.Normal, SecondaryVisualState.None);
        public VisualState VisualState
        {
            get => _VisualState;
            private set
            {
                if (_VisualState != value)
                {
                    VisualState Previous = VisualState;
                    _VisualState = value;
                    VisualStateChanged?.Invoke(this, new(Previous, VisualState));
                }
            }
        }

        /// <summary>Invoked when <see cref="VisualState"/> changes.</summary>
        public event EventHandler<EventArgs<VisualState>> VisualStateChanged;

        /// <summary>This property does not account for the parent's <see cref="IsSelected"/>. Consider using <see cref="DerivedIsSelected"/> instead.</summary>
        public bool IsSelected { get; set; } = false;
        /// <summary>This property is only false if both this <see cref="MGElement"/> and every parent along the visual tree have <see cref="IsSelected"/>==false.</summary>
        public bool DerivedIsSelected => IsSelected || (Parent?.DerivedIsSelected ?? IsSelected);

        /// <summary>This property does not account for the parent's <see cref="IsEnabled"/>. Consider using <see cref="DerivedIsEnabled"/> instead.</summary>
        public bool IsEnabled { get; set; } = true;
        /// <summary>This property is only true if both this <see cref="MGElement"/> and every parent along the visual tree have <see cref="IsEnabled"/>==true.</summary>
        public bool DerivedIsEnabled => IsEnabled && (Parent?.DerivedIsEnabled ?? IsEnabled);

        /// <summary>The <see cref="IFillBrush"/>es to use for this <see cref="MGElement"/>'s background, depending on the current <see cref="VisualState"/>.<para/>
        /// See also: <see cref="BackgroundUnderlay"/>, <see cref="BackgroundOverlay"/></summary>
        public VisualStateFillBrush BackgroundBrush { get; set; }
        /// <summary>The first <see cref="IFillBrush"/> used to draw this <see cref="MGElement"/>'s background. Drawn before <see cref="BackgroundOverlay"/></summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public IFillBrush BackgroundUnderlay => BackgroundBrush.GetUnderlay(VisualState.Primary);
        /// <summary>The second <see cref="IFillBrush"/> used to draw this <see cref="MGElement"/>'s background. Drawn after <see cref="BackgroundUnderlay"/></summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public IFillBrush BackgroundOverlay => BackgroundBrush.GetFillOverlay(VisualState.Secondary);

        /// <summary>The default foreground color to use when rendering text on this <see cref="MGElement"/> or any of its child elements.<br/>
        /// For a value that accounts for the parent's <see cref="DefaultTextForeground"/>, use <see cref="DerivedDefaultTextForeground"/> instead.<para/>
        /// See also: <see cref="DerivedDefaultTextForeground"/>, <see cref="CurrentDefaultTextForeground"/></summary>
        public VisualStateSetting<Color?> DefaultTextForeground { get; set; }
        /// <summary>The currently-active value from <see cref="DefaultTextForeground"/>, based on <see cref="VisualState"/></summary>
        public Color? CurrentDefaultTextForeground => DefaultTextForeground.GetValue(VisualState.Primary);
        /// <summary>This property prioritizes <see cref="CurrentDefaultTextForeground"/> if it has a value.<br/>
        /// Else traverses up the visual tree until finding the first non-null <see cref="CurrentDefaultTextForeground"/>.</summary>
        public Color? DerivedDefaultTextForeground => CurrentDefaultTextForeground ?? Parent?.DerivedDefaultTextForeground;

        //TODO VisualStateFillBrush OverlayBrush, drawn after DrawSelf and DrawContents
        //When drawing the overlay, draws OverlayBrush?.GetUnderlay(VisualState.Primary), then OverlayBrush?.GetOverlay(VisualState.Secondary)
        //Add a new event to Draw: OnBeginDrawOverlay. Look for anything that subscribes to OnEndDraw, probably change them all to OnBeginDrawOverlay
        //in MGElement Constructor, set OverlayBrush = new(null, null, null, ...);



        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Visibility _Visibility;
        public Visibility Visibility
        {
            get => _Visibility;
            set
            {
                if (_Visibility != value)
                {
                    Visibility Previous = Visibility;
                    _Visibility = value;
                    if (Previous == Visibility.Collapsed || Visibility == Visibility.Collapsed)
                        LayoutChanged(this, true);
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsVisibilityCollapsed => Visibility == Visibility.Collapsed;

        /// <summary>This property does not account for the parent's <see cref="Opacity"/>.</summary>
        public float Opacity { get; set; } = 1.0f;

        /// <summary>General-purpose dictionary to attach your own data to this <see cref="MGElement"/></summary>
        public Dictionary<string, object> Metadata { get; } = new();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DeferEventsManager InitializationManager { get; }
		/// <summary>To improve performance, consider invoking this method during constructor initialization.<para/>
		/// This will defer layout updates until the <see cref="DeferEventsTransaction"/> is disposed.</summary>
		protected internal DeferEventsTransaction BeginInitializing() => InitializationManager.DeferEvents();

        protected MGElement(MGWindow ParentWindow, MGElementType ElementType)
			: this(ParentWindow.Desktop, ParentWindow, ElementType)
		{

		}

        protected MGElement(MGDesktop Desktop, MGWindow ParentWindow, MGElementType ElementType, MGTheme Theme = null)
		{
			this.InitializationManager = new(() => { LayoutChanged(this, true); });
			using (BeginInitializing())
			{
				this.UniqueId = Guid.NewGuid().ToString();
                this.ParentWindow = ParentWindow;
                this.ElementType = ElementType;

                MGTheme ActualTheme = Theme ?? ParentWindow?.Theme ?? Desktop.Theme;

                this.MouseHandler = Desktop.InputTracker.Mouse.CreateHandler(this, null);
                this.KeyboardHandler = Desktop.InputTracker.Keyboard.CreateHandler(this, null);

                this.Margin = new(0);
				this.Padding = new(0);

				this.HorizontalAlignment = HorizontalAlignment.Stretch;
				this.VerticalAlignment = VerticalAlignment.Stretch;
				this.HorizontalContentAlignment = HorizontalAlignment.Stretch;
				this.VerticalContentAlignment = VerticalAlignment.Stretch;

                this.BackgroundBrush = ActualTheme.GetBackgroundBrush(ElementType);
                this.DefaultTextForeground = new VisualStateSetting<Color?>(null, null, null);

                this.Visibility = Visibility.Visible;
                this.IsEnabled = true;
                this.IsSelected = false;
				this.IsHitTestVisible = true;

                SelfOrParentWindow.OnWindowPositionChanged += (sender, e) =>
				{
#if NEVER
					InvalidateLayout();
#else
					Rectangle PreviousLayoutBounds = LayoutBounds;

					Point Offset = new(e.NewValue.Left - e.PreviousValue.Left, e.NewValue.Top - e.PreviousValue.Top);
					this.AllocatedBounds = AllocatedBounds.GetTranslated(Offset);
					this.RenderBounds = RenderBounds.GetTranslated(Offset);
					this.LayoutBounds = LayoutBounds.GetTranslated(Offset);
					this.StretchedContentBounds = StretchedContentBounds.GetTranslated(Offset);
					this.AlignedContentBounds = AlignedContentBounds.GetTranslated(Offset);

					OnLayoutBoundsChanged?.Invoke(this, new(PreviousLayoutBounds, LayoutBounds));
#endif
				};

				this.MouseHandler.RMBReleasedInside += (sender, e) =>
				{
					if (ContextMenu?.TryOpenContextMenu(e.Position) == true)
					{
						e.SetHandled(ContextMenu, false);
					}
				};
            }
		}

		public override string ToString() => string.IsNullOrEmpty(Name) ?
            $"{ElementType}: {GetType().Name} ({RenderBounds})" :
            $"'{Name}' - {ElementType}: {GetType().Name} ({RenderBounds})";

		public virtual IEnumerable<MGElement> GetChildren() => Enumerable.Empty<MGElement>();

        public virtual MGBorder GetBorder() => null;
        public bool HasBorder => GetBorder() != null;

        #region Bounds
        protected internal Matrix GetTransform(CoordinateSpace From, CoordinateSpace To)
        {
            if (From == To)
                return Matrix.Identity;
            else
                return From switch
                {
                    CoordinateSpace.Layout => To switch
                    {
                        CoordinateSpace.Layout => Matrix.Identity,
                        CoordinateSpace.UnscaledScreen => Matrix.CreateTranslation(-Origin.X, -Origin.Y, 0),
                        CoordinateSpace.Screen => Matrix.CreateTranslation(-Origin.X, -Origin.Y, 0) * SelfOrParentWindow.UnscaledScreenSpaceToScaledScreenSpace,
                        _ => throw new NotImplementedException($"Unrecognized {nameof(CoordinateSpace)}: {To}")
                    },
                    CoordinateSpace.UnscaledScreen => To switch
                    {
                        CoordinateSpace.Layout => Matrix.CreateTranslation(Origin.X, Origin.Y, 0),
                        CoordinateSpace.UnscaledScreen => Matrix.Identity,
                        CoordinateSpace.Screen => SelfOrParentWindow.UnscaledScreenSpaceToScaledScreenSpace,
                        _ => throw new NotImplementedException($"Unrecognized {nameof(CoordinateSpace)}: {To}")
                    },
                    CoordinateSpace.Screen => To switch
                    {
                        CoordinateSpace.Layout => SelfOrParentWindow.ScaledScreenSpaceToUnscaledScreenSpace * Matrix.CreateTranslation(Origin.X, Origin.Y, 0),
                        CoordinateSpace.UnscaledScreen => SelfOrParentWindow.ScaledScreenSpaceToUnscaledScreenSpace,
                        CoordinateSpace.Screen => Matrix.Identity,
                        _ => throw new NotImplementedException($"Unrecognized {nameof(CoordinateSpace)}: {To}")
                    },
                    _ => throw new NotImplementedException($"Unrecognized {nameof(CoordinateSpace)}: {From}")
                };
        }

        public Rectangle ConvertCoordinateSpace(CoordinateSpace From, CoordinateSpace To, Rectangle Value)
        {
            if (From == To)
                return Value;
            else
            {
                Matrix Transform = GetTransform(From, To);
                return Value.CreateTransformedF(Transform).RoundUp();
            }
        }

        public Vector2 ConvertCoordinateSpace(CoordinateSpace From, CoordinateSpace To, Vector2 Value)
        {
            if (From == To)
                return Value;
            else
            {
                Matrix Transform = GetTransform(From, To);
                return Value.TransformBy(Transform);
            }
        }

        public Point ConvertCoordinateSpace(CoordinateSpace From, CoordinateSpace To, Point Value) => ConvertCoordinateSpace(From, To, Value.ToVector2()).ToPoint();

        public virtual bool ClipToBounds { get; set; } = true;

        /// <summary>This value is typically <see cref="Point.Zero"/> except when this <see cref="MGElement"/> is a child of an <see cref="MGScrollViewer"/>,<br/>
        /// in which case the origin would be based on the <see cref="MGScrollViewer.HorizontalOffset"/> / <see cref="MGScrollViewer.VerticalOffset"/></summary>
        public Point Origin { get; private set; } = Point.Zero;

        /// <summary>The screen space that this element is rendered to.<para/>
        /// Unlike <see cref="LayoutBounds"/>, this value always uses an origin of <see cref="Point.Zero"/>, rather than being relative to <see cref="Origin"/>,<br/>
        /// and also accounts for <see cref="ClipToBounds"/> by intersecting the bounds with the parent's <see cref="ActualLayoutBounds"/>.<para/>
        /// This value will show Width=0/Height=0 for elements that are outside the visible viewport, even if they've technically been allocated non-zero dimensions.<para/>
        /// See also: <see cref="LayoutBounds"/>, <see cref="Origin"/></summary>
        public Rectangle ActualLayoutBounds { get; private set; }

        /// <summary>The screen space that was allocated to this <see cref="MGElement"/>.<br/>
        /// This <see cref="MGElement"/> may choose not to use all of the allocated space based on <see cref="HorizontalAlignment"/> and <see cref="VerticalAlignment"/>.<para/>
        /// This value does not account for <see cref="Origin"/> and thus might not match the exact screen bounds where this <see cref="MGElement"/> was drawn.<para/>
        /// See also: <br/><see cref="AllocatedBounds"/><br/><see cref="RenderBounds"/><br/><see cref="LayoutBounds"/><br/><see cref="StretchedContentBounds"/><br/>
        /// <see cref="AlignedContentBounds"/><br/><see cref="ActualLayoutBounds"/></summary>
        public Rectangle AllocatedBounds { get; private set; }

        /// <summary>The screen space that this <see cref="MGElement"/> will render itself to.<br/>
        /// This value accounts for <see cref="HorizontalAlignment"/> and <see cref="VerticalAlignment"/>.<para/>
		/// This value does not account for <see cref="Origin"/> and thus might not match the exact screen bounds where this <see cref="MGElement"/> was drawn.<para/>
        /// See also: <br/><see cref="AllocatedBounds"/><br/><see cref="RenderBounds"/><br/><see cref="LayoutBounds"/><br/><see cref="StretchedContentBounds"/><br/>
        /// <see cref="AlignedContentBounds"/><br/><see cref="ActualLayoutBounds"/></summary>
        public Rectangle RenderBounds { get; private set; }

        /// <summary>The screen space that this <see cref="MGElement"/> will render itself to, after accounting for <see cref="Margin"/>.<br/>
        /// The <see cref="BackgroundBrush"/> of this <see cref="MGElement"/> spans these bounds.<para/>
		/// This value does not account for <see cref="Origin"/> and thus might not match the exact screen bounds where this <see cref="MGElement"/> was drawn.<para/>
        /// See also: <br/><see cref="AllocatedBounds"/><br/><see cref="RenderBounds"/><br/><see cref="LayoutBounds"/><br/><see cref="StretchedContentBounds"/><br/>
        /// <see cref="AlignedContentBounds"/><br/><see cref="ActualLayoutBounds"/></summary>
        public Rectangle LayoutBounds { get; private set; }

        /// <summary>The screen space that this <see cref="MGElement"/>'s contents will render to, before accounting for <see cref="HorizontalContentAlignment"/> and <see cref="VerticalContentAlignment"/>.<br/>
        /// This value accounts for <see cref="Margin"/>, <see cref="Padding"/>, and any other properties that affect this <see cref="MGElement"/>'s size (such as BorderThickness of a <see cref="MGBorder"/>).<para/>
		/// This value does not account for <see cref="Origin"/> and thus might not match the exact screen bounds where this <see cref="MGElement"/> was drawn.<para/>
        /// See also: <br/><see cref="AllocatedBounds"/><br/><see cref="RenderBounds"/><br/><see cref="LayoutBounds"/><br/><see cref="StretchedContentBounds"/><br/>
        /// <see cref="AlignedContentBounds"/><br/><see cref="ActualLayoutBounds"/></summary>
        public Rectangle StretchedContentBounds { get; private set; }

        /// <summary>The screen space that this <see cref="MGElement"/>'s contents will render to, after accounting for <see cref="HorizontalContentAlignment"/> and <see cref="VerticalContentAlignment"/>.<br/>
        /// This value accounts for <see cref="Margin"/>, <see cref="Padding"/>, and any other properties that affect this <see cref="MGElement"/>'s size (such as BorderThickness of a <see cref="MGBorder"/>).<para/>
		/// This value does not account for <see cref="Origin"/> and thus might not match the exact screen bounds where this <see cref="MGElement"/> was drawn.<para/>
        /// See also: <br/><see cref="AllocatedBounds"/><br/><see cref="RenderBounds"/><br/><see cref="LayoutBounds"/><br/><see cref="StretchedContentBounds"/><br/>
        /// <see cref="AlignedContentBounds"/><br/><see cref="ActualLayoutBounds"/></summary>
        public Rectangle AlignedContentBounds { get; private set; }

        /// <summary>Note: This value does not include <see cref="Margin"/>. For a value with <see cref="Margin"/>, consider using <see cref="RenderBounds"/>.Width</summary>
        public int ActualWidth => LayoutBounds.Width;
        /// <summary>Note: This value does not include <see cref="Margin"/>. For a value with <see cref="Margin"/>, consider using <see cref="RenderBounds"/>.Height</summary>
        public int ActualHeight => LayoutBounds.Height;
        #endregion Bounds

        #region Update
        [DebuggerStepThrough]
        public class ElementUpdateEventArgs : EventArgs
        {
			public readonly MGElement Element;
            public readonly ElementUpdateArgs UA;

            public ElementUpdateEventArgs(MGElement Element, ElementUpdateArgs UA)
            {
				this.Element = Element;
                this.UA = UA;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DateTime? HoverStartTime = null;

        public void Update(ElementUpdateArgs UA)
		{ 
            bool ComputedIsEnabled = UA.IsEnabled && this.IsEnabled;
            bool ComputedIsSelected = UA.IsSelected || this.IsSelected;
            bool ComputedIsHitTestVisible = UA.IsHitTestVisible && this.IsHitTestVisible;

            this.Origin = UA.Offset;

            Rectangle UnscaledScreenBounds = ConvertCoordinateSpace(CoordinateSpace.Layout, CoordinateSpace.UnscaledScreen, LayoutBounds);
            //TODO there's a bug with ActualLayoutBounds that I'm too lazy to fix:
            //Rectangle.Intersect(Parent.ActualLayoutBounds, this.LayoutBounds.GetTranslated(-UA.Offset)) does NOT properly account for the parent's Padding.
            //We can't simply compress the ActualLayoutBounds by the parent's Padding because not all element's pad all of their children.
            //For example, an MGTabControl's padding isn't applied to the HeadersPanel that hosts the TabControl's Tab headers.
#if true
            if (IsWindow)
                this.ActualLayoutBounds = UnscaledScreenBounds;
            else
                this.ActualLayoutBounds = Rectangle.Intersect(UA.ActualLayoutBounds, UnscaledScreenBounds);
#else
            Rectangle ParentLayoutBounds = IsWindow ? GetDesktop().ValidScreenBounds : UA.ActualLayoutBounds;
            this.ActualLayoutBounds = Rectangle.Intersect(ParentLayoutBounds, UnscaledScreenBounds);
#endif

            UA = UA with {
                IsEnabled = ComputedIsEnabled, 
                IsSelected = ComputedIsSelected, 
                IsHitTestVisible = ComputedIsHitTestVisible, 
                ActualLayoutBounds = this.ActualLayoutBounds
            };

            PrimaryVisualState PrimaryVisualState = !ComputedIsEnabled ? PrimaryVisualState.Disabled : ComputedIsSelected ? PrimaryVisualState.Selected : PrimaryVisualState.Normal;
            SecondaryVisualState SecondaryVisualState = !ComputedIsHitTestVisible || SelfOrParentWindow.HasModalWindow ? SecondaryVisualState.None : IsLMBPressed ? SecondaryVisualState.Pressed : IsHovered ? SecondaryVisualState.Hovered : SecondaryVisualState.None;
            this.VisualState = new(PrimaryVisualState, SecondaryVisualState);

            ElementUpdateEventArgs UpdateEventArgs = new(this, UA);

            OnBeginUpdate?.Invoke(this, UpdateEventArgs);

			if (ComputedIsHitTestVisible && Visibility == Visibility.Visible && IsHovered)
				HoverStartTime ??= DateTime.Now;
			else
				HoverStartTime = null;

			if (!RecentDrawWasClipped && HoverStartTime.HasValue && !SelfOrParentWindow.HasModalWindow)
			{
				TimeSpan HoveredTime = DateTime.Now.Subtract(HoverStartTime.Value);
                if (HoveredTime >= GetDesktop().ToolTipShowDelay)
				{
                    if (!TryGetToolTip(out MGToolTip ToolTip))
                        ToolTip = this.ToolTip;

                    if (ToolTip != null && (ComputedIsEnabled || ToolTip.ShowOnDisabled))
                        GetDesktop().QueuedToolTip = ToolTip;
                }
			}

			bool BaseCanReceiveInput = (Visibility == Visibility.Visible || (Visibility == Visibility.Hidden && CanHandleInputsWhileHidden)) && ComputedIsEnabled && ComputedIsHitTestVisible
				&& (!RecentDrawWasClipped || (Visibility == Visibility.Hidden && CanHandleInputsWhileHidden));
            this._CanReceiveMouseInput = BaseCanReceiveInput && (Parent?._CanReceiveMouseInput ?? true);
			this._CanReceiveKeyboardInput = BaseCanReceiveInput && (Parent?._CanReceiveKeyboardInput ?? true);

            if (_CanReceiveMouseInput && SecondaryVisualState == SecondaryVisualState.Hovered && (ElementType != MGElementType.Border || !IsComponent) && this is not MGMultiContentHost)
                SelfOrParentWindow.HoveredElement = this;

            OnBeginUpdateContents?.Invoke(this, UpdateEventArgs);
			foreach (MGElement Component in Components.Where(x => x.UpdateBeforeContents).Select(x => x.BaseElement))
				Component.Update(UA);
			UpdateContents(UA);
            foreach (MGElement Component in Components.Where(x => x.UpdateAfterContents).Select(x => x.BaseElement))
                Component.Update(UA);
            OnEndUpdateContents?.Invoke(this, UpdateEventArgs);

            if (ComputedIsHitTestVisible)
			{
				MouseHandler.ManualUpdate();
				KeyboardHandler.ManualUpdate();
			}
            UpdateSelf(UA);

            OnEndUpdate?.Invoke(this, UpdateEventArgs);
        }

        protected virtual bool TryGetToolTip(out MGToolTip ToolTip)
        {
            ToolTip = null;
            return false;
        }

		protected virtual void UpdateContents(ElementUpdateArgs UA)
        {
            foreach (MGElement Child in GetVisualTreeChildren().Reverse().ToList())
                Child.Update(UA);
        }

        public event EventHandler<ElementUpdateEventArgs> OnBeginUpdate;
        public event EventHandler<ElementUpdateEventArgs> OnEndUpdate;
        public event EventHandler<ElementUpdateEventArgs> OnBeginUpdateContents;
        public event EventHandler<ElementUpdateEventArgs> OnEndUpdateContents;

		public virtual void UpdateSelf(ElementUpdateArgs UA) { }
        #endregion Update

        #region Draw
        /// <summary>A scale transform to apply to this <see cref="MGElement"/> when the appropriate condition is met. (such as <see cref="IsLMBPressed"/> is true)<para/>
        /// This scale transform only affects how the element is rendered, but not its layout, so it may result in overlapping elements. Uses the center of <see cref="LayoutBounds"/> as the scaling origin.<para/>
        /// Default value: null</summary>
        public ConditionalScaleTransform? RenderScale { get; set; }

		/// <summary>True if the most recent Draw call resulted in this <see cref="MGElement"/> not being drawn, due to one of the following reasons:<para/>
		/// It was out-of-bounds (See: <see cref="ClipToBounds"/>, <see cref="GraphicsDevice.ScissorRectangle"/>, <see cref="RasterizerState.ScissorTestEnable"/>)<br/>
		/// It was not visible (See: <see cref="Visibility"/>)<br/>
		/// It had no geometry (See: <see cref="LayoutBounds"/>, Width and/or Height = 0)</summary>
		public bool RecentDrawWasClipped { get; private set; } = false;

        public virtual void Draw(ElementDrawArgs DA)
		{
			RecentDrawWasClipped = false;

            DA = DA.SetOpacity(DA.Opacity * this.Opacity) with { VisualState = this.VisualState };
            MGElementDrawEventArgs DrawEventArgs = new(DA);

			OnBeginDraw?.Invoke(this, DrawEventArgs);

            if (Visibility != Visibility.Visible || LayoutBounds.Width <= 0 || LayoutBounds.Height <= 0)
			{
				RecentDrawWasClipped = true;
				return;
			}

            Rectangle TargetBounds = LayoutBounds.GetTranslated(DA.Offset).CreateTransformedF(DA.DT.CurrentSettings.Transform).RoundUp();

            //  Apply render scale, if any
            IDisposable TempTransform = null;
            IDisposable TempScissorRect = null;
            if (RenderScale?.TryGetScale(VisualState, out float Scale) == true)
            {
                Matrix Transform =
                    Matrix.CreateTranslation(new Vector3(-TargetBounds.Center.ToVector2(), 0)) *
                    Matrix.CreateScale(Scale) *
                    Matrix.CreateTranslation(new Vector3(TargetBounds.Center.ToVector2(), 0));
                TempTransform = DA.DT.SetTransformTemporary(DA.DT.CurrentSettings.Transform * Transform);
                TargetBounds = TargetBounds.CreateTransformedF(Transform).RoundUp();
                if (DA.DT.CurrentSettings.RasterizerState.ScissorTestEnable)
                {
                    Rectangle NewScissorBounds = DA.DT.GD.ScissorRectangle.CreateTransformedF(Transform).RoundUp();
                    TempScissorRect = DA.DT.SetClipTargetTemporary(NewScissorBounds, false);
                }
            }

            if (!ClipToBounds || !DA.DT.CurrentSettings.RasterizerState.ScissorTestEnable || TargetBounds.Intersects(DA.DT.GD.ScissorRectangle))
			{
				using (ClipToBounds ? DA.DT.SetClipTargetTemporary(TargetBounds, true) : null)
				{
                    foreach (MGElement Component in Components.Where(x => x.DrawBeforeBackground).Select(x => x.BaseElement))
                        Component.Draw(DA);

                    DrawBackground(DA, LayoutBounds);

                    foreach (MGElement Component in Components.Where(x => x.DrawBeforeSelf).Select(x => x.BaseElement))
                        Component.Draw(DA);

                    DrawSelf(DA, LayoutBounds);

					foreach (MGElement Component in Components.Where(x => x.DrawBeforeContents).Select(x => x.BaseElement))
						Component.Draw(DA);

                    DrawContents(DA);

                    foreach (MGElement Component in Components.Where(x => x.DrawAfterContents).Select(x => x.BaseElement))
                        Component.Draw(DA);

                    OnEndingDraw?.Invoke(this, DrawEventArgs);
                }
            }
            else
			{
				RecentDrawWasClipped = true;
			}

            TempTransform?.Dispose();
            TempScissorRect?.Dispose();

            OnEndDraw?.Invoke(this, DrawEventArgs);
		}

		protected virtual void DrawContents(ElementDrawArgs DA) { }

        [DebuggerStepThrough]
        public class MGElementDrawEventArgs : EventArgs
		{
			public readonly ElementDrawArgs DA;

            public MGElementDrawEventArgs(ElementDrawArgs DA)
			{
                this.DA = DA;
			}
		}

		public event EventHandler<MGElementDrawEventArgs> OnBeginDraw;
        /// <summary>Invoked after drawing the background, self, and all components, 
        /// but while the <see cref="GraphicsDevice.ScissorRectangle"/> is still set to the desired screen-space bounds of this element.<para/>
        /// See also: <see cref="OnEndDraw"/></summary>
        public event EventHandler<MGElementDrawEventArgs> OnEndingDraw;
        /// <summary>Invoked at the very end of <see cref="MGElement.Draw(ElementDrawArgs)"/>.<br/>
        /// Unlike <see cref="OnEndingDraw"/>, this event is invoked AFTER the <see cref="GraphicsDevice.ScissorRectangle"/> has been reverted to its prior value.<para/>
        /// See also: <see cref="OnEndingDraw"/></summary>
		public event EventHandler<MGElementDrawEventArgs> OnEndDraw;

        protected void DrawBackground(ElementDrawArgs DA) => DrawBackground(DA, this.LayoutBounds);

        public virtual void DrawBackground(ElementDrawArgs DA, Rectangle LayoutBounds)
		{
            Rectangle BorderlessBounds = !HasBorder ? LayoutBounds : LayoutBounds.GetCompressed(GetBorder().BorderThickness);
            BackgroundBrush.GetUnderlay(DA.VisualState.Primary)?.Draw(DA, this, BorderlessBounds);
            SecondaryVisualState SecondaryState = DA.VisualState.GetSecondaryState(SpoofIsPressedWhileDrawingBackground, SpoofIsHoveredWhileDrawingBackground);
            BackgroundBrush.GetFillOverlay(SecondaryState)?.Draw(DA, this, BorderlessBounds);
        }

        public virtual void DrawSelf(ElementDrawArgs DA, Rectangle LayoutBounds) 
            => DrawSelfBaseImplementation(DA, LayoutBounds);

        protected void DrawSelfBaseImplementation(ElementDrawArgs DA, Rectangle LayoutBounds)
        {
            if (HasBorder)
                BackgroundBrush.GetBorderOverlay(DA.VisualState.Secondary)?.Draw(DA, this, LayoutBounds, GetBorder().BorderThickness);
        }
        #endregion Draw

        #region Layout
		internal protected void InvalidateLayout()
		{
            IsLayoutValid = false;
            RecentMeasurementsSelfOnly.Clear();
            RecentMeasurementsFull.Clear();
        }

        /// <summary>Invoked when a property that affects this <see cref="MGElement"/>'s layout has changed, such as <see cref="Padding"/>, <see cref="Margin"/>, or its content.</summary>
        protected virtual void LayoutChanged(MGElement Source, bool NotifyParent)
        {
			if (InitializationManager.IsDeferringEvents)
				return;

            if (IsUpdatingLayout)
                SelfOrParentWindow.QueueLayoutRefresh = true;

            InvalidateLayout();
            if (NotifyParent)
                Parent?.LayoutChanged(Source, NotifyParent);
        }

        /// <summary>If true, indicates that the layout will be re-calculated during the next update tick. This also invalidates any cached measurements.</summary>
        public bool IsLayoutValid { get; private set; }

        #region Arrange
        protected internal bool IsUpdatingLayout { get; private set; }

        internal protected void UpdateLayout(Rectangle Bounds)
        {
            try
            {
                IsUpdatingLayout = true;

                if (!IsLayoutValid)
                {
                    RecentMeasurementsSelfOnly.Clear();
                    RecentMeasurementsFull.Clear();
                }

                Rectangle PreviousLayoutBounds = this.LayoutBounds;

                if (Bounds.Width <= 0 || Bounds.Height <= 0)
                {
                    this.AllocatedBounds = Rectangle.Empty;
                    this.RenderBounds = Rectangle.Empty;
                    this.LayoutBounds = Rectangle.Empty;
                    this.StretchedContentBounds = Rectangle.Empty;
                    this.AlignedContentBounds = Rectangle.Empty;
                }
                else
                {
                    this.AllocatedBounds = Bounds;

                    Size BoundsSize = new(Bounds.Width, Bounds.Height);
                    Thickness RequestedSelfSize;
                    Thickness RequestedFullSize;
                    Thickness SharedSize;
                    Thickness RequestedContentSize;
                    if (TryGetCachedMeasurement(BoundsSize, out ElementMeasurement SelfMeasurement, out ElementMeasurement FullMeasurement))
                    {
                        RequestedSelfSize = SelfMeasurement.RequestedSize;
                        RequestedFullSize = FullMeasurement.RequestedSize;
                        SharedSize = SelfMeasurement.SharedSize;
                        RequestedContentSize = FullMeasurement.ContentSize;
                    }
                    else
                    {
                        UpdateMeasurement(BoundsSize, out RequestedSelfSize, out RequestedFullSize, out SharedSize, out RequestedContentSize);
                    }

                    int ConsumedWidth = Math.Min(MaxSizeIncludingMargin.Width, HorizontalAlignment == HorizontalAlignment.Stretch ? BoundsSize.Width : Math.Min(BoundsSize.Width, RequestedFullSize.Width));
                    int ConsumedHeight = Math.Min(MaxSizeIncludingMargin.Height, VerticalAlignment == VerticalAlignment.Stretch ? BoundsSize.Height : Math.Min(BoundsSize.Height, RequestedFullSize.Height));
                    if (ConsumedWidth <= 0 || ConsumedHeight <= 0)
                    {
                        this.RenderBounds = Rectangle.Empty;
                        this.LayoutBounds = Rectangle.Empty;
                        this.StretchedContentBounds = Rectangle.Empty;
                        this.AlignedContentBounds = Rectangle.Empty;
                    }
                    else
                    {
                        //  Account for cases where stretching horizontally or vertically would cause the bounds to exceed MaxWidth and/or MaxHeight
                        HorizontalAlignment ActualHorizontalAlignment = MaxWidth.HasValue && HorizontalAlignment == HorizontalAlignment.Stretch && AllocatedBounds.Width > MaxWidth.Value + HorizontalMargin ?
                            HorizontalAlignment.Center :
                            HorizontalAlignment;
                        VerticalAlignment ActualVerticalAlignment = MaxHeight.HasValue && VerticalAlignment == VerticalAlignment.Stretch && AllocatedBounds.Height > MaxHeight.Value + VerticalMargin ?
                            VerticalAlignment.Center :
                            VerticalAlignment;

                        Size RenderSize = new(ConsumedWidth, ConsumedHeight);
                        this.RenderBounds = ApplyAlignment(AllocatedBounds, ActualHorizontalAlignment, ActualVerticalAlignment, RenderSize);

                        this.LayoutBounds = new(RenderBounds.Left + Margin.Left, RenderBounds.Top + Margin.Top,
                            RenderBounds.Width - HorizontalMargin, RenderBounds.Height - VerticalMargin);
                        if (LayoutBounds.Width <= 0 || LayoutBounds.Height <= 0)
                        {
                            this.LayoutBounds = Rectangle.Empty;
                            this.StretchedContentBounds = Rectangle.Empty;
                            this.AlignedContentBounds = Rectangle.Empty;
                        }
                        else
                        {
                            Rectangle RemainingComponentBounds = LayoutBounds;
                            foreach (MGComponentBase Component in this.Components)
                            {
                                Component.BaseElement.UpdateMeasurement(RemainingComponentBounds.Size, out _, out Thickness ComponentSize, out _, out _);
                                Rectangle ComponentBounds = Component.Arrange(RemainingComponentBounds, ComponentSize);
                                Component.BaseElement.UpdateLayout(ComponentBounds);

                                int Left = RemainingComponentBounds.Left;
                                int Right = RemainingComponentBounds.Right;
                                int Top = RemainingComponentBounds.Top;
                                int Bottom = RemainingComponentBounds.Bottom;

                                Thickness ConsumedSpace = Component.ConsumesAnySpace ? Component.Arrange(ComponentSize) : new(0);
                                if (!Component.IsWidthSharedWithContent)
                                {
                                    Left += ConsumedSpace.Left;
                                    Right -= ConsumedSpace.Right;
                                }

                                if (!Component.IsHeightSharedWithContent)
                                {
                                    Top += ConsumedSpace.Top;
                                    Bottom -= ConsumedSpace.Bottom;
                                }

                                RemainingComponentBounds = new(Left, Top, Right - Left, Bottom - Top);
                            }

                            int ContentBoundsLeft = Math.Min(RenderBounds.Right, RenderBounds.Left + RequestedSelfSize.Left - SharedSize.Left);
                            int ContentBoundsTop = Math.Min(RenderBounds.Bottom, RenderBounds.Top + RequestedSelfSize.Top - SharedSize.Top);
                            int ContentBoundsRight = Math.Max(RenderBounds.Left, RenderBounds.Right - RequestedSelfSize.Right - SharedSize.Right);
                            int ContentBoundsBottom = Math.Max(RenderBounds.Top, RenderBounds.Bottom - RequestedSelfSize.Bottom - SharedSize.Bottom);

                            this.StretchedContentBounds = new(ContentBoundsLeft, ContentBoundsTop,
                                Math.Max(0, ContentBoundsRight - ContentBoundsLeft), Math.Max(0, ContentBoundsBottom - ContentBoundsTop));

                            //  Determine how much space the content consumes based on VerticalContentAlignment / HorizontalContentAlignment
                            int ContentConsumedWidth;
                            int ContentConsumedHeight;
                            if (HorizontalContentAlignment == HorizontalAlignment.Stretch && VerticalContentAlignment == VerticalAlignment.Stretch)
                            {
                                ContentConsumedWidth = StretchedContentBounds.Width;
                                ContentConsumedHeight = StretchedContentBounds.Height;
                            }
                            else
                            {
                                ContentConsumedWidth = HorizontalContentAlignment == HorizontalAlignment.Stretch ? StretchedContentBounds.Width : Math.Min(StretchedContentBounds.Width, RequestedContentSize.Width);
                                ContentConsumedHeight = VerticalContentAlignment == VerticalAlignment.Stretch ? StretchedContentBounds.Height : Math.Min(StretchedContentBounds.Height, RequestedContentSize.Height);
                            }

                            //  Set bounds for content
                            if (ContentConsumedWidth <= 0 || ContentConsumedHeight <= 0)
                            {
                                this.AlignedContentBounds = Rectangle.Empty;
                            }
                            else
                            {
                                Size ContentSize = new(ContentConsumedWidth, ContentConsumedHeight);
                                this.AlignedContentBounds = ApplyAlignment(StretchedContentBounds, HorizontalContentAlignment, VerticalContentAlignment, ContentSize);
                            }

                            UpdateContentLayout(AlignedContentBounds);
                        }
                    }
                }

                IsLayoutValid = true;
                OnLayoutUpdated?.Invoke(this, EventArgs.Empty);
                OnLayoutBoundsChanged?.Invoke(this, new(PreviousLayoutBounds, LayoutBounds));
            }
            finally { IsUpdatingLayout = false; }
        }

		public event EventHandler<EventArgs> OnLayoutUpdated;
		public event EventHandler<EventArgs<Rectangle>> OnLayoutBoundsChanged;

        protected virtual void UpdateContentLayout(Rectangle Bounds) { }
        #endregion Arrange

        #region Measure
        protected const int MeasurementCacheSize = 15;

		/// <summary>Full measurements - I.E. includes the requested size of this <see cref="MGElement"/> and its content, if any.<para/>
		/// See also: <see cref="RecentMeasurementsSelfOnly"/></summary>
		private List<ElementMeasurement> RecentMeasurementsFull { get; } = new();
        private void CacheFullMeasurement(ElementMeasurement Value)
        {
            while (RecentMeasurementsFull.Count > MeasurementCacheSize)
                RecentMeasurementsFull.RemoveAt(RecentMeasurementsFull.Count - 1);
            if (!RecentMeasurementsFull.Contains(Value))
                RecentMeasurementsFull.Insert(0, Value);
        }

		/// <summary>Recent measurements that only account for the requested size of this <see cref="MGElement"/>; does not include the requested size of its content, if any.<br/>
		/// For example, a Border would only account for the total width of it's Margin, Padding, and BorderThickness.<para/>
		/// See also: <see cref="RecentMeasurementsFull"/></summary>
		private List<ElementMeasurement> RecentMeasurementsSelfOnly { get; } = new();
        private void CacheSelfMeasurement(ElementMeasurement Value)
        {
            while (RecentMeasurementsSelfOnly.Count > MeasurementCacheSize)
                RecentMeasurementsSelfOnly.RemoveAt(RecentMeasurementsSelfOnly.Count - 1);
            if (!RecentMeasurementsSelfOnly.Contains(Value))
                RecentMeasurementsSelfOnly.Insert(0, Value);
        }

        /// <param name="SelfMeasurement">A measurement that only accounts for this <see cref="MGElement"/> and not its content.<br/>
        /// For example, for a Border, this would include Margin, Padding, and BorderThickness.</param>
        /// <param name="FullMeasurement">A measurement that accounts for both this <see cref="MGElement"/> self-measurement and the measurement of its content, if any.</param>
        protected bool TryGetCachedMeasurement(Size AvailableSize, out ElementMeasurement SelfMeasurement, out ElementMeasurement FullMeasurement)
		{
			if (IsLayoutValid)
			{
				foreach (ElementMeasurement Self in RecentMeasurementsSelfOnly)
				{
					if (Self.AvailableSize == AvailableSize || 
						(Self.IsAvailableSizeGreaterThanOrEqual(AvailableSize) && Self.IsRequestedSizeLessThanOrEqual(AvailableSize))) 
							// If we decreased the available size, but it's still at least as much as what requested, we can re-use the cached measurement.
							// We can't re-use the measurement if we increased the available size, because wrappable content may end up consuming more width and less height
					{
                        SelfMeasurement = Self;

						foreach (ElementMeasurement Full in RecentMeasurementsFull)
						{
                            if (Full.AvailableSize == AvailableSize || (Full.IsAvailableSizeGreaterThanOrEqual(AvailableSize) && Full.IsRequestedSizeLessThanOrEqual(AvailableSize)))
							{
								FullMeasurement = Full;
								return true;
							}
						}

						break;
					}
				}
			}

            SelfMeasurement = default;
			FullMeasurement = default;
			return false;
		}

        protected virtual bool CanCacheSelfMeasurement { get => true; }

		private bool TryGetRecentSelfMeasurement(Size AvailableSize, out Thickness SelfSize, out Thickness SharedSize, out Thickness ContentSize)
		{
            if (CanCacheSelfMeasurement)
            {
                foreach (ElementMeasurement Measurement in RecentMeasurementsSelfOnly)
                {
                    if (Measurement.AvailableSize == AvailableSize || (Measurement.IsAvailableSizeGreaterThanOrEqual(AvailableSize) && Measurement.IsRequestedSizeLessThanOrEqual(AvailableSize)))
                    {
                        SelfSize = Measurement.RequestedSize;
                        SharedSize = Measurement.SharedSize;
                        ContentSize = Measurement.ContentSize;
                        return true;
                    }
                }
            }

			SelfSize = default;
			SharedSize = default;
            ContentSize = default;
			return false;
		}

        /// <summary>If true, this element is capable of consuming non-zero width when the height is zero, or a non-zero height when the width is zero.<br/>
        /// Default value is false, which means that if the element is assigned 0 width or height, then BOTH the width and height are automatically truncated to zero.<para/>
        /// For example, if an <see cref="MGButton"/> requested 16x16, but there was no vertical space available, then rather than allocating it 16x0, it is allocated 0x0</summary>
        protected virtual bool CanConsumeSpaceInSingleDimension => false;

        internal protected void UpdateMeasurement(Size AvailableSize, out Thickness SelfSize, out Thickness FullSize, out Thickness SharedSize, out Thickness ContentSize)
        {
			if (IsVisibilityCollapsed)
			{
				SelfSize = new(0);
                FullSize = new(0);
                SharedSize = new(0);
                ContentSize = new(0);
				return;
			}

			AvailableSize = AvailableSize.AsZeroOrGreater();
			Size RemainingSize = new(Math.Clamp(AvailableSize.Width, 0, MaxSizeIncludingMargin.Width), Math.Clamp(AvailableSize.Height, 0, MaxSizeIncludingMargin.Height)); ;

			if (!TryGetRecentSelfMeasurement(RemainingSize, out SelfSize, out SharedSize, out ContentSize))
			{
				SelfSize = MeasureSelf(RemainingSize, out SharedSize);
                ElementMeasurement SelfMeasurement = new(AvailableSize, SelfSize, SharedSize, ContentSize);
                CacheSelfMeasurement(SelfMeasurement);
            }

			Thickness UnsharedSelfSize = SelfSize.Subtract(SharedSize);
			RemainingSize = RemainingSize.Subtract(UnsharedSelfSize.Size, 0, 0);

			ContentSize = UpdateContentMeasurement(RemainingSize);
			FullSize = new Thickness(UnsharedSelfSize.Left + Math.Max(SharedSize.Left, ContentSize.Left),
				UnsharedSelfSize.Top + Math.Max(SharedSize.Top, ContentSize.Top),
				UnsharedSelfSize.Right + Math.Max(SharedSize.Right, ContentSize.Right),
				UnsharedSelfSize.Bottom + Math.Max(SharedSize.Bottom, ContentSize.Bottom));

			//  Adjust width/height based on preferred values
			if (ActualPreferredWidth.HasValue || ActualPreferredHeight.HasValue)
			{
				Size PreferredSize = new(ActualPreferredWidth ?? FullSize.Width, ActualPreferredHeight ?? FullSize.Height);
				FullSize = FullSize.Clamp(PreferredSize, PreferredSize);
			}

			//  Adjust width/height based on min/max sizes
			FullSize = FullSize.Clamp(MinSizeIncludingMargin, MaxSizeIncludingMargin).Clamp(Size.Empty, AvailableSize);

            if ((FullSize.Width <= 0 || FullSize.Height <= 0) && !CanConsumeSpaceInSingleDimension)
                FullSize = new(0);

            ElementMeasurement FullMeasurement = new(AvailableSize, FullSize, SharedSize, ContentSize);
            CacheFullMeasurement(FullMeasurement);
        }

        /// <summary>Returns the screen space required to display this <see cref="MGElement"/> if it has no child elements.<para/>
        /// For simple elements, this accounts for <see cref="Margin"/> and <see cref="Padding"/>.<br/>
        /// For a Border, this would also include the BorderThickness.<br/>
        /// For a TextBlock, this would include the size of the text content etc.</summary>
        /// <param name="SharedSize">Typically 0. This represents how much of the self measurement can be shared with the measurement of the content.<para/>
        /// For example, a checkbox contains a checkable rectangular portion that is in-line with the content.<br/>
        /// So if that checkable rectangle has height=16, and content has height=20, the total height is '16 + Math.Max(0, 20-16)' rather than 16+20=36.</param>
        protected Thickness MeasureSelf(Size AvailableSize, out Thickness SharedSize)
		{
			if (Visibility == Visibility.Collapsed)
			{
				SharedSize = new(0);
				return new(0);
			}

			Thickness Total = new(0);
            Size RemainingSize = AvailableSize;

			Thickness MarginAndPadding = Margin.Add(Padding);
			Total = Total.Add(MarginAndPadding);
            RemainingSize = RemainingSize.Subtract(MarginSize, 0, 0);

            Thickness Overridden = MeasureSelfOverride(RemainingSize, out SharedSize);
			Total = Total.Add(Overridden);
			RemainingSize = RemainingSize.Subtract(Overridden.Size, 0, 0);

			foreach (MGComponentBase Component in Components)
			{
				Size RemainingSizeForComponent = Component.UsesOwnersPadding ? RemainingSize.Subtract(PaddingSize, 0, 0) : RemainingSize;

				MGElement Element = Component.BaseElement;
                Element.UpdateMeasurement(RemainingSizeForComponent, out _, out Thickness ComponentSize, out _, out _);

				Thickness ActualComponentSize = Component.ConsumesAnySpace ? Component.Arrange(ComponentSize) : new(0);
				Thickness ComponentSharedSize = new(
					Component.IsWidthSharedWithContent ? ActualComponentSize.Left : 0,
					Component.IsHeightSharedWithContent ? ActualComponentSize.Top : 0,
					Component.IsWidthSharedWithContent ? ActualComponentSize.Right : 0,
					Component.IsHeightSharedWithContent ? ActualComponentSize.Bottom : 0);
				SharedSize = SharedSize.Add(ComponentSharedSize);

                Total = Total.Add(ActualComponentSize);
                RemainingSize = RemainingSize.Subtract(ActualComponentSize.Size, 0, 0);
			}

            if (Total.Width <= 0 && Total.Height <= 0)
				Total = new(0);

			return Total;
		}

		/// <summary>This method should not include <see cref="Margin"/> nor <see cref="Padding"/>.</summary>
		/// <param name="SharedSize">Typically 0. This represents how much of the self measurement can be shared with the measurement of the content.<para/>
		/// For example, a checkbox contains a checkable rectangular portion that is in-line with the content.<br/>
		/// So if that checkable rectangle has height=16, and content has height=20, the total height is '16 + Math.Max(0, 20-16)' rather than 16+20=36.</param>
		public virtual Thickness MeasureSelfOverride(Size AvailableSize, out Thickness SharedSize)
		{
			SharedSize = new(0);
			return new(0);
		}

        //  Note: This is split into 2 methods because MGContentHost declares an abstract override on UpdateContentMeasurement,
        //  but some derived classes may still want to call the base implementation
        //  More info: https://stackoverflow.com/questions/42271087/how-to-call-a-base-method-that-is-declared-as-abstract-override
        protected virtual Thickness UpdateContentMeasurement(Size AvailableSize) => UpdateContentMeasurementBaseImplementation(AvailableSize);
		protected Thickness UpdateContentMeasurementBaseImplementation(Size AvailableSize) => new(0);
        #endregion Measure
        #endregion Layout

        #region Visual Tree
        public virtual IEnumerable<MGElement> GetVisualTreeChildren() => GetChildren();

        public enum TreeTraversalMode
        {
            /// <summary>Visits the root node, then visits its children</summary>
            Preorder,
            /// <summary>Visits the root node's children, then visits itself</summary>
            Postorder
        }

        public bool TryFindParentOfType<T>(out T Result, bool IncludeSelf = false)
        {
            if (IncludeSelf && this is T TypedItem)
            {
                Result = TypedItem;
                return true;
            }
            else if (Parent != null)
            {
                return Parent.TryFindParentOfType(out Result, true);
            }
            else
            {
                Result = default;
                return false;
            }
        }

        //TODO maybe we should have parameters like:
        //bool IncludeToolTips to check MGElement.ToolTip and its descendents
        //bool IncludeContextMenus, to check MGElement.ContextMenu and its descendents
        //if you do add that, might need to modify MGWindow.Element_Added/MGWindow.Element_Removed since then MGContentHost would already be notifying on those elements

        /// <param name="IncludeSelf">If true, may return a reference to this element, or to one of this element's components (if <paramref name="IncludeComponents"/> is also true).</param>
        /// <param name="IncludeComponents">If true, may returns a reference to a component of an element, such as the button part of an <see cref="MGCheckBox"/>.</param>
        /// <param name="TraversalMode">The order to visit the nodes in.</param>
        public IEnumerable<MGElement> TraverseVisualTree(bool IncludeSelf = true, bool IncludeComponents = true, TreeTraversalMode TraversalMode = TreeTraversalMode.Preorder)
            => TraverseVisualTree<MGElement>(IncludeSelf, IncludeComponents, TraversalMode);

        /// <param name="IncludeSelf">If true, may return a reference to this element, or to one of this element's components (if <paramref name="IncludeComponents"/> is also true).</param>
        /// <param name="IncludeComponents">If true, may returns a reference to a component of an element, such as the button part of an <see cref="MGCheckBox"/>.</param>
        /// <param name="TraversalMode">The order to visit the nodes in.</param>
        public IEnumerable<T> TraverseVisualTree<T>(bool IncludeSelf = true, bool IncludeComponents = true, TreeTraversalMode TraversalMode = TreeTraversalMode.Preorder)
            where T : MGElement
        {
            if (TraversalMode == TreeTraversalMode.Preorder && IncludeSelf)
            {
                if (this is T TypedItem)
                    yield return TypedItem;

				if (IncludeComponents)
				{
					foreach (MGComponentBase Component in this.Components)
					{
						foreach (T Item in Component.BaseElement.TraverseVisualTree<T>(true, IncludeComponents, TraversalMode))
							yield return Item;
					}
				}
            }

            foreach (MGElement Child in GetVisualTreeChildren())
			{
				foreach (T Item in Child.TraverseVisualTree<T>(true, IncludeComponents, TraversalMode))
					yield return Item;
			}

			if (TraversalMode == TreeTraversalMode.Postorder && IncludeSelf)
			{
                if (IncludeComponents)
                {
                    foreach (MGComponentBase Component in this.Components)
                    {
                        foreach (T Item in Component.BaseElement.TraverseVisualTree<T>(true, IncludeComponents, TraversalMode))
                            yield return Item;
                    }
                }

                if (this is T TypedItem)
                    yield return TypedItem;
            }
        }

        /// <summary>Returns the first element in the visual tree that satisfies the given <paramref name="Predicate"/></summary>
        /// <param name="IncludeSelf">If true, may return a reference to this element, or to one of this element's components (if <paramref name="IncludeComponents"/> is also true).</param>
        /// <param name="IncludeComponents">If true, may returns a reference to a component of an element, such as the button part of an <see cref="MGCheckBox"/>.</param>
        /// <param name="TraversalMode">The order to visit the nodes and evaluate the <paramref name="Predicate"/> in.</param>
        /// <returns>The first match, or null if no valid match was found.</returns>
        public MGElement FindElement(Predicate<MGElement> Predicate, bool IncludeSelf = true, bool IncludeComponents = true, TreeTraversalMode TraversalMode = TreeTraversalMode.Preorder)
            => TraverseVisualTree<MGElement>(IncludeSelf, IncludeComponents, TraversalMode).FirstOrDefault(x => Predicate(x));

        /// <summary>Returns the first element in the visual tree of type=<typeparamref name="T"/> that satisfies the given <paramref name="Predicate"/></summary>
        /// <param name="IncludeSelf">If true, may return a reference to this element, or to one of this element's components (if <paramref name="IncludeComponents"/> is also true).</param>
        /// <param name="IncludeComponents">If true, may returns a reference to a component of an element, such as the button part of an <see cref="MGCheckBox"/>.</param>
        /// <param name="TraversalMode">The order to visit the nodes and evaluate the <paramref name="Predicate"/> in.</param>
        /// <returns>The first match, or null if no valid match was found.</returns>
        public T FindElement<T>(Predicate<T> Predicate, bool IncludeSelf = true, bool IncludeComponents = true, TreeTraversalMode TraversalMode = TreeTraversalMode.Preorder)
			where T : MGElement
			=> TraverseVisualTree<T>(IncludeSelf, IncludeComponents, TraversalMode).FirstOrDefault(x => Predicate(x));

        /// <summary>Returns all elements in the visual tree that satisfy the given <paramref name="Predicate"/></summary>
        /// <param name="IncludeSelf">If true, may return a reference to this element, or to one of this element's components (if <paramref name="IncludeComponents"/> is also true).</param>
        /// <param name="IncludeComponents">If true, may returns a reference to a component of an element, such as the button part of an <see cref="MGCheckBox"/>.</param>
        /// <param name="TraversalMode">The order to visit the nodes and evaluate the <paramref name="Predicate"/> in.</param>
        public IEnumerable<MGElement> GetElements(Predicate<MGElement> Predicate, bool IncludeSelf = true, bool IncludeComponents = true, TreeTraversalMode TraversalMode = TreeTraversalMode.Preorder)
            => TraverseVisualTree<MGElement>(IncludeSelf, IncludeComponents, TraversalMode).Where(x => Predicate(x));

        /// <summary>Returns all elements in the visual tree of type=<typeparamref name="T"/> that satisfy the given <paramref name="Predicate"/></summary>
        /// <param name="IncludeSelf">If true, may return a reference to this element, or to one of this element's components (if <paramref name="IncludeComponents"/> is also true).</param>
        /// <param name="IncludeComponents">If true, may returns a reference to a component of an element, such as the button part of an <see cref="MGCheckBox"/>.</param>
        /// <param name="TraversalMode">The order to visit the nodes and evaluate the <paramref name="Predicate"/> in.</param>
        public IEnumerable<T> GetElements<T>(Predicate<T> Predicate, bool IncludeSelf = true, bool IncludeComponents = true, TreeTraversalMode TraversalMode = TreeTraversalMode.Preorder)
            where T : MGElement
            => TraverseVisualTree<T>(IncludeSelf, IncludeComponents, TraversalMode).Where(x => Predicate(x));
        #endregion Visual Tree
	}
}
