using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using MGUI.Core.UI.Containers;
using Microsoft.Xna.Framework;

namespace MGUI.Core.UI;

/// <summary>Represents an individual node in a <see cref="MGTreeView"/> that can contain child nodes.</summary>
public class MGTreeViewItem : MGSingleContentHost
{
    private object _Header;
    private int _Level;
    private MGTreeViewItem _ParentItem;
    private bool _IsExpanded;
    private bool _IsSelected;
    private ObservableCollection<MGTreeViewItem> _Items;
    internal MGTreeView _OwnerTreeView; // Will be cast to MGTreeView later
    private object _DataContext;
    private object _HeaderTemplate; // Placeholder for future template support
    private VisualStateFillBrush _PreviousHeaderBackgroundBrush; // Stores the original HeaderPanel.BackgroundBrush before selection

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
                SetExpanderButtonState(value);
                UpdateChildrenVisibility();
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
    public MGTreeView OwnerTreeView => _OwnerTreeView;

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
            ExpanderButton.OnCheckStateChanged += OnExpanderButtonCheckStateChanged;
            HeaderPanel.TryAddChild(ExpanderButton, Dock.Left);

            // Create HeaderContainer (border to hold header content)
            // This is added last, so it will fill the remaining space (LastChildFill=true by default)
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
                            // Propagate owner and indentation/event registration to new child subtree
                            if (OwnerTreeView != null)
                                OwnerTreeView.RegisterItemRecursive(item);
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
            
        // Only update if the indentation actually changes
        if (IndentationBorder.PreferredWidth != indent)
        {
            Debug.WriteLine($"[TreeView] UpdateIndentation: Item='{Header}', Level={Level}, OldIndent={IndentationBorder.PreferredWidth}, NewIndent={indent}");
            IndentationBorder.PreferredWidth = indent;
        }
    }

    /// <summary>Updates the visibility of the expander button based on whether this item has children.</summary>
    private void UpdateExpanderVisibility()
    {
        if (HasItems)
        {
            ExpanderButton.Visibility = Visibility.Visible;
            // Synchronize the ToggleButton state with IsExpanded when the button becomes visible
            SetExpanderButtonState(IsExpanded);
        }
        else
        {
            ExpanderButton.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>Updates the visibility of the children panel based on the expansion state.</summary>
    private void UpdateChildrenVisibility()
    {
        Visibility newVisibility = IsExpanded ? Visibility.Visible : Visibility.Collapsed;
        ChildrenPanel.Visibility = newVisibility;
        LayoutChanged(this, true);
    }

    /// <summary>Synchronizes the ExpanderButton state with the IsExpanded property.</summary>
    /// <param name="isExpanded">The expansion state to set on the ExpanderButton.</param>
    private void SetExpanderButtonState(bool isExpanded)
    {
        if (ExpanderButton == null)
            return;

        // Temporarily unsubscribe to avoid triggering ToggleExpansion
        ExpanderButton.OnCheckStateChanged -= OnExpanderButtonCheckStateChanged;
        ExpanderButton.IsChecked = isExpanded;
        ExpanderButton.OnCheckStateChanged += OnExpanderButtonCheckStateChanged;
    }

    /// <summary>Handles the check state changed event of the ExpanderButton.</summary>
    private void OnExpanderButtonCheckStateChanged(object sender, EventArgs e)
    {
        ToggleExpansion();
    }

    /// <summary>Expands this item to show its children.</summary>
    public void Expand()
    {
        // Setting IsExpanded will automatically call SetExpanderButtonState and UpdateChildrenVisibility
        IsExpanded = true;
        Expanded?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Collapses this item to hide its children.</summary>
    public void Collapse()
    {
        // Setting IsExpanded will automatically call SetExpanderButtonState and UpdateChildrenVisibility
        IsExpanded = false;
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
            // Store the previous background before applying selection
            _PreviousHeaderBackgroundBrush = HeaderPanel.BackgroundBrush;
            // Apply SelectionBackgroundBrush to HeaderPanel
            HeaderPanel.BackgroundBrush = OwnerTreeView.SelectionBackgroundBrush;
        }
        else
        {
            // Restore previous (pre-selection) BackgroundBrush instead of null
            HeaderPanel.BackgroundBrush = _PreviousHeaderBackgroundBrush;
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

        // Single click on header also toggles expansion if this item has children
        if (HasItems)
            ToggleExpansion();

        // Rebuild visible items cache after potential expansion change
        OwnerTreeView?.RebuildVisibleItemsCache();
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