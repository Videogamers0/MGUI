using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if UseWPF
using System.Windows.Markup;
#else
using Portable.Xaml.Markup;
#endif

namespace MGUI.Core.UI.XAML
{
    /// <summary>XAML representation of <see cref="MGMenuBar"/>.<para/>
    /// Usage:
    /// <code>
    /// &lt;MenuBar&gt;
    ///   &lt;MenuBarItem Text="File"&gt;
    ///     &lt;ContextMenuButton CommandId="New"&gt;New&lt;/ContextMenuButton&gt;
    ///     &lt;ContextMenuSeparator /&gt;
    ///     &lt;ContextMenuButton CommandId="Exit"&gt;Exit&lt;/ContextMenuButton&gt;
    ///   &lt;/MenuBarItem&gt;
    /// &lt;/MenuBar&gt;
    /// </code>
    /// </summary>
    [ContentProperty(nameof(Items))]
    public class MenuBar : SingleContentHost
    {
        public override MGElementType ElementType => MGElementType.MenuBar;

        [Category("Data")]
        public List<MenuBarItem> Items { get; set; } = new();

        [Category("Data")]
        public Button ButtonWrapperTemplate { get; set; }

        [Category("Layout")]
        public StackPanel ItemsPanel { get; set; } = new();

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent)
            => new MGMenuBar(Window);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, bool IncludeContent)
        {
            MGMenuBar MenuBar = Element as MGMenuBar;

            ItemsPanel.ApplySettings(MenuBar, MenuBar.ItemsPanel, false);

            if (ButtonWrapperTemplate != null)
            {
                MenuBar.ButtonWrapperTemplate = (Win) =>
                {
                    MGButton Button = MenuBar.CreateDefaultBarButton(Win);
                    ButtonWrapperTemplate.ApplySettings(Win.SelfOrParentWindow, Button, true);
                    return Button;
                };
            }

            if (IncludeContent)
            {
                foreach (MenuBarItem Item in Items)
                    Item.ToElement<MGMenuBarItem>(Element.SelfOrParentWindow, MenuBar);
            }

            base.ApplyDerivedSettings(Parent, Element, IncludeContent);
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            foreach (Element Element in base.GetChildren())
                yield return Element;

            yield return ItemsPanel;

            if (ButtonWrapperTemplate != null)
                yield return ButtonWrapperTemplate;

            foreach (MenuBarItem Item in Items)
                yield return Item;
        }
    }

    /// <summary>XAML representation of <see cref="MGMenuBarItem"/>.<para/>
    /// Direct children become entries in the associated <see cref="MGContextMenu"/> dropdown.</summary>
    [ContentProperty(nameof(Items))]
    public class MenuBarItem : SingleContentHost
    {
        public override MGElementType ElementType => MGElementType.MenuBarItem;

        /// <summary>The label text displayed in the menu bar.</summary>
        [Category("Data")]
        public string Text { get; set; }

        /// <summary>Context menu items that appear in the dropdown when this item is clicked.<para/>
        /// Each child will be added to a new <see cref="MGContextMenu"/> created automatically.</summary>
        [Category("Data")]
        public List<ContextMenuItem> Items { get; set; } = new();

        /// <summary>Alternatively, specify an explicit <see cref="ContextMenu"/> element (overrides <see cref="Items"/>).</summary>
        [Category("Data")]
        public ContextMenu Submenu { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent)
        {
            if (Parent is MGMenuBar MenuBar)
            {
                MGElement ContentElement = Content?.ToElement<MGElement>(Window, null)
                    ?? new MGTextBlock(Window, Text ?? "", null, Window.GetTheme().FontSettings.ContextMenuFontSize);
                return MenuBar.AddItem(ContentElement);
            }
            else
                throw new InvalidOperationException($"{nameof(MenuBarItem)} must be a direct child of {nameof(MenuBar)}.");
        }

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, bool IncludeContent)
        {
            MGMenuBarItem Item = Element as MGMenuBarItem;
            MGWindow Window = Element.SelfOrParentWindow;

            if (IncludeContent)
            {
                if (Submenu != null)
                {
                    // Explicit <Submenu> element
                    Item.Submenu = Submenu.ToElement<MGContextMenu>(Window, Item);
                }
                else if (Items.Count > 0)
                {
                    // Implicit items — create a context menu automatically
                    MGContextMenu DropdownMenu = new(Window, "");
                    DropdownMenu.IsTitleBarVisible = false;
                    DropdownMenu.AutoCloseThreshold = null;
                    foreach (ContextMenuItem CMItem in Items)
                        CMItem.ToElement<MGContextMenuItem>(Window, DropdownMenu);
                    Item.Submenu = DropdownMenu;
                }
            }

            //base.ApplyDerivedSettings(Parent, Element, IncludeContent);
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            foreach (Element Element in base.GetChildren())
                yield return Element;

            if (Submenu != null)
                yield return Submenu;

            foreach (ContextMenuItem Item in Items)
                yield return Item;
        }
    }
}
