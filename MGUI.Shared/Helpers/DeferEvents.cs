using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Shared.Helpers
{
    public class DeferEventsTransaction : IDisposable
    {
        public DeferEventsManager Item { get; }
        public bool IsDisposed { get; private set; }

        public DeferEventsTransaction(DeferEventsManager Item)
        {
            this.Item = Item;
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                this.IsDisposed = true;
                Item.EndTransaction();
            }
        }
    }

    public class DeferEventsManager
    {
        private Action OnEndDeferEvents { get; }

        private List<DeferEventsTransaction> OpenTransactions { get; set; } = new();
        public bool IsDeferringEvents { get { return OpenTransactions?.Any(x => !x.IsDisposed) == true; } }

        public DeferEventsManager(Action OnEndDeferEvents)
        {
            this.OnEndDeferEvents = OnEndDeferEvents;
        }

        public DeferEventsTransaction DeferEvents()
        {
            DeferEventsTransaction Transaction = new(this);
            OpenTransactions.Add(Transaction);
            return Transaction;
        }

        internal void EndTransaction()
        {
            if (!IsDeferringEvents)
                OnEndDeferEvents?.Invoke();
        }
    }
}
