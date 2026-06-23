using App2.Commands;
using App2.Data;
using App2.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace App2.ViewModels
{
    public class ProductSalesReport
    {
        public int ProductId { get; set; }
        public string ColorNumber { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public decimal TotalQuantity { get; set; }
        public decimal TotalAmount { get; set; }
        public string DisplayQuantity => $"{TotalQuantity:F2}";
        public string DisplayAmount => $"{TotalAmount:N2} ر.ي";
    }

    public class FinancialSummary
    {
        public decimal CashSales { get; set; }
        public decimal DeferredSales { get; set; }
        public decimal TransferSales { get; set; }
        public decimal CashPayments { get; set; }
        public decimal TransferPayments { get; set; }
        public decimal TotalQuantitySold { get; set; }

        public decimal TotalSales => CashSales + DeferredSales + TransferSales;
        public decimal TotalPayments => CashPayments + TransferPayments;

        public string DisplayCashSales => $"{CashSales:N2} ر.ي";
        public string DisplayDeferredSales => $"{DeferredSales:N2} ر.ي";
        public string DisplayTransferSales => $"{TransferSales:N2} ر.ي";
        public string DisplayCashPayments => $"{CashPayments:N2} ر.ي";
        public string DisplayTransferPayments => $"{TransferPayments:N2} ر.ي";
        public string DisplayTotalSales => $"{TotalSales:N2} ر.ي";
        public string DisplayTotalPayments => $"{TotalPayments:N2} ر.ي";
        public string DisplayTotalQuantitySold => $"{TotalQuantitySold:N2}";
    }

    public class ReportsViewModel : ObservableObject
    {
        private DateTime _startDate;
        private DateTime _endDate;
        private bool _isLoading;
        private FinancialSummary _financialSummary = new();

        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                if (SetProperty(ref _startDate, value))
                {
                    _ = LoadReports();
                }
            }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                if (SetProperty(ref _endDate, value))
                {
                    _ = LoadReports();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public FinancialSummary FinancialSummary
        {
            get => _financialSummary;
            set => SetProperty(ref _financialSummary, value);
        }

        public ObservableCollection<ProductSalesReport> ProductSales { get; } = new();

        public ICommand RefreshCommand { get; }

        public ReportsViewModel()
        {
            var today = DateTime.Today;
            _startDate = new DateTime(today.Year, today.Month, 1);
            _endDate = today;

            RefreshCommand = new RelayCommand(ExecuteRefresh);
        }

        public async Task LoadDataAsync()
        {
            await LoadReports();
        }

        private async Task LoadReports()
        {
            if (IsLoading) return;
            IsLoading = true;

            try
            {
                var (productSales, financialSummary) = await Task.Run(() =>
                {
                    var factory = new AppDbContextFactory();
                    using var db = factory.CreateDbContext(Array.Empty<string>());

                    var startDate = StartDate.Date;
                    var endDate = EndDate.Date.AddDays(1).AddTicks(-1);

                    // جلب فواتير المبيعات في الفترة المحددة
                    var salesInvoices = db.SalesInvoices
                        .Include(s => s.Details)
                        .Where(s => s.InvoiceDate >= startDate && s.InvoiceDate <= endDate)
                        .AsNoTracking()
                        .ToList();

                    // حساب الكمية المباعة من كل صنف
                    var productSalesList = new List<ProductSalesReport>();
                    var productGroups = salesInvoices
                        .SelectMany(s => s.Details)
                        .GroupBy(d => d.ItemName);

                    foreach (var group in productGroups)
                    {
                        var report = new ProductSalesReport
                        {
                            ColorNumber = group.Key,
                            Color = group.Key,
                            TotalQuantity = group.Sum(d => d.Quantity),
                            TotalAmount = group.Sum(d => d.TotalPrice)
                        };
                        productSalesList.Add(report);
                    }

                    // حساب الملخص المالي
                    var summary = new FinancialSummary
                    {
                        CashSales = salesInvoices.Sum(s => s.PaidInCash),
                        DeferredSales = salesInvoices.Sum(s => s.Deferred),
                        TransferSales = salesInvoices.Sum(s => s.Transfer),
                        TotalQuantitySold = productSalesList.Sum(p => p.TotalQuantity)
                    };

                    // حساب المدفوعات النقدية والتحويل من المعاملات المالية
                    var transactions = db.FinancialTransactions
                        .Include(t => t.Lines)
                        .ThenInclude(l => l.Account)
                        .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate)
                        .AsNoTracking()
                        .ToList();

                    // المدفوعات من العملاء (ReferenceType = "CustomerPayment")
                    var customerPayments = transactions
                        .Where(t => t.ReferenceType == "CustomerPayment")
                        .SelectMany(t => t.Lines)
                        .ToList();

                    // المدفوعات النقدية (حساب الصندوق)
                    var cashPayments = customerPayments
                        .Where(l => l.Account != null && (l.Account.Name.Contains("صندوق") || l.Account.Code == "1001") && l.Debit > 0)
                        .Sum(l => l.Debit);

                    // المدفوعات بالتحويل (حساب التحويلات)
                    var transferPayments = customerPayments
                        .Where(l => l.Account != null && (l.Account.Name.Contains("تحويل") || l.Account.Name.Contains("شبكة") || l.Account.Code == "1002") && l.Debit > 0)
                        .Sum(l => l.Debit);

                    summary.CashPayments = cashPayments;
                    summary.TransferPayments = transferPayments;

                    return (productSalesList, summary);
                });

                ProductSales.Clear();
                foreach (var item in productSales)
                {
                    ProductSales.Add(item);
                }

                FinancialSummary = financialSummary;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل التقارير:\n{ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void ExecuteRefresh(object? parameter)
        {
            await LoadReports();
        }
    }
}
