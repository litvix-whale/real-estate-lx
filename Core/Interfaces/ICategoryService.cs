using Core.Entities;

namespace Core.Interfaces;

public interface ICategoryService
{
    Task<string> CreateCategoryAsync(string name);
    Task<string> DeleteCategoryAsync(string name);
    Task<IEnumerable<Category>> GetAllCategoriesAsync();
    Task<Category> GetCategoryByNameAsync(string name);
    Task<IEnumerable<Topic>> GetTopicsByCategoryAsync(string name);
    Task<string> AddTopicToCategoryAsync(string categoryName, string topicName);
    Task<string> RemoveTopicFromCategoryAsync(string categoryName, string topicName);
    Task<bool> IsTopicInCategoryAsync(string categoryName, string topicName);
    Task<bool> IsCategoryExistsAsync(string name);
    Task<IEnumerable<Category>> GetCategories(int start, int count, string searchQuery);
    Task<int> GetCategoriesCountAsync(string searchQuery);
}