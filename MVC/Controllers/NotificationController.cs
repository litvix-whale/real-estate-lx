using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Core.Interfaces;
using MVC.Models;
using System.Security.Claims;
using System;
using System.Threading.Tasks;

namespace MVC.Controllers;

[Authorize]
public class NotificationController : Controller
{
    private readonly INotificationService _notificationService;
    private readonly IPostService _postService;
    private readonly ITopicService _topicService;

    public NotificationController(
        INotificationService notificationService,
        IPostService postService,
        ITopicService topicService)
    {
        _notificationService = notificationService;
        _postService = postService;
        _topicService = topicService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId == Guid.Empty)
        {
            return Challenge();
        }

        var notifications = await _notificationService.GetNotificationsForUserAsync(userId);
        var model = new NotificationsViewModel
        {
            Notifications = notifications,
            UnreadCount = await _notificationService.GetUnreadCountAsync(userId)
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> MarkAsRead(Guid id, string? returnUrl = null)
    {
        var result = await _notificationService.MarkAsReadAsync(id);
        if (result != "Success")
        {
            TempData["ErrorMessage"] = result;
        }

        if (!string.IsNullOrEmpty(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty);
        var result = await _notificationService.MarkAllAsReadAsync(userId);
        if (result != "Success")
        {
            TempData["ErrorMessage"] = result;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _notificationService.DeleteAsync(id);
        if (result != "Success")
        {
            TempData["ErrorMessage"] = result;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Json(new { count = 0 });
        
        var count = await _notificationService.GetUnreadCountAsync(new Guid(userId));
        return Json(new { count = count });
    }

    [HttpPost]
    public async Task<IActionResult> NavigateToPost(Guid notificationId, Guid postId)
    {
        await _notificationService.MarkAsReadAsync(notificationId);
        
        return RedirectToAction("Details", "Post", new { id = postId });
    }
}