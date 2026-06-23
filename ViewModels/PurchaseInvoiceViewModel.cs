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
        private string _invoiceNumber = string.Empty;
        private DateTime _invoiceDate = DateTime.Now;
        private string _containerNumber = string.Empty;
        private string? _category;
        private ObservableCollection<Product> _suggestedProducts;
        private readonly AppDbContext _dbContext;

        public string InvoiceNumber
        {
            get => _invoiceNumber;
            set => SetProperty(ref _invoiceNumber, value);
        }

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
                var invoice = new PurchaseInvoice
                {
                    InvoiceNumber = InvoiceNumber,
                    InvoiceDate = InvoiceDate,
                    ContainerNumber = ContainerNumber,
                    Category = Category
                };

                _dbContext.PurchaseInvoices.Add(invoice);
                _dbContext.SaveChanges();

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
                ClearFields();
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

        private void ClearFields()
        {
            InvoiceNumber = GenerateInvoiceNumber();
            InvoiceDate = DateTime.Now;
            ContainerNumber = string.Empty;
            Category = null;
            InvoiceItems.Clear();
        }
    }
}
