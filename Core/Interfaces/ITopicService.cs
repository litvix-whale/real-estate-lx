using Core.Entities;

namespace Core.Interfaces;

public interface ITopicService
{
    Task<string> SubscribeUserAsync(string email, string topicName);
    Task<string> UnsubscribeUserAsync(string email, string topicName);
    Task<IEnumerable<Topic>> GetTopicsByNameAsync(string name);
    Task<Topic> GetByNameAsync(string name);
    Task<Topic> GetByIdAsync(Guid id);
    Task<IEnumerable<Topic>> GetAllTopicsAsync();
    Task<IEnumerable<User>> GetTopicSubscribersAsync(string topicName);
    Task<string> CreateTopicAsync(string topicName, string categoryName = "");
    Task<string> DeleteTopicAsync(string name);
    Task<string> FilterTopicsByCategoryAsync(string category);
    Task<bool> IsUserSubscribedAsync(string email, string topicName);
    Task<IEnumerable<Topic>> GetTopics(int start, int count, string searchQuery);
}
