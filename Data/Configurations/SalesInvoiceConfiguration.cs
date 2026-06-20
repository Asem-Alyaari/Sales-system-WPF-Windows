using App2.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App2.Data.Configurations
{
    public class SalesInvoiceConfiguration : IEntityTypeConfiguration<SalesInvoice>
    {
        public void Configure(EntityTypeBuilder<SalesInvoice> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.InvoiceNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(s => s.InvoiceDate)
                .IsRequired();

            builder.Property(s => s.Total)
                .HasColumnType("decimal(18,2)");

            builder.Property(s => s.Discount)
                .HasColumnType("decimal(18,2)");

            builder.Property(s => s.PaidInCash)
                .HasColumnType("decimal(18,2)");

            builder.Property(s => s.Deferred)
                .HasColumnType("decimal(18,2)");

            builder.Property(s => s.Transfer)
                .HasColumnType("decimal(18,2)");

            builder.HasOne(s => s.Customer)
                .WithMany()
                .HasForeignKey(s => s.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(s => s.Details)
                .WithOne(d => d.SalesInvoice)
                .HasForeignKey(d => d.SalesInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
