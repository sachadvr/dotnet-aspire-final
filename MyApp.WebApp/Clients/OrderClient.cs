using MyApp.Persistence;

namespace MyApp.WebApp.Clients;

public class OrderClient(HttpClient httpClient) : IOrderClient
{
    public async Task<List<Order>> GetOrdersAsync()
    {
        try
        {
            var result = await httpClient.GetFromJsonAsync<List<Order>>("/api/orders") ?? new List<Order>();
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OrderClient] GET /api/orders - Error: {ex.GetType().Name} - {ex.Message}");
            throw;
        }
    }

    public async Task<Order?> GetOrderAsync(int id)
    {
        try
        {
            var result = await httpClient.GetFromJsonAsync<Order>($"/api/orders/{id}");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OrderClient] GET /api/orders/{id} - Error: {ex.GetType().Name} - {ex.Message}");
            throw;
        }
    }

    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("/api/orders", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Order>() 
                ?? throw new InvalidOperationException("Réponse invalide du serveur");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OrderClient] POST /api/orders - Error: {ex.GetType().Name} - {ex.Message}");
            throw;
        }
    }
}
