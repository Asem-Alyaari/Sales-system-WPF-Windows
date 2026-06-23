using App2.Commands;
using App2.Data;
using App2.Services;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace App2.ViewModels
{
    public class SettingsViewModel : ObservableObject
    {
        // ── تغيير اسم المستخدم ──────────────────────────────────
        private string _newUsername = string.Empty;
        public string NewUsername
        {
            get => _newUsername;
            set { SetProperty(ref _newUsername, value); StatusMessage = string.Empty; }
        }

        // ── تغيير كلمة المرور ───────────────────────────────────
        private string _currentPassword = string.Empty;
        private string _newPassword = string.Empty;
        private string _confirmPassword = string.Empty;

        public string CurrentPassword
        {
            get => _currentPassword;
            set { SetProperty(ref _currentPassword, value); StatusMessage = string.Empty; }
        }
        public string NewPassword
        {
            get => _newPassword;
            set { SetProperty(ref _newPassword, value); StatusMessage = string.Empty; }
        }
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set { SetProperty(ref _confirmPassword, value); StatusMessage = string.Empty; }
        }

        // ── رسالة الحالة ────────────────────────────────────────
        private string _statusMessage = string.Empty;
        private bool _isSuccess;

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }
        public bool IsSuccess
        {
            get => _isSuccess;
            set => SetProperty(ref _isSuccess, value);
        }

        // ── معلومات المستخدم الحالي ─────────────────────────────
        public string CurrentUsername => SessionManager.CurrentUser?.Username ?? string.Empty;
        public string CurrentFullName => SessionManager.CurrentUser?.FullName ?? string.Empty;

        // ── الأوامر ─────────────────────────────────────────────
        public ICommand ChangeUsernameCommand { get; }
        public ICommand ChangePasswordCommand { get; }

        public SettingsViewModel()
        {
            NewUsername = CurrentUsername;
            ChangeUsernameCommand = new RelayCommand(ExecuteChangeUsername);
            ChangePasswordCommand = new RelayCommand(ExecuteChangePassword);
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            return Convert.ToBase64String(sha256.ComputeHash(bytes));
        }

        private void ExecuteChangeUsername(object? parameter)
        {
            if (string.IsNullOrWhiteSpace(NewUsername))
            {
                ShowError("اسم المستخدم لا يمكن أن يكون فارغاً");
                return;
            }

            if (NewUsername == CurrentUsername)
            {
                ShowError("اسم المستخدم الجديد مطابق للحالي");
                return;
            }

            try
            {
                var factory = new AppDbContextFactory();
                using var db = factory.CreateDbContext(Array.Empty<string>());

                // التحقق أن الاسم غير مستخدم من قبل
                bool exists = db.Users.Any(u => u.Username == NewUsername && u.Id != SessionManager.CurrentUser!.Id);
                if (exists)
                {
                    ShowError("اسم المستخدم هذا مستخدم بالفعل");
                    return;
                }

                var user = db.Users.Find(SessionManager.CurrentUser!.Id);
                if (user == null) { ShowError("المستخدم غير موجود"); return; }

                user.Username = NewUsername;
                db.SaveChanges();

                // تحديث الجلسة
                SessionManager.CurrentUser!.Username = NewUsername;
                OnPropertyChanged(nameof(CurrentUsername));

                ShowSuccess("تم تغيير اسم المستخدم بنجاح ✓");
            }
            catch (Exception ex)
            {
                ShowError($"خطأ: {ex.Message}");
            }
        }

        private void ExecuteChangePassword(object? parameter)
        {
            if (string.IsNullOrWhiteSpace(CurrentPassword) ||
                string.IsNullOrWhiteSpace(NewPassword) ||
                string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                ShowError("يرجى ملء جميع الحقول");
                return;
            }

            if (NewPassword != ConfirmPassword)
            {
                ShowError("كلمة المرور الجديدة وتأكيدها غير متطابقتين");
                return;
            }

            if (NewPassword.Length < 4)
            {
                ShowError("كلمة المرور يجب أن تكون 4 أحرف على الأقل");
                return;
            }

            try
            {
                var factory = new AppDbContextFactory();
                using var db = factory.CreateDbContext(Array.Empty<string>());

                var user = db.Users.Find(SessionManager.CurrentUser!.Id);
                if (user == null) { ShowError("المستخدم غير موجود"); return; }

                // التحقق من كلمة المرور الحالية
                if (user.Password != HashPassword(CurrentPassword))
                {
                    ShowError("كلمة المرور الحالية غير صحيحة");
                    return;
                }

                user.Password = HashPassword(NewPassword);
                db.SaveChanges();

                // مسح الحقول
                CurrentPassword = string.Empty;
                NewPassword = string.Empty;
                ConfirmPassword = string.Empty;

                ShowSuccess("تم تغيير كلمة المرور بنجاح ✓");
            }
            catch (Exception ex)
            {
                ShowError($"خطأ: {ex.Message}");
            }
        }

        private void ShowSuccess(string msg) { IsSuccess = true; StatusMessage = msg; }
        private void ShowError(string msg) { IsSuccess = false; StatusMessage = msg; }
    }
}
