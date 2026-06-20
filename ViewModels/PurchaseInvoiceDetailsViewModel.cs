using App2.Models;
using System.Collections.ObjectModel;

namespace App2.ViewModels
{
    public class PurchaseInvoiceDetailsViewModel : ObservableObject
    {
        private PurchaseInvoice _invoice;

        public PurchaseInvoice Invoice
        {
            get => _invoice;
            set => SetProperty(ref _invoice, value);
        }

        public ObservableCollection<PurchaseInvoiceItem> Items { get; } = new();

        public PurchaseInvoiceDetailsViewModel(PurchaseInvoice invoice)
        {
            _invoice = invoice;
            foreach (var item in invoice.Items)
            {
                Items.Add(item);
            }
        }
    }
}
