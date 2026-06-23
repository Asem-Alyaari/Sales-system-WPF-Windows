using App2.Commands;
using App2.Data;
using App2.Models;
using App2.Services;
using App2.Views;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace App2.ViewModels
{
    public class LoginViewModel : ObservableObject
    {
        private string _username = string.Empty;
        private string _password = string.Empty;
        private bool _isLoginEnabled;

        public string Username
        {
            get => _username;
            set
            {
                if (SetProperty(ref _username, value))
                {
                    CheckLoginButton();
                }
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                {
                    CheckLoginButton();
                }
            }
        }

        public bool IsLoginEnabled
        {
            get => _isLoginEnabled;
            set => SetProperty(ref _isLoginEnabled, value);
        }

        public ICommand LoginCommand { get; }

        public event Action? LoginSuccessful;

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin);
            CheckLoginButton();
        }

        private void CheckLoginButton()
        {
            IsLoginEnabled = !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
        }

        private bool CanExecuteLogin(object? parameter) => IsLoginEnabled;

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private void ExecuteLogin(object? parameter)
        {
            try
            {
                var factory = new AppDbContextFactory();
                using var db = factory.CreateDbContext(Array.Empty<string>());

                var hashedPassword = HashPassword(Password);

                // التحقق من قاعدة البيانات مع كلمة المرور المشفرة
                var user = db.Users
                    .FirstOrDefault(u => u.Username == Username && u.Password == hashedPassword && u.IsActive);

                if (user != null)
                {
                    // المسؤول يدخل مباشرة دون الحاجة للترخيص
                    if (user.Role != "Admin")
                    {
                        // التحقق من الترخيص للمستخدمين العاديين
                        var licenseCheck = LicenseService.ValidateLicense();

                        // يجب أن يكون الترخيص صالحاً للدخول
                        if (licenseCheck.Status != LicenseService.LicenseStatus.Valid)
                        {
                            if (licenseCheck.Status == LicenseService.LicenseStatus.NeedsActivation)
                            {
                                var licenseWindow = new LicenseActivationView
                                {
                                    Owner = Application.Current.MainWindow
                                };
                                var dialogResult = licenseWindow.ShowDialog();

                                if (dialogResult == true && licenseWindow.ActivationSuccessful)
                                {
                                    // إعادة التحقق من الترخيص بعد التفعيل
                                    var recheck = LicenseService.ValidateLicense();
                                    if (recheck.Status != LicenseService.LicenseStatus.Valid)
                                    {
                                        MessageBox.Show("فشل التحقق من الترخيص بعد التفعيل. يرجى استخدام مفتاح صالح صادر من النظام.", "خطأ",
                                            MessageBoxButton.OK, MessageBoxImage.Error);
                                        return;
                                    }
                                }
                                else
                                {
                                    MessageBox.Show("النظام يحتاج إلى تفعيل. يرجى التواصل مع المسؤول للحصول على مفتاح ترخيص صالح.", "تنبيه",
                                        MessageBoxButton.OK, MessageBoxImage.Warning);
                                    return;
                                }
                            }
                            else
                            {
                                MessageBox.Show(licenseCheck.Message, "خطأ في الترخيص",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                        }
                    }

                    SessionManager.Login(user);
                    LoginSuccessful?.Invoke();
                }
                else
                {
                    MessageBox.Show("اسم المستخدم أو كلمة المرور غير صحيحة", "خطأ في تسجيل الدخول",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تسجيل الدخول: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
