using Microsoft.Xna.Framework;
using MGUI.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoGame.Extended;
using MGUI.Core.UI.Containers;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Shared.Rendering;
using MGUI.Shared.Input.Mouse;
using System.Diagnostics;

namespace MGUI.Core.UI
{
    /// <summary>This class handles mutual exclusion between the <see cref="MGRadioButton.IsChecked"/> property of multiple <see cref="MGRadioButton"/>s belonging to the same <see cref="MGRadioButtonGroup"/></summary>
    public class MGRadioButtonGroup : ViewModelBase
    {
        public MGWindow Window { get; }
        public string Name { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _AllowUnchecking;
        /// <summary>If true, the <see cref="CheckedItem"/> can be unchecked by clicking it again.<br/>
        /// Only relevant if <see cref="AllowNullCheckedItem"/>=true.<para/>
        /// Default value: false<para/>See also: <see cref="ActualAllowUnchecking"/>, <see cref="AllowNullCheckedItem"/></summary>
        public bool AllowUnchecking
        {
            get => _AllowUnchecking;
            set
            {
                if (_AllowUnchecking != value)
                {
                    _AllowUnchecking = value;
                    NPC(nameof(AllowUnchecking));
                    NPC(nameof(ActualAllowUnchecking));
                }
            }
        }

        public bool ActualAllowUnchecking => AllowUnchecking && AllowNullCheckedItem;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _AllowNullCheckedItem;
        /// <summary>If true, <see cref="CheckedItem"/> can be set to null.<para/>
        /// Default value: false</summary>
        public bool AllowNullCheckedItem
        {
            get => _AllowNullCheckedItem;
            set
            {
                if (_AllowNullCheckedItem != value)
                {
                    _AllowNullCheckedItem = value;
                    if (!AllowNullCheckedItem)
                        CheckedItem ??= _RadioButtons.FirstOrDefault();
                    NPC(nameof(AllowNullCheckedItem));
                    NPC(nameof(ActualAllowUnchecking));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private MGRadioButton _CheckedItem;
        public MGRadioButton CheckedItem
        {
            get => _CheckedItem;
            set
            {
                MGRadioButton Value = AllowNullCheckedItem ? value : value ?? _RadioButtons.FirstOrDefault();
                if (_CheckedItem != Value)
                {
                    MGRadioButton Previous = CheckedItem;
                    _CheckedItem = Value;
                    Previous?.NPC(nameof(MGRadioButton.IsChecked));
                    CheckedItem?.NPC(nameof(MGRadioButton.IsChecked));
                    Previous?.HandleCheckStateChanged();
                    CheckedItem?.HandleCheckStateChanged();
                    NPC(nameof(CheckedItem));
                    CheckedItemChanged?.Invoke(this, new EventArgs<MGRadioButton>(Previous, CheckedItem));
                }
            }
        }

        /// <summary>Invoked when <see cref="CheckedItem"/> is set to a new value.</summary>
        public event EventHandler<EventArgs<MGRadioButton>> CheckedItemChanged;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ObservableCollection<MGRadioButton> _RadioButtons { get; }
        public IReadOnlyList<MGRadioButton> RadioButtons => _RadioButtons;

        public void AddRadioButton(MGRadioButton RB) => _RadioButtons.Add(RB);
        public bool RemoveRadioButton(MGRadioButton RB) => _RadioButtons.Remove(RB);

        public MGRadioButtonGroup(MGWindow Window, string Name)
        {
            if (Window.HasRadioButtonGroup(Name))
                throw new ArgumentException($"{nameof(Name)} '{Name}' must be unique within the scope of its {nameof(MGWindow)}.");

            this.Name = Name;
            this._RadioButtons = new();
            this.AllowUnchecking = false;
            this.AllowNullCheckedItem = false;

            _RadioButtons.CollectionChanged += (sender, e) =>
            {
                if (e.Action is NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Replace or NotifyCollectionChangedAction.Reset)
                {
                    bool RemovedCheckedItem = CheckedItem != null && e.OldItems.Cast<MGRadioButton>().Contains(CheckedItem);
                    if (RemovedCheckedItem)
                        CheckedItem = null;
                }

                if (!AllowNullCheckedItem)
                    CheckedItem ??= _RadioButtons.FirstOrDefault();
            };
        }
    }

    public class MGRadioButton : MGSingleContentHost
    {
        /// <summary>Provides direct access to the button component that appears to the left of this radiobutton's content.</summary>
        public MGComponent<MGButton> ButtonComponent { get; }
        /// <summary>The checkable button portion of this <see cref="MGRadioButton"/></summary>
        private MGButton ButtonElement { get; }

        /// <summary>The default width/height of the checkable part of an <see cref="MGRadioButton"/></summary>
        public const int DefaultBubbleSize = 16;
        /// <summary>The default empty width between the checkable part of an <see cref="MGRadioButton"/> and its <see cref="MGSingleContentHost.Content"/></summary>
        public const int DefaultBubbleSpacingWidth = 5;

        public MGRadioButtonGroup Group { get; }

        private Size GetButtonComponentPreferredSize() => new(BubbleComponentSize, BubbleComponentSize);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _BubbleComponentSize;
        /// <summary>The dimensions of the checkable part of this <see cref="MGRadioButton"/>.<para/>
        /// See also: <see cref="DefaultBubbleSize"/></summary>
        public int BubbleComponentSize
        {
            get => _BubbleComponentSize;
            set
            {
                if (_BubbleComponentSize != value)
                {
                    _BubbleComponentSize = value;
                    Size ButtonSize = GetButtonComponentPreferredSize();
                    ButtonElement.PreferredWidth = ButtonSize.Width;
                    ButtonElement.PreferredHeight = ButtonSize.Height;
                    NPC(nameof(BubbleComponentSize));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Color _BubbleComponentBorderColor;
        /// <summary>The <see cref="Color"/> to use when drawing the Border of the checkable part.</summary>
        public Color BubbleComponentBorderColor
        {
            get => _BubbleComponentBorderColor;
            set
            {
                if (_BubbleComponentBorderColor != value)
                {
                    _BubbleComponentBorderColor = value;
                    NPC(nameof(BubbleComponentBorderColor));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private float _BubbleComponentBorderThickness;
        /// <summary>The thickness to use for the checkable part's border.<br/>
        /// This value cannot exceed <see cref="BubbleComponentSize"/>/2.0<para/>
        /// Recommended value: 1 or 2</summary>
        public float BubbleComponentBorderThickness
        {
            get => _BubbleComponentBorderThickness;
            set
            {
                if (_BubbleComponentBorderThickness != value)
                {
                    _BubbleComponentBorderThickness = value;
                    NPC(nameof(BubbleComponentBorderThickness));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private VisualStateColorBrush _BubbleComponentBackground;
        /// <summary>The background to use when drawing the checkable part.</summary>
        public VisualStateColorBrush BubbleComponentBackground
        {
            get => _BubbleComponentBackground;
            set
            {
                if (_BubbleComponentBackground != value)
                {
                    _BubbleComponentBackground = value;
                    NPC(nameof(BubbleComponentBackground));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Color _BubbleCheckedColor;
        /// <summary>The <see cref="Color"/> to use when filling in the checkable bubble part when <see cref="IsChecked"/> is true.<para/>
        /// Default value: <see cref="MGTheme.RadioButtonCheckedFillColor"/><para/>
        /// See also:<br/><see cref="MGWindow.Theme"/><br/><see cref="MGDesktop.Theme"/></summary>
        public Color BubbleCheckedColor
        {
            get => _BubbleCheckedColor;
            set
            {
                if (_BubbleCheckedColor != value)
                {
                    _BubbleCheckedColor = value;
                    NPC(nameof(BubbleCheckedColor));
                }
            }
        }

        /// <summary>The reserved empty width between the checkable part of this <see cref="MGRadioButton"/> and its <see cref="MGSingleContentHost.Content"/>.<para/>
        /// See also: <see cref="DefaultBubbleSpacingWidth"/>.<para/>
        /// This value is functionally equivalent to <see cref="ButtonElement"/>'s right <see cref="MGElement.Margin"/></summary>
        public int SpacingWidth
        {
            get => ButtonElement.Margin.Right;
            set
            {
                if (SpacingWidth != value)
                {
                    ButtonElement.Margin = ButtonElement.Margin.ChangeRight(value);
                    NPC(nameof(SpacingWidth));
                }
            }
        }

        public bool IsChecked
        {
            get => Group.CheckedItem == this;
            set
            {
                if (value)
                    Group.CheckedItem = this;
                else if (IsChecked && Group.AllowUnchecking)
                    Group.CheckedItem = null;
            }
        }

        internal void HandleCheckStateChanged()
        {
            OnCheckStateChanged?.Invoke(this, IsChecked);
            if (IsChecked)
                OnChecked?.Invoke(this, EventArgs.Empty);
            else
                OnUnchecked?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Note: This event is invoked before <see cref="OnChecked"/> / <see cref="OnUnchecked"/></summary>
        public event EventHandler<bool> OnCheckStateChanged;
        public event EventHandler<EventArgs> OnChecked;
        public event EventHandler<EventArgs> OnUnchecked;

        public MGRadioButton(MGWindow Window, string GroupName)
            : this(Window, Window.GetOrCreateRadioButtonGroup(GroupName)) { }

        public MGRadioButton(MGWindow Window, MGRadioButtonGroup Group)
            : base(Window, MGElementType.RadioButton)
        {
            using (BeginInitializing())
            {
                this.Group = Group;
                Group.AddRadioButton(this);

                this.ButtonElement = new(Window, x => this.IsChecked = !this.IsChecked);
                this.ButtonElement.MinHeight = 8;
                this.ButtonElement.MinWidth = 8;
                this.ButtonElement.Visibility = Visibility.Hidden;
                this.ButtonElement.CanHandleInputsWhileHidden = true;

                this.ButtonComponent = new(ButtonElement, false, true, true, true, false, false, false,
                    (AvailableBounds, ComponentSize) => ApplyAlignment(AvailableBounds, HorizontalAlignment.Left, VerticalAlignment.Center, ComponentSize.Size));
                AddComponent(ButtonComponent);

                this.BubbleComponentSize = DefaultBubbleSize;
                this.BubbleComponentBorderColor = Color.Black;
                this.BubbleComponentBorderThickness = 1;
                this.BubbleComponentBackground = GetTheme().RadioButtonBubbleBackground.GetValue(true);
                this.BubbleCheckedColor = GetTheme().RadioButtonCheckedFillColor;

                this.SpacingWidth = DefaultBubbleSpacingWidth;
            }
        }

        /// <summary>The number of sides to use when approximating a circle as a polygon.<para/>
        /// Recommended value: 32</summary>
        private const int CircleDetailLevel = 32; // 16 looks bad

        public override void DrawSelf(ElementDrawArgs DA, Rectangle LayoutBounds)
        {
            DrawTransaction DT = DA.DT;
            float Opacity = DA.Opacity;

            Rectangle BubblePartBounds = ButtonElement.LayoutBounds;

            Point BubbleCenter = BubblePartBounds.Center + DA.Offset;
            int BubbleRadius = BubblePartBounds.Width / 2;

            Color BubbleComponentFillColor = BubbleComponentBackground.GetUnderlay(VisualState.Primary);
            DT.FillCircle(BubbleCenter.ToVector2(), BubbleComponentFillColor * Opacity, BubbleRadius - BubbleComponentBorderThickness / 2, CircleDetailLevel);
            DT.StrokeCircle(BubbleCenter.ToVector2(), BubbleComponentBorderColor * Opacity, BubbleRadius, BubbleComponentBorderThickness, CircleDetailLevel);
            //DT.StrokeAndFillCircle(BubbleCenter.ToVector2(), BubbleComponentBorderColor * Opacity, BubbleComponentFillColor * Opacity, BubbleRadius, BubbleComponentBorderThickness, CircleDetailLevel);

            if (!ParentWindow.HasModalWindow)
            {
                Point LayoutSpacePosition = ConvertCoordinateSpace(CoordinateSpace.Screen, CoordinateSpace.Layout, InputTracker.Mouse.CurrentPosition);
                bool IsBubblePartPressed;
                if (InputTracker.Mouse.RecentButtonPressedEvents[MouseButton.Left] != null)
                {
                    Point PressPositionScreenSpace = InputTracker.Mouse.RecentButtonPressedEvents[MouseButton.Left].Position;
                    Point PressPositionLayoutSpace = ConvertCoordinateSpace(CoordinateSpace.Screen, CoordinateSpace.Layout, PressPositionScreenSpace);
                    IsBubblePartPressed = BubblePartBounds.ContainsInclusive(PressPositionLayoutSpace);
                }
                else
                    IsBubblePartPressed = false;
                bool IsBubblePartHovered = BubblePartBounds.ContainsInclusive(LayoutSpacePosition);

                Color? Overlay = BubbleComponentBackground.GetColorOverlay(IsBubblePartPressed ? SecondaryVisualState.Pressed : IsBubblePartHovered ? SecondaryVisualState.Hovered : SecondaryVisualState.None);
                if (Overlay.HasValue)
                {
                    DT.FillCircle(BubbleCenter.ToVector2(), Overlay.Value * Opacity, BubbleRadius - BubbleComponentBorderThickness / 2, CircleDetailLevel);
                    DT.StrokeCircle(BubbleCenter.ToVector2(), Overlay.Value * Opacity, BubbleRadius, BubbleComponentBorderThickness, CircleDetailLevel);
                    //DT.StrokeAndFillCircle(BubbleCenter.ToVector2(), Overlay.Value * Opacity, Overlay.Value * Opacity, BubbleRadius, BubbleComponentBorderThickness, CircleDetailLevel);
                }
            }

            if (IsChecked)
            {
                int InnerRadius = BubbleRadius - 4;
                DT.FillCircle(BubbleCenter.ToVector2(), BubbleCheckedColor * Opacity, InnerRadius, CircleDetailLevel);
            }
        }
    }
}
