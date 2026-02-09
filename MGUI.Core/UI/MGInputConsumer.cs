using MGUI.Core.UI.Containers;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI
{
    /// <summary>A wrapper element that ensures mouse-related inputs are handled either by this element or its children,<br/>
    /// rather than allowing unhandled inputs to continue bubbling up the visual tree.</summary>
    public class MGInputConsumer : MGSingleContentHost
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _HandlesMousePresses;
        public bool HandlesMousePresses
        {
            get => _HandlesMousePresses;
            set
            {
                if (_HandlesMousePresses != value)
                {
                    _HandlesMousePresses = value;
                    NPC(nameof(HandlesMousePresses));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _HandlesMouseReleases;
        public bool HandlesMouseReleases
        {
            get => _HandlesMouseReleases;
            set
            {
                if (_HandlesMouseReleases != value)
                {
                    _HandlesMouseReleases = value;
                    NPC(nameof(HandlesMouseReleases));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _HandlesMouseDrags;
        public bool HandlesMouseDrags
        {
            get => _HandlesMouseDrags;
            set
            {
                if (_HandlesMouseDrags != value)
                {
                    _HandlesMouseDrags = value;
                    NPC(nameof(HandlesMouseDrags));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _HandlesMouseScroll;
        public bool HandlesMouseScroll
        {
            get => _HandlesMouseScroll;
            set
            {
                if (_HandlesMouseScroll != value)
                {
                    _HandlesMouseScroll = value;
                    NPC(nameof(HandlesMouseScroll));
                }
            }
        }


        public MGInputConsumer(MGWindow Window, bool HandlesMousePresses = true, bool HandlesMouseReleases = true, bool HandlesMouseDrags = true, bool HandlesMouseScroll = true)
            : base(Window, MGElementType.InputConsumer)
        {
            using (BeginInitializing())
            {
                MouseHandler.PressedInside += (sender, e) =>
                {
                    if (HandlesMousePresses)
                        e.SetHandledBy(this);
                };

                MouseHandler.ReleasedInside += (sender, e) =>
                {
                    if (HandlesMouseReleases)
                        e.SetHandledBy(this);
                };

                MouseHandler.DragStart += (sender, e) =>
                {
                    if (HandlesMouseDrags)
                        e.SetHandledBy(this);
                };

                MouseHandler.Scrolled += (sender, e) =>
                {
                    if (HandlesMouseScroll)
                        e.SetHandledBy(this);
                };
            }
        }
    }
}
