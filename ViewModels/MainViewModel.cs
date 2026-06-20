using App2.Commands;
using App2.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Input;

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
        private string _totalSales = "0 ر.س";
        private string _totalOrders = "0";
        private string _totalCustomers = "0";
        private string _growthRate = "0%";

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
        private Timer? _timer;

        public ICommand ManageAccountsCommand { get; }

        public ICommand ManageSalesInvoicesCommand { get; }

        public MainViewModel()
        {
            // Initialize commands
            GoToDashboardCommand = new RelayCommand(ExecuteGoToDashboard);
            AddOrderCommand = new RelayCommand(ExecuteAddOrder);
            ManageProductsCommand = new RelayCommand(ExecuteManageProducts);
            ManageCustomersCommand = new RelayCommand(ExecuteManageCustomers);
            ManagePurchaseInvoicesCommand = new RelayCommand(ExecuteManagePurchaseInvoices);
            ManageSalesInvoicesCommand = new RelayCommand(ExecuteManageSalesInvoices);
            ManageInventoryCommand = new RelayCommand(ExecuteManageInventory);
            ManageAccountsCommand = new RelayCommand(ExecuteManageAccounts);
            ReportsCommand = new RelayCommand(ExecuteReports);
            SettingsCommand = new RelayCommand(ExecuteSettings);

            // Initialize data
            LoadSampleData();
            UpdateDateTime();

            // Start timer for time updates
            _timer = new Timer(_ => UpdateDateTime(), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

            // Set default view
            CurrentView = new DashboardView { DataContext = this };
        }

        private void LoadSampleData()
        {
            TotalSales = "125,430 ر.س";
            TotalOrders = "1,234";
            TotalCustomers = "567";
            GrowthRate = "+15.3%";

            RecentActivities.Add(new ActivityItem
            {
                Icon = "💰",
                Title = "طلب جديد",
                Description = "تم استلام طلب من العميل أحمد محمد",
                Time = "منذ 5 دقائق"
            });

            RecentActivities.Add(new ActivityItem
            {
                Icon = "📦",
                Title = "تحديث المخزون",
                Description = "تم تحديث كمية المنتج #1234",
                Time = "منذ 15 دقيقة"
            });

            RecentActivities.Add(new ActivityItem
            {
                Icon = "👥",
                Title = "عميل جديد",
                Description = "تم تسجيل عميل جديد: سارة علي",
                Time = "منذ 30 دقيقة"
            });

            RecentActivities.Add(new ActivityItem
            {
                Icon = "✅",
                Title = "إتمام الطلب",
                Description = "تم إتمام الطلب #5678 بنجاح",
                Time = "منذ ساعة"
            });

            RecentActivities.Add(new ActivityItem
            {
                Icon = "📊",
                Title = "تقرير يومي",
                Description = "تم إنشاء التقرير اليومي تلقائياً",
                Time = "منذ ساعتين"
            });
        }

        private void UpdateDateTime()
        {
            CurrentDate = DateTime.Now.ToString("dddd، dd MMMM yyyy", new System.Globalization.CultureInfo("ar-SA"));
            CurrentTime = DateTime.Now.ToString("HH:mm");
        }

        private void ExecuteGoToDashboard(object? parameter)
        {
            CurrentView = new DashboardView { DataContext = this };
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
            CurrentView = new CustomersView();
        }

        private void ExecuteManagePurchaseInvoices(object? parameter)
        {
            CurrentView = new PurchasesLogView();
        }

        private void ExecuteManageSalesInvoices(object? parameter)
        {
            CurrentView = new App2.Views.SalesInvoiceView();
        }

        private void ExecuteManageInventory(object? parameter)
        {
            CurrentView = new InventoryView();
        }

        private void ExecuteManageAccounts(object? parameter)
        {
            CurrentView = new AccountsView();
        }

        private void ExecuteReports(object? parameter)
        {
            // TODO: Navigate to reports page
        }

        private void ExecuteSettings(object? parameter)
        {
            // TODO: Navigate to settings page
        }
    }
}
