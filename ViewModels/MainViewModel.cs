using App2.Commands;
using App2.Data;
using App2.Models;
using App2.Services;
using App2.Views;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace App2.ViewModels
{
    public class ActivityItem
    {
        public string Icon { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
    }

    public class MainViewModel : ObservableObject
    {
        private string _currentDate = string.Empty;
        private string _currentTime = string.Empty;
        private string _totalSales = "0 ر.ي";
        private string _totalOrders = "0";
        private string _totalCustomers = "0";
        private string _growthRate = "0%";

        // Cache للـ Views لتجنب التأخر عند الفتح المتكرر
        private readonly Dictionary<string, UserControl> _viewCache = new Dictionary<string, UserControl>();

        public string CurrentDate
        {
            get => _currentDate;
            set => SetProperty(ref _currentDate, value);
        }

        public string CurrentTime
        {
            get => _currentTime;
            set => SetProperty(ref _currentTime, value);
        }

        public string TotalSales
        {
            get => _totalSales;
            set => SetProperty(ref _totalSales, value);
        }

        public string TotalOrders
        {
            get => _totalOrders;
            set => SetProperty(ref _totalOrders, value);
        }

        public string TotalCustomers
        {
            get => _totalCustomers;
            set => SetProperty(ref _totalCustomers, value);
        }

        public string GrowthRate
        {
            get => _growthRate;
            set => SetProperty(ref _growthRate, value);
        }

        public ObservableCollection<ActivityItem> RecentActivities { get; } = new();

        public ICommand AddOrderCommand { get; }
        public ICommand ManageProductsCommand { get; }
        public ICommand ManageCustomersCommand { get; }
        public ICommand ManagePurchaseInvoicesCommand { get; }
        public ICommand AddPurchaseInvoiceCommand { get; }
        public ICommand ManageInventoryCommand { get; }
        public ICommand ReportsCommand { get; }
        public ICommand SettingsCommand { get; }

        private object? _currentView;
        public object? CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public ICommand GoToDashboardCommand { get; }
        private DispatcherTimer? _timer;

        public ICommand ManageAccountsCommand { get; }

        public ICommand ManageSalesInvoicesCommand { get; }
        public ICommand ManageSalesLogCommand { get; }
        public ICommand CustomerStatementCommand { get; }

        public ICommand LogoutCommand { get; }

        public event Action? LogoutRequested;

        public string CurrentUsername => SessionManager.CurrentUser?.Username ?? "مستخدم";
        public string CurrentFullName => SessionManager.CurrentUser?.FullName ?? string.Empty;

        public MainViewModel()
        {
            // Initialize commands
            GoToDashboardCommand = new RelayCommand(ExecuteGoToDashboard);
            AddOrderCommand = new RelayCommand(ExecuteAddOrder);
            ManageProductsCommand = new RelayCommand(ExecuteManageProducts);
            ManageCustomersCommand = new RelayCommand(ExecuteManageCustomers);
            ManagePurchaseInvoicesCommand = new RelayCommand(ExecuteManagePurchaseInvoices);
            AddPurchaseInvoiceCommand = new RelayCommand(ExecuteAddPurchaseInvoice);
            ManageSalesInvoicesCommand = new RelayCommand(ExecuteManageSalesInvoices);
            ManageSalesLogCommand = new RelayCommand(ExecuteManageSalesLog);
            CustomerStatementCommand = new RelayCommand(ExecuteCustomerStatement);
            ManageInventoryCommand = new RelayCommand(ExecuteManageInventory);
            ManageAccountsCommand = new RelayCommand(ExecuteManageAccounts);
            ReportsCommand = new RelayCommand(ExecuteReports);
            SettingsCommand = new RelayCommand(ExecuteSettings);
            LogoutCommand = new RelayCommand(ExecuteLogout);

            // Initialize data
            LoadSampleData();
            UpdateDateTime();

            // Start timer for time updates
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (_, _) => UpdateDateTime();
            _timer.Start();

            // Set default view
            CurrentView = new DashboardView { DataContext = this };

            // Check license status
            CheckLicenseStatus();
        }

        private void CheckLicenseStatus()
        {
            var licenseCheck = LicenseService.ValidateLicense();
            if (licenseCheck.Status == LicenseService.LicenseStatus.NeedsActivation)
            {
                MessageBox.Show(
                    "يرجى تفعيل النظام بإدخال مفتاح الترخيص من الإعدادات\n\nيمكنك إنشاء مفتاح ترخيص من صفحة الإعدادات إذا كنت مسؤولاً",
                    "تنبيه: النظام غير مفعل",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void LoadSampleData()
        {
            var factory = new AppDbContextFactory();
            using var db = factory.CreateDbContext(Array.Empty<string>());

            var today = DateTime.Today;
            var startDate = new DateTime(today.Year, today.Month, 1);
            var endDate = today.AddDays(1).AddTicks(-1);

            // جلب بيانات حقيقية من قاعدة البيانات
            var salesInvoices = db.SalesInvoices
                .Where(s => s.InvoiceDate >= startDate && s.InvoiceDate <= endDate)
                .ToList();

            var totalSales = salesInvoices.Sum(s => s.Total);
            var totalOrders = salesInvoices.Count;
            var totalCustomers = db.Customers.Count();

            TotalSales = $"{totalSales:N2} ر.ي";
            TotalOrders = totalOrders.ToString();
            TotalCustomers = totalCustomers.ToString();
            GrowthRate = "+0%";

            RecentActivities.Clear();

            // إضافة نشاط حقيقي للفواتير الأخيرة
            var recentInvoices = salesInvoices
                .OrderByDescending(s => s.InvoiceDate)
                .Take(5)
                .ToList();

            foreach (var invoice in recentInvoices)
            {
                var customer = db.Customers.FirstOrDefault(c => c.Id == invoice.CustomerId);
                var timeAgo = GetTimeAgo(invoice.InvoiceDate);

                RecentActivities.Add(new ActivityItem
                {
                    Icon = "�",
                    Title = "فاتورة مبيعات",
                    Description = $"فاتورة #{invoice.InvoiceNumber} للعميل {customer?.Name ?? "غير معروف"}",
                    Time = timeAgo
                });
            }

            // إضافة نشاط للمشتريات الأخيرة
            var recentPurchases = db.PurchaseInvoices
                .Where(p => p.InvoiceDate >= startDate && p.InvoiceDate <= endDate)
                .OrderByDescending(p => p.InvoiceDate)
                .Take(3)
                .ToList();

            foreach (var purchase in recentPurchases)
            {
                var timeAgo = GetTimeAgo(purchase.InvoiceDate);

                RecentActivities.Add(new ActivityItem
                {
                    Icon = "📦",
                    Title = "فاتورة مشتريات",
                    Description = $"فاتورة #{purchase.InvoiceNumber} - {purchase.Category ?? "غير مصنف"}",
                    Time = timeAgo
                });
            }
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            var now = DateTime.Now;
            var span = now - dateTime;

            if (span.TotalMinutes < 1)
                return "الآن";
            if (span.TotalMinutes < 60)
                return $"منذ {(int)span.TotalMinutes} دقيقة";
            if (span.TotalHours < 24)
                return $"منذ {(int)span.TotalHours} ساعة";
            if (span.TotalDays < 7)
                return $"منذ {(int)span.TotalDays} يوم";

            return dateTime.ToString("dd/MM/yyyy");
        }

        private void UpdateDateTime()
        {
            CurrentDate = DateTime.Now.ToString("dddd، dd MMMM yyyy", new System.Globalization.CultureInfo("ar-SA"));
            CurrentTime = DateTime.Now.ToString("HH:mm");
        }

        private void ExecuteGoToDashboard(object? parameter)
        {
            CurrentView = GetOrCreateView("Dashboard", () => new DashboardView { DataContext = this });
        }

        private void ExecuteAddOrder(object? parameter)
        {
            // TODO: Navigate to add order page
        }

        private void ExecuteManageProducts(object? parameter)
        {
            // TODO: Navigate to products management page
        }

        private void ExecuteManageCustomers(object? parameter)
        {
            var view = GetOrCreateView("Customers", () => new CustomersView());
            if (view.DataContext is CustomersViewModel viewModel)
            {
                viewModel.OnCustomerStatementRequested -= HandleCustomerStatementRequested;
                viewModel.OnCustomerStatementRequested += HandleCustomerStatementRequested;
            }
            CurrentView = view;
        }

        private void HandleCustomerStatementRequested(Customer? customer)
        {
            var statementView = GetOrCreateView("CustomerStatement", () => new CustomerStatementView());
            if (statementView.DataContext is CustomerStatementViewModel viewModel && customer != null)
            {
                viewModel.SelectedCustomer = customer;
            }
            CurrentView = statementView;
        }

        private void ExecuteManagePurchaseInvoices(object? parameter)
        {
            CurrentView = GetOrCreateView("PurchasesLog", () => new PurchasesLogView());
        }

        private void ExecuteAddPurchaseInvoice(object? parameter)
        {
            CurrentView = GetOrCreateView("PurchaseInvoice", () => new PurchaseInvoiceView());
        }

        private void ExecuteManageSalesInvoices(object? parameter)
        {
            CurrentView = GetOrCreateView("SalesInvoice", () => new App2.Views.SalesInvoiceView());
        }

        private void ExecuteManageSalesLog(object? parameter)
        {
            CurrentView = GetOrCreateView("SalesLog", () => new SalesLogView());
        }

        private void ExecuteCustomerStatement(object? parameter)
        {
            CurrentView = GetOrCreateView("CustomerStatement", () => new CustomerStatementView());
        }

        private void ExecuteManageInventory(object? parameter)
        {
            CurrentView = GetOrCreateView("Inventory", () => new InventoryView());
        }

        private void ExecuteManageAccounts(object? parameter)
        {
            var view = GetOrCreateView("Accounts", () => new AccountsView());
            if (view.DataContext is AccountsViewModel viewModel)
            {
                viewModel.OnAccountStatementRequested -= HandleAccountStatementRequested;
                viewModel.OnAccountStatementRequested += HandleAccountStatementRequested;
            }
            CurrentView = view;
        }

        private void HandleAccountStatementRequested(Account? account)
        {
            var statementView = GetOrCreateView("CustomerStatement", () => new CustomerStatementView());
            if (statementView.DataContext is CustomerStatementViewModel viewModel && account != null)
            {
                viewModel.SelectedAccount = account;
            }
            CurrentView = statementView;
        }

        private void ExecuteReports(object? parameter)
        {
            CurrentView = GetOrCreateView("Reports", () => new ReportsView());
        }

        private void ExecuteSettings(object? parameter)
        {
            CurrentView = GetOrCreateView("Settings", () => new SettingsView());
        }

        private void ExecuteLogout(object? parameter)
        {
            var result = MessageBox.Show(
                "هل أنت متأكد من تسجيل الخروج؟",
                "تسجيل الخروج",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _timer?.Stop();
                SessionManager.Logout();
                LogoutRequested?.Invoke();
            }
        }

        private UserControl GetOrCreateView(string key, Func<UserControl> createView)
        {
            if (!_viewCache.TryGetValue(key, out var view))
            {
                view = createView();
                _viewCache[key] = view;
            }
            return view;
        }
    }
}
