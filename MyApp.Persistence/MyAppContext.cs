using Microsoft.EntityFrameworkCore;

namespace MyApp.Persistence;

public class MyAppContext(DbContextOptions<MyAppContext> options) : DbContext(options)
{
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Category> Categories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        new ProductConfiguration().Configure(modelBuilder.Entity<Product>());
        new OrderConfiguration().Configure(modelBuilder.Entity<Order>());
        new OrderItemConfiguration().Configure(modelBuilder.Entity<OrderItem>());
        new CategoryConfiguration().Configure(modelBuilder.Entity<Category>());
        base.OnModelCreating(modelBuilder);
    }
}

