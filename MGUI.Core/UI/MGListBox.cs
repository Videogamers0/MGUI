using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.Containers;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI
{
    public class MGListBox : MGElement
    {
        #region Outer Border
        private MGComponent<MGBorder> OuterBorderComponent { get; }
        /// <summary><see cref="MGListBox"/>es contain 3 borders:<para/>
        /// 1. <see cref="OuterBorder"/>: Wrapped around the entire <see cref="MGListBox"/><br/>
        /// 2. <see cref="InnerBorder"/>: Wrapped around the <see cref="ItemsPanel"/>, but not the <see cref="TitleComponent"/><br/>
        /// 3. <see cref="TitleBorder"/>: Wrapped around the <see cref="TitleComponent"/></summary>
        public MGBorder OuterBorder { get; }

        public IBorderBrush OuterBorderBrush
        {
            get => OuterBorder.BorderBrush;
            set => OuterBorder.BorderBrush = value;
        }

        public Thickness OuterBorderThickness
        {
            get => OuterBorder.BorderThickness;
            set => OuterBorder.BorderThickness = value;
        }
        #endregion Outer Border

        #region Inner Border
        private MGComponent<MGBorder> InnerBorderComponent { get; }
        /// <summary><see cref="MGListBox"/>es contain 3 borders:<para/>
        /// 1. <see cref="OuterBorder"/>: Wrapped around the entire <see cref="MGListBox"/><br/>
        /// 2. <see cref="InnerBorder"/>: Wrapped around the <see cref="ItemsPanel"/>, but not the <see cref="TitleComponent"/><br/>
        /// 3. <see cref="TitleBorder"/>: Wrapped around the <see cref="TitleComponent"/></summary>
        public MGBorder InnerBorder { get; }

        public IBorderBrush InnerBorderBrush
        {
            get => InnerBorder.BorderBrush;
            set => InnerBorder.BorderBrush = value;
        }

        public Thickness InnerBorderThickness
        {
            get => InnerBorder.BorderThickness;
            set => InnerBorder.BorderThickness = value;
        }
        #endregion Inner Border

        #region Title
        private MGComponent<MGBorder> TitleComponent { get; }
        /// <summary><see cref="MGListBox"/>es contain 3 borders:<para/>
        /// 1. <see cref="OuterBorder"/>: Wrapped around the entire <see cref="MGListBox"/><br/>
        /// 2. <see cref="InnerBorder"/>: Wrapped around the <see cref="ItemsPanel"/>, but not the <see cref="TitleComponent"/><br/>
        /// 3. <see cref="TitleBorder"/>: Wrapped around the <see cref="TitleComponent"/></summary>
        public MGBorder TitleBorder { get; }
        public MGContentPresenter TitlePresenter { get; }

        public IBorderBrush TitleBorderBrush
        {
            get => TitleComponent.Element.BorderBrush;
            set => TitleComponent.Element.BorderBrush = value;
        }

        public Thickness TitleBorderThickness
        {
            get => TitleComponent.Element.BorderThickness;
            set => TitleComponent.Element.BorderThickness = value;
        }

        public bool IsTitleVisible
        {
            get => TitleBorder.Visibility == Visibility.Visible;
            set => TitleBorder.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }
        #endregion Title

        public MGScrollViewer ScrollViewer { get; }
        public MGStackPanel ItemsPanel { get; }

        public void SetTitleAndContentBorder(IFillBrush Brush, int BorderThickness)
        {
            TitleBorderBrush = Brush?.AsUniformBorderBrush();
            TitleBorderThickness = new(BorderThickness, BorderThickness, BorderThickness, 0);

            InnerBorderBrush = Brush?.AsUniformBorderBrush();
            InnerBorderThickness = new(BorderThickness);
        }

        private MGElement _Header;
        public MGElement Header
        {
            get => _Header;
            set
            {
                if (_Header != value)
                {
                    _Header = value;
                    using (TitlePresenter.AllowChangingContentTemporarily())
                    {
                        TitlePresenter.SetContent(Header);
                    }
                }
            }
        }

        //listbox : XAMLElement
        //      inside the stackpanel is a contentpresenter for each element in the itemssource?
        //      and has a action<ContentpresenteR> ItemContainerStyle to set things like margin padding, alternating row background colors etc
        //      Readonlycollection<ifillbrush> alternatingrowbackgrounds. whenever this is set or when adding/removing items to the stackpanel
        //          apply these background brushes to the contentpresenters

        //TDataType
        //itemssource
        //

        public MGListBox(MGWindow ParentWindow)
            : base(ParentWindow, MGElementType.ListBox)
        {
            using (BeginInitializing())
            {
                //  Create the outer border
                this.OuterBorder = new(ParentWindow, 0, MGSolidFillBrush.Black);
                this.OuterBorderComponent = MGComponentBase.Create(OuterBorder);
                AddComponent(OuterBorderComponent);

                //  Create the title bar
                this.TitleBorder = new(ParentWindow);
                TitleBorder.Padding = new(6, 3);
                TitleBorder.BackgroundBrush = GetTheme().TitleBackground.GetValue(true);
                TitleBorder.DefaultTextForeground.SetAll(Color.White);
                this.TitlePresenter = new(ParentWindow);
                TitlePresenter.VerticalAlignment = VerticalAlignment.Center;
                TitleBorder.SetContent(TitlePresenter);
                TitleBorder.CanChangeContent = false;
                TitlePresenter.CanChangeContent = false;
                this.TitleComponent = new(TitleBorder, true, false, true, true, false, false, false,
                    (AvailableBounds, ComponentSize) => ApplyAlignment(AvailableBounds, HorizontalAlignment.Stretch, VerticalAlignment.Top, ComponentSize.Size));
                AddComponent(TitleComponent);

                //  Create the inner border
                this.InnerBorder = new(ParentWindow);
                this.InnerBorderComponent = new(InnerBorder, false, false, true, true, true, true, false,
                    (AvailableBounds, ComponentSize) => ApplyAlignment(AvailableBounds, HorizontalAlignment.Stretch, VerticalAlignment.Stretch, ComponentSize.Size));
                AddComponent(InnerBorderComponent);

                //  Create the scrollviewer and itemspanel
                this.ItemsPanel = new(ParentWindow, Orientation.Vertical);
                ItemsPanel.CanChangeContent = false;
                this.ScrollViewer = new(ParentWindow);
                ScrollViewer.SetContent(ItemsPanel);
                ScrollViewer.CanChangeContent = false;
                InnerBorder.SetContent(ScrollViewer);
                InnerBorder.CanChangeContent = false;

                SetTitleAndContentBorder(MGSolidFillBrush.White, 1);

                MinHeight = 50;
            }
        }

        public override MGBorder GetBorder() => OuterBorder;
    }
}
