using Core.Entities;

namespace Core.Interfaces;

public interface INotificationService
{
    Task<IEnumerable<Notification>> GetNotificationsForUserAsync(Guid userId);
    Task<Notification> GetByIdAsync(Guid id);
    Task<string> CreateNotificationAsync(string message, Guid userId, Guid? postId = null, Guid? topicId = null);
    Task<string> CreateTopicNotificationAsync(string topicName, string postTitle, Guid postId);
    Task<string> MarkAsReadAsync(Guid id);
    Task<string> MarkAllAsReadAsync(Guid userId);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task<string> DeleteAsync(Guid id);
    Task<string> AskAdminForTopicAsync(string email, string topicName);
    Task<string> DeleteByContentAsync(string message);
}