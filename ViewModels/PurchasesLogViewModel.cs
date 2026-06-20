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

        public ICommand RefreshCommand { get; }
        public ICommand AddInvoiceCommand { get; }
        public ICommand ViewInvoiceCommand { get; }

        public PurchasesLogViewModel()
        {
            var factory = new AppDbContextFactory();
            _dbContext = factory.CreateDbContext(null);

            RefreshCommand = new RelayCommand(_ => LoadInvoices());
            AddInvoiceCommand = new RelayCommand(ExecuteAddInvoice);
            ViewInvoiceCommand = new RelayCommand(ExecuteViewInvoice);

            LoadInvoices();
        }

        private void LoadInvoices()
        {
            Invoices.Clear();
            var invoices = _dbContext.PurchaseInvoices
                .Include(i => i.Items)
                .OrderByDescending(i => i.InvoiceDate)
                .ToList();

            foreach (var invoice in invoices)
            {
                Invoices.Add(invoice);
            }
        }

        private void FilterInvoices()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                LoadInvoices();
                return;
            }

            var query = _dbContext.PurchaseInvoices
                .Include(i => i.Items)
                .Where(i => i.InvoiceNumber.Contains(SearchText) || 
                            i.ContainerNumber.Contains(SearchText) || 
                            (i.Category != null && i.Category.Contains(SearchText)))
                .OrderByDescending(i => i.InvoiceDate)
                .ToList();

            Invoices.Clear();
            foreach (var invoice in query)
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

        private void ExecuteViewInvoice(object? parameter)
        {
            if (parameter is PurchaseInvoice invoice)
            {
                var viewModel = new PurchaseInvoiceDetailsViewModel(invoice);
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
    }
}
