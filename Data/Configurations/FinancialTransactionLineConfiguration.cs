using App2.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App2.Data.Configurations
{
    public class FinancialTransactionLineConfiguration : IEntityTypeConfiguration<FinancialTransactionLine>
    {
        public void Configure(EntityTypeBuilder<FinancialTransactionLine> builder)
        {
            builder.HasKey(l => l.Id);
            builder.Property(l => l.Debit).HasColumnType("decimal(18,4)");
            builder.Property(l => l.Credit).HasColumnType("decimal(18,4)");
            builder.Property(l => l.Notes).HasMaxLength(500);

            builder.HasOne(l => l.FinancialTransaction)
                .WithMany(t => t.Lines)
                .HasForeignKey(l => l.FinancialTransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(l => l.Account)
                .WithMany(a => a.TransactionLines)
                .HasForeignKey(l => l.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
