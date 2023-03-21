using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace TodoList.Business.Ports
{
    public partial interface IPersistenceService
    {
        ITransaction BeginTransaction();

        IList<T> Query<T>(Expression<Func<T>> complexQuery, int? offset = null, int? limit = null);
    }

    public interface IPersistenceState<IState> where IState : class, IPersistenceState<IState>
    {
        long Id { get; }
        bool HasId { get; }

        IPersistenceService PersistenceService { get; }
        void CopyFrom(IState state);
        IState Clone();
    }

    public enum eSyncStatus
    {
        // Not yet existing in database but has an id so link can be done
        Added,
        // Marked for deletion in next Save
        Deleted,
        // The classic Dirty state
        Modified,
        // The classic Not Dirty state
        Unchanged,
        // The classic Not Dirty state
        Detached
    }

    /// <summary> TODO : https://www.entityframeworktutorial.net/code-first/simple-code-first-example.aspx
    /// All operations are supposed atomic
    /// <para>
    /// The "uniquenessOn" arguments in methods below are all about this great debate: https://softwareengineering.stackexchange.com/questions/386671/how-to-handle-business-rules-that-are-uniqueness-constraints
    /// To sumarise it : 
    /// - It is to business domain to enforce uniqueness on his properties
    /// - and to persistence model to modelize it as a real constraint (but as an _optimization_ !)
    /// This signature allows domain model to express it easily, for example:
    /// <code>Insert(a_new_user, u => u.Login)</code>
    /// or
    /// <code>Insert(a_new_user, u => u.Login, u => u.Email)</code>
    /// It does not allow complex unique key for now 
    /// (for example a couple of value must bunique)
    /// But this interface could evolve
    /// </para>
    /// </summary>
    public interface IRepository<IState>
        where IState : class, IPersistenceState<IState>
    {
        List<IState> Get(Func<IState, bool> predicate = null);
        IState GetSingle(Func<IState, bool> predicate);
        IState GetById(long id);
        int Count(Func<IState, bool> predicate);

        /// <summary>
        /// Factory to create a new record in memory (but not already inserted!)
        /// </summary>
        IState Create();

        /// <summary>
        /// Insert or update item(s) by checking constraints, and fill <see cref="IState.Id"/> if not already set
        /// </summary>
        void Upsert(IState item, params Expression<Func<IState, object>>[] uniquenessOn);
        void Upsert(IEnumerable<IState> items, params Expression<Func<IState, object>>[] uniquenessOn);

        void Delete(IState item);
        void Delete(IEnumerable<IState> items);
        void Delete(Func<IState, bool> predicate);
        void Delete(IEnumerable<long> ids);
    }


    public interface ITransaction : IDisposable
    {
        void Commit();
        void Rollback();
    }
}
