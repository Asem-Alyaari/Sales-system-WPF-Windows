using App2.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App2.Data.Configurations
{
    public class AccountConfiguration : IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> builder)
        {
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Name).IsRequired().HasMaxLength(150);
            builder.Property(a => a.Code).HasMaxLength(50);
            
            // To ensure Code is unique if provided
            builder.HasIndex(a => a.Code).IsUnique().HasFilter("[Code] IS NOT NULL AND [Code] != ''");
        }
    }
}
