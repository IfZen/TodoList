using System;

using TodoList.Business.BusyList;


namespace TodoList.Persistence.Tables
{
    public class Category : TableItem<Category, ICategoryState>, ICategoryState
    {
        public new long? Id { get => base.Id; }

        public string OwningUserName { get; set; }
        public string Name           { get; set; }

        public override void CopyFrom(ICategoryState state)
        {
            base.CopyFrom(state);
            OwningUserName = state.OwningUserName;
            Name = state.Name;
        }
    }
}
