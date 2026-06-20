using App2.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App2.Data.Configurations
{
    public class FinancialTransactionConfiguration : IEntityTypeConfiguration<FinancialTransaction>
    {
        public void Configure(EntityTypeBuilder<FinancialTransaction> builder)
        {
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Description).HasMaxLength(500);
            builder.Property(t => t.ReferenceType).HasMaxLength(100);
        }
    }
}
