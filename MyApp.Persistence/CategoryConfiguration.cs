using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MyApp.Persistence;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("T_Categories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Description).HasMaxLength(500);
        builder.Property(c => c.CreatedAt).IsRequired();
        
        builder.HasMany(c => c.Products)
            .WithOne(p => p.Category)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
