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
    /// <summary>A simple layout container that renders all its child elements overtop of each other, in the order they were added to this container, or in order of their Z-Index if specified.<para/>
    /// Children may also have an optional <see cref="Thickness"/> offset that determines how much empty space is around them inside this panel.</summary>
    public class MGOverlayPanel : MGMultiContentHost
    {
        private class OverlayChildComparer : IComparer<OverlayPanelChild>
        {
            public int Compare(OverlayPanelChild x, OverlayPanelChild y)
            {
                if (!x.HasZIndex && !y.HasZIndex)
                    return 0;
                else if (x.HasZIndex && y.HasZIndex)
                    return x.ZIndex.Value.CompareTo(y.ZIndex.Value);
                else if (!x.HasZIndex)
                    return -1;
                else
                    return 1;
            }
        }

        private readonly record struct OverlayPanelChild(MGElement Item, Thickness Offset, double? ZIndex)
        {
            public bool HasZIndex => ZIndex.HasValue;
        }

        private readonly List<OverlayPanelChild> PanelChildren = new();
        private IEnumerable<OverlayPanelChild> SortedChildren => PanelChildren.OrderBy(x => x.HasZIndex).ThenBy(x => x.ZIndex ?? int.MinValue);

        /// <param name="Offset">An offset to apply to this element's bounds. Use zero to have the element fill the entire panel's bounds (minus the panel's padding)</param>
        /// <param name="ZIndex">Determines the order that children are rendered in. Higher value is rendered last, appearing overtop of everything else.<para/>
        /// Children with <paramref name="ZIndex"/>=null will be rendered first.<para/>
        /// If multiple children have the same <paramref name="ZIndex"/> value, they are rendered in the same order that they were added to this panel in.</param>
        /// <returns>True if the given <paramref name="Item"/> was successfully added.<br/>
        /// False otherwise, such as if <see cref="MGContentHost.CanChangeContent"/> is false.</returns>
        public bool TryAddChild(MGElement Item, Thickness Offset = default, double? ZIndex = null)
        {
            if (!CanChangeContent)
                return false;

            if (!_Children.Contains(Item))
            {
                _Children.Add(Item);
                PanelChildren.Add(new OverlayPanelChild(Item, Offset, ZIndex));
                return true;
            }
            else
                return false;
        }

        /// <returns>True if the given <paramref name="Item"/> was found in <see cref="MGMultiContentHost.Children"/> and was successfully removed.<br/>
        /// False otherwise, such as if <see cref="MGContentHost.CanChangeContent"/> is false.</returns>
        public bool TryRemoveChild(MGElement Item)
        {
            if (!CanChangeContent)
                return false;

            if (_Children.Remove(Item))
            {
                OverlayPanelChild ToRemove = PanelChildren.First(x => x.Item == Item);
                PanelChildren.Remove(ToRemove);
                return true;
            }
            else
                return false;
        }

        /// <param name="Offset">An offset to apply to the new element's bounds. Use zero to have the element fill the entire panel's bounds (minus the panel's padding)</param>
        /// <returns>True if the given <paramref name="Old"/> item was found in <see cref="MGMultiContentHost.Children"/> and was successfully replaced with <paramref name="New"/>.<br/>
        /// False otherwise, such as if <see cref="MGContentHost.CanChangeContent"/> is false.</returns>
        public bool TryReplaceChild(MGElement Old, MGElement New, Thickness Offset = default, double? ZIndex = null)
        {
            if (!CanChangeContent)
                return false;

            int Index = _Children.IndexOf(Old);
            if (Index < 0)
                return false;

            _Children[Index] = New;
            OverlayPanelChild ToRemove = PanelChildren.First(x => x.Item == Old);
            PanelChildren.Remove(ToRemove);
            PanelChildren.Add(new OverlayPanelChild(New, Offset, ZIndex));
            return true;
        }

        public MGOverlayPanel(MGWindow Window)
            : base(Window, MGElementType.OverlayPanel)
        {
            using (BeginInitializing())
            {

            }
        }

        public override IEnumerable<MGElement> GetChildren() => SortedChildren.Select(x => x.Item);

        protected override void UpdateContentLayout(Rectangle Bounds)
        {
            foreach (OverlayPanelChild Child in SortedChildren)
            {
                Thickness Offset = Child.Offset;
                Child.Item.UpdateLayout(Bounds.GetCompressed(Offset));
            }
        }

        protected override Thickness UpdateContentMeasurement(Size AvailableSize)
        {
            if (HasContent)
            {
                //  Measure each child
                Dictionary<MGElement, Size> RequestedSizes = new();
                foreach (OverlayPanelChild Child in PanelChildren)
                {
                    Child.Item.UpdateMeasurement(AvailableSize, out Thickness SelfSize, out Thickness FullSize, out _, out _);
                    Thickness Offset = Child.Offset;
                    RequestedSizes.Add(Child.Item, FullSize.Size.Add(Offset.Size, 0, 0));
                }

                //  Return the largest dimensions
                return new Thickness(RequestedSizes.Values.Max(x => x.Width), RequestedSizes.Values.Max(x => x.Height), 0, 0);

            }
            else
                return UpdateContentMeasurementBaseImplementation(AvailableSize);
        }
    }
}
