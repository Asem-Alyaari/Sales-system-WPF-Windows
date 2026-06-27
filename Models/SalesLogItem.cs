using System;

namespace App2.Models
{
    public enum SalesLogItemType
    {
        Invoice,
        Return
    }

    public class SalesLogItem
    {
        public int Id { get; set; }
        public SalesLogItemType Type { get; set; }
        public string Number { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string? Notes { get; set; }
        public string? TransferNumber { get; set; }
        public string? PaymentMethodDisplayName { get; set; }
    }
}
