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

        public ObservableCollection<SalesInvoice> Invoices { get; set; } = new();

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
        public ICommand ViewDetailsCommand { get; }

        public SalesLogViewModel()
        {
            var factory = new AppDbContextFactory();
            _dbContext = factory.CreateDbContext(null);

            var today = DateTime.Today;
            _startDate = new DateTime(today.Year, today.Month, 1);
            _endDate = today;

            RefreshCommand = new RelayCommand(_ => LoadInvoices());
            AddInvoiceCommand = new RelayCommand(ExecuteAddInvoice);
            ViewDetailsCommand = new RelayCommand(ExecuteViewDetails);

            LoadInvoices();
        }

        private void ExecuteViewDetails(object? parameter)
        {
            if (parameter is SalesInvoice invoice)
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

        private void LoadInvoices()
        {
            Invoices.Clear();
            var startDate = StartDate.Date;
            var endDate = EndDate.Date.AddDays(1).AddTicks(-1);

            var invoices = _dbContext.SalesInvoices
                .Include(i => i.Customer)
                .Include(i => i.Details)
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

            var query = _dbContext.SalesInvoices
                .Include(i => i.Customer)
                .Include(i => i.Details)
                .Where(i => i.InvoiceDate >= startDate && i.InvoiceDate <= endDate);

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(i => i.InvoiceNumber.Contains(SearchText) ||
                            (i.Customer != null && i.Customer.Name.Contains(SearchText)));
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
            // For now, redirecting to the main sales invoice view might be done via the main window,
            // but we can also open a new window
            var view = new Views.SalesInvoiceView();
            var window = new Window
            {
                Title = "إضافة فاتورة مبيعات",
                Content = view,
                WindowState = WindowState.Maximized,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            window.ShowDialog();
            // Refresh list after new invoice is added
            LoadInvoices();
        }
    }
}
