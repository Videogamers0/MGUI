using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Shared.Helpers
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public virtual void NotifyPropertyChanged(string szPropertyName) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(szPropertyName)); }
        /// <summary>Notify Property Changed for the given <paramref name="szPropertyName"/></summary>
        public void NPC(string szPropertyName) { NotifyPropertyChanged(szPropertyName); }
        /// <summary>Parameter <paramref name="szPropertyName"/> is optional. If not specified, <see cref="CallerMemberNameAttribute"/> is automatically applied by the compiler. (Do not pass in null)</summary>
        /// <param name="szPropertyName"></param>
        public void AutoNPC([CallerMemberName] string szPropertyName = null) { NotifyPropertyChanged(szPropertyName); }
    }
}
