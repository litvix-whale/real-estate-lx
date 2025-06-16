using Core.Entities;

namespace Core.Interfaces;

public interface ICommentVoteRepository : IRepository<CommentVote>
{
    Task<CommentVote> GetByCommentIdAndUserIdAsync(Guid commentId, Guid userId);

    Task<List<CommentVote>> GetAllByUserIdAndCommentIdsAsync(Guid commentId, List<Guid> commentIds);
}