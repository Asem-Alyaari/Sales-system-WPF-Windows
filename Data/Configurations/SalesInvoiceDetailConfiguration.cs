using App2.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App2.Data.Configurations
{
    public class SalesInvoiceDetailConfiguration : IEntityTypeConfiguration<SalesInvoiceDetail>
    {
        public void Configure(EntityTypeBuilder<SalesInvoiceDetail> builder)
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
        }
    }
}
