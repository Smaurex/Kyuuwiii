namespace Kyuuwiii.Models
{
    public static class Session
    {
        public static User? CurrentUser { get; private set; }

        public static bool IsLoggedIn => CurrentUser != null;
        public static bool IsStudent => CurrentUser?.IsStudent ?? false;
        public static bool IsManager => CurrentUser?.IsManager ?? false;
        public static bool IsAdmin => CurrentUser?.IsAdmin ?? false;

        public static void Login(User user) => CurrentUser = user;
        public static void Logout() => CurrentUser = null;
    }
}
