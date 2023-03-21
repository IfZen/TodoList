using System;

namespace TodoList.Business.Authentification
{
    public class User : IDomainObject<User>
    {
        // login is a business Primary Key
        public string       Login           { get => State.Login; }
        public eUserRole    Role            { get => State.Role;         set => State.Role = value; }

        public string       FirstName       { get => State.FirstName;    set => State.FirstName = value; }
        public string       Surname         { get => State.Surname;      set => State.Surname = value; }
        public string       Email           { get => State.Email;        set => State.Email = value; }
        public string       PersonalNote    { get => State.PersonalNote; set => State.PersonalNote = value; }

        internal User(Service owningService, IUserState state)
        {
            _service = owningService;
            State = state;
        }
        readonly Service _service;
        internal IUserState State { get; }


        /// <summary>
        /// A method that clone the current object with all its composites (composition association relationships)
        /// Cloned objects have temporary lifes, they should be forgotten/released after the edition is done (saved or canceled).
        /// </summary>
        public User CloneForEdit()
        {
            var cloned = _service._persistence.Users.Create();
            cloned.CopyFrom(State);
            return new User(_service, cloned);
        }
    }

    public enum eUserRole
    {
        User = 0,
        Administrator = 1 << 31,
    }


    public interface IUserState : Ports.IPersistenceState<IUserState>
    {
        string    Login            { get; set; }
        string    FirstName        { get; set; }
        string    Surname          { get; set; }
        eUserRole Role             { get; set; }

        string    Email            { get; set; }
        string    PersonalNote     { get; set; }
    }

    public interface IPasswordState : Ports.IPersistenceState<IPasswordState>
    {
        string    Login            { get; set; }
        string    HashedPassword   { get; set; }
        DateTime  CreatedDateUTC   { get; set; }
        DateTime? EndOfValidityUTC { get; set; }
    }
}
