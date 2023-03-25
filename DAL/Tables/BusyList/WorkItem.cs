using System;

using TodoList.Business.BusyList;


namespace TodoList.Persistence.Tables
{
    public class WorkItem : TableItem<WorkItem, IWorkItemState>, IWorkItemState
    {
        public new long? Id { get => base.Id; }

        public string Title          { get; set; }
        public string Description    { get; set; }
        public bool   Done           { get; set; }
        public string OwningLogin    { get; set; }

        public override void CopyFrom(IWorkItemState state)
        {
            base.CopyFrom(state);
            Title = state.Title;
            Description = state.Description;
            Done = state.Done;
            OwningLogin = state.OwningLogin;
        }
    }
}
