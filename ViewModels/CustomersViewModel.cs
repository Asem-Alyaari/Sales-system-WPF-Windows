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

        public CustomersViewModel()
        {
            var factory = new AppDbContextFactory();
            _dbContext = factory.CreateDbContext(null);

            AddCustomerCommand = new RelayCommand(ExecuteAddCustomer);
            EditCustomerCommand = new RelayCommand(ExecuteEditCustomer, CanExecuteEditDelete);
            DeleteCustomerCommand = new RelayCommand(ExecuteDeleteCustomer, CanExecuteEditDelete);
            RefreshCommand = new RelayCommand(ExecuteRefresh);

            LoadCustomersAsync();
        }

        private async void LoadCustomersAsync()
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
                    c.Phone.Contains(SearchText) ||
                    c.Address.Contains(SearchText));

            foreach (var customer in filtered)
            {
                Customers.Add(customer);
            }
        }

        private bool CanExecuteEditDelete(object? parameter)
        {
            return SelectedCustomer != null;
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

        private void ExecuteDeleteCustomer(object? parameter)
        {
            if (SelectedCustomer != null)
            {
                var result = System.Windows.MessageBox.Show(
                    "هل أنت متأكد من حذف هذا العميل؟",
                    "تأكيد الحذف",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    _dbContext.Customers.Remove(SelectedCustomer);
                    _dbContext.SaveChanges();
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
