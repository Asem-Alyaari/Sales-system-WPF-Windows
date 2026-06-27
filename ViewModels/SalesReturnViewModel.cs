using App2.Data;
using App2.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace App2.ViewModels
{
    public class ExchangeBatchItemViewModel
    {
        public Inventory InventoryBatch { get; set; } = null!;
        public string DisplayText => $"{InventoryBatch.Product?.ColorNumber} - {InventoryBatch.Product?.Color} | الرصيد: {InventoryBatch.DisplayQuantity}";

        public override string ToString()
        {
            return DisplayText;
        }
    }

    public class SalesReturnViewModel : ObservableObject
    {
        private readonly AppDbContext _dbContext;
        private bool _isProcessingSelection;

        public SalesReturnViewModel()
        {
            var factory = new AppDbContextFactory();
            _dbContext = factory.CreateDbContext(null);

            LoadAllInvoices();
            RemoveItemCommand = new Commands.RelayCommand(ExecuteRemoveItem);
            SaveReturnCommand = new Commands.RelayCommand(ExecuteSaveReturn, CanExecuteSaveReturn);
            ReturnDate = DateTime.Now;
            ReturnNumber = "RET-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            SelectedPaymentMethod = ReturnPaymentMethod.ToCustomerAccount;
            
            // Listen for collection changes
            Items.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (var item in e.NewItems)
                    {
                        if (item is SalesReturnDetail detail)
                        {
                            detail.PropertyChanged += (sender, args) =>
                            {
                                UpdateTotals();
                            };
                        }
                    }
                }
                
                UpdateTotals();
            };
        }

        private ObservableCollection<SalesInvoice> _allInvoices = new ObservableCollection<SalesInvoice>();
        public ObservableCollection<SalesInvoice> FilteredInvoices { get; } = new ObservableCollection<SalesInvoice>();

        private string _searchInvoiceText = string.Empty;
        public string SearchInvoiceText
        {
            get => _searchInvoiceText;
            set
            {
                if (SetProperty(ref _searchInvoiceText, value))
                {
                    FilterInvoices();
                    IsInvoiceDropdownOpen = !string.IsNullOrWhiteSpace(value) && FilteredInvoices.Any();
                }
            }
        }

        private bool _isInvoiceDropdownOpen;
        public bool IsInvoiceDropdownOpen
        {
            get => _isInvoiceDropdownOpen;
            set => SetProperty(ref _isInvoiceDropdownOpen, value);
        }

        private SalesInvoice? _selectedInvoice;
        public SalesInvoice? SelectedInvoice
        {
            get => _selectedInvoice;
            set
            {
                if (SetProperty(ref _selectedInvoice, value))
                {
                    if (value != null)
                    {
                        SearchInvoiceText = value.InvoiceNumber;
                        IsInvoiceDropdownOpen = false;
                    }
                    LoadInvoiceDetails();
                    OnPropertyChanged(nameof(SelectedCustomerName));
                }
            }
        }

        public string? SelectedCustomerName => SelectedInvoice?.Customer.Name;

        private DateTime _returnDate;
        public DateTime ReturnDate
        {
            get => _returnDate;
            set => SetProperty(ref _returnDate, value);
        }

        private string _returnNumber = string.Empty;
        public string ReturnNumber
        {
            get => _returnNumber;
            set => SetProperty(ref _returnNumber, value);
        }

        private string? _notes;
        public string? Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        private ReturnPaymentMethod _selectedPaymentMethod;
        public ReturnPaymentMethod SelectedPaymentMethod
        {
            get => _selectedPaymentMethod;
            set => SetProperty(ref _selectedPaymentMethod, value);
        }

        // Always Return
        public ReturnType SelectedType => ReturnType.Return;

        private string? _transferNumber;
        public string? TransferNumber
        {
            get => _transferNumber;
            set => SetProperty(ref _transferNumber, value);
        }

        private decimal _originalInvoiceTotalQuantityKabba;
        public decimal OriginalInvoiceTotalQuantityKabba
        {
            get => _originalInvoiceTotalQuantityKabba;
            private set => SetProperty(ref _originalInvoiceTotalQuantityKabba, value);
        }

        public ObservableCollection<SalesReturnDetail> Items { get; } = new ObservableCollection<SalesReturnDetail>();

        private decimal _itemsTotalQuantityKabba;
        public decimal ItemsTotalQuantityKabba
        {
            get => _itemsTotalQuantityKabba;
            private set => SetProperty(ref _itemsTotalQuantityKabba, value);
        }

        private string _searchItemNumber = string.Empty;
        public string SearchItemNumber
        {
            get => _searchItemNumber;
            set
            {
                if (_isProcessingSelection) return;
                if (_searchItemNumber == value) return;

                _searchItemNumber = value;

                if (!string.IsNullOrEmpty(value))
                {
                    SearchBatches();
                }
                else
                {
                    AvailableBatches.Clear();
                    IsItemDropdownOpen = false;
                }
            }
        }

        private bool _isItemDropdownOpen;
        public bool IsItemDropdownOpen
        {
            get => _isItemDropdownOpen;
            set => SetProperty(ref _isItemDropdownOpen, value);
        }

        private ExchangeBatchItemViewModel? _selectedBatch;
        public ExchangeBatchItemViewModel? SelectedBatch
        {
            get => _selectedBatch;
            set => SetProperty(ref _selectedBatch, value);
        }

        public ObservableCollection<ExchangeBatchItemViewModel> AvailableBatches { get; } = new ObservableCollection<ExchangeBatchItemViewModel>();

        private decimal _totalAmount;
        public decimal TotalAmount
        {
            get => _totalAmount;
            private set => SetProperty(ref _totalAmount, value);
        }

        public System.Windows.Input.ICommand RemoveItemCommand { get; }
        public System.Windows.Input.ICommand SaveReturnCommand { get; }

        public void ReloadData()
        {
            LoadAllInvoices();
        }

        private void LoadAllInvoices()
        {
            var invoices = _dbContext.SalesInvoices
                .Include(i => i.Customer)
                .Include(i => i.Details)
                .OrderByDescending(i => i.InvoiceDate)
                .ToList();

            _allInvoices.Clear();
            foreach (var invoice in invoices)
            {
                _allInvoices.Add(invoice);
            }
            FilterInvoices();
        }

        private void FilterInvoices()
        {
            FilteredInvoices.Clear();
            var searchText = SearchInvoiceText?.ToLower() ?? string.Empty;
            
            var filtered = _allInvoices
                .Where(i => i.InvoiceNumber.ToLower().Contains(searchText) || 
                           (i.Customer != null && i.Customer.Name.ToLower().Contains(searchText)))
                .ToList();

            foreach (var invoice in filtered)
            {
                FilteredInvoices.Add(invoice);
            }
        }

        private void LoadInvoiceDetails()
        {
            Items.Clear();
            OriginalInvoiceTotalQuantityKabba = 0;
            if (SelectedInvoice != null)
            {
                // جلب جميع المرتجعات السابقة لهذه الفاتورة
                var existingReturns = _dbContext.SalesReturns
                    .Include(r => r.Details)
                    .Where(r => r.SalesInvoiceId == SelectedInvoice.Id)
                    .OrderByDescending(r => r.ReturnDate)
                    .ToList();

                // جلب آخر استبدال (إذا وجد)
                var lastExchange = existingReturns.FirstOrDefault(r => r.Type == ReturnType.Exchange);

                if (lastExchange != null)
                {
                    // إذا كان هناك استبدال سابق: عرض أصناف الاستبدال
                    // تخطي الأصناف الأصلية التي كانت كميتها 0
                    foreach (var detail in lastExchange.Details)
                    {
                        // تخطي الأصناف الأصلية (مع SalesInvoiceDetailId) إذا كانت Quantity == 0
                        if (detail.SalesInvoiceDetailId.HasValue && detail.Quantity == 0)
                        {
                            continue;
                        }
                        
                        var newDetail = new SalesReturnDetail
                        {
                            SalesInvoiceDetailId = detail.SalesInvoiceDetailId,
                            ProductId = detail.ProductId,
                            ThreadNumber = detail.ThreadNumber,
                            ItemName = detail.ItemName,
                            Quantity = detail.Quantity,
                            MaxReturnQuantityKabba = detail.MaxReturnQuantityKabba,
                            OriginalUnit = detail.OriginalUnit,
                            Unit = detail.Unit,
                            Price = detail.Price,
                            TotalPrice = detail.TotalPrice
                        };
                        Items.Add(newDetail);
                        newDetail.PropertyChanged += (s, e) =>
                        {
                            if (e.PropertyName == nameof(SalesReturnDetail.TotalPrice))
                            {
                                UpdateTotals();
                            }
                        };
                    }

                    // الكمية المطلوبة هي مجموع الأصناف الجديدة فقط (أو مجموع الكمية المرتجعة في الأصناف الأصلية)
                    // الحساب: مجموع الأصناف الجديدة (بدون SalesInvoiceDetailId)
                    var newItems = Items.Where(i => !i.SalesInvoiceDetailId.HasValue).ToList();
                    OriginalInvoiceTotalQuantityKabba = newItems.Sum(i => i.GetQuantityInKabba());
                }
                else
                {
                    // Calculate original invoice total quantity in Kabba MINUS quantities already returned
                    decimal originalInvoiceQuantityKabba = SelectedInvoice.Details.Sum(d => 
                        d.Unit == UnitType.Carton 
                            ? d.Quantity * Inventory.KabbaPerCarton 
                            : d.Quantity);

                    // Calculate total quantity already returned for this invoice
                    decimal totalReturnedQuantityKabba = existingReturns
                        .SelectMany(r => r.Details)
                        .Sum(d => d.GetQuantityInKabba());

                    OriginalInvoiceTotalQuantityKabba = originalInvoiceQuantityKabba - totalReturnedQuantityKabba;
                    
                    foreach (var detail in SelectedInvoice.Details)
                    {
                        // تحويل الكمية المباعة إلى الكبة لحساب الحد الأقصى المسموح بإرجاعه
                        decimal originalQuantityKabba = detail.Unit == UnitType.Carton 
                            ? detail.Quantity * Inventory.KabbaPerCarton 
                            : detail.Quantity;
                        
                        // حساب مجموع الكميات المرتجعة سابقاً لهذا الصنف
                        decimal returnedQuantityKabba = existingReturns
                            .SelectMany(r => r.Details)
                            .Where(d => d.SalesInvoiceDetailId == detail.Id)
                            .Sum(d => d.GetQuantityInKabba());
                        
                        // الكمية المتاحة للإرجاع = الكمية الأصلية - الكميات المرتجعة سابقاً
                        decimal maxReturnQuantityKabba = originalQuantityKabba - returnedQuantityKabba;
                        
                        // ضبط الكمية بناءً على نوع العملية (استبدال أو مرتجع)
                        decimal initialQuantityInKabba = SelectedType == ReturnType.Exchange 
                            ? maxReturnQuantityKabba  // في حالة استبدال: الكمية = الحد الأقصى
                            : 0;                      // في حالة مرتجع: الكمية = 0
                        
                        // تحويل الكمية إلى الوحدة الأصلية
                        decimal initialQuantityInOriginalUnit = detail.Unit == UnitType.Carton 
                            ? initialQuantityInKabba / Inventory.KabbaPerCarton 
                            : initialQuantityInKabba;
                        
                        var returnDetail = new SalesReturnDetail
                        {
                            SalesInvoiceDetailId = detail.Id,
                            ProductId = detail.ProductId,
                            ThreadNumber = detail.ThreadNumber,
                            ItemName = detail.ItemName,
                            Quantity = initialQuantityInOriginalUnit,
                            MaxReturnQuantityKabba = maxReturnQuantityKabba,
                            OriginalUnit = detail.Unit,
                            Unit = detail.Unit,
                            Price = detail.Price,
                            TotalPrice = 0
                        };
                        Items.Add(returnDetail);
                        returnDetail.PropertyChanged += (s, e) =>
                        {
                            if (e.PropertyName == nameof(SalesReturnDetail.TotalPrice))
                            {
                                UpdateTotals();
                            }
                        };
                    }
                }
                UpdateTotals();
            }
        }

        private void UpdateTotals()
        {
            TotalAmount = Items.Sum(i => i.TotalPrice);
            ItemsTotalQuantityKabba = Items.Sum(i => i.GetQuantityInKabba());
        }

        private void SearchBatches()
        {
            AvailableBatches.Clear();
            if (string.IsNullOrWhiteSpace(SearchItemNumber))
            {
                IsItemDropdownOpen = false;
                return;
            }

            var items = _dbContext.Inventories
                .Include(i => i.Product)
                .Where(i => i.Product != null &&
                            (i.Product.ColorNumber.Contains(SearchItemNumber) ||
                             (i.Product.Color != null && i.Product.Color.Contains(SearchItemNumber)))
                            && i.Quantity > 0)
                .ToList();

            foreach (var item in items)
            {
                AvailableBatches.Add(new ExchangeBatchItemViewModel { InventoryBatch = item });
            }
            IsItemDropdownOpen = AvailableBatches.Count > 0;
        }

        public void ConfirmSelection()
        {
            if (SelectedBatch != null)
            {
                _isProcessingSelection = true;
                AddBatchToItems(SelectedBatch.InventoryBatch);

                System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    SelectedBatch = null;
                    _searchItemNumber = string.Empty;
                    OnPropertyChanged(nameof(SearchItemNumber));
                    AvailableBatches.Clear();
                    IsItemDropdownOpen = false;
                    _isProcessingSelection = false;
                }, System.Windows.Threading.DispatcherPriority.ContextIdle);
            }
        }

        private void AddBatchToItems(Inventory batch)
        {
            var detail = new SalesReturnDetail
            {
                ProductId = batch.ProductId,
                ThreadNumber = batch.Product?.ColorNumber ?? "",
                ItemName = batch.Product?.Color ?? "",
                Quantity = 1,
                Unit = UnitType.Skein, // افتراضياً كبة
                Price = 0,
                TotalPrice = 0,
                MaxReturnQuantityKabba = 9999999 // في الاستبدال فقط نريد التحقق من المجموع الكل
            };

            Items.Add(detail);
        }

        private void ExecuteRemoveItem(object? parameter)
        {
            if (parameter is SalesReturnDetail detail)
            {
                Items.Remove(detail);
                UpdateTotals();
            }
        }

        private bool CanExecuteSaveReturn(object? parameter)
        {
            if (SelectedInvoice == null)
                return false;

            if (SelectedType == ReturnType.Return)
            {
                if (!Items.Any(i => i.Quantity > 0))
                    return false;

                if (SelectedPaymentMethod == ReturnPaymentMethod.Transfer && string.IsNullOrWhiteSpace(TransferNumber))
                    return false;
            }
            else
            {
                // For Exchange, just check that items exist (we'll validate exact quantity in Execute)
                if (!Items.Any())
                    return false;
            }

            return true;
        }

        private void ExecuteSaveReturn(object? parameter)
        {
            if (SelectedInvoice == null)
            {
                MessageBox.Show("الرجاء اختيار الفاتورة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Define itemsToProcess first for validation
            var itemsToProcess = SelectedType == ReturnType.Return 
                ? Items.Where(i => i.Quantity > 0).ToList() 
                : Items.ToList();

            if (SelectedType == ReturnType.Exchange)
            {
                // For exchange: sum of original items' (MaxReturn - Quantity) must equal sum of new items' quantity
                var originalItems = itemsToProcess.Where(i => i.SalesInvoiceDetailId.HasValue).ToList();
                var newItems = itemsToProcess.Where(i => !i.SalesInvoiceDetailId.HasValue).ToList();
                
                // Validate new items have quantity > 0
                foreach (var item in newItems)
                {
                    if (item.Quantity <= 0)
                    {
                        MessageBox.Show($"الكمية المرجعة للصنف {item.ItemName} يجب أن تكون أكبر من 0", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                
                decimal originalTotal = originalItems.Sum(i => i.MaxReturnQuantityKabba - i.GetQuantityInKabba());
                decimal newTotal = newItems.Sum(i => i.GetQuantityInKabba());
                
                if (originalTotal != newTotal)
                {
                    MessageBox.Show($"الكمية المرتجعة ({originalTotal} كبة) لا تساوي الكمية المتبادلة ({newTotal} كبة)!", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                // Only check that original items don't have negative quantity
                foreach (var item in originalItems)
                {
                    decimal quantityToKeepKabba = item.GetQuantityInKabba();
                    if (quantityToKeepKabba < 0)
                    {
                        MessageBox.Show($"لا يمكن الاحتفاظ بكمية سالبة للصنف {item.ItemName}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                
                // Check stock availability for new items
                foreach (var item in newItems)
                {
                    var product = _dbContext.Products.FirstOrDefault(p => p.ColorNumber == item.ThreadNumber);
                    if (product == null)
                    {
                        MessageBox.Show($"الصنف {item.ItemName} غير موجود في قاعدة البيانات!", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    var inventory = _dbContext.Inventories.FirstOrDefault(i => i.ProductId == product.Id);
                    if (inventory == null)
                    {
                        MessageBox.Show($"لا يوجد مخزون للصنف {item.ItemName}!", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    decimal quantityToSubtractKabba = item.GetQuantityInKabba();
                    if (inventory.Quantity < quantityToSubtractKabba)
                    {
                        MessageBox.Show($"الكمية المطلوبة ({quantityToSubtractKabba} كبة) للصنف {item.ItemName} أكبر من المخزون المتاح ({inventory.Quantity} كبة)!", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
            }
            else
            {
                var itemsToReturn = itemsToProcess;
                if (!itemsToReturn.Any())
                {
                    MessageBox.Show("لا يمكن حفظ مرتجع بدون أصناف", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // التحقق من عدم تجاوز الكمية المباعة
                foreach (var item in itemsToReturn)
                {
                    decimal quantityToReturnKabba = item.GetQuantityInKabba();
                    if (quantityToReturnKabba > item.MaxReturnQuantityKabba)
                    {
                        MessageBox.Show($"لا يمكن إرجاع أكثر من {item.MaxReturnQuantityDisplay} للصنف {item.ItemName}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
            }

            try
            {
                using var dbTransaction = _dbContext.Database.BeginTransaction();

                // 1. إنشاء مرتجع/استبدال المبيعات
                var salesReturn = new SalesReturn
                {
                    ReturnNumber = ReturnNumber,
                    ReturnDate = ReturnDate,
                    Type = SelectedType,
                    SalesInvoiceId = SelectedInvoice.Id,
                    CustomerId = SelectedInvoice.CustomerId,
                    TotalAmount = SelectedType == ReturnType.Return ? TotalAmount : 0,
                    Notes = Notes,
                    PaymentMethod = SelectedPaymentMethod,
                    TransferNumber = SelectedType == ReturnType.Return ? TransferNumber : null
                };

                _dbContext.SalesReturns.Add(salesReturn);
                _dbContext.SaveChanges();

                // 2. إضافة تفاصيل
                foreach (var item in itemsToProcess)
                {
                    var dbDetail = new SalesReturnDetail
                    {
                        SalesReturnId = salesReturn.Id, 
                        SalesInvoiceDetailId = item.SalesInvoiceDetailId,
                        ProductId = item.ProductId,
                        ThreadNumber = item.ThreadNumber,
                        Quantity = item.Quantity,
                        Unit = item.Unit,
                        OriginalUnit = item.OriginalUnit,
                        Price = item.Price,
                        TotalPrice = SelectedType == ReturnType.Return ? item.TotalPrice : 0,
                        ItemName = item.ItemName,
                        MaxReturnQuantityKabba = item.MaxReturnQuantityKabba
                    };
                    _dbContext.SalesReturnDetails.Add(dbDetail);
                }
                _dbContext.SaveChanges();
                
                // 3. تحديث المخزون (للرجوع والاستبدال)
                if (SelectedType == ReturnType.Return)
                {
                    foreach (var item in itemsToProcess)
                    {
                        var product = _dbContext.Products.FirstOrDefault(p => p.ColorNumber == item.ThreadNumber);
                        if (product != null)
                        {
                            var inventory = _dbContext.Inventories.FirstOrDefault(i => i.ProductId == product.Id);
                            if (inventory != null)
                            {
                                int quantityInKabba = (int)item.GetQuantityInKabba();
                                inventory.Quantity += quantityInKabba;
                            }
                        }
                    }
                }
                else if (SelectedType == ReturnType.Exchange)
                {
                    foreach (var item in itemsToProcess)
                    {
                        var product = _dbContext.Products.FirstOrDefault(p => p.ColorNumber == item.ThreadNumber);
                        if (product != null)
                        {
                            var inventory = _dbContext.Inventories.FirstOrDefault(i => i.ProductId == product.Id);
                            if (inventory != null)
                            {
                                if (item.SalesInvoiceDetailId.HasValue)
                                {
                                    // الأصناف المرتجعة من الفاتورة الأصلية → زيادة المخزون بمقدار (الحد الأقصى - الكمية المرتجعة)
                                    int quantityToAdd = (int)(item.MaxReturnQuantityKabba - item.GetQuantityInKabba());
                                    inventory.Quantity += quantityToAdd;
                                }
                                else
                                {
                                    // الأصناف الجديدة المتبادلة → تقليل المخزون
                                    int quantityToSubtract = (int)item.GetQuantityInKabba();
                                    inventory.Quantity -= quantityToSubtract;
                                }
                            }
                        }
                    }
                }
                _dbContext.SaveChanges();
                
                // 4. تحديث رصيد العميل أو الصندوق أو التحويل (للرجوع فقط)
                if (SelectedType == ReturnType.Return)
                {
                    var customer = _dbContext.Customers.FirstOrDefault(c => c.Id == SelectedInvoice.CustomerId);
                
                    // 5. إنشاء القيد المحاسبي
                    var financialTransaction = new FinancialTransaction
                    {
                        TransactionDate = DateTime.Now,
                        Description = $"مرتجع مبيعات رقم {ReturnNumber} - فاتورة {SelectedInvoice.InvoiceNumber}",
                        ReferenceType = "SalesReturn",
                        ReferenceId = salesReturn.Id // This will be populated after SaveChanges
                    };

                    var salesAccount = _dbContext.Accounts.FirstOrDefault(a => a.Name.Contains("مبيعات") && a.AccountType == AccountType.Revenue || a.Code == "4001");
                    var salesReturnsAccount = _dbContext.Accounts.FirstOrDefault(a => a.Name.Contains("مرتجع مبيعات") || a.Code == "4003");
                    var cashAccount = _dbContext.Accounts.FirstOrDefault(a => a.Name.Contains("صندوق") || a.Code == "1001");
                    var transferAccount = _dbContext.Accounts.FirstOrDefault(a => a.Name.Contains("تحويل") || a.Name.Contains("شبكة") || a.Code == "1002");

                    if (salesAccount == null)
                    {
                        salesAccount = new Account { Name = "إيرادات المبيعات", Code = "4001", AccountType = AccountType.Revenue, IsActive = true };
                        _dbContext.Accounts.Add(salesAccount);
                        _dbContext.SaveChanges();
                    }

                    if (salesReturnsAccount == null)
                    {
                        salesReturnsAccount = new Account { Name = "مرتجع مبيعات", Code = "4003", AccountType = AccountType.Revenue, IsActive = true };
                        _dbContext.Accounts.Add(salesReturnsAccount);
                        _dbContext.SaveChanges();
                    }

                    if (cashAccount == null)
                    {
                        cashAccount = new Account { Name = "صندوق", Code = "1001", AccountType = AccountType.Asset, IsActive = true };
                        _dbContext.Accounts.Add(cashAccount);
                        _dbContext.SaveChanges();
                    }

                    if (transferAccount == null)
                    {
                        transferAccount = new Account { Name = "حساب التحويلات", Code = "1002", AccountType = AccountType.Asset, IsActive = true };
                        _dbContext.Accounts.Add(transferAccount);
                        _dbContext.SaveChanges();
                    }

                    var lines = new List<FinancialTransactionLine>();

                    // دائن: إيرادات المبيعات
                    if (salesAccount != null)
                    {
                        lines.Add(new FinancialTransactionLine
                        {
                            AccountId = salesAccount.Id,
                            Debit = 0,
                            Credit = TotalAmount,
                            Notes = $"مرتجع مبيعات رقم {ReturnNumber}"
                        });
                    }

                    // مدين: مرتجع مبيعات
                    lines.Add(new FinancialTransactionLine
                    {
                        AccountId = salesReturnsAccount.Id,
                        Debit = TotalAmount,
                        Credit = 0,
                        Notes = $"مرتجع مبيعات رقم {ReturnNumber}"
                    });

                    // مدين: العميل دائماً (زيادة رصيده أو التسوية)
                    if (customer?.AccountId.HasValue == true)
                    {
                        string customerNote = $"مرتجع مبيعات رقم {ReturnNumber} | فاتورة مبيعات: {SelectedInvoice.InvoiceNumber}";
                        if (SelectedPaymentMethod == ReturnPaymentMethod.Transfer && !string.IsNullOrWhiteSpace(TransferNumber))
                        {
                            customerNote += $" | رقم الحوالة: {TransferNumber}";
                        }
                        lines.Add(new FinancialTransactionLine
                        {
                            AccountId = customer.AccountId.Value,
                            Debit = TotalAmount,
                            Credit = 0,
                            Notes = customerNote
                        });
                    }

                    // القيود حسب طريقة الإرجاع
                    if (SelectedPaymentMethod == ReturnPaymentMethod.ToCustomerAccount)
                    {
                        // دائن: العميل (تخفيض رصيده)
                        if (customer?.AccountId.HasValue == true)
                        {
                            string customerNote = $"تخفيض رصيد العميل عن مرتجع {ReturnNumber} | فاتورة مبيعات: {SelectedInvoice.InvoiceNumber}";
                            if (!string.IsNullOrWhiteSpace(TransferNumber))
                            {
                                customerNote += $" | رقم الحوالة: {TransferNumber}";
                            }
                            lines.Add(new FinancialTransactionLine
                            {
                                AccountId = customer.AccountId.Value,
                                Debit = 0,
                                Credit = TotalAmount,
                                Notes = customerNote
                            });
                            customer.Balance = (customer.Balance ?? 0) - TotalAmount;
                        }
                    }
                    else if (SelectedPaymentMethod == ReturnPaymentMethod.Cash)
                    {
                        // دائن: الصندوق (تخفيض الصندوق)
                        string cashNote = $"إرجاع نقدي عن مرتجع {ReturnNumber} | فاتورة مبيعات: {SelectedInvoice.InvoiceNumber}";
                        lines.Add(new FinancialTransactionLine
                        {
                            AccountId = cashAccount.Id,
                            Debit = 0,
                            Credit = TotalAmount,
                            Notes = cashNote
                        });
                        
                        // تحديث رصيد العميل (زيادته لان الصندوق يدفع)
                        if (customer != null)
                        {
                            customer.Balance = (customer.Balance ?? 0) - TotalAmount;
                        }
                    }
                    else if (SelectedPaymentMethod == ReturnPaymentMethod.Transfer)
                    {
                        string transferNote = !string.IsNullOrWhiteSpace(TransferNumber) 
                            ? $"إرجاع تحويل عن مرتجع {ReturnNumber} | فاتورة مبيعات: {SelectedInvoice.InvoiceNumber} | رقم الحوالة: {TransferNumber}" 
                            : $"إرجاع تحويل عن مرتجع {ReturnNumber} | فاتورة مبيعات: {SelectedInvoice.InvoiceNumber}";

                        // دائن: حساب التحويلات
                        lines.Add(new FinancialTransactionLine
                        {
                            AccountId = transferAccount.Id,
                            Debit = 0,
                            Credit = TotalAmount,
                            Notes = transferNote
                        });
                        
                        // تحديث رصيد العميل (زيادته لان الحساب التحويلات يدفع)
                        if (customer != null)
                        {
                            customer.Balance = (customer.Balance ?? 0) - TotalAmount;
                        }
                    }

                    financialTransaction.Lines = lines;
                    _dbContext.FinancialTransactions.Add(financialTransaction);
                    _dbContext.SaveChanges();
                } // End of SelectedType == Return block
                
                // حفظ جميع التغييرات مرة واحدة فقط
                _dbContext.SaveChanges();
                dbTransaction.Commit();

                string successMsg = SelectedType == ReturnType.Return ? "تم حفظ المرتجع بنجاح" : "تم حفظ الاستبدال بنجاح";
                MessageBox.Show(successMsg, "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                // مسح البيانات
                Items.Clear();
                SelectedInvoice = null;
                SearchInvoiceText = string.Empty;
                ReturnNumber = "RET-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                Notes = null;
                SelectedPaymentMethod = ReturnPaymentMethod.ToCustomerAccount;
                LoadAllInvoices();
            }
            catch (Exception ex)
            {
                _dbContext.ChangeTracker.Clear(); // Clear tracking on error
                var innerMsg = ex.InnerException?.Message ?? "";
                string errMsg = SelectedType == ReturnType.Return ? "خطأ في حفظ المرتجع" : "خطأ في حفظ الاستبدال";
                MessageBox.Show($"{errMsg}: {ex.Message}\n{innerMsg}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
