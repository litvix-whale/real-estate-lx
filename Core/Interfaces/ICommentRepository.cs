using Core.Entities;

namespace Core.Interfaces;

public interface ICommentRepository : IRepository<Comment>
{
    public Task<List<Comment>> GetAllCommentsAsync(Guid postId);
    Task<IEnumerable<Comment>> GetByUserIdAndPostIdAsync(Guid userId, object id);
}