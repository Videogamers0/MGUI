using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI.Data_Binding
{
    /// <summary>Wrapper class that only propagates the <see cref="PropertyChanged"/> events of the given <see cref="Source"/> object 
    /// if the <see cref="PropertyChangedEventArgs.PropertyName"/> matches the given <see cref="PropertyName"/>.</summary>
    internal class PropertyNameListener : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public readonly INotifyPropertyChanged Source;
        public readonly string PropertyName;

        public PropertyNameListener(INotifyPropertyChanged Source, string PropertyName)
        {
            this.Source = Source;
            this.PropertyName = PropertyName;
            this.Source.PropertyChanged += Source_PropertyChanged;
        }

        private void Source_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == PropertyName)
                PropertyChanged?.Invoke(sender, e);
        }

        public void Detach()
        {
            this.Source.PropertyChanged -= Source_PropertyChanged;
        }
    }
}
