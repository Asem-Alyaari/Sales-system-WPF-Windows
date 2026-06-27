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
                Tuple<List<ProductSalesReport>, FinancialSummary> result = await Task.Run(() =>
                {
                    var factory = new AppDbContextFactory();
                    using var db = factory.CreateDbContext(Array.Empty<string>());

                    var startDate = StartDate.Date;
                    var endDate = EndDate.Date.AddDays(1).AddTicks(-1);

                    // جلب جميع المنتجات للبحث السريع
                    var productsDict = db.Products
                        .ToDictionary(p => p.Id, p => p);
                    var productsByColorNumber = db.Products
                        .ToDictionary(p => p.ColorNumber.ToLowerInvariant(), p => p);

                    // جلب فواتير المبيعات في الفترة المحددة
                    var salesInvoices = db.SalesInvoices
                        .Include(s => s.Details)
                        .Where(s => s.InvoiceDate >= startDate && s.InvoiceDate <= endDate)
                        .AsNoTracking()
                        .ToList();

                    // جلب المرتجعات في الفترة المحددة
                    var salesReturns = db.SalesReturns
                        .Include(r => r.Details)
                        .Where(r => r.ReturnDate >= startDate && r.ReturnDate <= endDate)
                        .AsNoTracking()
                        .ToList();

                    // استخدام قاموس لتجميع الكميات للمبيعات، المرتجعات، والاستبدالات
                    var productData = new Dictionary<int, (decimal quantity, decimal amount)>();

                    // إضافة المبيعات الأصلية
                    foreach (var detail in salesInvoices.SelectMany(s => s.Details))
                    {
                        // تحديد معرف المنتج
                        int productId;
                        
                        if (detail.ProductId != 0)
                        {
                            productId = detail.ProductId;
                        }
                        else
                        {
                            // للبيانات القديمة، حاول العثور على المنتج باستخدام رقم الصنف
                            if (productsByColorNumber.TryGetValue(detail.ThreadNumber.ToLowerInvariant(), out var productFromThread))
                            {
                                productId = productFromThread.Id;
                            }
                            else
                            {
                                // إذا لم يتم العثور على المنتج، استخدم معرف عشوائي أو قم بتعرفه بـ ThreadNumber's hash code
                                productId = detail.ThreadNumber.GetHashCode();
                            }
                        }

                        if (!productData.ContainsKey(productId))
                        {
                            productData[productId] = (0, 0);
                        }

                        // تحويل الكمية إلى الكبة (الوحدة الأساسية (d.Unit == UnitType.Carton ? detail.Quantity * Inventory.KabbaPerCarton : detail.Quantity;
                        decimal quantityInKabba = detail.Unit == UnitType.Carton 
                            ? detail.Quantity * Inventory.KabbaPerCarton 
                            : detail.Quantity;

                        productData[productId] = (
                            productData[productId].quantity + quantityInKabba,
                            productData[productId].amount + detail.TotalPrice
                        );
                    }

                    // معالجة المرتجعات والاستبدالات
                    foreach (var returnItem in salesReturns)
                    {
                        foreach (var detail in returnItem.Details)
                        {
                            // تحديد معرف المنتج
                            int productId;
                            
                            if (detail.ProductId != 0)
                            {
                                productId = detail.ProductId;
                            }
                            else
                            {
                                // للبيانات القديمة، حاول العثور على المنتج باستخدام رقم الصنف
                                if (productsByColorNumber.TryGetValue(detail.ThreadNumber.ToLowerInvariant(), out var productFromThread))
                                {
                                    productId = productFromThread.Id;
                                }
                                else
                                {
                                    productId = detail.ThreadNumber.GetHashCode();
                                }
                            }
                            
                            if (!productData.ContainsKey(productId))
                            {
                                productData[productId] = (0, 0);
                            }

                            // تحويل الكمية إلى الكبة
                            decimal quantityInKabba = detail.GetQuantityInKabba();

                            if (returnItem.Type == ReturnType.Return)
                            {
                                // للمرتجع: طرح الكمية والقيمة
                                productData[productId] = (
                                    productData[productId].quantity - quantityInKabba,
                                    productData[productId].amount - detail.TotalPrice
                                );
                            }
                            else if (returnItem.Type == ReturnType.Exchange)
                            {
                                // للاستبدال: إذا كان الصنف من الفاتورة الأصلية (له SalesInvoiceDetailId) → طرح (الحد الأقصى - الكمية)
                                // وإلا → إضافة (الصنف الجديد)
                                if (detail.SalesInvoiceDetailId.HasValue)
                                {
                                    decimal quantityToSubtract = detail.MaxReturnQuantityKabba - quantityInKabba;
                                    productData[productId] = (
                                        productData[productId].quantity - quantityToSubtract,
                                        productData[productId].amount - detail.TotalPrice
                                    );
                                }
                                else
                                {
                                    productData[productId] = (
                                        productData[productId].quantity + quantityInKabba,
                                        productData[productId].amount + detail.TotalPrice
                                    );
                                }
                            }
                        }
                    }

                    // تحويل القاموس إلى قائمة ProductSalesReport
                    var productSalesList = new List<ProductSalesReport>();
                    foreach (var (productId, (quantity, amount)) in productData)
                    {
                        // تجاهل الأصناف التي أصبحت كميتها صفر
                        if (Math.Round(quantity, 2) != 0)
                        {
                            // جلب المنتج من جدول Products لاستخدام البيانات المحدثة
                            string colorNumber = "";
                            string color = "";
                            
                            if (productsDict.TryGetValue(productId, out var product))
                            {
                                colorNumber = product.ColorNumber;
                                color = product.Color ?? "";
                            }
                            else
                            {
                                // إذا لم يتم العثور على المنتج، حاول العثور عليه من خلال مثال من تفاصيل الفاتورة
                                SalesInvoiceDetail? sampleInvoiceDetail = salesInvoices
                                    .SelectMany(i => i.Details)
                                    .FirstOrDefault(d => 
                                        (d.ProductId != 0 && d.ProductId == productId) || 
                                        (d.ProductId == 0 && d.ThreadNumber.GetHashCode() == productId));
                                
                                if (sampleInvoiceDetail != null)
                                {
                                    colorNumber = sampleInvoiceDetail.ThreadNumber;
                                    color = sampleInvoiceDetail.ItemName;
                                }
                                else
                                {
                                    SalesReturnDetail? sampleReturnDetail = salesReturns
                                        .SelectMany(r => r.Details)
                                        .FirstOrDefault(d => 
                                            (d.ProductId != 0 && d.ProductId == productId) || 
                                            (d.ProductId == 0 && d.ThreadNumber.GetHashCode() == productId));
                                    
                                    if (sampleReturnDetail != null)
                                    {
                                        colorNumber = sampleReturnDetail.ThreadNumber;
                                        color = sampleReturnDetail.ItemName;
                                    }
                                }
                            }
                            
                            if (!string.IsNullOrEmpty(colorNumber) || !string.IsNullOrEmpty(color))
                            {
                                productSalesList.Add(new ProductSalesReport
                                {
                                    ProductId = productId,
                                    ColorNumber = colorNumber,
                                    Color = color,
                                    TotalQuantity = quantity,
                                    TotalAmount = amount
                                });
                            }
                        }
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

                    return new Tuple<List<ProductSalesReport>, FinancialSummary>(productSalesList, summary);
                });

                ProductSales.Clear();
                foreach (var item in result.Item1.OrderByDescending(p => p.TotalAmount))
                {
                    ProductSales.Add(item);
                }

                FinancialSummary = result.Item2;
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
