using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI
{
    public enum MGElementType
    {
        Undefined,

        //  Containers
        StackPanel,
        DockPanel,
        Grid,
        UniformGrid,
        OverlayPanel,
        //WrapPanel?

        //  Layout
        ScrollViewer,
        Window,
        Border,
        Expander,
        GroupBox,
        Separator,
        Spacer,
        ResizeGrip,
        TabControl,
        TabItem,
        ContentPresenter,
        ContextualContentPresenter,
        HeaderedContentPresenter,
        GridSplitter,

        //  Buttons and Selection
        Button,
        ToggleButton,
        //RepeatButton?
        CheckBox,
        RadioButton,
        ComboBox,
        Slider,
        RatingControl,

        //  Menus
        ToolTip,
        ContextMenu,
        ContextMenuItem,
        //MenuBar?
        //MenuItem?

        //  Data Display
        ListView,
        ListBox,
        //TreeView?

        //  User Info
        ProgressBar,
        TextBlock,
        Spoiler,
        Timer,
        Stopwatch,
        //StatusBar?

        //  Input
        TextBox,
        PasswordBox,
        ChatBox,
        ChatBoxMessage,
        InputConsumer,

        //  Media
        Image,

        //  Other
        Rectangle,

        OverlayHost,
        Overlay,
        XAMLDesigner,
        /// <summary>Represents an element that doesn't fit any other category</summary>
        Misc,
        Custom,
        UserControl,
    }

    /// <summary>Describes how a child element is vertically positioned or stretched within a parent's layout slot</summary>
    public enum VerticalAlignment
    {
        /// <summary>The child element is aligned to the top of the parent's layout slot.</summary>
        Top = 0,
        /// <summary>The child element is aligned to the center of the parent's layout slot.</summary>
        Center = 1,
        /// <summary>The child element is aligned to the bottom of the parent's layout slot.</summary>
        Bottom = 2,
        /// <summary>The child element stretches to fill the parent's layout slot.</summary>
        Stretch = 3
    }

    /// <summary>Indicates where an element should be displayed on the horizontal axis relative to the allocated layout slot of the parent element.</summary>
    public enum HorizontalAlignment
    {
        /// <summary>An element aligned to the left of the layout slot for the parent element.</summary>
        Left = 0,
        /// <summary>An element aligned to the center of the layout slot for the parent element.</summary>
        Center = 1,
        /// <summary>An element aligned to the right of the layout slot for the parent element.</summary>
        Right = 2,
        /// <summary>An element stretched to fill the entire layout slot of the parent element.</summary>
        Stretch = 3
    }

    /// <summary>Defines the different orientations that a control or layout can have.</summary>
    public enum Orientation
    {
        /// <summary>Control or layout should be horizontally oriented.</summary>
        Horizontal = 0,
        /// <summary>Control or layout should be vertically oriented.</summary>
        Vertical = 1
    }

    /// <summary>Specifies the display state of an element.</summary>
    public enum Visibility : byte
    {
        /// <summary>Display the element.</summary>
        Visible = 0,
        /// <summary>Do not display the element, but reserve space for the element in layout.</summary>
        Hidden = 1,
        /// <summary>Do not display the element, and do not reserve space for it in layout.</summary>
        Collapsed = 2
    }

    /// <summary>Specifies the visibility of a ScrollBar for scrollable content.</summary>
    public enum ScrollBarVisibility
    {
        /// <summary>A ScrollBar does not appear even when the viewport cannot display all of the content.<br/>
		/// The content is not allocated infinite space in the ScrollBar's dimension, and instead only receives the space of the viewport.<para/>
		/// ScrollBar Layout Size: Zero<br/>
		/// ScrollBar Render Size: Zero<br/>
		/// Content AvailableSpace: Same as Viewport</summary>
        Disabled = 0,
        /// <summary>A ScrollBar appears if the viewport cannot display all of the content. The content is allocated infinite space in the ScrollBar's dimension.<para/>
		/// If Content fits within Viewport:<br/>
		/// ScrollBar Layout Size: Zero<br/>
		/// ScrollBar Render Size: Zero<br/>
		/// Content AvailableSpace: Infinite<para/>
		/// If Content does not fit within Viewport:<br/>
		/// ScrollBar Layout Size: Non-Zero<br/>
		/// ScrollBar Render Size: Non-Zero<br/>
		/// Content AvailableSpace: Infinite</summary>
        Auto = 1,
        /// <summary>A ScrollBar does not appear, even when the viewport cannot display all of the content, but the ScrollBar still consumes space in the layout.<br/>
		/// The content is allocated infinite space in the ScrollBar's dimension.<br/>
		/// This value is typically only used if you want to render your own ScrollBar, while having the ScrollBar's layout bounds reserved for you. Otherwise, consider using <see cref="Collapsed"/> instead.<para/>
		/// ScrollBar Layout Size: Non-Zero<br/>
		/// ScrollBar Render Size: Zero<br/>
		/// Content AvailableSpace: Infinite</summary>
        Hidden = 2,
        /// <summary>A ScrollBar always appears, even if the viewport can display all of the content. The content is allocated infinite space in the ScrollBar's dimension.<para/>
		/// ScrollBar Layout Size: Non-Zero<br/>
		/// ScrollBar Render Size: Non-Zero<br/>
		/// Content AvailableSpace: Infinite</summary>
        Visible = 3,
        /// <summary>A ScrollBar does not appear, even when the viewport cannot display all of the content. The content is allocated infinite space in the ScrollBar's dimension.<para/>
        /// ScrollBar Layout Size: Zero<br/>
        /// ScrollBar Render Size: Zero<br/>
        /// Content  AvailableSpace: Infinite</summary>
        Collapsed = 4
    }

    /// <summary>Specifies the Dock position of a child element that is inside a DockPanel.</summary>
    public enum Dock
    {
        /// <summary>A child element that is positioned on the left side of the DockPanel.</summary>
        Left = 0,
        /// <summary>A child element that is positioned at the top of the DockPanel.</summary>
        Top = 1,
        /// <summary>A child element that is positioned on the right side of the DockPanel.</summary>
        Right = 2,
        /// <summary>A child element that is positioned at the bottom of the DockPanel.</summary>
        Bottom = 3
    }

    /// <summary>Specifies how a window will automatically size itself to fit the size of its content.</summary>
    public enum SizeToContent
    {
        /// <summary>Specifies that a window will not automatically set its size to fit the size of its content. 
		/// Instead, the size of a window is determined by other properties, including WindowWidth, WindowHeight, 
		/// MaxWidth, MaxHeight, MinWidth, and MinHeight.</summary>
        Manual = 0,
        /// <summary>Specifies that a window will automatically set its width to fit the width of its content, but not the height.</summary>
        Width = 1,
        /// <summary>Specifies that a window will automatically set its height to fit the height of its content, but not the width.</summary>
        Height = 2,
        /// <summary>Specifies that a window will automatically set both its width and height to fit the width and height of its content.</summary>
        WidthAndHeight = 3
    }

    public enum CornerType
    {
        TopLeft,
        TopRight,
        BottomRight,
        BottomLeft
    }

    public enum CoordinateSpace
    {
        /// <summary>Coordinates in this <see cref="CoordinateSpace"/> are relative to an origin specified by <see cref="MGElement.Origin"/></summary>
        Layout,
        /// <summary>Coordinates in this <see cref="CoordinateSpace"/> always use an origin of <see cref="Point.Zero"/>,<br/>
        /// but do not account for <see cref="MGWindow.Scale"/></summary>
        UnscaledScreen,
        /// <summary>Coordinates in this <see cref="CoordinateSpace"/> always use an origin of <see cref="Point.Zero"/>, and have already been scaled based on <see cref="MGWindow.Scale"/> value.<para/>
        /// Mouse Positions are typically defined in this <see cref="CoordinateSpace"/> and converted to <see cref="CoordinateSpace.Layout"/><br/>
        /// before checking if the position intersects the <see cref="MGElement"/> bounds (such as <see cref="MGElement.LayoutBounds"/>)</summary>
        Screen
    }

    public enum WindowStyle
    {
        Default,
        None
    }
}