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

namespace MGUI.Core.UI
{
    public class MGCheckBox : MGSingleContentHost
    {
        /// <summary>The default width/height of the checkable part of an <see cref="MGCheckBox"/></summary>
        public const int DefaultCheckBoxSize = 16;
        /// <summary>The default empty width between the checkable part of an <see cref="MGCheckBox"/> and its <see cref="MGSingleContentHost.Content"/></summary>
        public const int DefaultCheckBoxSpacingWidth = 5;

        /// <summary>Provides direct access to the button component that appears to the left of this checkbox's content.</summary>
        public MGComponent<MGButton> ButtonComponent { get; }
        /// <summary>The checkable button portion of this <see cref="MGCheckBox"/></summary>
        private MGButton ButtonElement { get; }

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
                }
            }
        }

        /// <summary>The reserved empty width between the checkable part of this <see cref="MGCheckBox"/> and its <see cref="MGSingleContentHost.Content"/>.<para/>
        /// See also: <see cref="DefaultCheckBoxSpacingWidth"/>.<para/>
        /// This value is functionally equivalent to <see cref="ButtonElement"/>'s right <see cref="MGElement.Margin"/></summary>
        public int SpacingWidth
        {
            get => ButtonElement.Margin.Right;
            set => ButtonElement.Margin = new(ButtonElement.Margin.Left, ButtonElement.Margin.Top, value, ButtonElement.Margin.Bottom);
        }

        /// <summary>The <see cref="Color"/> to use when stroking the check mark if <see cref="IsChecked"/> is true.</summary>
        public Color CheckMarkColor { get; set; }

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
                }
            }
        }

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
                    if (IsThreeState)
                    {
                        this.IsChecked = this.IsChecked.HasValue && this.IsChecked.Value ? null : !this.IsChecked.HasValue ? false : true;
                    }
                    else
                    {
                        this.IsChecked = !this.IsChecked.HasValue || this.IsChecked.Value ? false : true;
                    }
                });

                ButtonElement.OnEndDraw += (sender, e) =>
                {
                    if (!this.IsChecked.HasValue)
                        e.DA.DT.FillRectangle(e.DA.Offset.ToVector2(), ButtonElement.LayoutBounds.GetScaledFromCenter(0.60f), CheckMarkColor * e.DA.Opacity);
                    else if (this.IsChecked.Value)
                        DrawCheckMark(ButtonElement.LayoutBounds, e.DA.DT, e.DA.Opacity, e.DA.Offset, CheckMarkColor);
                };

                this.ButtonComponent = new(ButtonElement, false, true, true, true, false, false, false,
                    (AvailableBounds, ComponentSize) => ApplyAlignment(AvailableBounds, HorizontalAlignment.Left, VerticalAlignment.Center, ComponentSize.Size));

                AddComponent(ButtonComponent);

                this.CheckBoxComponentSize = DefaultCheckBoxSize;
                this.SpacingWidth = DefaultCheckBoxSpacingWidth;
                this.CheckMarkColor = Color.Green;

                this.IsThreeState = !IsChecked.HasValue;
                this.IsChecked = IsChecked;
            }
        }

        public static void DrawCheckMark(Rectangle Bounds, DrawTransaction DT, float Opacity, Point Offset, Color Color)
        {
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
        }

        public override void DrawSelf(ElementDrawArgs DA, Rectangle LayoutBounds)
        {

        }
    }
}
