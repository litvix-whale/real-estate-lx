using Core.Entities;
using Microsoft.AspNetCore.Identity;

namespace Core.Interfaces;

public interface ITopicCategoryRepository : IRepository<TopicCategory>
{
    Task<string> AddAsync(string topicName, string categoryName);
    Task<string> DeleteAsync(string topicName, string categoryName);
    Task<bool> IsTopicInCategoryAsync(string topicName, string categoryName);
    Task<Category> GetCategoryByTopicNameAsync(string topicName);
}