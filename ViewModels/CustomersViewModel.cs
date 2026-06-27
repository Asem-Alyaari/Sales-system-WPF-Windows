using App2.Commands;
using App2.Data;
using App2.Models;
using App2.Views;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace App2.ViewModels
{
    public class CustomersViewModel : ObservableObject
    {
        private string _searchText = string.Empty;
        private Customer? _selectedCustomer;
        private readonly AppDbContext _dbContext;

        public event Action<Customer?>? OnCustomerStatementRequested;

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                FilterCustomers();
            }
        }

        public Customer? SelectedCustomer
        {
            get => _selectedCustomer;
            set => SetProperty(ref _selectedCustomer, value);
        }

        public ObservableCollection<Customer> Customers { get; } = new();
        public ObservableCollection<Customer> AllCustomers { get; } = new();

        public ICommand AddCustomerCommand { get; }
        public ICommand EditCustomerCommand { get; }
        public ICommand DeleteCustomerCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand CustomerStatementCommand { get; }
        public ICommand MakePaymentCommand { get; }

        public CustomersViewModel()
        {
            var factory = new AppDbContextFactory();
            _dbContext = factory.CreateDbContext(null);

            AddCustomerCommand = new RelayCommand(ExecuteAddCustomer);
            EditCustomerCommand = new RelayCommand(ExecuteEditCustomer, CanExecuteEditDelete);
            DeleteCustomerCommand = new RelayCommand(ExecuteDeleteCustomer, CanExecuteEditDelete);
            RefreshCommand = new RelayCommand(ExecuteRefresh);
            CustomerStatementCommand = new RelayCommand(ExecuteCustomerStatement, CanExecuteCustomerStatement);
            MakePaymentCommand = new RelayCommand(ExecuteMakePayment, CanExecuteEditDelete);

            // لا تقم بتحميل البيانات هنا - سيتم تحميلها من حدث Loaded في الواجهة
        }

        public async System.Threading.Tasks.Task LoadDataAsync()
        {
            await LoadCustomersAsync();
        }

        private void ExecuteMakePayment(object? parameter)
        {
            if (parameter is Customer customer)
            {
                var window = new Views.PaymentReceiptWindow(_dbContext, customer);
                window.ShowDialog();
            }
        }

        private async 
        Task
LoadCustomersAsync()
        {
            try
            {
                var customers = await _dbContext.Customers.ToListAsync();
                AllCustomers.Clear();
                Customers.Clear();

                foreach (var customer in customers)
                {
                    AllCustomers.Add(customer);
                    Customers.Add(customer);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}");
            }
        }

        private void FilterCustomers()
        {
            Customers.Clear();

            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? AllCustomers
                : AllCustomers.Where(c =>
                    c.Name.Contains(SearchText) ||
                    c.Phone.Contains(SearchText));

            foreach (var customer in filtered)
            {
                Customers.Add(customer);
            }
        }

        private bool CanExecuteEditDelete(object? parameter)
        {
            return SelectedCustomer != null;
        }

        private bool CanExecuteCustomerStatement(object? parameter)
        {
            return SelectedCustomer != null;
        }

        private void ExecuteCustomerStatement(object? parameter)
        {
            OnCustomerStatementRequested?.Invoke(SelectedCustomer);
        }

        private void ExecuteAddCustomer(object? parameter)
        {
            var dialog = new CustomerDialog();
            if (dialog.ShowDialog() == true)
            {
                var newCustomer = dialog.Customer;
                if (newCustomer != null)
                {
                    _dbContext.Customers.Add(newCustomer);
                    _dbContext.SaveChanges();
                    AllCustomers.Add(newCustomer);
                    FilterCustomers();
                }
            }
        }

        private void ExecuteEditCustomer(object? parameter)
        {
            if (SelectedCustomer != null)
            {
                var dialog = new CustomerDialog(SelectedCustomer);
                if (dialog.ShowDialog() == true)
                {
                    _dbContext.SaveChanges();
                    FilterCustomers();
                }
            }
        }

        private async void ExecuteDeleteCustomer(object? parameter)
        {
            if (SelectedCustomer != null)
            {
                // التحقق من وجود عمليات مرتبطة بالعميل
                var hasSalesInvoices = await _dbContext.SalesInvoices
                    .AnyAsync(s => s.CustomerId == SelectedCustomer.Id);
                
                var hasFinancialTransactions = await _dbContext.FinancialTransactionLines
                    .AnyAsync(t => t.AccountId == SelectedCustomer.AccountId);

                if (hasSalesInvoices || hasFinancialTransactions)
                {
                    string message = "لا يمكن حذف هذا العميل لأنه مرتبط بعمليات:\n";
                    if (hasSalesInvoices)
                        message += "- فواتير مبيعات\n";
                    if (hasFinancialTransactions)
                        message += "- قيود محاسبية\n";
                    message += "\nيرجى حذف العمليات المرتبطة أولاً أو استخدام ميزة الأرشفة.";

                    System.Windows.MessageBox.Show(
                        message,
                        "لا يمكن الحذف",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }

                var result = System.Windows.MessageBox.Show(
                    "هل أنت متأكد من حذف هذا العميل؟",
                    "تأكيد الحذف",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    _dbContext.Customers.Remove(SelectedCustomer);
                    await _dbContext.SaveChangesAsync();
                    AllCustomers.Remove(SelectedCustomer);
                    FilterCustomers();
                }
            }
        }

        private void ExecuteRefresh(object? parameter)
        {
            LoadCustomersAsync();
        }
    }
}
