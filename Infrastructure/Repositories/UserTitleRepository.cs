using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories;

public class UserTitleRepository : IUserTitleRepository
{
    private readonly AppDbContext _context;

    public UserTitleRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserTitle?> GetByUserIdAndTopicIdAsync(Guid userId, Guid topicId)
    {
        return await _context.UserTitles
            .FirstOrDefaultAsync(ut => ut.UserId == userId && ut.TopicId == topicId);
    }

    public async Task<List<UserTitle>> GetByUserIdAsync(Guid userId)
    {
        return await _context.UserTitles
            .Include(ut => ut.Topic)
            .Where(ut => ut.UserId == userId)
            .ToListAsync();
    }

    public async Task AddAsync(UserTitle userTitle)
    {
        await _context.UserTitles.AddAsync(userTitle);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var userTitle = await _context.UserTitles.FindAsync(id);
        if (userTitle != null)
        {
            _context.UserTitles.Remove(userTitle);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> UserHasTitleAsync(Guid userId, Guid topicId)
    {
        return await _context.UserTitles.AnyAsync(ut => 
            ut.UserId == userId && ut.TopicId == topicId);
    }
}