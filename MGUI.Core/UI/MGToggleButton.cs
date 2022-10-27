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
using MGUI.Core.UI.Brushes.Fill_Brushes;

namespace MGUI.Core.UI
{
    public class MGToggleButton : MGSingleContentHost
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

        /// <summary>The background brush to use for this <see cref="MGToggleButton"/> when <see cref="IsChecked"/> is true.</summary>
        public IFillBrush CheckedBackgroundBrush
        {
            get => BackgroundBrush.SelectedValue;
            set => BackgroundBrush.SelectedValue = value;
        }

        private bool _UseAlternateTextForegroundWhenChecked;
        /// <summary>If true, <see cref="MGElement.DefaultTextForeground"/> will automatically be set to <see cref="CheckedTextForeground"/> when <see cref="IsChecked"/>=true.</summary>
        public bool UseAlternateTextForegroundWhenChecked
        {
            get => _UseAlternateTextForegroundWhenChecked;
            set
            {
                if (_UseAlternateTextForegroundWhenChecked != value)
                {
                    _UseAlternateTextForegroundWhenChecked = value;
                    NPC(nameof(UseAlternateTextForegroundWhenChecked));
                    DefaultTextForeground = IsChecked ? CheckedTextForeground : null;
                }
            }
        }

        private Color? _CheckedTextForeground;
        /// <summary>This value is only relevant if <see cref="UseAlternateTextForegroundWhenChecked"/>=true.<para/>
        /// Default value: <see cref="Color.LightGray"/><para/>
        /// See also: <see cref="UseAlternateTextForegroundWhenChecked"/></summary>
        public Color? CheckedTextForeground
        {
            get => _CheckedTextForeground;
            set
            {
                if (_CheckedTextForeground != value)
                {
                    _CheckedTextForeground = value;
                    NPC(nameof(CheckedTextForeground));

                    if (UseAlternateTextForegroundWhenChecked && IsChecked)
                    {
                        DefaultTextForeground = CheckedTextForeground;
                    }
                }
            }
        }

        private bool _IsChecked;
        public bool IsChecked
        {
            get => _IsChecked;
            set
            {
                if (_IsChecked != value)
                {
                    bool Previous = IsChecked;
                    _IsChecked = value;
                    NPC(nameof(IsChecked));
                    OnCheckStateChanged?.Invoke(this, new(Previous, IsChecked));

                    IsSelected = IsChecked;
                    if (IsChecked)
                        OnChecked?.Invoke(this, EventArgs.Empty);
                    else
                        OnUnchecked?.Invoke(this, EventArgs.Empty);

                    if (UseAlternateTextForegroundWhenChecked)
                    {
                        DefaultTextForeground = IsChecked ? CheckedTextForeground : null;
                    }
                }
            }
        }

        /// <summary>Note: This event is invoked before <see cref="OnChecked"/> / <see cref="OnUnchecked"/></summary>
        public event EventHandler<EventArgs<bool>> OnCheckStateChanged;
        public event EventHandler<EventArgs> OnChecked;
        public event EventHandler<EventArgs> OnUnchecked;

        public MGToggleButton(MGWindow Window, bool IsChecked = false)
            : this(Window, new(1), MGUniformBorderBrush.Black, IsChecked) { }

        public MGToggleButton(MGWindow Window, Thickness BorderThickness, IBorderBrush BorderBrush, bool IsChecked)
            : base(Window, MGElementType.ToggleButton)
        {
            using (BeginInitializing())
            {
                this.MinWidth = 16;
                this.MinHeight = 16;

                this.BorderElement = new(Window, BorderThickness, BorderBrush);
                this.BorderComponent = MGComponentBase.Create(BorderElement);
                AddComponent(BorderComponent);

                this.UseAlternateTextForegroundWhenChecked = true;
                this.CheckedTextForeground = Color.LightGray;

                this.HorizontalContentAlignment = HorizontalAlignment.Center;
                this.VerticalContentAlignment = VerticalAlignment.Center;
                this.Padding = new(4, 2, 4, 2);

                MouseHandler.LMBReleasedInside += (sender, e) =>
                {
                    this.IsChecked = !this.IsChecked;
                    e.SetHandled(this, false);
                };

                this.IsChecked = IsChecked;
            }
        }

        public override MGBorder GetBorder() => BorderElement;
    }
}