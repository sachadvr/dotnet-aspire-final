using System.Text.Json.Serialization;

namespace MyApp.Persistence;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    
    // Navigation properties
    [JsonIgnore] // Éviter les cycles de référence lors de la sérialisation JSON
    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
