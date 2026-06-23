using App2.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App2.Data.Configurations
{
    public class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
    {
        public void Configure(EntityTypeBuilder<Inventory> builder)
        {
            builder.HasKey(i => i.Id);

            builder.Property(i => i.Quantity)
                .IsRequired();

            builder.Property(i => i.Unit)
                .IsRequired()
                .HasMaxLength(20);

           

            builder.Property(i => i.DateAdded)
                .IsRequired();

            builder.Property(i => i.InvoiceNumber)
                .IsRequired(false)
                .HasMaxLength(50);

            builder.HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
