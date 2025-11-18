using MyApp.Persistence;

namespace MyApp.WebApp.Clients;

public class ProductClient(HttpClient httpClient) : IProductClient
{
    public async Task<List<Product>> GetProductsAsync()
    {
        try
        {
            var result = await httpClient.GetFromJsonAsync<List<Product>>("/api/products") ?? new List<Product>();
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProductClient] GET /api/products - Error: {ex.GetType().Name} - {ex.Message}");
            throw;
        }
    }

    public async Task<Product?> GetProductAsync(int id)
    {
        try
        {
            var result = await httpClient.GetFromJsonAsync<Product>($"/api/products/{id}");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProductClient] GET /api/products/{id} - Error: {ex.GetType().Name} - {ex.Message}");
            throw;
        }
    }
}
