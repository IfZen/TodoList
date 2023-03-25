using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using TodoList.Business.Ports;


namespace TodoList.Persistence
{
    public abstract class Table
    {
        public Table(Service service)
        {
            Service = service;
        }
        public Service Service { get; }

        protected internal abstract string TableName { get; }
    }

    public partial class Table<TTableItem, ITableItem> : Table, IRepository<ITableItem>
        where TTableItem : TableItem<TTableItem, ITableItem>, ITableItem, new()
        where ITableItem : class, IPersistenceState<ITableItem>
    {
        public Table(Service service)
            : base(service)
        {
            if (service._seeds != null)
            {
                var state = service._seeds.Create();
                state.TableName = typeof(TTableItem).Name;
                service._seeds.Upsert(state);
            }
        }
        protected readonly Dictionary<long, TTableItem> _records = new();

        protected internal override string TableName { get { return typeof(TTableItem).Name; } }

        public List<ITableItem> Get(Func<ITableItem, bool> predicate = null)
        {
            lock (_records)
            {
                var results = new List<ITableItem>();
                foreach (var record in _records.Values)
                    if (predicate == null || predicate.Invoke(record))
                        results.Add(record.Clone());
                return results;
            }
        }
        public ITableItem GetById(long id)
        {
            lock (_records)
                return _GetById(id)?.Clone();
        }
        public ITableItem GetSingle(Func<ITableItem, bool> predicate)
        {
            lock (_records)
                return (_records.Values.FirstOrDefault(predicate) as TTableItem)?.Clone();
        }
        public int Count(Func<ITableItem, bool> predicate)
        {
            lock (_records)
                return _records.Values.Count(predicate);
        }

        // This version return the record instance in database.
        // It must be cloned after if retunred to user
        protected TTableItem _GetById(long id)
        {
            lock (_records)
            {
                _records.TryGetValue(id, out TTableItem result);
                return result;
            }
        }

        // note: Very often,
        //       withId is false when called for internally purpose in managing class
        //       withId is true when managing class wants to wrapp the state and return it as a business object
        public TTableItem Create()
        {
            var item = new TTableItem();
            item.CreatorTyped = this;
            return item;
        }
        ITableItem IRepository<ITableItem>.Create() { return Create(); }

        public void Upsert(ITableItem item, params Expression<Func<ITableItem, object>>[] getUniqueKeys)
        {
            Upsert(new[] { item }, getUniqueKeys);
        }
        public virtual void Upsert(IEnumerable<ITableItem> items, params Expression<Func<ITableItem, object>>[] getUniqueKeys)
        {
            var getUniqueKeysCompiled = CompileUniqueKeysDescription(getUniqueKeys);

            // Make a "snapshot" of objects to avoid race condition
            // This is required because we are in a memory implementation of a database and user could still mutate items
            // We can see this line as a serialization / deserialization that occurs when sending to a real database.
            var records = items.Select(it =>
            {
                ((TTableItem)it).EnsureIdIsGenerated();
                var record = Create();
                record.CopyFrom(it);
                return record;
            }).ToList();

            lock (_records)
            {
                CheckUniqueness(getUniqueKeys, getUniqueKeysCompiled, records);

                foreach (var newRecord in records)
                    if (_records.TryGetValue(newRecord.Id, out var record))
                        record.CopyFrom(newRecord); // it is probably best to keep same reference for later
                    else
                        _records.Add(newRecord.Id, newRecord);
            }
        }

        public void Delete(ITableItem item)
        {
            Delete(new[] { item });
        }
        public void Delete(IEnumerable<ITableItem> items)
        {
            Delete(items.Where(item => item.HasId).Select(item => item.Id));
        }
        public void Delete(Func<ITableItem, bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            lock (_records)
            {
                // Make sure predicate does not throw
                var recordsToRemove = _records.Values.Where(predicate).Select(r => r.Id).ToList();
                Delete(recordsToRemove);
            }
        }
        public void Delete(IEnumerable<long> itemIds)
        {
            // Not optimized but hey... we rarely delete anyway
            lock (_records)
                foreach(var id in itemIds)
                    _records.Remove(id);
        }
    }
}
