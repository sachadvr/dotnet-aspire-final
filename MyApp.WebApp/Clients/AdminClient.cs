using MyApp.Persistence;

namespace MyApp.WebApp.Clients;

public class AdminClient(HttpClient httpClient) : IAdminClient
{
    public async Task<List<Product>> GetProductsAsync()
    {
        try
        {
            var result = await httpClient.GetFromJsonAsync<List<Product>>("/api/admin/products") ?? new List<Product>();
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminClient] GET /api/admin/products - Error: {ex.GetType().Name} - {ex.Message}");
            throw;
        }
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("/api/admin/products", product);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Product>() 
                ?? throw new InvalidOperationException("Réponse invalide du serveur");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminClient] POST /api/admin/products - Error: {ex.GetType().Name} - {ex.Message}");
            throw;
        }
    }

    public async Task<Product> UpdateProductAsync(int id, Product product)
    {
        try
        {
            var response = await httpClient.PutAsJsonAsync($"/api/admin/products/{id}", product);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Product>() 
                ?? throw new InvalidOperationException("Réponse invalide du serveur");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminClient] PUT /api/admin/products/{id} - Error: {ex.GetType().Name} - {ex.Message}");
            throw;
        }
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        try
        {
            var response = await httpClient.DeleteAsync($"/api/admin/products/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminClient] DELETE /api/admin/products/{id} - Error: {ex.GetType().Name} - {ex.Message}");
            throw;
        }
    }

    // Commandes
    public async Task<List<Order>> GetOrdersAsync()
    {
        try
        {
            var result = await httpClient.GetFromJsonAsync<List<Order>>("/api/admin/orders") ?? new List<Order>();
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminClient] GET /api/admin/orders - Error: {ex.GetType().Name} - {ex.Message}");
            throw;
        }
    }

    public async Task<Order> UpdateOrderStatusAsync(int id, string status)
    {
        try
        {
            var request = new { Status = status };
            var response = await httpClient.PutAsJsonAsync($"/api/admin/orders/{id}/status", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Order>() 
                ?? throw new InvalidOperationException("Réponse invalide du serveur");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminClient] PUT /api/admin/orders/{id}/status - Error: {ex.GetType().Name} - {ex.Message}");
            throw;
        }
    }

    public async Task<Order> GeneratePaymentLinkAsync(int id, string paymentLink)
    {
        try
        {
            var request = new { PaymentLink = paymentLink };
            var response = await httpClient.PostAsJsonAsync($"/api/admin/orders/{id}/payment-link", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Order>() 
                ?? throw new InvalidOperationException("Réponse invalide du serveur");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminClient] POST /api/admin/orders/{id}/payment-link - Error: {ex.GetType().Name} - {ex.Message}");
            throw;
        }
    }

    // Catégories
    public async Task<List<Category>> GetCategoriesAsync()
    {
        try
        {
            var result = await httpClient.GetFromJsonAsync<List<Category>>("/api/admin/categories") ?? new List<Category>();
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminClient] GET /api/admin/categories - Error: {ex.GetType().Name} - {ex.Message}");
            throw;
        }
    }

    public async Task<Category> CreateCategoryAsync(Category category)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("/api/admin/categories", category);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Category>() 
                ?? throw new InvalidOperationException("Réponse invalide du serveur");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminClient] POST /api/admin/categories - Error: {ex.GetType().Name} - {ex.Message}");
            throw;
        }
    }

    public async Task<Category> UpdateCategoryAsync(int id, Category category)
    {
        try
        {
            var response = await httpClient.PutAsJsonAsync($"/api/admin/categories/{id}", category);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Category>() 
                ?? throw new InvalidOperationException("Réponse invalide du serveur");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminClient] PUT /api/admin/categories/{id} - Error: {ex.GetType().Name} - {ex.Message}");
            throw;
        }
    }

    public async Task<bool> DeleteCategoryAsync(int id)
    {
        try
        {
            var response = await httpClient.DeleteAsync($"/api/admin/categories/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminClient] DELETE /api/admin/categories/{id} - Error: {ex.GetType().Name} - {ex.Message}");
            throw;
        }
    }
}
