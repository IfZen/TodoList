using System;
using System.Collections.Generic;
using System.Linq;

using TodoList.Business.Authentification;
using TodoList.Business.Ports;


namespace TodoList.Business.BusyList
{
    public class WorkItem : IDomainObject<WorkItem>
    {
        public   long     Id           { get => State.Id; }
        public   string   Title        { get => State.Title;       set => State.Title = value; }
        public   string   Description  { get => State.Description; set => State.Description = value;  }
        public   bool     Done         { get => State.Done;        set => State.Done = value;  }

        internal User     Owner        { get => _service.GetUser(State.OwningLogin); }

        public IReadOnlyCollection<Category> Categories { get { return _service.GetCategoriesOf(this); } }

        internal WorkItem(Service owningService, IWorkItemState state)
        {
            _service = owningService;
            State = state;
        }
        internal readonly Service _service;
        internal IWorkItemState State;

        public void Save()
        {
            _service.EnsureUserAndTokenAreValid();
            _service._persistence.WorkItems.Upsert(State);
        }
    }

    public interface IWorkItemState : IPersistenceState<IWorkItemState>
    {
        string  Title       { get; set; }
        string  Description { get; set; }
        bool    Done        { get; set; }

        string  OwningLogin { get; set; }
    }

    public interface IWorkItemCategoryState : IPersistenceState<IWorkItemCategoryState>
    {
        long   WorkItemId { get; set; }
        long   CategoryId { get; set; }
    }
}
