using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Logic.Hubs;

namespace Logic.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ITopicService _topicService;
    private readonly IUserService _userService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationService(
        INotificationRepository notificationRepository,
        ITopicService topicService,
        IUserService userService,
        IHubContext<NotificationHub> hubContext)
    {
        _notificationRepository = notificationRepository;
        _topicService = topicService;
        _userService = userService;
        _hubContext = hubContext;
    }

    public async Task<IEnumerable<Notification>> GetNotificationsForUserAsync(Guid userId)
    {
        return await _notificationRepository.GetNotificationsForUserAsync(userId);
    }

    public async Task<Notification> GetByIdAsync(Guid id)
    {
        var notification = await _notificationRepository.GetByIdAsync(id) ?? throw new InvalidOperationException("Notification not found.");
        return notification;
    }

    public async Task<string> CreateNotificationAsync(string message, Guid userId, Guid? postId = null, Guid? topicId = null)
    {
        var notification = new Notification
        {
            Message = message,
            UserId = userId,
            PostId = postId,
            TopicId = topicId,
            IsRead = false
        };

        await _notificationRepository.CreateAsync(notification);

        // Send real-time notification via SignalR
        await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", message);

        return "Success";
    }

    public async Task<string> CreateTopicNotificationAsync(string topicName, string postTitle, Guid postId)
    {
        try
        {
            // Get the topic
            var topic = await _topicService.GetByNameAsync(topicName);
            if (topic == null)
            {
                Console.WriteLine($"Topic not found: {topicName}");
                return "Topic not found";
            }

            // Get subscribers
            var subscribers = await _topicService.GetTopicSubscribersAsync(topicName);
            Console.WriteLine($"Found {subscribers.Count()} subscribers for topic {topicName}");
            
            // Create notification for each subscriber
            foreach (var subscriber in subscribers)
            {
                var message = $"New post '{postTitle}' was added to topic '{topicName}'";
                var result = await CreateNotificationAsync(message, subscriber.Id, postId, topic.Id);
            }

            return "Success";
        }
        catch (Exception ex)
        {
            return $"Error creating topic notification: {ex.Message}";
        }
    }

    public async Task<string> AskAdminForTopicAsync(string email, string topicName)
    {
        var message = $"User {email} asked for topic '{topicName}'";
        string adminId = await _userService.GetUserIdByName("Admin");
        var result = await CreateNotificationAsync(message, Guid.Parse(adminId));
        return result;
    }

    public async Task<string> MarkAsReadAsync(Guid id)
    {
        return await _notificationRepository.MarkAsReadAsync(id);
    }

    public async Task<string> MarkAllAsReadAsync(Guid userId)
    {
        return await _notificationRepository.MarkAllAsReadAsync(userId);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _notificationRepository.GetUnreadCountAsync(userId);
    }

    public async Task<string> DeleteAsync(Guid id)
    {
        await _notificationRepository.DeleteAsync(id);
        return "Notification deleted successfully";
    }

    public async Task<string> DeleteByContentAsync(string message)
    {
        await _notificationRepository.DeleteByContentAsync(message);
        return "Notifications deleted successfully";
    }
}