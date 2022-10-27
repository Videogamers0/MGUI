using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Shared.Helpers
{
    public class EventArgs<TObject, TProperty> : EventArgs
    {
        public TObject Item { get; }
        public TProperty PreviousValue { get; }
        public TProperty NewValue { get; }

        public EventArgs(TObject Obj, TProperty PreviousValue, TProperty NewValue)
        {
            this.Item = Item;
            this.PreviousValue = PreviousValue;
            this.NewValue = NewValue;
        }
    }

    public class EventArgs<TProperty> : EventArgs
    {
        public TProperty PreviousValue { get; }
        public TProperty NewValue { get; }

        public EventArgs(TProperty PreviousValue, TProperty NewValue)
        {
            this.PreviousValue = PreviousValue;
            this.NewValue = NewValue;
        }
    }
}
