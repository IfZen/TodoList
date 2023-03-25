using System.Security.Claims;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

using TodoList.Business.Exceptions;

using TodoList.WebUI.Blazor.Helpers;

using TodoList.Business.Authentification;
using UserService = TodoList.Business.Authentification.Service;
using System.Diagnostics;

namespace TodoList.WebUI.Blazor.Authentication
{
    // TO read : https://stackoverflow.com/questions/62529029/customizing-the-authenticationstateprovider-in-blazor-server-app-with-jwt-token
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        public CustomAuthenticationStateProvider(ProtectedSessionStorage sessionStorage, UserService userService, ExceptionPolicy exPolicy)
        {
            _sessionStorage = sessionStorage;
            _userService = userService;
            _exPolicy = exPolicy;
        }
        readonly ProtectedSessionStorage _sessionStorage;
        readonly UserService _userService;
        readonly ExceptionPolicy _exPolicy;
        readonly ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        public async Task<UserSession> GetCurrentSession()
        {
            try
            {
                var userSessionStorageResult = await _sessionStorage.GetAsync<UserSession>(nameof(UserSession));
                var userSession = userSessionStorageResult.Success ? userSessionStorageResult.Value : null;
                // TODO: if user is in the middle of something how to prevent loose of data when authenticating again ?
                return userSession ?? throw new UserUnderstandableException("Your session ends, please login again!", null);
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public async Task<AccessToken> GetToken(UserSession session = null)
        {
            session ??= await GetCurrentSession();
            return _userService.GetToken(session.Login, session.TokenGuid);
        }

        public User TryCreateNewUser(string login)
        {
            return _userService.NewUser(login, true);
        }

        public async Task Authenticate(string userName, string password)
        {
            await CheckAuthenticate(userName, password);
        }

        async Task<AccessToken> CheckAuthenticate(string userName, string password)
        {
            var token = _userService.Authenticate(userName, password);
            await UpdateAuthenticationState(token);
            return token;
        }

        public async Task UpdateAuthenticationState(AccessToken token)
        {
            ClaimsPrincipal claimsPrincipal;

            if (token == null)
            {
                await _sessionStorage.DeleteAsync(nameof(UserSession));
                claimsPrincipal = _anonymous;
            }
            else
            {
                var userSession = new UserSession() { Login = token.Login, Role = token.Role, TokenGuid = token.Guid };
                await _sessionStorage.SetAsync(nameof(UserSession), userSession);
                claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>()
                {
                    new Claim(ClaimTypes.Name, token.Login),
                    new Claim(ClaimTypes.Role, token.Role.ToString()),
                }));
            }

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));

            if (token != null)
            {
                var session = await GetCurrentSession();
                Debug.Assert(session.Login == token.Login);
                Debug.Assert(session.Role == token.Role);
                Debug.Assert(session.TokenGuid == token.Guid);
            }
        }


        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var userSession = await GetCurrentSession();
                var claimsPrincipal = userSession == null ? _anonymous : new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>()
                {
                    new Claim(ClaimTypes.Name, userSession.Login),
                    new Claim(ClaimTypes.Role, userSession.Role.ToString()),
                }, "CustomAuth"));
                return await Task.FromResult(new AuthenticationState(claimsPrincipal));
            }
            // if user try to modify the encrypted user session details from the protected sessions storage
            // if user modify the encrypted value, the application will not be able to fetch the user session from it
            catch
            {
                return await Task.FromResult(new AuthenticationState(_anonymous));
            }
        }

        public async Task<User> GetCurrentUser()
        {
            var userSession = await GetCurrentSession();
            return _userService.GetUser(userSession.Login);
        }

        public Task RegisterNewUser(User user, string password, string confirmedPassword)
        {
            _exPolicy.WrapTechnicalError(() => _userService.Insert(user, password, confirmedPassword));
            return CheckAuthenticate(user.Login, password);
        }
        public async Task UpdateAccount(User user, string password = null, string newPassword = null, string newPasswordConfirmed = null)
        {
            var userToken = await GetToken();

            // Update access token
            if (!string.IsNullOrEmpty(password + newPassword + newPasswordConfirmed))
                await _exPolicy.WrapTechnicalError(CheckAuthenticate(user.Login, password));

            _exPolicy.WrapTechnicalError(() => _userService.UpdateUser(user, userToken, newPassword, newPasswordConfirmed));
        }
    }
}
