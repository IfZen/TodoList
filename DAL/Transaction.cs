using System;
using System.Threading;

using TodoList.Business.Ports;


namespace TodoList.Persistence
{
    class Transaction : ITransaction
    {
        internal Transaction(Service service, Transaction parentTransaction)
        {
            _service = service;
            _parentTransaction = parentTransaction;

            if (_parentTransaction == null)
                Monitor.Enter(_service);
        }
        readonly Service _service;
        readonly Transaction _parentTransaction;

        public void Dispose()
        {
            if (_parentTransaction == null)
                Monitor.Exit(_service);
        }
        void IDisposable.Dispose() { Dispose(); }

        public void Commit()
        {
            // TODO
        }

        public void Rollback()
        {
            // TODO
        }
    }
}
