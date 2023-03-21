using System;

using TodoList.Business.Exceptions;

using TodoList.Business.Ports;

namespace TodoList.Business.Authentification
{
    public class AccessToken : IDomainObject<AccessToken>
    {
        public string    Login            { get => State.Login;            }
        public Guid      Guid             { get => State.Guid;             }
        public eUserRole Role             { get { LazyLoadIfNeeded(); return State.Role; } }
        public DateTime  CreatedDateUTC   { get { LazyLoadIfNeeded(); return State.CreatedDateUTC; } }
        public DateTime  EndOfValidityUTC { get { LazyLoadIfNeeded(); return State.EndOfValidityUTC; } }
        public string    InfoOrReason     { get { LazyLoadIfNeeded(); return State.InfoOrReason; } }

        internal AccessToken(Service owningService, IAccessTokenState state, bool needUpdate)
        {
            _service = owningService;
            State = state;
            _needUpdate = needUpdate;
        }
        readonly Service _service;
        internal IAccessTokenState State { get; }
        bool _needUpdate;

        void LazyLoadIfNeeded()
        {
            if (_needUpdate)
            {
                var state = _service._persistence.AccessTokens.GetSingle(tok => tok.Login == Login && tok.Guid == Guid);
                if (state == null)
                    throw new TechnicalException("Token not found in database!", null);
                State.CopyFrom(state);
                _needUpdate = false;
            }
        }

        public bool IsValid()
        {
            LazyLoadIfNeeded();
            if (State.Id == null)
                return false;
            // Check still exist in db (in case an admin removed it)
            // TODO : this would need a cache in a real app
            var state = _service._persistence.AccessTokens.GetById(State.Id);
            if (state == null)
                return false;
            var dt = DateTime.UtcNow;
            return state.Guid == Guid
                && dt >= CreatedDateUTC && dt < EndOfValidityUTC;
        }
        public void EnsureIsValid()
        {
            if (!IsValid())
                throw new BusinessException("You are not allowed to perform this operation. Please authenticate again", null);
        }
        public void EnsureIsValidForAccessing(User accessedUser)
        {
            EnsureIsValidForAccessing(accessedUser?.Login);
        }
        public void EnsureIsValidForAccessing(string login)
        {
            if (string.IsNullOrWhiteSpace(login))
                throw new ArgumentNullException(nameof(login));
            EnsureIsValid();
            if (Role != eUserRole.Administrator && Login != login)
                throw new BusinessException("You are not allowed to access other user data", null);
        }
    }

    public interface IAccessTokenState : IPersistenceState<IAccessTokenState>
    {
        string      Login            { get; set; }
        Guid        Guid             { get; set; }
        eUserRole   Role             { get; set; }
        DateTime    CreatedDateUTC   { get; set; }
        DateTime    EndOfValidityUTC { get; set; }
        string      InfoOrReason     { get; set; }
    }
}
