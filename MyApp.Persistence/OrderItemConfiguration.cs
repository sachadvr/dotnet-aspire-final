using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MyApp.Persistence;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("T_OrderItems");
        builder.HasKey(oi => oi.Id);
        builder.Property(oi => oi.Quantity).IsRequired();
        builder.Property(oi => oi.UnitPrice).HasColumnType("decimal(18,2)").IsRequired();
    }
}
