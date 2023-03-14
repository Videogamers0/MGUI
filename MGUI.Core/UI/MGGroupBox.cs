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
    public class MGGroupBox : MGSingleContentHost
    {
        public override IEnumerable<MGElement> GetVisualTreeChildren(bool IncludeInactive, bool IncludeActive)
        {
            foreach (MGElement Child in base.GetVisualTreeChildren(IncludeInactive, IncludeActive))
                yield return Child;

            if (IncludeActive)
                yield return OuterHeaderPresenter;
        }

        #region Border
        private MGBorder BorderElement { get; }
        public override MGBorder GetBorder() => BorderElement;

        public MGUniformBorderBrush BorderBrush
        {
            get => (MGUniformBorderBrush)BorderElement.BorderBrush;
            set => BorderElement.BorderBrush = value;
        }

        public Thickness BorderThickness
        {
            get => BorderElement.BorderThickness;
            set
            {
                if (!BorderElement.BorderThickness.Equals(value))
                {
                    BorderElement.BorderThickness = value;
                    LayoutChanged(this, true);
                }
            }
        }
        #endregion Border

        /// <summary>The primary header of this <see cref="MGGroupBox"/> which contains both the <see cref="Expander"/> and the <see cref="HeaderPresenter"/></summary>
        public MGHeaderedContentPresenter OuterHeaderPresenter { get; }
        /// <summary>Only visible if <see cref="IsExpandable"/> is true. Defaults to a 5px right margin.</summary>
        public MGExpander Expander { get; }
        /// <summary>The wrapper element that contains the <see cref="Header"/>.</summary>
        public MGContentPresenter HeaderPresenter { get; }

        public bool IsExpandable
        {
            get => Expander.Visibility == Visibility.Visible;
            set
            {
                if (IsExpandable != value)
                {
                    Expander.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                    NPC(nameof(IsExpandable));
                    NPC(nameof(HasHeaderContent));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private MGElement _Header;
        /// <summary>The content to displays in this <see cref="MGGroupBox"/>'s header, above the content and vertically-centered with the top edge of the <see cref="BorderBrush"/>.</summary>
        public MGElement Header
        {
            get => _Header;
            set
            {
                if (_Header != value)
                {
                    _Header = value;
                    using (HeaderPresenter.AllowChangingContentTemporarily())
                    {
                        HeaderPresenter.SetContent(Header);
                    }
                    NPC(nameof(Header));
                    NPC(nameof(HasHeaderContent));
                }
            }
        }

        private bool HasHeaderContent => IsExpandable || Header != null;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _HeaderHorizontalMargin;
        /// <summary>The empty width to the left and the right of the <see cref="Header"/>. This space will not be occupied by the top portion of the border.</summary>
        public int HeaderHorizontalMargin
        {
            get => _HeaderHorizontalMargin;
            set
            {
                if (_HeaderHorizontalMargin != value)
                {
                    _HeaderHorizontalMargin = value;
                    LayoutChanged(this, true);
                    NPC(nameof(HeaderHorizontalMargin));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _HeaderHorizontalPadding;
        /// <summary>The empty width to the left and the right of the <see cref="Header"/>. This space WILL be occupied by the top portion of the border.</summary>
        public int HeaderHorizontalPadding
        {
            get => _HeaderHorizontalPadding;
            set
            {
                if (_HeaderHorizontalPadding != value)
                {
                    _HeaderHorizontalPadding = value;
                    LayoutChanged(this, true);
                    NPC(nameof(HeaderHorizontalPadding));
                }
            }
        }

        public MGGroupBox(MGWindow Window, MGElement Header = null)
            : base(Window, MGElementType.GroupBox)
        {
            using (BeginInitializing())
            {
                this.HeaderHorizontalMargin = 5;
                this.HeaderHorizontalPadding = 10;

                this.BorderElement = new(Window, new Thickness(2), MGUniformBorderBrush.Black);
                BorderElement.SetParent(this);
                BorderElement.ManagedParent = this;
                BorderElement.OnBorderBrushChanged += (sender, e) => { NPC(nameof(BorderBrush)); };
                BorderElement.OnBorderThicknessChanged += (sender, e) => { NPC(nameof(BorderThickness)); };

                this.Expander = new(Window);
                Expander.Margin = new(0, 0, 5, 0);
                Expander.VerticalAlignment = VerticalAlignment.Center;
                this.HeaderPresenter = new(Window);
                HeaderPresenter.VerticalAlignment = VerticalAlignment.Center;
                HeaderPresenter.CanChangeContent = false;
                this.OuterHeaderPresenter = new(Window, Expander, HeaderPresenter);
                OuterHeaderPresenter.Spacing = 0;
                OuterHeaderPresenter.CanChangeContent = false;
                OuterHeaderPresenter.ManagedParent = this;
                OuterHeaderPresenter.SetParent(this);

                this.Padding = new(8,4,8,8);

                this.Header = Header;

                OnContentAdded += (sender, e) => { Expander.BindVisibility(e); };
                OnContentRemoved += (sender, e) => { Expander.UnbindVisibility(e); };

                OnLayoutUpdated += (sender, e) =>
                {
                    Size HeaderlessSize = GetHeaderlessSize();
                    Size AvailableSize = LayoutBounds.Size.AsSize().Subtract(HeaderlessSize, 0, 0);
                    this.OuterHeaderPresenter.UpdateMeasurement(AvailableSize, out _, out Thickness HeaderContentSize, out _, out _);
                    Rectangle HeaderBounds = new(LayoutBounds.Left + BorderThickness.Left + HeaderHorizontalMargin + HeaderHorizontalPadding, LayoutBounds.Top, HeaderContentSize.Width, HeaderContentSize.Height);
                    this.OuterHeaderPresenter.UpdateLayout(HeaderBounds);
                };

            }
        }

        private Size GetHeaderlessSize()
        {
            int Width = BorderThickness.Width + HeaderHorizontalMargin * 2 + HeaderHorizontalPadding * 2;
            int Height = BorderThickness.Height;
            return new(Width, Height);
        }

        public override Thickness MeasureSelfOverride(Size AvailableSize, out Thickness SharedSize)
        {
            Size HeaderlessSize = GetHeaderlessSize();

            Size RemainingSize = AvailableSize.Subtract(HeaderlessSize, 0, 0);
            OuterHeaderPresenter.UpdateMeasurement(RemainingSize, out _, out Thickness HeaderSize, out _, out _);

            SharedSize = new(HeaderHorizontalMargin * 2 + HeaderHorizontalPadding * 2 + HeaderSize.Width, 0, 0, 0);

            return new(BorderThickness.Left + HeaderHorizontalMargin * 2 + HeaderHorizontalPadding * 2 + HeaderSize.Width, 
                BorderThickness.Height + HeaderSize.Height, BorderThickness.Right, BorderThickness.Bottom);
        }

        public override void DrawBackground(ElementDrawArgs DA, Rectangle LayoutBounds)
        {
            //  Move the top edge of the background downwards by half the header's height (since we also do this to the border)
            Rectangle ActualLayoutBounds = new(LayoutBounds.Left, LayoutBounds.Top + OuterHeaderPresenter.LayoutBounds.Height / 2, LayoutBounds.Width, LayoutBounds.Height - OuterHeaderPresenter.LayoutBounds.Height / 2);

            base.DrawBackground(DA, ActualLayoutBounds);
        }

        public override void DrawSelf(ElementDrawArgs DA, Rectangle LayoutBounds)
        {
            //  Move the top edge of the border down by half the header's height
            Rectangle BorderBounds = new(LayoutBounds.Left, LayoutBounds.Top + OuterHeaderPresenter.LayoutBounds.Height / 2, LayoutBounds.Width, LayoutBounds.Height - OuterHeaderPresenter.LayoutBounds.Height / 2);

            //  Draw each side of the border
            Thickness BT = BorderThickness;
            IFillBrush Brush = BorderBrush.Brush;
            if (BT.Left > 0)
                Brush.Draw(DA, this, new(BorderBounds.Left, BorderBounds.Top, BT.Left, BorderBounds.Height));
            if (BT.Right > 0)
                Brush.Draw(DA, this, new(BorderBounds.Right - BT.Right, BorderBounds.Top, BT.Right, BorderBounds.Height));
            if (BT.Top > 0)
            {
                if (HasHeaderContent)
                {
                    //  Divide the top border into 2 pieces, the part to the left of the header content and the part to the right of it
                    int StartX = BorderBounds.Left + BT.Left;
                    Brush.Draw(DA, this, new(StartX, BorderBounds.Top, HeaderHorizontalPadding, BT.Top));
                    StartX += HeaderHorizontalPadding + HeaderHorizontalMargin + OuterHeaderPresenter.LayoutBounds.Width + HeaderHorizontalMargin;
                    Brush.Draw(DA, this, new(StartX, BorderBounds.Top, BorderBounds.Right - StartX, BT.Top));
                }
                else
                {
                    Brush.Draw(DA, this, new(BorderBounds.Left + BT.Left, BorderBounds.Top, BorderBounds.Width - BT.Width, BT.Top));
                }
            }
            if (BT.Bottom > 0)
                Brush.Draw(DA, this, new(BorderBounds.Left + BT.Left, BorderBounds.Bottom - BT.Bottom, BorderBounds.Width - BT.Width, BT.Bottom));

            //  Draw the header
            OuterHeaderPresenter.Draw(DA);
        }
    }
}
