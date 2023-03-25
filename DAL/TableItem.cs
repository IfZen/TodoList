using System;

using TodoList.Business.Ports;


namespace TodoList.Persistence
{
    public abstract class TableItem
    {
        public long Id
        {
            get
            {
                if (_id == null)
                    EnsureIdIsGenerated();
                return _id.Value;
            }
        }
        public bool HasId => _id != null;
        protected internal long? _id;
        internal void EnsureIdIsGenerated()
        {
            if (_id == null)
                lock (Creator)
                    if (_id == null)
                        _id = Creator.Service._seeds.GetNewIdFor(Creator);
        }

        internal abstract Table Creator { get; }
        public IPersistenceService PersistenceService { get => Creator.Service; }

        public override int GetHashCode()
        {
            return _id?.GetHashCode() ?? base.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            return obj is TableItem item && _id.HasValue && _id == item._id ||
                   ReferenceEquals(this, obj);
        }
    }

    public abstract class TableItem<TTableItem, ITableItem> : TableItem, IPersistenceState<ITableItem>
        where TTableItem : TableItem<TTableItem, ITableItem>, ITableItem, new()
        where ITableItem : class, IPersistenceState<ITableItem>
    {
        public virtual void CopyFrom(ITableItem state)
        {
            if (((TTableItem)state)._id.HasValue)
                _id = state.Id;
        }

        internal override Table Creator { get => CreatorTyped; }
        internal Table<TTableItem, ITableItem> CreatorTyped { get; set; }

        public TTableItem Clone()
        {
            var cloned = CreatorTyped.Create();
            cloned.CopyFrom((TTableItem)this);
            return cloned;
        }
        ITableItem IPersistenceState<ITableItem>.Clone()
        {
            return Clone();
        }
    }
}
