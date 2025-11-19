using MyApp.Persistence;

namespace MyApp.WebApp.Clients;

public class CategoryClient(HttpClient httpClient) : ICategoryClient
{
    public async Task<List<Category>> GetCategoriesAsync()
    {
        try
        {
            var result = await httpClient.GetFromJsonAsync<List<Category>>("/api/categories") ?? new List<Category>();
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CategoryClient] GET /api/categories - Error: {ex.GetType().Name} - {ex.Message}");
            throw;
        }
    }

    public async Task<Category?> GetCategoryAsync(int id)
    {
        try
        {
            var result = await httpClient.GetFromJsonAsync<Category>($"/api/categories/{id}");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CategoryClient] GET /api/categories/{id} - Error: {ex.GetType().Name} - {ex.Message}");
            throw;
        }
    }
}
