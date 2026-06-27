using System;
using System.Collections.Generic;

namespace App2.Models
{
    public class SalesInvoice
    {
        public int Id { get; set; }
        
        // رقم الفاتورة
        public string InvoiceNumber { get; set; } = string.Empty; 
        
        // تاريخ الفاتورة
        public DateTime InvoiceDate { get; set; } = DateTime.Now; 
        
        // ID العميل
        public int CustomerId { get; set; } 
        public Customer Customer { get; set; } = null!;
        
        // الاجمالي
        public decimal Total { get; set; } 
        
        // الخصم
        public decimal Discount { get; set; } 
        
        // المدفوع نقدا
        public decimal PaidInCash { get; set; } 
        
        // الاجل
        public decimal Deferred { get; set; } 
        
        // التحويل
        public decimal Transfer { get; set; }
        
        // رقم الحوالة
        public string? TransferNumber { get; set; }

        // تفاصيل الفاتورة
        public List<SalesInvoiceDetail> Details { get; set; } = new List<SalesInvoiceDetail>(); 
    }
}
