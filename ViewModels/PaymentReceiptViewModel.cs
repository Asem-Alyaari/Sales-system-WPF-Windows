using App2.Data;
using App2.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel;
using System.Windows;

namespace App2.ViewModels
{
    public class PaymentReceiptViewModel : ObservableObject
    {
        private readonly AppDbContext _dbContext;
        private Customer _selectedCustomer;
        private decimal _paymentAmount;
        private string _paymentType; // "نقدي" or "تحويل"
        private string? _notes;
        private DateTime _paymentDate;
        private bool _isSaveEnabled;
        
        public event Action? PaymentSaved;

        public PaymentReceiptViewModel(AppDbContext dbContext, Customer customer)
        {
            _dbContext = dbContext;
            _selectedCustomer = customer;
            _paymentDate = DateTime.Now;
            _paymentType = "نقدي";
            _notes = "";
            SavePaymentCommand = new Commands.RelayCommand(_ => ExecuteSavePayment(), CanExecuteSavePayment);
            CheckSaveButton();
        }

        public Customer SelectedCustomer => _selectedCustomer;

        public decimal PaymentAmount
        {
            get => _paymentAmount;
            set
            {
                SetProperty(ref _paymentAmount, value);
                CheckSaveButton();
            }
        }

        public string PaymentType
        {
            get => _paymentType;
            set => SetProperty(ref _paymentType, value);
        }

        public string? Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public DateTime PaymentDate
        {
            get => _paymentDate;
            set => SetProperty(ref _paymentDate, value);
        }

        public bool IsSaveEnabled
        {
            get => _isSaveEnabled;
            set => SetProperty(ref _isSaveEnabled, value);
        }

        public System.Windows.Input.ICommand SavePaymentCommand { get; }

        private void CheckSaveButton()
        {
            IsSaveEnabled = SelectedCustomer != null && SelectedCustomer.AccountId.HasValue && PaymentAmount > 0;
        }

        private bool CanExecuteSavePayment(object? parameter) => IsSaveEnabled;

        private async void ExecuteSavePayment()
        {
            try
            {
                // Get or create the necessary accounts (cash/transfer)
                var cashAccount = await _dbContext.Accounts
                    .FirstOrDefaultAsync(a => a.Name.Contains("صندوق") || a.Code == "1001");
                if (cashAccount == null)
                {
                    cashAccount = new Account
                    {
                        Name = "صندوق",
                        Code = "1001",
                        AccountType = AccountType.Asset,
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    };
                    _dbContext.Accounts.Add(cashAccount);
                    await _dbContext.SaveChangesAsync();
                }

                var transferAccount = await _dbContext.Accounts
                    .FirstOrDefaultAsync(a => a.Name.Contains("تحويل") || a.Name.Contains("شبكة") || a.Code == "1002");
                if (transferAccount == null)
                {
                    transferAccount = new Account
                    {
                        Name = "حساب التحويلات",
                        Code = "1002",
                        AccountType = AccountType.Asset,
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    };
                    _dbContext.Accounts.Add(transferAccount);
                    await _dbContext.SaveChangesAsync();
                }

                var customerAccountId = SelectedCustomer.AccountId.Value;
                var debitAccount = PaymentType == "نقدي" ? cashAccount : transferAccount;
                
                // Update Customer's Balance (decrease it since they're paying)
                SelectedCustomer.Balance = (SelectedCustomer.Balance ?? 0) - PaymentAmount;

                var transaction = new FinancialTransaction
                {
                    TransactionDate = PaymentDate,
                    Description = $"سداد مديونية من العميل {SelectedCustomer.Name} - {Notes}",
                    ReferenceType = "CustomerPayment",
                    ReferenceId = SelectedCustomer.Id
                };

                var lines = new System.Collections.Generic.List<FinancialTransactionLine>
                {
                    new FinancialTransactionLine
                    {
                        AccountId = debitAccount.Id,
                        Debit = PaymentAmount,
                        Credit = 0,
                        Notes = PaymentType == "نقدي" 
                            ? $"سداد نقدي من العميل {SelectedCustomer.Name}" 
                            : $"سداد تحويل من العميل {SelectedCustomer.Name}"
                    },
                    new FinancialTransactionLine
                    {
                        AccountId = customerAccountId,
                        Debit = 0,
                        Credit = PaymentAmount,
                        Notes = $"سداد مديونية"
                    }
                };

                transaction.Lines = lines;
                _dbContext.FinancialTransactions.Add(transaction);
                // Tell EF Core to update the Customer!
                _dbContext.Customers.Update(SelectedCustomer);
                await _dbContext.SaveChangesAsync();

                MessageBox.Show("تم تسجيل الدفع بنجاح!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                PaymentSaved?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تسجيل الدفع: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
