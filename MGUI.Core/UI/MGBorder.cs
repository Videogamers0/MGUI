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
using System.Diagnostics;

namespace MGUI.Core.UI
{
    public class MGBorder : MGSingleContentHost
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IBorderBrush _BorderBrush;
        public IBorderBrush BorderBrush
        {
            get => _BorderBrush;
            set
            {
                if (_BorderBrush != value)
                {
                    IBorderBrush Previous = BorderBrush;
                    _BorderBrush = value;
                    NPC(nameof(BorderBrush));
                    OnBorderBrushChanged?.Invoke(this, new(Previous, BorderBrush));
                }
            }
        }

        public event EventHandler<EventArgs<IBorderBrush>> OnBorderBrushChanged;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Thickness _BorderThickness;
        public Thickness BorderThickness
        {
            get => _BorderThickness;
            set
            {
                if (!_BorderThickness.Equals(value))
                {
                    Thickness Previous = BorderThickness;
                    _BorderThickness = value;
                    LayoutChanged(this, true);
                    NPC(nameof(BorderThickness));
                    OnBorderThicknessChanged?.Invoke(this, new(Previous, BorderThickness));
                }
            }
        }

        public event EventHandler<EventArgs<Thickness>> OnBorderThicknessChanged;

        public MGBorder(MGWindow Window)
            : this(Window, new(1), MGUniformBorderBrush.Black) { }

        public MGBorder(MGWindow Window, Thickness BorderThickness, IFillBrush BorderBrush)
            : this(Window, BorderThickness, BorderBrush == null ? null : new MGUniformBorderBrush(BorderBrush)) { }

        public MGBorder(MGWindow Window, Thickness BorderThickness, IBorderBrush BorderBrush)
            : base(Window, MGElementType.Border)
        {
            using (BeginInitializing())
            {
                this.BorderBrush = BorderBrush;
                this.BorderThickness = BorderThickness;
            }
        }

        public override Thickness MeasureSelfOverride(Size AvailableSize, out Thickness SharedSize)
        {
            SharedSize = new(0);
            return BorderThickness;
        }

        public override MGBorder GetBorder() => this;

        protected override bool CanConsumeSpaceInSingleDimension => true;

        public override void DrawSelf(ElementDrawArgs DA, Rectangle LayoutBounds)
        {
            BorderBrush?.Draw(DA, this, LayoutBounds, BorderThickness);
            DrawSelfBaseImplementation(DA, LayoutBounds);
        }
    }
}
