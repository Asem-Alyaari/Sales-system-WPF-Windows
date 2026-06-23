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

        public event Action<Account?>? OnAccountStatementRequested;

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

        public ICommand ViewStatementCommand { get; }
        public ICommand RefreshCommand { get; }

        public AccountsViewModel()
        {
            var factory = new AppDbContextFactory();
            _dbContext = factory.CreateDbContext(null);

            ViewStatementCommand = new RelayCommand(ExecuteViewStatement);
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

        private void ExecuteViewStatement(object? parameter)
        {
            if (parameter is Account account)
            {
                OnAccountStatementRequested?.Invoke(account);
            }
        }

        private void ExecuteRefresh(object? parameter)
        {
            LoadAccountsAsync();
        }
    }
}
