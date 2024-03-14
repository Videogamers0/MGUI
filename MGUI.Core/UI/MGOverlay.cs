using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.Containers;
using MGUI.Shared.Helpers;
using MGUI.Shared.Input;
using MGUI.Shared.Input.Mouse;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI
{
    /// <summary>Manages 1 or more <see cref="MGOverlay"/>s, allowing you to define overlay(s) overtop of a piece of content. The overlays span the same bounds and scope as this <see cref="MGOverlayHost"/>.<para/>
    /// Although you could just use an <see cref="MGOverlayPanel"/> to achieve similar results, <see cref="MGOverlay"/>s provides a more convenient and specialized use-case.<br/>
    /// For example, this class can guarantee that no inputs fall-through to content underneath the overlay (<see cref="IsModal"/>=<see langword="true"/>). It also ensures that, if there are multiple overlays
    /// for the same content, at most only 1 of them will be visibile/interactable at a time.</summary>
    public class MGOverlayHost : MGSingleContentHost
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private List<MGOverlay> _Overlays { get; } = new();
        public IReadOnlyList<MGOverlay> Overlays => _Overlays;

        /// <summary>Add a new <see cref="MGOverlay"/> with the given <paramref name="OverlayContent"/> to this <see cref="MGOverlayHost"/>.<para/>
        /// Note: this does not open the overlay.<br/>See also: <see cref="TryOpen(MGOverlay)"/></summary>
        /// <param name="OverlayContent">The content to display in the created overlay element.</param>
        /// <returns>The newly-created overlay.</returns>
        public MGOverlay AddOverlay(MGElement OverlayContent)
        {
            MGOverlay Overlay = new(this, OverlayContent);
            Overlay.OnZIndexChanged += HandleOverlayZIndexChanged;
            _Overlays.Add(Overlay);
            InvokeContentAdded(Overlay);
            UpdateActiveOverlay();
            return Overlay;
        }

        /// <returns><see langword="true"/> if the given <paramref name="Overlay"/> was successfully removed.<para/>
        /// <see langword="false"/> if it could not be removed, either because it was not a valid <see cref="MGOverlay"/> belonging to this <see cref="MGOverlayHost"/>,
        /// or because it was already opened and closing it was cancelled by <see cref="MGOverlay.OnClosing"/>.</returns>
        public bool TryRemoveOverlay(MGOverlay Overlay)
        {
            if (_Overlays.Contains(Overlay))
            {
                bool WasOpen = _OpenOverlays.Contains(Overlay);
                if (WasOpen)
                {
                    CancelEventArgs<MGOverlay> ClosingArgs = new(Overlay);
                    Overlay.InvokeOnClosing(ClosingArgs);
                    if (ClosingArgs.Cancel)
                        return false;
                }

                Overlay.OnZIndexChanged -= HandleOverlayZIndexChanged;
                _Overlays.Remove(Overlay);
                InvokeContentRemoved(Overlay);

                if (WasOpen)
                {
                    Overlay.InvokeOnClosed();
                    Overlay.NPC(nameof(MGOverlay.IsOpen));
                    UpdateActiveOverlay();
                }

                return true;
            }

            return false;
        }

        private void HandleOverlayZIndexChanged(object sender, double e)
        {
            if (sender is MGOverlay Overlay && _OpenOverlays.Contains(Overlay) && _OpenOverlays.Count > 1)
                UpdateActiveOverlay();
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private List<MGOverlay> _OpenOverlays { get; } = new();
        /// <summary>The overlays that are currently open.<br/>
        /// Note: If multiple overlays are open, only 1 of them can be visible/interactable at a time.<para/> 
        /// To open or close an overlay, use:<br/><see cref="TryOpen(MGOverlay)"/><br/><see cref="TryClose(MGOverlay)"/><para/>
        /// See also: <see cref="ActiveOverlay"/></summary>
        public IReadOnlyList<MGOverlay> OpenOverlays => _OpenOverlays;

        public bool TryOpen(MGOverlay Overlay)
        {
            //  Validate that the overlay belongs to this element
            if (!_Overlays.Contains(Overlay))
                return false;

            //  Validate that the overlay isn't already open
            if (_OpenOverlays.Contains(Overlay))
                return false;

            CancelEventArgs<MGOverlay> OpeningArgs = new(Overlay);
            Overlay.InvokeOnOpening(OpeningArgs);
            if (OpeningArgs.Cancel)
                return false;

            _OpenOverlays.Add(Overlay);
            Overlay.InvokeOnOpened();
            Overlay.NPC(nameof(MGOverlay.IsOpen));

            UpdateActiveOverlay();
            return true;
        }

        public bool TryClose(MGOverlay Overlay)
        {
            if (!_OpenOverlays.Contains(Overlay))
                return false;

            CancelEventArgs<MGOverlay> ClosingArgs = new(Overlay);
            Overlay.InvokeOnClosing(ClosingArgs);
            if (ClosingArgs.Cancel)
                return false;

            _OpenOverlays.Remove(Overlay);
            Overlay.InvokeOnClosed();
            Overlay.NPC(nameof(MGOverlay.IsOpen));

            UpdateActiveOverlay();
            return true;
        }

        private bool _IsModal;
        /// <summary>If <see langword="true" />, unhandled mouse inputs will not be allowed to fall-through to content underneath the active overlay when an overlay is being displayed.<para/>
        /// Default value: <see langword="true" /></summary>
        public bool IsModal
        {
            get => _IsModal;
            set
            {
                if (_IsModal != value)
                {
                    _IsModal = value;
                    NPC(nameof(IsModal));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private MGOverlay _ActiveOverlay;
        /// <summary>The currently active overlay, or <see langword="null" /> if no overlays are open.<br/>
        /// If multiple overlays are open, this is the overlay with the highest <see cref="MGOverlay.ZIndex"/> value.<para/>
        /// This value is automatically derived from the <see cref="OpenOverlays"/>.<br/>
        /// To change this value, use:<br/><see cref="TryOpen(MGOverlay)"/><br/><see cref="TryClose(MGOverlay)"/><para/>
        /// See also: <see cref="OnActiveOverlayChanged"/></summary>
        public MGOverlay ActiveOverlay => _ActiveOverlay;

        private void SetActiveOverlay(MGOverlay Value)
        {
            if (_ActiveOverlay != Value)
            {
                MGOverlay PreviousValue = _ActiveOverlay;
                _ActiveOverlay = Value;
                NPC(nameof(ActiveOverlay));
                ActiveOverlayPresenter.SetContent(ActiveOverlay);
                ActiveOverlayPresenter.Visibility = ActiveOverlay == null ? Visibility.Collapsed : Visibility.Visible;
                //LayoutChanged(this, true);
                PreviousValue?.InvokeOnDeactivated();
                ActiveOverlay?.InvokeOnActivated();
                OnActiveOverlayChanged?.Invoke(this, new(PreviousValue, ActiveOverlay));
            }
        }

        private void UpdateActiveOverlay() => SetActiveOverlay(_OpenOverlays.OrderByDescending(x => x.ZIndex).FirstOrDefault());

        /// <summary>Invoked when <see cref="ActiveOverlay"/> changes to a new value.<para/>
        /// Note: This event is invoked after <see cref="MGOverlay.OnActivated"/> and <see cref="MGOverlay.OnDeactivated"/>.</summary>
        public event EventHandler<EventArgs<MGOverlay>> OnActiveOverlayChanged;

        public MGComponent<MGContentPresenter> ActiveOverlayPresenterComponent { get; }
        /// <summary>The wrapper that renders the <see cref="ActiveOverlay"/>. This element is only visible when there is an overlay being shown.</summary>
        public MGContentPresenter ActiveOverlayPresenter { get; }

        public MGOverlayHost(MGWindow Window)
            : base(Window, MGElementType.OverlayHost)
        {
            using (BeginInitializing())
            {
                _Overlays = new();
                OverlayBackground = DefaultOverlayBackground.Copy();

                IsModal = true;

                Padding = new(4);

                ActiveOverlayPresenter = new(Window, true);
                ActiveOverlayPresenterComponent = new(ActiveOverlayPresenter, ComponentUpdatePriority.BeforeContents, ComponentDrawPriority.AfterContents, true, true, true, true, false, false, true,
                    (AvailableBounds, ComponentSize) => AvailableBounds.GetCompressed(Padding));
                ActiveOverlayPresenter.Visibility = Visibility.Collapsed;
                AddComponent(ActiveOverlayPresenterComponent);

                //  Ensure all inputs are handled by the overlay presenter so they cannot fall-through to the content when an overlay is active
                //TODO what about keyboard inputs?
                ActiveOverlayPresenter.MouseHandler.PressedInside += (sender, e) => TryHandleInputs(() => e.SetHandledBy(this, false));
                ActiveOverlayPresenter.MouseHandler.ReleasedInside += (sender, e) => TryHandleInputs(() => e.SetHandledBy(this, false));
                ActiveOverlayPresenter.MouseHandler.DragStart += (sender, e) => TryHandleInputs(() => e.SetHandledBy(this, false));
                ActiveOverlayPresenter.MouseHandler.Scrolled += (sender, e) =>
                {
                    if (IsModal && ActiveOverlay != null && Content != null)
                    {
                        //  Only swallow the mouse scroll events if the content underneath the overlay is scrollable.
                        //  This will allow scroll events to continue bubbling up the visual tree if the content under the overlay wasn't scrollable
                        //  (Such as if the parent of the OverlayHost was wrapped in a ScrollViewer)
                        bool IsContentScrollable = Content.TraverseVisualTree(true, true, false, false, TreeTraversalMode.Preorder).Any(x => x.ElementType == MGElementType.ScrollViewer);
                        if (IsContentScrollable)
                            TryHandleInputs(() => e.SetHandledBy(this, false));
                    }
                };
                void TryHandleInputs(Action SetHandled)
                {
                    if (IsModal && ActiveOverlay != null)
                    {
                        SetHandled();
                    }
                }
            }
        }

        //Maybe an IsMutuallyExclusive property, which determines if multiple overlays can be active concurrently?

        protected override void SetContentVirtual(MGElement Value)
        {
            if (_Content != Value)
            {
                MGElement Previous = _Content;
                base.SetContentVirtual(Value);

                if (Previous != null)
                    Previous.OnEndDraw -= Content_OnEndDraw;
                if (Content != null)
                    Content.OnEndDraw += Content_OnEndDraw;

                void Content_OnEndDraw(object sender, MGElementDrawEventArgs e)
                {
                    //  Draw the OverlayBackground overtop of the content, but underneath the active overlay
                    if (ActiveOverlay != null && OverlayBackground != null)
                    {
                        Rectangle BorderlessBounds = !HasBorder ? LayoutBounds : LayoutBounds.GetCompressed(GetBorder().BorderThickness);
                        Rectangle BackgroundBounds = BorderlessBounds.GetCompressed(BackgroundRenderPadding);
                        OverlayBackground.Draw(e.DA, this, BackgroundBounds);
                    }
                }
            }
        }

        /// <summary>Default value: A solid black brush with 0.35 opacity.</summary>
        public static IFillBrush DefaultOverlayBackground { get; set; } = Color.Black.AsFillBrush() * 0.35f;

        private IFillBrush _OverlayBackground;
        /// <summary>A background brush drawn overtop of the content, but underneath the <see cref="ActiveOverlay"/>.<br/>
        /// Unlike <see cref="MGElement.BackgroundBrush"/>, this brush is only drawn if an overlay is currently being shown.<br/>
        /// Recommended to use a semi-transparent brush so that the content underneath the overlay is still partially visible.<para/>
        /// Default value: <see cref="DefaultOverlayBackground"/></summary>
        public IFillBrush OverlayBackground
        {
            get => _OverlayBackground;
            set
            {
                if (_OverlayBackground != value)
                {
                    _OverlayBackground = value;
                    NPC(nameof(OverlayBackground));
                }
            }
        }

        /*public override IEnumerable<MGElement> GetChildren()
        {
            foreach (MGElement Child in base.GetChildren())
                yield return Child;

            //  The ActiveOverlay is wrapped in an MGComponent so it should already be enumerated
            if (ActiveOverlay != null)
                yield return ActiveOverlay;
        }*/

        public override IEnumerable<MGElement> GetVisualTreeChildren(bool IncludeInactive, bool IncludeActive)
        {
            foreach (MGElement Child in base.GetVisualTreeChildren(IncludeInactive, IncludeActive))
                yield return Child;

            if (IncludeInactive)
            {
                foreach (MGOverlay InactiveOverlay in _Overlays.Where(x => x != ActiveOverlay))
                    yield return InactiveOverlay;
            }

            //  The ActiveOverlay is wrapped in an MGComponent so it should already be enumerated
            //if (IncludeActive && ActiveOverlay != null)
            //    yield return ActiveOverlay;
        }
    }

    /// <summary>Represents an overlay overtop of a piece of content. To instantiate this class, create an <see cref="MGOverlayHost"/> and call <see cref="MGOverlayHost.AddOverlay(MGElement)"/></summary>
    public class MGOverlay : MGSingleContentHost
    {
        public MGOverlayHost Host { get; }

        #region Border
        /// <summary>Provides direct access to this element's border.</summary>
        public MGComponent<MGBorder> BorderComponent { get; }
        private MGBorder BorderElement { get; }
        public override MGBorder GetBorder() => BorderElement;

        public IBorderBrush BorderBrush
        {
            get => BorderElement.BorderBrush;
            set => BorderElement.BorderBrush = value;
        }

        public Thickness BorderThickness
        {
            get => BorderElement.BorderThickness;
            set => BorderElement.BorderThickness = value;
        }
        #endregion Border

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private double _ZIndex;
        /// <summary>Only relevant if multiple overlays belonging to the same <see cref="MGOverlayHost"/> are currently open.<br/>
        /// In that case, the open overlay with the highest <see cref="ZIndex"/> is shown.</summary>
        public double ZIndex
        {
            get => _ZIndex;
            set
            {
                if (_ZIndex != value)
                {
                    _ZIndex = value;
                    NPC(nameof(ZIndex));
                    OnZIndexChanged?.Invoke(this, ZIndex);
                }
            }
        }

        internal event EventHandler<double> OnZIndexChanged;

        /// <summary>True if this overlay is currently open.<para/>
        /// Note: If multiple overlays belonging to the same <see cref="MGOverlayHost"/> are currently open, only the one with the highest <see cref="ZIndex"/> will be shown.</summary>
        public bool IsOpen
        {
            get => Host.OpenOverlays.Contains(this);
            set
            {
                if (IsOpen && !value)
                    _ = Host.TryClose(this);
                else if (!IsOpen && value)
                    _ = Host.TryOpen(this);
            }
        }

        #region Events
        /// <summary>Invoked just after this <see cref="MGOverlay"/> is opened.<para/>
        /// If multiple overlays belonging to the same <see cref="MGOverlayHost"/> are open at once, only one of them will actually be visible.<br/>
        /// You may instead wish to use <see cref="OnActivated"/>.<para/>
        /// See also:<br/><see cref="OnOpened"/><br/><see cref="OnOpening"/><br/><see cref="OnClosed" /><br/><see cref="OnClosing"/><br/><see cref="OnActivated"/><br/><see cref="OnDeactivated"/></summary>
        public event EventHandler<MGOverlay> OnOpened;
        internal void InvokeOnOpened() => OnOpened?.Invoke(this, this);
        /// <summary>Invoked just before this <see cref="MGOverlay"/> is opened. Cancellable.<para/>
        /// See also:<br/><see cref="OnOpened"/><br/><see cref="OnOpening"/><br/><see cref="OnClosed" /><br/><see cref="OnClosing"/><br/><see cref="OnActivated"/><br/><see cref="OnDeactivated"/></summary>
        public event EventHandler<CancelEventArgs<MGOverlay>> OnOpening;
        internal void InvokeOnOpening(CancelEventArgs<MGOverlay> Args) => OnOpening?.Invoke(this, Args);

        /// <summary>Invoked just after this <see cref="MGOverlay"/> is closed.<para/>
        /// If multiple overlays belonging to the same <see cref="MGOverlayHost"/> were open at once, only one of them will actually be visible.<br/>
        /// You may instead wish to use <see cref="OnDeactivated"/>.<para/>
        /// See also:<br/><see cref="OnOpened"/><br/><see cref="OnOpening"/><br/><see cref="OnClosed" /><br/><see cref="OnClosing"/><br/><see cref="OnActivated"/><br/><see cref="OnDeactivated"/></summary>
        public event EventHandler<MGOverlay> OnClosed;
        internal void InvokeOnClosed() => OnClosed?.Invoke(this, this);
        /// <summary>Invoked just before this <see cref="MGOverlay"/> is closed. Cancellable.<para/>
        /// See also:<br/><see cref="OnOpened"/><br/><see cref="OnOpening"/><br/><see cref="OnClosed" /><br/><see cref="OnClosing"/><br/><see cref="OnActivated"/><br/><see cref="OnDeactivated"/></summary>
        public event EventHandler<CancelEventArgs<MGOverlay>> OnClosing;
        internal void InvokeOnClosing(CancelEventArgs<MGOverlay> Args) => OnClosing?.Invoke(this, Args);

        /// <summary>Invoked just after this <see cref="MGOverlay"/> is set as the visible active overlay.<para/>
        /// See also: <see cref="MGOverlayHost.ActiveOverlay"/></summary>
        public event EventHandler<MGOverlay> OnActivated;
        internal void InvokeOnActivated() => OnActivated?.Invoke(this, this);
        /// <summary>Invoked just after this <see cref="MGOverlay"/> is unset as the visible active overlay.<para/>
        /// See also: <see cref="MGOverlayHost.ActiveOverlay"/></summary>
        public event EventHandler<MGOverlay> OnDeactivated;
        internal void InvokeOnDeactivated() => OnDeactivated?.Invoke(this, this);
        #endregion Events

        public MGComponent<MGButton> CloseButtonComponent { get; }
        /// <summary>Only visible if <see cref="ShowCloseButton"/> is <see langword="true" />.<para/>
        /// By default, this is placed in the top-right corner and shares its consumed space with the overlay content.<br/>
        /// (Meaning this button might be rendered overtop of other overlay content. You may wish to add a top and/or right <see cref="MGElement.Padding"/> to your overlay content to avoid overlaps with the close button).</summary>
        public MGButton CloseButton { get; }

        /// <summary><see langword="true"/> if the <see cref="CloseButton"/> should be displayed in the top-right corner.<para/>
        /// Default value: <see langword="false"/></summary>
        public bool ShowCloseButton
        {
            get => CloseButton.Visibility == Visibility.Visible;
            set => CloseButton.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>To instantiate an <see cref="MGOverlay"/>, use <see cref="MGOverlayHost.AddOverlay(MGElement)"/></summary>
        internal MGOverlay(MGOverlayHost Host, MGElement Content)
            : base(Host.ParentWindow, MGElementType.Overlay)
        {
            using (BeginInitializing())
            {
                this.Host = Host;
                SetContent(Content);
                SetParent(Host);

                HorizontalAlignment = HorizontalAlignment.Center;
                VerticalAlignment = VerticalAlignment.Center;
                Padding = new(5);

                BorderElement = new(Host.ParentWindow, new(1), Color.Black.AsFillBrush());
                BorderComponent = MGComponentBase.Create(BorderElement);
                AddComponent(BorderComponent);
                BorderElement.OnBorderBrushChanged += (sender, e) => { NPC(nameof(BorderBrush)); };
                BorderElement.OnBorderThicknessChanged += (sender, e) => { NPC(nameof(BorderThickness)); };

                CloseButton = new(Host.ParentWindow, x => IsOpen = false);
                CloseButton.MinWidth = 12;
                CloseButton.MinHeight = 12;
                CloseButton.BackgroundBrush = new(Color.Crimson.AsFillBrush() * 0.8f, Color.White * 0.18f, PressedModifierType.Darken, 0.06f);
                CloseButton.BorderBrush = MGUniformBorderBrush.Black;
                CloseButton.BorderThickness = new(1);
                CloseButton.Margin = BorderThickness;
                CloseButton.Padding = new(4, -1);
                CloseButton.VerticalAlignment = VerticalAlignment.Center;
                CloseButton.VerticalContentAlignment = VerticalAlignment.Center;
                CloseButton.HorizontalContentAlignment = HorizontalAlignment.Center;
                CloseButton.SetContent(new MGTextBlock(Host.ParentWindow, "[b][shadow=Black 1 1]x[/shadow][/b]", Color.White));
                CloseButtonComponent = new(CloseButton, ComponentUpdatePriority.BeforeContents, ComponentDrawPriority.AfterContents, true, true, true, true, false, false, false,
                    (AvailableBounds, ComponentSize) => ApplyAlignment(AvailableBounds, HorizontalAlignment.Right, VerticalAlignment.Top, ComponentSize.Size));
                AddComponent(CloseButtonComponent);
                ShowCloseButton = false;
            }
        }
    }
}