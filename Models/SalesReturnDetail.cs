using System.ComponentModel.DataAnnotations.Schema;
using App2.ViewModels;

namespace App2.Models
{
    public class SalesReturnDetail : ObservableObject
    {
        private decimal _quantity;
        private UnitType _unit;
        private decimal _price;
        private decimal _totalPrice;
        private decimal _maxReturnQuantityKabba;

        public int Id { get; set; }
        
        public int SalesReturnId { get; set; }
        public SalesReturn SalesReturn { get; set; } = null!;
        
        public int? SalesInvoiceDetailId { get; set; }
        public SalesInvoiceDetail? SalesInvoiceDetail { get; set; }
        
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        
        public string ThreadNumber { get; set; } = string.Empty;
        
        // الحد الأقصى المسموح بإرجاعه بالكبة دائماً
        public decimal MaxReturnQuantityKabba
        {
            get => _maxReturnQuantityKabba;
            set => SetProperty(ref _maxReturnQuantityKabba, value);
        }
        
        // وحدة البيع الأصلية من الفاتورة (لا يمكن تغييرها)
        public UnitType OriginalUnit { get; set; }
        
        // السعر الأصلية من الفاتورة (يمكن تغييره للأصناف الجديدة في الاستبدال)
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
        
        // الحد الأقصى المسموح بإرجاعه بالوحدة الأصلية
        [NotMapped]
        public decimal MaxReturnQuantityOriginalUnit
        {
            get
            {
                if (OriginalUnit == UnitType.Carton)
                {
                    return MaxReturnQuantityKabba / Inventory.KabbaPerCarton;
                }
                return MaxReturnQuantityKabba;
            }
        }
        
        // هل يمكن تغيير الوحدة (أي أن الوحدة الأصلية كانت كرتون)
        [NotMapped]
        public bool CanChangeUnit => OriginalUnit == UnitType.Carton;
        
        [NotMapped]
        public string MaxReturnQuantityDisplay
        {
            get
            {
                // For manual items (no SalesInvoiceDetailId), show current quantity
                if (!SalesInvoiceDetailId.HasValue)
                {
                    // عرض الكمية الحالية بالكرتون والكبة
                    decimal quantityKabba = GetQuantityInKabba();
                    int cartons = (int)(quantityKabba / Inventory.KabbaPerCarton);
                    int remainingKabba = (int)(quantityKabba % Inventory.KabbaPerCarton);

                    if (cartons > 0 && remainingKabba > 0)
                    {
                        return $"{cartons} كرتون و {remainingKabba} كبة";
                    }
                    else if (cartons > 0)
                    {
                        return $"{cartons} كرتون";
                    }
                    else
                    {
                        return $"{remainingKabba} كبة";
                    }
                }
                
                // For original invoice items, show max return quantity
                decimal maxKabba = MaxReturnQuantityKabba;
                int maxCartons = (int)(maxKabba / Inventory.KabbaPerCarton);
                int maxRemainingKabba = (int)(maxKabba % Inventory.KabbaPerCarton);

                if (maxCartons > 0 && maxRemainingKabba > 0)
                {
                    return $"{maxCartons} كرتون و {maxRemainingKabba} كبة";
                }
                else if (maxCartons > 0)
                {
                    return $"{maxCartons} كرتون";
                }
                else
                {
                    return $"{maxRemainingKabba} كبة";
                }
            }
        }
        
        public decimal Quantity 
        { 
            get => _quantity;
            set
            {
                if (SetProperty(ref _quantity, value))
                {
                    OnPropertyChanged(nameof(MaxReturnQuantityDisplay));
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
                    OnPropertyChanged(nameof(MaxReturnQuantityDisplay));
                    UpdateCalculations();
                }
            }
        } 

        [NotMapped]
        public string UnitName
        {
            get => Unit == UnitType.Carton ? "كرتون" : "كبة";
            set => Unit = value == "كرتون" ? UnitType.Carton : UnitType.Skein;
        }
        
        
        public decimal TotalPrice 
        { 
            get => _totalPrice;
            set => SetProperty(ref _totalPrice, value);
        }

        public string ItemName { get; set; } = string.Empty;

        private void UpdateCalculations()
        {
            // لأصناف جديدة في الاستبدال (بدون SalesInvoiceDetailId): الحساب بسيط: الكمية × السعر
            if (!SalesInvoiceDetailId.HasValue)
            {
                TotalPrice = Quantity * Price;
                return;
            }
            
            // لأصناف الفاتورة الأصلية
            if (OriginalUnit == UnitType.Carton)
            {
                if (Unit == UnitType.Carton)
                {
                    // السعر للكرتون × الكمية بالكرتون
                    TotalPrice = Quantity * Price;
                }
                else
                {
                    // السعر للكرتون ثابت، نحسب سعر الكبة الواحدة
                    // الإجمالي = الكمية بالكبة × (سعر الكرتون / عدد الكبات في الكرتون)
                    decimal pricePerKabba = Price / Inventory.KabbaPerCarton;
                    TotalPrice = Quantity * pricePerKabba;
                }
            }
            else
            {
                // Original unit is skein, so unit can't change (CanChangeUnit is false)
                TotalPrice = Quantity * Price;
            }
        }
        
        // تحويل الكمية المرجعة إلى الكبة
        public decimal GetQuantityInKabba()
        {
            return Unit == UnitType.Carton 
                ? Quantity * Inventory.KabbaPerCarton 
                : Quantity;
        }
    }
}
