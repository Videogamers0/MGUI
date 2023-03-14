using Microsoft.Xna.Framework;
using MGUI.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoGame.Extended;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using System.Diagnostics;

namespace MGUI.Core.UI
{
    public class MGSeparator : MGElement
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Orientation _Orientation;
        public Orientation Orientation
        {
            get => _Orientation;
            set
            {
                if (_Orientation != value)
                {
                    _Orientation = value;
                    Margin = AutoMargin;
                    LayoutChanged(this, true);
                    NPC(nameof(Orientation));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _Size;
        public int Size
        {
            get => _Size;
            set
            {
                if (_Size != value)
                {
                    _Size = value;
                    LayoutChanged(this, true);
                    NPC(nameof(Size));
                }
            }
        }

        private const int DefaultMarginAmount = 3;
        private Thickness AutoMargin => Orientation switch
        {
            Orientation.Horizontal => new(0, DefaultMarginAmount, 0, DefaultMarginAmount),
            Orientation.Vertical => new(DefaultMarginAmount, 0, DefaultMarginAmount, 0),
            _ => throw new NotImplementedException($"Unrecognized {nameof(Orientation)}: {Orientation}")
        };

        /// <param name="Size">For a vertical separator, this represents the width. For a horizontal separator, this represents the height.</param>
        public MGSeparator(MGWindow Window, Orientation Orientation, int Size = 2)
            : base(Window, MGElementType.Separator)
        {
            using (BeginInitializing())
            {
                this.Orientation = Orientation;
                this.Margin = AutoMargin;
                this.Size = Size;
            }
        }

        public override Thickness MeasureSelfOverride(Size AvailableSize, out Thickness SharedSize)
        {
            SharedSize = new(0);

            if (Orientation == Orientation.Horizontal)
                return new(1, Size, 0, 0);
            else if (Orientation == Orientation.Vertical)
                return new(Size, 1, 0, 0);
            else
                throw new NotImplementedException($"Unrecognized {nameof(Orientation)}: {Orientation}");
        }
    }
}
