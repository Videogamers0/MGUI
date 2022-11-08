﻿using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MGUI.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace MGUI.Core.UI.Containers
{
    /// <summary>Represents an <see cref="MGElement"/> that is capable of hosting one or more child <see cref="MGElement"/>s as its content.</summary>
    public abstract class MGContentHost : MGElement
    {
        protected override void AddComponent(MGComponentBase Component)
        {
            base.AddComponent(Component);
            InvokeContentAdded(Component.BaseElement);
        }

        public abstract override IEnumerable<MGElement> GetChildren();
        protected abstract override Thickness UpdateContentMeasurement(Size AvailableSize);
        protected abstract override void UpdateContentLayout(Rectangle Bounds);

        /// <summary>If false, attempting to add or remove children to this <see cref="MGContentHost"/> will throw an exception.<para/>
        /// This is typically only false in rare cases where a control has built-in logic to micro-manage its children,<br/>
        /// such as the <see cref="MGTabControl"/>'s <see cref="MGTabControl.HeadersPanelComponent"/>, or the <see cref="MGSingleContentHost.Content"/> of a <see cref="MGComboBox{TItemType}"/> (which is derived from <see cref="MGComboBox{TItemType}.SelectedItemTemplate"/>)</summary>
        public bool CanChangeContent { get; internal set; } = true;

        internal IDisposable AllowChangingContentTemporarily()
            => new TemporaryChange<bool>(CanChangeContent, true, x => CanChangeContent = x);

        /// <summary>Invoked when a child element is added directly to this <see cref="MGContentHost"/>.<br/>
        /// (I.E. an immediate child is added)<para/>See also: <see cref="OnNestedContentAdded"/></summary>
        public event EventHandler<MGElement> OnContentAdded;
        protected void InvokeContentAdded(MGElement Element)
        {
            if (Element != null)
            {
                OnContentAdded?.Invoke(this, Element);
                OnDirectOrNestedContentAdded?.Invoke(this, Element);

                foreach (MGElement Nested in Element.TraverseVisualTree(false, true))
                {
                    InvokeNestedContentAdded(this, Nested);
                }

                if (Element is MGContentHost ContentHost)
                {
                    ContentHost.OnContentAdded += InvokeNestedContentAdded;
                    ContentHost.OnNestedContentAdded += InvokeNestedContentAdded;
                    ContentHost.OnContentRemoved += InvokeNestedContentRemoved;
                    ContentHost.OnNestedContentRemoved += InvokeNestedContentRemoved;
                }
            }
        }

        public event EventHandler<MGElement> OnNestedContentAdded;
        private void InvokeNestedContentAdded(object sender, MGElement Element)
        {
            if (Element != null)
            {
                OnNestedContentAdded?.Invoke(this, Element);
                OnDirectOrNestedContentAdded?.Invoke(this, Element);
            }
        }

        /// <summary>Invoked after either <see cref="OnContentAdded"/> or <see cref="OnNestedContentAdded"/> are invoked.</summary>
        public event EventHandler<MGElement> OnDirectOrNestedContentAdded;

        /// <summary>Invoked when a child element is removed directly from this <see cref="MGContentHost"/>.<br/>
        /// (I.E. an immediate child is removed)<para/>See also: <see cref="OnNestedContentRemoved"/></summary>
        public event EventHandler<MGElement> OnContentRemoved;
        protected void InvokeContentRemoved(MGElement Element)
        {
            if (Element != null)
            {
                OnContentRemoved?.Invoke(this, Element);
                OnDirectOrNestedContentRemoved?.Invoke(this, Element);

                foreach (MGElement Nested in Element.TraverseVisualTree(false, true))
                {
                    InvokeNestedContentRemoved(this, Nested);
                }

                if (Element is MGContentHost ContentHost)
                {
                    ContentHost.OnContentAdded -= InvokeNestedContentAdded;
                    ContentHost.OnNestedContentAdded -= InvokeNestedContentAdded;
                    ContentHost.OnContentRemoved -= InvokeNestedContentRemoved;
                    ContentHost.OnNestedContentRemoved -= InvokeNestedContentRemoved;
                }
            }
        }

        public event EventHandler<MGElement> OnNestedContentRemoved;
        private void InvokeNestedContentRemoved(object sender, MGElement Element)
        {
            if (Element != null)
            {
                OnNestedContentRemoved?.Invoke(this, Element);
                OnDirectOrNestedContentRemoved?.Invoke(this, Element);
            }
        }

        /// <summary>Invoked after either <see cref="OnContentRemoved"/> or <see cref="OnNestedContentRemoved"/> are invoked.</summary>
        public event EventHandler<MGElement> OnDirectOrNestedContentRemoved;

        /// <summary>Only intended to be used by <see cref="MGWindow"/>'s constructor.</summary>
        protected MGContentHost(MGDesktop UI, MGWindow Window, MGElementType ElementType)
            : base(UI, Window, ElementType) { }

        protected MGContentHost(MGWindow Window, MGElementType ElementType)
            : base(Window, ElementType)
        {

        }

        protected override void UpdateContents(ElementUpdateArgs UA)
        {
            foreach (MGElement Child in GetVisualTreeChildren().ToList())
                Child.Update(UA);
        }

        protected override void DrawContents(ElementDrawArgs DA)
        {
            foreach (MGElement Child in GetChildren())
                Child.Draw(DA);
        }
    }

    /// <summary>Represents an <see cref="MGElement"/> that is capable of hosting 1 or more <see cref="MGElement"/>s as its content, such as an <see cref="MGStackPanel"/>.</summary>
    public abstract class MGMultiContentHost : MGContentHost
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        protected ObservableCollection<MGElement> _Children { get; }
        public IReadOnlyList<MGElement> Children => _Children;
        public override IEnumerable<MGElement> GetChildren() => Children;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool HasContent => _Children.Any();
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool HasMultipleChildren => _Children.Count > 1;

        public MGMultiContentHost(MGWindow Window, MGElementType ElementType)
            : base(Window, ElementType)
        {
            using (BeginInitializing())
            {
                _Children = new();

                _Children.CollectionChanged += (sender, e) =>
                {
                    if (e.Action is NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Replace or NotifyCollectionChangedAction.Reset && e.OldItems != null)
                    {
                        foreach (MGElement Item in e.OldItems)
                        {
                            Item.SetParent(null);
                            InvokeContentRemoved(Item);
                        }
                    }

                    if (e.Action is NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Replace && e.NewItems != null)
                    {
                        foreach (MGElement Item in e.NewItems)
                        {
                            Item.SetParent(this);
                            InvokeContentAdded(Item);
                        }
                    }

                    LayoutChanged(this, true);
                };
            }
        }
    }

    /// <summary>Represents an <see cref="MGElement"/> that is capable of hosting exactly one child <see cref="MGElement"/> as its content, such as an <see cref="MGBorder"/>.</summary>
    public abstract class MGSingleContentHost : MGContentHost
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        protected MGElement _Content;
        public MGElement Content { get => _Content; }

        protected virtual void SetContentVirtual(MGElement Value)
        {
            if (_Content != Value)
            {
                if (!CanChangeContent)
                    throw new InvalidOperationException($"Cannot set {nameof(MGSingleContentHost)}.{nameof(Content)} while {nameof(CanChangeContent)} is false.");

                _Content?.SetParent(null);
                InvokeContentRemoved(_Content);
                _Content = Value;
                _Content?.SetParent(this);
                LayoutChanged(this, true);
                InvokeContentAdded(_Content);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool HasContent => Content != null;

        /// <returns>This <see cref="MGContentHost"/> (for convenience, to chain multiple <see cref="SetContent{THostType}(MGElement)"/> calls. Such as:<para/>
        /// <code>new MGBorder(...).SetContent(new MGButton(...).SetContent(new MGTextBlock(...)))</code></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public THostType SetContent<THostType>(MGElement Content)
            where THostType : MGSingleContentHost
        {
            SetContentVirtual(Content);
            return this as THostType;
        }

        public MGSingleContentHost SetContent(MGElement Content)
            => SetContent<MGSingleContentHost>(Content);

        public THostType SetContent<THostType>(string Content)
            where THostType : MGSingleContentHost
            => SetContent<THostType>(new MGTextBlock(SelfOrParentWindow, Content));

        public MGSingleContentHost SetContent(string Content)
            => SetContent<MGSingleContentHost>(new MGTextBlock(SelfOrParentWindow, Content));

        /// <summary>Only intended to be used by <see cref="MGWindow"/>'s constructor.</summary>
        protected MGSingleContentHost(MGDesktop UI, MGWindow Window, MGElementType ElementType)
            : base(UI, Window, ElementType) { }

        protected MGSingleContentHost(MGWindow Window, MGElementType ElementType)
            : base(Window, ElementType)
        {

        }

        public override IEnumerable<MGElement> GetChildren()
        {
            if (Content != null)
            {
                yield return Content;
            }
        }

        protected override Thickness UpdateContentMeasurement(Size AvailableSize)
        {
            if (HasContent)
            {
                Content.UpdateMeasurement(AvailableSize, out _, out Thickness ContentSize, out _, out _);
                return ContentSize;
            }
            else
                return UpdateContentMeasurementBaseImplementation(AvailableSize);
        }

        protected override void UpdateContentLayout(Rectangle Bounds)
        {
            Content?.UpdateLayout(Bounds);
        }
    }

    /// <summary>Lightweight wrapper class that contains a single <see cref="MGElement"/> as its <see cref="MGSingleContentHost.Content"/></summary>
    public class MGContentPresenter : MGSingleContentHost
    {
        public MGContentPresenter(MGWindow Window)
            : base(Window, MGElementType.ContentPresenter)
        {

        }

        public override void DrawSelf(ElementDrawArgs DA, Rectangle LayoutBounds) { }
    }

    /// <summary>A wrapper element that displays a <see cref="Header"/> at a given <see cref="HeaderPosition"/> in addition to the <see cref="MGSingleContentHost.Content"/></summary>
    public class MGHeaderedContentPresenter : MGSingleContentHost
    {
        public MGElement Header
        {
            get => HeaderPresenter.Content;
            set => HeaderPresenter.SetContent(value);
        }

        private Dock _HeaderPosition;
        public Dock HeaderPosition
        {
            get => _HeaderPosition;
            set => SetHeaderPosition(value, false);
        }

        private void SetHeaderPosition(Dock Value, bool IsInitializing)
        {
            if (_HeaderPosition != Value || IsInitializing)
            {
                _HeaderPosition = Value;

                switch (HeaderPosition)
                {
                    case Dock.Left:
                        HeaderPresenterComponent.IsWidthSharedWithContent = false;
                        HeaderPresenterComponent.IsHeightSharedWithContent = true;
                        HeaderPresenterComponent.ConsumesLeftSpace = true;
                        HeaderPresenterComponent.ConsumesTopSpace = true;
                        HeaderPresenterComponent.ConsumesRightSpace = false;
                        HeaderPresenterComponent.ConsumesBottomSpace = false;
                        break;
                    case Dock.Top:
                        HeaderPresenterComponent.IsWidthSharedWithContent = true;
                        HeaderPresenterComponent.IsHeightSharedWithContent = false;
                        HeaderPresenterComponent.ConsumesLeftSpace = true;
                        HeaderPresenterComponent.ConsumesTopSpace = true;
                        HeaderPresenterComponent.ConsumesRightSpace = false;
                        HeaderPresenterComponent.ConsumesBottomSpace = false;
                        break;
                    case Dock.Right:
                        HeaderPresenterComponent.IsWidthSharedWithContent = false;
                        HeaderPresenterComponent.IsHeightSharedWithContent = true;
                        HeaderPresenterComponent.ConsumesLeftSpace = false;
                        HeaderPresenterComponent.ConsumesTopSpace = true;
                        HeaderPresenterComponent.ConsumesRightSpace = true;
                        HeaderPresenterComponent.ConsumesBottomSpace = false;
                        break;
                    case Dock.Bottom:
                        HeaderPresenterComponent.IsWidthSharedWithContent = true;
                        HeaderPresenterComponent.IsHeightSharedWithContent = false;
                        HeaderPresenterComponent.ConsumesLeftSpace = true;
                        HeaderPresenterComponent.ConsumesTopSpace = false;
                        HeaderPresenterComponent.ConsumesRightSpace = false;
                        HeaderPresenterComponent.ConsumesBottomSpace = true;
                        break;
                    default: throw new NotImplementedException($"Unrecognized {nameof(Dock)}: {this.HeaderPosition}");
                }

                UpdateHeaderMargin();
                LayoutChanged(this, true);
            }
        }

        public const int DefaultSpacing = 5;

        private int _Spacing;
        /// <summary>Empty space, in pixels, between the <see cref="Header"/> and <see cref="MGSingleContentHost.Content"/>.<para/>
        /// Default value: <see cref="DefaultSpacing"/></summary>
        public int Spacing
        {
            get => _Spacing;
            set
            {
                if (_Spacing != value)
                {
                    _Spacing = value;
                    UpdateHeaderMargin();
                }
            }
        }

        private void UpdateHeaderMargin()
        {
            HeaderPresenter.Margin = HeaderPosition switch
            {
                Dock.Left => new(0, 0, Spacing, 0),
                Dock.Top => new(0, 0, 0, Spacing),
                Dock.Right => new(Spacing, 0, 0, 0),
                Dock.Bottom => new(0, Spacing, 0, 0),
                _ => throw new NotImplementedException($"Unrecognized {nameof(Dock)}: {this.HeaderPosition}")
            };
        }

        public MGComponent<MGContentPresenter> HeaderPresenterComponent { get; }
        public MGContentPresenter HeaderPresenter { get; }

        public MGHeaderedContentPresenter(MGWindow Window, MGElement Header = null, MGElement Content = null)
            : base(Window, MGElementType.HeaderedContentPresenter)
        {
            using (BeginInitializing())
            {
                this.HeaderPresenter = new(Window);
                this.HeaderPresenterComponent = new(HeaderPresenter, false, false, false, false, false, false, false,
                    (AvailableBounds, ComponentSize) => this.HeaderPosition switch
                    {
                        Dock.Left => ApplyAlignment(AvailableBounds, HorizontalAlignment.Left, VerticalAlignment.Stretch, ComponentSize.Size),
                        Dock.Top => ApplyAlignment(AvailableBounds, HorizontalAlignment.Stretch, VerticalAlignment.Top, ComponentSize.Size),
                        Dock.Right => ApplyAlignment(AvailableBounds, HorizontalAlignment.Right, VerticalAlignment.Stretch, ComponentSize.Size),
                        Dock.Bottom => ApplyAlignment(AvailableBounds, HorizontalAlignment.Stretch, VerticalAlignment.Bottom, ComponentSize.Size),
                        _ => throw new NotImplementedException($"Unrecognized {nameof(Dock)}: {this.HeaderPosition}")
                    });
                AddComponent(HeaderPresenterComponent);

                SetHeaderPosition(Dock.Left, true);

                Spacing = DefaultSpacing;
            }
        }
    }
}
