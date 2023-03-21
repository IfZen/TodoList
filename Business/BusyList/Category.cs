using System;

using TodoList.Business.Exceptions;

using TodoList.Business.Authentification;
using TodoList.Business.Ports;


namespace TodoList.Business.BusyList
{
    public class Category : IDomainObject<Category>
    {
        public   long     Id    { get => State.Id; }
        public   string   Name  { get => State.Name; set => State.Name = value; }

        internal User     Owner { get => _service.GetUser(State.OwningUserName); set => State.OwningUserName = value?.Login; }

        public bool       IsBuiltIn => State.OwningUserName == null;

        internal Category(Service owningService, ICategoryState state)
        {
            _service = owningService;
            State = state;
        }
        internal readonly Service _service;
        internal ICategoryState State { get; }

        public void Save()
        {
            if (Owner == null)
                throw new BusinessException("This category cannot be edited!", null);
            _service.EnsureUserAndTokenAreValid();
            _service._persistence.Categories.Upsert(State);
        }
    }

    public interface ICategoryState : IPersistenceState<ICategoryState>
    {
        string OwningUserName { get; set; }
        string Name           { get; set; }
    }
}
