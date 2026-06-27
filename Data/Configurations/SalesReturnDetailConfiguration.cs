using App2.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App2.Data.Configurations
{
    public class SalesReturnDetailConfiguration : IEntityTypeConfiguration<SalesReturnDetail>
    {
        public void Configure(EntityTypeBuilder<SalesReturnDetail> builder)
        {
            builder.HasKey(d => d.Id);

            builder.Property(d => d.ThreadNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(d => d.ItemName)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(d => d.Quantity)
                .HasColumnType("decimal(18,2)");

            builder.Property(d => d.Price)
                .HasColumnType("decimal(18,2)");

            builder.Property(d => d.TotalPrice)
                .HasColumnType("decimal(18,2)");

            builder.Property(d => d.Unit)
                .HasConversion<string>(); // Save enum as string representation
            
            builder.Property(d => d.OriginalUnit)
                .HasConversion<string>(); // Save enum as string representation
            
            builder.Property(d => d.MaxReturnQuantityKabba)
                .HasColumnType("decimal(18,2)");

            // Relationships
            builder.HasOne(d => d.SalesReturn)
                .WithMany(r => r.Details)
                .HasForeignKey(d => d.SalesReturnId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasOne(d => d.SalesInvoiceDetail)
                .WithMany()
                .HasForeignKey(d => d.SalesInvoiceDetailId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasOne(d => d.Product)
                .WithMany()
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
