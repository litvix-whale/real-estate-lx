using Core.Entities;

namespace MVC.Models;

public class NotificationsViewModel
{
    public IEnumerable<Notification> Notifications { get; set; } = new List<Notification>();
    public int UnreadCount { get; set; }
}

public class NotificationBadgeViewModel
{
    public int UnreadCount { get; set; }
}