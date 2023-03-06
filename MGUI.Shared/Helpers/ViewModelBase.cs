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
        public virtual void NotifyPropertyChanged(string PropertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        /// <summary>Notify Property Changed for the given <paramref name="PropertyName"/></summary>
        public void NPC(string PropertyName) => NotifyPropertyChanged(PropertyName);
        /// <summary>Parameter <paramref name="PropertyName"/> is optional. If not specified, <see cref="CallerMemberNameAttribute"/> is automatically applied by the compiler. (Do not pass in null)</summary>
        /// <param name="PropertyName"></param>
        public void AutoNPC([CallerMemberName] string PropertyName = null) => NotifyPropertyChanged(PropertyName);
    }
}
