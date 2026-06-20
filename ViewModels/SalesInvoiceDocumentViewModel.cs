using App2.Data;
using App2.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;

namespace App2.ViewModels
{
    public class BatchItemViewModel
    {
        public Inventory InventoryBatch { get; set; } = null!;
        public string DisplayText => $"دفعة: {InventoryBatch.InvoiceNumber} | الكمية: {InventoryBatch.DisplayQuantity} | الوزن: {InventoryBatch.TotalWeight} كجم";
    }

    public class SalesInvoiceDocumentViewModel : ObservableObject
    {
        private readonly AppDbContext _dbContext;
        private string _invoiceNumber = string.Empty;
        private Customer? _selectedCustomer;
        private string _searchItemNumber = string.Empty;
        private BatchItemViewModel? _selectedBatch;
        private bool _isDropdownOpen;

        public SalesInvoiceDocumentViewModel(AppDbContext dbContext)
        {
            _dbContext = dbContext;
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

        public Customer? SelectedCustomer
        {
            get => _selectedCustomer;
            set => SetProperty(ref _selectedCustomer, value);
        }

        public ObservableCollection<SalesInvoiceDetail> Items { get; } = new ObservableCollection<SalesInvoiceDetail>();

        public ObservableCollection<BatchItemViewModel> AvailableBatches { get; } = new ObservableCollection<BatchItemViewModel>();

        public string SearchItemNumber
        {
            get => _searchItemNumber;
            set
            {
                if (SetProperty(ref _searchItemNumber, value))
                {
                    SearchBatches();
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
            set
            {
                if (SetProperty(ref _selectedBatch, value))
                {
                    if (value != null)
                    {
                        AddBatchToInvoice(value.InventoryBatch);
                        
                        System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            SelectedBatch = null;
                            SearchItemNumber = string.Empty;
                            AvailableBatches.Clear();
                            IsDropdownOpen = false;
                        }, DispatcherPriority.ContextIdle);
                    }
                }
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

            var batches = _dbContext.Inventories
                .Include(i => i.Product)
                .Where(i => i.Product != null && i.Product.ColorNumber.Contains(SearchItemNumber) && i.Quantity > 0)
                .ToList();

            foreach (var batch in batches)
            {
                AvailableBatches.Add(new BatchItemViewModel { InventoryBatch = batch });
            }
            IsDropdownOpen = AvailableBatches.Count > 0;
        }

        private void AddBatchToInvoice(Inventory batch)
        {
            decimal weightPerKabba = batch.Quantity > 0 && batch.TotalWeight.HasValue ? (batch.TotalWeight.Value / batch.Quantity) : 0;

            var detail = new SalesInvoiceDetail
            {
                ThreadNumber = batch.Product?.ColorNumber ?? "",
                ItemName = batch.Product?.Color ?? "",
                Quantity = 1,
                Unit = UnitType.Skein, // افتراضياً كبة
                Price = 0,
                TotalPrice = 0,
                BatchWeightPerKabba = weightPerKabba
            };
            // Force weight calculation after initialization
            detail.Weight = 1 * (detail.Unit == UnitType.Carton ? Inventory.KabbaPerCarton : 1) * weightPerKabba;
            
            Items.Add(detail);
        }
    }
}
