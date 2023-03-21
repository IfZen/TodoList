using System;

using TodoList.Business.BusyList;


namespace TodoList.Persistence.Tables
{
    public class WorkItemCategory : TableItem<WorkItemCategory, IWorkItemCategoryState>, IWorkItemCategoryState
    {
        public new long? Id          { get => base.Id; }

        public long      WorkItemId  { get; set; }
        public long      CategoryId  { get; set; }

        public override void CopyFrom(IWorkItemCategoryState state)
        {
            base.CopyFrom(state);
            WorkItemId = state.WorkItemId;
            CategoryId = state.CategoryId;
        }
    }
}
