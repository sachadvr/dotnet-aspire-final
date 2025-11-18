using MyApp.Persistence;

namespace MyApp.WebApp.Clients;

public interface IProductClient
{
    Task<List<Product>> GetProductsAsync();
    Task<Product?> GetProductAsync(int id);
}
