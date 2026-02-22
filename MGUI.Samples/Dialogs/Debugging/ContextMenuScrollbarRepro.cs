using MGUI.Core.UI;
using Microsoft.Xna.Framework;

namespace MGUI.Samples.Dialogs.Debugging
{
    /// <summary>
    /// Repro for the scrollbar-overlap bug: items overlapping the vertical scrollbar on the
    /// second (and subsequent) open of a context menu that uses <see cref="ScrollBarVisibility.Auto"/>.
    ///
    /// How to use — add one line in Game1.Initialize(), after the Desktop is created:
    /// <code>
    ///     ContextMenuScrollbarRepro.AddToDesktop(Desktop);
    /// </code>
    ///
    /// Repro steps:
    ///   1. Run — a small window appears.
    ///   2. Right-click the label → a context menu with 20 items opens; height is capped at
    ///      150 px so the vertical scrollbar is always visible.
    ///   3. Click outside to close the menu.
    ///   4. Right-click again.
    ///
    /// Expected : items are correctly inset to the left of the scrollbar on every open.
    /// Bug (pre-fix): on the second open, items overlapped the scrollbar — stale cached
    ///                measurements from the previous open cycle were being reused.
    /// </summary>
    public static class ContextMenuScrollbarRepro
    {
        public static void AddToDesktop(MGDesktop desktop)
        {
            MGWindow win = new(desktop, 200, 200, 380, 160);
            win.TitleText = "ContextMenu scrollbar repro";

            MGTextBlock label = new(win,
                "Right-click here.  Close the menu, then right-click again to check for scrollbar overlap.",
                null,
                desktop.Theme.FontSettings.DefaultFontSize);
            label.WrapText = true;
            label.HorizontalAlignment = HorizontalAlignment.Stretch;
            label.VerticalAlignment   = VerticalAlignment.Stretch;
            label.Padding = new(8);
            win.SetContent(label);

            // Context menu: 20 items, height capped so the Auto scrollbar is always triggered.
            var menu = new MGContextMenu(win, "");
            menu.MaxHeight = 150;

            // ItemsFactory rebuilds items on every open — this exercises the scrollbar layout path.
            menu.ItemsFactory = m =>
            {
                for (int i = 1; i <= 20; i++)
                {
                    int n = i;   // capture loop variable
                    m.AddButton($"Item {n}", _ => { });
                }
            };

            label.ContextMenu = menu;
            desktop.Windows.Add(win);
        }
    }
}
