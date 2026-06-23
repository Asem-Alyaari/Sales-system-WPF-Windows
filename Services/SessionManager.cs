using App2.Models;

namespace App2.Services
{
    /// <summary>
    /// مدير الجلسة — يحتفظ ببيانات المستخدم الحالي أثناء التشغيل
    /// </summary>
    public static class SessionManager
    {
        public static User? CurrentUser { get; private set; }

        public static void Login(User user)
        {
            CurrentUser = user;
        }

        public static void Logout()
        {
            CurrentUser = null;
        }

        public static bool IsLoggedIn => CurrentUser != null;

        public static bool IsAdmin => CurrentUser?.Role == "Admin";
    }
}
