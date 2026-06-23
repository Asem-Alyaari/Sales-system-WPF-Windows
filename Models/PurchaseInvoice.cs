namespace App2.Models
{
    public class PurchaseInvoice
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; } = DateTime.Now;
        public string? ContainerNumber { get; set; }
        public string? Category { get; set; }

        public List<PurchaseInvoiceItem> Items { get; set; } = new List<PurchaseInvoiceItem>();

        public PurchaseInvoice()
        {
        }

        public PurchaseInvoice(string invoiceNumber, DateTime invoiceDate, string? containerNumber, string? category)
        {
            InvoiceNumber = invoiceNumber;
            InvoiceDate = invoiceDate;
            ContainerNumber = containerNumber;
            Category = category;
        }
    }
}
