namespace MyApp.Persistence;

public class Order
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? PaymentLink { get; set; }
    
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

public enum OrderStatus
{
    Pending,
    Confirmed,
    Shipped,
    Delivered,
    Cancelled
}
