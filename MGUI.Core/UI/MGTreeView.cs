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
    /// <summary>Represents an individual node in a <see cref="MGTreeView"/> that can contain child nodes.</summary>
    public class MGTreeViewItem : MGSingleContentHost
    {
        private object _Header;
        private int _Level;
        private MGTreeViewItem _ParentItem;
        private bool _IsExpanded;
        private bool _IsSelected;
        private ObservableCollection<MGTreeViewItem> _Items;
        internal object _OwnerTreeView; // Will be cast to MGTreeView later
        private object _DataContext;
        private object _HeaderTemplate; // Placeholder for future template support

        /// <summary>Gets the visual element that displays the header content.</summary>
        public MGElement HeaderContent { get; private set; }

        /// <summary>Gets or sets the content to display in the header of this tree view item.</summary>
        public object Header
        {
            get => _Header;
            set
            {
                if (_Header != value)
                {
                    _Header = value;
                    UpdateHeaderContent();
                    NPC(nameof(Header));
                }
            }
        }

        /// <summary>Updates the HeaderContent based on the current Header value.</summary>
        private void UpdateHeaderContent()
        {
            // Create new HeaderContent based on Header type
            if (_Header is string text)
            {
                // If Header is string, create MGTextBlock with the text
                HeaderContent = new MGTextBlock(SelfOrParentWindow, text);
            }
            else if (_Header is MGElement element)
            {
                // If Header is MGElement, use it directly
                HeaderContent = element;
            }
            else if (_Header != null)
            {
                // Otherwise, create MGTextBlock with Header?.ToString()
                HeaderContent = new MGTextBlock(SelfOrParentWindow, _Header.ToString());
            }
            else
            {
                // If Header is null, create empty MGTextBlock
                HeaderContent = new MGTextBlock(SelfOrParentWindow, string.Empty);
            }

            // Set HeaderContent to HeaderContainer
            using (HeaderContainer.AllowChangingContentTemporarily())
            {
                HeaderContainer.SetContent(HeaderContent);
            }
        }

        /// <summary>Gets or sets the depth level of this item in the tree hierarchy (0 for root items).</summary>
        public int Level
        {
            get => _Level;
            set
            {
                if (_Level != value)
                {
                    _Level = value;
                    NPC(nameof(Level));
                }
            }
        }

        /// <summary>Gets or sets the parent item of this tree view item. Null if this is a root item.</summary>
        public MGTreeViewItem ParentItem
        {
            get => _ParentItem;
            internal set => _ParentItem = value;
        }

        /// <summary>Gets or sets whether this item is expanded to show its children.</summary>
        public bool IsExpanded
        {
            get => _IsExpanded;
            set
            {
                if (_IsExpanded != value)
                {
                    _IsExpanded = value;
                    NPC(nameof(IsExpanded));
                }
            }
        }

        /// <summary>Gets or sets whether this item is currently selected.</summary>
        public new bool IsSelected
        {
            get => _IsSelected;
            internal set
            {
                if (_IsSelected != value)
                {
                    _IsSelected = value;
                    NPC(nameof(IsSelected));
                }
            }
        }

        /// <summary>Gets the collection of child items.</summary>
        public IReadOnlyList<MGTreeViewItem> Items => _Items;

        /// <summary>Gets whether this item has any child items.</summary>
        public bool HasItems => Items?.Count > 0;

        /// <summary>Gets the <see cref="MGTreeView"/> that owns this item.</summary>
        public MGTreeView OwnerTreeView => _OwnerTreeView as MGTreeView;

        /// <summary>Gets or sets the data context for this tree view item (used for data binding).</summary>
        public new object DataContext
        {
            get => _DataContext;
            set
            {
                if (_DataContext != value)
                {
                    _DataContext = value;
                    NPC(nameof(DataContext));
                }
            }
        }

        /// <summary>Gets or sets the template used to display the header content (placeholder for future template support).</summary>
        public object HeaderTemplate
        {
            get => _HeaderTemplate;
            set
            {
                if (_HeaderTemplate != value)
                {
                    _HeaderTemplate = value;
                    NPC(nameof(HeaderTemplate));
                }
            }
        }

        /// <summary>Occurs when this item is expanded to show its children.</summary>
        public event EventHandler Expanded;

        /// <summary>Occurs when this item is collapsed to hide its children.</summary>
        public event EventHandler Collapsed;

        // Visual components
        private MGBorder IndentationBorder { get; set; }
        private MGToggleButton ExpanderButton { get; set; }
        private MGBorder HeaderContainer { get; set; }
        private MGStackPanel ChildrenPanel { get; set; }
        private MGDockPanel HeaderPanel { get; set; }

        // Click tracking
        private DateTime _lastClickTime;

        public MGTreeViewItem(MGWindow Window)
            : base(Window, MGElementType.TreeViewItem)
        {
            using (BeginInitializing())
            {
                // Initialize Items collection
                _Items = new ObservableCollection<MGTreeViewItem>();
                _Items.CollectionChanged += Items_CollectionChanged;

                // Create MainPanel (vertical stack panel)
                var mainPanel = new MGStackPanel(Window, Orientation.Vertical);

                // Create HeaderPanel (dock panel for horizontal layout)
                HeaderPanel = new MGDockPanel(Window);

                // Create IndentationBorder (for spacing based on level)
                IndentationBorder = new MGBorder(Window);
                HeaderPanel.TryAddChild(IndentationBorder, Dock.Left);

                // Create ExpanderButton (toggle button for expand/collapse)
                ExpanderButton = new MGToggleButton(Window, false);
                ExpanderButton.OnCheckStateChanged += (sender, e) => ToggleExpansion();
                HeaderPanel.TryAddChild(ExpanderButton, Dock.Left);

                // Create HeaderContainer (border to hold header content)
                HeaderContainer = new MGBorder(Window);
                HeaderPanel.TryAddChild(HeaderContainer, Dock.Left);

                // Create ChildrenPanel (vertical stack panel for child items)
                ChildrenPanel = new MGStackPanel(Window, Orientation.Vertical);

                // Configure visibility and behavior
                ExpanderButton.Visibility = Visibility.Collapsed;
                ChildrenPanel.Visibility = Visibility.Collapsed;
                ChildrenPanel.CanChangeContent = false;

                // Subscribe to HeaderPanel click events
                HeaderPanel.MouseHandler.LMBReleasedInside += OnHeaderPanelClick;
                HeaderPanel.MouseHandler.RMBReleasedInside += OnHeaderPanelRightClick;

                // Add HeaderPanel and ChildrenPanel to MainPanel
                mainPanel.TryAddChild(HeaderPanel);
                mainPanel.TryAddChild(ChildrenPanel);

                // Set MainPanel as the content of this MGTreeViewItem
                SetContent(mainPanel);
            }
        }

        private void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            using (ChildrenPanel.AllowChangingContentTemporarily())
            {
                switch (e.Action)
                {
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                        // Add new items to ChildrenPanel
                        if (e.NewItems != null)
                        {
                            foreach (MGTreeViewItem item in e.NewItems)
                            {
                                ChildrenPanel.TryAddChild(item);
                            }
                        }
                        break;

                    case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                        // Remove items from ChildrenPanel
                        if (e.OldItems != null)
                        {
                            foreach (MGTreeViewItem item in e.OldItems)
                            {
                                ChildrenPanel.TryRemoveChild(item);
                            }
                        }
                        break;

                    case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                        // Clear all items from ChildrenPanel
                        ChildrenPanel.TryRemoveAll();
                        break;
                }
            }

            // Update expander visibility after any collection change
            UpdateExpanderVisibility();
        }

        /// <summary>Updates the indentation of this item based on its level in the tree hierarchy.</summary>
        internal void UpdateIndentation()
        {
            // Calculate indentation: Level * (OwnerTreeView?.IndentSize ?? 20)
            int indent = Level * (OwnerTreeView?.IndentSize ?? 20);
            IndentationBorder.PreferredWidth = indent;
        }

        /// <summary>Updates the visibility of the expander button based on whether this item has children.</summary>
        private void UpdateExpanderVisibility()
        {
            if (HasItems)
            {
                ExpanderButton.Visibility = Visibility.Visible;
            }
            else
            {
                ExpanderButton.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>Updates the visibility of the children panel based on the expansion state.</summary>
        private void UpdateChildrenVisibility()
        {
            if (IsExpanded)
            {
                ChildrenPanel.Visibility = Visibility.Visible;
            }
            else
            {
                ChildrenPanel.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>Expands this item to show its children.</summary>
        public void Expand()
        {
            IsExpanded = true;
            UpdateChildrenVisibility();
            Expanded?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Collapses this item to hide its children.</summary>
        public void Collapse()
        {
            IsExpanded = false;
            UpdateChildrenVisibility();
            Collapsed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Toggles the expansion state of this item.</summary>
        public void ToggleExpansion()
        {
            if (IsExpanded)
            {
                Collapse();
            }
            else
            {
                Expand();
            }
        }

        /// <summary>Expands this item and all of its descendant items recursively.</summary>
        public void ExpandAll()
        {
            // Expand this item
            Expand();

            // Recursively expand all children
            foreach (var child in Items)
            {
                child.ExpandAll();
            }
        }

        /// <summary>Collapses this item and all of its descendant items recursively.</summary>
        public void CollapseAll()
        {
            // Collapse this item
            Collapse();

            // Recursively collapse all children
            foreach (var child in Items)
            {
                child.CollapseAll();
            }
        }

        /// <summary>Determines whether this item is an ancestor of the specified item.</summary>
        /// <param name="item">The item to check.</param>
        /// <returns>True if this item is an ancestor of the specified item; otherwise, false.</returns>
        public bool IsAncestorOf(MGTreeViewItem item)
        {
            if (item == null)
                return false;

            var current = item.ParentItem;
            while (current != null)
            {
                if (current == this)
                    return true;
                current = current.ParentItem;
            }
            return false;
        }

        /// <summary>Adds a child item to this tree view item.</summary>
        /// <param name="item">The item to add as a child.</param>
        /// <exception cref="ArgumentNullException">Thrown when item is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when attempting to add this item as its own child or when creating a circular reference.</exception>
        public void AddItem(MGTreeViewItem item)
        {
            // Validate: item != null
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            // Validate: item != this (cannot add itself as a child)
            if (item == this)
                throw new InvalidOperationException("Cannot add item as its own child");

            // Validate: !IsAncestorOf(item) (prevent circular references)
            if (IsAncestorOf(item))
                throw new InvalidOperationException("Cannot create circular reference in tree");

            // Add item to _Items collection
            _Items.Add(item);

            // Set item.ParentItem = this
            item.ParentItem = this;

            // Set item.Level = this.Level + 1
            item.Level = Level + 1;

            // Propagate the OwnerTreeView
            item._OwnerTreeView = OwnerTreeView;

            // Update indentation for the new item
            item.UpdateIndentation();
        }

        /// <summary>Removes a child item from this tree view item.</summary>
        /// <param name="item">The item to remove.</param>
        public void RemoveItem(MGTreeViewItem item)
        {
            // Remove item from _Items collection
            _Items.Remove(item);

            // Set item.ParentItem = null
            if (item != null)
            {
                item.ParentItem = null;

                // Set item.OwnerTreeView = null
                item._OwnerTreeView = null;
            }
        }

        /// <summary>Removes all child items from this tree view item.</summary>
        public void ClearItems()
        {
            // For each item in _Items.ToList(), call RemoveItem(item)
            foreach (var item in _Items.ToList())
            {
                RemoveItem(item);
            }
        }

        /// <summary>Gets all visible descendant items (children and their visible descendants) of this item.</summary>
        /// <returns>An enumerable of visible descendant items.</returns>
        public IEnumerable<MGTreeViewItem> GetVisibleDescendants()
        {
            // If not expanded, return empty list
            if (!IsExpanded)
                yield break;

            // For each child in Items, yield return child
            foreach (var child in Items)
            {
                yield return child;

                // Recursively yield return child's visible descendants
                foreach (var descendant in child.GetVisibleDescendants())
                {
                    yield return descendant;
                }
            }
        }

        /// <summary>Sets the selection state of this item.</summary>
        /// <param name="selected">True to select this item; false to deselect it.</param>
        internal void SetSelected(bool selected)
        {
            // If already in the desired state, return early
            if (_IsSelected == selected)
                return;

            // Update the selection state
            _IsSelected = selected;

            // Apply selection styling
            if (selected && OwnerTreeView != null)
            {
                // Apply SelectionBackgroundBrush to HeaderPanel
                HeaderPanel.BackgroundBrush = OwnerTreeView.SelectionBackgroundBrush;
            }
            else
            {
                // Restore normal BackgroundBrush
                HeaderPanel.BackgroundBrush = null;
            }

            // Notify property changed
            NPC(nameof(IsSelected));
        }

        /// <summary>Handles the click event on the header panel.</summary>
        private void OnHeaderPanelClick(object sender, MGUI.Shared.Input.Mouse.BaseMouseReleasedEventArgs e)
        {
            // Calculate time elapsed since last click
            var currentTime = DateTime.Now;
            var timeSinceLastClick = currentTime - _lastClickTime;

            // Check if this is a double-click (less than 500ms since last click)
            if (timeSinceLastClick.TotalMilliseconds < 500)
            {
                // Trigger ItemDoubleClicked event on the owner TreeView
                OwnerTreeView?.RaiseItemDoubleClicked(this);
            }

            // Update last click time
            _lastClickTime = currentTime;

            // Notify the owner TreeView that this item was selected
            OwnerTreeView?.NotifyItemSelected(this);
        }

        /// <summary>Handles the right-click event on the header panel.</summary>
        private void OnHeaderPanelRightClick(object sender, MGUI.Shared.Input.Mouse.BaseMouseReleasedEventArgs e)
        {
            // Trigger ItemRightClicked event on the owner TreeView
            OwnerTreeView?.RaiseItemRightClicked(this);
        }

        /// <summary>Draws the tree view item, including the expander arrow if the item has children.</summary>
        public override void Draw(ElementDrawArgs DA)
        {
            // Call base Draw to render the standard elements
            base.Draw(DA);

            // If this item has no children, don't draw the expander arrow
            if (!HasItems)
                return;

            // Get the bounds of the ExpanderButton
            Microsoft.Xna.Framework.Rectangle expanderBounds = ExpanderButton.LayoutBounds;

            // Calculate the center and size of the arrow
            Point center = expanderBounds.Center;
            int size = 8;

            // Create the triangle vertices based on expansion state
            List<Point> arrowVertices;

            if (!IsExpanded)
            {
                // Triangle pointing right (collapsed state)
                arrowVertices = new List<Point>
                {
                    new Point(center.X - size / 2, center.Y - size),
                    new Point(center.X - size / 2, center.Y + size),
                    new Point(center.X + size / 2, center.Y)
                };
            }
            else
            {
                // Triangle pointing down (expanded state)
                arrowVertices = new List<Point>
                {
                    new Point(center.X - size, center.Y - size / 2),
                    new Point(center.X + size, center.Y - size / 2),
                    new Point(center.X, center.Y + size / 2)
                };
            }

            // Draw the triangle using FillPolygon
            // Use Color.Black for now (will be configurable later)
            DA.DT.FillPolygon(DA.Offset.ToVector2(), arrowVertices.Select(x => x.ToVector2()), Color.Black * DA.Opacity);
        }
    }

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
        private void RebuildVisibleItemsCache()
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

            // Set item.ParentItem = null (this is a root item)
            item.ParentItem = null;

            // Set item.Level = 0 (root level)
            item.Level = 0;

            // Set item.OwnerTreeView = this
            item._OwnerTreeView = this;

            // Call item.UpdateIndentation()
            item.UpdateIndentation();

            // Subscribe to item.Expanded event
            item.Expanded += OnItemExpanded;

            // Subscribe to item.Collapsed event
            item.Collapsed += OnItemCollapsed;
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

            // Find the index of item in _VisibleItemsCache
            int index = _VisibleItemsCache.IndexOf(item);

            // If not found, return
            if (index == -1)
                return;

            // Calculate position = index * 30 (estimated height per item)
            int position = index * 30;

            // Set the vertical offset to scroll to the item
            ScrollViewer.VerticalOffset = position;
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
