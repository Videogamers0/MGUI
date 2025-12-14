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
    /// <summary>
    /// Represents a control that displays hierarchical data in a tree structure with expandable and collapsible nodes.
    /// </summary>
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

        /// <summary>
        /// Gets the outer border element that wraps the entire tree view.
        /// </summary>
        public MGBorder OuterBorder { get; private set; }

        /// <summary>
        /// Gets the component that manages the outer border.
        /// </summary>
        private MGComponentBase BorderComponent { get; set; }

        /// <summary>
        /// Gets the scroll viewer that provides scrolling functionality for the tree view.
        /// </summary>
        public MGScrollViewer ScrollViewer { get; private set; }

        /// <summary>
        /// Gets the panel that contains all root-level tree view items.
        /// </summary>
        public MGStackPanel ItemsPanel { get; private set; }

        /// <summary>
        /// Gets the collection of root-level items in the tree view.
        /// </summary>
        public IReadOnlyList<MGTreeViewItem> Items => _Items;

        /// <summary>
        /// Gets or sets the currently selected item in the tree view.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the amount of indentation (in pixels) for each level of the tree hierarchy.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the brush used to paint the background of selected items.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the foreground color used for selected items.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the brush used to paint the border of the tree view.
        /// </summary>
        public IBorderBrush BorderBrush
        {
            get => OuterBorder.BorderBrush;
            set => OuterBorder.BorderBrush = value;
        }

        /// <summary>
        /// Gets or sets the thickness of the border around the tree view.
        /// </summary>
        public MonoGame.Extended.Thickness BorderThickness
        {
            get => OuterBorder.BorderThickness;
            set => OuterBorder.BorderThickness = value;
        }

        /// <summary>
        /// Gets or sets the data source used to generate the tree view items.
        /// </summary>
        public System.Collections.IEnumerable ItemsSource
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

        /// <summary>
        /// Handles changes to the ItemsSource collection.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the name of the property that contains child items in the data source.
        /// </summary>
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

        /// <summary>
        /// Occurs when the selected item changes.
        /// </summary>
        public event EventHandler<MGTreeViewItem> SelectionChanged;

        /// <summary>
        /// Occurs when an item is expanded to show its children.
        /// </summary>
        public event EventHandler<MGTreeViewItem> ItemExpanded;

        /// <summary>
        /// Occurs when an item is collapsed to hide its children.
        /// </summary>
        public event EventHandler<MGTreeViewItem> ItemCollapsed;

        /// <summary>
        /// Occurs when an item is double-clicked.
        /// </summary>
        public event EventHandler<MGTreeViewItem> ItemDoubleClicked;

        /// <summary>
        /// Occurs when an item is right-clicked.
        /// </summary>
        public event EventHandler<MGTreeViewItem> ItemRightClicked;

        public MGTreeView(MGWindow Window)
        : base(Window, MGElementType.TreeView)
        {
            PropertyChanged += MGTreeView_PropertyChanged;

            using (BeginInitializing())
            {
                _Items = new ObservableCollection<MGTreeViewItem>();
                _Items.CollectionChanged += Items_CollectionChanged;

                _VisibleItemsCache = new List<MGTreeViewItem>();

                OuterBorder = new MGBorder(Window);
                BorderComponent = MGComponentBase.Create(OuterBorder);
                BorderComponent.BaseElement.PropertyChanged += MGTreeView_PropertyChanged;
                AddComponent(BorderComponent);

                ScrollViewer = new MGScrollViewer(Window);
                ItemsPanel = new MGStackPanel(Window, Orientation.Vertical);
                ItemsPanel.CanChangeContent = false;

                ScrollViewer.SetContent(ItemsPanel);
                OuterBorder.SetContent(ScrollViewer);
                SetContent(OuterBorder);

                ApplyDefaultStyles();

                HorizontalAlignment = HorizontalAlignment.Stretch;
                VerticalAlignment = VerticalAlignment.Stretch;
                OuterBorder.HorizontalAlignment = HorizontalAlignment.Stretch;
                OuterBorder.VerticalAlignment = VerticalAlignment.Stretch;
                ScrollViewer.HorizontalAlignment = HorizontalAlignment.Stretch;
                ScrollViewer.VerticalAlignment = VerticalAlignment.Stretch;
                ItemsPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
                ItemsPanel.VerticalAlignment = VerticalAlignment.Top;

                OuterBorder.Margin = new MonoGame.Extended.Thickness(0);
                OuterBorder.Padding = new MonoGame.Extended.Thickness(0);
                ScrollViewer.Margin = new MonoGame.Extended.Thickness(0);
                ScrollViewer.Padding = new MonoGame.Extended.Thickness(0);
                ItemsPanel.Margin = new MonoGame.Extended.Thickness(0);
                ItemsPanel.Padding = new MonoGame.Extended.Thickness(0);
            }
        }

        /// <summary>
        /// Applies the default visual styles to the tree view.
        /// </summary>
        private void ApplyDefaultStyles()
        {
            var theme = GetTheme();
            BorderBrush = theme?.TreeViewBorderBrush ?? MGUniformBorderBrush.Black;
            BorderThickness = theme?.TreeViewBorderThickness ?? new MonoGame.Extended.Thickness(1);
            SelectionBackgroundBrush = theme?.TreeViewSelectionBackground?.GetValue(true) ?? new VisualStateFillBrush(new MGSolidFillBrush(Color.LightBlue));
            if (theme != null)
                SelectionForeground = theme.TreeViewSelectionForeground;
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
                        if (e.NewItems != null)
                        {
                            foreach (MGTreeViewItem item in e.NewItems)
                            {
                                ItemsPanel.TryAddChild(item);
                            }
                        }
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                        if (e.OldItems != null)
                        {
                            foreach (MGTreeViewItem item in e.OldItems)
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

        /// <summary>
        /// Rebuilds the cache of visible items in the tree view.
        /// </summary>
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

        /// <summary>
        /// Adds a root-level item to the tree view.
        /// </summary>
        /// <param name="item">The item to add as a root-level item.</param>
        /// <exception cref="ArgumentNullException">Thrown when item is null.</exception>
        public void AddItem(MGTreeViewItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            _Items.Add(item);
            item.ParentItem = null;
            item.Level = 0;
            RegisterItemRecursive(item);
            RebuildVisibleItemsCache();
        }

        /// <summary>
        /// Registers the given item and all of its descendants with this tree view (owner assignment, indentation, event hooks).
        /// </summary>
        internal void RegisterItemRecursive(MGTreeViewItem item)
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

        /// <summary>
        /// Handles the Expanded event of a tree view item.
        /// </summary>
        private void OnItemExpanded(object sender, EventArgs e)
        {
            if (sender is MGTreeViewItem item)
            {
                ItemExpanded?.Invoke(this, item);
                RebuildVisibleItemsCache();
            }
        }

        /// <summary>
        /// Handles the Collapsed event of a tree view item.
        /// </summary>
        private void OnItemCollapsed(object sender, EventArgs e)
        {
            if (sender is MGTreeViewItem item)
            {
                ItemCollapsed?.Invoke(this, item);
                RebuildVisibleItemsCache();
            }
        }

        /// <summary>
        /// Removes a root-level item from the tree view.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        public void RemoveItem(MGTreeViewItem item)
        {
            _Items.Remove(item);
            if (item != null)
                item._OwnerTreeView = null;
        }

        /// <summary>
        /// Removes all root-level items from the tree view.
        /// </summary>
        public void ClearItems()
        {
            foreach (var item in _Items.ToList())
                RemoveItem(item);
        }

        /// <summary>
        /// Gets the next visible item after the specified item in the tree view.
        /// </summary>
        /// <param name="current">The current item.</param>
        /// <returns>The next visible item, or null if there is no next item.</returns>
        public MGTreeViewItem GetNextVisibleItem(MGTreeViewItem current)
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

        /// <summary>
        /// Gets the previous visible item before the specified item in the tree view.
        /// </summary>
        /// <param name="current">The current item.</param>
        /// <returns>The previous visible item, or null if there is no previous item.</returns>
        public MGTreeViewItem GetPreviousVisibleItem(MGTreeViewItem current)
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

        /// <summary>
        /// Notifies the tree view that an item has been selected.
        /// </summary>
        /// <param name="item">The item that was selected, or null to clear selection.</param>
        internal void NotifyItemSelected(MGTreeViewItem item)
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

        /// <summary>
        /// Selects the specified item in the tree view.
        /// </summary>
        /// <param name="item">The item to select.</param>
        public void SelectItem(MGTreeViewItem item)
        {
            NotifyItemSelected(item);
            if (item != null)
                ScrollIntoView(item);
        }

        /// <summary>
        /// Clears the current selection in the tree view.
        /// </summary>
        public void ClearSelection()
        {
            NotifyItemSelected(null);
        }

        /// <summary>
        /// Scrolls the tree view to make the specified item visible.
        /// </summary>
        /// <param name="item">The item to scroll into view.</param>
        public void ScrollIntoView(MGTreeViewItem item)
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

        /// <summary>
        /// Generates tree view items from the ItemsSource data.
        /// </summary>
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

        /// <summary>
        /// Creates a tree view item from a data object, recursively creating child items.
        /// </summary>
        /// <param name="data">The data object to create an item from.</param>
        /// <param name="level">The depth level of this item in the tree hierarchy.</param>
        /// <returns>A new MGTreeViewItem populated with the data.</returns>
        private MGTreeViewItem CreateItemFromData(object data, int level)
        {
            var item = new MGTreeViewItem(SelfOrParentWindow) { DataContext = data, Level = level, Header = data };
            var dataType = data.GetType();
            var childrenProperty = dataType.GetProperty(ChildrenPropertyName);
            if (childrenProperty != null)
            {
                var childrenValue = childrenProperty.GetValue(data);
                if (childrenValue is System.Collections.IEnumerable children)
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

        /// <summary>
        /// Triggers the ItemDoubleClicked event.
        /// </summary>
        internal void RaiseItemDoubleClicked(MGTreeViewItem item)
        {
            ItemDoubleClicked?.Invoke(this, item);
        }

        /// <summary>
        /// Triggers the ItemRightClicked event.
        /// </summary>
        internal void RaiseItemRightClicked(MGTreeViewItem item)
        {
            ItemRightClicked?.Invoke(this, item);
        }

        //TODO: remove this
        private void MGTreeView_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LayoutBounds))
            {
                Debug.WriteLine($"{sender.GetType().Name}.LayoutBounds {LayoutBounds}");
                //System.Diagnostics.Debugger.Break();
            }
        }

        //TODO: remove this
        public override void Draw(ElementDrawArgs DA)
        {
            base.Draw(DA);
        }
    }
}
