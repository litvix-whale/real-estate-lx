using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Repositories;

public class NotificationRepository : RepositoryBase<Notification>, INotificationRepository
{
    public NotificationRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Notification>> GetNotificationsForUserAsync(Guid userId)
    {
        return await _context.Set<Notification>()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<string> CreateAsync(Notification notification)
    {
        try
        {
            notification.Id = Guid.NewGuid();
            notification.CreatedAt = DateTime.UtcNow;
            notification.IsRead = false;
            
            await _context.Set<Notification>().AddAsync(notification);
            await _context.SaveChangesAsync();
            return "Success";
        }
        catch (Exception ex)
        {
            return $"Error creating notification: {ex.Message}";
        }
    }

    public async Task<string> MarkAsReadAsync(Guid id)
    {
        try
        {
            var notification = await _context.Set<Notification>().FindAsync(id);
            if (notification == null)
                return "Notification not found";

            notification.IsRead = true;
            _context.Set<Notification>().Update(notification);
            await _context.SaveChangesAsync();
            return "Success";
        }
        catch (Exception ex)
        {
            return $"Error marking notification as read: {ex.Message}";
        }
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _context.Set<Notification>()
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task<string> MarkAllAsReadAsync(Guid userId)
    {
        try
        {
            var notifications = await _context.Set<Notification>()
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return "Success";
        }
        catch (Exception ex)
        {
            return $"Error marking all notifications as read: {ex.Message}";
        }
    }

    public async Task<string> DeleteByContentAsync(string content)
    {
        try
        {
            var notifications = await _context.Set<Notification>()
                .Where(n => n.Message.Contains(content))
                .ToListAsync();

            _context.Set<Notification>().RemoveRange(notifications);
            await _context.SaveChangesAsync();
            return "Notifications deleted successfully";
        }
        catch (Exception ex)
        {
            return $"Error deleting notifications by content id: {ex.Message}";
        }
    }
}