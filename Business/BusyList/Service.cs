using System;
using System.Collections.Generic;
using System.Linq;

using TodoList.Business.Exceptions;

using TodoList.Business.Authentification;


namespace TodoList.Business.BusyList
{
    // TODO : we should use SecureString class instead of string for password for methods
    public class Service
    {
        public Service(IPersistenceService persistence, Authentification.Service userService)
        {
            _persistence = persistence;
            _userService = userService;
        }
        internal IPersistenceService _persistence { get; }
        readonly Authentification.Service _userService;

        public User GetUser(string login)
        {
            return _userService.GetUser(login);
        }


        public void Initialize(User user, AccessToken token)
        {
            token.EnsureIsValidForAccessing(user);
            _user = user;
            _token = token;
        }
        User _user;
        AccessToken _token;
        internal void EnsureUserAndTokenAreValid()
        {
            if (_user == null)
                throw new TechnicalException("Service must be initialized!", null);
            _token.EnsureIsValidForAccessing(_user);
        }

        public List<WorkItem> GetWorkItems()
        {
            EnsureUserAndTokenAreValid();

            return _persistence.WorkItems.Get(state => state.OwningLogin == _user.Login)
                               .Select(state => new WorkItem(this, state))
                               .ToList();
        }

        internal List<Category> GetCategoriesOf(WorkItem workItem)
        {
            var catIds = _persistence.WorkItemCategories
                                     .Get(wic => wic.WorkItemId == workItem.Id)
                                     .Select(wic => wic.CategoryId)
                                     .ToList();
            return _persistence.Categories
                               .Get(cat => catIds.Contains(cat.Id))
                               .Select(catState => new Category(this, catState))
                               .ToList();
        }

        public List<Category> GetAllCategories()
        {
            EnsureUserAndTokenAreValid();

            return _persistence.Categories.Get(state => state.OwningUserName == _user.Login || state.OwningUserName == null)
                               .Select(state => new Category(this, state))
                               .ToList();
        }
        public WorkItem NewWorkItem()
        {
            EnsureUserAndTokenAreValid();

            var state = _persistence.WorkItems.Create();
            state.OwningLogin = _user.Login;
            _persistence.WorkItems.Upsert(state);

            return new WorkItem(this, state);
        }
        public Category NewCategory()
        {
            EnsureUserAndTokenAreValid();

            var state = _persistence.Categories.Create();
            state.OwningUserName = _user.Login;
            _persistence.Categories.Upsert(state);

            return new Category(this, state);
        }

        public void AddCategoryTo(Category cat, WorkItem item)
        {
            EnsureUserAndTokenAreValid();

            var alreadyExistingLink = _persistence.WorkItemCategories.GetSingle(wic => wic.WorkItemId == item.Id
                                                                                    && wic.CategoryId == cat.Id);
            if (alreadyExistingLink != null)
                return;

            var wicState = _persistence.WorkItemCategories.Create();
            wicState.WorkItemId = item.Id;
            wicState.CategoryId = cat.Id;
            _persistence.WorkItemCategories.Upsert(wicState);
        }

        public void RemoveCategoryFrom(Category cat, WorkItem item)
        {
            _persistence.WorkItemCategories.Delete(wic => wic.WorkItemId == item.Id
                                                       && wic.CategoryId == cat.Id);
        }

        public void Delete(WorkItem wi)
        {
            using (var tran = _persistence.BeginTransaction())
            {
                // In DDD how can we express we want cascading on delete ?
                // How do we passe information about composition / aggregation between object anyway?
                // So we do it manually for now
                _persistence.WorkItemCategories.Delete(wi => wi.WorkItemId == wi.Id);
                _persistence.WorkItems.Delete(wi.State);
                tran.Commit();
            }
        }

        public void Delete(Category cat)
        {
            if (cat.Owner == null)
                throw new BusinessException("This category is built-in. It cannot be deleted!", null);

            _persistence.WorkItemCategories.Delete(wi => wi.CategoryId == cat.Id);
            _persistence.Categories.Delete(cat.State);
        }
    }
}
