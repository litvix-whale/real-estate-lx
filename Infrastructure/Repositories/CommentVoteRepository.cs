using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class CommentVoteRepository(AppDbContext context) : RepositoryBase<CommentVote>(context), ICommentVoteRepository
{

    public async Task<CommentVote> GetByCommentIdAndUserIdAsync(Guid commentId, Guid userId)
    {
        var comment =  await _context.CommentVotes.FirstOrDefaultAsync(cv => cv.CommentId == commentId && cv.UserId == userId);

        return comment!;
    }

    public async Task<List<CommentVote>> GetAllByUserIdAndCommentIdsAsync(Guid commentId, List<Guid> commentIds)
    {
        var commentVotes = await _context.CommentVotes.Where(cv => cv.UserId == commentId && commentIds.Contains(cv.CommentId)).ToListAsync();

        return commentVotes;
    }
}