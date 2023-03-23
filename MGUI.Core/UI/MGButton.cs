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
        /// See also: <see cref="Command"/>, <see cref="MGWindow.NamedActions"/>, <see cref="MGWindow.AddNamedAction(string, Action{MGElement})"/></summary>
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
                    if (!e.IsHandled && this.Command != null)
                    {
                        bool Handled = this.Command(this);
                        if (Handled)
                            e.SetHandledBy(this, false);
                    }

                    if (!e.IsHandled && !string.IsNullOrEmpty(this.CommandName) && 
                        ParentWindow.NamedActions.TryGetValue(this.CommandName, out Action<MGElement> Command))
                    {
                        e.SetHandledBy(this, true);
                        Command(this);
                    }
                };
            }
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
