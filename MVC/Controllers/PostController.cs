using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MVC.Models;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Core.Entities;
using Infrastructure.Repositories;

namespace MVC.Controllers;

public class PostController : Controller
{
    private readonly IPostService _postService;
    private readonly IUserService _userService;
    private readonly ITopicService _topicService;
    private readonly INotificationService _notificationService;
    private readonly IPostVoteRepository _postvoteRepository;
    private readonly IUserTitleRepository _userTitleRepository;

    public PostController(
        IPostService postService, 
        IUserService userService, 
        ITopicService topicService,
        INotificationService notificationService,
        IPostVoteRepository postvoteRepository,
        IUserTitleRepository userTitleRepository
        )
    {
        _postService = postService;
        _userService = userService;
        _topicService = topicService;
        _notificationService = notificationService;
        _postvoteRepository = postvoteRepository;
        _userTitleRepository = userTitleRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string sortBy = "ratinga", int page = 1, int pageSize = 10) 
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdValue != null)
        {
            ViewBag.BannedTo = _userService.GetBannedTo(Guid.Parse(userIdValue));
        }
        else
        {
            ViewBag.BannedTo = null;
        }

        // Get all posts
        var allPosts = await _postService.GetAllPosts();
        var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

        // Calculate weight for each post
        var weightedPosts = new List<(Post Post, double Weight)>();
        
        foreach (var post in allPosts)
        {
            string? topicName = null;
            if (post.TopicId != null && post.TopicId != Guid.Empty)
            {
                var topic = await _topicService.GetByIdAsync(post.TopicId.Value);
                topicName = topic?.Name;
            }

            var postRating = await _postService.GetRateForPostAsync(post.Id); 
            double weight = await CalculatePostWeightAsync(
                post, 
                userEmail, 
                topicName ?? string.Empty, 
                postRating,
                DateTime.UtcNow);
                
            weightedPosts.Add((post, weight));
        }

        // Apply sorting based on sortBy parameter
        IOrderedEnumerable<(Post Post, double Weight)> sortedPosts;
        switch (sortBy.ToLower())
        {
            case "titlea":
                sortedPosts = weightedPosts.OrderBy(x => x.Post.Title);
                break;
            case "titled":
                sortedPosts = weightedPosts.OrderByDescending(x => x.Post.Title);
                break;
            case "ratinga": 
                sortedPosts = weightedPosts.OrderByDescending(x => x.Weight);
                break;
            case "ratingd": 
                sortedPosts = weightedPosts.OrderBy(x => x.Weight);
                break;
            case "newest":
                sortedPosts = weightedPosts.OrderByDescending(x => x.Post.CreatedAt);
                break;
            case "oldest":
                sortedPosts = weightedPosts.OrderBy(x => x.Post.CreatedAt);
                break;
            default: // Default to highest weight first
                sortedPosts = weightedPosts.OrderByDescending(x => x.Weight);
                break;
        }

        // Apply pagination
        var totalItems = sortedPosts.Count();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        
        // Ensure page is within valid range
        page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));
        
        // Get just the page of data we need
        var pagedPosts = sortedPosts
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.Post)
            .ToList();

        // Create the model with paged posts
        var model = new PostsListViewModel 
        { 
            Posts = pagedPosts,
            TopicNames = new Dictionary<Guid, string>(),
            CurrentPage = page,
            TotalPages = totalPages,
            SortBy = sortBy
        };
        
        // Load the topic information for each post (for display)
        foreach (var post in model.Posts)
        {
            if (post.TopicId != null && post.TopicId != Guid.Empty)
            {
                var topic = await _topicService.GetByIdAsync(post.TopicId.Value);
                if (topic != null)
                {
                    model.TopicNames[post.TopicId.Value] = topic.Name;
                }
            }
        }
        
        return View(model);
    }

    private async Task<double> CalculatePostWeightAsync(Post post, string userEmail, string topicName, int postRating, DateTime currentTime)
    {
        // Base weight components
        double subscriptionWeight = 0;
        double ratingWeight = 0;
        double timeWeight = 0;
        
        // 1. Subscription factor (higher weight if user is subscribed)
        if (!string.IsNullOrEmpty(userEmail) && !string.IsNullOrEmpty(topicName))
        {
            bool isSubscribed = await _topicService.IsUserSubscribedAsync(userEmail, topicName);
            if (isSubscribed)
            {
                subscriptionWeight = 50; // Bonus for subscribed topics
            }
        }
        
        // 2. Rating factor (using the provided post rating)
        ratingWeight = Math.Log(Math.Max(postRating, 0) + 1) * 20; // Using log to prevent extremely high ratings from dominating
        
        // 3. Time decay factor (newer posts get more weight)
        var hoursSinceCreation = (currentTime - post.CreatedAt).TotalHours;
        // Halflife of 24 hours (weight halves every 24 hours)
        timeWeight = 100 * Math.Pow(0.5, hoursSinceCreation / 24);
        
        // Combine weights
        return subscriptionWeight + ratingWeight + timeWeight;
    }


    [HttpGet]
    [Authorize] // Add authorization to ensure only logged-in users can access this page
    public async Task<IActionResult> Add(Guid? topicId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var topics = await _topicService.GetAllTopicsAsync();
        
        var model = new AddPostViewModel
        {
            UserId = Guid.Parse(userId ?? string.Empty),
            AvailableTopics = topics.ToList(),
            TopicId = topicId ?? Guid.Empty
        };
        
        return View(model);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Add(AddPostViewModel model, IFormFile? file)
    {
        if (ModelState.IsValid)
        {
            // Validate file size and type if needed
            if (file != null)
            {
                var allowedFileTypes = new[] { 
                    "image/jpeg", 
                    "image/png", 
                    "image/gif", 
                    "application/pdf", 
                    "text/plain", 
                    "application/msword", 
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                };

                if (file.Length > 10 * 1024 * 1024) // 10MB limit
                {
                    ModelState.AddModelError("file", "File size cannot exceed 10MB.");
                    model.AvailableTopics = (await _topicService.GetAllTopicsAsync()).ToList();
                    return View(model);
                }

                if (!allowedFileTypes.Contains(file.ContentType))
                {
                    ModelState.AddModelError("file", "File type not allowed.");
                    model.AvailableTopics = (await _topicService.GetAllTopicsAsync()).ToList();
                    return View(model);
                }
            }

            var (result, postId) = await _postService.AddPostAsync(model.Title, model.Text, file, model.UserId, model.TopicId);

            if (result == "Success" && postId != Guid.Empty)
            {
                var topic = await _topicService.GetByIdAsync(model.TopicId);
                if (topic != null)
                {
                    await _notificationService.CreateTopicNotificationAsync(
                        topic.Name, 
                        model.Title, 
                        postId);
                }
                
                return RedirectToAction("Index", "Post");
            }

            ModelState.AddModelError(string.Empty, result);
        }

        // Reload topics in case of validation errors
        model.AvailableTopics = (await _topicService.GetAllTopicsAsync()).ToList();
        return View(model);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Delete(string title)
    {
        if (ModelState.IsValid)
        {
            var result = await _postService.DeletePostAsync(title);

            if (result == "Success")
            {
                return RedirectToAction("Index", "Post");
            }

            ModelState.AddModelError(string.Empty, result);
        }

        return RedirectToAction("Index", "Post");
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdValue != null)
        {
            ViewBag.BannedTo = _userService.GetBannedTo(Guid.Parse(userIdValue));
        }
        else
        {
            ViewBag.BannedTo = null;
        }

        var post = await _postService.GetPostByIdAsync(id);
        if (post == null)
        {
            return NotFound();
        }

        var comments = await _postService.GetAllCommentsAsync(id);

        var userIds = comments.Select(c => c.UserId).Distinct().ToList();
        if (!userIds.Contains(post.UserId ?? Guid.Empty))
        {
            userIds.Add(post.UserId ?? Guid.Empty);
        }
        var postTopic = await _topicService.GetByIdAsync(post.TopicId ?? Guid.Empty);
        var userNameDictionary = new Dictionary<Guid, string>();
        var userProfilePictures = new Dictionary<Guid, string>();
        var userTitles = new Dictionary<Guid, List<string>>();

        foreach (var userId in userIds)
        {
            var user = await _userService.GetUserByIdAsync(userId);
            userNameDictionary[userId] = user?.UserName ?? "Unknown";
            userProfilePictures[userId] = user?.ProfilePicture ?? "pfp_1.png";
            Console.WriteLine($"Starting title retrieval for user: {user?.UserName ?? "null"}");

            var titles = new List<string>();
            if (user != null)
            {
                Console.WriteLine($"User exists with ID: {user.Id}");

                bool isAdmin = await _userService.IsUserAdmin(user.UserName!);
                Console.WriteLine($"Is admin check result: {isAdmin}");
                if (isAdmin && postTopic!.Id != Guid.Parse("071b0258-ff8d-4d29-8fc5-255cf558ac33"))
                {
                    Console.WriteLine("Adding Admin title");
                    titles.Add("Admin");
                }


                var userTitlesList = await _userTitleRepository.GetByUserIdAsync(userId);
                Console.WriteLine($"User titles count from repository: {userTitlesList?.Count() ?? 0}");

                if (userTitlesList != null && userTitlesList.Any())
                {
                    Console.WriteLine("Processing user titles from repository...");
                    foreach (var userTitle in userTitlesList)
                    {
                        Console.WriteLine($"Processing title with TopicId: {userTitle.TopicId}");
                        var titleTopic = await _topicService.GetByIdAsync(userTitle.TopicId);
                        Console.WriteLine($"Title topic name: {titleTopic?.Name ?? "null"}");
                        Console.WriteLine($"Post topic name: {postTopic?.Name ?? "null"}");

                        if (titleTopic != null && postTopic != null && titleTopic.Name == postTopic.Name)
                        {
                            Console.WriteLine($"Adding topic-specific title: {userTitle.Title}");
                            titles.Add(userTitle.Title);
                        }
                        else
                        {
                            Console.WriteLine("Topic names don't match or one of the topics is null");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("User has no titles defined in repository");
                }
            }
            else
            {
                Console.WriteLine("User is null");
            }

            Console.WriteLine($"Final titles count: {titles.Count}");
            foreach (var title in titles)
            {
                Console.WriteLine($"Title added: {title}");
            }
            userTitles[userId] = titles;
        }

        var userVotes = new Dictionary<Guid, string>();
        var userNamePost = new Dictionary<string, string>();
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _userService.GetUserByIdAsync(Guid.Parse(userId));
                userNamePost[user.UserName!] = user.ProfilePicture;
                userVotes = await _postService.GetUserVotesForCommentsAsync(Guid.Parse(userId), comments.Select(c => c.Id).ToList());
            }
        }

        var userPostVote = new PostVote();
        
        if (userIdValue! != null)
        {
            userPostVote = await _postvoteRepository.GetVoteByUser(post.Id, Guid.Parse(userIdValue!));
        }

        var model = new PostDetailsViewModel
        {
            Post = post,
            Comments = comments.ToList(),
            UserNames = userNameDictionary,
            UserVotes = userVotes,
            UserProfilePictures = userProfilePictures,
            UserPostVote = userPostVote?.RateType ?? string.Empty,
            UserPost = userNamePost,
            UserTitles = userTitles
        };

        // Check if the current user is subscribed to this post
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue(ClaimTypes.Name);
            if (!string.IsNullOrEmpty(email))
            {
                var isSubscribed = await _postService.IsUserSubscribedAsync(email, post.Title);
                ViewBag.IsSubscribed = isSubscribed;
            }
            else
            {
                ViewBag.IsSubscribed = false;
            }
        }

        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PostVote([FromBody] PostDetailsViewModel model)
    {
        if(ModelState.IsValid)
        {
            var userId = model.Post.UserId ?? Guid.Empty;
            var result = await _postService.PostVoteAsync(model.Post.Id, userId, model.UserPostVote);
            
            if (result.Item1 == "Success")
            {
                return Json(new { success = true, newScore = result.Item2 });
            }
            
            return Json(new { success = false, message = result.Item1 });
        }
        
        return Json(new { success = false, message = "Invalid model state" });
    }


    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Subscribe(string title)
    {
        var email = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(email))
        {
            ModelState.AddModelError(string.Empty, "Unable to subscribe: email is missing.");
            return RedirectToAction("Details", "Post", new { id = await GetPostIdByTitle(title) });
        }
        var result = await _postService.SubscribeUserAsync(email, title);
        
        return RedirectToAction("Details", "Post", new { id = await GetPostIdByTitle(title) });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Unsubscribe(string title)
    {
        var email = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(email))
        {
            ModelState.AddModelError(string.Empty, "Unable to unsubscribe: email is missing.");
            return RedirectToAction("Details", "Post", new { id = await GetPostIdByTitle(title) });
        }

        var result = await _postService.UnsubscribeUserAsync(email, title);
        
        return RedirectToAction("Details", "Post", new { id = await GetPostIdByTitle(title) });
    }

    private async Task<Guid> GetPostIdByTitle(string title)
    {
        var post = await _postService.GetPostByTitleAsync(title);
        return post?.Id ?? Guid.Empty;
    }

    private async Task<IFormFile?> GetFileForPost(Guid postId)
    {
        var post = await _postService.GetPostByIdAsync(postId);
        if (post != null)
        {
            return await _postService.GetFileAsync(postId);
        }
        return null;
    }

    [HttpGet]
    public async Task<IActionResult> DownloadFile(Guid postId)
    {
        var file = await _postService.GetFileAsync(postId);
        if (file == null)
        {
            return NotFound();
        }

        return File(file.OpenReadStream(), file.ContentType, file.FileName);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadFileComment(Guid commentId)
    {
        var file = await _postService.GetCommentFileAsync(commentId);
        if (file == null)
        {
            return NotFound();
        }

        return File(file.OpenReadStream(), file.ContentType, file.FileName);
    }

    [HttpGet]
    public async Task<IActionResult> ViewImage(Guid postId)
    {
        var post = await _postService.GetPostByIdAsync(postId);
        if (post == null || post.FileContent == null || !post.FileType!.StartsWith("image/"))
        {
            return NotFound();
        }

        return File(post.FileContent, post.FileType);
    }

    [HttpGet]
    public async Task<IActionResult> GetUserByPost(Guid postId)
    {
        var post = await _postService.GetPostByIdAsync(postId);
        if (post == null)
        {
            return NotFound();
        }

        var user = await _userService.GetUserByIdAsync(post.UserId ?? Guid.Empty);
        if (user == null)
        {
            return NotFound();
        }

        return RedirectToAction("Details", "User", new { id = user.Id });
    }
}