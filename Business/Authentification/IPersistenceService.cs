using System;

using TodoList.Business.Ports;

namespace TodoList.Business.Authentification
{
    public partial interface IPersistenceService : Ports.IPersistenceService
    {
        IRepository<IUserState>        Users        { get; }
        IRepository<IPasswordState>    Passwords    { get; }
        IRepository<IAccessTokenState> AccessTokens { get; }
    }
}
