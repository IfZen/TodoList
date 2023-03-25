

using TodoList.Business.Authentification;

namespace TodoList.WebUI.Blazor.Authentication
{
    // This class needs to be serializable!
    // so all components too
    public class UserSession
    {
        public string Login   { get; set; }
        public eUserRole Role { get; set; }
        public Guid TokenGuid { get; set; }
    }
}
