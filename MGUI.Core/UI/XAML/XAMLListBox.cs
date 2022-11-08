using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI.XAML
{
    public class XAMLListBox : XAMLElement
    {
        public XAMLElement Header { get; set; }

        public bool? IsTitleVisible { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGListBox(Window);
        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element)
        {
            MGListBox ListBox = Element as MGListBox;

            if (Header != null)
                ListBox.Header = Header.ToElement<MGElement>(ListBox.SelfOrParentWindow, ListBox);

            if (IsTitleVisible.HasValue)
                ListBox.IsTitleVisible = IsTitleVisible.Value;
        }
    }
}
