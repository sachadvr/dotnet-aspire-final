using MyApp.Persistence;

namespace MyApp.WebApp.Clients;

public interface ICategoryClient
{
    Task<List<Category>> GetCategoriesAsync();
    Task<Category?> GetCategoryAsync(int id);
}
