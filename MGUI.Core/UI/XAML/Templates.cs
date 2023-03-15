using MGUI.Shared.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

#if UseWPF
using System.Windows.Markup;
#else
using Portable.Xaml.Markup;
#endif

namespace MGUI.Core.UI.XAML
{
    [ContentProperty(nameof(Content))]
    public class ContentTemplate
    {
        [Category("Data")]
        public Element Content { get; set; }

        /// <param name="ApplyBaseSettings">If not null, this action will be invoked before <see cref="Element.ApplySettings(MGElement, MGElement, bool)"/>
        /// executes on the created <see cref="MGElement"/>.</param>
        public MGElement GetContent(MGWindow Window, MGElement Parent, object DataContext, Action<MGElement> ApplyBaseSettings = null)
        {
            if (Content == null)
                return null;

            Element ContentCopy = Content.Copy();
            MGElement Item = ContentCopy.ToElement(Window, Parent, ApplyBaseSettings);
            Element.ProcessBindings(Item, true, DataContext);
            return Item;
        }
    }
}
