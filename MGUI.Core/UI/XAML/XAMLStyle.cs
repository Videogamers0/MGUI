﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace MGUI.Core.UI.XAML
{
#if UseWPF
    [ContentProperty(nameof(Setters))]
#endif
    public class XAMLStyle
    {
        public MGElementType TargetType { get; set; }
        //public Type TargetType { get; set; }
        public List<XAMLStyleSetter> Setters { get; set; } = new();

        /// <summary>If null, this style will affect all elements of the <see cref="TargetType"/>.<br/>
        /// Otherwise, this style will only affect elements of the <see cref="TargetType"/> that also have this name in their <see cref="XAMLElement.StyleNames"/><para/>
        /// This value should never contain commas, because commas are used to delimit multiple style names in <see cref="XAMLElement.StyleNames"/></summary>
        public string Name { get; set; }

        //TODO maybe a bool, 'AffectsComponents'? Default=true. If true, the style affects components of elements, such as XAMLCheckBox.Button
        //      If false, the style only affects elements that are explicitly defined in the visual tree, such as the Content of a SingleContentHost
    }

    public class XAMLStyleSetter
    {
        public string Property { get; set; }
        public object Value { get; set; }
    }
}
