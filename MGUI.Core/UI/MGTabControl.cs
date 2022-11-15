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
using MGUI.Core.UI.Containers;
using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using System.Diagnostics;

namespace MGUI.Core.UI
{
    public class MGTabControl : MGSingleContentHost
    {
        protected override void SetContentVirtual(MGElement Value)
        {
            if (_Content != Value)
            {
                if (!CanChangeContent)
                    throw new InvalidOperationException($"Cannot set {nameof(MGSingleContentHost)}.{nameof(Content)} while {nameof(CanChangeContent)} is false.");

                //_Content?.SetParent(null);
                //InvokeContentRemoved(_Content);
                _Content = Value;
                //_Content?.SetParent(this);
                LayoutChanged(this, true);
                //InvokeContentAdded(_Content);
            }
        }

        public override IEnumerable<MGElement> GetVisualTreeChildren() => _Tabs;

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

        #region Tab Headers
        /// <summary>Provides direct access to to the stackpanel component that the tab headers are placed in at the top of this tabcontrol.</summary>
        public MGComponent<MGStackPanel> HeadersPanelComponent { get; }
        private MGStackPanel HeadersPanelElement { get; }

        /// <summary>The background brush of the entire header region of this <see cref="MGTabControl"/>. This is rendered behind the tab headers.<para/>
        /// To change the background of a specific tab, consider setting the <see cref="UnselectedTabHeaderTemplate"/> and <see cref="SelectedTabHeaderTemplate"/>.</summary>
        public VisualStateFillBrush HeaderAreaBackground
        {
            get => HeadersPanelElement.BackgroundBrush;
            set => HeadersPanelElement.BackgroundBrush = value;
        }

        private void ManagedAddHeadersPanelChild(MGSingleContentHost NewItem)
        {
            using (HeadersPanelElement.AllowChangingContentTemporarily())
            {
                HeadersPanelElement.TryAddChild(NewItem);
            }
        }

        private bool ManagedReplaceHeadersPanelChild(MGSingleContentHost OldItem, MGSingleContentHost NewItem)
        {
            using (HeadersPanelElement.AllowChangingContentTemporarily())
            {
                return HeadersPanelElement.TryReplaceChild(OldItem, NewItem);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Func<MGTabItem, MGButton> _UnselectedTabHeaderTemplate;
        /// <summary>Creates the wrapper element that hosts the given <see cref="MGTabItem"/>'s <see cref="MGTabItem.Header"/> for tabs that aren't selected.<para/>
        /// See also: <see cref="SelectedTabHeaderTemplate"/></summary>
        public Func<MGTabItem, MGButton> UnselectedTabHeaderTemplate
        {
            get => _UnselectedTabHeaderTemplate;
            set
            {
                if (_UnselectedTabHeaderTemplate != value)
                {
                    _UnselectedTabHeaderTemplate = value;
                    foreach (KeyValuePair<MGTabItem, MGButton> KVP in ActualTabHeaders.ToList())
                    {
                        MGTabItem Tab = KVP.Key;
                        if (!Tab.IsTabSelected)
                            UpdateHeaderWrapper(Tab);
                    }
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Func<MGTabItem, MGButton> _SelectedTabHeaderTemplate;
        /// <summary>Creates the wrapper element that hosts the given <see cref="MGTabItem"/>'s <see cref="MGTabItem.Header"/> for the selected tab.<para/>
        /// See also: <see cref="UnselectedTabHeaderTemplate"/></summary>
        public Func<MGTabItem, MGButton> SelectedTabHeaderTemplate
        {
            get => _SelectedTabHeaderTemplate;
            set
            {
                if (_SelectedTabHeaderTemplate != value)
                {
                    _SelectedTabHeaderTemplate = value;
                    foreach (KeyValuePair<MGTabItem, MGButton> KVP in ActualTabHeaders.ToList())
                    {
                        MGTabItem Tab = KVP.Key;
                        if (Tab.IsTabSelected)
                            UpdateHeaderWrapper(Tab);
                    }
                }
            }
        }

        private void UpdateHeaderWrapper(MGTabItem Tab)
        {
            if (Tab != null && ActualTabHeaders.TryGetValue(Tab, out MGButton OldHeaderWrapper))
            {
                MGButton NewHeaderWrapper = Tab.IsTabSelected ? SelectedTabHeaderTemplate(Tab) : UnselectedTabHeaderTemplate(Tab);
                if (ManagedReplaceHeadersPanelChild(OldHeaderWrapper, NewHeaderWrapper))
                {
                    OldHeaderWrapper.SetContent(null as MGElement);
                    NewHeaderWrapper.SetContent(Tab.Header);
                    ActualTabHeaders[Tab] = NewHeaderWrapper;
                }
            }
        }

        private Dictionary<MGTabItem, MGButton> ActualTabHeaders { get; }
        #endregion Tab Headers

        #region Tabs
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ObservableCollection<MGTabItem> _Tabs { get; }
        public IReadOnlyList<MGTabItem> Tabs => _Tabs;

        public void RemoveTab(MGTabItem Tab)
        {
            if (_Tabs.Contains(Tab))
            {
                if (Tab.IsTabSelected && _Tabs.Count > 1)
                    _Tabs.First(x => x != Tab).IsTabSelected = true;

                _Tabs.Remove(Tab);
                InvokeContentRemoved(Tab);
            }
        }

        public MGTabItem AddTab(string TabHeader, MGElement TabContent)
            => AddTab(new MGTextBlock(ParentWindow, TabHeader), TabContent);

        public MGTabItem AddTab(MGElement TabHeader, MGElement TabContent)
        {
            MGTabItem Tab = new(this, TabHeader, TabContent);

            MGButton HeaderWrapper = UnselectedTabHeaderTemplate(Tab);
            HeaderWrapper.SetContent(TabHeader);
            ActualTabHeaders.Add(Tab, HeaderWrapper);

            _Tabs.Add(Tab);
            InvokeContentAdded(Tab);

            ManagedAddHeadersPanelChild(HeaderWrapper);

            if (SelectedTab == null)
                _ = TrySelectTab(Tab);

            return Tab;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private MGTabItem _SelectedTab;
        public MGTabItem SelectedTab { get => _SelectedTab; }
        public int SelectedTabIndex => _SelectedTab == null ? -1 : _Tabs.IndexOf(_SelectedTab);

        /// <summary>Invoked just before <see cref="SelectedTab"/> changes. Argument value is the new tab being selected. This event allows cancellation.</summary>
        public event EventHandler<CancelEventArgs<MGTabItem>> SelectedTabChanging;
        /// <summary>Invoked when <see cref="SelectedTab"/> changes to a different <see cref="MGTabItem"/></summary>
        public event EventHandler<EventArgs<MGTabItem>> SelectedTabChanged;

        /// <summary>Attempts to set the given <paramref name="Tab"/> as the <see cref="SelectedTab"/>.<para/>
        /// To deselect a tab, use <see cref="TryDeselectTab(MGTabItem, bool)"/> rather than a null <paramref name="Tab"/> parameter.</summary>
        /// <param name="Tab">Cannot be null, and should be a tab that belongs to this <see cref="MGTabControl"/> (I.E. it was created via <see cref="AddTab(MGElement, MGElement)"/>)</param>
        /// <returns>False if unable to select the given <paramref name="Tab"/>, such as if the value was null, or it belongs to a different <see cref="MGTabControl"/>, or the action was cancelled by <see cref="SelectedTabChanging"/> event.</returns>
        public bool TrySelectTab(MGTabItem Tab)
        {
            if (Tab != null && _Tabs.Contains(Tab) && Tab.TabControl == this && Tab != SelectedTab)
            {
                if (SelectedTabChanging != null)
                {
                    CancelEventArgs<MGTabItem> CancelArgs = new(Tab);
                    SelectedTabChanging.Invoke(this, CancelArgs);
                    if (CancelArgs.Cancel)
                        return false;
                }

                MGTabItem Previous = SelectedTab;
                _SelectedTab = Tab;

                UpdateHeaderWrapper(Previous);
                UpdateHeaderWrapper(SelectedTab);

                SetContent(SelectedTab);
                SelectedTabChanged?.Invoke(this, new(Previous, SelectedTab));
                return true;
            }
            else
                return false;
        }

        public bool TrySelectTabAtIndex(int Index)
        {
            if (Index >= 0 && Index < _Tabs.Count)
                return TrySelectTab(_Tabs[Index]);
            else
                return false;
        }

        /// <summary>Attempts to deselect the given <paramref name="Tab"/>. Does nothing if the <paramref name="Tab"/> is not already selected or if there are no other tabs to select in place of it.</summary>
        /// <param name="Tab">The tab to deselect.</param>
        /// <param name="FocusTabToRight">If true, will attempt to select the tab to the right of the tab being deselected.<br/>
        /// If false, will attempt to select the tab to the left of the tab being deselected.</param>
        public bool TryDeselectTab(MGTabItem Tab, bool FocusTabToRight)
        {
            if (Tab == null || Tab != SelectedTab || _Tabs.Count <= 1)
                return false;

            int TabIndex = _Tabs.IndexOf(Tab);
            if (TabIndex < 0)
                return false;

            int DesiredIndex = FocusTabToRight ? TabIndex + 1 : TabIndex - 1;
            int ActualIndex = (DesiredIndex + _Tabs.Count) % _Tabs.Count;
            return TrySelectTabAtIndex(ActualIndex);
        }
        #endregion Tabs

        public MGTabControl(MGWindow Window)
            : base(Window, MGElementType.TabControl)
        {
            using (BeginInitializing())
            {
                this.HeadersPanelElement = new(Window, Orientation.Horizontal);
                this.HeadersPanelElement.CanChangeContent = false;
                this.HeadersPanelElement.Spacing = 0;
                this.HeadersPanelElement.VerticalAlignment = VerticalAlignment.Bottom;
                this.HeadersPanelElement.HorizontalContentAlignment = HorizontalAlignment.Left;
                this.HeadersPanelComponent = new(HeadersPanelElement, ComponentUpdatePriority.BeforeContents, ComponentDrawPriority.AfterContents,
                    true, false, true, true, false, false, false,
                    (AvailableBounds, ComponentSize) => ApplyAlignment(AvailableBounds, HorizontalAlignment.Stretch, VerticalAlignment.Top, ComponentSize.Size));
                AddComponent(HeadersPanelComponent);

                this.BorderElement = new(Window);
                this.BorderComponent = MGComponentBase.Create(BorderElement);
                AddComponent(BorderComponent);

                this.Padding = new(12);

                this.ActualTabHeaders = new();

                this._Tabs = new();
                _Tabs.CollectionChanged += (sender, e) =>
                {
                    if (e.Action is NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Replace)
                    {
                        if (e.NewItems != null)
                        {
                            foreach (MGTabItem Item in e.NewItems)
                            {
                                Item.HeaderChanged += Tab_HeaderChanged;
                            }
                        }
                    }

                    if (e.Action is NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Replace or NotifyCollectionChangedAction.Reset)
                    {
                        if (e.OldItems != null)
                        {
                            foreach (MGTabItem Item in e.OldItems)
                            {
                                Item.HeaderChanged -= Tab_HeaderChanged;
                            }
                        }
                    }
                };

                this.SelectedTabHeaderTemplate = (MGTabItem TabItem) =>
                {
                    MGButton Button = new(Window, new(1, 1, 1, 0), MGUniformBorderBrush.Black, x => TabItem.IsTabSelected = true);
                    Button.Padding = new(8, 5, 8, 5);
                    Button.BackgroundBrush = GetTheme().SelectedTabHeaderBackground.GetValue(true);
                    //Button.DefaultTextForeground.SetAll(Color.Black);
                    Button.VerticalAlignment = VerticalAlignment.Bottom;
                    return Button;
                };

                this.UnselectedTabHeaderTemplate = (MGTabItem TabItem) =>
                {
                    MGButton Button = new(Window, new(1, 1, 1, 0), MGUniformBorderBrush.Gray, x => TabItem.IsTabSelected = true);
                    Button.Padding = new(8, 3, 8, 3);
                    Button.BackgroundBrush = GetTheme().UnselectedTabHeaderBackground.GetValue(true);
                    //Button.DefaultTextForeground.SetAll(Color.Black);
                    Button.Opacity = 0.9f;
                    Button.VerticalAlignment = VerticalAlignment.Bottom;
                    return Button;
                };
            }
        }

        private void Tab_HeaderChanged(object sender, EventArgs<MGElement> e)
        {
            MGTabItem TabItem = sender as MGTabItem;
            this.ActualTabHeaders[TabItem].SetContent(e.NewValue);
        }

        protected override void UpdateContents(ElementUpdateArgs UA)
        {
            foreach (MGElement Child in GetVisualTreeChildren().ToList())
            {
                if (Child == SelectedTab || !_Tabs.Contains(Child))
                    Child.Update(UA);
                else
                    Child.Update(UA with { IsHitTestVisible = false });
            }
        }

        public override void DrawBackground(ElementDrawArgs DA, Rectangle LayoutBounds)
        {
            //  The background only spans the content region of this TabControl,
            //  Does not fill the region with the tab headers
            Rectangle TabHeadersBounds = HeadersPanelElement.LayoutBounds;
            Rectangle TabContentBounds = new(LayoutBounds.Left, TabHeadersBounds.Bottom, LayoutBounds.Width, LayoutBounds.Height - TabHeadersBounds.Height);
            base.DrawBackground(DA, TabContentBounds);
        }

        public override void DrawSelf(ElementDrawArgs DA, Rectangle LayoutBounds) { }
    }
}
