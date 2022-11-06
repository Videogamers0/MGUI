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

namespace MGUI.Core.UI
{
    public class MGGroupBox : MGSingleContentHost
    {
        public override IEnumerable<MGElement> GetVisualTreeChildren()
        {
            foreach (MGElement Child in base.GetVisualTreeChildren())
                yield return Child;
            if (HeaderContent != null)
                yield return HeaderContent;
        }

        #region Border
        private MGBorder BorderComponent { get; }

        public MGUniformBorderBrush BorderBrush
        {
            get => (MGUniformBorderBrush)BorderComponent.BorderBrush;
            set => BorderComponent.BorderBrush = value;
        }

        public Thickness BorderThickness
        {
            get => BorderComponent.BorderThickness;
            set
            {
                if (!BorderComponent.BorderThickness.Equals(value))
                {
                    BorderComponent.BorderThickness = value;
                    LayoutChanged(this, true);
                }
            }
        }
        #endregion Border

        private MGElement _HeaderContent;
        public MGElement HeaderContent
        {
            get => _HeaderContent;
            set
            {
                if (_HeaderContent != value)
                {
                    if (HeaderContent != null)
                    {
                        InvokeContentRemoved(HeaderContent);
                        HeaderContent.SetParent(null);
                    }
                    _HeaderContent = value;
                    if (HeaderContent != null)
                    {
                        HeaderContent.SetParent(this);
                        InvokeContentAdded(HeaderContent);
                    }
                    LayoutChanged(this, true);
                }
            }
        }

        public bool HasHeader => HeaderContent != null;

        private int _HeaderHorizontalMargin;
        /// <summary>The empty width to the left and the right of the <see cref="HeaderContent"/>. This space will not be occupied by the top portion of the border.</summary>
        public int HeaderHorizontalMargin
        {
            get => _HeaderHorizontalMargin;
            set
            {
                if (_HeaderHorizontalMargin != value)
                {
                    _HeaderHorizontalMargin = value;
                    LayoutChanged(this, true);
                }
            }
        }

        private int _HeaderHorizontalPadding;
        /// <summary>The empty width to the left and the right of the <see cref="HeaderContent"/>. This space WILL be occupied by the top portion of the border.</summary>
        public int HeaderHorizontalPadding
        {
            get => _HeaderHorizontalPadding;
            set
            {
                if (_HeaderHorizontalPadding != value)
                {
                    _HeaderHorizontalPadding = value;
                    LayoutChanged(this, true);
                }
            }
        }

        public MGGroupBox(MGWindow Window, MGElement HeaderContent = null)
            : base(Window, MGElementType.GroupBox)
        {
            using (BeginInitializing())
            {
                this.HeaderHorizontalMargin = 5;
                this.HeaderHorizontalPadding = 10;

                this.BorderComponent = new(Window, new Thickness(2), MGUniformBorderBrush.Black);
                BorderComponent.SetParent(this);
                BorderComponent.ComponentParent = this;

                this.Padding = new(8,4,8,8);

                this.HeaderContent = HeaderContent;

                OnLayoutUpdated += (sender, e) =>
                {
                    if (HasHeader)
                    {
                        Size HeaderlessSize = GetHeaderlessSize();
                        Size AvailableSize = LayoutBounds.Size.AsSize().Subtract(HeaderlessSize, 0, 0);
                        this.HeaderContent.UpdateMeasurement(AvailableSize, out _, out Thickness HeaderContentSize, out _, out _);
                        Rectangle HeaderBounds = new(LayoutBounds.Left + BorderThickness.Left + HeaderHorizontalMargin + HeaderHorizontalPadding, LayoutBounds.Top, HeaderContentSize.Width, HeaderContentSize.Height);
                        this.HeaderContent.UpdateLayout(HeaderBounds);
                    }
                };
            }
        }

        protected override void UpdateContents(ElementUpdateArgs UA)
        {
            HeaderContent?.Update(UA);
            base.UpdateContents(UA);
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

            Thickness HeaderSize = new(0);
            if (HeaderContent != null)
            {
                Size RemainingSize = AvailableSize.Subtract(HeaderlessSize, 0, 0);
                HeaderContent.UpdateMeasurement(RemainingSize, out _, out HeaderSize, out _, out _);
            }

            SharedSize = new(HeaderHorizontalMargin * 2 + HeaderHorizontalPadding * 2 + HeaderSize.Width, 0, 0, 0);

            return new(BorderThickness.Left + HeaderHorizontalMargin * 2 + HeaderHorizontalPadding * 2 + HeaderSize.Width, 
                BorderThickness.Height + HeaderSize.Height, BorderThickness.Right, BorderThickness.Bottom);
        }

        public override MGBorder GetBorder() => BorderComponent;

        public override void DrawBackground(ElementDrawArgs DA, Rectangle LayoutBounds)
        {
            Rectangle ActualLayoutBounds = LayoutBounds;
            if (HeaderContent != null)
            {
                //  Move the top edge of the background downwards by half the header's height (since we also do this to the border)
                ActualLayoutBounds = new(LayoutBounds.Left, LayoutBounds.Top + HeaderContent.LayoutBounds.Height / 2, LayoutBounds.Width, LayoutBounds.Height - HeaderContent.LayoutBounds.Height / 2);
            }
            base.DrawBackground(DA, ActualLayoutBounds);
        }

        public override void DrawSelf(ElementDrawArgs DA, Rectangle LayoutBounds)
        {
            Rectangle BorderBounds = LayoutBounds;
            if (HeaderContent != null)
            {
                //  Move the top edge of the border down by half the header's height
                BorderBounds = new(LayoutBounds.Left, LayoutBounds.Top + HeaderContent.LayoutBounds.Height / 2, LayoutBounds.Width, LayoutBounds.Height - HeaderContent.LayoutBounds.Height / 2);
            }

            //  Draw each side of the border
            Thickness BT = BorderThickness;
            IFillBrush Brush = BorderBrush.Brush;
            if (BT.Left > 0)
                Brush.Draw(DA, this, new(BorderBounds.Left, BorderBounds.Top, BT.Left, BorderBounds.Height));
            if (BT.Right > 0)
                Brush.Draw(DA, this, new(BorderBounds.Right - BT.Right, BorderBounds.Top, BT.Right, BorderBounds.Height));
            if (BT.Top > 0)
            {
                if (HasHeader)
                {
                    //  Divide the top border into 2 pieces, the part to the left of the header content and the part to the right of it
                    int StartX = BorderBounds.Left + BT.Left;
                    Brush.Draw(DA, this, new(StartX, BorderBounds.Top, HeaderHorizontalPadding, BT.Top));
                    StartX += HeaderHorizontalPadding + HeaderHorizontalMargin + HeaderContent.LayoutBounds.Width + HeaderHorizontalMargin;
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
            HeaderContent?.Draw(DA);
        }
    }
}
