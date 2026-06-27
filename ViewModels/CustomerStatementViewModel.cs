using App2.Data;
using App2.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace App2.ViewModels
{
    public class CustomerTransactionItem
    {
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
        public string ReferenceType { get; set; } = string.Empty;
        public int? ReferenceId { get; set; }
    }

    public class CustomerStatementViewModel : ObservableObject
    {
        private readonly AppDbContext _dbContext;
        private Customer? _selectedCustomer;
        private Account? _selectedAccount;
        private DateTime _fromDate;
        private DateTime _toDate;
        private decimal _openingBalance;
        private decimal _totalDebit;
        private decimal _totalCredit;
        private decimal _closingBalance;
        private string _searchText = string.Empty;
        private ObservableCollection<CustomerTransactionItem> _allTransactions = new ObservableCollection<CustomerTransactionItem>();

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterTransactions();
                }
            }
        }

        public Customer? SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                if (SetProperty(ref _selectedCustomer, value))
                {
                    if (value != null)
                    {
                        SelectedAccount = value.Account;
                    }
                    LoadTransactions();
                }
            }
        }

        public Account? SelectedAccount
        {
            get => _selectedAccount;
            set
            {
                if (SetProperty(ref _selectedAccount, value))
                {
                    LoadTransactions();
                }
            }
        }

        public DateTime FromDate
        {
            get => _fromDate;
            set
            {
                if (SetProperty(ref _fromDate, value))
                {
                    LoadTransactions();
                }
            }
        }

        public DateTime ToDate
        {
            get => _toDate;
            set
            {
                if (SetProperty(ref _toDate, value))
                {
                    LoadTransactions();
                }
            }
        }

        public decimal OpeningBalance
        {
            get => _openingBalance;
            private set => SetProperty(ref _openingBalance, value);
        }

        public decimal TotalDebit
        {
            get => _totalDebit;
            private set => SetProperty(ref _totalDebit, value);
        }

        public decimal TotalCredit
        {
            get => _totalCredit;
            private set => SetProperty(ref _totalCredit, value);
        }

        public decimal ClosingBalance
        {
            get => _closingBalance;
            private set => SetProperty(ref _closingBalance, value);
        }

        public ObservableCollection<CustomerTransactionItem> Transactions { get; } = new ObservableCollection<CustomerTransactionItem>();
        public ObservableCollection<Customer> Customers { get; } = new ObservableCollection<Customer>();

        public CustomerStatementViewModel()
        {
            var factory = new AppDbContextFactory();
            _dbContext = factory.CreateDbContext(null);

            FromDate = DateTime.Now.AddMonths(-1);
            ToDate = DateTime.Now;

            LoadCustomers();
        }

        private async void LoadCustomers()
        {
            try
            {
                var customers = await _dbContext.Customers.ToListAsync();
                Customers.Clear();
                foreach (var customer in customers)
                {
                    Customers.Add(customer);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل العملاء: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadTransactions()
        {
            _allTransactions.Clear();

            int? accountId = null;
            if (SelectedAccount != null)
            {
                accountId = SelectedAccount.Id;
            }
            else if (SelectedCustomer != null && SelectedCustomer.AccountId.HasValue)
            {
                accountId = SelectedCustomer.AccountId.Value;
            }

            if (!accountId.HasValue)
            {
                OpeningBalance = 0;
                TotalDebit = 0;
                TotalCredit = 0;
                ClosingBalance = 0;
                FilterTransactions();
                return;
            }

            try
            {
                // جلب جميع الحركات للحساب
                var allTransactions = _dbContext.FinancialTransactionLines
                    .Include(l => l.FinancialTransaction)
                    .Where(l => l.AccountId == accountId.Value)
                    .OrderBy(l => l.FinancialTransaction.TransactionDate)
                    .ThenBy(l => l.Id)
                    .ToList();

                // حساب الرصيد الافتتاحي (قبل تاريخ البداية)
                var openingTransactions = allTransactions
                    .Where(l => l.FinancialTransaction.TransactionDate.Date < FromDate.Date)
                    .ToList();

                OpeningBalance = openingTransactions.Sum(l => l.Debit) - openingTransactions.Sum(l => l.Credit);

                // جلب الحركات في الفترة المحددة
                var periodTransactions = allTransactions
                    .Where(l => l.FinancialTransaction.TransactionDate.Date >= FromDate.Date && 
                               l.FinancialTransaction.TransactionDate.Date <= ToDate.Date)
                    .ToList();

                decimal runningBalance = OpeningBalance;

                foreach (var line in periodTransactions)
                {
                    var item = new CustomerTransactionItem
                    {
                        Date = line.FinancialTransaction.TransactionDate,
                        Description = !string.IsNullOrWhiteSpace(line.Notes) ? line.Notes : line.FinancialTransaction.Description,
                        Debit = line.Debit,
                        Credit = line.Credit,
                        Balance = runningBalance + line.Debit - line.Credit,
                        ReferenceType = line.FinancialTransaction.ReferenceType,
                        ReferenceId = line.FinancialTransaction.ReferenceId
                    };

                    runningBalance = item.Balance;
                    _allTransactions.Add(item);
                }

                TotalDebit = periodTransactions.Sum(l => l.Debit);
                TotalCredit = periodTransactions.Sum(l => l.Credit);
                ClosingBalance = runningBalance;
                
                FilterTransactions();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الحركات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void FilterTransactions()
        {
            Transactions.Clear();
            
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                foreach (var item in _allTransactions)
                {
                    Transactions.Add(item);
                }
            }
            else
            {
                var searchLower = SearchText.ToLower();
                foreach (var item in _allTransactions)
                {
                    if (item.Description.ToLower().Contains(searchLower) || 
                        (item.ReferenceType != null && item.ReferenceType.ToLower().Contains(searchLower)) ||
                        (item.ReferenceId != null && item.ReferenceId.ToString().Contains(searchLower)))
                    {
                        Transactions.Add(item);
                    }
                }
            }
        }

        public void Refresh()
        {
            LoadCustomers();
            LoadTransactions();
        }
    }
}
