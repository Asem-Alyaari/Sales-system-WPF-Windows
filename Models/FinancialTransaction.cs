using System;
using System.Collections.Generic;

namespace App2.Models
{
    public class FinancialTransaction
    {
        public int Id { get; set; }
        public DateTime TransactionDate { get; set; } = DateTime.Now;
        public string Description { get; set; } = string.Empty;
        
        // مثلاً: "SalesInvoice", "Receipt", "Payment"
        public string ReferenceType { get; set; } = string.Empty;
        public int? ReferenceId { get; set; }

        public ICollection<FinancialTransactionLine> Lines { get; set; } = new List<FinancialTransactionLine>();
    }
}
