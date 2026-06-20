using App2.Commands;
using App2.Data;
using App2.Models;
using App2.Views;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System;

namespace App2.ViewModels
{
    public class AccountsViewModel : ObservableObject
    {
        private string _searchText = string.Empty;
        private Account? _selectedAccount;
        private readonly AppDbContext _dbContext;

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                FilterAccounts();
            }
        }

        public Account? SelectedAccount
        {
            get => _selectedAccount;
            set => SetProperty(ref _selectedAccount, value);
        }

        public ObservableCollection<Account> Accounts { get; } = new();
        public ObservableCollection<Account> AllAccounts { get; } = new();

        public ICommand AddAccountCommand { get; }
        public ICommand EditAccountCommand { get; }
        public ICommand DeleteAccountCommand { get; }
        public ICommand RefreshCommand { get; }

        public AccountsViewModel()
        {
            var factory = new AppDbContextFactory();
            _dbContext = factory.CreateDbContext(null);

            AddAccountCommand = new RelayCommand(ExecuteAddAccount);
            EditAccountCommand = new RelayCommand(ExecuteEditAccount, CanExecuteEditDelete);
            DeleteAccountCommand = new RelayCommand(ExecuteDeleteAccount, CanExecuteEditDelete);
            RefreshCommand = new RelayCommand(ExecuteRefresh);

            LoadAccountsAsync();
        }

        private async void LoadAccountsAsync()
        {
            try
            {
                var accounts = await _dbContext.Accounts.ToListAsync();
                AllAccounts.Clear();
                Accounts.Clear();

                foreach (var account in accounts)
                {
                    AllAccounts.Add(account);
                    Accounts.Add(account);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الحسابات: {ex.Message}");
            }
        }

        private void FilterAccounts()
        {
            Accounts.Clear();

            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? AllAccounts
                : AllAccounts.Where(a =>
                    a.Name.Contains(SearchText) ||
                    a.Code.Contains(SearchText));

            foreach (var account in filtered)
            {
                Accounts.Add(account);
            }
        }

        private bool CanExecuteEditDelete(object? parameter)
        {
            return SelectedAccount != null;
        }

        private void ExecuteAddAccount(object? parameter)
        {
            var dialog = new AccountDialog();
            if (dialog.ShowDialog() == true)
            {
                var newAccount = dialog.Account;
                if (newAccount != null)
                {
                    _dbContext.Accounts.Add(newAccount);
                    _dbContext.SaveChanges();
                    AllAccounts.Add(newAccount);
                    FilterAccounts();
                }
            }
        }

        private void ExecuteEditAccount(object? parameter)
        {
            if (SelectedAccount != null)
            {
                var dialog = new AccountDialog(SelectedAccount);
                if (dialog.ShowDialog() == true)
                {
                    _dbContext.SaveChanges();
                    FilterAccounts();
                }
            }
        }

        private void ExecuteDeleteAccount(object? parameter)
        {
            if (SelectedAccount != null)
            {
                var result = MessageBox.Show(
                    "هل أنت متأكد من حذف هذا الحساب؟",
                    "تأكيد الحذف",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    bool hasTransactions = _dbContext.FinancialTransactionLines.Any(l => l.AccountId == SelectedAccount.Id);
                    if (hasTransactions)
                    {
                        MessageBox.Show("لا يمكن حذف هذا الحساب لأن عليه حركات مالية.", "حذف حساب", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    _dbContext.Accounts.Remove(SelectedAccount);
                    _dbContext.SaveChanges();
                    AllAccounts.Remove(SelectedAccount);
                    FilterAccounts();
                }
            }
        }

        private void ExecuteRefresh(object? parameter)
        {
            LoadAccountsAsync();
        }
    }
}
