using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MVC.Models;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Security.Claims;
using Microsoft.JSInterop;

namespace MVC.Controllers;

public class CommentController : Controller
{
    private readonly IPostService _postService;
    private readonly INotificationService _notificationService;

    public CommentController(IPostService postService, INotificationService notificationService)
    {
        _postService = postService;
        _notificationService = notificationService;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Add(string text, IFormFile? file, Guid postId, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return RedirectToAction("Details", "Post", new { id = postId });
        }

        var result = await _postService.AddCommentAsync(text, file, userId, postId);

        if (result == "Success")
        {
            // Get the post
            var post = await _postService.GetPostByIdAsync(postId);
            
            if (post != null)
            {
                // Get subscribers to this post
                var subscribers = await _postService.GetPostSubscribersAsync(postId);
                
                // Send notifications to subscribers
                foreach (var subscriber in subscribers)
                {
                    if (subscriber.Id != userId) // Don't notify the comment author
                    {
                        await _notificationService.CreateNotificationAsync(
                            $"New comment on post '{post.Title}' you're subscribed to",
                            subscriber.Id,
                            postId);
                    }
                }
            }
        }

        return RedirectToAction("Details", "Post", new { id = postId });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Delete(Guid commentId, Guid postId)
    {
        if (ModelState.IsValid)
        {
            var result = await _postService.DeleteCommentAsync(commentId);

            if (result == "Success")
            {
                return RedirectToAction("Details", "Post", new { id = postId });
            }

            ModelState.AddModelError(string.Empty, result);
        }

        return RedirectToAction("Details", "Post", new { id = postId });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Vote([FromBody] CommentVoteModel model)
    {
        if (ModelState.IsValid)
        {
            var (result, newScore) = await _postService.VoteAsync(model.CommentId, model.UserId, model.VoteType);

            if (result == "Success")
            {
                return Json(new {success = true, newScore});
            }

            return Json(new {success = false, message = result});
        }

        return Json(new {success = false, message = "Invalid data"});
    }
}