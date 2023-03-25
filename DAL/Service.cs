using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

using TodoList.Business.Authentification;
using TodoList.Business.BusyList;
using TodoList.Business.Ports;


namespace TodoList.Persistence
{
    public partial class Service : Business.Authentification.IPersistenceService, Business.BusyList.IPersistenceService
    {
        public IRepository<IUserState>             Users => _users;
        public IRepository<IPasswordState>         Passwords => _passwords;
        public IRepository<IAccessTokenState>      AccessTokens => _accessTokens;

        public IRepository<IWorkItemState>         WorkItems => _workItems;
        public IRepository<ICategoryState>         Categories => _categories;
        public IRepository<IWorkItemCategoryState> WorkItemCategories => _workItemCategories;
        

        public Service()
        {
            InitHiLoSeeds(out _seeds);

            _users = new Table<Tables.User, IUserState>(this);
            _passwords = new Table<Tables.Password, IPasswordState>(this);
            _accessTokens = new Table<Tables.AccessToken, IAccessTokenState>(this);

            _workItems = new Table<Tables.WorkItem, IWorkItemState>(this);
            _categories = new Table<Tables.Category, ICategoryState>(this);
            _workItemCategories = new Table<Tables.WorkItemCategory, IWorkItemCategoryState>(this);
        }
        readonly Table<Tables.User, IUserState> _users;
        readonly Table<Tables.Password, IPasswordState> _passwords;
        readonly Table<Tables.AccessToken, IAccessTokenState> _accessTokens;

        readonly Table<Tables.WorkItem, IWorkItemState> _workItems;
        readonly Table<Tables.Category, ICategoryState> _categories;
        readonly Table<Tables.WorkItemCategory, IWorkItemCategoryState> _workItemCategories;


        public IList<T> Query<T>(Expression<Func<T>> complexQuery, int? offset = null, int? limit = null)
        {
            // Require something like linq to Sql
            // We could use something like MicroOrm.Dapper.Repositories
            throw new NotImplementedException();
        }

        public ITransaction BeginTransaction()
        {
            var newTransaction = new Transaction(this, _currentOpenTransactions.Value);
            _currentOpenTransactions.Value = newTransaction;
            return newTransaction;
        }
        readonly ThreadLocal<Transaction> _currentOpenTransactions = new ThreadLocal<Transaction>();
    }
}
