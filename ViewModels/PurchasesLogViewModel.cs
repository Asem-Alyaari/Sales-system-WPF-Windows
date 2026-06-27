using App2.Commands;
using App2.Data;
using App2.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace App2.ViewModels
{
    public class PurchasesLogViewModel : ObservableObject
    {
        private readonly AppDbContext _dbContext;
        private string _searchText = string.Empty;
        private DateTime _startDate;
        private DateTime _endDate;

        public ObservableCollection<PurchaseInvoice> Invoices { get; set; } = new();

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterInvoices();
                }
            }
        }

        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                if (SetProperty(ref _startDate, value))
                {
                    FilterInvoices();
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
                    FilterInvoices();
                }
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand AddInvoiceCommand { get; }
        public ICommand ViewInvoiceCommand { get; }
        public ICommand EditInvoiceCommand { get; }
        public ICommand PostInvoiceCommand { get; }
        public ICommand PostAllInvoicesCommand { get; }

        public PurchasesLogViewModel()
        {
            var factory = new AppDbContextFactory();
            _dbContext = factory.CreateDbContext(null);

            var today = DateTime.Today;
            _startDate = new DateTime(today.Year, today.Month, 1);
            _endDate = today;

            RefreshCommand = new RelayCommand(_ => LoadInvoices());
            AddInvoiceCommand = new RelayCommand(ExecuteAddInvoice);
            ViewInvoiceCommand = new RelayCommand(ExecuteViewInvoice);
            EditInvoiceCommand = new RelayCommand(ExecuteEditInvoice);
            PostInvoiceCommand = new RelayCommand(ExecutePostInvoice);
            PostAllInvoicesCommand = new RelayCommand(ExecutePostAllInvoices);

            LoadInvoices();
        }

        private void LoadInvoices()
        {
            Invoices.Clear();
            var startDate = StartDate.Date;
            var endDate = EndDate.Date.AddDays(1).AddTicks(-1);

            var invoices = _dbContext.PurchaseInvoices
                .Include(i => i.Items)
                .AsNoTracking()
                .Where(i => i.InvoiceDate >= startDate && i.InvoiceDate <= endDate)
                .OrderByDescending(i => i.InvoiceDate)
                .ToList();

            foreach (var invoice in invoices)
            {
                Invoices.Add(invoice);
            }
        }

        private void FilterInvoices()
        {
            var startDate = StartDate.Date;
            var endDate = EndDate.Date.AddDays(1).AddTicks(-1);

            var query = _dbContext.PurchaseInvoices
                .Include(i => i.Items)
                .AsNoTracking()
                .Where(i => i.InvoiceDate >= startDate && i.InvoiceDate <= endDate);

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(i => i.InvoiceNumber.Contains(SearchText) || 
                            i.ContainerNumber.Contains(SearchText) || 
                            (i.Category != null && i.Category.Contains(SearchText)));
            }

            var invoices = query.OrderByDescending(i => i.InvoiceDate).ToList();

            Invoices.Clear();
            foreach (var invoice in invoices)
            {
                Invoices.Add(invoice);
            }
        }

        private void ExecuteAddInvoice(object? parameter)
        {
            var view = new Views.PurchaseInvoiceView();
            var window = new Window
            {
                Title = "إضافة فاتورة شراء",
                Content = view,
                WindowState = WindowState.Maximized,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            window.ShowDialog();
            // Refresh list after new invoice is added
            LoadInvoices();
        }

        private void ExecuteEditInvoice(object? parameter)
        {
            if (parameter is PurchaseInvoice invoice)
            {
                if (invoice.IsPosted)
                {
                    MessageBox.Show("لا يمكن تعديل فاتورة مرحلة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                // تحميل الفاتورة مع العناصر بشكل كامل من قاعدة البيانات
                // بدون تتبع التغييرات حتى لا تحمل التعديلات القديمة
                var fullInvoice = _dbContext.PurchaseInvoices
                    .Include(i => i.Items)
                    .AsNoTracking()
                    .FirstOrDefault(i => i.Id == invoice.Id);

                if (fullInvoice == null) return;

                var viewModel = new PurchaseInvoiceViewModel(fullInvoice);
                var view = new Views.PurchaseInvoiceView { DataContext = viewModel };
                
                var window = new Window
                {
                    Title = "تعديل فاتورة شراء",
                    Content = view,
                    WindowState = WindowState.Maximized,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                // إغلاق النافذة عند طلب الإلغاء من ViewModel
                viewModel.RequestClose += (sender, e) => window.Close();
                
                window.ShowDialog();
                // تحديث القائمة بعد التعديل
                LoadInvoices();
            }
        }

        private void ExecuteViewInvoice(object? parameter)
        {
            if (parameter is PurchaseInvoice invoice)
            {
                // تحميل الفاتورة مع العناصر بشكل كامل من قاعدة البيانات بدون تتبع
                var fullInvoice = _dbContext.PurchaseInvoices
                    .Include(i => i.Items)
                    .AsNoTracking()
                    .FirstOrDefault(i => i.Id == invoice.Id);
                
                if (fullInvoice == null) return;
                
                var viewModel = new PurchaseInvoiceDetailsViewModel(fullInvoice);
                var view = new Views.PurchaseInvoiceDetailsView(viewModel);
                var window = new Window
                {
                    Title = "تفاصيل فاتورة الشراء",
                    Content = view,
                    Width = 1000,
                    Height = 700,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                window.ShowDialog();
            }
        }

        private void ExecutePostInvoice(object? parameter)
        {
            if (parameter is PurchaseInvoice invoice)
            {
                try
                {
                    var dbInvoice = _dbContext.PurchaseInvoices.FirstOrDefault(i => i.Id == invoice.Id);
                    if (dbInvoice != null)
                    {
                        dbInvoice.IsPosted = true;
                        _dbContext.SaveChanges();
                        MessageBox.Show("تم ترحيل الفاتورة بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadInvoices();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في ترحيل الفاتورة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExecutePostAllInvoices(object? parameter)
        {
            try
            {
                var unpostedInvoices = _dbContext.PurchaseInvoices.Where(i => !i.IsPosted).ToList();
                foreach (var invoice in unpostedInvoices)
                {
                    invoice.IsPosted = true;
                }
                _dbContext.SaveChanges();
                MessageBox.Show($"تم ترحيل {unpostedInvoices.Count} فاتورة بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadInvoices();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في ترحيل الفواتير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
