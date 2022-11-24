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
using System.Diagnostics;

namespace MGUI.Core.UI
{
    public class MGExpander : MGSingleContentHost
    {
        #region Expander Button
        public static int DefaultExpanderButtonSize = 20;
        public static int DefaultExpanderDropdownArrowSize = 10;

        public MGToggleButton ExpanderToggleButton { get; }

        /// <summary>The width and height of the button part of this <see cref="MGExpander"/>'s header.<para/>
        /// Default value: <see cref="DefaultExpanderButtonSize"/></summary>
        public int ExpanderButtonSize
        {
            get => ExpanderToggleButton.PreferredWidth ?? 0;
            set
            {
                ExpanderToggleButton.PreferredWidth = value;
                ExpanderToggleButton.PreferredHeight = value;
            }
        }

        public IBorderBrush ExpanderButtonBorderBrush
        {
            get => ExpanderToggleButton.BorderBrush;
            set => ExpanderToggleButton.BorderBrush = value;
        }

        public Thickness ExpanderButtonBorderThickness
        {
            get => ExpanderToggleButton.BorderThickness;
            set => ExpanderToggleButton.BorderThickness = value;
        }

        /// <summary>Contains the <see cref="Brushes.Fill_Brushes.IFillBrush"/>es to use when drawing the background of the button part of this <see cref="MGExpander"/>'s header.</summary>
        public VisualStateFillBrush ExpanderButtonBackgroundBrush
        {
            get => ExpanderToggleButton.BackgroundBrush;
            set => ExpanderToggleButton.BackgroundBrush = value;
        }

        /// <summary>The color to use when rendering the dropdown arrow icon inside the button part of this <see cref="MGExpander"/>'s header.<para/>
        /// Default value: <see cref="MGTheme.DropdownArrowColor"/><para/>
        /// See also:<br/><see cref="MGWindow.Theme"/><br/><see cref="MGDesktop.Theme"/></summary>
        public Color ExpanderDropdownArrowColor { get; set; }

        /// <summary>The width of the dropdown arrow icon inside the button part of this <see cref="MGExpander"/>'s header.<para/>
        /// The height of the dropdown arrow icon is based on the width.<para/>
        /// Default value: <see cref="DefaultExpanderDropdownArrowSize"/></summary>
        public int ExpanderDropdownArrowSize { get; set; }
        #endregion Expander Button

        #region Header
        /// <summary>The empty width between the expander button and the <see cref="Header"/></summary>
        public int HeaderSpacingWidth
        {
            get => HeadersPanelElement.Spacing;
            set => HeadersPanelElement.Spacing = value;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private MGElement _Header;
        /// <summary>Optional. Additional content to display to the right of the expander button.</summary>
        public MGElement Header
        {
            get => _Header;
            set
            {
                if (_Header != value)
                {
                    if (Header != null)
                    {
                        using (HeadersPanelElement.AllowChangingContentTemporarily())
                        {
                            HeadersPanelElement.TryRemoveChild(Header);
                        }
                    }

                    _Header = value;

                    if (Header != null)
                    {
                        using (HeadersPanelElement.AllowChangingContentTemporarily())
                        {
                            HeadersPanelElement.TryAddChild(Header);
                        }
                    }
                }
            }
        }

        public bool HasHeader => Header != null;
        #endregion Header

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _IsExpanded;
        public bool IsExpanded
        {
            get => _IsExpanded;
            set
            {
                if (_IsExpanded != value)
                {
                    CancelEventArgs CancelArgs = new();
                    PreviewExpandedStateChanging?.Invoke(this, CancelArgs);
                    if (CancelArgs.Cancel)
                        return;

                    if (value)
                        PreviewExpanding?.Invoke(this, CancelArgs);
                    else
                        PreviewCollapsing?.Invoke(this, CancelArgs);

                    if (CancelArgs.Cancel)
                        return;

                    _IsExpanded = value;

                    foreach (MGElement Item in BoundItems)
                        Item.Visibility = IsExpanded ? ExpandedVisibility : CollapsedVisibility;

                    ExpandedStateChanged?.Invoke(this, IsExpanded);
                    if (IsExpanded)
                        Expanded?.Invoke(this, EventArgs.Empty);
                    else
                        Collapsed?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool IsCollapsed => !IsExpanded;

        public event EventHandler<CancelEventArgs> PreviewExpanding;
        public event EventHandler<CancelEventArgs> PreviewCollapsing;
        public event EventHandler<CancelEventArgs> PreviewExpandedStateChanging;

        public event EventHandler<bool> ExpandedStateChanged;
        public event EventHandler<EventArgs> Expanded;
        public event EventHandler<EventArgs> Collapsed;

        /// <summary>The <see cref="Visibility"/> value to apply to <see cref="MGSingleContentHost.Content"/> when <see cref="IsExpanded"/>=true<para/>
        /// Default value: <see cref="Visibility.Visible"/></summary>
        public Visibility ExpandedVisibility { get; set; } = Visibility.Visible;
        /// <summary>The <see cref="Visibility"/> value to apply to <see cref="MGSingleContentHost.Content"/> when <see cref="IsCollapsed"/>=true<para/>
        /// Default value: <see cref="Visibility.Collapsed"/></summary>
        public Visibility CollapsedVisibility { get; set; } = Visibility.Collapsed;

        private ObservableCollection<MGElement> BoundItems { get; }
        /// <summary>Binds the given <paramref name="Element"/>'s <see cref="MGElement.Visibility"/> to this <see cref="MGExpander"/>'s <see cref="IsExpanded"/> state.<para/>
        /// See also: <see cref="UnbindVisibility(MGElement)"/>, <see cref="ExpandedVisibility"/>, <see cref="CollapsedVisibility"/></summary>
        public void BindVisibility(MGElement Element) => BoundItems.Add(Element);
        /// <summary>Unbinds the given <paramref name="Element"/>'s <see cref="MGElement.Visibility"/> from this <see cref="MGExpander"/>'s <see cref="IsExpanded"/> state.<para/>
        /// See also: <see cref="BindVisibility(MGElement)"/>, <see cref="ExpandedVisibility"/>, <see cref="CollapsedVisibility"/></summary>
        public void UnbindVisibility(MGElement Element) => BoundItems.Remove(Element);

        /// <summary>Provides direct access to to the stackpanel component that the expander button and the header content are placed inside of.</summary>
        public MGComponent<MGStackPanel> HeadersPanelComponent { get; }
        private MGStackPanel HeadersPanelElement { get; }

        public MGExpander(MGWindow Window, bool IsExpanded = true) 
            : base(Window, MGElementType.Expander)
        {
            using (BeginInitializing())
            {
                this.HeadersPanelElement = new(Window, Orientation.Horizontal) { Spacing = 6 };
                this.HeadersPanelComponent = new(HeadersPanelElement, ComponentUpdatePriority.BeforeContents, ComponentDrawPriority.AfterContents,
                    true, false, true, true, false, false, false,
                    (AvailableBounds, ComponentSize) => ApplyAlignment(AvailableBounds, HorizontalAlignment.Stretch, VerticalAlignment.Top, ComponentSize.Size));
                AddComponent(HeadersPanelComponent);

                this.ExpanderToggleButton = new(Window, IsExpanded);
                ExpanderToggleButton.VerticalAlignment = VerticalAlignment.Center;
                ExpanderToggleButton.ManagedParent = this;

                this.ExpanderButtonSize = DefaultExpanderButtonSize;

                HeadersPanelElement.TryAddChild(ExpanderToggleButton);
                HeadersPanelElement.CanChangeContent = false;

                this.ExpanderDropdownArrowColor = GetTheme().DropdownArrowColor;
                this.ExpanderDropdownArrowSize = DefaultExpanderDropdownArrowSize;

                ExpanderToggleButton.OnCheckStateChanged += (sender, e) =>
                {
                    this.IsExpanded = ExpanderToggleButton.IsChecked;
                };

                ExpanderToggleButton.OnEndDraw += (sender, e) =>
                {
                    int DropdownArrowHeight = ExpanderDropdownArrowSize / 2;
                    if (DropdownArrowHeight % 2 != 0)
                        DropdownArrowHeight++;
                    Size DropdownArrowSize = new Size(ExpanderDropdownArrowSize, DropdownArrowHeight);
                    Rectangle DropdownArrowBounds = ApplyAlignment(ExpanderToggleButton.LayoutBounds, HorizontalAlignment.Center, VerticalAlignment.Center, DropdownArrowSize);

                    List<Point> DropdownArrowVertices;
                    if (this.IsExpanded)
                    {
                        DropdownArrowBounds = DropdownArrowBounds.GetTranslated(new Point(0, -1));
                        DropdownArrowVertices = new() {
                            DropdownArrowBounds.BottomLeft(), DropdownArrowBounds.BottomRight(), new(DropdownArrowBounds.Center.X, DropdownArrowBounds.Top)
                        };
                    }
                    else
                    {
                        DropdownArrowBounds = DropdownArrowBounds.GetTranslated(new Point(0, 1));
                        DropdownArrowVertices = new() {
                            DropdownArrowBounds.TopLeft(), DropdownArrowBounds.TopRight(), new(DropdownArrowBounds.Center.X, DropdownArrowBounds.Bottom)
                        };
                    }

                    e.DA.DT.FillPolygon(e.DA.Offset.ToVector2(), DropdownArrowVertices.Select(x => x.ToVector2()), ExpanderDropdownArrowColor * e.DA.Opacity * this.Opacity);
                };

                this.BoundItems = new();
                this.BoundItems.CollectionChanged += (sender, e) =>
                {
                    if (e.NewItems != null)
                    {
                        foreach (MGElement Item in e.NewItems)
                        {
                            Item.Visibility = this.IsExpanded? this.ExpandedVisibility : this.CollapsedVisibility;
                        }
                    }
                };

                OnContentAdded += (sender, e) => { BindVisibility(e); };
                OnContentRemoved += (sender, e) => { UnbindVisibility(e); };

                this.IsExpanded = IsExpanded;
            }
        }
    }
}
