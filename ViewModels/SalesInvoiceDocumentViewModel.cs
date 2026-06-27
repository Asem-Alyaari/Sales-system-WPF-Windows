using App2.Data;
using App2.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace App2.ViewModels
{
    public class BatchItemViewModel
    {
        public Inventory InventoryBatch { get; set; } = null!;
        public string DisplayText => $"{InventoryBatch.Product?.ColorNumber} - {InventoryBatch.Product?.Color} | الرصيد: {InventoryBatch.DisplayQuantity}";

        public override string ToString()
        {
            return DisplayText;
        }
    }

    public class SalesInvoiceDocumentViewModel : ObservableObject
    {
        private readonly AppDbContext _dbContext;
        private string _invoiceNumber = string.Empty;
        private Customer? _selectedCustomer;
        private string _searchCustomerText = string.Empty;
        private bool _isCustomerDropdownOpen;
        private string _searchItemNumber = string.Empty;
        private BatchItemViewModel? _selectedBatch;
        private bool _isDropdownOpen;
        private bool _isProcessingSelection;

        private bool _isCashSale;
        private bool _isCashCustomer;

        public bool IsCashSale
        {
            get => _isCashSale;
            set
            {
                if (SetProperty(ref _isCashSale, value))
                {
                    if (value)
                    {
                        SetCashCustomer();
                    }
                    else
                    {
                        SelectedCustomer = null;
                        SearchCustomerText = string.Empty;
                    }
                }
            }
        }

        public bool IsCashCustomer
        {
            get => _isCashCustomer;
            private set => SetProperty(ref _isCashCustomer, value);
        }

        private async void SetCashCustomer()
        {
            try
            {
                var cashCustomer = await _dbContext.Customers
                    .Include(c => c.Account)
                    .FirstOrDefaultAsync(c => c.Name == "عميل نقدي");

                if (cashCustomer == null)
                {
                    // إنشاء حساب للعميل النقدي
                    var cashAccount = new Account
                    {
                        Name = "حساب عملاء نقدي",
                        Code = "1201", // كود افتراضي للعملاء النقديين
                        AccountType = AccountType.Asset,
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    };
                    _dbContext.Accounts.Add(cashAccount);
                    await _dbContext.SaveChangesAsync();

                    cashCustomer = new Customer
                    {
                        Name = "عميل نقدي",
                        Phone = "0000",
                        Balance = 0,
                        AddedDate = DateTime.Now,
                        AccountId = cashAccount.Id
                    };
                    _dbContext.Customers.Add(cashCustomer);
                    await _dbContext.SaveChangesAsync();
                }

                SelectedCustomer = cashCustomer;
                SearchCustomerText = cashCustomer.Name;
                IsCashCustomer = true;

                // تلقائياً جعل المبلغ مدفوع نقداً
                if (TotalAmount > 0)
                {
                    PaidCash = AmountAfterDiscount;
                    PaidNetwork = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تعيين العميل النقدي: {ex.Message}");
            }
        }

        public SalesInvoiceDocumentViewModel(AppDbContext dbContext)
        {
            _dbContext = dbContext;
            RemoveItemCommand = new App2.Commands.RelayCommand(ExecuteRemoveItem);
            SaveInvoiceCommand = new App2.Commands.RelayCommand(ExecuteSaveInvoice, CanExecuteSaveInvoice);
            ToggleCashSaleCommand = new App2.Commands.RelayCommand(_ => IsCashSale = !IsCashSale);
            Items.CollectionChanged += Items_CollectionChanged;
            InvoiceDate = DateTime.Now;
            // رقم الفاتورة التلقائي بناءً على التوقيت
            InvoiceNumber = "INV-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            Discount = 0;
        }

        public System.Windows.Input.ICommand ToggleCashSaleCommand { get; }

        private void Items_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (SalesInvoiceDetail item in e.NewItems)
                {
                    item.PropertyChanged += Item_PropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (SalesInvoiceDetail item in e.OldItems)
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                }
            }
            UpdateTotals();
        }

        private void Item_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SalesInvoiceDetail.TotalPrice))
            {
                UpdateTotals();
            }
        }

        public System.Windows.Input.ICommand RemoveItemCommand { get; }
        public System.Windows.Input.ICommand SaveInvoiceCommand { get; }

        private void ExecuteRemoveItem(object? parameter)
        {
            if (parameter is SalesInvoiceDetail detail)
            {
                Items.Remove(detail);
            }
        }

        private bool CanExecuteSaveInvoice(object? parameter)
        {
            if (SelectedCustomer == null || !Items.Any() || !Items.All(i => i.Price > 0 && i.Quantity > 0))
            {
                return false;
            }

            // For cash customer, prevent deferred payment (RemainingAmount must be 0)
            if (IsCashCustomer && RemainingAmount > 0)
            {
                return false;
            }

            // If payment includes transfer, transfer number is mandatory
            if (PaidNetwork > 0 && string.IsNullOrWhiteSpace(TransferNumber))
            {
                return false;
            }

            return true;
        }

        private void ExecuteSaveInvoice(object? parameter)
        {
            if (SelectedCustomer == null)
            {
                MessageBox.Show("الرجاء اختيار العميل", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Items.Any())
            {
                MessageBox.Show("لا يمكن حفظ فاتورة فارغة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Items.Any(i => i.Price <= 0 || i.Quantity <= 0))
            {
                MessageBox.Show("الرجاء التأكد من أن جميع الأصناف لها سعر وكمية صحيحة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 🛑 خطوة الإصلاح الجوهري: التحقق المسبق من توفر الكميات لجميع الأصناف قبل أي عملية حفظ 🛑
                foreach (var item in Items)
                {
                    var product = _dbContext.Products.FirstOrDefault(p => p.ColorNumber == item.ThreadNumber);
                    if (product == null)
                    {
                        MessageBox.Show($"الصنف ذو الرقم {item.ThreadNumber} غير موجود في النظام!", "خطأ في التحقق", MessageBoxButton.OK, MessageBoxImage.Error);
                        return; // إلغاء الحفظ فوراً
                    }

                    var inventory = _dbContext.Inventories.FirstOrDefault(i => i.ProductId == product.Id);
                    int quantityInKabba = (int)(item.Unit == UnitType.Carton ? item.Quantity * Inventory.KabbaPerCarton : item.Quantity);

                    if (inventory == null || inventory.Quantity < quantityInKabba)
                    {
                        int available = inventory?.Quantity ?? 0;
                        MessageBox.Show($"لا يمكن الحفظ! الرصيد غير كافٍ للصنف ({item.ItemName}).\nالكمية المطلوبة: {quantityInKabba} كبة.\nالكمية المتوفرة في المخزن: {available} كبة.",
                                        "رصيد غير كافٍ", MessageBoxButton.OK, MessageBoxImage.Stop);
                        return; // إلغاء الحفظ فوراً ومنع استمرار الدالة
                    }
                }

                // 1. حفظ الفاتورة (لن نصل إلى هنا إلا إذا كانت جميع الكميات متوفرة 100%)
                var invoice = new SalesInvoice
                {
                    InvoiceNumber = InvoiceNumber,
                    InvoiceDate = InvoiceDate,
                    CustomerId = SelectedCustomer.Id,
                    Total = TotalAmount,
                    Discount = Discount,
                    PaidInCash = PaidCash,
                    Transfer = PaidNetwork,
                    Deferred = RemainingAmount,
                    TransferNumber = TransferNumber
                };

                // Update Customer's Balance
                SelectedCustomer.Balance = (SelectedCustomer.Balance ?? 0) + RemainingAmount;

                // Tell EF Core to update the Customer!
                _dbContext.Customers.Update(SelectedCustomer);

                _dbContext.SalesInvoices.Add(invoice);
                _dbContext.SaveChanges();

                // 2. حفظ تفاصيل الفاتورة
                foreach (var item in Items)
                {
                    item.SalesInvoiceId = invoice.Id;
                    _dbContext.SalesInvoiceDetails.Add(item);
                }
                _dbContext.SaveChanges();

                // 3. إنشاء القيد المحاسبي
                var transaction = new FinancialTransaction
                {
                    TransactionDate = DateTime.Now,
                    Description = $"فاتورة مبيعات رقم {InvoiceNumber} - {SelectedCustomer.Name}",
                    ReferenceType = "SalesInvoice",
                    ReferenceId = invoice.Id
                };

                // البحث عن الحسابات المطلوبة
                var cashAccount = _dbContext.Accounts.FirstOrDefault(a => a.Name.Contains("صندوق") || a.Code == "1001");
                var transferAccount = _dbContext.Accounts.FirstOrDefault(a => a.Name.Contains("تحويل") || a.Name.Contains("شبكة") || a.Code == "1002");
                var salesAccount = _dbContext.Accounts.FirstOrDefault(a => a.Name.Contains("مبيعات") && a.AccountType == AccountType.Revenue || a.Code == "4001");
                var discountAccount = _dbContext.Accounts.FirstOrDefault(a => a.Name.Contains("خصم") && (a.AccountType == AccountType.Revenue || a.AccountType == AccountType.Expense) || a.Code == "4002");

                // إنشاء الحسابات إذا لم تكن موجودة
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

                if (salesAccount == null)
                {
                    salesAccount = new Account { Name = "إيرادات المبيعات", Code = "4001", AccountType = AccountType.Revenue, IsActive = true };
                    _dbContext.Accounts.Add(salesAccount);
                    _dbContext.SaveChanges();
                }

                if (discountAccount == null)
                {
                    discountAccount = new Account { Name = "خصم مسموح به", Code = "4002", AccountType = AccountType.Revenue, IsActive = true };
                    _dbContext.Accounts.Add(discountAccount);
                    _dbContext.SaveChanges();
                }

                // إضافة قيود المحاسبة
                var lines = new List<FinancialTransactionLine>();

                // 1. مدين: العميل (بإجمالي الفاتورة قبل الخصم)
                if (SelectedCustomer.AccountId.HasValue)
                {
                    lines.Add(new FinancialTransactionLine
                    {
                        AccountId = SelectedCustomer.AccountId.Value,
                        Debit = TotalAmount,
                        Credit = 0,
                        Notes = $"فاتورة مبيعات رقم {InvoiceNumber}"
                    });
                }

                // 2. دائن: إيرادات المبيعات (بإجمالي الفاتورة قبل الخصم)
                lines.Add(new FinancialTransactionLine
                {
                    AccountId = salesAccount.Id,
                    Debit = 0,
                    Credit = TotalAmount,
                    Notes = $"مبيعات فاتورة رقم {InvoiceNumber} - {SelectedCustomer.Name}"
                });

                // 3. إذا كان هناك خصم
                if (Discount > 0)
                {
                    // مدين: حساب الخصم
                    lines.Add(new FinancialTransactionLine
                    {
                        AccountId = discountAccount.Id,
                        Debit = Discount,
                        Credit = 0,
                        Notes = $"خصم مسموح به فاتورة {InvoiceNumber} - {SelectedCustomer.Name}"
                    });

                    // دائن: العميل
                    if (SelectedCustomer.AccountId.HasValue)
                    {
                        lines.Add(new FinancialTransactionLine
                        {
                            AccountId = SelectedCustomer.AccountId.Value,
                            Debit = 0,
                            Credit = Discount,
                            Notes = $"خصم مسموح به - فاتورة {InvoiceNumber}"
                        });
                    }
                }

                // 4. إذا كان هناك مدفوع نقداً
                if (PaidCash > 0)
                {
                    // مدين: الصندوق
                    lines.Add(new FinancialTransactionLine
                    {
                        AccountId = cashAccount.Id,
                        Debit = PaidCash,
                        Credit = 0,
                        Notes = $"سداد نقدي فاتورة {InvoiceNumber} - {SelectedCustomer.Name}"
                    });

                    // دائن: العميل
                    if (SelectedCustomer.AccountId.HasValue)
                    {
                        lines.Add(new FinancialTransactionLine
                        {
                            AccountId = SelectedCustomer.AccountId.Value,
                            Debit = 0,
                            Credit = PaidCash,
                            Notes = $"سداد نقدي - فاتورة {InvoiceNumber}"
                        });
                    }
                }

                // 5. إذا كان هناك مدفوع شبكة/تحويل
                if (PaidNetwork > 0)
                {
                    string transferNote = !string.IsNullOrWhiteSpace(TransferNumber) 
                        ? $"سداد شبكة/تحويل فاتورة {InvoiceNumber} - {SelectedCustomer.Name} | رقم الحوالة: {TransferNumber}" 
                        : $"سداد شبكة/تحويل فاتورة {InvoiceNumber} - {SelectedCustomer.Name}";
                        
                    string customerTransferNote = !string.IsNullOrWhiteSpace(TransferNumber) 
                        ? $"سداد شبكة/تحويل - فاتورة {InvoiceNumber} | رقم الحوالة: {TransferNumber}" 
                        : $"سداد شبكة/تحويل - فاتورة {InvoiceNumber}";
                        
                    // مدين: حساب التحويلات
                    lines.Add(new FinancialTransactionLine
                    {
                        AccountId = transferAccount.Id,
                        Debit = PaidNetwork,
                        Credit = 0,
                        Notes = transferNote
                    });

                    // دائن: العميل
                    if (SelectedCustomer.AccountId.HasValue)
                    {
                        lines.Add(new FinancialTransactionLine
                        {
                            AccountId = SelectedCustomer.AccountId.Value,
                            Debit = 0,
                            Credit = PaidNetwork,
                            Notes = customerTransferNote
                        });
                    }
                }

                transaction.Lines = lines;
                _dbContext.FinancialTransactions.Add(transaction);
                _dbContext.SaveChanges();

                // 4. خصم الكميات من المخزون (تتم بأمان الآن لأننا ضمنا توفر الكمية مسبقاً)
                foreach (var item in Items)
                {
                    var product = _dbContext.Products.FirstOrDefault(p => p.ColorNumber == item.ThreadNumber);
                    if (product != null)
                    {
                        var inventory = _dbContext.Inventories.FirstOrDefault(i => i.ProductId == product.Id);
                        if (inventory != null)
                        {
                            int quantityInKabba = (int)(item.Unit == UnitType.Carton ? item.Quantity * Inventory.KabbaPerCarton : item.Quantity);
                            inventory.Quantity -= quantityInKabba;
                        }
                    }
                }
                _dbContext.SaveChanges();

                MessageBox.Show("تم حفظ الفاتورة بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                // مسح الفاتورة الحالية
                Items.Clear();
                InvoiceNumber = "INV-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                Discount = 0;
                PaidCash = 0;
                PaidNetwork = 0;
            }
            catch (Exception ex)
            {
                _dbContext.ChangeTracker.Clear();
                MessageBox.Show($"خطأ في حفظ الفاتورة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public string TabTitle => string.IsNullOrWhiteSpace(InvoiceNumber) ? "فاتورة جديدة" : $"فاتورة: {InvoiceNumber}";

        public string InvoiceNumber
        {
            get => _invoiceNumber;
            set
            {
                if (SetProperty(ref _invoiceNumber, value))
                {
                    OnPropertyChanged(nameof(TabTitle));
                }
            }
        }

        private DateTime _invoiceDate;
        public DateTime InvoiceDate
        {
            get => _invoiceDate;
            set => SetProperty(ref _invoiceDate, value);
        }

        public Customer? SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                if (SetProperty(ref _selectedCustomer, value))
                {
                    // Check if the selected customer is the cash customer
                    IsCashCustomer = value != null && value.Name == "عميل نقدي";

                    // If cash customer, automatically set full payment
                    if (IsCashCustomer && TotalAmount > 0)
                    {
                        PaidCash = AmountAfterDiscount;
                        PaidNetwork = 0;
                    }
                }
            }
        }

        public void ConfirmCustomerSelection()
        {
            if (SelectedCustomer != null)
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _searchCustomerText = string.Empty;
                    OnPropertyChanged(nameof(SearchCustomerText));
                    IsCustomerDropdownOpen = false;
                }, DispatcherPriority.ContextIdle);
            }
        }

        public string SearchCustomerText
        {
            get => _searchCustomerText;
            set
            {
                if (SetProperty(ref _searchCustomerText, value))
                {
                    SearchCustomers();
                }
            }
        }

        public bool IsCustomerDropdownOpen
        {
            get => _isCustomerDropdownOpen;
            set => SetProperty(ref _isCustomerDropdownOpen, value);
        }

        public ObservableCollection<Customer> FilteredCustomers { get; } = new ObservableCollection<Customer>();

        public ObservableCollection<SalesInvoiceDetail> Items { get; } = new ObservableCollection<SalesInvoiceDetail>();

        private decimal _totalAmount;
        public decimal TotalAmount
        {
            get => _totalAmount;
            private set
            {
                if (SetProperty(ref _totalAmount, value))
                {
                    OnPropertyChanged(nameof(AmountAfterDiscount));
                    if (IsCashSale || IsCashCustomer)
                    {
                        PaidCash = AmountAfterDiscount;
                        PaidNetwork = 0;
                    }
                    OnPropertyChanged(nameof(RemainingAmount));
                }
            }
        }

        private decimal _paidCash;
        public decimal PaidCash
        {
            get => _paidCash;
            set
            {
                // For cash customer, prevent deferred payment
                if (IsCashCustomer)
                {
                    var maxAllowed = AmountAfterDiscount - PaidNetwork;
                    if (value > maxAllowed)
                    {
                        value = maxAllowed;
                    }
                }

                if (SetProperty(ref _paidCash, value))
                {
                    OnPropertyChanged(nameof(RemainingAmount));
                }
            }
        }

        private decimal _paidNetwork;
        public decimal PaidNetwork
        {
            get => _paidNetwork;
            set
            {
                // For cash customer, prevent deferred payment
                if (IsCashCustomer)
                {
                    var maxAllowed = AmountAfterDiscount - PaidCash;
                    if (value > maxAllowed)
                    {
                        value = maxAllowed;
                    }
                }

                if (SetProperty(ref _paidNetwork, value))
                {
                    OnPropertyChanged(nameof(RemainingAmount));
                }
            }
        }
        
        private string? _transferNumber;
        public string? TransferNumber
        {
            get => _transferNumber;
            set => SetProperty(ref _transferNumber, value);
        }

        private decimal _discount;
        public decimal Discount
        {
            get => _discount;
            set
            {
                if (SetProperty(ref _discount, value))
                {
                    OnPropertyChanged(nameof(AmountAfterDiscount));
                    if (IsCashSale || IsCashCustomer)
                    {
                        PaidCash = AmountAfterDiscount;
                        PaidNetwork = 0;
                    }
                    OnPropertyChanged(nameof(RemainingAmount));
                }
            }
        }

        public decimal AmountAfterDiscount => TotalAmount - Discount;

        public decimal RemainingAmount => AmountAfterDiscount - PaidCash - PaidNetwork;

        private void UpdateTotals()
        {
            TotalAmount = Items.Sum(i => i.TotalPrice);
        }

        public ObservableCollection<BatchItemViewModel> AvailableBatches { get; } = new ObservableCollection<BatchItemViewModel>();

        public string SearchItemNumber
        {
            get => _searchItemNumber;
            set
            {
                if (_isProcessingSelection) return;
                if (_searchItemNumber == value) return;

                _searchItemNumber = value;

                // لا نستخدم SetProperty لأن رفع حدث PropertyChanged أثناء الكتابة 
                // يؤدي إلى تحديد (تظليل) النص داخل ComboBox ومسح ما كتبه المستخدم

                if (!string.IsNullOrEmpty(value))
                {
                    SearchBatches();
                }
                else
                {
                    AvailableBatches.Clear();
                    IsDropdownOpen = false;
                }
            }
        }

        public bool IsDropdownOpen
        {
            get => _isDropdownOpen;
            set => SetProperty(ref _isDropdownOpen, value);
        }

        public BatchItemViewModel? SelectedBatch
        {
            get => _selectedBatch;
            set => SetProperty(ref _selectedBatch, value);
        }

        // سنضيف هذا الأمر ليتم استدعاؤه عند الضغط على Enter أو عند اختيار عنصر
        public void ConfirmSelection()
        {
            if (SelectedBatch != null)
            {
                _isProcessingSelection = true;
                AddBatchToInvoice(SelectedBatch.InventoryBatch);

                System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    SelectedBatch = null;
                    _searchItemNumber = string.Empty;
                    OnPropertyChanged(nameof(SearchItemNumber));
                    AvailableBatches.Clear();
                    IsDropdownOpen = false;
                    _isProcessingSelection = false;
                }, DispatcherPriority.ContextIdle);
            }
        }

        private void SearchBatches()
        {
            AvailableBatches.Clear();
            if (string.IsNullOrWhiteSpace(SearchItemNumber))
            {
                IsDropdownOpen = false;
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
                AvailableBatches.Add(new BatchItemViewModel { InventoryBatch = item });
            }
            IsDropdownOpen = AvailableBatches.Count > 0;
        }

        private void SearchCustomers()
        {
            FilteredCustomers.Clear();
            if (string.IsNullOrWhiteSpace(SearchCustomerText))
            {
                IsCustomerDropdownOpen = false;
                return;
            }

            var customers = _dbContext.Customers
                .Where(c => c.Name.Contains(SearchCustomerText) || (c.Phone != null && c.Phone.Contains(SearchCustomerText)))
                .ToList();

            foreach (var c in customers)
            {
                FilteredCustomers.Add(c);
            }
            IsCustomerDropdownOpen = FilteredCustomers.Count > 0;
        }

        private void AddBatchToInvoice(Inventory batch)
        {
            var detail = new SalesInvoiceDetail
            {
                ProductId = batch.ProductId,
                ThreadNumber = batch.Product?.ColorNumber ?? "",
                ItemName = batch.Product?.Color ?? "",
                Quantity = 1,
                Unit = UnitType.Skein, // افتراضياً كبة
                Price = 0,
                TotalPrice = 0
            };

            // إضافة العنصر في أعلى القائمة
            Items.Insert(0, detail);
        }
    }
}