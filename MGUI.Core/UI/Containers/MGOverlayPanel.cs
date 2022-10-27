using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MGUI.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI.Containers
{
    /// <summary>A simple layout container that renders all its child elements overtop of each other, in the order they were added to this container.</summary>
    public class MGOverlayPanel : MGMultiContentHost
    {
        private readonly Dictionary<MGElement, Thickness> ChildOffsets = new();

        /// <returns>True if the given <paramref name="Item"/> was successfully added.<br/>
        /// False otherwise, such as if <see cref="MGContentHost.CanChangeContent"/> is false.</returns>
        public bool TryAddChild(MGElement Item) => TryAddChild(Item, new(0));

        /// <param name="Offset">An offset to apply to this element's bounds. Use zero to have the element fill the entire panel's bounds (minus the panel's padding)</param>
        /// <returns>True if the given <paramref name="Item"/> was successfully added.<br/>
        /// False otherwise, such as if <see cref="MGContentHost.CanChangeContent"/> is false.</returns>
        public bool TryAddChild(MGElement Item, Thickness Offset)
        {
            if (!CanChangeContent)
                return false;

            _Children.Add(Item);
            ChildOffsets.Add(Item, Offset);
            return true;
        }

        /// <returns>True if the given <paramref name="Item"/> was found in <see cref="MGMultiContentHost.Children"/> and was successfully removed.<br/>
        /// False otherwise, such as if <see cref="MGContentHost.CanChangeContent"/> is false.</returns>
        public bool TryRemoveChild(MGElement Item)
        {
            if (!CanChangeContent)
                return false;

            if (_Children.Remove(Item))
            {
                ChildOffsets.Remove(Item);
                return true;
            }
            else
                return false;
        }

        /// <returns>True if the given <paramref name="Old"/> item was found in <see cref="MGMultiContentHost.Children"/> and was successfully replaced with <paramref name="New"/>.<br/>
        /// False otherwise, such as if <see cref="MGContentHost.CanChangeContent"/> is false.</returns>
        public bool TryReplaceChild(MGElement Old, MGElement New) => TryReplaceChild(Old, New, new(0));

        /// <param name="Offset">An offset to apply to the new element's bounds. Use zero to have the element fill the entire panel's bounds (minus the panel's padding)</param>
        /// <returns>True if the given <paramref name="Old"/> item was found in <see cref="MGMultiContentHost.Children"/> and was successfully replaced with <paramref name="New"/>.<br/>
        /// False otherwise, such as if <see cref="MGContentHost.CanChangeContent"/> is false.</returns>
        public bool TryReplaceChild(MGElement Old, MGElement New, Thickness Offset)
        {
            if (!CanChangeContent)
                return false;

            int Index = _Children.IndexOf(Old);
            if (Index < 0)
                return false;

            _Children[Index] = New;
            ChildOffsets.Remove(Old);
            ChildOffsets.Add(New, Offset);
            return true;
        }

        public MGOverlayPanel(MGWindow Window)
            : base(Window, MGElementType.OverlayPanel)
        {
            using (BeginInitializing())
            {

            }
        }

        protected override void UpdateContentLayout(Rectangle Bounds)
        {
            foreach (MGElement Child in Children)
            {
                Thickness Offset = ChildOffsets[Child];
                Child.UpdateLayout(Bounds.GetCompressed(Offset));
            }
        }

        protected override Thickness UpdateContentMeasurement(Size AvailableSize)
        {
            if (HasContent)
            {
                //  Measure each child
                Dictionary<MGElement, Size> RequestedSizes = new();
                foreach (MGElement Child in Children)
                {
                    Child.UpdateMeasurement(AvailableSize, out Thickness SelfSize, out Thickness FullSize, out _, out _);
                    Thickness Offset = ChildOffsets[Child];
                    RequestedSizes.Add(Child, FullSize.Size.Add(Offset.Size, 0, 0));
                }

                //  Return the largest dimensions
                return new Thickness(RequestedSizes.Values.Max(x => x.Width), RequestedSizes.Values.Max(x => x.Height), 0, 0);

            }
            else
                return UpdateContentMeasurementBaseImplementation(AvailableSize);
        }
    }
}
