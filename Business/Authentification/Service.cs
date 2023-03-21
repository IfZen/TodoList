using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

using TodoList.Business.Exceptions;


namespace TodoList.Business.Authentification
{
    // TODO : we should use SecureString class instead of string for password for methods
    public class Service
    {
        public Service(IPersistenceService persistence)
        {
            _persistence = persistence;
        }
        internal readonly IPersistenceService _persistence;

        public AccessToken Authenticate(string login, string password, TimeSpan? validityDuration = null, string infoOrReason = null)
        {
            var user = GetUserState(login);
            var currentPassword = GetPassword(login);
            if (currentPassword?.HashedPassword != PBKDF2Hash(password))
                 throw new BusinessException("Invalid User Name or Password!", null);
            var token = _persistence.AccessTokens.Create();
            token.Login = login;
            token.Role = user.Role;
            token.CreatedDateUTC = DateTime.UtcNow;
            token.EndOfValidityUTC = token.CreatedDateUTC + (validityDuration ?? TimeSpan.FromHours(1));
            token.InfoOrReason = infoOrReason;
            
            // from https://stackoverflow.com/a/2621603
            // Create Cryptographically Strong Guid
            var rng = new RNGCryptoServiceProvider();
            var data = new byte[16];
            rng.GetBytes(data);
            token.Guid = new Guid(data);

            _persistence.AccessTokens.Upsert(token);

            var result = new AccessToken(this, token, false);
            Debug.Assert(result.IsValid());
            return result;
        }

        public AccessToken GetToken(string login, Guid tokenGuid)
        {
            var state = _persistence.AccessTokens.Create();
            state.Login = login;
            state.Guid = tokenGuid;
            return new AccessToken(this, state, true);
        }

        public User GetUser(string login)
        {
            var state = GetUserState(login);
            if (state == null)
                return null;
            state = state.Clone(); // give a clone so user can edit
            return new User(this, state); 
        }
        IUserState GetUserState(string login)
        {
            if (string.IsNullOrWhiteSpace(login))
                return null;
            return _persistence.Users.GetSingle(ua => ua.Login == login);
        }
        IPasswordState GetPassword(string login, DateTime? atUtc = null)
        {
            atUtc = atUtc ?? DateTime.UtcNow;
            return _persistence.Passwords.GetSingle(p => p.Login == login 
                                                      && p.CreatedDateUTC <= atUtc && atUtc < (p.EndOfValidityUTC ?? DateTime.MaxValue));
        }

        /// <summary>
        /// Create a not valiaded user object to edit.
        /// <see cref="Insert"/> must be called after.
        /// </summary>
        public User NewUser(string login, bool checkLogin = false)
        {
            if (checkLogin)
            {
                ValidateLogin(login);
                ValidateLoginAvailability(login);
            }
            var newUserState = _persistence.Users.Create();
            newUserState.Login = login;
            newUserState.Role = eUserRole.User;
            return new User(this, newUserState);
        }

        /// <summary>
        /// Save a new user with specified data if data are valid
        /// </summary>
        public void Insert(User user, string password, string confirmedPassword)
        {
            // Tests are done here in order of most annoying test to pass to the least one
            // This is to reduce frustration of user to type again all data to find for example an available login
            ValidateLogin(user.Login);
            ValidateLoginAvailability(user.Login);
            ValidatePassword(password, confirmedPassword);
            user.Email = user.Email?.Trim();
            ValidateEmail(user.Email);
            user.FirstName = user.FirstName?.Trim();
            user.Surname = user.Surname?.Trim();
            ValidateNames(user.FirstName, user.Surname);

            using (var tran = _persistence.BeginTransaction())
            {
                var newUserState = _persistence.Users.Create();
                newUserState.CopyFrom(user.State);
                newUserState.Role = eUserRole.User;
                _persistence.Users.Upsert(newUserState, UniqueConstraintOnLogin);

                var passwordState = _persistence.Passwords.Create();
                passwordState.Login = newUserState.Login;
                passwordState.CreatedDateUTC = DateTime.UtcNow;
                passwordState.EndOfValidityUTC = null;
                passwordState.HashedPassword = PBKDF2Hash(password);
                _persistence.Passwords.Upsert(passwordState);

                tran.Commit();

                user.State.CopyFrom(newUserState);
            }
        }
        readonly Expression<Func<IUserState, object>> UniqueConstraintOnLogin = u => u.Login;

        public void UpdateUser(User editedUser, AccessToken authorToken)
        {
            UpdateUser(editedUser, authorToken, null, null);
        }
        public void UpdateUser(User editedUser, AccessToken authorToken, string newPassword, string confirmedNewPassword)
        {
            if (editedUser == null) throw new ArgumentNullException(nameof(editedUser));
            if (authorToken == null) throw new ArgumentNullException(nameof(authorToken));
            if (!authorToken.IsValid()) throw new BusinessException("Please authenticate again!", null);

            #region Checks

            var currentUser = GetUserState(editedUser.Login);
            var author = GetUserState(authorToken.Login);
            if (currentUser.Role != editedUser.Role && author.Role != eUserRole.Administrator)
                throw new BusinessException($"Role {editedUser.Role} can be assigned only by an administrator!", null);

            // Check there is always one admin in db. SO we cannot remove ourselve admin rights.
            // We have to give it to someone else first
            if (author.Role == eUserRole.Administrator &&
                currentUser.Role == eUserRole.Administrator && editedUser.Role != eUserRole.Administrator &&
                _persistence.Users.Count(u => u.Login != editedUser.Login
                                           && u.Role == eUserRole.Administrator) == 0)
                throw new BusinessException($"You cannot remove role {eUserRole.Administrator} to yourself, otherwise no one with same role would exist anymore!", null);

            bool mustUpdatePassword = !string.IsNullOrWhiteSpace(newPassword) || !string.IsNullOrWhiteSpace(confirmedNewPassword);

            // user can optionally enter a new password
            if (mustUpdatePassword)
            {
                ValidatePassword(newPassword, confirmedNewPassword);

                // Verify again password if...
                if (editedUser.Login == authorToken.Login || // an admin or user edit itsef
                    editedUser.Role == eUserRole.Administrator) // or someone else (another admin) is editing an admin
                    if (DateTime.UtcNow > authorToken.CreatedDateUTC + TimeSpan.FromMinutes(5))
                        throw new TechnicalException("Token validity must be < 5 minutes! Ask again current password to User", null);
            }

            // In some few case names can change
            ValidateNames(editedUser.FirstName, editedUser.Surname);
            
            // In case policy change later about what is a good email
            ValidateEmail(editedUser.Email);

            #endregion Checks
            using (var tran = _persistence.BeginTransaction())
            {
                if (mustUpdatePassword)
                {
                    var newPasswordState = _persistence.Passwords.Create();
                    newPasswordState.Login = currentUser.Login;
                    newPasswordState.CreatedDateUTC = DateTime.UtcNow;
                    newPasswordState.EndOfValidityUTC = null;
                    newPasswordState.HashedPassword = PBKDF2Hash(newPassword);

                    var passwordState = _persistence.Passwords.GetSingle(p => p.EndOfValidityUTC == null);
                    passwordState.EndOfValidityUTC = newPasswordState.CreatedDateUTC;

                    _persistence.Passwords.Upsert(passwordState);
                    _persistence.Passwords.Upsert(newPasswordState);
                }

                _persistence.Users.Upsert(editedUser.State);

                tran.Commit();

                // We can see that line as a "memory commit"
                currentUser.CopyFrom(editedUser.State);
            }
        }


        #region Validation

        void ValidatePassword(string password, string confirmation)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new BusinessException("Password cannot be empty!", null);
            if (string.IsNullOrWhiteSpace(confirmation))
                throw new BusinessException("Please confirm password!", null);
            if (!Regex.Match(password, @"^\S{8,}$").Success)
                throw new BusinessException("Password must be 8 characters minimum!", null);
            if (!Regex.Match(password, @"^\S{8,16}$").Success)
                throw new BusinessException("Password must be 16 characters top!", null);
            if (!Regex.Match(password, @"\p{Lu}").Success)
                throw new BusinessException("Password must contains at least one upper letter!", null);
            if (!Regex.Match(password, @"\p{Ll}").Success)
                throw new BusinessException("Password must contains at least one lower letter!", null);
            if (!Regex.Match(password, @"[0-9]").Success)
                throw new BusinessException("Password must contains at least one digit!", null);
            if (Regex.Match(password, @"\s").Success)
                throw new BusinessException("Password cannot contain space!", null);
            if (password != confirmation)
                throw new BusinessException("Confirmation password does not match password!", null);
        }

        void ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new BusinessException("Email cannot be empty!", null);
            if (!reEmailValidation.Match(email).Success)
                throw new BusinessException("Email seems invalid!", null);
        }
        static readonly Regex reEmailValidation = new Regex(@"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9-]+(\.[a-zA-Z0-9-]+)*$", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
        
        void ValidateLogin(string login)
        {
            if (string.IsNullOrWhiteSpace(login))
                throw new BusinessException("Login cannot be empty!", null);
            if (Regex.Match(login, @"^\s").Success)
                throw new BusinessException("Login cannot begins with space!", null);
            if (Regex.Match(login, @"\s\s").Success)
                throw new BusinessException("Login cannot contains two consecutive spaces!", null);
            if (Regex.Match(login, @"\s$").Success)
                throw new BusinessException("Login cannot ends with space!", null);
            if (login.Length < 2) // because: See ValidateNames
                throw new BusinessException("Login must be at least 2 characters!", null);
            if (login.Length > 32)
                throw new BusinessException("Login must be 32 characters long top!", null);
            if (!Regex.Match(login, @"^(\p{Ll}|\p{Lu}|[-_. ])+$").Success)
                throw new BusinessException("Login can be composed only of letters, '_', '-', '.', and internal space are allowed!", null);
        }

        void ValidateLoginAvailability(string login)
        {
            if (_persistence.Users.Count(u => u.Login == login) > 0)
                throw new BusinessException("This login is already taken!", null);
        }

        void ValidateNames(string firstName, string surname)
        {
            // chinese / japanese people can have firstname or name of only one character !
            if (string.IsNullOrEmpty(firstName?.Trim()))
                throw new BusinessException("First name cannot be empty!", null);
            if (string.IsNullOrEmpty(surname?.Trim()))
                throw new BusinessException("Surname cannot be empty!", null);
        }


        static string PBKDF2Hash(string password)
        {
            var input = Encoding.UTF8.GetBytes(password);
            var salt = Encoding.UTF8.GetBytes("TodoList: Security is important but bothers me!");
            var pbkdf2 = new Rfc2898DeriveBytes(input, salt, iterations: 5000);
            return Convert.ToBase64String(pbkdf2.GetBytes(20));
        }

        #endregion
    }
}
