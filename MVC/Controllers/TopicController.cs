using Microsoft.AspNetCore.Mvc;
using MVC.Models;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Core.Entities;

namespace MVC.Controllers;

public class TopicController : Controller
{
    private readonly ITopicService _topicService;
    private readonly INotificationService _notificationService;
    private readonly ICategoryService _categoryService;
    private readonly ITopicCategoryRepository _topicCategoryRepository;
    private readonly IPostRepository _postRepository;

    public TopicController(ITopicService topicService, INotificationService notificationService, ICategoryService categoryService, ITopicCategoryRepository topicCategoryRepository, IPostRepository postRepository)
    {
        _topicService = topicService;
        _notificationService = notificationService;
        _categoryService = categoryService;
        _topicCategoryRepository = topicCategoryRepository;
        _postRepository = postRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int count = 7, string searchQuery = "")
    {
        var topics = await _topicService.GetTopics((page - 1) * count, count, searchQuery);
        var userName = User.FindFirstValue(ClaimTypes.Name);
        var model = new TopicsListViewModel
        {
            Topics = new List<TopicViewModel>(),
            Categories = await _categoryService.GetAllCategoriesAsync()
        };

        foreach (var topic in topics)
        {
            var isUserSubscribed = userName != null && await _topicService.IsUserSubscribedAsync(userName, topic.Name);
            model.Topics.Add(new TopicViewModel
            {
                Topic = topic,
                Category = await _topicCategoryRepository.GetCategoryByTopicNameAsync(topic.Name) ?? new Category { Name = "Uncategorized" },
                IsUserSubscribed = isUserSubscribed
            });
        }

        return View(model);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult Add()
    {
        return View();
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Add(string topicName, string categoryName = "")
    {
        if (ModelState.IsValid)
        {
            var result = await _topicService.CreateTopicAsync(topicName, categoryName);

            if (result == "Success")
            {
                await _notificationService.DeleteByContentAsync($"asked for topic '{topicName}'");
                return RedirectToAction("Index", "Topic");
            }

            ModelState.AddModelError(string.Empty, result);
        }

        return View(topicName, categoryName);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult Delete()
    {
        return View();
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string name)
    {
        if (ModelState.IsValid)
        {
            var result = await _topicService.DeleteTopicAsync(name);

            if (result == "Success")
            {
                return RedirectToAction("Index", "Topic");
            }

            ModelState.AddModelError(string.Empty, result);
        }

        return View(name);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Subscribe(string topicName)
    {
        var userName = User.FindFirstValue(ClaimTypes.Name);

        if (topicName == null)
        {
            return BadRequest("Topic not found.");
        }

        var success = await _topicService.SubscribeUserAsync(userName!, topicName);
        if (success != "Success")
        {
            return BadRequest("Failed to subscribe to the topic.");
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Unsubscribe(string topicName)
    {
        var userName = User.FindFirstValue(ClaimTypes.Name);

        if (topicName == null)
        {
            return BadRequest("Topic not found.");
        }

        var success = await _topicService.UnsubscribeUserAsync(userName!, topicName);
        if (success != "Success")
        {
            return BadRequest("Failed to unsubscribe from the topic.");
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AskForTopic(string topicName)
    {
        var userName = User.FindFirstValue(ClaimTypes.Name);

        if (topicName == null)
        {
            return BadRequest("Topic not found.");
        }

        var success = await _notificationService.AskAdminForTopicAsync(userName!, topicName);
        if (success != "Success")
        {
            return BadRequest("Failed to ask for the topic.");
        }

        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Posts(Guid topicId, string sortBy = "newesta", int page = 1)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid input.");
        }

        var posts = await _postRepository.GetPostsByTopicAsync(topicId);
        //sort posts by title, rating or time added based on sortBy parameter asc or desc
        switch (sortBy)
        {
            case "titlea":
                posts = posts.OrderBy(p => p.Title);
                break;
            case "titled":
                posts = posts.OrderByDescending(p => p.Title);
                break;
            // case "ratinga":
            //     posts = posts.OrderBy(p => p.Rating);
            //     break;
            // case "ratingd":
            //     posts = posts.OrderByDescending(p => p.Rating);
            //     break;
            case "newest":
                posts = posts.OrderByDescending(p => p.CreatedAt);
                break;
            case "oldest":
                posts = posts.OrderBy(p => p.CreatedAt);
                break;
            default:
                posts = posts.OrderByDescending(p => p.CreatedAt);
                break;
        }
        int postsCount = posts.Count();
        const int pageSize = 5;
        var items = posts.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var model = new PostsListViewModel
        {
            Posts = items,
            TopicNames = null!,
            PageNumber = page,
            TotalPages = (int)Math.Ceiling(postsCount / (double)pageSize),
            TopicId = topicId
        };

        ViewBag.TopicId = topicId;
        return View(model);
    }
}
