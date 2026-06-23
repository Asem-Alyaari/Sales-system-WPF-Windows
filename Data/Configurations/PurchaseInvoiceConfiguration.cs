using App2.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App2.Data.Configurations
{
    public class PurchaseInvoiceConfiguration : IEntityTypeConfiguration<PurchaseInvoice>
    {
        public void Configure(EntityTypeBuilder<PurchaseInvoice> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.InvoiceNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(p => p.InvoiceDate)
                .IsRequired();

            builder.Property(p => p.ContainerNumber)
                .IsRequired(false)
                .HasMaxLength(50);

            builder.Property(p => p.Category)
                .IsRequired(false)
                .HasMaxLength(100);

            builder.HasMany(p => p.Items)
                .WithOne(i => i.PurchaseInvoice)
                .HasForeignKey(i => i.PurchaseInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
