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
    public class MGStopwatch : MGElement
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

        /// <summary>Provides direct access to the textblock component that displays this stopwatch's <see cref="Elapsed"/> time.</summary>
        public MGComponent<MGTextBlock> ValueComponent { get; }
        private MGTextBlock ValueElement { get; }

        public bool TrySetFont(string FontFamily, int FontSize) => ValueElement.TrySetFont(FontFamily, FontSize);

        private void UpdateDisplayedValue(bool ForceLayoutRefresh)
        {
            if (ValueElement == null || ValueDisplayFormat == null || ElapsedToString == null)
                return;

            string ElapsedDisplayString = ElapsedToString(Elapsed);
            string ValueDisplayString = ValueDisplayFormat.Replace($"{{{{{nameof(Elapsed)}}}}}", ElapsedDisplayString);

            //  Assume the required size of this element hasn't changed if the text length stayed the same
            //  This assumption may be incorrect for non-monospaced font
            ValueElement.SetText(ValueDisplayString, !ForceLayoutRefresh && (ValueElement.Text?.Length ?? 0) == ValueDisplayString.Length);
        }

        public const string DefaultValueDisplayFormat = $"[b][shadow=Black 1 1]{{{{{nameof(Elapsed)}}}}}[/shadow][/b]";

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _ValueDisplayFormat;
        /// <summary>A format string to use when computing the text to display.<br/>
        /// "{{Elapsed}}" will be replaced with the value retrieved via <see cref="ElapsedToString"/>.<para/>
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
        private TimeSpan _Elapsed;
        /// <summary>The amount of time that has passed while this <see cref="MGStopwatch"/> was active.<para/>
        /// See also: <see cref="IsRunning"/></summary>
        public TimeSpan Elapsed
        {
            get => _Elapsed;
            set
            {
                if (_Elapsed != value)
                {
                    TimeSpan Previous = Elapsed;
                    _Elapsed = value;
                    UpdateDisplayedValue(false);
                    NPC(nameof(Elapsed));
                    ElapsedChanged?.Invoke(this, new(Previous, Elapsed));
                }
            }
        }

        /// <summary>Invoked when <see cref="Elapsed"/> changes.</summary>
        public event EventHandler<EventArgs<TimeSpan>> ElapsedChanged;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private double _TimeScale;
        /// <summary>Determines the rate at which <see cref="Elapsed"/> changes.<para/>
        /// Default value: 1.0, which means <see cref="Elapsed"/> is incremented by 1 second per 1 real-life second.<para/>
        /// For example, if the player has an ability that slows time to 0.50x, you may wish to temporarily set <see cref="TimeScale"/> to 0.50 while the ability is active,<br/>
        /// unless you still wanted the stopwatch to track real time</summary>
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
        private Func<TimeSpan, string> _ElapsedToString;
        /// <summary>A function whose input is the <see cref="Elapsed"/> time, and returns the string value that should be displayed by this <see cref="MGStopwatch"/>.<para/>
        /// Default value: A function that returns:
        /// <code>TimeSpan.ToString(@"m\:ss\.%f")</code>
        /// For example, a value of 13566 seconds would be formatted as: "46:06.0"<para/>
        /// More info: <see href="https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-timespan-format-strings"/></summary>
        public Func<TimeSpan, string> ElapsedToString
        {
            get => _ElapsedToString;
            set
            {
                if (_ElapsedToString != value)
                {
                    _ElapsedToString = value;
                    UpdateDisplayedValue(true);
                    NPC(nameof(ElapsedToString));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _IsRunning;
        /// <summary>See also: <see cref="Start"/>, <see cref="Stop"/>, <see cref="Started"/>, <see cref="Stopped"/>, <see cref="ResetAndStart"/>, <see cref="ResetAndStop"/></summary>
        public bool IsRunning
        {
            get => _IsRunning;
            set
            {
                if (_IsRunning != value)
                {
                    _IsRunning = value;
                    NPC(nameof(IsRunning));
                    if (IsRunning)
                        Started?.Invoke(this, EventArgs.Empty);
                    else
                        Stopped?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>Invoked when this <see cref="MGStopwatch"/> is started (<see cref="IsRunning"/> set to true)</summary>
        public event EventHandler<EventArgs> Started;
        /// <summary>Invoked when this <see cref="MGStopwatch"/> is stopped (<see cref="IsRunning"/> set to false)</summary>
        public event EventHandler<EventArgs> Stopped;

        /// <summary>Start the <see cref="MGStopwatch"/> if not already started.<br/>
        /// See also: <see cref="Stop"/>, <see cref="IsRunning"/>, <see cref="ResetAndStart"/>, <see cref="ResetAndStop"/></summary>
        public void Start() => IsRunning = true;
        /// <summary>Stop the <see cref="MGStopwatch"/> if not already stopped.<br/>
        /// See also: <see cref="Start"/>, <see cref="IsRunning"/>, <see cref="ResetAndStart"/>, <see cref="ResetAndStop"/></summary>
        public void Stop() => IsRunning = false;

        /// <summary>Resets <see cref="Elapsed"/> to <see cref="TimeSpan.Zero"/>, and starts the <see cref="MGStopwatch"/>.</summary>
        public void ResetAndStart()
        {
            Stop();
            Elapsed = TimeSpan.Zero;
            Start();
        }

        /// <summary>Resets <see cref="Elapsed"/> to <see cref="TimeSpan.Zero"/>, and stops the <see cref="MGStopwatch"/> if <see cref="IsRunning"/> is true.</summary>
        public void ResetAndStop()
        {
            Stop();
            Elapsed = TimeSpan.Zero;
        }

        /// <param name="IsRunning">If false, <see cref="Elapsed"/> will not increment until <see cref="Start"/> is called.</param>
        public MGStopwatch(MGWindow Window, bool IsRunning = false)
            : base(Window, MGElementType.Stopwatch)
        {
            using (BeginInitializing())
            {
                this.Elapsed = TimeSpan.Zero;
                this.IsRunning = IsRunning;

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

                this.ElapsedToString = (TimeSpan Elapsed) => Elapsed.ToString(@"m\:ss\.%f");
                this.ValueDisplayFormat = DefaultValueDisplayFormat;
                this.TimeScale = 1.0;
            }
        }

        public override void UpdateSelf(ElementUpdateArgs UA)
        {
            base.UpdateSelf(UA);
            if (IsRunning)
            {
                Elapsed += UA.BA.FrameElapsed * TimeScale;
            }
        }
    }
}
