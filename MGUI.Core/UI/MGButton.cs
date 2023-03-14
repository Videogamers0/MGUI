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

        /// <param name="HandleLeftClick">An <see cref="Action"/> to invoke when this <see cref="MGButton"/> is left-clicked.<para/>
        /// This handler will only be invoked if <see cref="HandledByEventArgs{THandlerType}.IsHandled"/> is false.<br/>This handler will also set <see cref="HandledByEventArgs{THandlerType}.IsHandled"/> to true after being invoked.<para/>
        /// If you want to avoid this default behavior, manually subscribe to <see cref="OnLeftClicked"/><br/>or call <see cref="AddCommandHandler(Action{MGButton, BaseMouseReleasedEventArgs}, bool)"/>.</param>
        public MGButton(MGWindow Window, Action<MGButton> HandleLeftClick = null)
            : this(Window, new(1), MGUniformBorderBrush.Black, HandleLeftClick) { }

        /// <param name="HandleLeftClick">An <see cref="Action"/> to invoke when this <see cref="MGButton"/> is left-clicked.<para/>
        /// This handler will only be invoked if <see cref="HandledByEventArgs{THandlerType}.IsHandled"/> is false.<br/>This handler will also set <see cref="HandledByEventArgs{THandlerType}.IsHandled"/> to true after being invoked.<para/>
        /// If you want to avoid this default behavior, manually subscribe to <see cref="OnLeftClicked"/><br/>or call <see cref="AddCommandHandler(Action{MGButton, BaseMouseReleasedEventArgs}, bool)"/>.</param>
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
                        OnLeftClicked?.Invoke(this, e);
                    else if (e.IsRMB)
                        OnRightClicked?.Invoke(this, e);
                };

                if (HandleLeftClick != null)
                {
                    AddCommandHandler((btn, e) => { HandleLeftClick(btn); }, true);
                }
            }
        }

        /// <summary>Helper method to subscribe to <see cref="OnLeftClicked"/></summary>
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
