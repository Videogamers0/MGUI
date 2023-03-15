using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI.Data_Binding
{
    /// <summary>Helper class that executes the given handler logic when a specific named property on the target object changes.</summary>
    internal class PropertyNameHandler
    {
        public readonly PropertyNameListener Listener;
        private readonly PropertyChangedEventHandler Handler;

        public PropertyNameHandler(INotifyPropertyChanged Source, string PropertyName, Action<object, PropertyChangedEventArgs> Handler)
        {
            this.Listener = new(Source, PropertyName);
            this.Handler = new(Handler);
            this.Listener.PropertyChanged += this.Handler;
        }

        public void Detach()
        {
            this.Listener.PropertyChanged -= this.Handler;
            this.Listener.Detach();
        }
    }
}
