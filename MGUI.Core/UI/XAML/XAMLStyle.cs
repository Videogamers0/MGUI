using System;
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
    }

    public class XAMLStyleSetter
    {
        public string Property { get; set; }
        public object Value { get; set; }
    }
}
