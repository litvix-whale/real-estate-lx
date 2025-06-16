using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class UserPostRepository(AppDbContext context) : RepositoryBase<UserPost>(context), IUserPostRepository
{
    public async Task<UserPost?> GetByUserIdAndPostIdAsync(Guid userId, Guid postId)
    {
        var userPost = await _context.UserPosts
            .FirstOrDefaultAsync(up => up.UserId == userId && up.PostId == postId);
        
        if (userPost == null)
        {
            return null;
        }
        
        return userPost;
    }
}