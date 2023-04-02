using Microsoft.Xna.Framework;
using MGUI.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using MonoGame.Extended;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using System.Diagnostics;
using MGUI.Core.UI.Data_Binding.Converters;

namespace MGUI.Core.UI
{
    [TypeConverter(typeof(StringToToolTipTypeConverter))]
    public class MGToolTip : MGWindow
    {
        public MGElement Host { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _ShowOnDisabled;
        /// <summary>True if this <see cref="MGToolTip"/> can be shown on an <see cref="MGElement"/> where <see cref="MGElement.IsEnabled"/>==false.<para/>
        /// Default value: false</summary>
        public bool ShowOnDisabled
        {
            get => _ShowOnDisabled;
            set
            {
                if (_ShowOnDisabled != value)
                {
                    _ShowOnDisabled = value;
                    NPC(nameof(ShowOnDisabled));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Point _DrawOffset;
        /// <summary>An offset from the current mouse cursor position to draw this <see cref="MGToolTip"/> at.<para/>
        /// Default value: <see cref="MGTheme.ToolTipOffset"/></summary>
        public Point DrawOffset
        {
            get => _DrawOffset;
            set
            {
                if (_DrawOffset != value)
                {
                    _DrawOffset = value;
                    NPC(nameof(DrawOffset));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private TimeSpan? _ShowDelayOverride;
        /// <summary>The amount of time that the mouse must hover a particular <see cref="MGElement"/> before its <see cref="MGElement.ToolTip"/> can be shown.<para/>
        /// If not null, this value takes precedence over <see cref="MGDesktop.ToolTipShowDelay"/>.<para/>
        /// See also: <see cref="ActualShowDelay"/>, <see cref="MGDesktop.ToolTipShowDelay"/></summary>
        public TimeSpan? ShowDelayOverride
        {
            get => _ShowDelayOverride;
            set
            {
                if (_ShowDelayOverride != value)
                {
                    _ShowDelayOverride = value;
                    NPC(nameof(ShowDelayOverride));
                    NPC(nameof(ActualShowDelay));
                }
            }
        }

        public TimeSpan ActualShowDelay => ShowDelayOverride ?? GetDesktop().ToolTipShowDelay;

        public MGToolTip(MGWindow Window, MGElement Host, int Width, int Height, MGTheme Theme = null)
            : base(Window.Desktop, Theme ?? Window.Theme, Window, MGElementType.ToolTip, 0, 0, Width, Height)
        {
            using (BeginInitializing())
            {
                MGTheme ActualTheme = (this.Theme ?? Window.GetTheme());

                this.Host = Host;
                this.BorderBrush = Color.Black.AsFillBrush().AsUniformBorderBrush();
                this.BorderThickness = new(2);
                this.DrawOffset = ActualTheme.ToolTipOffset;
                this.ShowOnDisabled = false;
                this.ShowDelayOverride = null;
                this.DefaultTextForeground = ActualTheme.ToolTipTextForeground.GetCopy();
                this.Padding = new(6,3);
                this.MinWidth = 10;
                this.MinHeight = 10;
                this.IsUserResizable = false;
                this.IsTitleBarVisible = false;

#if NEVER
                Host.OnLayoutBoundsChanged += (sender, e) =>
                {
                    //this.Left = (int)e.NewValue.BottomLeft.X;
                    //this.Top = (int)e.NewValue.BottomLeft.Y;
                };

                Window.ToolTipOpened += (sender, e) =>
                {
                    if (e == this)
                    {
                        //this.Left = (int)Host.LayoutBounds.Center.X;
                        //this.Top = (int)Host.LayoutBounds.Center.Y;
                    }
                };
#endif
            }
        }

        public void DrawAtDefaultPosition(ElementDrawArgs DA) => DrawAtMousePosition(DA, DrawOffset.X, DrawOffset.Y);
        public void DrawAtMousePosition(ElementDrawArgs DA, int XOffset = 5, int YOffset = 5)
        {
            Point CurrentMousePosition = InputTracker.Mouse.CurrentPosition;
            Draw(DA with { Offset = DA.Offset + CurrentMousePosition + new Point(XOffset, YOffset) });
        }
    }
}
