using System;

using TodoList.Business.Authentification;

namespace TodoList.Persistence.Tables
{
    public class Password : TableItem<Password, IPasswordState>, IPasswordState
    {
        public new long? Id               { get => base.Id; }

        public string    Login            { get; set; }
        public string    HashedPassword   { get; set; }
        public DateTime  CreatedDateUTC   { get; set; }
        public DateTime? EndOfValidityUTC { get; set; }

        public override void CopyFrom(IPasswordState state)
        {
            base.CopyFrom(state);
            Login = state.Login;
            HashedPassword = state.HashedPassword;
            CreatedDateUTC = state.CreatedDateUTC;
            EndOfValidityUTC = state.EndOfValidityUTC;
        }
    }
}
