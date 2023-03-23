using Microsoft.Xna.Framework;
using MonoGame.Extended;
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
    public class MGTimer : MGElement
    {
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

        /// <summary>Provides direct access to the textblock component that displays this timer's <see cref="RemainingDuration"/> time.</summary>
        public MGComponent<MGTextBlock> ValueComponent { get; }
        private MGTextBlock ValueElement { get; }

        public bool TrySetFont(string FontFamily, int FontSize) => ValueElement.TrySetFont(FontFamily, FontSize);

        private void UpdateDisplayedValue(bool ForceLayoutRefresh)
        {
            if (ValueElement == null || ValueDisplayFormat == null || RemainingDurationToString == null)
                return;

            string RemainingDurationDisplayString = RemainingDurationToString(RemainingDuration);
            string ValueDisplayString = ValueDisplayFormat.Replace($"{{{{{nameof(RemainingDuration)}}}}}", RemainingDurationDisplayString);

            //  Assume the required size of this element hasn't changed if the text length stayed the same
            //  This assumption may be incorrect for non-monospaced font
            ValueElement.SetText(ValueDisplayString, !ForceLayoutRefresh && (ValueElement.Text?.Length ?? 0) == ValueDisplayString.Length);
        }

        public const string DefaultValueDisplayFormat = $"[b][shadow=Black 1 1]{{{{{nameof(RemainingDuration)}}}}}[/shadow][/b]";

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _ValueDisplayFormat;
        /// <summary>A format string to use when computing the text to display.<br/>
        /// "{{RemainingDuration}}" will be replaced with the value retrieved via <see cref="RemainingDurationToString"/>.<para/>
        /// Default value: <see cref="DefaultValueDisplayFormat"/><para/>
        /// This value supports some basic text markdown, such as "[b]" for bold text, "[fg=Red]" to set the text foreground color to a given value, "[opacity=0.5]" etc.</summary>
        public string ValueDisplayFormat
        {
            get => _ValueDisplayFormat;
            set
            {
                if (_ValueDisplayFormat != value)
                {
                    _ValueDisplayFormat = value;
                    UpdateDisplayedValue(true);
                    NPC(nameof(ValueDisplayFormat));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private TimeSpan _RemainingDuration;
        public TimeSpan RemainingDuration
        {
            get => _RemainingDuration;
            set
            {
                TimeSpan ActualValue = AllowsNegativeDuration || RemainingDuration.TotalSeconds >= 0 ? value : TimeSpan.Zero;
                if (_RemainingDuration != ActualValue)
                {
                    TimeSpan Previous = RemainingDuration;
                    _RemainingDuration = ActualValue;
                    UpdateDisplayedValue(false);
                    NPC(nameof(RemainingDuration));
                    RemainingDurationChanged?.Invoke(this, new(Previous, RemainingDuration));

                    if (RemainingDuration.TotalSeconds <= 0)
                        TimeUp?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>Invoked when <see cref="RemainingDuration"/> changes.</summary>
        public event EventHandler<EventArgs<TimeSpan>> RemainingDurationChanged;

        /// <summary>Invoked when <see cref="RemainingDuration"/> hits zero (or negative if <see cref="AllowsNegativeDuration"/> is true).</summary>
        public event EventHandler<EventArgs> TimeUp;

        public bool AllowsNegativeDuration { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private double _TimeScale = 1.0;
        /// <summary>Determines the rate at which <see cref="RemainingDuration"/> changes.<para/>
        /// Default value: 1.0, which means <see cref="RemainingDuration"/> is decremented by 1 second per 1 real-life second.<para/>
        /// For example, if the player has an ability that slows time to 0.50x, you may wish to temporarily set <see cref="TimeScale"/> to 0.50 while the ability is active,<br/>
        /// unless you still wanted the timer to track real time</summary>
        public double TimeScale
        {
            get => _TimeScale;
            set
            {
                if (_TimeScale != value)
                {
                    _TimeScale = value;
                    NPC(nameof(TimeScale));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Func<TimeSpan, string> _RemainingDurationToString;
        /// <summary>A function whose input is the <see cref="RemainingDuration"/> time, and returns the string value that should be displayed by this <see cref="MGTimer"/>.<para/>
        /// Default value: A function that returns:
        /// <code>TimeSpan.ToString(@"m\:ss\.%f")</code>
        /// For example, a value of 13566 seconds would be formatted as: "46:06.0"<para/>
        /// More info: <see href="https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-timespan-format-strings"/></summary>
        public Func<TimeSpan, string> RemainingDurationToString
        {
            get => _RemainingDurationToString;
            set
            {
                if (_RemainingDurationToString != value)
                {
                    _RemainingDurationToString = value;
                    UpdateDisplayedValue(true);
                    NPC(nameof(RemainingDurationToString));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _IsPaused;
        /// <summary>See also: <see cref="Pause"/>, <see cref="Resume"/>, <see cref="Paused"/>, <see cref="Resumed"/></summary>
        public bool IsPaused
        {
            get => _IsPaused;
            set
            {
                if (_IsPaused != value)
                {
                    _IsPaused = value;
                    NPC(nameof(IsPaused));
                    if (IsPaused)
                        Paused?.Invoke(this, EventArgs.Empty);
                    else
                        Resumed?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>Invoked when this <see cref="MGTimer"/> is paused (<see cref="IsPaused"/> set to true)</summary>
        public event EventHandler<EventArgs> Paused;
        /// <summary>Invoked when this <see cref="MGTimer"/> is resumed (<see cref="IsPaused"/> set to false)</summary>
        public event EventHandler<EventArgs> Resumed;

        /// <summary>Pause the <see cref="MGTimer"/> if not already paused.<br/>
        /// See also: <see cref="Resume"/>, <see cref="IsPaused"/></summary>
        public void Pause() => IsPaused = true;
        /// <summary>Resume the <see cref="MGTimer"/> if paused.<br/>
        /// See also: <see cref="Pause"/>, <see cref="IsPaused"/></summary>
        public void Resume() => IsPaused = false;

        //TODO
        //Additional settings that make the timer flicker/highlight/shake or something when certain RemainingDuration thresholds are hit?
        //      Such as a timer that flashes Red when 10s or less are remaining Idk

        /// <param name="IsPaused">If true, <see cref="RemainingDuration"/> will not decrement until <see cref="Resume"/> is called.</param>
        /// <param name="AllowsNegativeDuration">If true, <see cref="RemainingDuration"/> will continue decreasing even after hitting 0.</param>
        public MGTimer(MGWindow Window, TimeSpan Duration, bool IsPaused = true, bool AllowsNegativeDuration = false)
            : base(Window, MGElementType.Timer)
        {
            using (BeginInitializing())
            {
                this.AllowsNegativeDuration = AllowsNegativeDuration;
                this.IsPaused = IsPaused;
                this.RemainingDuration = Duration;

                this.BorderElement = new(Window);
                this.BorderComponent = MGComponentBase.Create(BorderElement);
                AddComponent(BorderComponent);
                BorderElement.OnBorderBrushChanged += (sender, e) => { NPC(nameof(BorderBrush)); };
                BorderElement.OnBorderThicknessChanged += (sender, e) => { NPC(nameof(BorderThickness)); };

                this.ValueElement = new(Window, "", Color.White, GetTheme().FontSettings.MediumFontSize);
                this.ValueComponent = new(ValueElement, false, false, true, true, false, false, true,
                    (AvailableBounds, ComponentSize) => ApplyAlignment(AvailableBounds.GetCompressed(Padding), HorizontalContentAlignment, VerticalContentAlignment, ComponentSize.Size));
                AddComponent(ValueComponent);

                this.HorizontalContentAlignment = HorizontalAlignment.Center;
                this.VerticalContentAlignment = VerticalAlignment.Center;
                this.Padding = new(4, 2, 4, 2);

                this.RemainingDurationToString = (TimeSpan Elapsed) => Elapsed.ToString(@"m\:ss\.%f");
                this.ValueDisplayFormat = DefaultValueDisplayFormat;
            }
        }

        public override void UpdateSelf(ElementUpdateArgs UA)
        {
            base.UpdateSelf(UA);
            if (!IsPaused)
            {
                RemainingDuration -= UA.BA.FrameElapsed * TimeScale;
            }
        }
    }
}