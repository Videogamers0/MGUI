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

    /// <summary>A container class for multiple <see cref="IFillBrush"/>es, where a specific one is chosen based on an <see cref="MGElement"/>'s <see cref="VisualState"/></summary>
    public class VisualStateBrush : VisualStateSetting<IFillBrush>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Color? _HoveredColor;
        /// <summary>An overlay color that is drawn overtop of <see cref="IFillBrush"/> if the mouse is currently hovering the <see cref="MGElement"/>.<br/>
        /// Recommended to use a transparent color.<para/>
        /// Default Value: <see cref="MGTheme.HoveredColor"/></summary>
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
        /// when the mouse is currently pressed, but not yet released, overtop of the <see cref="MGElement"/>.<br/>
        /// Default value: <see cref="MGTheme.PressedDarkenModifier"/></summary>
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
                HoveredOverlay = new MGSolidFillBrush(HoveredColor.Value);
                HoveredBorderOverlay = new(HoveredOverlay);
            }
            else
            {
                HoveredOverlay = MGSolidFillBrush.Transparent;
                HoveredBorderOverlay = MGUniformBorderBrush.Transparent;
            }
        }

        private void UpdatePressedOverlay()
        {
            if (HoveredColor.HasValue)
            {
                PressedOverlay = PressedModifierType switch
                {
                    PressedModifierType.Darken => new MGSolidFillBrush(HoveredColor.Value.Darken(PressedModifier)),
                    PressedModifierType.Brighten => new MGSolidFillBrush(HoveredColor.Value.Brighten(PressedModifier)),
                    _ => throw new NotImplementedException($"Unrecognized {nameof(PressedModifierType)}: {PressedModifierType}")
                };
                PressedBorderOverlay = new(PressedOverlay);
            }
            else
            {
                PressedOverlay = MGSolidFillBrush.Transparent;
                PressedBorderOverlay = MGUniformBorderBrush.Transparent;
            }
        }

        public MGSolidFillBrush HoveredOverlay { get; private set; }
        public MGSolidFillBrush PressedOverlay { get; private set; }

        public MGUniformBorderBrush HoveredBorderOverlay { get; private set; }
        public MGUniformBorderBrush PressedBorderOverlay { get; private set; }

        public IFillBrush GetUnderlay(PrimaryVisualState State) => GetValue(State);
        public IFillBrush GetOverlay(SecondaryVisualState State) =>
            State switch
            {
                SecondaryVisualState.Pressed => PressedOverlay,
                SecondaryVisualState.Hovered => HoveredOverlay,
                SecondaryVisualState.None => null,
                _ => throw new NotImplementedException($"Unrecognized {nameof(SecondaryVisualState)}: {State}")
            };

        public IBorderBrush GetBorderOverlay(SecondaryVisualState State) =>
            State switch
            {
                SecondaryVisualState.Pressed => PressedBorderOverlay,
                SecondaryVisualState.Hovered => HoveredBorderOverlay,
                SecondaryVisualState.None => null,
                _ => throw new NotImplementedException($"Unrecognized {nameof(SecondaryVisualState)}: {State}")
            };

        /// <param name="Brush">See also: <see cref="MGSolidFillBrush"/>, <see cref="MGCompositedFillBrush"/>, <see cref="MGTextureFillBrush"/>, <see cref="MGGradientFillBrush"/>, <see cref="MGDiagonalGradientFillBrush"/></param>
        public VisualStateBrush(MGTheme Theme, IFillBrush Brush)
            : this(Theme, Brush, null) { }

        /// <param name="Brush">See also: <see cref="MGSolidFillBrush"/>, <see cref="MGCompositedFillBrush"/>, <see cref="MGTextureFillBrush"/>, <see cref="MGGradientFillBrush"/>, <see cref="MGDiagonalGradientFillBrush"/></param>
        public VisualStateBrush(MGTheme Theme, IFillBrush Brush, Color? HoveredColor)
            : this(Theme, Brush, Brush, Brush, HoveredColor) { }

        public VisualStateBrush(MGTheme Theme, IFillBrush NormalBrush, IFillBrush SelectedBrush, IFillBrush DisabledBrush, Color? HoveredColor)
            : base(NormalBrush, SelectedBrush, DisabledBrush)
        {
            this.HoveredColor = HoveredColor;
            this.PressedModifierType = PressedModifierType.Darken;
            this.PressedModifier = PressedModifierType switch
            {
                PressedModifierType.Darken => Theme.PressedDarkenModifier,
                PressedModifierType.Brighten => Theme.PressedBrightenModifier,
                _ => throw new NotImplementedException($"Unrecognized {nameof(PressedModifierType)}: {PressedModifierType}")
            };
        }

        private VisualStateBrush(VisualStateBrush InheritFrom)
            : base(InheritFrom.NormalValue, InheritFrom.SelectedValue, InheritFrom.DisabledValue)
        {
            this.HoveredColor = InheritFrom.HoveredColor;
            this.PressedModifierType = InheritFrom._PressedModifierType;
            this.PressedModifier = InheritFrom.PressedModifier;
        }

        public VisualStateBrush GetCopy() => new(this);
    }
}
