using App2.Models;
using System.Collections.ObjectModel;

namespace App2.ViewModels
{
    public class SalesInvoiceDetailsViewModel : ObservableObject
    {
        private SalesInvoice _invoice;

        public SalesInvoice Invoice
        {
            get => _invoice;
            set => SetProperty(ref _invoice, value);
        }

        public ObservableCollection<SalesInvoiceDetail> Items { get; } = new();

        public SalesInvoiceDetailsViewModel(SalesInvoice invoice)
        {
            _invoice = invoice;
            foreach (var item in invoice.Details)
            {
                Items.Add(item);
            }
        }
    }
}
