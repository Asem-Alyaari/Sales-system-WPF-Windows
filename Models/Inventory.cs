namespace App2.Models
{
    public class Inventory
    {
        public const int KabbaPerCarton = 12;

        public int Id { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; } // دائماً بوحدة الكبة
        public string Unit { get; set; } = "كبة"; 
        public decimal? TotalWeight { get; set; }
        public DateTime DateAdded { get; set; } = DateTime.Now;
        public string InvoiceNumber { get; set; } = string.Empty;

        public Product? Product { get; set; }

        public Inventory()
        {
        }

        public Inventory(int productId, int quantity, string unit, decimal? totalWeight, string invoiceNumber = "")
        {
            ProductId = productId;
            // التحويل إلى كبة إذا كانت الوحدة كرتون
            if (unit == "كرتون")
            {
                Quantity = quantity * KabbaPerCarton;
                Unit = "كبة";
            }
            else
            {
                Quantity = quantity;
                Unit = "كبة";
            }
            TotalWeight = totalWeight;
            InvoiceNumber = invoiceNumber;
            DateAdded = DateTime.Now;
        }

        // إجمالي الكراتين (للعرض فقط)
        public int Cartons => Quantity / KabbaPerCarton;
        
        // الكبة المتبقية بعد حساب الكراتين (للعرض فقط)
        public int RemainingKabba => Quantity % KabbaPerCarton;
        
        // نص العرض المنسق
        public string DisplayQuantity => Cartons > 0 
            ? $"{Cartons} كرتون" + (RemainingKabba > 0 ? $" و {RemainingKabba} كبة" : "")
            : $"{Quantity} كبة";
    }
}
