namespace App2.Models
{
    public class FinancialTransactionLine
    {
        public int Id { get; set; }
        
        public int FinancialTransactionId { get; set; }
        public FinancialTransaction FinancialTransaction { get; set; } = null!;
        
        public int AccountId { get; set; }
        public Account Account { get; set; } = null!;

        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}
