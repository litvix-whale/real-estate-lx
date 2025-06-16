using Core.Entities;

namespace Core.Interfaces;

public interface IUserTopicRepository : IRepository<UserTopic>
{
    Task<string> AddAsync(string email, string topicName);
    Task<string> DeleteAsync(string email, string topicName);
    Task<bool> IsUserSubscribedAsync(string email, string topicName);
    Task<IEnumerable<User>> GetTopicSubscribersAsync(string topicName);
    Task<IEnumerable<Topic>> GetUserTopicsAsync(Guid userId);
}