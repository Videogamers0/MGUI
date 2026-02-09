using System;
using System.Collections.Generic;
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
    [ContentProperty(nameof(Setters))]
    public class Style
    {
        public MGElementType TargetType { get; set; }
        //public Type TargetType { get; set; }
        public List<Setter> Setters { get; set; } = new();

        /// <summary>If null, this style will affect all elements of the <see cref="TargetType"/>.<br/>
        /// Otherwise, this style will only affect elements of the <see cref="TargetType"/> that also have this name in their <see cref="Element.StyleNames"/><para/>
        /// This value should never contain commas, because commas are used to delimit multiple style names in <see cref="Element.StyleNames"/></summary>
        public string Name { get; set; }

        //TODO maybe a bool, 'AffectsComponents'? Default=true. If true, the style affects components of elements, such as MGUI.Core/UI/XAML/CheckBox.Button
        //      If false, the style only affects elements that are explicitly defined in the visual tree, such as the Content of a SingleContentHost
    }

    public class Setter
    {
        public string Property { get; set; }
        public object Value { get; set; }
    }
}
