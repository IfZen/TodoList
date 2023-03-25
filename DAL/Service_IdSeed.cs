using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

using TodoList.Business.Ports;


namespace TodoList.Persistence
{
    public partial class Service
    {
        void InitHiLoSeeds(out SeedTable _seeds)
        {
            _seeds = new SeedTable(this);
            _seeds.BootstrapInitialize();
        }
        internal readonly SeedTable _seeds;

        public class SeedTable : Table<IdSeed, IIdSeed>
        {
            public SeedTable(Service service)
                : base(service)
            {
            }
            internal void BootstrapInitialize()
            {
                // bootstrap the id generator by doing things in not usual order
                var state = Create();
                state.TableName = typeof(IdSeed).Name;
                Debug.Assert(!state.HasId);
                _records.Add(1, state);
                _recordByNames.Add(TableName, state);
                state.EnsureIdIsGenerated();
                Debug.Assert(state.Id == 1);
            }
            readonly Dictionary<string, IdSeed> _recordByNames = new Dictionary<string, IdSeed>();

            internal long GetNewIdFor(Table table)
            {
                lock (_recordByNames)
                {
                    var record = _recordByNames[table.TableName];
                    record.Seed += 1;
                    return record.Seed;
                }
            }

            public override void Upsert(IEnumerable<IIdSeed> items, params Expression<Func<IIdSeed, object>>[] getUniqueKeys)
            {
                base.Upsert(items, getUniqueKeys);
                var addedRecord = _GetById(items.Single().Id);
                lock (_recordByNames)
                    _recordByNames[addedRecord.TableName] = addedRecord;
            }
        }

        public interface IIdSeed : IPersistenceState<IIdSeed>
        {
            string TableName { get; set; }
            long   Seed      { get; set; }
        }

        public class IdSeed : TableItem<IdSeed, IIdSeed>, IIdSeed
        {
            public string TableName { get; set; }
            public long   Seed      { get; set; }

            public override void CopyFrom(IIdSeed state)
            {
                base.CopyFrom(state);
                TableName = state.TableName;
                Seed = state.Seed;
            }
        }
    }
}
