using Core.Entities;

namespace Core.Interfaces;

public interface IPostVoteRepository : IRepository<PostVote>
{
    Task<PostVote> GetVoteByUser(Guid postId, Guid userId);
}