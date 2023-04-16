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
using MGUI.Shared.Input.Mouse;
using MGUI.Shared.Input;
using System.Diagnostics;

namespace MGUI.Core.UI
{
    public class MGButton : MGSingleContentHost
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

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _CommandName;
        /// <summary>The name of the command to execute when this <see cref="MGButton"/> is left-clicked, or null if no named command should be executed when left-clicked.<para/>
        /// If <see cref="Command"/> is also specified, <see cref="Command"/> will take priority and be executed first.<br/>
        /// (Which may result in the <see cref="CommandName"/> logic not being executed if <see cref="Command"/> returns true)<para/>
        /// See also:<br/><see cref="Command"/><br/><see cref="MGElement.GetResources"/><br/><see cref="MGResources.Commands"/><br/><see cref="MGResources.AddCommand(string, Action{MGElement})"/></summary>
        public string CommandName
        {
            get => _CommandName;
            set
            {
                if (_CommandName != value)
                {
                    _CommandName = value;
                    NPC(nameof(CommandName));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Func<MGButton, bool> _Command;
        /// <summary>The function to execute when this <see cref="MGButton"/> is left-clicked, or null if no explicit action should be executed when left-clicked.<para/>
        /// The parameter of this <see cref="Func{T, TResult}"/> is this <see cref="MGButton"/> instance.<br/>
        /// The return value should be true if the function handled the click, or false to let the click propagate to other mouse handlers.<para/>
        /// If both <see cref="CommandName"/> and <see cref="Command"/> are specified, this <see cref="Command"/> takes priority and is executed first.<br/>
        /// (Which may result in the <see cref="CommandName"/> logic not being executed if this <see cref="Command"/> returns true)</summary>
        public Func<MGButton, bool> Command
        {
            get => _Command;
            set
            {
                if (_Command != value)
                {
                    _Command = value;
                    NPC(nameof(Command));
                }
            }
        }

        private bool HasCommand => !string.IsNullOrEmpty(CommandName) || Command != null;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _IsRepeatButton;
        /// <summary>If true, this <see cref="MGButton"/>'s <see cref="CommandName"/> and/or <see cref="Command"/> will be repeatedly fired while the mouse is held pressed overtop it.<para/>
        /// See also: <see cref="InitialRepeatInterval"/>, <see cref="RepeatInterval"/></summary>
        public bool IsRepeatButton
        {
            get => _IsRepeatButton;
            set
            {
                if (_IsRepeatButton != value)
                {
                    _IsRepeatButton = value;
                    NPC(nameof(IsRepeatButton));
                }
            }
        }

        /// <summary>0.5s</summary>
        public static readonly TimeSpan DefaultInitialRepeatInterval = TimeSpan.FromSeconds(0.5);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private TimeSpan _InitialRepeatInterval = DefaultInitialRepeatInterval;
        /// <summary>Only relevant if <see cref="IsRepeatButton"/> is true.<para/>
        /// The initial delay before this <see cref="MGButton"/>'s <see cref="CommandName"/> and/or <see cref="Command"/> will be repeatedly fired while the mouse is held pressed overtop it.<br/>
        /// Default value: <see cref="DefaultInitialRepeatInterval"/><para/>
        /// See also: <see cref="IsRepeatButton"/>, <see cref="RepeatInterval"/></summary>
        public TimeSpan InitialRepeatInterval
        {
            get => _InitialRepeatInterval;
            set
            {
                if (_InitialRepeatInterval != value)
                {
                    _InitialRepeatInterval = value;
                    NPC(nameof(InitialRepeatInterval));
                }
            }
        }

        /// <summary>10 repetitions per second</summary>
        public static readonly TimeSpan DefaultRepeatInterval = TimeSpan.FromSeconds(1.0 / 10);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private TimeSpan _RepeatInterval = DefaultRepeatInterval;
        /// <summary>Only relevant if <see cref="IsRepeatButton"/> is true.<para/>
        /// How often to repeatedly fire this <see cref="MGButton"/>'s <see cref="CommandName"/> and/or <see cref="Command"/> while the mouse is held pressed overtop it.<br/>
        /// Default value: <see cref="DefaultRepeatInterval"/><para/>
        /// See also: <see cref="IsRepeatButton"/>, <see cref="InitialRepeatInterval"/></summary>
        public TimeSpan RepeatInterval
        {
            get => _RepeatInterval;
            set
            {
                if (_RepeatInterval != value)
                {
                    _RepeatInterval = value;
                    NPC(nameof(RepeatInterval));
                }
            }
        }

        /// <param name="HandleLeftClick">An <see cref="Action"/> to invoke when this <see cref="MGButton"/> is left-clicked.<para/>
        /// This handler will only be invoked if <see cref="HandledByEventArgs{THandlerType}.IsHandled"/> is false.<br/>
        /// This handler will also set <see cref="HandledByEventArgs{THandlerType}.IsHandled"/> to true after being invoked.<para/>
        /// If you want to avoid this default behavior, manually subscribe to <see cref="OnLeftClicked"/>,<br/>
        /// call <see cref="AddCommandHandler(Action{MGButton, BaseMouseReleasedEventArgs}, bool)"/>,<br/>
        /// or set <see cref="Command"/> instead.</param>
        public MGButton(MGWindow Window, Action<MGButton> HandleLeftClick = null)
            : this(Window, new(1), MGUniformBorderBrush.Black, HandleLeftClick) { }

        /// <param name="HandleLeftClick">An <see cref="Action"/> to invoke when this <see cref="MGButton"/> is left-clicked.<para/>
        /// This handler will only be invoked if <see cref="HandledByEventArgs{THandlerType}.IsHandled"/> is false.<br/>
        /// This handler will also set <see cref="HandledByEventArgs{THandlerType}.IsHandled"/> to true after being invoked.<para/>
        /// If you want to avoid this default behavior, manually subscribe to <see cref="OnLeftClicked"/>,<br/>
        /// call <see cref="AddCommandHandler(Action{MGButton, BaseMouseReleasedEventArgs}, bool)"/>,<br/>
        /// or set <see cref="Command"/> instead.</param>
        public MGButton(MGWindow Window, Thickness BorderThickness, IBorderBrush BorderBrush, Action<MGButton> HandleLeftClick = null)
            : base(Window, MGElementType.Button)
        {
            using (BeginInitializing())
            {
                this.MinWidth = 16;
                this.MinHeight = 16;

                this.BorderElement = new(Window, BorderThickness, BorderBrush);
                this.BorderComponent = MGComponentBase.Create(BorderElement);
                AddComponent(BorderComponent);
                BorderElement.OnBorderBrushChanged += (sender, e) => { NPC(nameof(BorderBrush)); };
                BorderElement.OnBorderThicknessChanged += (sender, e) => { NPC(nameof(BorderThickness)); };

                this.HorizontalContentAlignment = HorizontalAlignment.Center;
                this.VerticalContentAlignment = VerticalAlignment.Center;
                this.Padding = new(4,2,4,2);

                MouseHandler.PressedInside += (sender, e) =>
                { 
                    PressedArgs = e;
                    if (IsRepeatButton)
                        e.SetHandledBy(this, false);
                };
                MouseHandler.ReleasedInside += (sender, e) =>
                {
                    if (e.IsLMB)
                    {
                        OnLeftClicked?.Invoke(this, e);
                        //e.SetHandledBy(this, false); // Avoid auto-handling left-clicks because some controls like MGComboBox react to inputs after the child button received it
                    }
                    else if (e.IsRMB)
                    {
                        OnRightClicked?.Invoke(this, e);
                        //e.SetHandledBy(this, false); // Avoid auto-handling right-clicks because it may prevent MGElement from opening ContextMenus
                    }
                };

                if (HandleLeftClick != null)
                {
                    this.Command = (btn) => 
                    {
                        HandleLeftClick(btn);
                        return true;
                    };
                }

                OnLeftClicked += (sender, e) =>
                {
                    //  For repeat buttons, I avoid invoking the commands on mouse released because it feels a bit strange.
                    //  EX: RepeatInterval=1s. Press at t=0, Repeat at t=1, Release at t=1.2 - this would fire the commands twice, once at t=1 and again at t=1.2,
                    //  which just seems weird for the last execution to have an unpredictable interval compared to every prior execution
                    if (!IsRepeatButton || !RepeatedAt.HasValue)
                        TryInvokeCommands(e, false);
                };
            }
        }

        private bool TryInvokeCommands(HandledByEventArgs<IMouseHandlerHost> args, bool IsRepeating)
        {
            bool CanInvoke() => !args.IsHandled || (IsRepeating && args.HandledBy == this);

            bool Invoked = false;

            if (CanInvoke() && this.Command != null)
            {
                bool Handled = this.Command(this);
                if (Handled)
                    args.SetHandledBy(this, false);
                Invoked = true;
            }

            if (CanInvoke() && GetResources().TryGetCommand(CommandName, out Action<MGElement> Command))
            {
                args.SetHandledBy(this, false);
                Command(this);
                Invoked = true;
            }

            return Invoked;
        }

        private BaseMousePressedEventArgs PressedArgs { get; set; }
        /// <summary>Only relevant if <see cref="IsRepeatButton"/> is true.<para/>
        /// The last time that this <see cref="MGButton"/> was pressed.</summary>
        private DateTime? PressedAt { get; set; }
        /// <summary>Only relevant if <see cref="IsRepeatButton"/> is true.<para/>
        /// The last time that this <see cref="MGButton"/>'s <see cref="CommandName"/> and/or <see cref="Command"/> was repeated.</summary>
        private DateTime? RepeatedAt { get; set; }

        private bool IsRepeatPending(DateTime Now) => 
            (!RepeatedAt.HasValue && Now.Subtract(PressedAt.Value) >= InitialRepeatInterval) ||
            (RepeatedAt.HasValue && Now.Subtract(RepeatedAt.Value) >= RepeatInterval);

        public override void UpdateSelf(ElementUpdateArgs UA)
        {
            if (IsRepeatButton && VisualState.IsPressed)
            {
                DateTime Now = DateTime.Now;
                if (!PressedAt.HasValue)
                    PressedAt = Now;
                else if (HasCommand && IsRepeatPending(Now) && PressedArgs != null)
                {
                    RepeatedAt = Now;
                    _ = TryInvokeCommands(PressedArgs, true);
                }
            }
            else
            {
                PressedAt = null;
                RepeatedAt = null;
            }

            base.UpdateSelf(UA);
        }

        /// <summary>Helper method to subscribe to <see cref="OnLeftClicked"/>.<para/>
        /// Recommended to set <see cref="Command"/> or <see cref="CommandName"/> instead, unless you require multiple handlers for the left-click event.</summary>
        /// <param name="Command">The <see cref="Action"/> to invoke when this <see cref="MGButton"/> is left-clicked (more specifically, occurs when the left mouse button is released overtop of this <see cref="MGButton"/>)</param>
        /// <param name="SetsHandledToTrue">If true, this <paramref name="Command"/> will set <see cref="HandledByEventArgs{THandlerType}.IsHandled"/> to true if it executes.</param>
        public void AddCommandHandler(Action<MGButton, BaseMouseReleasedEventArgs> Command, bool SetsHandledToTrue = true)
        {
            if (Command != null)
            {
                OnLeftClicked += (sender, e) =>
                {
                    if (SetsHandledToTrue)
                        e.SetHandledBy(this, false);
                    Command(this, e);
                };
            }
        }

        /// <summary>Invoked when the left mouse button is released overtop of this <see cref="MGButton"/>.<para/>
        /// Consider checking <see cref="HandledByEventArgs{THandlerType}.IsHandled"/> at the start of this <see cref="EventHandler"/><br/>
        /// or subscribing via <see cref="AddCommandHandler(Action{MGButton, BaseMouseReleasedEventArgs}, bool)"/>.</summary>
        public event EventHandler<BaseMouseReleasedEventArgs> OnLeftClicked;
        /// <summary>Invoked when the right mouse button is released overtop of this <see cref="MGButton"/>.<para/>
        /// Consider checking <see cref="HandledByEventArgs{THandlerType}.IsHandled"/> at the start of this <see cref="EventHandler"/><br/>
        /// or subscribing via <see cref="AddCommandHandler(Action{MGButton, BaseMouseReleasedEventArgs}, bool)"/>.</summary>
        public event EventHandler<BaseMouseReleasedEventArgs> OnRightClicked;
    }
}
