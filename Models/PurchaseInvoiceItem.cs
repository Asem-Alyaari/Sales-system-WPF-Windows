using App2.Data;
using App2.ViewModels;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

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
        private bool _isNew;

        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string BoxNumber
        {
            get => _boxNumber;
            set
            {
                if (SetProperty(ref _boxNumber, value))
                {
                    CheckIfProductIsNew();
                }
            }
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

        [NotMapped]
        public bool IsNew
        {
            get => _isNew;
            set => SetProperty(ref _isNew, value);
        }

        private void CheckIfProductIsNew()
        {
            if (string.IsNullOrWhiteSpace(_boxNumber))
            {
                IsNew = false;
                return;
            }

            var factory = new AppDbContextFactory();
            using var db = factory.CreateDbContext(null);
            IsNew = !db.Products.Any(p => p.ColorNumber == _boxNumber);
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
