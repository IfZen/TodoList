using System;

using TodoList.Business.Authentification;

namespace TodoList.Persistence.Tables
{
    public class AccessToken : TableItem<AccessToken, IAccessTokenState>, IAccessTokenState
    {
        public new long?   Id               { get => base.Id; }

        public string      Login            { get; set; }
        public Guid        Guid             { get; set; }
        public eUserRole   Role             { get; set; }
        public DateTime    CreatedDateUTC   { get; set; }
        public DateTime    EndOfValidityUTC { get; set; }
        public string      InfoOrReason     { get; set; }

        public override void CopyFrom(IAccessTokenState state)
        {
            base.CopyFrom(state);

            Login = state.Login;
            Guid = state.Guid;
            Role = state.Role;
            CreatedDateUTC = state.CreatedDateUTC;
            EndOfValidityUTC = state.EndOfValidityUTC;
            InfoOrReason = state.InfoOrReason;
        }
    }
}
