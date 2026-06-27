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
    public class SalesLogViewModel : ObservableObject
    {
        private readonly AppDbContext _dbContext;
        private string _searchText = string.Empty;
        private DateTime _startDate;
        private DateTime _endDate;
        private SalesFilterType? _selectedFilterType = null;

        public ObservableCollection<SalesLogItem> Items { get; set; } = new();
        public ObservableCollection<SalesFilterType> FilterTypes { get; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterItems();
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
                    FilterItems();
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
                    FilterItems();
                }
            }
        }

        public SalesFilterType? SelectedFilterType
        {
            get => _selectedFilterType;
            set
            {
                if (SetProperty(ref _selectedFilterType, value))
                {
                    FilterItems();
                }
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand AddInvoiceCommand { get; }
        public ICommand AddReturnCommand { get; }
        public ICommand ViewDetailsCommand { get; }

        public SalesLogViewModel()
        {
            var factory = new AppDbContextFactory();
            _dbContext = factory.CreateDbContext(null);

            FilterTypes = new ObservableCollection<SalesFilterType>
            {
                new SalesFilterType { Type = null, DisplayName = "الكل" },
                new SalesFilterType { Type = SalesLogItemType.Invoice, DisplayName = "فاتورة مبيعات" },
                new SalesFilterType { Type = SalesLogItemType.Return, DisplayName = "مرتجع" }
            };

            var today = DateTime.Today;
            _startDate = new DateTime(today.Year, today.Month, 1);
            _endDate = today;

            RefreshCommand = new RelayCommand(_ => LoadItems());
            AddInvoiceCommand = new RelayCommand(ExecuteAddInvoice);
            AddReturnCommand = new RelayCommand(ExecuteAddReturn);
            ViewDetailsCommand = new RelayCommand(ExecuteViewDetails);

            LoadItems();
        }

        private void ExecuteViewDetails(object? parameter)
        {
            if (parameter is SalesLogItem item)
            {
                if (item.Type == SalesLogItemType.Invoice)
                {
                    var invoice = _dbContext.SalesInvoices
                        .Include(i => i.Customer)
                        .Include(i => i.Details)
                        .FirstOrDefault(i => i.Id == item.Id);
                    if (invoice != null)
                    {
                        var viewModel = new SalesInvoiceDetailsViewModel(invoice);
                        var view = new Views.SalesInvoiceDetailsView(viewModel);
                        var window = new Window
                        {
                            Title = "تفاصيل فاتورة المبيعات",
                            Content = view,
                            Width = 850,
                            Height = 650,
                            WindowStartupLocation = WindowStartupLocation.CenterScreen
                        };
                        window.ShowDialog();
                    }
                }
                else if (item.Type == SalesLogItemType.Return)
                {
                    var returnItem = _dbContext.SalesReturns
                        .Include(r => r.Customer)
                        .Include(r => r.Details)
                        .FirstOrDefault(r => r.Id == item.Id);
                    if (returnItem != null)
                    {
                        var viewModel = new SalesReturnDetailsViewModel(returnItem);
                        var view = new Views.SalesReturnDetailsView(viewModel);
                        var window = new Window
                        {
                            Title = "تفاصيل فاتورة المرتجع",
                            Content = view,
                            Width = 850,
                            Height = 650,
                            WindowStartupLocation = WindowStartupLocation.CenterScreen
                        };
                        window.ShowDialog();
                    }
                }
            }
        }

        private void LoadItems()
        {
            Items.Clear();
            var startDate = StartDate.Date;
            var endDate = EndDate.Date.AddDays(1).AddTicks(-1);

            var salesLogItems = new List<SalesLogItem>();

            var invoices = _dbContext.SalesInvoices
                .Include(i => i.Customer)
                .Where(i => i.InvoiceDate >= startDate && i.InvoiceDate <= endDate)
                .OrderByDescending(i => i.InvoiceDate)
                .ToList();

            foreach (var invoice in invoices)
            {
                salesLogItems.Add(new SalesLogItem
                {
                    Id = invoice.Id,
                    Type = SalesLogItemType.Invoice,
                    Number = invoice.InvoiceNumber,
                    Date = invoice.InvoiceDate,
                    CustomerName = invoice.Customer?.Name ?? "غير معرف",
                    TotalAmount = invoice.Total,
                    Notes = null
                });
            }

            var returns = _dbContext.SalesReturns
                .Include(r => r.Customer)
                .Where(r => r.ReturnDate >= startDate && r.ReturnDate <= endDate)
                .OrderByDescending(r => r.ReturnDate)
                .ToList();

            foreach (var returnItem in returns)
            {
                string paymentMethodDisplay = returnItem.PaymentMethod switch
                {
                    ReturnPaymentMethod.ToCustomerAccount => "إلى حساب العميل",
                    ReturnPaymentMethod.Cash => "نقدي من الصندوق",
                    ReturnPaymentMethod.Transfer => "تحويل/شبكة",
                    _ => "غير محدد"
                };

                salesLogItems.Add(new SalesLogItem
                {
                    Id = returnItem.Id,
                    Type = SalesLogItemType.Return,
                    Number = returnItem.ReturnNumber,
                    Date = returnItem.ReturnDate,
                    CustomerName = returnItem.Customer?.Name ?? "غير معرف",
                    TotalAmount = returnItem.TotalAmount,
                    Notes = returnItem.Notes,
                    TransferNumber = returnItem.TransferNumber,
                    PaymentMethodDisplayName = paymentMethodDisplay
                });
            }

            foreach (var item in salesLogItems.OrderByDescending(x => x.Date))
            {
                Items.Add(item);
            }
        }

        private void FilterItems()
        {
            var startDate = StartDate.Date;
            var endDate = EndDate.Date.AddDays(1).AddTicks(-1);

            var salesLogItems = new List<SalesLogItem>();

            var invoices = _dbContext.SalesInvoices
                .Include(i => i.Customer)
                .Where(i => i.InvoiceDate >= startDate && i.InvoiceDate <= endDate);

            var returns = _dbContext.SalesReturns
                .Include(r => r.Customer)
                .Where(r => r.ReturnDate >= startDate && r.ReturnDate <= endDate);

            if (SelectedFilterType?.Type == SalesLogItemType.Invoice)
            {
                returns = Enumerable.Empty<SalesReturn>().AsQueryable();
            }
            else if (SelectedFilterType?.Type == SalesLogItemType.Return)
            {
                invoices = Enumerable.Empty<SalesInvoice>().AsQueryable();
            }

            foreach (var invoice in invoices.OrderByDescending(i => i.InvoiceDate))
            {
                salesLogItems.Add(new SalesLogItem
                {
                    Id = invoice.Id,
                    Type = SalesLogItemType.Invoice,
                    Number = invoice.InvoiceNumber,
                    Date = invoice.InvoiceDate,
                    CustomerName = invoice.Customer?.Name ?? "غير معرف",
                    TotalAmount = invoice.Total,
                    Notes = null
                });
            }

            foreach (var returnItem in returns.OrderByDescending(r => r.ReturnDate))
            {
                string paymentMethodDisplay = returnItem.PaymentMethod switch
                {
                    ReturnPaymentMethod.ToCustomerAccount => "إلى حساب العميل",
                    ReturnPaymentMethod.Cash => "نقدي من الصندوق",
                    ReturnPaymentMethod.Transfer => "تحويل/شبكة",
                    _ => "غير محدد"
                };

                salesLogItems.Add(new SalesLogItem
                {
                    Id = returnItem.Id,
                    Type = SalesLogItemType.Return,
                    Number = returnItem.ReturnNumber,
                    Date = returnItem.ReturnDate,
                    CustomerName = returnItem.Customer?.Name ?? "غير معرف",
                    TotalAmount = returnItem.TotalAmount,
                    Notes = returnItem.Notes,
                    TransferNumber = returnItem.TransferNumber,
                    PaymentMethodDisplayName = paymentMethodDisplay
                });
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                salesLogItems = salesLogItems.Where(i =>
                    i.Number.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    i.CustomerName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            Items.Clear();
            foreach (var item in salesLogItems.OrderByDescending(x => x.Date))
            {
                Items.Add(item);
            }
        }

        private void ExecuteAddInvoice(object? parameter)
        {
            var view = new Views.SalesInvoiceView();
            var window = new Window
            {
                Title = "إضافة فاتورة مبيعات",
                Content = view,
                WindowState = WindowState.Maximized,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            window.ShowDialog();
            LoadItems();
        }

        private void ExecuteAddReturn(object? parameter)
        {
            var view = new Views.SalesReturnView();
            var window = new Window
            {
                Title = "إضافة فاتورة مرتجع",
                Content = view,
                WindowState = WindowState.Maximized,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            window.ShowDialog();
            LoadItems();
        }
    }
}
