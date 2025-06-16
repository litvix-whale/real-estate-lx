using Core.Entities;

namespace Core.Interfaces;

public interface INotificationRepository : IRepository<Notification>
{
    Task<IEnumerable<Notification>> GetNotificationsForUserAsync(Guid userId);
    Task<string> CreateAsync(Notification notification);
    Task<string> MarkAsReadAsync(Guid id);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task<string> MarkAllAsReadAsync(Guid userId);
    Task<string> DeleteByContentAsync(string content);
}