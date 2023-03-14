using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MGUI.Shared.Helpers;
using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.Containers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MGUI.Core.UI
{
    /// <summary>A simple wrapper class that allows hiding this <see cref="MGElement"/>'s <see cref="MGSingleContentHost.Content"/> until user clicks it to reveal the contents.</summary>
    public class MGSpoiler : MGSingleContentHost
    {
        #region Button
        /// <summary>Provides direct access to the button component that is drawn overtop of this spoiler's content when  <see cref="IsRevealed"/> is false.</summary>
        public MGComponent<MGButton> ButtonComponent { get; }
        private MGButton ButtonElement { get; }

        public IBorderBrush UnspoiledBorderBrush
        {
            get => ButtonElement.BorderBrush;
            set
            {
                if (ButtonElement.BorderBrush != value)
                {
                    ButtonElement.BorderBrush = value;
                    NPC(nameof(UnspoiledBorderBrush));
                }
            }
        }

        public Thickness UnspoiledBorderThickness
        {
            get => ButtonElement.BorderThickness;
            set
            {
                if (!ButtonElement.BorderThickness.Equals(value))
                {
                    ButtonElement.BorderThickness = value;
                    NPC(nameof(UnspoiledBorderThickness));
                }
            }
        }

        /// <summary>A background to use when this <see cref="MGSpoiler"/> is not revealed.</summary>
        public VisualStateFillBrush UnspoiledBackgroundBrush
        {
            get => ButtonElement.BackgroundBrush;
            set
            {
                if (ButtonElement.BackgroundBrush != value)
                {
                    ButtonElement.BackgroundBrush = value;
                    NPC(nameof(UnspoiledBackgroundBrush));
                }
            }
        }
        #endregion Button

        #region Text
        /// <summary>The content of the <see cref="ButtonElement"/>. This <see cref="MGTextBlock"/> is used to render the <see cref="UnspoiledText"/></summary>
        private MGTextBlock TextElement { get; }

        public const string DefaultUnspoiledText = "[color=White][b]Spoiler:[/b] Click to reveal[/color]";

        /// <summary>The text to display overtop of this <see cref="MGSpoiler"/> if it is not revealed. Can be null.<br/>
        /// If not null, this text will be displayed in the center of this <see cref="MGSpoiler"/> (HorizontalAlignment=<see cref="HorizontalAlignment.Center"/>, VerticalAlignment=<see cref="VerticalAlignment.Center"/>)<para/>
        /// Default value: <see cref="DefaultUnspoiledText"/><para/>
        /// This value supports some basic text markdown, such as "[b]" for bold text, "[fg=Red]" to set the text foreground color to a given value, "[opacity=0.5]" etc.</summary>
        public string UnspoiledText
        {
            get => TextElement.Text;
            set
            {
                if (TextElement.Text != value)
                {
                    TextElement.Text = value;
                    NPC(nameof(UnspoiledText));
                }
            }
        }

        public HorizontalAlignment UnspoiledTextAlignment
        {
            get => TextElement.TextAlignment;
            set
            {
                if (TextElement.TextAlignment != value)
                {
                    TextElement.TextAlignment = value;
                    NPC(nameof(UnspoiledTextAlignment));
                }
            }
        }
        #endregion Text

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _IsRevealed;
        /// <summary>If false, the <see cref="MGSingleContentHost.Content"/> will not be visible.<br/>
        /// Instead, <see cref="UnspoiledBorderBrush"/>, <see cref="UnspoiledBorderThickness"/>, <see cref="UnspoiledBackgroundBrush"/>, and <see cref="UnspoiledText"/> will be visible.</summary>
        public bool IsRevealed
        {
            get => _IsRevealed;
            set
            {
                if (_IsRevealed != value)
                {
                    if (OnIsRevealedChanging != null)
                    {
                        CancelEventArgs<bool> Args = new(value);
                        OnIsRevealedChanging?.Invoke(this, Args);
                        if (Args.Cancel)
                            return;
                    }

                    _IsRevealed = value;

                    this.ButtonElement.Visibility = IsRevealed ? Visibility.Collapsed : Visibility.Visible;
                    UpdateContentVisibility();

                    NPC(nameof(IsRevealed));
                    if (IsRevealed)
                        OnRevealed?.Invoke(this, EventArgs.Empty);
                    else
                        OnHidden?.Invoke(this, EventArgs.Empty);
                    OnIsRevealedChanged?.Invoke(this, IsRevealed);
                }
            }
        }

        /// <summary>Invoked just before <see cref="IsRevealed"/>'s value changes. Allows cancellation.<para/>
        /// See also: <see cref="OnRevealed"/>, <see cref="OnHidden"/>, <see cref="OnIsRevealedChanged"/></summary>
        public event EventHandler<CancelEventArgs<bool>> OnIsRevealedChanging;
        /// <summary>Invoked immediately after <see cref="IsRevealed"/> is set to true.<para/>
        /// See also: <see cref="OnIsRevealedChanging"/>, <see cref="OnHidden"/>, <see cref="OnIsRevealedChanged"/></summary>
        public event EventHandler<EventArgs> OnRevealed;
        /// <summary>Invoked immediately after <see cref="IsRevealed"/> is set to false.<para/>
        /// See also: <see cref="OnIsRevealedChanging"/>, <see cref="OnRevealed"/>, <see cref="OnIsRevealedChanged"/></summary>
        public event EventHandler<EventArgs> OnHidden;

        public event EventHandler<bool> OnIsRevealedChanged;

        private void UpdateContentVisibility()
        {
            if (HasContent)
            {
                Content.Visibility = IsRevealed ? Visibility.Visible : Visibility.Hidden;
            }
        }

        public MGSpoiler(MGWindow Window)
            : base(Window, MGElementType.Spoiler)
        {
            using (BeginInitializing())
            {
                this.ButtonElement = new(Window, new(1), MGUniformBorderBrush.Black, x => IsRevealed = true);
                this.TextElement = new(Window, DefaultUnspoiledText, Color.White);
                TextElement.ManagedParent = this;
                this.ButtonElement.SetContent(TextElement);
                this.ButtonComponent = new(ButtonElement, ComponentUpdatePriority.BeforeContents, ComponentDrawPriority.AfterContents,
                    true, true, true, true, false, false, false,
                    (AvailableBounds, ComponentSize) => AvailableBounds);
                AddComponent(ButtonComponent);

                this.UnspoiledTextAlignment = HorizontalAlignment.Center;
                this.UnspoiledBackgroundBrush = GetTheme().SpoilerUnspoiledBackground.GetValue(true);
                this.IsRevealed = false;

                OnContentAdded += (sender, e) => { UpdateContentVisibility(); };
            }
        }
    }
}
