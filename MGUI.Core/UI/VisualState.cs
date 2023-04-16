using Microsoft.Xna.Framework;
using MGUI.Shared.Helpers;
using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI
{
    public enum PrimaryVisualState
    {
        /// <summary>Highest priority. I.E. if an <see cref="MGElement"/> is both Disabled and Selected, it is treated as Disabled.<br/>
        /// See also: <see cref="MGElement.IsEnabled"/>, <see cref="MGElement.DerivedIsEnabled"/></summary>
        Disabled,
        /// <summary>See also: <see cref="MGElement.IsSelected"/>, <see cref="MGElement.DerivedIsSelected"/></summary>
        Selected,
        Normal
    }

    public enum SecondaryVisualState
    {
        /// <summary>Highest priority. I.E. if an <see cref="MGElement"/> is both Pressed and Hovered, it is treated as Pressed.<para/>
        /// Indicates that the Left MouseButton is currently pressed overtop of the <see cref="MGElement"/></summary>
        Pressed,
        Hovered,
        None
    }

    public readonly record struct VisualState(PrimaryVisualState Primary, SecondaryVisualState Secondary)
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]public bool IsDisabled => Primary == PrimaryVisualState.Disabled;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]public bool IsSelected => Primary == PrimaryVisualState.Selected;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]public bool IsPressed => Secondary == SecondaryVisualState.Pressed;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]public bool IsHovered => Secondary == SecondaryVisualState.Hovered;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]public bool IsPressedOrHovered => IsPressed || IsHovered;

        public SecondaryVisualState GetSecondaryState(bool SpoofIsPressed, bool SpoofIsHovered)
        {
            if (SpoofIsPressed)
                return SecondaryVisualState.Pressed;
            else if (SpoofIsHovered && Secondary != SecondaryVisualState.Pressed)
                return SecondaryVisualState.Hovered;
            else
                return Secondary;
        }
    }

    public class VisualStateSetting<TDataType> : ViewModelBase
    {
        private readonly EqualityComparer<TDataType> EqualityComparer = EqualityComparer<TDataType>.Default;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private TDataType _DisabledValue;
        public TDataType DisabledValue
        {
            get => _DisabledValue;
            set
            {
                if (!EqualityComparer.Equals(_DisabledValue, value))
                {
                    _DisabledValue = value;
                    NPC(nameof(DisabledValue));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private TDataType _SelectedValue;
        public TDataType SelectedValue
        {
            get => _SelectedValue;
            set
            {
                if (!EqualityComparer.Equals(_SelectedValue, value))
                {
                    _SelectedValue = value;
                    NPC(nameof(SelectedValue));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private TDataType _NormalValue;
        public TDataType NormalValue
        {
            get => _NormalValue;
            set
            {
                if (!EqualityComparer.Equals(_NormalValue, value))
                {
                    _NormalValue = value;
                    NPC(nameof(NormalValue));
                }
            }
        }

        public TDataType GetValue(PrimaryVisualState State)
            => State switch
            {
                PrimaryVisualState.Disabled => DisabledValue,
                PrimaryVisualState.Selected => SelectedValue,
                PrimaryVisualState.Normal => NormalValue,
                _ => throw new NotImplementedException($"Unrecognized {nameof(PrimaryVisualState)}: {State}")
            };

        public void SetAll(TDataType Value)
        {
            this.DisabledValue = Value;
            this.SelectedValue = Value;
            this.NormalValue = Value;
        }

        public VisualStateSetting(TDataType Value)
            : this(Value, Value, Value) { }

        public VisualStateSetting(TDataType NormalValue, TDataType SelectedValue, TDataType DisabledValue)
        {
            this.NormalValue = NormalValue;
            this.SelectedValue = SelectedValue;
            this.DisabledValue = DisabledValue;
        }

        public VisualStateSetting<TDataType> GetCopy() => new(NormalValue, SelectedValue, DisabledValue);
    }

    public enum PressedModifierType
    {
        Darken,
        Brighten
    }

    public abstract class VisualStateBrush<TDataType> : VisualStateSetting<TDataType>, ICloneable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Color? _HoveredColor;
        /// <summary>An overlay color that is drawn overtop if the mouse is currently hovering the <see cref="MGElement"/>.<br/>
        /// Recommended to use a transparent color.<para/>
        /// If the mouse is also pressed overtop of the <see cref="MGElement"/>, then this color is further adjusted based on <see cref="PressedModifierType"/> and <see cref="PressedModifier"/></summary>
        public Color? FocusedColor
        {
            get => _HoveredColor;
            set
            {
                if (_HoveredColor != value)
                {
                    _HoveredColor = value;
                    UpdateFocusedOverlay();
                    UpdatePressedOverlay();
                    NPC(nameof(FocusedColor));
                    NPC(nameof(HoveredColorOverlay));
                    NPC(nameof(PressedColorOverlay));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private float _PressedModifier;
        /// <summary>A percentage to darken or brighten (depending on <see cref="PressedModifierType"/>) the <see cref="FocusedColor"/> by 
        /// when the mouse is currently pressed, but not yet released, overtop of the <see cref="MGElement"/>.</summary>
        public float PressedModifier
        {
            get => _PressedModifier;
            set
            {
                if (_PressedModifier != value)
                {
                    _PressedModifier = value;
                    UpdatePressedOverlay();
                    NPC(nameof(PressedModifier));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private PressedModifierType _PressedModifierType;
        public PressedModifierType PressedModifierType
        {
            get => _PressedModifierType;
            set
            {
                if (_PressedModifierType != value)
                {
                    _PressedModifierType = value;
                    UpdatePressedOverlay();
                    NPC(nameof(PressedModifierType));
                }
            }
        }

        private void UpdateFocusedOverlay()
        {
            if (FocusedColor.HasValue)
            {
                HoveredFillOverlay = new MGSolidFillBrush(FocusedColor.Value);
                HoveredBorderOverlay = new(HoveredFillOverlay);
            }
            else
            {
                HoveredFillOverlay = MGSolidFillBrush.Transparent;
                HoveredBorderOverlay = MGUniformBorderBrush.Transparent;
            }
            NPC(nameof(HoveredColorOverlay));
            NPC(nameof(HoveredFillOverlay));
            NPC(nameof(HoveredBorderOverlay));
        }

        private void UpdatePressedOverlay()
        {
            if (FocusedColor.HasValue)
            {
                PressedColorOverlay = PressedModifierType switch
                {
                    PressedModifierType.Darken => FocusedColor.Value.Darken(PressedModifier),
                    PressedModifierType.Brighten => FocusedColor.Value.Brighten(PressedModifier),
                    _ => throw new NotImplementedException($"Unrecognized {nameof(PressedModifierType)}: {PressedModifierType}")
                };
                PressedFillOverlay = new(PressedColorOverlay.Value);
                PressedBorderOverlay = new(PressedFillOverlay);
            }
            else
            {
                PressedColorOverlay = null;
                PressedFillOverlay = MGSolidFillBrush.Transparent;
                PressedBorderOverlay = MGUniformBorderBrush.Transparent;
            }
            NPC(nameof(PressedColorOverlay));
            NPC(nameof(PressedFillOverlay));
            NPC(nameof(PressedBorderOverlay));
        }

        private Color? HoveredColorOverlay => FocusedColor;
        private Color? PressedColorOverlay { get; set; }

        private MGSolidFillBrush HoveredFillOverlay { get; set; }
        private MGSolidFillBrush PressedFillOverlay { get; set; }

        private MGUniformBorderBrush HoveredBorderOverlay { get; set; }
        private MGUniformBorderBrush PressedBorderOverlay { get; set; }

        public TDataType GetUnderlay(PrimaryVisualState State) => GetValue(State);

        /// <summary>Retrieves the <see cref="Color"/> used by <see cref="GetFillOverlay(SecondaryVisualState)"/> / <see cref="GetBorderOverlay(SecondaryVisualState)"/></summary>
        public Color? GetColorOverlay(SecondaryVisualState State) =>
            State switch
            {
                SecondaryVisualState.Pressed => PressedColorOverlay,
                SecondaryVisualState.Hovered => FocusedColor,
                SecondaryVisualState.None => null,
                _ => throw new NotImplementedException($"Unrecognized {nameof(SecondaryVisualState)}: {State}")
            };

        /// <summary>Returns a <see cref="IFillBrush"/> that should be rendered overtop of the element's graphics, not including the border's bounds.</summary>
        public MGSolidFillBrush? GetFillOverlay(SecondaryVisualState State) =>
            State switch
            {
                SecondaryVisualState.Pressed => PressedFillOverlay,
                SecondaryVisualState.Hovered => HoveredFillOverlay,
                SecondaryVisualState.None => null,
                _ => throw new NotImplementedException($"Unrecognized {nameof(SecondaryVisualState)}: {State}")
            };

        /// <summary>Returns a <see cref="IBorderBrush"/> that should be rendered overtop of the border portion of the element's graphics.</summary>
        public MGUniformBorderBrush? GetBorderOverlay(SecondaryVisualState State) =>
            State switch
            {
                SecondaryVisualState.Pressed => PressedBorderOverlay,
                SecondaryVisualState.Hovered => HoveredBorderOverlay,
                SecondaryVisualState.None => null,
                _ => throw new NotImplementedException($"Unrecognized {nameof(SecondaryVisualState)}: {State}")
            };

        protected VisualStateBrush(TDataType NormalValue, TDataType SelectedValue, TDataType DisabledValue, 
            Color? HoveredColor, PressedModifierType PressedModifierType, float PressedModifier)
            : base(NormalValue, SelectedValue, DisabledValue)
        {
            this.FocusedColor = HoveredColor;
            this.PressedModifierType = PressedModifierType;
            this.PressedModifier = PressedModifier;
        }

        public abstract object Clone();
    }

    /// <summary>A wrapper class for multiple <see cref="IFillBrush"/>es, where a specific one is chosen based on an <see cref="MGElement"/>'s <see cref="VisualState"/></summary>
    public class VisualStateFillBrush : VisualStateBrush<IFillBrush>
    {
        public VisualStateFillBrush(IFillBrush Brush)
            : this(Brush, null, PressedModifierType.Darken, 0.06f) { }

        public VisualStateFillBrush(IFillBrush Brush, Color? HoveredColor, PressedModifierType PressedModifierType, float PressedModifier)
            : this(Brush, Brush, Brush, HoveredColor, PressedModifierType, PressedModifier) { }

        public VisualStateFillBrush(IFillBrush NormalBrush, IFillBrush SelectedBrush, IFillBrush DisabledBrush, Color? HoveredColor, PressedModifierType PressedModifierType, float PressedModifier)
            : base(NormalBrush, SelectedBrush, DisabledBrush, HoveredColor, PressedModifierType, PressedModifier) { }

        private VisualStateFillBrush(VisualStateFillBrush InheritFrom)
            : base(InheritFrom.NormalValue?.Copy(), InheritFrom.SelectedValue?.Copy(), InheritFrom.DisabledValue?.Copy(),
                  InheritFrom.FocusedColor, InheritFrom.PressedModifierType, InheritFrom.PressedModifier) { }

        public VisualStateFillBrush Copy() => new(this);
        public override object Clone() => Copy();
    }

    /// <summary>A wrapper class for multiple <see cref="Color"/>s, where a specific one is chosen based on an <see cref="MGElement"/>'s <see cref="VisualState"/></summary>
    public class VisualStateColorBrush : VisualStateBrush<Color>
    {
        public VisualStateColorBrush(Color Color)
            : this(Color, null, PressedModifierType.Darken, 0.06f) { }

        public VisualStateColorBrush(Color Color, Color? HoveredColor, PressedModifierType PressedModifierType, float PressedModifier)
            : this(Color, Color, Color, HoveredColor, PressedModifierType, PressedModifier) { }

        public VisualStateColorBrush(Color NormalColor, Color SelectedColor, Color DisabledColor, Color? HoveredColor, PressedModifierType PressedModifierType, float PressedModifier)
            : base(NormalColor, SelectedColor, DisabledColor, HoveredColor, PressedModifierType, PressedModifier) { }

        private VisualStateColorBrush(VisualStateColorBrush InheritFrom)
            : base(InheritFrom.NormalValue, InheritFrom.SelectedValue, InheritFrom.DisabledValue,
                  InheritFrom.FocusedColor, InheritFrom.PressedModifierType, InheritFrom.PressedModifier) { }

        public VisualStateColorBrush Copy() => new(this);
        public override object Clone() => Copy();
    }
}
