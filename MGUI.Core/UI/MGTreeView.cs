using Microsoft.Xna.Framework;
using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.Containers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace MGUI.Core.UI
{
    /// <summary>Represents a control that displays hierarchical data in a tree structure with expandable and collapsible nodes.</summary>
    public class MGTreeView : MGSingleContentHost
    {
        private ObservableCollection<MGTreeViewItem> _Items;
        private MGTreeViewItem _SelectedItem;
        private List<MGTreeViewItem> _VisibleItemsCache;
        private int _IndentSize = 20;
        private VisualStateFillBrush _SelectionBackgroundBrush;
        private Color _SelectionForeground;
        private System.Collections.IEnumerable _ItemsSource;
        private string _ChildrenPropertyName = "Children";

        /// <summary>Gets the outer border element that wraps the entire tree view.</summary>
        public MGBorder OuterBorder { get; private set; }

        /// <summary>Gets the component that manages the outer border.</summary>
        private MGComponentBase BorderComponent { get; set; }

        /// <summary>Gets the scroll viewer that provides scrolling functionality for the tree view.</summary>
        public MGScrollViewer ScrollViewer { get; private set; }

        /// <summary>Gets the panel that contains all root-level tree view items.</summary>
        public MGStackPanel ItemsPanel { get; private set; }

        /// <summary>Gets the collection of root-level items in the tree view.</summary>
        public IReadOnlyList<MGTreeViewItem> Items => _Items;

        /// <summary>Gets or sets the currently selected item in the tree view.</summary>
        public MGTreeViewItem SelectedItem
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

        /// <summary>Gets or sets the brush used to paint the border of the tree view.</summary>
        public IBorderBrush BorderBrush
        {
            get => OuterBorder.BorderBrush;
            set => OuterBorder.BorderBrush = value;
        }

        /// <summary>Gets or sets the thickness of the border around the tree view.</summary>
        public MonoGame.Extended.Thickness BorderThickness
        {
            get => OuterBorder.BorderThickness;
            set => OuterBorder.BorderThickness = value;
        }

        /// <summary>Gets or sets the data source used to generate the tree view items.</summary>
        public System.Collections.IEnumerable ItemsSource
        {
            get => _ItemsSource;
            set
            {
                if (_ItemsSource != value)
                {
                    // Unsubscribe from old collection if it implements INotifyCollectionChanged
                    if (_ItemsSource is System.Collections.Specialized.INotifyCollectionChanged oldCollection)
                    {
                        oldCollection.CollectionChanged -= ItemsSource_CollectionChanged;
                    }

                    _ItemsSource = value;

                    // Subscribe to new collection if it implements INotifyCollectionChanged
                    if (_ItemsSource is System.Collections.Specialized.INotifyCollectionChanged newCollection)
                    {
                        newCollection.CollectionChanged += ItemsSource_CollectionChanged;
                    }

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
                    // Add new items
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
                    // Remove items - for simplicity, regenerate all items
                    GenerateItemsFromSource();
                    break;

                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    // Regenerate all items
                    GenerateItemsFromSource();
                    break;
            }
        }

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

        /// <summary>Occurs when the selected item changes.</summary>
        public event EventHandler<MGTreeViewItem> SelectionChanged;

        /// <summary>Occurs when an item is expanded to show its children.</summary>
        public event EventHandler<MGTreeViewItem> ItemExpanded;

        /// <summary>Occurs when an item is collapsed to hide its children.</summary>
        public event EventHandler<MGTreeViewItem> ItemCollapsed;

        /// <summary>Occurs when an item is double-clicked.</summary>
        public event EventHandler<MGTreeViewItem> ItemDoubleClicked;

        /// <summary>Occurs when an item is right-clicked.</summary>
        public event EventHandler<MGTreeViewItem> ItemRightClicked;

        public MGTreeView(MGWindow Window)
            : base(Window, MGElementType.TreeView)
        {
            using (BeginInitializing())
            {
                // Initialize _Items collection
                _Items = new ObservableCollection<MGTreeViewItem>();
                _Items.CollectionChanged += Items_CollectionChanged;

                // Initialize _VisibleItemsCache
                _VisibleItemsCache = new List<MGTreeViewItem>();

                // Create OuterBorder (MGBorder) and store as property
                OuterBorder = new MGBorder(Window);

                // Create BorderComponent (MGComponent<MGBorder>)
                BorderComponent = MGComponentBase.Create(OuterBorder);

                // Call AddComponent(BorderComponent)
                AddComponent(BorderComponent);

                // Create ScrollViewer (MGScrollViewer) and store as property
                ScrollViewer = new MGScrollViewer(Window);

                // Create ItemsPanel (MGStackPanel vertical) and store as property
                ItemsPanel = new MGStackPanel(Window, Orientation.Vertical);

                // Define ItemsPanel.CanChangeContent = false
                ItemsPanel.CanChangeContent = false;

                // Assemble the visual hierarchy
                ScrollViewer.SetContent(ItemsPanel);
                OuterBorder.SetContent(ScrollViewer);
                SetContent(OuterBorder);

                // Apply default styles
                ApplyDefaultStyles();

                // Ensure the TreeView occupies all available space by default
                HorizontalAlignment = HorizontalAlignment.Stretch;
                VerticalAlignment = VerticalAlignment.Stretch;
                OuterBorder.HorizontalAlignment = HorizontalAlignment.Stretch;
                OuterBorder.VerticalAlignment = VerticalAlignment.Stretch;
                ScrollViewer.HorizontalAlignment = HorizontalAlignment.Stretch;
                ScrollViewer.VerticalAlignment = VerticalAlignment.Stretch;
                ItemsPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
                ItemsPanel.VerticalAlignment = VerticalAlignment.Top; // Contents grow downward

                // Remove any default margin/padding that could cause horizontal offset
                OuterBorder.Margin = new MonoGame.Extended.Thickness(0);
                OuterBorder.Padding = new MonoGame.Extended.Thickness(0);
                ScrollViewer.Margin = new MonoGame.Extended.Thickness(0);
                ScrollViewer.Padding = new MonoGame.Extended.Thickness(0);
                ItemsPanel.Margin = new MonoGame.Extended.Thickness(0);
                ItemsPanel.Padding = new MonoGame.Extended.Thickness(0);
            }
        }

        /// <summary>Applies the default visual styles to the tree view.</summary>
        private void ApplyDefaultStyles()
        {
            // Get the theme
            var theme = GetTheme();

            // Set default border brush from theme or fallback to black
            BorderBrush = theme?.TreeViewBorderBrush ?? MGUniformBorderBrush.Black;

            // Set default border thickness from theme or fallback to 1 pixel
            BorderThickness = theme?.TreeViewBorderThickness ?? new MonoGame.Extended.Thickness(1);

            // Set default selection background from theme or fallback to light blue
            SelectionBackgroundBrush = theme?.TreeViewSelectionBackground?.GetValue(true) ?? new VisualStateFillBrush(new MGSolidFillBrush(Color.LightBlue));

            // Set default selection foreground from theme
            if (theme != null)
                SelectionForeground = theme.TreeViewSelectionForeground;

            // Set default indent size from theme
            if (theme != null)
                IndentSize = theme.TreeViewIndentSize;
        }

        private void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            using (ItemsPanel.AllowChangingContentTemporarily())
            {
                switch (e.Action)
                {
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                        // Add new items to ItemsPanel
                        if (e.NewItems != null)
                        {
                            foreach (MGTreeViewItem item in e.NewItems)
                            {
                                ItemsPanel.TryAddChild(item);
                            }
                        }
                        break;

                    case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                        // Remove items from ItemsPanel
                        if (e.OldItems != null)
                        {
                            foreach (MGTreeViewItem item in e.OldItems)
                            {
                                ItemsPanel.TryRemoveChild(item);
                            }
                        }
                        break;

                    case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                        // Clear all items from ItemsPanel
                        ItemsPanel.TryRemoveAll();
                        break;
                }
            }

            // Rebuild visible items cache after any collection change
            RebuildVisibleItemsCache();
        }

        /// <summary>Rebuilds the cache of visible items in the tree view.</summary>
        internal void RebuildVisibleItemsCache()
        {
            // Clear the cache
            _VisibleItemsCache.Clear();

            // For each root item in Items
            foreach (var rootItem in Items)
            {
                // Add the root item to the cache
                _VisibleItemsCache.Add(rootItem);

                // If the root item is expanded, add all its visible descendants
                if (rootItem.IsExpanded)
                {
                    _VisibleItemsCache.AddRange(rootItem.GetVisibleDescendants());
                }
            }
        }

        /// <summary>Adds a root-level item to the tree view.</summary>
        /// <param name="item">The item to add as a root-level item.</param>
        /// <exception cref="ArgumentNullException">Thrown when item is null.</exception>
        public void AddItem(MGTreeViewItem item)
        {
            // Validate: item != null (ArgumentNullException)
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            // Add item to _Items
            _Items.Add(item);

            // Initialize root item meta
            item.ParentItem = null;
            item.Level = 0;

            // Register this item (and all descendants) with the tree view
            RegisterItemRecursive(item);

            // After adding a new root item, rebuild visible cache
            RebuildVisibleItemsCache();
        }

        /// <summary>Registers the given item and all of its descendants with this tree view (owner assignment, indentation, event hooks).</summary>
        internal void RegisterItemRecursive(MGTreeViewItem item)
        {
            if (item == null)
                return;

            // Assign owner
            item._OwnerTreeView = this;
            item.UpdateIndentation();

            // Ensure events are hooked exactly once
            item.Expanded -= OnItemExpanded;
            item.Expanded += OnItemExpanded;
            item.Collapsed -= OnItemCollapsed;
            item.Collapsed += OnItemCollapsed;

            // Recurse
            foreach (var child in item.Items)
                RegisterItemRecursive(child);
        }

        /// <summary>Handles the Expanded event of a tree view item.</summary>
        private void OnItemExpanded(object sender, EventArgs e)
        {
            if (sender is MGTreeViewItem item)
            {
                // Trigger ItemExpanded event
                ItemExpanded?.Invoke(this, item);

                // Rebuild visible items cache after expansion
                RebuildVisibleItemsCache();
            }
        }

        /// <summary>Handles the Collapsed event of a tree view item.</summary>
        private void OnItemCollapsed(object sender, EventArgs e)
        {
            if (sender is MGTreeViewItem item)
            {
                // Trigger ItemCollapsed event
                ItemCollapsed?.Invoke(this, item);

                // Rebuild visible items cache after collapse
                RebuildVisibleItemsCache();
            }
        }

        /// <summary>Removes a root-level item from the tree view.</summary>
        /// <param name="item">The item to remove.</param>
        public void RemoveItem(MGTreeViewItem item)
        {
            // Remove item from _Items collection
            _Items.Remove(item);

            // Set item.OwnerTreeView = null
            if (item != null)
            {
                item._OwnerTreeView = null;
            }
        }

        /// <summary>Removes all root-level items from the tree view.</summary>
        public void ClearItems()
        {
            // For each item in _Items.ToList(), call RemoveItem(item)
            foreach (var item in _Items.ToList())
            {
                RemoveItem(item);
            }
        }

        /// <summary>Gets the next visible item after the specified item in the tree view.</summary>
        /// <param name="current">The current item.</param>
        /// <returns>The next visible item, or null if there is no next item.</returns>
        public MGTreeViewItem GetNextVisibleItem(MGTreeViewItem current)
        {
            if (current == null)
                return null;

            // Find the index of current in _VisibleItemsCache
            int index = _VisibleItemsCache.IndexOf(current);

            // If not found, return null
            if (index == -1)
                return null;

            // Return the item at index + 1 if available, otherwise null
            if (index + 1 < _VisibleItemsCache.Count)
                return _VisibleItemsCache[index + 1];

            return null;
        }

        /// <summary>Gets the previous visible item before the specified item in the tree view.</summary>
        /// <param name="current">The current item.</param>
        /// <returns>The previous visible item, or null if there is no previous item.</returns>
        public MGTreeViewItem GetPreviousVisibleItem(MGTreeViewItem current)
        {
            if (current == null)
                return null;

            // Find the index of current in _VisibleItemsCache
            int index = _VisibleItemsCache.IndexOf(current);

            // If not found, return null
            if (index == -1)
                return null;

            // Return the item at index - 1 if available, otherwise null
            if (index - 1 >= 0)
                return _VisibleItemsCache[index - 1];

            return null;
        }

        /// <summary>Notifies the tree view that an item has been selected.</summary>
        /// <param name="item">The item that was selected, or null to clear selection.</param>
        internal void NotifyItemSelected(MGTreeViewItem item)
        {
            // If _SelectedItem == item, return early (no change)
            if (_SelectedItem == item)
                return;

            // Store oldSelection = _SelectedItem
            var oldSelection = _SelectedItem;

            // If _SelectedItem != null, call _SelectedItem.SetSelected(false)
            if (_SelectedItem != null)
            {
                _SelectedItem.SetSelected(false);
            }

            // Define _SelectedItem = item
            _SelectedItem = item;

            // If _SelectedItem != null, call _SelectedItem.SetSelected(true)
            if (_SelectedItem != null)
            {
                _SelectedItem.SetSelected(true);
            }

            // Trigger SelectionChanged with oldSelection and item
            SelectionChanged?.Invoke(this, item);

            // Call NPC(nameof(SelectedItem))
            NPC(nameof(SelectedItem));
        }

        /// <summary>Selects the specified item in the tree view.</summary>
        /// <param name="item">The item to select.</param>
        public void SelectItem(MGTreeViewItem item)
        {
            // Call NotifyItemSelected(item)
            NotifyItemSelected(item);

            // Scroll the item into view if it's not null
            if (item != null)
            {
                ScrollIntoView(item);
            }
        }

        /// <summary>Clears the current selection in the tree view.</summary>
        public void ClearSelection()
        {
            // Call NotifyItemSelected(null)
            NotifyItemSelected(null);
        }

        /// <summary>Scrolls the tree view to make the specified item visible.</summary>
        /// <param name="item">The item to scroll into view.</param>
        public void ScrollIntoView(MGTreeViewItem item)
        {
            // If item == null, return
            if (item == null)
                return;

            // Ensure cache reflects current visibility state
            // (may be needed if selection occurs immediately after expansion change)
            if (!_VisibleItemsCache.Contains(item))
                RebuildVisibleItemsCache();

            // If no scrolling possible, return
            if (ScrollViewer.MaxVerticalOffset <= 0)
                return;

            // Primary method uses actual layout bounds
            var bounds = item.LayoutBounds;
            float itemTop = bounds.Y;
            float itemBottom = bounds.Bottom;
            float viewportTop = ScrollViewer.VerticalOffset;
            float viewportHeight = ScrollViewer.ContentViewport.Height;
            float viewportBottom = viewportTop + viewportHeight;

            // If layout bounds are invalid (can happen if selection invoked same tick before layout update)
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
                // Fallback: use index-based estimation
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
            // Clear existing items
            ClearItems();

            // If ItemsSource is null, return
            if (ItemsSource == null)
                return;

            // For each object in ItemsSource, create a tree view item
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
        /// <returns>A new MGTreeViewItem populated with the data.</returns>
        private MGTreeViewItem CreateItemFromData(object data, int level)
        {
            // Create a new MGTreeViewItem
            var item = new MGTreeViewItem(SelfOrParentWindow);

            // Set item.DataContext = data
            item.DataContext = data;

            // Set item.Level = level
            item.Level = level;

            // Set item.Header = data (will be displayed as string)
            item.Header = data;

            // Use reflection to get the children property
            try
            {
                var dataType = data.GetType();
                var childrenProperty = dataType.GetProperty(ChildrenPropertyName);

                if (childrenProperty != null)
                {
                    var childrenValue = childrenProperty.GetValue(data);

                    if (childrenValue is System.Collections.IEnumerable children)
                    {
                        // Recursively create child items
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
            }
            catch (Exception ex)
            {
                // Log error but continue without children
                Debug.WriteLine($"Error accessing children property '{ChildrenPropertyName}': {ex.Message}");
            }

            return item;
        }

        /// <summary>Triggers the ItemDoubleClicked event.</summary>
        internal void RaiseItemDoubleClicked(MGTreeViewItem item)
        {
            ItemDoubleClicked?.Invoke(this, item);
        }

        /// <summary>Triggers the ItemRightClicked event.</summary>
        internal void RaiseItemRightClicked(MGTreeViewItem item)
        {
            ItemRightClicked?.Invoke(this, item);
        }
    }
}
