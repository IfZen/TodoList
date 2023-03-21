using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using TodoList.Business.Exceptions;
using TodoList.Persistence.Tools;


namespace TodoList.Persistence
{
    public partial class Table<TTableItem, ITableItem>
    {
        List<Func<object, object>> CompileUniqueKeysDescription(Expression<Func<ITableItem, object>>[] getUniqueKeys)
        {
            return getUniqueKeys.Select(getUniqueKey => _compiled.GetOrAdd(getUniqueKey,
                                                        guk =>
                                                        {
                                                            Func<ITableItem, object> compiled = getUniqueKey.Compile();
                                                            return (object obj) => compiled((ITableItem)obj);
                                                        }))
                                .ToList();
        }
        readonly ConcurrentDictionary<object, Func<object, object>> _compiled = new ConcurrentDictionary<object, Func<object, object>>();

        // A NOT optimized way to check for uniqueness
        void CheckUniqueness(Expression<Func<ITableItem, object>>[] getUniqueKeys, // original keys for exception message
                             List<Func<object, object>> getUniqueKeysCompiled, // compiled key to really use
                             IReadOnlyCollection<ITableItem> itemsToConsider)
        {
            for (int i = 0; i < getUniqueKeysCompiled.Count; ++i) // rarely more than one iteration
            {
                var getUniqueKeyCompiled = getUniqueKeysCompiled[i];

                // Later this dictionary could be generated and kept in memory (like an "index" in a real database)
                var recordsByUK = new Dictionary<object, ITableItem>(_records.Count);
                foreach (var item in _records.Values)
                {
                    var key = getUniqueKeyCompiled(item);
                    if (recordsByUK.ContainsKey(key))
                        throw new UniqueConstraintViolatedException($"Database contains initialy multiple items that do not satisfy uniqueness constraint {GetMemberName.For(getUniqueKeys[i])}!" + Environment.NewLine +
                                                                    $"This is true for Item of type {typeof(ITableItem)} with Id {item.Id} and {recordsByUK[key].Id}!",
                                                                    GetMemberName.For(getUniqueKeys[i]), typeof(ITableItem).Name);
                    else
                        recordsByUK[key] = item;
                }

                var itemsToConsiderByUK = new Dictionary<object, ITableItem>(itemsToConsider.Count);
                foreach (var item in itemsToConsider)
                {
                    var key = getUniqueKeyCompiled(item);
                    if (itemsToConsiderByUK.ContainsKey(key))
                        throw new UniqueConstraintViolatedException($"Object with Id {item.Id} cannot be inserted/updated in set of {typeof(ITableItem)}," + Environment.NewLine +
                                                                    $"because it would violate constraints {GetMemberName.For(getUniqueKeys[i])}." + Environment.NewLine +
                                                                    $"The object to insert/update should have a unique value instead of {getUniqueKeyCompiled(item)}!",
                                                                    GetMemberName.For(getUniqueKeys[i]), typeof(ITableItem).Name);
                    else
                        itemsToConsiderByUK[key] = item;
                }

                var itemsToConsiderById = itemsToConsider.Select(r => r.Id).ToHashSet();

                // So now we know two sets have uniqueness constraints checked
                // let's merge second set in first/general set. 
                foreach (var kvp in itemsToConsiderByUK)
                {
                    // if an item with same key already exist in db
                    if (recordsByUK.TryGetValue(kvp.Key, out var existingItem))
                        // it must be an item user is actually upserting 
                        // This handle the case user switch unique key between two objects and he want to upsert them
                        if (!itemsToConsiderById.Contains(existingItem.Id)) 
                            throw new UniqueConstraintViolatedException($"Object with Id {kvp.Value.Id} cannot be inserted/updated in set of {typeof(ITableItem)}," + Environment.NewLine +
                                                                        $"because it would violate constraints {GetMemberName.For(getUniqueKeys[i])}." + Environment.NewLine +
                                                                        $"The object to insert/update should have a unique value instead of {getUniqueKeyCompiled(kvp.Value)}!",
                                                                        GetMemberName.For(getUniqueKeys[i]), typeof(ITableItem).Name);
                }
            }
        }
        public class UniqueConstraintViolatedException : TechnicalException, ITechnicalDbConstraint
        {
            public string KeyName { get; }
            public string EntityName { get; }

            public UniqueConstraintViolatedException(string message, string keyName, string entityName)
                : base(message, null)
            {
                KeyName = keyName;
                EntityName = entityName;
            }
        }
    }
}
