using App2.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App2.Data.Configurations
{
    public class PurchaseInvoiceItemConfiguration : IEntityTypeConfiguration<PurchaseInvoiceItem>
    {
        public void Configure(EntityTypeBuilder<PurchaseInvoiceItem> builder)
        {
            builder.HasKey(i => i.Id);

            builder.Property(i => i.BoxNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(i => i.Color)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(i => i.Quantity)
                .IsRequired();

            builder.Property(i => i.Unit)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(i => i.Weight)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.HasOne(i => i.PurchaseInvoice)
                .WithMany(p => p.Items)
                .HasForeignKey(i => i.PurchaseInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
