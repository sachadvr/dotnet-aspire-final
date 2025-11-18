using MyApp.Persistence;

namespace MyApp.WebApp.Clients;

public interface IAdminClient
{
    Task<List<Product>> GetProductsAsync();
    Task<Product> CreateProductAsync(Product product);
    Task<Product> UpdateProductAsync(int id, Product product);
    Task<bool> DeleteProductAsync(int id);
    
    Task<List<Order>> GetOrdersAsync();
    Task<Order> UpdateOrderStatusAsync(int id, string status);
    Task<Order> GeneratePaymentLinkAsync(int id, string paymentLink);
}
