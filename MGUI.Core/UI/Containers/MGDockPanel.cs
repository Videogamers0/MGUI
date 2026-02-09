using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using MGUI.Shared.Helpers;
using Microsoft.Xna.Framework;
using System.Collections.Specialized;
using System.Collections.Generic;
using System;
using MonoGame.Extended;

namespace MGUI.Core.UI.Containers
{
    public class MGDockPanel : MGMultiContentHost
    {
        private readonly record struct DockedChild(MGElement Item, Dock Position)
        {
            public bool IsLeftOrRight => Position == Dock.Left || Position == Dock.Right;
            public bool IsTopOrBottom => Position == Dock.Top || Position == Dock.Bottom;
        }

        private readonly record struct ActualDockedChild(MGElement Item, Dock? Position)
        {
            public bool IsLastChild => Position == null;
            public bool IsLeftOrRight => Position.HasValue && (Position == Dock.Left || Position == Dock.Right);
            public bool IsTopOrBottom => Position.HasValue && (Position == Dock.Top || Position == Dock.Bottom);
        }

        private bool _LastChildFill;
        /// <summary>If true, the last child of this <see cref="MGDockPanel"/> will consume all remaining available space, regardless of it's <see cref="Dock"/> position.<para/>
        /// Default value: true</summary>
        public bool LastChildFill
        {
            get => _LastChildFill;
            set
            {
                if (_LastChildFill != value)
                {
                    _LastChildFill = value;
                    LayoutChanged(this, true);
                    NPC(nameof(LastChildFill));
                }
            }
        }

        private readonly List<DockedChild> DockedChildren = new();
        /// <summary>The last child will consume all remaining available space if <see cref="LastChildFill"/> is true, thus ignoring its <see cref="DockedChild.Position"/> value.</summary>
        private IEnumerable<ActualDockedChild> ActualDockedChildren
        {
            get
            {
                foreach (DockedChild Item in DockedChildren)
                {
                    if (Item == DockedChildren[^1] && LastChildFill)
                        yield return new ActualDockedChild(Item.Item, null);
                    else
                        yield return new ActualDockedChild(Item.Item, Item.Position);
                }
            }
        }

        public bool TryAddChild(MGElement Item, Dock Dock)
        {
            if (!CanChangeContent)
                return false;

            DockedChildren.Add(new(Item, Dock));
            _Children.Add(Item);
            return true;
        }

        public MGDockPanel(MGWindow Window, bool LastChildFill = true)
            : base(Window, MGElementType.DockPanel)
        {
            using (BeginInitializing())
            {
                this.LastChildFill = LastChildFill;
            }
        }

        protected override void UpdateContentLayout(Rectangle Bounds)
        {
            if (!HasContent)
                return;

            Size AvailableSize = new(Bounds.Width, Bounds.Height);

            //Referenced:
            //https://referencesource.microsoft.com/#PresentationFramework/src/Framework/System/windows/Controls/DockPanel.cs

            int AccumulatedLeft = 0;
            int AccumulatedTop = 0;
            int AccumulatedRight = 0;
            int AccumulatedBottom = 0;

            foreach (ActualDockedChild Child in ActualDockedChildren)
            {
                int AccumulatedWidth = AccumulatedLeft + AccumulatedRight;
                int AccumulatedHeight = AccumulatedTop + AccumulatedBottom;

                Size RemainingSize = AvailableSize.Subtract(new Size(AccumulatedWidth, AccumulatedHeight), 0, 0);
                Child.Item.UpdateMeasurement(RemainingSize, out Thickness SelfSize, out Thickness FullSize, out _, out _);

                Rectangle ChildBounds;
                if (!Child.Position.HasValue)
                {
                    ChildBounds = new(Bounds.Left + AccumulatedLeft, Bounds.Top + AccumulatedTop, Bounds.Width - AccumulatedWidth, Bounds.Height - AccumulatedHeight);
                }
                else
                {
                    switch (Child.Position.Value)
                    {
                        case Dock.Left:
                            ChildBounds = new(Bounds.Left + AccumulatedLeft, Bounds.Top + AccumulatedTop, FullSize.Width, RemainingSize.Height);
                            AccumulatedLeft += FullSize.Width;
                            break;

                        case Dock.Right:
                            AccumulatedRight += FullSize.Width;
                            ChildBounds = new(Bounds.Right - AccumulatedRight, Bounds.Top + AccumulatedTop, FullSize.Width, RemainingSize.Height);
                            break;

                        case Dock.Top:
                            ChildBounds = new(Bounds.Left + AccumulatedLeft, Bounds.Top + AccumulatedTop, RemainingSize.Width, FullSize.Height);
                            AccumulatedTop += FullSize.Height;
                            break;

                        case Dock.Bottom:
                            AccumulatedBottom += FullSize.Height;
                            ChildBounds = new(Bounds.Left + AccumulatedLeft, Bounds.Bottom - AccumulatedBottom, RemainingSize.Width, FullSize.Height);
                            break;

                        default: throw new NotImplementedException($"Unrecognized {nameof(Dock)}: {Child.Position.Value}");
                    }
                }

                Child.Item.UpdateLayout(ChildBounds);
            }
        }

        protected override Thickness UpdateContentMeasurement(Size AvailableSize)
        {
            if (HasContent)
            {
                //Referenced:
                //https://referencesource.microsoft.com/#PresentationFramework/src/Framework/System/windows/Controls/DockPanel.cs

                int TotalContentWidth = 0;
                int TotalContentHeight = 0;
                int AccumulatedWidth = 0;
                int AccumulatedHeight = 0;

                foreach (DockedChild Child in DockedChildren)
                {
                    Size RemainingSize = AvailableSize.Subtract(new Size(AccumulatedWidth, AccumulatedHeight), 0, 0);
                    Child.Item.UpdateMeasurement(RemainingSize, out Thickness SelfSize, out Thickness FullSize, out _, out _);

                    if (Child.IsLeftOrRight)
                    {
                        TotalContentHeight = Math.Max(TotalContentHeight, AccumulatedHeight + FullSize.Height);
                        AccumulatedWidth += FullSize.Width;
                    }
                    else if (Child.IsTopOrBottom)
                    {
                        TotalContentWidth = Math.Max(TotalContentWidth, AccumulatedWidth + FullSize.Width);
                        AccumulatedHeight += FullSize.Height;
                    }
                }

                TotalContentWidth = Math.Max(TotalContentWidth, AccumulatedWidth);
                TotalContentHeight = Math.Max(TotalContentHeight, AccumulatedHeight);

                return new Thickness(TotalContentWidth, TotalContentHeight, 0, 0);
            }
            else
                return UpdateContentMeasurementBaseImplementation(AvailableSize);
        }
    }
}
