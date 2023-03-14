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
        public static int DefaultExpanderButtonSize { get; set; } = 20;
        public static int DefaultExpanderDropdownArrowSize { get; set; } = 10;

        public MGToggleButton ExpanderToggleButton { get; }

        /// <summary>The width and height of the button part of this <see cref="MGExpander"/>'s header.<para/>
        /// Default value: <see cref="DefaultExpanderButtonSize"/></summary>
        public int ExpanderButtonSize
        {
            get => ExpanderToggleButton.PreferredWidth ?? 0;
            set
            {
                if (ExpanderToggleButton.PreferredWidth != value || ExpanderToggleButton.PreferredHeight != value)
                {
                    ExpanderToggleButton.PreferredWidth = value;
                    ExpanderToggleButton.PreferredHeight = value;
                    NPC(nameof(ExpanderButtonSize));
                }
            }
        }

        public IBorderBrush ExpanderButtonBorderBrush
        {
            get => ExpanderToggleButton.BorderBrush;
            set
            {
                if (ExpanderToggleButton.BorderBrush != value)
                {
                    ExpanderToggleButton.BorderBrush = value;
                    NPC(nameof(ExpanderButtonBorderBrush));
                }
            }
        }

        public Thickness ExpanderButtonBorderThickness
        {
            get => ExpanderToggleButton.BorderThickness;
            set
            {
                if (!ExpanderToggleButton.BorderThickness.Equals(value))
                {
                    ExpanderToggleButton.BorderThickness = value;
                    NPC(nameof(ExpanderButtonBorderThickness));
                }
            }
        }

        /// <summary>Contains the <see cref="Brushes.Fill_Brushes.IFillBrush"/>es to use when drawing the background of the button part of this <see cref="MGExpander"/>'s header.</summary>
        public VisualStateFillBrush ExpanderButtonBackgroundBrush
        {
            get => ExpanderToggleButton.BackgroundBrush;
            set
            {
                if (ExpanderToggleButton.BackgroundBrush != value)
                {
                    ExpanderToggleButton.BackgroundBrush = value;
                    NPC(nameof(ExpanderButtonBackgroundBrush));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Color _ExpanderDropdownArrowColor;
        /// <summary>The color to use when rendering the dropdown arrow icon inside the button part of this <see cref="MGExpander"/>'s header.<para/>
        /// Default value: <see cref="MGTheme.DropdownArrowColor"/><para/>
        /// See also:<br/><see cref="MGWindow.Theme"/><br/><see cref="MGDesktop.Theme"/></summary>
        public Color ExpanderDropdownArrowColor
        {
            get => _ExpanderDropdownArrowColor;
            set
            {
                if (_ExpanderDropdownArrowColor != value)
                {
                    _ExpanderDropdownArrowColor = value;
                    NPC(nameof(ExpanderDropdownArrowColor));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _ExpanderDropdownArrowSize;
        /// <summary>The width of the dropdown arrow icon inside the button part of this <see cref="MGExpander"/>'s header.<para/>
        /// The height of the dropdown arrow icon is based on the width.<para/>
        /// Default value: <see cref="DefaultExpanderDropdownArrowSize"/></summary>
        public int ExpanderDropdownArrowSize
        {
            get => _ExpanderDropdownArrowSize;
            set
            {
                if (_ExpanderDropdownArrowSize != value)
                {
                    _ExpanderDropdownArrowSize = value;
                    NPC(nameof(ExpanderDropdownArrowSize));
                }
            }
        }
        #endregion Expander Button

        #region Header
        /// <summary>The empty width between the expander button and the <see cref="Header"/></summary>
        public int HeaderSpacingWidth
        {
            get => HeadersPanelElement.Spacing;
            set
            {
                if (HeadersPanelElement.Spacing != value)
                {
                    HeadersPanelElement.Spacing = value;
                    NPC(nameof(HeaderSpacingWidth));
                }
            }
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

                    NPC(nameof(Header));
                    NPC(nameof(HeaderVerticalAlignment));
                    NPC(nameof(HasHeader));
                }
            }
        }

        /// <summary>Convenience property that just gets or sets <see cref="Header"/>'s <see cref="MGElement.VerticalAlignment"/>.<para/>
        /// This property is null if there is no <see cref="Header"/>.<para/>
        /// This value is not maintained if <see cref="Header"/> is set to a new value. 
        /// For example, if you set <see cref="HeaderVerticalAlignment"/> to <see cref="VerticalAlignment.Bottom"/>,
        /// but then set <see cref="Header"/> to a new value with <see cref="MGElement.VerticalAlignment"/>=<see cref="VerticalAlignment.Center"/>,
        /// the new <see cref="Header"/> will not automatically be changed to <see cref="VerticalAlignment.Bottom"/></summary>
        public VerticalAlignment? HeaderVerticalAlignment
        {
            get => Header?.VerticalAlignment;
            set
            {
                if (value.HasValue && Header != null)
                {
                    Header.VerticalAlignment = value.Value;
                    NPC(nameof(HeaderVerticalAlignment));
                }
            }
        }

        public bool HasHeader => Header != null;
        #endregion Header

        #region IsExpanded
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _IsExpanded;
        /// <summary>See also: <see cref="IsCollapsed"/></summary>
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
                    ExpanderToggleButton.IsChecked = IsExpanded;
                    NPC(nameof(IsExpanded));
                    NPC(nameof(IsCollapsed));

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

        /// <summary>This value is always the opposite of <see cref="IsExpanded"/></summary>
        public bool IsCollapsed
        {
            get => !IsExpanded;
            set => IsExpanded = !value;
        }

        /// <summary>Invoked just before <see cref="IsExpanded"/> is changed. (Invoked before <see cref="PreviewExpanding"/> and <see cref="PreviewCollapsing"/>)</summary>
        public event EventHandler<CancelEventArgs> PreviewExpandedStateChanging;
        /// <summary>Invoked just before <see cref="IsExpanded"/> is set to true. (Invoked after <see cref="PreviewExpandedStateChanging"/>)</summary>
        public event EventHandler<CancelEventArgs> PreviewExpanding;
        /// <summary>Invoked just before <see cref="IsExpanded"/> is set to false. (Invoked after <see cref="PreviewExpandedStateChanging"/>)</summary>
        public event EventHandler<CancelEventArgs> PreviewCollapsing;

        /// <summary>Invoked after <see cref="IsExpanded"/> is changed. (Invoked before <see cref="Expanded"/> and <see cref="Collapsed"/>)</summary>
        public event EventHandler<bool> ExpandedStateChanged;
        /// <summary>Invoked after <see cref="IsExpanded"/> is set to true. (Invoked after <see cref="ExpandedStateChanged"/>)</summary>
        public event EventHandler<EventArgs> Expanded;
        /// <summary>Invoked after <see cref="IsExpanded"/> is set to false. (Invoked after <see cref="ExpandedStateChanged"/>)</summary>
        public event EventHandler<EventArgs> Collapsed;
        #endregion IsExpanded

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Visibility _ExpandedVisibility;
        /// <summary>The <see cref="Visibility"/> value to apply to <see cref="MGSingleContentHost.Content"/> when <see cref="IsExpanded"/> is true<para/>
        /// Default value: <see cref="Visibility.Visible"/></summary>
        public Visibility ExpandedVisibility
        {
            get => _ExpandedVisibility;
            set
            {
                if (_ExpandedVisibility != value)
                {
                    _ExpandedVisibility = value;
                    NPC(nameof(ExpandedVisibility));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Visibility _CollapsedVisibility;
        /// <summary>The <see cref="Visibility"/> value to apply to <see cref="MGSingleContentHost.Content"/> when <see cref="IsCollapsed"/> is true<para/>
        /// Default value: <see cref="Visibility.Collapsed"/></summary>
        public Visibility CollapsedVisibility
        {
            get => _CollapsedVisibility;
            set
            {
                if (_CollapsedVisibility != value)
                {
                    _CollapsedVisibility = value;
                    NPC(nameof(CollapsedVisibility));
                }
            }
        }

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

        /// <summary>Margin between the bottom of the header and the top of the expandable content.<para/>
        /// Functionally equivalent to the bottom margin of <see cref="HeadersPanelElement"/>'s <see cref="MGElement.Margin"/><para/>
        /// Default value: 3</summary>
        public int HeaderBottomMargin
        {
            get => HeadersPanelElement.Margin.Bottom;
            set
            {
                if (HeadersPanelElement.Margin.Bottom != value)
                {
                    HeadersPanelElement.Margin = HeadersPanelElement.Margin.ChangeBottom(value);
                    NPC(nameof(HeaderBottomMargin));
                }
            }
        }

        public MGExpander(MGWindow Window, bool IsExpanded = true) 
            : base(Window, MGElementType.Expander)
        {
            using (BeginInitializing())
            {
                this.HeadersPanelElement = new(Window, Orientation.Horizontal) { Spacing = 6 };
                this.HeadersPanelComponent = new(HeadersPanelElement, ComponentUpdatePriority.BeforeContents, ComponentDrawPriority.AfterContents,
                    true, false, true, true, false, false, true,
                    (AvailableBounds, ComponentSize) => ApplyAlignment(AvailableBounds.GetCompressed(Padding), HorizontalAlignment.Stretch, VerticalAlignment.Top, ComponentSize.Size));
                AddComponent(HeadersPanelComponent);

                ExpandedVisibility = Visibility.Visible;
                CollapsedVisibility = Visibility.Collapsed;

                this.HeaderBottomMargin = 3;

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

                ExpanderToggleButton.OnEndingDraw += (sender, e) =>
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
