using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class CommentRepository(AppDbContext context) : RepositoryBase<Comment>(context), ICommentRepository
{
    public async Task<List<Comment>> GetAllCommentsAsync(Guid postId)
    {
        var comments = await _context.Comments.ToListAsync();

        return comments.Where(c => c.PostId == postId).ToList();
    }

    public async Task<IEnumerable<Comment>> GetByUserIdAndPostIdAsync(Guid userId, object id)
    {
        var comments = await _context.Comments.ToListAsync();

        return comments.Where(c => c.UserId == userId && c.PostId == (Guid)id).ToList();
    }
}