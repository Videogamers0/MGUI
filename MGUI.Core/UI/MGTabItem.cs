using MGUI.Core.UI.Containers;
using MGUI.Shared.Helpers;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI
{
    /// <summary>Can be instantiated via: <see cref="MGTabControl.AddTab(MGElement, MGElement)"/> or <see cref="MGTabControl.AddTab(string, MGElement)"/></summary>
    public class MGTabItem : MGSingleContentHost
    {
        public MGTabControl TabControl { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private MGElement _Header;
        /// <summary>The content to display inside the tab's header.<para/>
        /// This content is automatically wrapped inside of an <see cref="MGButton"/> that is created via <see cref="MGTabControl.UnselectedTabHeaderTemplate"/> or <see cref="MGTabControl.SelectedTabHeaderTemplate"/>,<br/>
        /// See also: <see cref="IsTabSelected"/>, <see cref="MGTabControl.SelectedTab"/></summary>
        public MGElement Header
        {
            get => _Header;
            set
            {
                if (_Header != value)
                {
                    MGElement Previous = Header;
                    _Header = value;
                    NPC(nameof(Header));
                    HeaderChanged?.Invoke(this, new(Previous, Header));
                    LayoutChanged(this, true);
                }
            }
        }

        /// <summary>Invoked after <see cref="Header"/> has changed.</summary>
        public event EventHandler<EventArgs<MGElement>> HeaderChanged;

        /// <summary>Convenience property that is really just an alias for <see cref="MGTabControl.SelectedTab"/>.<para/>
        /// See also:<br/><see cref="MGTabControl.TrySelectTab(MGTabItem)"/><br/><see cref="MGTabControl.TrySelectTabAtIndex(int)"/><br/><see cref="MGTabControl.TryDeselectTab(MGTabItem, bool)"/></summary>
        public bool IsTabSelected
        {
            get => TabControl.SelectedTab == this;
            set
            {
                if (value)
                    TabControl.TrySelectTab(this);
                else if (!value && IsTabSelected)
                    TabControl.TryDeselectTab(this, true);
            }
        }

        internal MGTabItem(MGTabControl TabControl, MGElement Header, MGElement TabContent)
            : base(TabControl.ParentWindow, MGElementType.TabItem)
        {
            using (BeginInitializing())
            {
                this.TabControl = TabControl;
                this.Header = Header;
                SetContent(TabContent);
                SetParent(TabControl);
            }
        }

        public override void DrawSelf(ElementDrawArgs DA, Rectangle LayoutBounds)
        {

        }
    }
}
