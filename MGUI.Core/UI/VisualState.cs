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
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsDisabled => Primary == PrimaryVisualState.Disabled;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsSelected => Primary == PrimaryVisualState.Selected;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsPressed => Secondary == SecondaryVisualState.Pressed;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsHovered => Secondary == SecondaryVisualState.Hovered;

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
        public TDataType DisabledValue { get; set; }
        public TDataType SelectedValue { get; set; }
        public TDataType NormalValue { get; set; }

        public TDataType GetValue(PrimaryVisualState State)
            => State switch
            {
                PrimaryVisualState.Disabled => DisabledValue,
                PrimaryVisualState.Selected => SelectedValue,
                PrimaryVisualState.Normal => NormalValue,
                _ => throw new NotImplementedException($"Unrecognized {nameof(PrimaryVisualState)}: {State}")
            };

        public VisualStateSetting(TDataType NormalValue, TDataType SelectedValue, TDataType DisabledValue)
        {
            this.NormalValue = NormalValue;
            this.SelectedValue = SelectedValue;
            this.DisabledValue = DisabledValue;
        }
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
        /// Recommended to use a transparent color.</summary>
        public Color? HoveredColor
        {
            get => _HoveredColor;
            set
            {
                if (_HoveredColor != value)
                {
                    _HoveredColor = value;
                    NPC(nameof(HoveredColor));
                    UpdateHoveredOverlay();
                    UpdatePressedOverlay();
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private float _PressedModifier;
        /// <summary>A percentage to darken or brighten (depending on <see cref="PressedModifierType"/>) the <see cref="HoveredColor"/> by 
        /// when the mouse is currently pressed, but not yet released, overtop of the <see cref="MGElement"/>.</summary>
        public float PressedModifier
        {
            get => _PressedModifier;
            set
            {
                if (_PressedModifier != value)
                {
                    _PressedModifier = value;
                    NPC(nameof(PressedModifier));
                    UpdatePressedOverlay();
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
                    NPC(nameof(PressedModifierType));
                    UpdatePressedOverlay();
                }
            }
        }

        private void UpdateHoveredOverlay()
        {
            if (HoveredColor.HasValue)
            {
                HoveredFillOverlay = new MGSolidFillBrush(HoveredColor.Value);
                HoveredBorderOverlay = new(HoveredFillOverlay);
            }
            else
            {
                HoveredFillOverlay = MGSolidFillBrush.Transparent;
                HoveredBorderOverlay = MGUniformBorderBrush.Transparent;
            }
        }

        private void UpdatePressedOverlay()
        {
            if (HoveredColor.HasValue)
            {
                PressedColorOverlay = PressedModifierType switch
                {
                    PressedModifierType.Darken => HoveredColor.Value.Darken(PressedModifier),
                    PressedModifierType.Brighten => HoveredColor.Value.Brighten(PressedModifier),
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
        }

        public Color? HoveredColorOverlay => HoveredColor;
        public Color? PressedColorOverlay { get; private set; }

        public MGSolidFillBrush HoveredFillOverlay { get; private set; }
        public MGSolidFillBrush PressedFillOverlay { get; private set; }

        public MGUniformBorderBrush HoveredBorderOverlay { get; private set; }
        public MGUniformBorderBrush PressedBorderOverlay { get; private set; }

        public TDataType GetUnderlay(PrimaryVisualState State) => GetValue(State);

        public Color? GetColorOverlay(SecondaryVisualState State) =>
            State switch
            {
                SecondaryVisualState.Pressed => PressedColorOverlay,
                SecondaryVisualState.Hovered => HoveredColor,
                SecondaryVisualState.None => null,
                _ => throw new NotImplementedException($"Unrecognized {nameof(SecondaryVisualState)}: {State}")
            };

        public MGSolidFillBrush? GetFillOverlay(SecondaryVisualState State) =>
            State switch
            {
                SecondaryVisualState.Pressed => PressedFillOverlay,
                SecondaryVisualState.Hovered => HoveredFillOverlay,
                SecondaryVisualState.None => null,
                _ => throw new NotImplementedException($"Unrecognized {nameof(SecondaryVisualState)}: {State}")
            };

        public MGUniformBorderBrush? GetBorderOverlay(SecondaryVisualState State) =>
            State switch
            {
                SecondaryVisualState.Pressed => PressedBorderOverlay,
                SecondaryVisualState.Hovered => HoveredBorderOverlay,
                SecondaryVisualState.None => null,
                _ => throw new NotImplementedException($"Unrecognized {nameof(SecondaryVisualState)}: {State}")
            };

        protected VisualStateBrush(MGTheme Theme, TDataType NormalValue, TDataType SelectedValue, TDataType DisabledValue, Color? HoveredColor)
            : base(NormalValue, SelectedValue, DisabledValue)
        {
            this.HoveredColor = HoveredColor;
            this.PressedModifierType = PressedModifierType.Darken;
            this.PressedModifier = Theme.ClickablePressedModifier;
        }

        protected VisualStateBrush(TDataType NormalValue, TDataType SelectedValue, TDataType DisabledValue, 
            Color? HoveredColor, PressedModifierType PressedModifierType, float PressedModifier)
            : base(NormalValue, SelectedValue, DisabledValue)
        {
            this.HoveredColor = HoveredColor;
            this.PressedModifierType = PressedModifierType;
            this.PressedModifier = PressedModifier;
        }

        public abstract object Clone();
    }

    /// <summary>A wrapper class for multiple <see cref="IFillBrush"/>es, where a specific one is chosen based on an <see cref="MGElement"/>'s <see cref="VisualState"/></summary>
    public class VisualStateFillBrush : VisualStateBrush<IFillBrush>
    {
        public VisualStateFillBrush(MGTheme Theme, IFillBrush Brush)
            : this(Theme, Brush, null) { }

        public VisualStateFillBrush(MGTheme Theme, IFillBrush Brush, Color? HoveredColor)
            : this(Theme, Brush, Brush, Brush, HoveredColor) { }

        public VisualStateFillBrush(MGTheme Theme, IFillBrush NormalBrush, IFillBrush SelectedBrush, IFillBrush DisabledBrush, Color? HoveredColor)
            : base(Theme, NormalBrush, SelectedBrush, DisabledBrush, HoveredColor) { }

        private VisualStateFillBrush(VisualStateFillBrush InheritFrom)
            : base(InheritFrom.NormalValue?.Copy(), InheritFrom.SelectedValue?.Copy(), InheritFrom.DisabledValue?.Copy(),
                  InheritFrom.HoveredColor, InheritFrom.PressedModifierType, InheritFrom.PressedModifier) { }

        public VisualStateFillBrush Copy() => new(this);
        public override object Clone() => Copy();
    }

    /// <summary>A wrapper class for multiple <see cref="Color"/>s, where a specific one is chosen based on an <see cref="MGElement"/>'s <see cref="VisualState"/></summary>
    public class VisualStateColorBrush : VisualStateBrush<Color>
    {
        public VisualStateColorBrush(MGTheme Theme, Color Color)
            : this(Theme, Color, null) { }

        public VisualStateColorBrush(MGTheme Theme, Color Color, Color? HoveredColor)
            : this(Theme, Color, Color, Color, HoveredColor) { }

        public VisualStateColorBrush(MGTheme Theme, Color NormalColor, Color SelectedColor, Color DisabledColor, Color? HoveredColor)
            : base(Theme, NormalColor, SelectedColor, DisabledColor, HoveredColor) { }

        private VisualStateColorBrush(VisualStateColorBrush InheritFrom)
            : base(InheritFrom.NormalValue, InheritFrom.SelectedValue, InheritFrom.DisabledValue,
                  InheritFrom.HoveredColor, InheritFrom.PressedModifierType, InheritFrom.PressedModifier) { }

        public VisualStateColorBrush Copy() => new(this);
        public override object Clone() => Copy();
    }
}
