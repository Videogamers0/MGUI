using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.Containers;
using MGUI.Core.UI.XAML;
using MGUI.Shared.Helpers;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Thickness = MonoGame.Extended.Thickness;

namespace MGUI.Core.UI
{
    /// <summary>Represents a control that displays hierarchical data in a tree structure with expandable and collapsible nodes.</summary>
    /// <typeparam name="TItemType">The type that the ItemsSource will be bound to.</typeparam>
    public class MGTreeView<TItemType> : MGSingleContentHost
    {
        #region Border
        /// <summary>Provides direct access to this element's border.</summary>
        public MGComponent<MGBorder> BorderComponent { get; }
        /// <summary>Gets the outer border element that wraps the entire tree view.</summary>
        public MGBorder BorderElement { get; }
        public override MGBorder GetBorder() => BorderElement;

        /// <summary>Gets or sets the brush used to paint the border of the tree view.</summary>
        public IBorderBrush BorderBrush
        {
            get => BorderElement.BorderBrush;
            set => BorderElement.BorderBrush = value;
        }

        /// <summary>Gets or sets the thickness of the border around the tree view.</summary>
        public Thickness BorderThickness
        {
            get => BorderElement.BorderThickness;
            set => BorderElement.BorderThickness = value;
        }
        #endregion Border

        private readonly List<MGTreeViewItem<TItemType>> _VisibleItemsCache;

        /// <summary>Gets the scroll viewer that provides scrolling functionality for the tree view.</summary>
        public MGScrollViewer ScrollViewer { get; }

        /// <summary>Gets the panel that contains all root-level tree view items.</summary>
        public MGStackPanel ItemsPanel { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ObservableCollection<MGTreeViewItem<TItemType>> _Items;
        /// <summary>Gets the collection of root-level items in the tree view.</summary>
        public IReadOnlyList<MGTreeViewItem<TItemType>> Items => _Items;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private MGTreeViewItem<TItemType> _SelectedItem;
        /// <summary>Gets or sets the currently selected item in the <see cref="MGTreeView{TItemType}"/>.</summary>
        public MGTreeViewItem<TItemType> SelectedItem
        {
            get => _SelectedItem;
            set
            {
                if (_SelectedItem != value)
                {
                    _SelectedItem = value;
                    NPC(nameof(SelectedItem));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _IndentSize = 20;
        /// <summary>Gets or sets the amount of indentation (in pixels) for each level of the tree hierarchy.</summary>
        public int IndentSize
        {
            get => _IndentSize;
            set
            {
                if (_IndentSize != value)
                {
                    _IndentSize = value;
                    NPC(nameof(IndentSize));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private VisualStateFillBrush _SelectionBackgroundBrush;
        /// <summary>Gets or sets the brush used to paint the background of selected items.</summary>
        public VisualStateFillBrush SelectionBackgroundBrush
        {
            get => _SelectionBackgroundBrush;
            set
            {
                if (_SelectionBackgroundBrush != value)
                {
                    _SelectionBackgroundBrush = value;
                    NPC(nameof(SelectionBackgroundBrush));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Color _SelectionForeground;
        /// <summary>Gets or sets the foreground color used for selected items.</summary>
        public Color SelectionForeground
        {
            get => _SelectionForeground;
            set
            {
                if (_SelectionForeground != value)
                {
                    _SelectionForeground = value;
                    NPC(nameof(SelectionForeground));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IEnumerable _ItemsSource;
        /// <summary>Gets or sets the data source used to generate the tree view items.</summary>
        public IEnumerable ItemsSource
        {
            get => _ItemsSource;
            set
            {
                if (_ItemsSource != value)
                {
                    if (_ItemsSource is System.Collections.Specialized.INotifyCollectionChanged oldCollection)
                        oldCollection.CollectionChanged -= ItemsSource_CollectionChanged;

                    _ItemsSource = value;

                    if (_ItemsSource is System.Collections.Specialized.INotifyCollectionChanged newCollection)
                        newCollection.CollectionChanged += ItemsSource_CollectionChanged;

                    GenerateItemsFromSource();
                    NPC(nameof(ItemsSource));
                }
            }
        }

        /// <summary>Handles changes to the ItemsSource collection.</summary>
        private void ItemsSource_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                    {
                        foreach (var obj in e.NewItems)
                        {
                            if (obj != null)
                            {
                                var item = CreateItemFromData(obj, 0);
                                AddItem(item);
                            }
                        }
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    GenerateItemsFromSource();
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    GenerateItemsFromSource();
                    break;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _ChildrenPropertyName = "Children";
        /// <summary>Gets or sets the name of the property that contains child items in the data source.</summary>
        public string ChildrenPropertyName
        {
            get => _ChildrenPropertyName;
            set
            {
                if (_ChildrenPropertyName != value)
                {
                    _ChildrenPropertyName = value;
                    NPC(nameof(ChildrenPropertyName));
                }
            }
        }

        /// <summary>Invoked when the <see cref="SelectedItem"/> changes.</summary>
        public event EventHandler<MGTreeViewItem<TItemType>> SelectionChanged;

        /// <summary>Invoked when an item is expanded to show its children.</summary>
        public event EventHandler<MGTreeViewItem<TItemType>> ItemExpanded;

        /// <summary>Invoked when an item is collapsed to hide its children.</summary>
        public event EventHandler<MGTreeViewItem<TItemType>> ItemCollapsed;

        /// <summary>Invoked when an item is double-clicked.</summary>
        public event EventHandler<MGTreeViewItem<TItemType>> ItemDoubleClicked;

        /// <summary>Invoked when an item is right-clicked.</summary>
        public event EventHandler<MGTreeViewItem<TItemType>> ItemRightClicked;

        public MGTreeView(MGWindow Window)
            : base(Window, MGElementType.TreeView)
        {
            using (BeginInitializing())
            {
                _Items = new ObservableCollection<MGTreeViewItem<TItemType>>();
                _Items.CollectionChanged += Items_CollectionChanged;

                _VisibleItemsCache = new List<MGTreeViewItem<TItemType>>();

                MGTheme Theme = GetTheme();

                BorderElement = new(Window, Theme.TreeViewBorderThickness, Theme.TreeViewBorderBrush ?? MGUniformBorderBrush.Black);
                BorderComponent = MGComponentBase.Create(BorderElement);
                AddComponent(BorderComponent);
                BorderElement.OnBorderBrushChanged += (sender, e) => { NPC(nameof(BorderBrush)); };
                BorderElement.OnBorderThicknessChanged += (sender, e) => { NPC(nameof(BorderThickness)); };

                //  Create the panel that hosts the root-level tree items
                ItemsPanel = new MGStackPanel(Window, Orientation.Vertical);
                ItemsPanel.CanChangeContent = false;
                ItemsPanel.ManagedParent = this;
                ItemsPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
                ItemsPanel.VerticalAlignment = VerticalAlignment.Top;
                ItemsPanel.Margin = new Thickness(0);
                ItemsPanel.Padding = new Thickness(0);

                //  Create the scroll viewer that wraps the ItemsPanel to allow a vertical scrollbar for the entire tree's content
                ScrollViewer = new MGScrollViewer(Window);
                ScrollViewer.SetContent(ItemsPanel);
                ScrollViewer.CanChangeContent = false;
                ScrollViewer.ManagedParent = this;
                ScrollViewer.HorizontalAlignment = HorizontalAlignment.Stretch;
                ScrollViewer.VerticalAlignment = VerticalAlignment.Stretch;
                ScrollViewer.Margin = new Thickness(0);
                ScrollViewer.Padding = new Thickness(0);

                SetContent(ScrollViewer);
                CanChangeContent = false;

                HorizontalAlignment = HorizontalAlignment.Stretch;
                VerticalAlignment = VerticalAlignment.Stretch;

                SelectionBackgroundBrush = Theme.TreeViewSelectionBackground?.GetValue(true) ?? new VisualStateFillBrush(new MGSolidFillBrush(Color.LightBlue));
                SelectionForeground = Theme.TreeViewSelectionForeground;
                IndentSize = Theme.TreeViewIndentSize;

                if (!BorderThickness.IsEmpty())
                    Padding = new Thickness(3);
            }
        }

        //  This method is invoked via reflection in MGUI.Core.UI.XAML.Controls.TreeView.ApplyDerivedSettings.
        //  Do not modify the method signature.
        internal void LoadSettings(TreeView Settings, bool IncludeContent)
        {
            MGDesktop Desktop = GetDesktop();

            Settings.Border.ApplySettings(this, BorderComponent.Element, false);

            if (Settings.IndentSize.HasValue)
                IndentSize = Settings.IndentSize.Value;

            if (Settings.SelectionBackgroundBrush != null)
                SelectionBackgroundBrush = new VisualStateFillBrush(Settings.SelectionBackgroundBrush.ToFillBrush(Desktop, this));
            if (Settings.SelectionForeground.HasValue)
                SelectionForeground = Settings.SelectionForeground.Value.ToXNAColor();

            //  Add TreeViewItems
            if (IncludeContent)
            {
                foreach (Element Child in Settings.Children.OfType<TreeViewItem>())
                {
                    MGTreeViewItem<TItemType> item = Child.ToElement<MGTreeViewItem<TItemType>>(SelfOrParentWindow, this);
                    AddItem(item);
                }
            }
        }

        private void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            using (ItemsPanel.AllowChangingContentTemporarily())
            {
                switch (e.Action)
                {
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                        if (e.NewItems != null)
                        {
                            foreach (MGTreeViewItem<TItemType> item in e.NewItems)
                            {
                                ItemsPanel.TryAddChild(item);
                            }
                        }
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                        if (e.OldItems != null)
                        {
                            foreach (MGTreeViewItem<TItemType> item in e.OldItems)
                            {
                                ItemsPanel.TryRemoveChild(item);
                            }
                        }
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                        ItemsPanel.TryRemoveAll();
                        break;
                }
            }
            RebuildVisibleItemsCache();
        }

        /// <summary>Rebuilds the cache of visible items in the tree view.</summary>
        internal void RebuildVisibleItemsCache()
        {
            _VisibleItemsCache.Clear();
            foreach (var rootItem in Items)
            {
                _VisibleItemsCache.Add(rootItem);
                if (rootItem.IsExpanded)
                    _VisibleItemsCache.AddRange(rootItem.GetVisibleDescendants());
            }
        }

        /// <summary>Adds a root-level item to the tree view.</summary>
        /// <param name="item">The item to add as a root-level item.</param>
        /// <exception cref="ArgumentNullException">Thrown when item is null.</exception>
        public void AddItem(MGTreeViewItem<TItemType> item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            _Items.Add(item);
            item.ParentItem = null;
            item.Level = 0;
            RegisterItemRecursive(item);
            RebuildVisibleItemsCache();
        }

        /// <summary>Registers the given item and all of its descendants with this tree view (owner assignment, indentation, event hooks).</summary>
        internal void RegisterItemRecursive(MGTreeViewItem<TItemType> item)
        {
            if (item == null)
                return;
            item._OwnerTreeView = this;
            item.UpdateIndentation();
            item.Expanded -= OnItemExpanded;
            item.Expanded += OnItemExpanded;
            item.Collapsed -= OnItemCollapsed;
            item.Collapsed += OnItemCollapsed;
            foreach (var child in item.Items)
                RegisterItemRecursive(child);
        }

        /// <summary>Handles the Expanded event of a tree view item.</summary>
        private void OnItemExpanded(object sender, EventArgs e)
        {
            if (sender is MGTreeViewItem<TItemType> item)
            {
                ItemExpanded?.Invoke(this, item);
                RebuildVisibleItemsCache();
            }
        }

        /// <summary>Handles the Collapsed event of a tree view item.</summary>
        private void OnItemCollapsed(object sender, EventArgs e)
        {
            if (sender is MGTreeViewItem<TItemType> item)
            {
                ItemCollapsed?.Invoke(this, item);
                RebuildVisibleItemsCache();
            }
        }

        /// <summary>Removes a root-level item from the tree view.</summary>
        /// <param name="item">The item to remove.</param>
        public void RemoveItem(MGTreeViewItem<TItemType> item)
        {
            _Items.Remove(item);
            if (item != null)
                item._OwnerTreeView = null;
        }

        /// <summary>Removes all root-level items from the tree view.</summary>
        public void ClearItems()
        {
            foreach (var item in _Items.ToList())
                RemoveItem(item);
        }

        /// <summary>Gets the next visible item after the specified item in the tree view.</summary>
        /// <param name="current">The current item.</param>
        /// <returns>The next visible item, or null if there is no next item.</returns>
        public MGTreeViewItem<TItemType> GetNextVisibleItem(MGTreeViewItem<TItemType> current)
        {
            if (current == null)
                return null;
            int index = _VisibleItemsCache.IndexOf(current);
            if (index == -1)
                return null;
            if (index + 1 < _VisibleItemsCache.Count)
                return _VisibleItemsCache[index + 1];
            return null;
        }

        /// <summary>Gets the previous visible item before the specified item in the tree view.</summary>
        /// <param name="current">The current item.</param>
        /// <returns>The previous visible item, or null if there is no previous item.</returns>
        public MGTreeViewItem<TItemType> GetPreviousVisibleItem(MGTreeViewItem<TItemType> current)
        {
            if (current == null)
                return null;
            int index = _VisibleItemsCache.IndexOf(current);
            if (index == -1)
                return null;
            if (index - 1 >= 0)
                return _VisibleItemsCache[index - 1];
            return null;
        }

        /// <summary>Notifies the tree view that an item has been selected.</summary>
        /// <param name="item">The item that was selected, or null to clear selection.</param>
        internal void NotifyItemSelected(MGTreeViewItem<TItemType> item)
        {
            if (_SelectedItem == item)
                return;
            if (_SelectedItem != null)
                _SelectedItem.SetSelected(false);
            _SelectedItem = item;
            if (_SelectedItem != null)
                _SelectedItem.SetSelected(true);
            SelectionChanged?.Invoke(this, item);
            NPC(nameof(SelectedItem));
        }

        /// <summary>Selects the specified item in the tree view.</summary>
        /// <param name="item">The item to select.</param>
        public void SelectItem(MGTreeViewItem<TItemType> item)
        {
            NotifyItemSelected(item);
            if (item != null)
                ScrollIntoView(item);
        }

        /// <summary>Clears the current selection in the tree view.</summary>
        public void ClearSelection()
        {
            NotifyItemSelected(null);
        }

        /// <summary>Scrolls the tree view to make the specified item visible.</summary>
        /// <param name="item">The item to scroll into view.</param>
        public void ScrollIntoView(MGTreeViewItem<TItemType> item)
        {
            if (item == null)
                return;
            if (!_VisibleItemsCache.Contains(item))
                RebuildVisibleItemsCache();
            if (ScrollViewer.MaxVerticalOffset <= 0)
                return;
            var bounds = item.LayoutBounds;
            float itemTop = bounds.Y;
            float itemBottom = bounds.Bottom;
            float viewportTop = ScrollViewer.VerticalOffset;
            float viewportHeight = ScrollViewer.ContentViewport.Height;
            float viewportBottom = viewportTop + viewportHeight;
            bool invalidBounds = bounds.Height <= 0;
            float newOffset = viewportTop;
            if (!invalidBounds)
            {
                if (itemTop < viewportTop)
                    newOffset = itemTop;
                else if (itemBottom > viewportBottom)
                    newOffset = itemBottom - viewportHeight;
            }
            else
            {
                int index = _VisibleItemsCache.IndexOf(item);
                if (index >= 0)
                {
                    int estimatedHeight = item.ActualHeight > 0 ? item.ActualHeight : (_VisibleItemsCache.FirstOrDefault()?.ActualHeight ?? 24);
                    float estimatedTop = index * estimatedHeight;
                    float estimatedBottom = estimatedTop + estimatedHeight;
                    if (estimatedTop < viewportTop)
                        newOffset = estimatedTop;
                    else if (estimatedBottom > viewportBottom)
                        newOffset = estimatedBottom - viewportHeight;
                }
            }
            if (newOffset < 0) newOffset = 0;
            if (newOffset > ScrollViewer.MaxVerticalOffset) newOffset = ScrollViewer.MaxVerticalOffset;
            if (Math.Abs(newOffset - ScrollViewer.VerticalOffset) > 0.5f)
                ScrollViewer.VerticalOffset = newOffset;
        }

        /// <summary>Generates tree view items from the ItemsSource data.</summary>
        private void GenerateItemsFromSource()
        {
            ClearItems();
            if (ItemsSource == null)
                return;
            foreach (var obj in ItemsSource)
            {
                if (obj != null)
                {
                    var item = CreateItemFromData(obj, 0);
                    AddItem(item);
                }
            }
        }

        /// <summary>Creates a tree view item from a data object, recursively creating child items.</summary>
        /// <param name="data">The data object to create an item from.</param>
        /// <param name="level">The depth level of this item in the tree hierarchy.</param>
        /// <returns>A new <see cref="MGTreeViewItem{TDataType}"/> populated with the data.</returns>
        private MGTreeViewItem<TItemType> CreateItemFromData(object data, int level)
        {
            var item = new MGTreeViewItem<TItemType>(SelfOrParentWindow) { DataContext = data, Level = level, Header = data };
            var dataType = data.GetType();
            var childrenProperty = dataType.GetProperty(ChildrenPropertyName);
            if (childrenProperty != null)
            {
                var childrenValue = childrenProperty.GetValue(data);
                if (childrenValue is IEnumerable children)
                {
                    foreach (var child in children)
                    {
                        if (child != null)
                        {
                            var childItem = CreateItemFromData(child, level + 1);
                            item.AddItem(childItem);
                        }
                    }
                }
            }

            return item;
        }

        /// <summary>Triggers the ItemDoubleClicked event.</summary>
        internal void RaiseItemDoubleClicked(MGTreeViewItem<TItemType> item)
        {
            ItemDoubleClicked?.Invoke(this, item);
        }

        /// <summary>Triggers the ItemRightClicked event.</summary>
        internal void RaiseItemRightClicked(MGTreeViewItem<TItemType> item)
        {
            ItemRightClicked?.Invoke(this, item);
        }
    }
}
