using Core.Entities;

namespace Core.Interfaces;

public interface ICategoryRepository : IRepository<Category>
{
    Task<Category> GetByNameAsync(string name);
    Task<IEnumerable<Category>> GetCategoiresByNameAsync(string name);
    Task<IEnumerable<Category>> GetAllCategoriesAsync();
    Task<IEnumerable<Category>> GetCategories(int start, int count, string searchQuery);
    Task<int> GetCategoriesCountAsync(string searchQuery);
}