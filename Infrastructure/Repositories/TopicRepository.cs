using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class TopicRepository(AppDbContext context) : RepositoryBase<Topic>(context), ITopicRepository
{
    public async Task<Topic> GetByNameAsync(string name)
    {
        var topic = await _context.Topics.FirstOrDefaultAsync(t => t.Name == name);
        if (topic == null)
        {
            throw new InvalidOperationException($"Topic with name '{name}' not found.");
        }
        return topic;
    }

    public async Task<IEnumerable<Topic>> GetTopicsByNameAsync(string name)
    {
        return await _context.Topics.Where(t => t.Name.Contains(name)).ToListAsync();
    }

    public async Task<IEnumerable<Topic>> GetAllTopicsAsync()
    {
        return await _context.Topics.ToListAsync();
    }

    public async Task<IEnumerable<Topic>> GetTopics(int start, int count, string searchQuery)
    {
        return await _context.Topics.Skip(start).Take(count).Where(x => x.Name.ToLower().Contains(searchQuery.ToLower())).ToListAsync();
    }
}
