using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MGUI.Shared.Helpers;
using MGUI.Core.UI.Text;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ColorTranslator = System.Drawing.ColorTranslator;
using MonoGame.Extended;
using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Brushes.Fill_Brushes;

namespace MGUI.Core.UI
{
    public class MGProgressBar : MGElement
    {
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

        #region Value
        /// <summary>Provides direct access to the textblock component that displays the current value if <see cref="ShowValue"/> is true.</summary>
        public MGComponent<MGTextBlock> ValueComponent { get; }
        private MGTextBlock ValueElement { get; }

        public enum ValueDisplayType
        {
            /// <summary>The current <see cref="Value"/> will not be shown.</summary>
            None,
            /// <summary>The current <see cref="Value"/> will be shown (up to 2 decimal places)</summary>
            AsExact,
            /// <summary>The current <see cref="Value"/>, but will be displayed using its percentage (<see cref="ValuePercent"/>)</summary>
            AsPercent
        }

        private bool _ShowValue;
        /// <summary>True if the current value should be displayed.<para/>
        /// The value is rendered to the 'end' edge of the bar, which depends on <see cref="Orientation"/> and <see cref="IsReversed"/>.<br/>
        /// If <see cref="Orientation"/> is <see cref="Orientation.Horizontal"/>, and <see cref="IsReversed"/> is false, the value is drawn to the right edge of this <see cref="MGElement"/>'s bounds.<para/>
        /// Default value: false<para/>
        /// See also: <see cref="ValueDisplayFormat"/></summary>
        public bool ShowValue
        {
            get => _ShowValue;
            set
            {
                if (_ShowValue != value)
                {
                    _ShowValue = value;
                    NPC(nameof(ShowValue));
                    ValueElement.Visibility = ShowValue ? Visibility.Visible : Visibility.Collapsed;
                    UpdateDisplayedValue();
                }
            }
        }

        public const string RecommendedPercentageValueDisplayFormat = "[fg=White][b][shadow=Black 1 1]{{ValuePercent}}%[/shadow][/b][/fg]";
        public const string RecommendedExactValueDisplayFormat = "[fg=White][b][shadow=Black 1 1]{{Value-Minimum}}[/shadow][/b] / [b][shadow=Black 1 1]{{Maximum-Minimum}}[/shadow][/b][/fg]";

        private string _ValueDisplayFormat;
        /// <summary>Only relevant if <see cref="ShowValue"/>==true.<para/>
        /// A format string to use when computing the text to display.<para/>
        /// "{{Minimum}}", "{{Maximum}}", "{{Value}}", and "{{ValuePercent}}" will be replaced with their actual underlying values when formatting the string.<br/>
        /// "{{Value-Minimum}}" will be replaced with the mathematical result of: <see cref="Value"/> - <see cref="Minimum"/><br/>
        /// "{{Maximum-Minimum}}" will be replaced with the mathemtical result of: <see cref="Maximum"/> - <see cref="Minimum"/><para/>
        /// Default value: <see cref="RecommendedPercentageValueDisplayFormat"/><para/>
        /// This value supports some basic text markdown, such as "[b]" for bold text, "[fg=Red]" to set the text foreground color to a given value, "[opacity=0.5]" etc.<para/>
        /// See also: <see cref="ShowValue"/>, <see cref="Minimum"/>, <see cref="Maximum"/>, <see cref="Value"/>, <see cref="ValuePercent"/>,<br/>
        /// <see cref="RecommendedPercentageValueDisplayFormat"/>, <see cref="RecommendedExactValueDisplayFormat"/></summary>
        public string ValueDisplayFormat
        {
            get => _ValueDisplayFormat;
            set
            {
                if (_ValueDisplayFormat != value)
                {
                    _ValueDisplayFormat = value;
                    NPC(nameof(ValueDisplayFormat));
                    UpdateDisplayedValue();
                }
            }
        }

        private void UpdateDisplayedValue()
        {
            if (ShowValue)
            {
                string FormattedValue;
                if (string.IsNullOrEmpty(ValueDisplayFormat))
                {
                    FormattedValue = "";
                }
                else
                {
                    List<(string, string)> Tokens = new()
                    {
                        ($@"{{{{{nameof(Minimum)}}}}}", Minimum.ToString("0.##")),
                        ($@"{{{{{nameof(Maximum)}}}}}", Maximum.ToString("0.##")),
                        ($@"{{{{{nameof(Value)}}}}}", Value.ToString("0.##")),
                        ($@"{{{{{nameof(ValuePercent)}}}}}", ValuePercent.ToString("0.##")),
                        ($@"{{{{{nameof(Value)}-{nameof(Minimum)}}}}}", (Value - Minimum).ToString("0.##")),
                        ($@"{{{{{nameof(Maximum)}-{nameof(Minimum)}}}}}", (Maximum - Minimum).ToString("0.##")),
                    };

                    FormattedValue = ValueDisplayFormat;
                    foreach (var Item in Tokens)
                    {
                        FormattedValue = FormattedValue.Replace(Item.Item1, Item.Item2);
                    }
                }

                ValueElement.Text = FormattedValue;
            }
        }

        private float _Minimum;
        public float Minimum
        {
            get => _Minimum;
            set
            {
                if (_Minimum != value)
                {
                    _Minimum = value;
                    NPC(nameof(Minimum));
                    NPC(nameof(ActualValue));
                    UpdateDisplayedValue();
                }
            }
        }

        private float _Maximum;
        public float Maximum
        {
            get => _Maximum;
            set
            {
                if (_Maximum != value)
                {
                    _Maximum = value;
                    NPC(nameof(Maximum));
                    NPC(nameof(ActualValue));
                    UpdateDisplayedValue();
                }
            }
        }

        private float _Value;
        public float Value
        {
            get => _Value;
            set
            {
                if (_Value != value)
                {
                    _Value = value;
                    NPC(nameof(Value));
                    NPC(nameof(ActualValue));
                    UpdateDisplayedValue();
                }
            }
        }

        public float ActualValue => Maximum.IsLessThan(Minimum) ? Minimum : Math.Clamp(Value, Minimum, Maximum);

        /// <summary>Completed percent, between 0.0 and 100.0</summary>
        public float ValuePercent => (Maximum.IsAlmostEqual(Minimum) ? 1.0f : (ActualValue - Minimum) / (Maximum - Minimum)) * 100.0f;
        #endregion Value

        private int _Size;
        /// <summary>For a horizontal progress bar, this represents the requested Height.<br/>
        /// For a vertical progress bar, this represents the requested Width.</summary>
        public int Size
        {
            get => _Size;
            set
            {
                if (_Size != value)
                {
                    _Size = value;
                    NPC(nameof(Size));
                    LayoutChanged(this, true);
                }
            }
        }

        /// <summary>The brush to use for the completed portion of this <see cref="MGProgressBar"/></summary>
        public IFillBrush CompletedBrush { get; set; }

        /// <summary>The brush to use for the incomplete portion of this <see cref="MGProgressBar"/>. Can be null. This brush is rendered overtop of <see cref="MGElement.BackgroundBrush"/></summary>
        public IFillBrush IncompleteBrush { get; set; }

        private Orientation _Orientation;
        /// <summary>If <see cref="Orientation.Horizontal"/>, the bar grows from left to right.<br/>
        /// If <see cref="Orientation.Vertical"/>, the bar grows from bottom to top.<para/>
        /// Default value: <see cref="Orientation.Horizontal"/><para/>
        /// See also: <see cref="IsReversed"/></summary>
        public Orientation Orientation
        {
            get => _Orientation;
            set
            {
                if (_Orientation != value)
                {
                    _Orientation = value;
                    NPC(nameof(Orientation));
                    LayoutChanged(this, true);
                }
            }
        }

        public bool IsHorizontal => Orientation == Orientation.Horizontal;
        public bool IsVertical => Orientation == Orientation.Vertical;

        private bool _IsReversed;
        /// <summary>If true, the bar will flow in the opposite direction.<para/>
        /// If <see cref="Orientation"/> is <see cref="Orientation.Horizontal"/>, progress bar starts on the right, and completes on the left.<br/>
        /// If <see cref="Orientation"/> is <see cref="Orientation.Vertical"/>, progress bar starts on the top, and completes on the bottom.<para/>
        /// This value also reverses the placement of the current value, if <see cref="ShowValue"/> is true.<para/>
        /// Default value: false</summary>
        public bool IsReversed
        {
            get => _IsReversed;
            set
            {
                if (_IsReversed != value)
                {
                    _IsReversed = value;
                    NPC(nameof(IsReversed));
                }
            }
        }

        /// <param name="Value">The current value. This should be in the inclusive range [<paramref name="Minimum"/>, <paramref name="Maximum"/>]</param>
        /// <param name="Size">The size, in pixels, of the bar. If the orientation is <see cref="Orientation.Horizontal"/>, this represents the height of the bar.</param>
        /// <param name="ShowValue">If true, the current value will be displayed as a piece of text on the inside of the end of the bar.</param>
        public MGProgressBar(MGWindow Window, float Minimum = 0, float Maximum = 100, float Value = 0, int Size = 22, bool ShowValue = false, Orientation Orientation = Orientation.Horizontal)
            : base(Window, MGElementType.ProgressBar)
        {
            using (BeginInitializing())
            {
                this.BorderElement = new(Window);
                this.BorderComponent = MGComponentBase.Create(BorderElement);
                AddComponent(BorderComponent);

                this.ValueElement = new(Window, "", Color.White, 12);
                this.ValueComponent = new(ValueElement, ComponentUpdatePriority.AfterContents, ComponentDrawPriority.BeforeContents,
                    true, true, false, false, false, false, false,
                    (AvailableBounds, ComponentSize) =>
                    {
                        return Orientation switch
                        {
                            Orientation.Horizontal => ApplyAlignment(AvailableBounds, IsReversed ? HorizontalAlignment.Left : HorizontalAlignment.Right, VerticalAlignment.Center, ComponentSize.Size),
                            Orientation.Vertical => ApplyAlignment(AvailableBounds, HorizontalAlignment.Center, IsReversed ? VerticalAlignment.Bottom : VerticalAlignment.Top, ComponentSize.Size),
                            _ => throw new NotImplementedException($"Unrecognized {nameof(Orientation)}: {Orientation}")
                        };
                    });
                this.ValueElement.Padding = new(3);
                AddComponent(ValueComponent);

                this.CompletedBrush = GetTheme().ProgressBarCompletedBrush.GetValue(true);
                this.IncompleteBrush = GetTheme().AccentBackground.GetValue(true);

                this.ShowValue = ShowValue;
                this.ValueDisplayFormat = RecommendedPercentageValueDisplayFormat;

                this.Size = Size;

                this.Minimum = Minimum;
                this.Maximum = Maximum;
                this.Value = Value;

                this.Padding = new(0);
                this.Orientation = Orientation;
                this.IsReversed = false;
            }
        }

        public override Thickness MeasureSelfOverride(Size AvailableSize, out Thickness SharedSize)
        {
            Thickness Self = Orientation switch
            {
                Orientation.Horizontal => new(Size, Size, 0, 0),
                Orientation.Vertical => new(Size, Size, 0, 0),
                _ => throw new NotImplementedException($"Unrecognized {nameof(Orientation)}: {Orientation}")
            };

            return Self.Add(base.MeasureSelfOverride(AvailableSize, out SharedSize));
        }

        public override MGBorder GetBorder() => BorderElement;

        public override void DrawSelf(ElementDrawArgs DA, Rectangle LayoutBounds)
        {
            Rectangle BorderlessBounds = LayoutBounds.GetCompressed(BorderThickness);
            Rectangle CompletedBounds;
            Rectangle IncompleteBounds;

            float CompletedScalar = ValuePercent / 100.0f;

            if (Orientation == Orientation.Horizontal)
            {
                if (!IsReversed)
                {
                    CompletedBounds = ApplyAlignment(BorderlessBounds, HorizontalAlignment.Left, VerticalAlignment.Stretch, new((int)(BorderlessBounds.Width * CompletedScalar), 0));
                    IncompleteBounds = RectangleUtils.GetRectangle(CompletedBounds.TopRight(), BorderlessBounds.BottomRight() - new Point(1, 1));
                }
                else
                {
                    CompletedBounds = ApplyAlignment(BorderlessBounds, HorizontalAlignment.Right, VerticalAlignment.Stretch, new((int)(BorderlessBounds.Width * CompletedScalar), 0));
                    IncompleteBounds = RectangleUtils.GetRectangle(BorderlessBounds.TopLeft(), CompletedBounds.BottomLeft() - new Point(1, 1));
                }
            }
            else if (Orientation == Orientation.Vertical)
            {
                if (!IsReversed)
                {
                    CompletedBounds = ApplyAlignment(BorderlessBounds, HorizontalAlignment.Stretch, VerticalAlignment.Bottom, new(0, (int)(BorderlessBounds.Height * CompletedScalar)));
                    IncompleteBounds = RectangleUtils.GetRectangle(BorderlessBounds.TopLeft(), CompletedBounds.TopRight() - new Point(1, 1));
                }
                else
                {
                    CompletedBounds = ApplyAlignment(BorderlessBounds, HorizontalAlignment.Stretch, VerticalAlignment.Top, new(0, (int)(BorderlessBounds.Height * CompletedScalar)));
                    IncompleteBounds = RectangleUtils.GetRectangle(CompletedBounds.BottomLeft(), BorderlessBounds.BottomRight() - new Point(1, 1));
                }
            }
            else
                throw new NotImplementedException($"Unrecognized {nameof(Orientation)}: {Orientation}");

            IncompleteBrush?.Draw(DA, this, IncompleteBounds);
            CompletedBrush?.Draw(DA, this, CompletedBounds);

            DrawSelfBaseImplementation(DA, LayoutBounds);
        }
    }
}
