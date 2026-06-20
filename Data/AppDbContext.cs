using App2.Data.Configurations;
using App2.Models;
using Microsoft.EntityFrameworkCore;

namespace App2.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<PurchaseInvoice> PurchaseInvoices { get; set; }
        public DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems { get; set; }
        
        public DbSet<SalesInvoice> SalesInvoices { get; set; }
        public DbSet<SalesInvoiceDetail> SalesInvoiceDetails { get; set; }
        
        public DbSet<Account> Accounts { get; set; }
        public DbSet<FinancialTransaction> FinancialTransactions { get; set; }
        public DbSet<FinancialTransactionLine> FinancialTransactionLines { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=.;Database=App2Db;Trusted_Connection=True;TrustServerCertificate=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new ProductConfiguration());
            modelBuilder.ApplyConfiguration(new InventoryConfiguration());
            modelBuilder.ApplyConfiguration(new PurchaseInvoiceConfiguration());
            modelBuilder.ApplyConfiguration(new PurchaseInvoiceItemConfiguration());
            
            modelBuilder.ApplyConfiguration(new SalesInvoiceConfiguration());
            modelBuilder.ApplyConfiguration(new SalesInvoiceDetailConfiguration());
            
            modelBuilder.ApplyConfiguration(new AccountConfiguration());
            modelBuilder.ApplyConfiguration(new FinancialTransactionConfiguration());
            modelBuilder.ApplyConfiguration(new FinancialTransactionLineConfiguration());
        }
    }
}
