using Core.Entities;

namespace Core.Interfaces;

public interface IUserPostRepository : IRepository<UserPost>
{
    Task<UserPost?> GetByUserIdAndPostIdAsync(Guid userId, Guid postId);
}