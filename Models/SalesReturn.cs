using System;
using System.Collections.Generic;

namespace App2.Models
{
    public enum ReturnType
    {
        Return,
        Exchange
    }

    public enum ReturnPaymentMethod
    {
        ToCustomerAccount,
        Cash,
        Transfer
    }
    
    public class SalesReturn
    {
        public int Id { get; set; }
        
        public string ReturnNumber { get; set; } = string.Empty;
        
        public DateTime ReturnDate { get; set; } = DateTime.Now;
        
        public ReturnType Type { get; set; } = ReturnType.Return;
        
        public int SalesInvoiceId { get; set; }
        public SalesInvoice SalesInvoice { get; set; } = null!;
        
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;
        
        public decimal TotalAmount { get; set; }
        
        public string? Notes { get; set; }
        
        public ReturnPaymentMethod PaymentMethod { get; set; }
        
        public string? TransferNumber { get; set; }
        
        public List<SalesReturnDetail> Details { get; set; } = new List<SalesReturnDetail>();
    }
}
