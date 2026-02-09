using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI.Text
{
    public class StringClipboard
    {
#if WINDOWS
        public string Text
        {
            get => System.Windows.Clipboard.GetText(System.Windows.TextDataFormat.Text);
            set => System.Windows.Clipboard.SetText(value);
        }
#else
        public string Text { get; set; }
#endif

        public StringClipboard()
        {

        }
    }
}
