using Core.Entities;

namespace Core.Interfaces;

public interface ITopicRepository : IRepository<Topic>
{
    Task<Topic> GetByNameAsync(string name);
    Task<IEnumerable<Topic>> GetTopicsByNameAsync(string name);
    Task<IEnumerable<Topic>> GetAllTopicsAsync();
    Task<IEnumerable<Topic>> GetTopics(int start, int count, string searchQuery);
}
