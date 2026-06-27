using System.Windows;
using System.Linq;
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
            // يمكنك إلغاء التعليق على الكود التالي إذا كنت تريد فرض تنسيق معين
            //var numberFormat = new System.Globalization.NumberFormatInfo
            //{
            //    NumberDecimalSeparator = ",",
            //    NumberGroupSeparator = ".",
            //    CurrencyDecimalSeparator = ",",
            //    CurrencyGroupSeparator = ".",
            //    PercentDecimalSeparator = ",",
            //    PercentGroupSeparator = "."
            //};
            
            //var culture = new System.Globalization.CultureInfo("ar-SA");
            //culture.NumberFormat = numberFormat;
            //culture.DateTimeFormat.Calendar = new System.Globalization.GregorianCalendar();
            
            //System.Globalization.CultureInfo.CurrentCulture = culture;
            //System.Globalization.CultureInfo.CurrentUICulture = culture;
            //System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
            //System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;
            
            // التحقق مما إذا كان المطور يريد تشغيل مولد المفاتيح
            if (e.Args.Contains("--gen"))
            {
                var generator = new LicenseGeneratorView();
                generator.Show();
                return;
            }

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
                try
                {
                    db.Database.ExecuteSqlRaw("ALTER TABLE SalesInvoices ADD COLUMN TransferNumber TEXT NULL;");
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

                // إضافة حقول جديدة لجدول تفاصيل المرتجعات
                try
                {
                    db.Database.ExecuteSqlRaw("ALTER TABLE SalesReturnDetails ADD COLUMN MaxReturnQuantityKabba TEXT NOT NULL DEFAULT '0';");
                }
                catch { }
                
                try
                {
                    db.Database.ExecuteSqlRaw("ALTER TABLE SalesReturnDetails ADD COLUMN OriginalUnit TEXT NOT NULL DEFAULT 'Skein';");
                }
                catch { }

                try
                {
                    // إضافة العمود Type إذا لم يكن موجوداً
                    // نستخدم INTEGER لأن الـ Enum في EF Core يتم تخزينه كأرقام افتراضياً
                    db.Database.ExecuteSqlRaw("ALTER TABLE SalesReturns ADD COLUMN Type INTEGER NOT NULL DEFAULT 0;");
                }
                catch 
                {
                    // يتم تجاهل الخطأ في حال كان العمود موجوداً بالفعل
                }
                // إضافة حقل رقم الحوالة لجدول فواتير المبيعات
                try
                {
                    db.Database.ExecuteSqlRaw("ALTER TABLE SalesInvoice ADD COLUMN TransferNumber TEXT NULL;");
                }
                catch { }
                
                // إضافة حقول PaymentMethod و TransferNumber لجدول SalesReturns
                try
                {
                    db.Database.ExecuteSqlRaw("ALTER TABLE SalesReturns ADD COLUMN PaymentMethod INTEGER NOT NULL DEFAULT 0;");
                }
                catch { }
                
                try
                {
                    db.Database.ExecuteSqlRaw("ALTER TABLE SalesReturns ADD COLUMN TransferNumber TEXT NULL;");
                }
                catch { }

                // إضافة حقل IsPosted لجدول PurchaseInvoices
                try
                {
                    db.Database.ExecuteSqlRaw("ALTER TABLE PurchaseInvoices ADD COLUMN IsPosted INTEGER NOT NULL DEFAULT 0;");
                }
                catch { }
                
                // جعل SalesInvoiceDetailId قابل للقيمة null في SalesReturnDetails
                try
                {
                    // SQLite لا يدعم تعديل عمود مباشرة، نحتاج لإنشاء جدول جديد ونقل البيانات
                    db.Database.ExecuteSqlRaw(@"
                        PRAGMA foreign_keys = 0;
                        CREATE TABLE ""ef_temp_SalesReturnDetails"" (
                            ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_SalesReturnDetails"" PRIMARY KEY AUTOINCREMENT,
                            ""ItemName"" TEXT NOT NULL,
                            ""MaxReturnQuantityKabba"" decimal(18,2) NOT NULL,
                            ""OriginalUnit"" TEXT NOT NULL,
                            ""Price"" decimal(18,2) NOT NULL,
                            ""Quantity"" decimal(18,2) NOT NULL,
                            ""SalesInvoiceDetailId"" INTEGER NULL,
                            ""SalesReturnId"" INTEGER NOT NULL,
                            ""ThreadNumber"" TEXT NOT NULL,
                            ""TotalPrice"" decimal(18,2) NOT NULL,
                            ""Unit"" TEXT NOT NULL,
                            CONSTRAINT ""FK_SalesReturnDetails_SalesInvoiceDetails_SalesInvoiceDetailId"" FOREIGN KEY (""SalesInvoiceDetailId"") REFERENCES ""SalesInvoiceDetails"" (""Id"") ON DELETE CASCADE,
                            CONSTRAINT ""FK_SalesReturnDetails_SalesReturns_SalesReturnId"" FOREIGN KEY (""SalesReturnId"") REFERENCES ""SalesReturns"" (""Id"") ON DELETE CASCADE
                        );
                        INSERT INTO ""ef_temp_SalesReturnDetails"" (""Id"", ""ItemName"", ""MaxReturnQuantityKabba"", ""OriginalUnit"", ""Price"", ""Quantity"", ""SalesInvoiceDetailId"", ""SalesReturnId"", ""ThreadNumber"", ""TotalPrice"", ""Unit"")
                        SELECT ""Id"", ""ItemName"", ""MaxReturnQuantityKabba"", ""OriginalUnit"", ""Price"", ""Quantity"", ""SalesInvoiceDetailId"", ""SalesReturnId"", ""ThreadNumber"", ""TotalPrice"", ""Unit"" FROM ""SalesReturnDetails"";
                        DROP TABLE ""SalesReturnDetails"";
                        ALTER TABLE ""ef_temp_SalesReturnDetails"" RENAME TO ""SalesReturnDetails"";
                        PRAGMA foreign_keys = 1;
                        CREATE INDEX ""IX_SalesReturnDetails_SalesInvoiceDetailId"" ON ""SalesReturnDetails"" (""SalesInvoiceDetailId"");
                        CREATE INDEX ""IX_SalesReturnDetails_SalesReturnId"" ON ""SalesReturnDetails"" (""SalesReturnId"");
                    ");
                }
                catch { }
                
                // =============================================
                // إضافة ProductId إلى SalesInvoiceDetails و SalesReturnDetails
                // =============================================
                
                // 1. أولاً: SalesInvoiceDetails
                try
                {
                    // نضيف العمود ProductId ك NULL أولاً
                    db.Database.ExecuteSqlRaw("ALTER TABLE SalesInvoiceDetails ADD COLUMN ProductId INTEGER NULL;");
                    
                    // نملأ ProductId للبيانات القديمة باستخدام ThreadNumber
                    db.Database.ExecuteSqlRaw(@"
                        UPDATE SalesInvoiceDetails
                        SET ProductId = (
                            SELECT Id 
                            FROM Products 
                            WHERE Products.ColorNumber = SalesInvoiceDetails.ThreadNumber
                            LIMIT 1
                        );
                    ");
                    
                    // الآن نضيف الجدول الجديد مع ProductId NOT NULL
                    db.Database.ExecuteSqlRaw(@"
                        PRAGMA foreign_keys = 0;
                        CREATE TABLE ""ef_temp_SalesInvoiceDetails"" (
                            ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_SalesInvoiceDetails"" PRIMARY KEY AUTOINCREMENT,
                            ""ItemName"" TEXT NOT NULL,
                            ""Price"" decimal(18,2) NOT NULL,
                            ""ProductId"" INTEGER NOT NULL,
                            ""Quantity"" decimal(18,2) NOT NULL,
                            ""SalesInvoiceId"" INTEGER NOT NULL,
                            ""ThreadNumber"" TEXT NOT NULL,
                            ""TotalPrice"" decimal(18,2) NOT NULL,
                            ""Unit"" TEXT NOT NULL,
                            CONSTRAINT ""FK_SalesInvoiceDetails_Products_ProductId"" FOREIGN KEY (""ProductId"") REFERENCES ""Products"" (""Id"") ON DELETE RESTRICT,
                            CONSTRAINT ""FK_SalesInvoiceDetails_SalesInvoices_SalesInvoiceId"" FOREIGN KEY (""SalesInvoiceId"") REFERENCES ""SalesInvoices"" (""Id"") ON DELETE CASCADE
                        );
                        INSERT INTO ""ef_temp_SalesInvoiceDetails"" (""Id"", ""ItemName"", ""Price"", ""ProductId"", ""Quantity"", ""SalesInvoiceId"", ""ThreadNumber"", ""TotalPrice"", ""Unit"")
                        SELECT ""Id"", ""ItemName"", ""Price"", COALESCE(""ProductId"", 0), ""Quantity"", ""SalesInvoiceId"", ""ThreadNumber"", ""TotalPrice"", ""Unit"" FROM ""SalesInvoiceDetails"";
                        DROP TABLE ""SalesInvoiceDetails"";
                        ALTER TABLE ""ef_temp_SalesInvoiceDetails"" RENAME TO ""SalesInvoiceDetails"";
                        PRAGMA foreign_keys = 1;
                        CREATE INDEX ""IX_SalesInvoiceDetails_ProductId"" ON ""SalesInvoiceDetails"" (""ProductId"");
                        CREATE INDEX ""IX_SalesInvoiceDetails_SalesInvoiceId"" ON ""SalesInvoiceDetails"" (""SalesInvoiceId"");
                    ");
                }
                catch { }
                
                // 2. ثانياً: SalesReturnDetails
                try
                {
                    // نضيف العمود ProductId ك NULL أولاً
                    db.Database.ExecuteSqlRaw("ALTER TABLE SalesReturnDetails ADD COLUMN ProductId INTEGER NULL;");
                    
                    // نملأ ProductId للبيانات القديمة باستخدام ThreadNumber
                    db.Database.ExecuteSqlRaw(@"
                        UPDATE SalesReturnDetails
                        SET ProductId = (
                            SELECT Id 
                            FROM Products 
                            WHERE Products.ColorNumber = SalesReturnDetails.ThreadNumber
                            LIMIT 1
                        );
                    ");
                    
                    // الآن نضيف الجدول الجديد مع ProductId NOT NULL
                    db.Database.ExecuteSqlRaw(@"
                        PRAGMA foreign_keys = 0;
                        CREATE TABLE ""ef_temp_SalesReturnDetails"" (
                            ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_SalesReturnDetails"" PRIMARY KEY AUTOINCREMENT,
                            ""ItemName"" TEXT NOT NULL,
                            ""MaxReturnQuantityKabba"" decimal(18,2) NOT NULL,
                            ""OriginalUnit"" TEXT NOT NULL,
                            ""Price"" decimal(18,2) NOT NULL,
                            ""ProductId"" INTEGER NOT NULL,
                            ""Quantity"" decimal(18,2) NOT NULL,
                            ""SalesInvoiceDetailId"" INTEGER NULL,
                            ""SalesReturnId"" INTEGER NOT NULL,
                            ""ThreadNumber"" TEXT NOT NULL,
                            ""TotalPrice"" decimal(18,2) NOT NULL,
                            ""Unit"" TEXT NOT NULL,
                            CONSTRAINT ""FK_SalesReturnDetails_Products_ProductId"" FOREIGN KEY (""ProductId"") REFERENCES ""Products"" (""Id"") ON DELETE RESTRICT,
                            CONSTRAINT ""FK_SalesReturnDetails_SalesInvoiceDetails_SalesInvoiceDetailId"" FOREIGN KEY (""SalesInvoiceDetailId"") REFERENCES ""SalesInvoiceDetails"" (""Id"") ON DELETE CASCADE,
                            CONSTRAINT ""FK_SalesReturnDetails_SalesReturns_SalesReturnId"" FOREIGN KEY (""SalesReturnId"") REFERENCES ""SalesReturns"" (""Id"") ON DELETE CASCADE
                        );
                        INSERT INTO ""ef_temp_SalesReturnDetails"" (""Id"", ""ItemName"", ""MaxReturnQuantityKabba"", ""OriginalUnit"", ""Price"", ""ProductId"", ""Quantity"", ""SalesInvoiceDetailId"", ""SalesReturnId"", ""ThreadNumber"", ""TotalPrice"", ""Unit"")
                        SELECT ""Id"", ""ItemName"", ""MaxReturnQuantityKabba"", ""OriginalUnit"", ""Price"", COALESCE(""ProductId"", 0), ""Quantity"", ""SalesInvoiceDetailId"", ""SalesReturnId"", ""ThreadNumber"", ""TotalPrice"", ""Unit"" FROM ""SalesReturnDetails"";
                        DROP TABLE ""SalesReturnDetails"";
                        ALTER TABLE ""ef_temp_SalesReturnDetails"" RENAME TO ""SalesReturnDetails"";
                        PRAGMA foreign_keys = 1;
                        CREATE INDEX ""IX_SalesReturnDetails_ProductId"" ON ""SalesReturnDetails"" (""ProductId"");
                        CREATE INDEX ""IX_SalesReturnDetails_SalesInvoiceDetailId"" ON ""SalesReturnDetails"" (""SalesInvoiceDetailId"");
                        CREATE INDEX ""IX_SalesReturnDetails_SalesReturnId"" ON ""SalesReturnDetails"" (""SalesReturnId"");
                    ");
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
            var currentHardwareId = HardwareIdService.GetHardwareId();
            var factory = new AppDbContextFactory();
            using var db = factory.CreateDbContext(Array.Empty<string>());
            var allLicenses = db.Licenses.ToList();

            if (allLicenses.Count > 0)
            {
                var existingLicense = allLicenses.First();
                if (existingLicense.HardwareId != currentHardwareId)
                {
                    var result = MessageBox.Show(
                        $"تم العثور على ترخيص مرتبط بمعرف جهاز آخر.\nهل تريد نقله إلى هذا الجهاز؟\n\nالمعرف الحالي: {currentHardwareId}\nالمعرف القديم: {existingLicense.HardwareId}",
                        "نقل الترخيص",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        existingLicense.HardwareId = currentHardwareId;
                        existingLicense.MachineName = Environment.MachineName;
                        existingLicense.LastActivatedDate = DateTime.Now;
                        db.SaveChanges();
                        MessageBox.Show("تم نقلة الترخيص إلى هذا الجهاز بنجاح!", "نجاح النقل", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }

            var licenseCheck = LicenseService.ValidateLicense();

            if (licenseCheck.Status == LicenseService.LicenseStatus.Valid)
            {
                return true;
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
