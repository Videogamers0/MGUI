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
using System.Diagnostics;

namespace MGUI.Core.UI
{
    /// <summary>A lightweight control with no graphics whose primary purpose is to consume a defined amount of screen space</summary>
    public class MGSpacer : MGElement
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

        public MGSpacer(MGWindow Window, int Width, int Height)
            : base(Window, MGElementType.Spacer)
        {
            using (BeginInitializing())
            {
                this.Width = Width;
                this.Height = Height;
            }
        }

        protected override bool CanConsumeSpaceInSingleDimension => true;

        public override Thickness MeasureSelfOverride(Size AvailableSize, out Thickness SharedSize)
        {
            SharedSize = new(0);
            return new(Width, Height, 0, 0);
        }

        protected override void UpdateContents(ElementUpdateArgs UA) { }
        public override void UpdateSelf(ElementUpdateArgs UA) { }
        protected override void UpdateContentLayout(Rectangle Bounds) { }

        public override void DrawBackground(ElementDrawArgs DA, Rectangle LayoutBounds) { }
        protected override void DrawContents(ElementDrawArgs DA) { }
    }
}
