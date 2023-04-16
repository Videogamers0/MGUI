using Microsoft.Xna.Framework;
using MGUI.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoGame.Extended;
using MGUI.Core.UI.Containers;
using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Shared.Rendering;
using System.Diagnostics;

namespace MGUI.Core.UI
{
    public class MGCheckBox : MGSingleContentHost
    {
        /// <summary>The default width/height of the checkable part of an <see cref="MGCheckBox"/></summary>
        public const int DefaultCheckBoxSize = 16;
        /// <summary>The default empty width between the checkable part of an <see cref="MGCheckBox"/> and its <see cref="MGSingleContentHost.Content"/></summary>
        public const int DefaultCheckBoxSpacingWidth = 5;

        /// <summary>Provides direct access to the button component that appears to the left of this checkbox's content.<para/>
        /// See also: <see cref="ButtonElement"/></summary>
        public MGComponent<MGButton> ButtonComponent { get; }
        /// <summary>The checkable button portion of this <see cref="MGCheckBox"/></summary>
        public MGButton ButtonElement { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _CheckBoxComponentSize;
        /// <summary>The dimensions of the checkable part of this <see cref="MGCheckBox"/>.<para/>
        /// See also: <see cref="DefaultCheckBoxSize"/></summary>
        public int CheckBoxComponentSize
        {
            get => _CheckBoxComponentSize;
            set
            {
                if (_CheckBoxComponentSize != value)
                {
                    _CheckBoxComponentSize = value;

                    Size ButtonSize = new(CheckBoxComponentSize, CheckBoxComponentSize);
                    ButtonElement.PreferredWidth = ButtonSize.Width;
                    ButtonElement.PreferredHeight = ButtonSize.Height;

                    NPC(nameof(CheckBoxComponentSize));
                }
            }
        }

        /// <summary>The reserved empty width between the checkable part of this <see cref="MGCheckBox"/> and its <see cref="MGSingleContentHost.Content"/>.<para/>
        /// See also: <see cref="DefaultCheckBoxSpacingWidth"/>.<para/>
        /// This value is functionally equivalent to <see cref="ButtonElement"/>'s right <see cref="MGElement.Margin"/></summary>
        public int SpacingWidth
        {
            get => ButtonElement.Margin.Right;
            set
            {
                if (ButtonElement.Margin.Right != value)
                {
                    ButtonElement.Margin = ButtonElement.Margin.ChangeRight(value);
                    NPC(nameof(SpacingWidth));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Color _CheckMarkColor;
        /// <summary>The <see cref="Color"/> to use when stroking the check mark if <see cref="IsChecked"/> is true.<para/>
        /// Default value: <see cref="MGTheme.CheckMarkColor"/><para/>
        /// See also:<br/><see cref="MGWindow.Theme"/><br/><see cref="MGDesktop.Theme"/></summary>
        public Color CheckMarkColor
        {
            get => _CheckMarkColor;
            set
            {
                if (_CheckMarkColor != value)
                {
                    _CheckMarkColor = value;
                    NPC(nameof(CheckMarkColor));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _IsCheckMarkShadowed;
        /// <summary>If true, the graphics of the <see cref="ButtonElement"/> will be drawn an extra time
        /// with Color=<see cref="CheckMarkShadowColor"/> and using Offset=<see cref="CheckMarkShadowOffset"/><para/>
        /// Default value: false</summary>
        public bool IsCheckMarkShadowed
        {
            get => _IsCheckMarkShadowed;
            set
            {
                if (_IsCheckMarkShadowed != value)
                {
                    _IsCheckMarkShadowed = value;
                    NPC(nameof(IsCheckMarkShadowed));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Color _CheckMarkShadowColor;
        public Color CheckMarkShadowColor
        {
            get => _CheckMarkShadowColor;
            set
            {
                if (_CheckMarkShadowColor != value)
                {
                    _CheckMarkShadowColor = value;
                    NPC(nameof(CheckMarkShadowColor));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Point _CheckMarkShadowOffset;
        public Point CheckMarkShadowOffset
        {
            get => _CheckMarkShadowOffset;
            set
            {
                if (_CheckMarkShadowOffset != value)
                {
                    _CheckMarkShadowOffset = value;
                    NPC(nameof(CheckMarkShadowOffset));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _IsThreeState;
        /// <summary>True if 'null' is a valid value for <see cref="IsChecked"/><para/>
        /// Default value: false</summary>
        public bool IsThreeState
        {
            get => _IsThreeState;
            set
            {
                if (_IsThreeState != value)
                {
                    _IsThreeState = value;
                    if (!IsThreeState && !IsChecked.HasValue)
                        IsChecked = false;
                    NPC(nameof(IsThreeState));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool? _IsChecked;
        /// <summary>If <see cref="IsThreeState"/> is false, this value should not be set to null.</summary>
        public bool? IsChecked
        {
            get => _IsChecked;
            set
            {
                if (_IsChecked != value)
                {
                    if (!IsThreeState && !value.HasValue)
                        throw new InvalidOperationException($"{nameof(MGCheckBox)}.{nameof(IsChecked)} can only be set to 'null' if {nameof(IsThreeState)} is true.");

                    bool? Previous = IsChecked;
                    _IsChecked = value;
                    NPC(nameof(IsChecked));
                    OnCheckStateChanged?.Invoke(this, new(Previous, IsChecked));

                    if (IsChecked.HasValue)
                    {
                        if (IsChecked.Value)
                            OnChecked?.Invoke(this, EventArgs.Empty);
                        else
                            OnUnchecked?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _IsReadonly;
        /// <summary>If true, the user will be unable to modify <see cref="IsChecked"/> by manually clicking the <see cref="ButtonElement"/>.<para/>
        /// Default value: false</summary>
        public bool IsReadonly
        {
            get => _IsReadonly;
            set
            {
                if (_IsReadonly != value)
                {
                    _IsReadonly = value;
                    NPC(nameof(IsReadonly));
                }
            }
        }

        /// <summary>Note: This event is invoked before <see cref="OnChecked"/> / <see cref="OnUnchecked"/></summary>
        public event EventHandler<EventArgs<bool?>> OnCheckStateChanged;
        public event EventHandler<EventArgs> OnChecked;
        public event EventHandler<EventArgs> OnUnchecked;

        public MGCheckBox(MGWindow Window, bool? IsChecked = false)
            : base(Window, MGElementType.CheckBox)
        {
            using (BeginInitializing())
            {
                this.ButtonElement = new(Window, new(1), MGUniformBorderBrush.Black, x =>
                {
                    if (!IsReadonly)
                    {
                        if (IsThreeState)
                        {
                            this.IsChecked = this.IsChecked.HasValue && this.IsChecked.Value ? null : !this.IsChecked.HasValue ? false : true;
                        }
                        else
                        {
                            this.IsChecked = !this.IsChecked.HasValue || this.IsChecked.Value ? false : true;
                        }
                    }
                });
                ButtonElement.MinWidth = 12;
                ButtonElement.MinHeight = 12;
                ButtonElement.Padding = new(0);

                ButtonElement.OnEndingDraw += (sender, e) =>
                {
                    if (!this.IsChecked.HasValue)
                    {
                        Rectangle TargetBounds = ButtonElement.LayoutBounds.GetCompressed(ButtonElement.Padding).GetScaledFromCenter(0.60f);
                        //  Force the bounds to be an even width/height
                        if (TargetBounds.Width % 2 != 0 || TargetBounds.Height % 2 != 0)
                            TargetBounds = new(TargetBounds.Left, TargetBounds.Top, TargetBounds.Width / 2 * 2, TargetBounds.Height / 2 * 2);

                        if (IsCheckMarkShadowed)
                            e.DA.DT.FillRectangle(e.DA.Offset.ToVector2(), TargetBounds.GetTranslated(CheckMarkShadowOffset), CheckMarkShadowColor * e.DA.Opacity);
                        e.DA.DT.FillRectangle(e.DA.Offset.ToVector2(), TargetBounds, CheckMarkColor * e.DA.Opacity);

                    }
                    else if (this.IsChecked.Value)
                    {
                        Rectangle TargetBounds = ButtonElement.LayoutBounds.GetCompressed(ButtonElement.Padding);
                        if (IsCheckMarkShadowed)
                            DrawCheckMark(GetDesktop(), TargetBounds, e.DA.DT, e.DA.Opacity, e.DA.Offset + CheckMarkShadowOffset, CheckMarkShadowColor);
                        DrawCheckMark(GetDesktop(), TargetBounds, e.DA.DT, e.DA.Opacity, e.DA.Offset, CheckMarkColor);
                    }
                };

                this.ButtonComponent = new(ButtonElement, false, true, true, true, false, false, false,
                    (AvailableBounds, ComponentSize) => ApplyAlignment(AvailableBounds, HorizontalAlignment.Left, VerticalAlignment.Center, ComponentSize.Size));

                AddComponent(ButtonComponent);

                this.CheckBoxComponentSize = DefaultCheckBoxSize;
                this.SpacingWidth = DefaultCheckBoxSpacingWidth;
                this.CheckMarkColor = GetTheme().CheckMarkColor;
                this.IsCheckMarkShadowed = false;
                this.CheckMarkShadowColor = Color.Black;
                this.CheckMarkShadowOffset = new(0, 1);

                this.IsThreeState = !IsChecked.HasValue;
                this.IsChecked = IsChecked;
                this.IsReadonly = false;
            }
        }

        public static void DrawCheckMark(MGDesktop Desktop, Rectangle Bounds, DrawTransaction DT, float Opacity, Point Offset, Color Color)
        {
#if true
            Vector2 TopLeft = Bounds.TopLeft().ToVector2();
            List<Vector2> CheckMarkVertices = new()
            {
                TopLeft + new Vector2(Bounds.Width * 0.2f, Bounds.Height * 0.5f),
                TopLeft + new Vector2(Bounds.Width * 0.5f, Bounds.Height * 0.8f),
                TopLeft + new Vector2(Bounds.Width * 0.8f, Bounds.Height * 0.12f)
            };

            foreach ((Vector2 v0, Vector2 v1) in CheckMarkVertices.SelectConsecutivePairs(false))
            {
                DT.StrokeLineSegment(Offset.ToVector2(), v0, v1, Color * Opacity, 2);
            }
#else
            Rectangle CheckMarkBounds = Bounds.GetScaledFromCenter(0.75f).GetTranslated(Offset);
            //Desktop.Resources.TryDrawTexture(DT, "CheckMarkGreen_12x12", CheckMarkBounds, Opacity, Color);
            DT.DrawTextureTo(Desktop.Resources.Textures["CheckMark_64x64"], null, CheckMarkBounds, Color);
#endif
        }

        public override void DrawSelf(ElementDrawArgs DA, Rectangle LayoutBounds)
        {

        }
    }
}
