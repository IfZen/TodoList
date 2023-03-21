using System;

using TodoList.Business.Authentification;


namespace TodoList.Persistence.Tables
{
    public class User : TableItem<User, IUserState>, IUserState
    {
        public string      Login           { get; set; }
        public eUserRole   Role            { get; set; }
        public string      FirstName       { get; set; }
        public string      Surname         { get; set; }
        public string      Email           { get; set; }
        public string      PersonalNote    { get; set; }

        public override void CopyFrom(IUserState state)
        {
            base.CopyFrom(state);

            Login = state.Login;
            Role = state.Role;
            FirstName = state.FirstName;
            Surname = state.Surname;
            Email = state.Email;
            PersonalNote = state.PersonalNote;
        }
    }
}
