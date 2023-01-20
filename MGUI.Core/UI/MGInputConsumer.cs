using MGUI.Core.UI.Containers;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI
{
    /// <summary>A wrapper element that ensures mouse-related inputs are handled either by this element or its children,<br/>
    /// rather than allowing unhandled inputs to continue bubbling up the visual tree.</summary>
    public class MGInputConsumer : MGSingleContentHost
    {
        public bool HandlesMousePresses { get; set; }
        public bool HandlesMouseReleases { get; set; }
        public bool HandlesMouseDrags { get; set; }
        public bool HandlesMouseScroll { get; set; }

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
