using System;
using System.Collections.Generic;

namespace App2.Models
{
    public class Account
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty; // رقم الحساب
        public AccountType AccountType { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        public ICollection<FinancialTransactionLine> TransactionLines { get; set; } = new List<FinancialTransactionLine>();
    }
}
