using App2.ViewModels;

namespace App2.Models
{
    public class PurchaseInvoiceItem : ObservableObject
    {
        private int _id;
        private string _boxNumber = string.Empty;
        private string _color = string.Empty;
        private int _quantity;
        private string _unit = "كرتون";
        private int _purchaseInvoiceId;
        private PurchaseInvoice? _purchaseInvoice;

        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string BoxNumber
        {
            get => _boxNumber;
            set => SetProperty(ref _boxNumber, value);
        }

        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        public int Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value);
        }



        public int PurchaseInvoiceId
        {
            get => _purchaseInvoiceId;
            set => SetProperty(ref _purchaseInvoiceId, value);
        }

        public PurchaseInvoice? PurchaseInvoice
        {
            get => _purchaseInvoice;
            set => SetProperty(ref _purchaseInvoice, value);
        }

        public PurchaseInvoiceItem()
        {
        }

        public PurchaseInvoiceItem(string boxNumber, string color, int quantity, string unit, int purchaseInvoiceId)
        {
            BoxNumber = boxNumber;
            Color = color;
            Quantity = quantity;
            Unit = unit;
            PurchaseInvoiceId = purchaseInvoiceId;
        }
    }
}
