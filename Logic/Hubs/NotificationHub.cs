using Microsoft.AspNetCore.SignalR;

namespace Logic.Hubs;

public class NotificationHub : Hub
{
    public async Task SendNotification(Guid user, string message)
    {
        await Clients.User(user.ToString()).SendAsync("ReceiveNotification", message);
    }
    
    public async Task SendNotificationToAll(string message)
    {
        await Clients.All.SendAsync("ReceiveNotification", message);
    }
    
    public async Task SendNotificationToGroup(string groupName, string message)
    {
        await Clients.Group(groupName).SendAsync("ReceiveNotification", message);
    }
    
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }
    
    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }
}