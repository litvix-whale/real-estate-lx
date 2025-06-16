using Microsoft.EntityFrameworkCore;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class TopicCategoryRepository(AppDbContext context) : RepositoryBase<TopicCategory>(context), ITopicCategoryRepository
{
    public async Task<string> AddAsync(string topicName, string categoryName)
    {
        var topic = await _context.Topics.FirstOrDefaultAsync(t => t.Name == topicName);
        if (topic == null) { return "Topic not found"; }

        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Name == categoryName);
        if (category == null) { return "Category not found"; }

        var topicCategory = new TopicCategory
        {
            TopicId = topic.Id,
            CategoryId = category.Id
        };

        await _context.TopicCategories.AddAsync(topicCategory);
        await _context.SaveChangesAsync();
        return "Success";
    }

    public async Task<string> DeleteAsync(string topicName, string categoryName)
    {
        var topic = await _context.Topics.FirstOrDefaultAsync(t => t.Name == topicName);
        if (topic == null) { return "Topic not found"; }

        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Name == categoryName);
        if (category == null) { return "Category not found"; }

        var topicCategory = await _context.TopicCategories.FirstOrDefaultAsync(tc => tc.TopicId == topic.Id && tc.CategoryId == category.Id);
        if (topicCategory == null) { return "TopicCategory not found"; }

        _context.TopicCategories.Remove(topicCategory);
        await _context.SaveChangesAsync();
        return "Success";
    }

    public async Task<bool> IsTopicInCategoryAsync(string topicName, string categoryName)
    {
        var topic = await _context.Topics.FirstOrDefaultAsync(t => t.Name == topicName);
        if (topic == null) { return false; }

        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Name == categoryName);
        if (category == null) { return false; }

        return await _context.TopicCategories.AnyAsync(tc => tc.TopicId == topic.Id && tc.CategoryId == category.Id);
    }

    public async Task<Category> GetCategoryByTopicNameAsync(string topicName)
    {
        var topic = await _context.Topics.FirstOrDefaultAsync(t => t.Name == topicName);
        if (topic == null) { return null!; }

        var topicCategory = await _context.TopicCategories.FirstOrDefaultAsync(tc => tc.TopicId == topic.Id);
        if (topicCategory == null) { return null!; }

        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == topicCategory.CategoryId);
        if (category == null) { throw new InvalidOperationException("Category not found for the given TopicCategory."); }
        return category;
    }
}