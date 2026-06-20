using App2.Models;
using System;
using System.Collections.Generic;

namespace App2.ViewModels
{
    public class InventorySummaryItem
    {
        public int ProductId { get; set; }
        public string ColorNumber { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public int TotalQuantity { get; set; } // إجمالي العدد بوحدة الكبة
        public decimal TotalWeight { get; set; } // إجمالي الوزن
        public List<InventoryBatchItem> Batches { get; set; } = new();

        // إجمالي الكراتين للعرض
        public int TotalCartons => TotalQuantity / Inventory.KabbaPerCarton;
        
        // الكبة المتبقية للعرض
        public int RemainingKabba => TotalQuantity % Inventory.KabbaPerCarton;

        // نص العرض المنسق للإجمالي
        public string DisplayTotalQuantity => TotalCartons > 0 
            ? $"{TotalCartons} كرتون" + (RemainingKabba > 0 ? $" و {RemainingKabba} كبة" : "")
            : $"{TotalQuantity} كبة";
    }

    public class InventoryBatchItem
    {
        public int Id { get; set; }
        public int Quantity { get; set; } // الكمية بوحدة الكبة
        public string Unit { get; set; } = "كبة";
        public decimal Weight { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime DateAdded { get; set; }
        public string BatchName { get; set; } = string.Empty; // مثل: دفعة 1، دفعة 2

        // إجمالي الكراتين لهذه الدفعة
        public int Cartons => Quantity / Inventory.KabbaPerCarton;
        
        // الكبة المتبقية لهذه الدفعة
        public int RemainingKabba => Quantity % Inventory.KabbaPerCarton;

        // نص العرض المنسق للدفعة
        public string DisplayQuantity => Cartons > 0 
            ? $"{Cartons} كرتون" + (RemainingKabba > 0 ? $" و {RemainingKabba} كبة" : "")
            : $"{Quantity} كبة";
    }
}
