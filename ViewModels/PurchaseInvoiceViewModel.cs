using App2.Commands;
using App2.Data;
using App2.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace App2.ViewModels
{
    public class PurchaseInvoiceViewModel : ObservableObject
    {
        // حدث طلب إغلاق النافذة
        public event EventHandler? RequestClose;
        
        private string _invoiceNumber = string.Empty;
        private DateTime _invoiceDate = DateTime.Now;
        private string _containerNumber = string.Empty;
        private string? _category;
        private bool _isPosted;
        private int? _existingInvoiceId;
        private ObservableCollection<Product> _suggestedProducts;
        private readonly AppDbContext _dbContext;
        
        public bool IsPosted
        {
            get => _isPosted;
            set => SetProperty(ref _isPosted, value);
        }

        public string InvoiceNumber
        {
            get => _invoiceNumber;
            set => SetProperty(ref _invoiceNumber, value);
        }

        public bool IsEditMode => _existingInvoiceId.HasValue;

        public string Title => IsEditMode ? "تعديل فاتورة شراء" : "إضافة فاتورة شراء";
        public string ClearButtonText => IsEditMode ? "إلغاء" : "مسح الحقول";

        public DateTime InvoiceDate
        {
            get => _invoiceDate;
            set => SetProperty(ref _invoiceDate, value);
        }

        public string ContainerNumber
        {
            get => _containerNumber;
            set => SetProperty(ref _containerNumber, value);
        }

        public string? Category
        {
            get => _category;
            set => SetProperty(ref _category, value);
        }

        public ObservableCollection<Product> SuggestedProducts
        {
            get => _suggestedProducts;
            set => SetProperty(ref _suggestedProducts, value);
        }

        public ObservableCollection<PurchaseInvoiceItem> InvoiceItems { get; } = new();

        public List<string> UnitOptions { get; } = new List<string> { "كرتون", "كبة" };

        public ICommand AddItemCommand { get; }
        public ICommand RemoveItemCommand { get; }
        public ICommand SaveInvoiceCommand { get; }
        public ICommand ClearCommand { get; }

        public PurchaseInvoiceViewModel()
        {
            var factory = new AppDbContextFactory();
            _dbContext = factory.CreateDbContext(null);
            _suggestedProducts = new ObservableCollection<Product>();

            AddItemCommand = new RelayCommand(ExecuteAddItem);
            RemoveItemCommand = new RelayCommand(ExecuteRemoveItem);
            SaveInvoiceCommand = new RelayCommand(ExecuteSaveInvoice, CanExecuteSaveInvoice);
            ClearCommand = new RelayCommand(ExecuteClear);

            // توليد رقم الفاتورة تلقائياً
            InvoiceNumber = GenerateInvoiceNumber();
        }

        public PurchaseInvoiceViewModel(PurchaseInvoice invoice) : this()
        {
            // مسح التتبع حتى لا تحمل أي تعديلات قديمة من التتبع
            _dbContext.ChangeTracker.Clear();
            
            _existingInvoiceId = invoice.Id;
            InvoiceNumber = invoice.InvoiceNumber;
            InvoiceDate = invoice.InvoiceDate;
            ContainerNumber = invoice.ContainerNumber ?? string.Empty;
            Category = invoice.Category;
            IsPosted = invoice.IsPosted;

            InvoiceItems.Clear();
            foreach (var item in invoice.Items)
            {
                InvoiceItems.Add(new PurchaseInvoiceItem
                {
                    Id = 0, // Reset to 0 so they are treated as new if added back
                    BoxNumber = item.BoxNumber,
                    Color = item.Color,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    PurchaseInvoiceId = 0
                });
            }
            
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(ClearButtonText));
            OnPropertyChanged(nameof(IsPosted));
        }

        private string GenerateInvoiceNumber()
        {
            // البحث عن آخر رقم فاتورة في قاعدة البيانات
            var lastInvoice = _dbContext.PurchaseInvoices
                .OrderByDescending(i => i.Id)
                .FirstOrDefault();

            int nextNumber = (lastInvoice?.Id ?? 0) + 1;
            return $"PUR-{DateTime.Now:yyyy}-{nextNumber:D4}";
        }

        public void SearchProducts(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                SuggestedProducts.Clear();
                return;
            }

            var products = _dbContext.Products
                .Where(p => p.ColorNumber.Contains(searchTerm) || 
                           (p.Color != null && p.Color.Contains(searchTerm)))
                .ToList();

            SuggestedProducts.Clear();
            foreach (var product in products)
            {
                SuggestedProducts.Add(product);
            }
        }

        public void SelectProduct(Product product)
        {
            // سيتم استدعاؤه عند اختيار منتج من القائمة
        }

        private void ExecuteAddItem(object? parameter)
        {
            InvoiceItems.Add(new PurchaseInvoiceItem
            {
                BoxNumber = string.Empty,
                Color = string.Empty,
                Quantity = 1,
                Unit = "كرتون"
            });
        }

        private void ExecuteRemoveItem(object? parameter)
        {
            if (parameter is PurchaseInvoiceItem item)
            {
                InvoiceItems.Remove(item);
            }
        }

        private bool CanExecuteSaveInvoice(object? parameter)
        {
            return !string.IsNullOrWhiteSpace(InvoiceNumber) &&
                   InvoiceItems.Any();
        }

        private void ExecuteSaveInvoice(object? parameter)
        {
            if (string.IsNullOrWhiteSpace(InvoiceNumber))
            {
                System.Windows.MessageBox.Show("الرجاء إدخال رقم الفاتورة", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }



            var validItems = InvoiceItems.Where(i => !string.IsNullOrWhiteSpace(i.BoxNumber)).ToList();

            if (!validItems.Any())
            {
                System.Windows.MessageBox.Show("لا يمكن حفظ فاتورة فارغة. الرجاء إضافة أصناف", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            // التحقق من أن الكمية أكبر من 0
            if (validItems.Any(i => i.Quantity <= 0))
            {
                System.Windows.MessageBox.Show("لا يمكن حفظ صنف بكمية 0 أو أقل. الرجاء التحقق من الكميات.", "تنبيه", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            try
            {
                PurchaseInvoice invoice;
                var productQuantities = new Dictionary<string, int>(); // key: box number, value: quantity in kabba

                if (IsEditMode)
                {
                    invoice = _dbContext.PurchaseInvoices
                        .Include(i => i.Items)
                        .FirstOrDefault(i => i.Id == _existingInvoiceId)
                        ?? throw new Exception("لم يتم العثور على الفاتورة الأصلية في قاعدة البيانات");

                    // First, calculate old quantities per product
                    foreach (var oldItem in invoice.Items)
                    {
                        int oldQuantityInKabba = oldItem.Unit == "كرتون" ? oldItem.Quantity * Inventory.KabbaPerCarton : oldItem.Quantity;
                        if (productQuantities.ContainsKey(oldItem.BoxNumber))
                            productQuantities[oldItem.BoxNumber] -= oldQuantityInKabba;
                        else
                            productQuantities[oldItem.BoxNumber] = -oldQuantityInKabba;
                    }

                    // Then calculate new quantities per product
                    foreach (var newItem in validItems)
                    {
                        int newQuantityInKabba = newItem.Unit == "كرتون" ? newItem.Quantity * Inventory.KabbaPerCarton : newItem.Quantity;
                        if (productQuantities.ContainsKey(newItem.BoxNumber))
                            productQuantities[newItem.BoxNumber] += newQuantityInKabba;
                        else
                            productQuantities[newItem.BoxNumber] = newQuantityInKabba;
                    }

                    // Now check inventory for each product
                    foreach (var kvp in productQuantities)
                    {
                        var product = _dbContext.Products.FirstOrDefault(p => p.ColorNumber == kvp.Key);
                        if (product != null)
                        {
                            var inventoryRecord = _dbContext.Inventories.FirstOrDefault(i => i.ProductId == product.Id);
                            int currentInventory = inventoryRecord?.Quantity ?? 0;
                            int newInventory = currentInventory + kvp.Value;
                            if (newInventory < 0)
                            {
                                System.Windows.MessageBox.Show($"الكمية في المخزون للصنف {kvp.Key} لا تكفي. الكمية المتاحة: {currentInventory} كبة، المطلوبة: {Math.Abs(kvp.Value)} كبة", "خطأ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                                return;
                            }
                        }
                    }

                    // Now adjust inventory
                    foreach (var oldItem in invoice.Items)
                    {
                        var product = _dbContext.Products.FirstOrDefault(p => p.ColorNumber == oldItem.BoxNumber);
                        if (product != null)
                        {
                            var inventoryRecord = _dbContext.Inventories.FirstOrDefault(i => i.ProductId == product.Id);
                            if (inventoryRecord != null)
                            {
                                int oldQuantityInKabba = oldItem.Unit == "كرتون" ? oldItem.Quantity * Inventory.KabbaPerCarton : oldItem.Quantity;
                                inventoryRecord.Quantity -= oldQuantityInKabba;
                            }
                        }
                    }

                    // تحديث بيانات الفاتورة الأساسية
                    invoice.InvoiceNumber = InvoiceNumber;
                    invoice.InvoiceDate = InvoiceDate;
                    invoice.ContainerNumber = ContainerNumber;
                    invoice.Category = Category;

                    // حذف الأصناف القديمة (سيتم إضافة الجديدة في الخطوة التالية)
                    _dbContext.PurchaseInvoiceItems.RemoveRange(invoice.Items);
                }
                else
                {
                    invoice = new PurchaseInvoice
                    {
                        InvoiceNumber = InvoiceNumber,
                        InvoiceDate = InvoiceDate,
                        ContainerNumber = ContainerNumber,
                        Category = Category
                    };
                    _dbContext.PurchaseInvoices.Add(invoice);
                    // حفظ الفاتورة الجديدة للحصول على معرفها (ID)
                    _dbContext.SaveChanges();
                }

                var newProductsCache = new Dictionary<string, Product>();

                foreach (var item in validItems)
                {
                    item.PurchaseInvoiceId = invoice.Id;
                    
                    // تأكد من أن اللون ليس فارغاً (لتجنب خطأ قاعدة البيانات)
                    if (string.IsNullOrWhiteSpace(item.Color))
                    {
                        item.Color = "بدون لون";
                    }

                    _dbContext.PurchaseInvoiceItems.Add(item);

                    // الحصول على المنتج (من قاعدة البيانات أو من المنتجات الجديدة التي أضفناها للتو)
                    Product? currentProduct = _dbContext.Products.FirstOrDefault(p => p.ColorNumber == item.BoxNumber);
                    
                    if (currentProduct == null)
                    {
                        if (newProductsCache.TryGetValue(item.BoxNumber, out var cachedProduct))
                        {
                            currentProduct = cachedProduct;
                        }
                        else
                        {
                            currentProduct = new Product
                            {
                                Color = item.Color == "بدون لون" ? "" : item.Color,
                                ColorNumber = item.BoxNumber
                            };
                            _dbContext.Products.Add(currentProduct);
                            newProductsCache[item.BoxNumber] = currentProduct;
                        }
                    }

                    // إضافة سجل المخزون أو تحديثه
                    int quantityInKabba = item.Unit == "كرتون" ? item.Quantity * Inventory.KabbaPerCarton : item.Quantity;
                    
                    var existingInventory = _dbContext.Inventories.Local.FirstOrDefault(i => i.Product == currentProduct)
                                            ?? _dbContext.Inventories.FirstOrDefault(i => i.Product == currentProduct);

                    if (existingInventory != null)
                    {
                        existingInventory.Quantity += quantityInKabba;
                        existingInventory.DateAdded = DateTime.Now;
                        existingInventory.InvoiceNumber = invoice.InvoiceNumber;
                    }
                    else
                    {
                        var inventoryRecord = new Inventory
                        {
                            Product = currentProduct,
                            Quantity = quantityInKabba,
                            Unit = "كبة",
                            InvoiceNumber = invoice.InvoiceNumber,
                            DateAdded = DateTime.Now
                        };
                        _dbContext.Inventories.Add(inventoryRecord);
                    }
                }

                _dbContext.SaveChanges();

                System.Windows.MessageBox.Show("تم حفظ الفاتورة بنجاح", "نجاح", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                
                if (IsEditMode)
                {
                    RequestClose?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    ClearFields();
                }
            }
            catch (Exception ex)
            {
                // مسح التتبع حتى لا تتراكم الكيانات في حالة فشل الحفظ السابق
                _dbContext.ChangeTracker.Clear();

                // إعادة تعيين أرقام المعرفات (Id) للأصناف إلى صفر حتى يعتبرها إدخالات جديدة
                foreach (var item in validItems)
                {
                    item.Id = 0;
                    item.PurchaseInvoiceId = 0;
                }

                // طباعة تفاصيل الخطأ للتشخيص في حالة حدوث استثناء داخلي من EF Core
                string errorMsg = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMsg += "\nالتفاصيل: " + ex.InnerException.Message;
                }
                System.Windows.MessageBox.Show($"خطأ في حفظ الفاتورة:\n{errorMsg}", "خطأ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteClear(object? parameter)
        {
            if (IsEditMode)
            {
                var result = System.Windows.MessageBox.Show(
                    "هل تريد إلغاء التعديل وإغلاق النافذة؟",
                    "تأكيد الإلغاء",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning,
                    System.Windows.MessageBoxResult.No);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    RequestClose?.Invoke(this, EventArgs.Empty);
                }
            }
            else
            {
                var result = System.Windows.MessageBox.Show(
                    "هل أنت متأكد من مسح جميع الحقول؟",
                    "تأكيد المسح",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning,
                    System.Windows.MessageBoxResult.No);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    ClearFields();
                }
            }
        }

        private void ClearFields()
        {
            _existingInvoiceId = null;
            InvoiceNumber = GenerateInvoiceNumber();
            InvoiceDate = DateTime.Now;
            ContainerNumber = string.Empty;
            Category = null;
            InvoiceItems.Clear();
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(ClearButtonText));
        }
    }
}
