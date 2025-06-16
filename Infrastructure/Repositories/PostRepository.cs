using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class PostRepository(AppDbContext context) : RepositoryBase<Post>(context), IPostRepository
{
    public async Task<Post> GetByTitleAsync(string name)
    {
        var post = await _context.Posts.FirstOrDefaultAsync(p => p.Title == name);
        if (post == null)
        {
            throw new InvalidOperationException($"Post with name '{name}' not found.");
        }
        return post;
    }

    public async Task<IEnumerable<Post>> GetPostsByUserNameAsync(string name)
    {
        return await _context.Posts.Where(p => p.Title.Contains(name)).ToListAsync();
    }

    public async Task<List<Post>> GetAllPosts()
    {
        return await _context.Posts.ToListAsync();
    }

    public async Task<Post> GetPostByTitleAsync(string title)
    {
        return await _context.Posts.FirstOrDefaultAsync(p => p.Title == title) ?? throw new InvalidOperationException("Post not found.");
    }

    public async Task<IEnumerable<Post>> GetPostsByTopicAsync(Guid topicId)
    {
        return await _context.Posts.Where(p => p.TopicId == topicId).ToListAsync();
    }

    public async Task<IEnumerable<object>> GetByTopicIdAsync(Guid topicId)
    {
        return await _context.Posts.Where(p => p.TopicId == topicId).ToListAsync();
    }
}