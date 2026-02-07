using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MGUI.Core.UI.Containers;
using Microsoft.Xna.Framework;
using MGUI.Shared.Helpers;

namespace MGUI.Core.UI;

/// <summary>
/// Represents an individual node in a <see cref="MGTreeView"/> that can contain child nodes.
/// </summary>
public class MGTreeViewItem : MGSingleContentHost
{
    private object _Header;
    private int _Level;
    private MGTreeViewItem _ParentItem;
    private bool _IsExpanded;
    private readonly ObservableCollection<MGTreeViewItem> _Items;
    internal MGTreeView _OwnerTreeView;
    private object _HeaderTemplate;
    private VisualStateFillBrush _PreviousHeaderBackgroundBrush;

    /// <summary>
    /// Gets the visual element that displays the header content.
    /// </summary>
    public MGElement HeaderContent { get; private set; }

    /// <summary>
    /// Gets or sets the content to display in the header of this tree view item.
    /// </summary>
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

    /// <summary>
    /// Updates the HeaderContent based on the current Header value.
    /// </summary>
    private void UpdateHeaderContent()
    {
        if (_Header is string text)
            HeaderContent = new MGTextBlock(SelfOrParentWindow, text);
        else if (_Header is MGElement element)
            HeaderContent = element;
        else if (_Header != null)
            HeaderContent = new MGTextBlock(SelfOrParentWindow, _Header.ToString());
        else
            HeaderContent = new MGTextBlock(SelfOrParentWindow, string.Empty);

        using (HeaderContainer.AllowChangingContentTemporarily())
            HeaderContainer.SetContent(HeaderContent);
    }

    /// <summary>
    /// Gets or sets the depth level of this item in the tree hierarchy (0 for root items).
    /// </summary>
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

    /// <summary>
    /// Gets or sets the parent item of this tree view item. Null if this is a root item.
    /// </summary>
    public MGTreeViewItem ParentItem
    {
        get => _ParentItem;
        internal set => _ParentItem = value;
    }

    /// <summary>
    /// Gets or sets whether this item is expanded to show its children.
    /// </summary>
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

    /// <summary>
    /// Gets the collection of child items.
    /// </summary>
    public IReadOnlyList<MGTreeViewItem> Items => _Items;

    /// <summary>
    /// Gets whether this item has any child items.
    /// </summary>
    public bool HasItems => Items?.Count > 0;

    /// <summary>
    /// Gets the <see cref="MGTreeView"/> that owns this item.
    /// </summary>
    public MGTreeView OwnerTreeView => _OwnerTreeView;

    /// <summary>
    /// Gets or sets the template used to display the header content (placeholder for future template support).
    /// </summary>
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

    /// <summary>
    /// Occurs when this item is expanded to show its children.
    /// </summary>
    public event EventHandler Expanded;

    /// <summary>
    /// Occurs when this item is collapsed to hide its children.
    /// </summary>
    public event EventHandler Collapsed;

    private MGBorder IndentationBorder { get; set; }
    private MGToggleButton ExpanderButton { get; set; }
    private MGBorder HeaderContainer { get; set; }
    private MGStackPanel ChildrenPanel { get; set; }
    private MGDockPanel HeaderPanel { get; set; }
    private DateTime _lastClickTime;

    public MGTreeViewItem(MGWindow Window) : base(Window, MGElementType.TreeViewItem)
    {
        using (BeginInitializing())
        {
            _Items = new ObservableCollection<MGTreeViewItem>();
            _Items.CollectionChanged += Items_CollectionChanged;

            var mainPanel = new MGStackPanel(Window, Orientation.Vertical);
            HeaderPanel = new MGDockPanel(Window);
            IndentationBorder = new MGBorder(Window);
            HeaderPanel.TryAddChild(IndentationBorder, Dock.Left);
            ExpanderButton = new MGToggleButton(Window, false);
            ExpanderButton.OnCheckStateChanged += OnExpanderButtonCheckStateChanged;
            HeaderPanel.TryAddChild(ExpanderButton, Dock.Left);
            HeaderContainer = new MGBorder(Window);
            HeaderPanel.TryAddChild(HeaderContainer, Dock.Left);
            ChildrenPanel = new MGStackPanel(Window, Orientation.Vertical);
            ExpanderButton.Visibility = Visibility.Collapsed;
            ChildrenPanel.Visibility = Visibility.Collapsed;
            ChildrenPanel.CanChangeContent = false;
            HeaderPanel.MouseHandler.LMBReleasedInside += OnHeaderPanelClick;
            HeaderPanel.MouseHandler.RMBReleasedInside += OnHeaderPanelRightClick;
            mainPanel.TryAddChild(HeaderPanel);
            mainPanel.TryAddChild(ChildrenPanel);
            SetContent(mainPanel);
        }
    }

    private void Items_CollectionChanged(object sender,
        System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        using (ChildrenPanel.AllowChangingContentTemporarily())
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                    {
                        foreach (MGTreeViewItem item in e.NewItems)
                        {
                            ChildrenPanel.TryAddChild(item);
                            if (OwnerTreeView != null)
                                OwnerTreeView.RegisterItemRecursive(item);
                        }
                    }

                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                    {
                        foreach (MGTreeViewItem item in e.OldItems)
                        {
                            ChildrenPanel.TryRemoveChild(item);
                        }
                    }

                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    ChildrenPanel.TryRemoveAll();
                    break;
            }
        }

        UpdateExpanderVisibility();
    }

    internal void UpdateIndentation()
    {
        int indent = Level * (OwnerTreeView?.IndentSize ?? 20);
        if (IndentationBorder.PreferredWidth != indent)
        {
            IndentationBorder.PreferredWidth = indent;
        }
    }

    private void UpdateExpanderVisibility()
    {
        if (HasItems)
        {
            ExpanderButton.Visibility = Visibility.Visible;
            SetExpanderButtonState(IsExpanded);
        }
        else
        {
            ExpanderButton.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdateChildrenVisibility()
    {
        Visibility newVisibility = IsExpanded ? Visibility.Visible : Visibility.Collapsed;
        ChildrenPanel.Visibility = newVisibility;
        LayoutChanged(this, true);
    }

    private void SetExpanderButtonState(bool isExpanded)
    {
        if (ExpanderButton == null)
            return;
        ExpanderButton.OnCheckStateChanged -= OnExpanderButtonCheckStateChanged;
        ExpanderButton.IsChecked = isExpanded;
        ExpanderButton.OnCheckStateChanged += OnExpanderButtonCheckStateChanged;
    }

    private void OnExpanderButtonCheckStateChanged(object sender, EventArgs<bool> e)
    {
        if (e.NewValue) Expand();
        else Collapse();
    }

    /// <summary>
    /// Expands this item to show its children.
    /// </summary>
    public void Expand()
    {
        IsExpanded = true;
        SetExpanderButtonState(true);
        Expanded?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Collapses this item to hide its children.
    /// </summary>
    public void Collapse()
    {
        IsExpanded = false;
        SetExpanderButtonState(false);
        Collapsed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Toggles the expansion state of this item.
    /// </summary>
    public void ToggleExpansion()
    {
        if (IsExpanded) Collapse();
        else Expand();
    }

    /// <summary>
    /// Expands this item and all of its descendant items recursively.
    /// </summary>
    public void ExpandAll()
    {
        Expand();
        foreach (var child in Items)
            child.ExpandAll();
    }

    /// <summary>
    /// Collapses this item and all of its descendant items recursively.
    /// </summary>
    public void CollapseAll()
    {
        Collapse();
        foreach (var child in Items)
            child.CollapseAll();
    }

    /// <summary>
    /// Determines whether this item is an ancestor of the specified item.
    /// </summary>
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

    /// <summary>
    /// Adds a child item to this tree view item.
    /// </summary>
    /// <param name="item">The item to add as a child.</param>
    /// <exception cref="ArgumentNullException">Thrown when item is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when attempting to add this item as its own child or when creating a circular reference.</exception>
    public void AddItem(MGTreeViewItem item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));
        if (item == this)
            throw new InvalidOperationException("Cannot add item as its own child");
        if (IsAncestorOf(item))
            throw new InvalidOperationException("Cannot create circular reference in tree");
        _Items.Add(item);
        item.ParentItem = this;
        item.Level = Level + 1;
        item._OwnerTreeView = OwnerTreeView;
        item.UpdateIndentation();
    }

    /// <summary>
    /// Removes a child item from this tree view item.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    public void RemoveItem(MGTreeViewItem item)
    {
        _Items.Remove(item);
        if (item != null)
        {
            item.ParentItem = null;
            item._OwnerTreeView = null;
        }
    }

    /// <summary>
    /// Removes all child items from this tree view item.
    /// </summary>
    public void ClearItems()
    {
        foreach (var item in _Items.ToList())
            RemoveItem(item);
    }

    /// <summary>
    /// Gets all visible descendant items (children and their visible descendants) of this item.
    /// </summary>
    /// <returns>An enumerable of visible descendant items.</returns>
    public IEnumerable<MGTreeViewItem> GetVisibleDescendants()
    {
        if (!IsExpanded)
            yield break;
        foreach (var child in Items)
        {
            yield return child;
            foreach (var descendant in child.GetVisibleDescendants())
                yield return descendant;
        }
    }

    /// <summary>
    /// Sets the selection state of this item.
    /// </summary>
    /// <param name="selected">True to select this item; false to deselect it.</param>
    internal void SetSelected(bool selected)
    {
        if (IsSelected == selected)
            return;
        IsSelected = selected;
        if (selected && OwnerTreeView != null)
        {
            _PreviousHeaderBackgroundBrush = HeaderPanel.BackgroundBrush;
            HeaderPanel.BackgroundBrush = OwnerTreeView.SelectionBackgroundBrush;
        }
        else
        {
            HeaderPanel.BackgroundBrush = _PreviousHeaderBackgroundBrush;
        }
    }

    /// <summary>
    /// Handles the click event on the header panel.
    /// </summary>
    private void OnHeaderPanelClick(object sender, Shared.Input.Mouse.BaseMouseReleasedEventArgs e)
    {
        var now = DateTime.Now;
        bool isDouble = (now - _lastClickTime).TotalMilliseconds < 500;
        _lastClickTime = now;
        Point layoutPos = ConvertCoordinateSpace(CoordinateSpace.Screen, CoordinateSpace.Layout, e.Position);
        bool clickedExpander = ExpanderButton != null && ExpanderButton.LayoutBounds.ContainsInclusive(layoutPos);
        if (clickedExpander)
            return;
        OwnerTreeView?.NotifyItemSelected(this);
        if (HasItems && !isDouble)
            ToggleExpansion();
        if (isDouble && HasItems)
            OwnerTreeView?.RaiseItemDoubleClicked(this);
        OwnerTreeView?.RebuildVisibleItemsCache();
    }

    /// <summary>
    /// Handles the right-click event on the header panel.
    /// </summary>
    private void OnHeaderPanelRightClick(object sender, Shared.Input.Mouse.BaseMouseReleasedEventArgs e)
    {
        OwnerTreeView?.RaiseItemRightClicked(this);
    }

    /// <summary>
    /// Draws the tree view item, including the expander arrow if the item has children.
    /// </summary>
    public override void Draw(ElementDrawArgs DA)
    {
        base.Draw(DA);
        if (!HasItems)
            return;
        Rectangle expanderBounds = ExpanderButton.LayoutBounds;
        Point center = expanderBounds.Center;
        int size = 8;
        List<Point> arrowVertices = !IsExpanded
            ? new List<Point>
            {
                new(center.X - size / 2, center.Y - size),
                new(center.X - size / 2, center.Y + size),
                new(center.X + size / 2, center.Y)
            }
            : new List<Point>
            {
                new(center.X - size, center.Y - size / 2),
                new(center.X + size, center.Y - size / 2),
                new(center.X, center.Y + size / 2)
            };

        Color arrowColor = OwnerTreeView?.GetTheme()?.TreeViewExpanderArrowColor ?? Color.Black;
        arrowColor *= DA.Opacity;
        DA.DT.FillPolygon(DA.Offset.ToVector2(), arrowVertices.Select(x => x.ToVector2()), arrowColor);
    }
}