using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class UserTopicRepository(AppDbContext context) : RepositoryBase<UserTopic>(context), IUserTopicRepository
{
    public async Task<string> AddAsync(string email, string topicName)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == email || u.Email == email);
        if (user == null)
        {
            return "User not found";
        }

        var topic = await _context.Topics.FirstOrDefaultAsync(t => t.Name == topicName);
        if (topic == null)
        {
            return "Topic not found";
        }

        var userTopic = new UserTopic
        {
            UserId = user.Id,
            TopicId = topic.Id
        };

        await _context.UserTopics.AddAsync(userTopic);
        await _context.SaveChangesAsync();
        return "Success";
    }

    public async Task<string> DeleteAsync(string email, string topicName)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email || u.UserName == email);
        if (user == null)
        {
            return "User not found";
        }

        var topic = await _context.Topics.FirstOrDefaultAsync(t => t.Name == topicName);
        if (topic == null)
        {
            return "Topic not found";
        }

        var userTopic = await _context.UserTopics.FirstOrDefaultAsync(ut => ut.UserId == user.Id && ut.TopicId == topic.Id);
        if (userTopic == null)
        {
            return "UserTopic not found";
        }

        _context.UserTopics.Remove(userTopic);
        await _context.SaveChangesAsync();
        return "Success";
    }

    public async Task<bool> IsUserSubscribedAsync(string email, string topicName)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email || u.UserName == email);
        if (user == null)
        {
            return false;
        }

        var topic = await _context.Topics.FirstOrDefaultAsync(t => t.Name == topicName);
        if (topic == null)
        {
            return false;
        }

        return await _context.UserTopics.AnyAsync(ut => ut.UserId == user.Id && ut.TopicId == topic.Id);
    }

    public async Task<IEnumerable<User>> GetTopicSubscribersAsync(string topicName)
    {
        var topic = await _context.Topics.FirstOrDefaultAsync(t => t.Name == topicName);
        if (topic == null)
        {
            return new List<User>();
        }

        var userTopics = await _context.UserTopics.Where(ut => ut.TopicId == topic.Id).ToListAsync();
        var subscribers = new List<User>();
        foreach (var userTopic in userTopics)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userTopic.UserId);
            if (user != null)
            {
                subscribers.Add(user);
            }
        }

        return subscribers;
    }

    public async Task<IEnumerable<Topic>> GetUserTopicsAsync(Guid userId)
    {
        var userTopics = await _context.UserTopics.Where(ut => ut.UserId == userId).ToListAsync();
        var topics = new List<Topic>();
        foreach (var userTopic in userTopics)
        {
            var topic = await _context.Topics.FirstOrDefaultAsync(t => t.Id == userTopic.TopicId);
            if (topic != null)
            {
                topics.Add(topic);
            }
        }

        return topics;
    }
}