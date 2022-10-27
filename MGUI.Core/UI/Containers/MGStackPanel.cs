using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using MGUI.Shared.Helpers;
using Microsoft.Xna.Framework;
using System.Collections.Specialized;

namespace MGUI.Core.UI.Containers
{
    public class MGStackPanel : MGMultiContentHost
    {
        private Orientation _Orientation;
        public Orientation Orientation
        {
            get => _Orientation;
            set
            {
                if (_Orientation != value)
                {
                    _Orientation = value;
                    NPC(nameof(Orientation));
                    LayoutChanged(this, true);
                }
            }
        }

        /*private FlowDirection _FlowDirection;
        public FlowDirection FlowDirection
        {
            get => _FlowDirection;
            set
            {
                if (_FlowDirection != value)
                {
                    _FlowDirection = value;
                    NPC(nameof(FlowDirection));
                    LayoutChanged(this, true);
                }
            }
        }*/

        private int _Spacing;
        /// <summary>A padding amount, in pixels, between each consecutive non-collapsed child in this <see cref="MGStackPanel"/>.<br/>
        /// Spacing is ignored between children where <see cref="MGElement.IsVisibilityCollapsed"/> is true.<para/>
        /// Default value: 4</summary>
        public int Spacing
        {
            get => _Spacing;
            set
            {
                if (_Spacing != value)
                {
                    _Spacing = value;
                    NPC(nameof(Spacing));
                    LayoutChanged(this, true);
                }
            }
        }

        /// <returns>True if the given <paramref name="Item"/> was successfully added.<br/>
        /// False otherwise, such as if <see cref="MGContentHost.CanChangeContent"/> is false.</returns>
        public bool TryAddChild(MGElement Item)
        {
            if (!CanChangeContent)
                return false;

            _Children.Add(Item);
            return true;
        }

        /// <returns>True if the given <paramref name="Item"/> was successfully inserted.<br/>
        /// False otherwise, such as if <see cref="MGContentHost.CanChangeContent"/> is false.</returns>
        public bool TryInsertChild(int Index, MGElement Item)
        {
            if (!CanChangeContent || Index < 0 || Index > _Children.Count)
                return false;

            if (Index == _Children.Count)
                return TryAddChild(Item);
            else
            {
                _Children.Insert(Index, Item);
                return true;
            }
        }

        /// <returns>True if the given <paramref name="Item"/> was found in <see cref="MGMultiContentHost.Children"/> and was successfully removed.<br/>
        /// False otherwise, such as if <see cref="MGContentHost.CanChangeContent"/> is false.</returns>
        public bool TryRemoveChild(MGElement Item)
        {
            if (!CanChangeContent)
                return false;

            return _Children.Remove(Item);
        }

        /// <returns>True if the given <paramref name="Old"/> item was found in <see cref="MGMultiContentHost.Children"/> and was successfully replaced with <paramref name="New"/>.<br/>
        /// False otherwise, such as if <see cref="MGContentHost.CanChangeContent"/> is false.</returns>
        public bool TryReplaceChild(MGElement Old, MGElement New)
        {
            if (!CanChangeContent)
                return false;

            int Index = _Children.IndexOf(Old);
            if (Index < 0)
                return false;

            _Children[Index] = New;
            return true;
        }

        public MGStackPanel(MGWindow Window, Orientation Orientation)
            : base(Window, MGElementType.StackPanel)
        {
            using (BeginInitializing())
            {
                this.Orientation = Orientation;
                //this.FlowDirection = FlowDirection.LeftToRight;
                Spacing = 4;
            }
        }

        protected override void UpdateContentLayout(Rectangle Bounds)
        {
            if (!HasContent)
                return;

            Size AvailableSize = new(Bounds.Width, Bounds.Height);
            Size RemainingSize = AvailableSize;

            int NonCollapsedChildrenCount = Children.Count(x => !x.IsVisibilityCollapsed);

            if (Orientation == Orientation.Vertical)
            {
                //  Measure each child
                Dictionary<MGElement, Thickness> RequestedSizes = new();
                foreach (MGElement Child in Children)
                {
                    Child.UpdateMeasurement(RemainingSize, out Thickness SelfSize, out Thickness FullSize, out _, out _);
                    RequestedSizes.Add(Child, FullSize);
                    int ConsumedHeight = FullSize.Height + (Child.IsVisibilityCollapsed ? 0 : Spacing);
                    RemainingSize = RemainingSize.Subtract(new Size(0, ConsumedHeight), 0, 0);
                }
                Thickness TotalContentSize = new(RequestedSizes.Values.Max(x => x.Width), RequestedSizes.Values.Sum(x => x.Height) + Spacing * (NonCollapsedChildrenCount - 1), 0, 0);

                //  Account for content alignment
                int ConsumedWidth = HorizontalContentAlignment == HorizontalAlignment.Stretch ? AvailableSize.Width : Math.Min(AvailableSize.Width, TotalContentSize.Width);
                Size ConsumedContentSize = new(ConsumedWidth, TotalContentSize.Height);
                Rectangle AlignedBounds = ApplyAlignment(Bounds, HorizontalContentAlignment, VerticalContentAlignment, ConsumedContentSize);

                //  Allocate space for each child
                int CurrentY = AlignedBounds.Top;
                foreach (MGElement Child in Children)
                {
                    int Height = RequestedSizes[Child].Height;
                    Rectangle ChildBounds = new(AlignedBounds.Left, CurrentY, AlignedBounds.Width, Height);
                    Child.UpdateLayout(ChildBounds);
                    CurrentY += Height + (Child.IsVisibilityCollapsed ? 0 : Spacing);
                }
            }
            else if (Orientation == Orientation.Horizontal)
            {
                //  Measure each child
                Dictionary<MGElement, Thickness> RequestedSizes = new();
                foreach (MGElement Child in Children)
                {
                    Child.UpdateMeasurement(RemainingSize, out Thickness SelfSize, out Thickness FullSize, out _, out _);
                    RequestedSizes.Add(Child, FullSize);
                    int ConsumedWidth = FullSize.Width + (Child.IsVisibilityCollapsed ? 0 : Spacing);
                    RemainingSize = RemainingSize.Subtract(new Size(ConsumedWidth, 0), 0, 0);
                }

                //  Account for content alignment
                Thickness TotalContentSize = new(RequestedSizes.Values.Sum(x => x.Width) + Spacing * (NonCollapsedChildrenCount - 1), RequestedSizes.Values.Max(x => x.Height), 0, 0);
                int ConsumedHeight = VerticalContentAlignment == VerticalAlignment.Stretch ? AvailableSize.Height : Math.Min(AvailableSize.Height, TotalContentSize.Height);
                Size ConsumedContentSize = new(TotalContentSize.Width, ConsumedHeight);
                Rectangle AlignedBounds = ApplyAlignment(Bounds, HorizontalContentAlignment, VerticalContentAlignment, ConsumedContentSize);

                //  Allocate space for each child
                int CurrentX = AlignedBounds.Left;
                foreach (MGElement Child in Children)
                {
                    int Width = RequestedSizes[Child].Width;
                    Rectangle ChildBounds = new(CurrentX, AlignedBounds.Top, Width, AlignedBounds.Height);
                    Child.UpdateLayout(ChildBounds);
                    CurrentX += Width + (Child.IsVisibilityCollapsed ? 0 : Spacing);
                }
            }
            else
                throw new NotImplementedException($"Unrecognized {nameof(Orientation)}: {Orientation}");
        }

        protected override Thickness UpdateContentMeasurement(Size AvailableSize)
        {
            if (HasContent)
            {
                Size RemainingSize = AvailableSize;

                int NonCollapsedChildrenCount = Children.Count(x => !x.IsVisibilityCollapsed);

                if (Orientation == Orientation.Vertical)
                {
                    Dictionary<MGElement, Thickness> RequestedSizes = new();
                    foreach (MGElement Child in Children)
                    {
                        Child.UpdateMeasurement(RemainingSize, out Thickness SelfSize, out Thickness FullSize, out _, out _);
                        RequestedSizes.Add(Child, FullSize);
                        int ConsumedHeight = FullSize.Height + (Child.IsVisibilityCollapsed ? 0 : Spacing);
                        RemainingSize = RemainingSize.Subtract(new Size(0, ConsumedHeight), 0, 0);
                    }

                    Thickness TotalContentSize = new(RequestedSizes.Values.Max(x => x.Width), RequestedSizes.Values.Sum(x => x.Height) + Spacing * (NonCollapsedChildrenCount - 1), 0, 0);
                    return TotalContentSize;
                }
                else if (Orientation == Orientation.Horizontal)
                {
                    Dictionary<MGElement, Thickness> RequestedSizes = new();
                    foreach (MGElement Child in Children)
                    {
                        Child.UpdateMeasurement(RemainingSize, out Thickness SelfSize, out Thickness FullSize, out _, out _);
                        RequestedSizes.Add(Child, FullSize);
                        int ConsumedWidth = FullSize.Width + (Child.IsVisibilityCollapsed ? 0 : Spacing);
                        RemainingSize = RemainingSize.Subtract(new Size(ConsumedWidth, 0), 0, 0);
                    }

                    Thickness TotalContentSize = new(RequestedSizes.Values.Sum(x => x.Width) + Spacing * (NonCollapsedChildrenCount - 1), RequestedSizes.Values.Max(x => x.Height), 0, 0);
                    return TotalContentSize;
                }
                else
                    throw new NotImplementedException($"Unrecognized {nameof(Orientation)}: {Orientation}");
            }
            else
                return UpdateContentMeasurementBaseImplementation(AvailableSize);
        }
    }
}
