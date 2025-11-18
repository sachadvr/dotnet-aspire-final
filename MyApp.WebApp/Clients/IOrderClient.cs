using MyApp.Persistence;

namespace MyApp.WebApp.Clients;

public interface IOrderClient
{
    Task<List<Order>> GetOrdersAsync();
    Task<Order?> GetOrderAsync(int id);
    Task<Order> CreateOrderAsync(CreateOrderRequest request);
}

public record CreateOrderRequest(List<CreateOrderItemRequest> Items, string Address);
public record CreateOrderItemRequest(int ProductId, int Quantity);
