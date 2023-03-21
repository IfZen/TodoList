using System;

using TodoList.Business.Ports;

namespace TodoList.Business.BusyList
{
    public partial interface IPersistenceService : Ports.IPersistenceService
    {
        IRepository<IWorkItemState> WorkItems { get; }
        IRepository<ICategoryState> Categories { get; }
        IRepository<IWorkItemCategoryState> WorkItemCategories { get; }
    }
}
