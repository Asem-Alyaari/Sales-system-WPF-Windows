using System.Windows;
using App2.ViewModels;
using App2.Views;
using App2.Data;
using App2.Services;
using Microsoft.EntityFrameworkCore;

namespace App2
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // تهيئة قاعدة البيانات وإنشاء المستخدم الافتراضي
            InitializeDatabase();

            // منع WPF من إغلاق التطبيق تلقائياً عند إغلاق نافذة الدخول
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            ShowLoginLoop();
        }

        private void InitializeDatabase()
        {
            try
            {
                var factory = new AppDbContextFactory();
                using var db = factory.CreateDbContext(Array.Empty<string>());
                db.Database.EnsureCreated();
                
                // إضافة حقل تاريخ الانتهاء لجدول التراخيص إذا لم يكن موجوداً
                try
                {
                    db.Database.ExecuteSqlRaw("ALTER TABLE Licenses ADD COLUMN ExpiryDate TEXT NULL;");
                }
                catch { /* العمود موجود بالفعل */ }

                // إنشاء جدول مفاتيح التراخيص المصدرة إذا لم يكن موجوداً
                try
                {
                    db.Database.ExecuteSqlRaw(@"
                        CREATE TABLE IF NOT EXISTS IssuedKeys (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            LicenseKey TEXT NOT NULL,
                            ExpiryDate TEXT NOT NULL,
                            IsUsed INTEGER NOT NULL DEFAULT 0,
                            CreatedDate TEXT NOT NULL
                        );");
                }
                catch { }

                DatabaseSeeder.SeedDefaultUser(db);
            }
            catch
            {
                // Ignore errors during database initialization
            }
        }

        private bool CheckLicense()
        {
            var licenseCheck = LicenseService.ValidateLicense();

            if (licenseCheck.Status == LicenseService.LicenseStatus.Valid)
            {
                return true;
            }

            if (licenseCheck.Status == LicenseService.LicenseStatus.NeedsActivation)
            {
                // لا يسمح بالدخول بدون ترخيص صالح
                // يجب على المستخدم تفعيل الترخيص أولاً
                MessageBox.Show("يرجى تفعيل النظام باستخدام مفتاح ترخيص صادر من النظام قبل الدخول", "تنبيه", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (licenseCheck.Status == LicenseService.LicenseStatus.InvalidDevice)
            {
                MessageBox.Show(licenseCheck.Message, "خطأ في الترخيص", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (licenseCheck.Status == LicenseService.LicenseStatus.DatabaseError)
            {
                MessageBox.Show(licenseCheck.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // حالة انتهاء صلاحية الترخيص
            if (licenseCheck.Status == LicenseService.LicenseStatus.InvalidLicenseKey)
            {
                MessageBox.Show(licenseCheck.Message, "انتهت صلاحية الترخيص", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return false;
        }

        private void ShowLoginLoop()
        {
            while (true)
            {
                // التحقق من الترخيص قبل عرض نافذة تسجيل الدخول
                if (!CheckLicense())
                {
                    // إذا فشل التحقق من الترخيص، حاول عرض نافذة التفعيل
                    var licenseWindow = new Views.LicenseActivationView();
                    var licenseResult = licenseWindow.ShowDialog();

                    if (licenseResult != true || !licenseWindow.ActivationSuccessful)
                    {
                        // إذا فشل التفعيل، أغلق التطبيق
                        Shutdown();
                        return;
                    }

                    // إعادة التحقق من الترخيص بعد التفعيل
                    if (!CheckLicense())
                    {
                        MessageBox.Show("فشل التحقق من الترخيص بعد التفعيل. يرجى استخدام مفتاح صالح صادر من النظام.", "خطأ",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        Shutdown();
                        return;
                    }
                }

                // عرض نافذة تسجيل الدخول
                var loginWindow = new LoginView();
                var loginResult = loginWindow.ShowDialog();

                if (loginResult == true)
                {
                    // تسجيل دخول ناجح — فتح النافذة الرئيسية
                    var mainWindow = new MainWindow();
                    var viewModel = new MainViewModel();
                    mainWindow.DataContext = viewModel;

                    bool loggedOut = false;
                    viewModel.LogoutRequested += () =>
                    {
                        loggedOut = true;
                        mainWindow.Close();
                    };

                    mainWindow.ShowDialog();

                    if (loggedOut)
                        continue;   // أعد عرض شاشة الدخول
                    else
                        break;      // أغلق النافذة الرئيسية يدوياً → خروج من التطبيق
                }
                else
                {
                    break; // ضغط إلغاء في شاشة الدخول → خروج
                }
            }

            Shutdown();
        }
    }
}

