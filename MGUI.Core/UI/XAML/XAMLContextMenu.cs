using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if UseWPF
using System.Windows.Markup;
#endif

namespace MGUI.Core.UI.XAML
{
#if UseWPF
    [ContentProperty(nameof(Items))]
#endif
    public class XAMLContextMenu : XAMLWindow
    {
        public bool? CanOpen { get; set; }
        public bool? StaysOpenOnItemSelected { get; set; }
        public bool? StaysOpenOnItemToggled { get; set; }
        public float? AutoCloseThreshold { get; set; }

        public List<XAMLContextMenuItem> Items { get; set; } = new();

        public XAMLScrollViewer ScrollViewer { get; set; } = new();
        public XAMLStackPanel ItemsPanel { get; set; } = new();

        public int? HeaderWidth { get; set; }
        public int? HeaderHeight { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent)
        {
            if (Parent is MGContextMenu ParentMenu)
                return new MGContextMenu(ParentMenu);
            else if (Parent is MGContextMenuItem CMI)
                return new MGContextMenu(CMI.Menu);
            else
                return new MGContextMenu(Window);
        }

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGContextMenu ContextMenu = Element as MGContextMenu;
            ScrollViewer.ApplySettings(ContextMenu, ContextMenu.ScrollViewerElement);
            ItemsPanel.ApplySettings(ContextMenu, ContextMenu.ItemsPanel);

            if (CanOpen.HasValue)
                ContextMenu.CanContextMenuOpen = CanOpen.Value;
            if (StaysOpenOnItemSelected.HasValue)
                ContextMenu.StaysOpenOnItemSelected = StaysOpenOnItemSelected.Value;
            if (StaysOpenOnItemToggled.HasValue)
                ContextMenu.StaysOpenOnItemToggled = StaysOpenOnItemToggled.Value;
            if (AutoCloseThreshold.HasValue)
                ContextMenu.AutoCloseThreshold = AutoCloseThreshold.Value;

            if (HeaderWidth.HasValue || HeaderHeight.HasValue)
                ContextMenu.HeaderSize = new(HeaderWidth ?? ContextMenu.HeaderSize.Width, HeaderHeight ?? ContextMenu.HeaderSize.Height);

            foreach (XAMLContextMenuItem Item in Items)
            {
                _ = Item.ToElement<MGContextMenuItem>(Element.SelfOrParentWindow, ContextMenu);
            }

            base.ApplyDerivedSettings(Parent, Element);
        }
    }

    public abstract class XAMLContextMenuItem : XAMLSingleContentHost
    {

    }

    public abstract class XAMLWrappedContextMenuItem : XAMLContextMenuItem
    {
        public XAMLContextMenu Submenu { get; set; }

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGWrappedContextMenuItem ContextMenuItem = Element as MGWrappedContextMenuItem;

            if (Submenu != null)
                ContextMenuItem.Submenu = Submenu.ToElement<MGContextMenu>(ContextMenuItem.SelfOrParentWindow, ContextMenuItem);

            //base.ApplyDerivedSettings(Parent, Element, NamedTextures);
        }
    }

    public class XAMLContextMenuButton : XAMLWrappedContextMenuItem
    {
        public XAMLImage Icon { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent)
        {
            if (Parent is MGContextMenu ContextMenu)
            {
                MGElement ContentElement = Content?.ToElement<MGElement>(Window, null) ?? new MGTextBlock(Window, "");
                return ContextMenu.AddButton(ContentElement, null);
            }
            else
                throw new InvalidOperationException($"The {nameof(Parent)} {nameof(MGElement)} of an {nameof(MGContextMenuButton)} should be of type {nameof(MGContextMenu)}");
        }

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGContextMenuButton ContextMenuButton = Element as MGContextMenuButton;

            if (Icon != null)
                ContextMenuButton.Icon = Icon.ToElement<MGImage>(ContextMenuButton.SelfOrParentWindow, ContextMenuButton);

            base.ApplyDerivedSettings(Parent, Element);
        }
    }

    public class XAMLContextMenuToggle : XAMLWrappedContextMenuItem
    {
        public bool? IsChecked { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent)
        {
            if (Parent is MGContextMenu ContextMenu)
            {
                MGElement ContentElement = Content?.ToElement<MGElement>(Window, null) ?? new MGTextBlock(Window, "");
                return ContextMenu.AddToggle(ContentElement, IsChecked ?? default);
            }
            else
                throw new InvalidOperationException($"The {nameof(Parent)} {nameof(MGElement)} of an {nameof(MGContextMenuButton)} should be of type {nameof(MGContextMenu)}");
        }

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGContextMenuToggle ContextMenuToggle = Element as MGContextMenuToggle;

            if (IsChecked.HasValue)
                ContextMenuToggle.IsChecked = IsChecked.Value;

            base.ApplyDerivedSettings(Parent, Element);
        }
    }

    public class XAMLContextMenuSeparator : XAMLContextMenuItem
    {
        public XAMLSeparator Separator { get; set; } = new();

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent)
        {
            if (Parent is MGContextMenu ContextMenu)
            {
                return ContextMenu.AddSeparator(Height ?? 4);
            }
            else
                throw new InvalidOperationException($"The {nameof(Parent)} {nameof(MGElement)} of an {nameof(MGContextMenuButton)} should be of type {nameof(MGContextMenu)}");
        }

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGContextMenuSeparator ContextMenuSeparator = Element as MGContextMenuSeparator;
            Separator.ApplySettings(Element, ContextMenuSeparator.SeparatorElement);
            base.ApplyDerivedSettings(Parent, Element);
        }
    }
}
