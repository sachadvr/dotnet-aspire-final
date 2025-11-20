using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MyApp.Persistence;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("T_Orders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.UserId).IsRequired().HasMaxLength(200);
        builder.Property(o => o.UserName).IsRequired().HasMaxLength(200);
        builder.Property(o => o.OrderDate).IsRequired();
        builder.Property(o => o.Status).IsRequired().HasConversion<string>();
        builder.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(o => o.Address).IsRequired().HasMaxLength(500);
        builder.Property(o => o.PaymentLink).HasMaxLength(1000);
        
        builder.HasMany(o => o.OrderItems)
            .WithOne(oi => oi.Order)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
