using App2.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App2.Data.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Color)
                .IsRequired(false)
                .HasMaxLength(50);

            builder.Property(p => p.ColorNumber)
                .IsRequired()
                .HasMaxLength(20);
        }
    }
}
