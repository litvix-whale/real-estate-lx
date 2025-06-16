using Core.Entities;

namespace Core.Interfaces;

public interface IPostRepository : IRepository<Post>
{
   public Task<Post> GetByTitleAsync(string name);

   public Task<IEnumerable<Post>> GetPostsByUserNameAsync(string name);

   public Task<List<Post>> GetAllPosts();

   public Task<Post> GetPostByTitleAsync(string title);
   public Task<IEnumerable<Post>> GetPostsByTopicAsync(Guid topicId);
    Task<IEnumerable<object>> GetByTopicIdAsync(Guid topicId);
}