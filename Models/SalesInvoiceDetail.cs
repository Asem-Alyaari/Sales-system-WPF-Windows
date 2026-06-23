using System.ComponentModel.DataAnnotations.Schema;
using App2.ViewModels;

namespace App2.Models
{
    public class SalesInvoiceDetail : ObservableObject
    {
        private decimal _quantity;
        private UnitType _unit;
        private decimal _price;
        private decimal _totalPrice;

        public int Id { get; set; }
        
        public int SalesInvoiceId { get; set; } 
        public SalesInvoice SalesInvoice { get; set; } = null!;
        
        public string ThreadNumber { get; set; } = string.Empty; 
        
        public decimal Quantity 
        { 
            get => _quantity;
            set
            {
                if (SetProperty(ref _quantity, value))
                {
                    UpdateCalculations();
                }
            }
        } 
        
        public UnitType Unit 
        { 
            get => _unit;
            set
            {
                if (SetProperty(ref _unit, value))
                {
                    OnPropertyChanged(nameof(UnitName));
                    UpdateCalculations();
                }
            }
        } 

        [NotMapped]
        public string UnitName
        {
            get => Unit == UnitType.Carton ? "كرتون" : "كبة";
            set
            {
                Unit = value == "كرتون" ? UnitType.Carton : UnitType.Skein;
            }
        }
        
        public decimal Price 
        { 
            get => _price;
            set
            {
                if (SetProperty(ref _price, value))
                {
                    UpdateCalculations();
                }
            }
        } 
        
        public decimal TotalPrice 
        { 
            get => _totalPrice;
            set => SetProperty(ref _totalPrice, value);
        } 

        public string ItemName { get; set; } = string.Empty; 

        private void UpdateCalculations()
        {
            TotalPrice = Quantity * Price;
        }
    }
}
