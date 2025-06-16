using Core.Entities;
using Core.Interfaces;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;

namespace Logic.Services;

public class CategoryService(ICategoryRepository categoryRepository, ITopicCategoryRepository topicCategoryRepository) : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly ITopicCategoryRepository _topicCategoryRepository = topicCategoryRepository;

    public async Task<string> AddTopicToCategoryAsync(string categoryName, string topicName)
    {
        try
        {
            await _topicCategoryRepository.AddAsync(topicName, categoryName);
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
        return "Success";
    }

    public async Task<string> CreateCategoryAsync(string name)
    {

        try
        {
            await _categoryRepository.AddAsync(new Category { Name = name });
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
        return "Success";
    }

    public async Task<string> DeleteCategoryAsync(string name)
    {
        try
        {
            var category = await _categoryRepository.GetByNameAsync(name);
            await _categoryRepository.DeleteAsync(category.Id);
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
        return "Success";
    }

    public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
    {
        return await _categoryRepository.GetAllAsync();
    }

    public async Task<Category> GetCategoryByNameAsync(string name)
    {
        return await _categoryRepository.GetByNameAsync(name);
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task<IEnumerable<Topic>> GetTopicsByCategoryAsync(string name)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        throw new NotImplementedException();
    }

    public async Task<bool> IsCategoryExistsAsync(string name)
    {
        return await _categoryRepository.GetByNameAsync(name) != null;
    }

    public async Task<bool> IsTopicInCategoryAsync(string categoryName, string topicName)
    {
        return await _topicCategoryRepository.IsTopicInCategoryAsync(topicName, categoryName);
    }

    public async Task<string> RemoveTopicFromCategoryAsync(string categoryName, string topicName)
    {
        try
        {
            await _topicCategoryRepository.DeleteAsync(topicName, categoryName);
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
        return "Success";
    }

    public async Task<IEnumerable<Category>> GetCategories(int start, int count, string searchQuery)
    {
        return await _categoryRepository.GetCategories(start, count, searchQuery);
    }

    public async Task<int> GetCategoriesCountAsync(string searchQuery)
    {
        return await _categoryRepository.GetCategoriesCountAsync(searchQuery);
    }
}