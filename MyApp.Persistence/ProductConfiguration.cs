using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MyApp.Persistence;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("T_Products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.Property(p => p.Price).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(p => p.Stock).IsRequired();
        builder.Property(p => p.ImageUrl).HasMaxLength(500);
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.CategoryId).IsRequired(false);
        
        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
        
        builder.HasMany(p => p.OrderItems)
            .WithOne(oi => oi.Product)
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
