using Microsoft.Xna.Framework.Graphics;
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
    [ContentProperty(nameof(Items))]
    public class ContextMenu : Window
    {
        public override MGElementType ElementType => MGElementType.ContextMenu;

        [Category("Data")]
        public Button ButtonWrapperTemplate { get; set; }

        [Category("Behavior")]
        public bool? CanOpen { get; set; }
        [Category("Behavior")]
        public bool? StaysOpenOnItemSelected { get; set; }
        [Category("Behavior")]
        public bool? StaysOpenOnItemToggled { get; set; }
        [Category("Behavior")]
        public float? AutoCloseThreshold { get; set; }

        public List<ContextMenuItem> Items { get; set; } = new();

        public ScrollViewer ScrollViewer { get; set; } = new();
        public StackPanel ItemsPanel { get; set; } = new();

        [Category("Layout")]
        public int? HeaderWidth { get; set; }
        [Category("Layout")]
        public int? HeaderHeight { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent)
        {
            if (Parent is MGContextMenu ParentMenu)
                return new MGContextMenu(ParentMenu);
            else if (Parent is MGContextMenuItem CMI)
                return new MGContextMenu(CMI.Menu);
            else
                return new MGContextMenu(Window, Theme: Window?.Theme);
        }

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, bool IncludeContent)
        {
            MGContextMenu ContextMenu = Element as MGContextMenu;
            ScrollViewer.ApplySettings(ContextMenu, ContextMenu.ScrollViewerElement, false);
            ItemsPanel.ApplySettings(ContextMenu, ContextMenu.ItemsPanel, false);

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

            if (ButtonWrapperTemplate != null)
            {
                ContextMenu.ButtonWrapperTemplate = (Item) =>
                {
                    MGButton Button = ContextMenu.CreateDefaultDropdownButton(ContextMenu);
                    ButtonWrapperTemplate.ApplySettings(Button.Parent, Button, true);
                    return Button;
                };
            }

            if (IncludeContent)
            {
                foreach (ContextMenuItem Item in Items)
                {
                    _ = Item.ToElement<MGContextMenuItem>(Element.SelfOrParentWindow, ContextMenu);
                }
            }

            base.ApplyDerivedSettings(Parent, Element, IncludeContent);
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            foreach (Element Element in base.GetChildren())
                yield return Element;

            yield return ScrollViewer;
            yield return ItemsPanel;

            if (ButtonWrapperTemplate != null)
                yield return ButtonWrapperTemplate;

            foreach (ContextMenuItem Item in Items)
                yield return Item;
        }
    }

    public abstract class ContextMenuItem : SingleContentHost
    {

    }

    public abstract class WrappedContextMenuItem : ContextMenuItem
    {
        public ContextMenu Submenu { get; set; }

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, bool IncludeContent)
        {
            MGWrappedContextMenuItem ContextMenuItem = Element as MGWrappedContextMenuItem;

            if (Submenu != null)
                ContextMenuItem.Submenu = Submenu.ToElement<MGContextMenu>(ContextMenuItem.SelfOrParentWindow, ContextMenuItem);

            //base.ApplyDerivedSettings(Parent, Element, IncludeContent);
        }
    }

    public class ContextMenuButton : WrappedContextMenuItem
    {
        public override MGElementType ElementType => MGElementType.ContextMenuItem;

        public string CommandId { get; set; }
        public Image Icon { get; set; }
        public string ShortcutText { get; set; }

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

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, bool IncludeContent)
        {
            MGContextMenuButton ContextMenuButton = Element as MGContextMenuButton;

            if (CommandId != null)
                ContextMenuButton.CommandId = CommandId;
            if (Icon != null)
                ContextMenuButton.Icon = Icon.ToElement<MGImage>(ContextMenuButton.SelfOrParentWindow, ContextMenuButton);
            if (ShortcutText != null)
                ContextMenuButton.ShortcutText = ShortcutText;

            base.ApplyDerivedSettings(Parent, Element, IncludeContent);
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            foreach (Element Element in base.GetChildren())
                yield return Element;

            if (Icon != null)
                yield return Icon;
        }
    }

    public class ContextMenuToggle : WrappedContextMenuItem
    {
        public override MGElementType ElementType => MGElementType.ContextMenuItem;

        public string CommandId { get; set; }
        public bool? IsChecked { get; set; }
        public string ShortcutText { get; set; }

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

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, bool IncludeContent)
        {
            MGContextMenuToggle ContextMenuToggle = Element as MGContextMenuToggle;

            if (CommandId != null)
                ContextMenuToggle.CommandId = CommandId;
            if (IsChecked.HasValue)
                ContextMenuToggle.IsChecked = IsChecked.Value;
            if (ShortcutText != null)
                ContextMenuToggle.ShortcutText = ShortcutText;

            base.ApplyDerivedSettings(Parent, Element, IncludeContent);
        }
    }

    public class ContextMenuSeparator : ContextMenuItem
    {
        public override MGElementType ElementType => MGElementType.ContextMenuItem;

        public Separator Separator { get; set; } = new();

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent)
        {
            if (Parent is MGContextMenu ContextMenu)
            {
                return ContextMenu.AddSeparator(Height ?? 4);
            }
            else
                throw new InvalidOperationException($"The {nameof(Parent)} {nameof(MGElement)} of an {nameof(MGContextMenuButton)} should be of type {nameof(MGContextMenu)}");
        }

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, bool IncludeContent)
        {
            MGContextMenuSeparator ContextMenuSeparator = Element as MGContextMenuSeparator;
            Separator.ApplySettings(Element, ContextMenuSeparator.SeparatorElement, false);
            base.ApplyDerivedSettings(Parent, Element, IncludeContent);
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            foreach (Element Element in base.GetChildren())
                yield return Element;

            yield return Separator;
        }
    }

    /// <summary>XAML class for <see cref="MGContextMenuRadio"/>.<para/>
    /// Items sharing the same <see cref="GroupName"/> are mutually exclusive within the same <see cref="ContextMenu"/>.</summary>
    public class ContextMenuRadio : WrappedContextMenuItem
    {
        public override MGElementType ElementType => MGElementType.ContextMenuItem;

        [Category("Data")]
        public string GroupName { get; set; }
        [Category("Data")]
        public bool? IsChecked { get; set; }
        [Category("Data")]
        public string CommandId { get; set; }
        [Category("Data")]
        public string ShortcutText { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent)
        {
            if (Parent is MGContextMenu ContextMenu)
            {
                MGElement ContentElement = Content?.ToElement<MGElement>(Window, null) ?? new MGTextBlock(Window, "");
                return ContextMenu.AddRadioButton(ContentElement, GroupName ?? "default", IsChecked ?? false);
            }
            else
                throw new InvalidOperationException($"The parent of a {nameof(ContextMenuRadio)} must be a {nameof(MGContextMenu)}.");
        }

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, bool IncludeContent)
        {
            MGContextMenuRadio Radio = Element as MGContextMenuRadio;

            if (CommandId != null)
                Radio.CommandId = CommandId;
            if (GroupName != null)
                Radio.GroupName = GroupName;
            if (IsChecked.HasValue)
                Radio.IsChecked = IsChecked.Value;
            if (ShortcutText != null)
                Radio.ShortcutText = ShortcutText;

            base.ApplyDerivedSettings(Parent, Element, IncludeContent);
        }
    }
}
