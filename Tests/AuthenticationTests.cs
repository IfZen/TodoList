using System;
using System.Linq;

using NUnit.Framework;

using TodoList.Business.Exceptions;


namespace TodoList.Tests
{
    [TestFixture]
    public class AuthenticationTests
    {
        /// <summary>
        /// Create a default database with an admin user
        /// </summary>
        public static (Persistence.Service, Business.Authentification.User admin, string password)  CreateDefaultDatabase(string login = "admin", string password = "Admin1234;")
        {
            var db = new Persistence.Service();
            
            // Create admin through temporary service (let's consider it works)
            var userService = new Business.Authentification.Service(db);
            var admin = TryCreateUser(userService, login, password, "admin@test.com", "Mickael", "Labau");

            // Then go to db to get the state and initialize role manually
            var adminState = db.Users.Get().Single();
            adminState.Role = Business.Authentification.eUserRole.Administrator;
            db.Users.Upsert(adminState);

            return (db, admin, password);
        }

        static Business.Authentification.User TryCreateUser(Business.Authentification.Service userService, string login, string password, string email, string firstName = "John", string surname = "Doe", string confirmPassword = null)
        {
            var user = userService.NewUser(login);
            user.Email = email;
            user.FirstName = firstName;
            user.Surname = surname;
            userService.Insert(user, password, confirmPassword ?? password);
            return user;
        }

        [TestCase(null, TestName = "1) Initial database works/is enough")]
        public void DatabaseCreationWorks(object _)
        {
            var (db, adminUser, adminPassword) = CreateDefaultDatabase();
            var userService = new Business.Authentification.Service(db);

            Assert.Throws<BusinessException>(() => userService.Authenticate("NonExistingUser", ""));
            Assert.Throws<BusinessException>(() => userService.Authenticate(adminUser.Login, "bad_password"));

            var admin = userService.Authenticate(adminUser.Login, adminPassword);
            Assert.AreEqual(admin.Role, Business.Authentification.eUserRole.Administrator);
        }


        [TestCase(null, TestName = "2) User Creation")]
        public void UserCreation(object _)
        {
            var (db, adminUser, adminPassword) = CreateDefaultDatabase();
            var userService = new Business.Authentification.Service(db);

            var validPassword = ";abcDEF123";
            var validEmail = "test@test.com";
            Assert.Throws<BusinessException>(message: "Throws because mistake in password confirmation!",
                                             code:() => TryCreateUser(userService, "toto", validPassword, validEmail, confirmPassword: validPassword + "typo Error"));
            Assert.Throws<BusinessException>(message: "Throws because password is too short!", 
                                             code:() => TryCreateUser(userService, "toto", validPassword.Remove(7), validEmail));
            Assert.Throws<BusinessException>(message: "login invalid!",
                                             code: () => TryCreateUser(userService, "     ", validPassword, validEmail));
            Assert.Throws<BusinessException>(message: "login too short!",
                                             code: () => TryCreateUser(userService, "x", validPassword, validEmail));
            Assert.Throws<BusinessException>(message: "login too long!",
                                             code: () => TryCreateUser(userService, new string('a', 33), validPassword, validEmail));

            var user = TryCreateUser(userService, "user", validPassword, validEmail);
            Assert.AreEqual(user.Login, "user");
            Assert.AreEqual(user.Role, Business.Authentification.eUserRole.User);
            Assert.AreEqual(user.FirstName, "John");
            Assert.AreEqual(user.Surname, "Doe");
            Assert.AreEqual(user.PersonalNote, null);
            Assert.AreEqual(user.Email, validEmail);

            var token = userService.Authenticate("user", validPassword);
            Assert.AreEqual(token.Role, Business.Authentification.eUserRole.User);
            Assert.IsTrue(token.IsValid());
        }

        [TestCase(null, TestName = "3) Changing Role Of Users")]
        public void ChangingRoleOfUsers(object _)
        {
            var (db, adminUser, adminPassword) = CreateDefaultDatabase();
            var userService = new Business.Authentification.Service(db);

            // Admin try to renounce hir admin right
            var adminToken = userService.Authenticate(adminUser.Login, adminPassword);
            
            adminUser.Role = Business.Authentification.eUserRole.User;
            Assert.Throws<BusinessException>(() => userService.UpdateUser(adminUser, adminToken),
                                             "Cannot remove admin because we are the only one / last one!");
            adminUser = userService.GetUser(adminUser.Login); // refresh
            Assert.AreEqual(adminUser.Role, Business.Authentification.eUserRole.Administrator,
                            "Role should has been restored, database state should have not been altered by update attemp!");

            /*** So we create a user, give him admin right, and try again to renounce admin's right ***/
            var validPassword = ";abcDEF123";
            var user = TryCreateUser(userService, "user", validPassword, "test@test.com");
            Assert.AreEqual(user.Role, Business.Authentification.eUserRole.User);
            var userToken = userService.Authenticate(user.Login, validPassword);

            user.Role = Business.Authentification.eUserRole.Administrator;
            Assert.Throws<BusinessException>(() => userService.UpdateUser(user, userToken), "Cannot set admin role like that !");

            // Only if admin is author of the change it is ok!
            user.Role = Business.Authentification.eUserRole.Administrator;
            userService.UpdateUser(user, adminToken);
            user = userService.GetUser(user.Login);
            Assert.AreEqual(user.Role, Business.Authentification.eUserRole.Administrator);

            // Now admin can renounce his right
            adminUser.Role = Business.Authentification.eUserRole.User;
            userService.UpdateUser(adminUser, adminToken);
            adminUser = userService.GetUser(adminUser.Login); // refresh
            Assert.AreEqual(adminUser.Role, Business.Authentification.eUserRole.User);
        }
    }
}
