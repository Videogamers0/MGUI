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
    public class MGRectangle : MGElement
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _Width;
        public int Width
        {
            get => _Width;
            set
            {
                if (_Width != value)
                {
                    _Width = value;
                    LayoutChanged(this, true);
                    NPC(nameof(Width));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _Height;
        public int Height
        {
            get => _Height;
            set
            {
                if (_Height != value)
                {
                    _Height = value;
                    LayoutChanged(this, true);
                    NPC(nameof(Height));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Color _Stroke;
        /// <summary>The <see cref="Color"/> to use for the border of this <see cref="MGRectangle"/>.<para/>
        /// See also: <see cref="StrokeThickness"/></summary>
        public Color Stroke
        {
            get => _Stroke;
            set
            {
                if (_Stroke != value)
                {
                    _Stroke = value;
                    NPC(nameof(Stroke));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _StrokeThickness;
        /// <summary>The uniform thickness of each side of this <see cref="MGRectangle"/>'s border.<para/>
        /// See also: <see cref="Stroke"/></summary>
        public int StrokeThickness
        {
            get => _StrokeThickness;
            set
            {
                if (_StrokeThickness != value)
                {
                    _StrokeThickness = value;
                    NPC(nameof(StrokeThickness));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IFillBrush _Fill;
        /// <summary>The brush to use to fill the space occupied by this <see cref="MGRectangle"/>.<para/>
        /// See also: <see cref="Width"/>, <see cref="Height"/></summary>
        public IFillBrush Fill
        {
            get => _Fill;
            set
            {
                if (_Fill != value)
                {
                    _Fill = value;
                    NPC(nameof(Fill));
                }
            }
        }

        public MGRectangle(MGWindow Window, int Width, int Height, Color Stroke, int StrokeThickness, Color Fill)
            : this(Window, Width, Height, Stroke, StrokeThickness, Fill.AsFillBrush()) { }

        public MGRectangle(MGWindow Window, int Width, int Height, Color Stroke, int StrokeThickness, IFillBrush Fill)
            : base(Window, MGElementType.Rectangle)
        {
            using (BeginInitializing())
            {
                this.Width = Width;
                this.Height = Height;

                this.Stroke = Stroke;
                this.StrokeThickness = StrokeThickness;
                this.Fill = Fill;

                this.HorizontalAlignment = HorizontalAlignment.Left;
                this.VerticalAlignment = VerticalAlignment.Top;
            }
        }

        public override Thickness MeasureSelfOverride(Size AvailableSize, out Thickness SharedSize)
        {
            SharedSize = new(0);
            return new(Width, Height, 0, 0);
        }

        public override void DrawSelf(ElementDrawArgs DA, Rectangle LayoutBounds)
        {
            Rectangle ActualBounds = ApplyAlignment(LayoutBounds, HorizontalAlignment.Center, VerticalAlignment.Center, new Size(Width, Height));
            if (ActualBounds.Width <= 0 || ActualBounds.Height <= 0)
                return;

            Fill?.Draw(DA, this, ActualBounds);
            if (StrokeThickness > 0)
                DA.DT.StrokeRectangle(DA.Offset.ToVector2(), ActualBounds, Stroke * DA.Opacity, new(StrokeThickness));
        }
    }
}
