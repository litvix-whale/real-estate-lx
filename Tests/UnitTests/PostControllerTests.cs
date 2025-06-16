using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using MVC.Controllers;
using MVC.Models;
using Core.Interfaces;
using Core.Entities;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
namespace Tests.UnitTests;
public class PostControllerTests
{
    private readonly Mock<IPostService> _mockPostService;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<ITopicService> _mockTopicService;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<IPostVoteRepository> _mockPostVoteRepository;
    private readonly Mock<IUserTitleRepository> _mockUserTitleRepository;
    private readonly PostController _controller;

    public PostControllerTests()
    {
        _mockPostService = new Mock<IPostService>();
        _mockUserService = new Mock<IUserService>();
        _mockTopicService = new Mock<ITopicService>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockPostVoteRepository = new Mock<IPostVoteRepository>();
        _mockUserTitleRepository = new Mock<IUserTitleRepository>();

        _controller = new PostController(
            _mockPostService.Object,
            _mockUserService.Object,
            _mockTopicService.Object,
            _mockNotificationService.Object,
            _mockPostVoteRepository.Object,
            _mockUserTitleRepository.Object
            );
    }

    #region Index Tests
    [Fact]
    public async Task Index_UserIsBanned_ReturnsViewWithBannedToDate()
    {
        // Arrange
        var bannedUntil = "2025-12-31";
        var userId = Guid.NewGuid();
        
        _mockUserService.Setup(s => s.GetBannedTo(userId))
            .Returns(bannedUntil);

        _mockPostService.Setup(s => s.GetAllPosts())
            .ReturnsAsync(new List<Post>());

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "mock"));
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(bannedUntil, viewResult.ViewData["BannedTo"]);
        var viewModel = Assert.IsType<PostsListViewModel>(viewResult.Model);
        Assert.Empty(viewModel.Posts);
    }

   [Fact]
    public async Task Index_SortsPostsByWeight_ReturnsCorrectOrder()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Name, "test@example.com")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        
        var posts = new List<Post>
        {
            new Post { Id = Guid.NewGuid(), Title = "Post 1", CreatedAt = DateTime.UtcNow.AddHours(-12) },
            new Post { Id = Guid.NewGuid(), Title = "Post 2", CreatedAt = DateTime.UtcNow.AddHours(-24) }
        };

        _mockPostService.Setup(s => s.GetAllPosts())
            .ReturnsAsync(posts);

        _mockTopicService.Setup(s => s.IsUserSubscribedAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        _mockPostService.Setup(s => s.GetRateForPostAsync(It.IsAny<Guid>()))
            .ReturnsAsync(0);

        // Set up the controller context with the user principal
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };

        // Act
        var result = await _controller.Index("ratinga");

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PostsListViewModel>(viewResult.Model);
        Assert.Equal(2, model.Posts.Count);
        // Newer post should have higher weight
        Assert.Equal("Post 1", model.Posts[0].Title);
    }
    #endregion

    #region Add Tests
    [Fact]
    public async Task Add_ReturnsViewWithTopics_ForAuthenticatedUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testTopics = new List<Topic>
        {
            new Topic { Id = Guid.NewGuid(), Name = "ASP.NET Core" },
            new Topic { Id = Guid.NewGuid(), Name = "Docker" }
        };

        _mockTopicService.Setup(x => x.GetAllTopicsAsync())
            .ReturnsAsync(testTopics);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "test"));
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

        // Act
        var result = await _controller.Add(null);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<AddPostViewModel>(viewResult.Model);
        Assert.Equal(userId, model.UserId);
        Assert.Equal(2, model.AvailableTopics.Count);
    }
    #endregion

    #region Details Tests
    [Fact]
    public async Task Details_ValidPostId_ReturnsViewWithModel()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var testPost = new Post { Id = postId, Title = "Test Post", UserId = userId };
        var testComments = new List<Comment>
        {
            new Comment { Id = Guid.NewGuid(), PostId = postId, UserId = userId, Text = "Comment 1" }
        };

        _mockPostService.Setup(s => s.GetPostByIdAsync(postId))
            .ReturnsAsync(testPost);
        _mockPostService.Setup(s => s.GetAllCommentsAsync(postId))
            .ReturnsAsync(testComments);
        _mockUserService.Setup(s => s.GetUserByIdAsync(userId))
            .ReturnsAsync(new User { Id = userId, UserName = "TestUser" });

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "test"));
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

        // Act
        var result = await _controller.Details(postId);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PostDetailsViewModel>(viewResult.Model);
        Assert.Equal(testPost, model.Post);
        Assert.Single(model.Comments);
    }
    #endregion

    #region Vote Tests
    [Fact]
    public async Task PostVote_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var model = new PostDetailsViewModel
        {
            Post = new Post { Id = Guid.NewGuid(), UserId = Guid.NewGuid() },
            UserPostVote = "upvote"
        };

        _mockPostService.Setup(s => s.PostVoteAsync(model.Post.Id, model.Post.UserId.Value, model.UserPostVote))
            .ReturnsAsync(("Success", 1));

        // Act
        var result = await _controller.PostVote(model);

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        
        
        var json = JsonConvert.SerializeObject(jsonResult.Value);
        var response = JObject.Parse(json);
        Assert.True(response.Value<bool>("success"));
        Assert.Equal(1, response.Value<int>("newScore"));
           
    }
    [Fact]
    public async Task PostVote_InvalidModel_ReturnsFailure()
    {
        // Arrange
        var model = new PostDetailsViewModel
        {
            Post = new Post { Id = Guid.NewGuid() },
            UserPostVote = "invalid_vote_type" // Invalid vote type
        };

        // Act
        var result = await _controller.PostVote(model);

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        var json = JsonConvert.SerializeObject(jsonResult.Value);
        var response = JObject.Parse(json);
        Assert.False(response.Value<bool>("success"));
    }

    [Fact]
    public async Task PostVote_UnauthorizedUser_ReturnsFailure()
    {
        // Arrange
        var model = new PostDetailsViewModel
        {
            Post = new Post { Id = Guid.NewGuid() }, // No UserId set
            UserPostVote = "upvote"
        };

        // Act
        var result = await _controller.PostVote(model);

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        var json = JsonConvert.SerializeObject(jsonResult.Value);
        var response = JObject.Parse(json);
        Assert.False(response.Value<bool>("success"));
    }
    #endregion

    #region Subscription Tests
    [Fact]
    public async Task Subscribe_ValidEmailAndTitle_ReturnsRedirectToDetails()
    {
        // Arrange
        var testTitle = "Test Post";
        var testEmail = "user@example.com";
        var testPostId = Guid.NewGuid();

        _mockPostService.Setup(s => s.SubscribeUserAsync(testEmail, testTitle))
            .ReturnsAsync("Success");
        _mockPostService.Setup(s => s.GetPostByTitleAsync(testTitle))
            .ReturnsAsync(new Post { Id = testPostId });

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.Name, testEmail)
        }, "test"));
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

        // Act
        var result = await _controller.Subscribe(testTitle);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(testPostId, redirectResult.RouteValues!["id"]);
    }
    #endregion

    #region File Download Tests
    [Fact]
    public async Task DownloadFile_ExistingFile_ReturnsFileResult()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());
        mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
        mockFile.Setup(f => f.FileName).Returns("test.jpg");

        _mockPostService.Setup(s => s.GetFileAsync(postId))
            .ReturnsAsync(mockFile.Object);

        // Act
        var result = await _controller.DownloadFile(postId);

        // Assert
        Assert.IsType<FileStreamResult>(result);
    }
    #endregion

    #region Helper Methods Tests
    [Fact]
    public async Task GetPostIdByTitle_ExistingTitle_ReturnsPostId()
    {
        // Arrange
        var testTitle = "Test Post";
        var expectedId = Guid.NewGuid();

        _mockPostService.Setup(s => s.GetPostByTitleAsync(testTitle))
            .ReturnsAsync(new Post { Id = expectedId });

        // Since GetPostIdByTitle is private/protected, test it indirectly through a public method
        // For example, using the Subscribe method which likely uses GetPostIdByTitle internally
        _mockPostService.Setup(s => s.SubscribeUserAsync(It.IsAny<string>(), testTitle))
            .ReturnsAsync("Success");

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.Name, "user@example.com")
        }, "test"));
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

        // Act
        var result = await _controller.Subscribe(testTitle);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(expectedId, redirectResult.RouteValues!["id"]);
    }
    #endregion

    #region Sorting Tests
    [Fact]
    public async Task Index_SortsByTitleAscending_ReturnsCorrectOrder()
    {   
        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Name, "test@example.com")
        };
        
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        // Arrange
        var posts = new List<Post>
        {
            new Post { Id = Guid.NewGuid(), Title = "B Post", CreatedAt = DateTime.UtcNow },
            new Post { Id = Guid.NewGuid(), Title = "A Post", CreatedAt = DateTime.UtcNow.AddHours(-1) }
        };

        _mockPostService.Setup(s => s.GetAllPosts())
            .ReturnsAsync(posts);
        _mockPostService.Setup(s => s.GetRateForPostAsync(It.IsAny<Guid>()))
            .ReturnsAsync(0);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };

        // Act
        var result = await _controller.Index("titlea");

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PostsListViewModel>(viewResult.Model);
        Assert.Equal("A Post", model.Posts[0].Title);
        Assert.Equal("B Post", model.Posts[1].Title);
    }

    [Fact]
    public async Task Index_SortsByNewest_ReturnsCorrectOrder()
    {

        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Name, "test@example.com")
        };
        
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        // Arrange
        var posts = new List<Post>
        {
            new Post { Id = Guid.NewGuid(), Title = "Older", CreatedAt = DateTime.UtcNow.AddHours(-24) },
            new Post { Id = Guid.NewGuid(), Title = "Newer", CreatedAt = DateTime.UtcNow }
        };

        _mockPostService.Setup(s => s.GetAllPosts())
            .ReturnsAsync(posts);
        _mockPostService.Setup(s => s.GetRateForPostAsync(It.IsAny<Guid>()))
            .ReturnsAsync(0);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };

        // Act
        var result = await _controller.Index("newest");

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PostsListViewModel>(viewResult.Model);
        Assert.Equal("Newer", model.Posts[0].Title);
        Assert.Equal("Older", model.Posts[1].Title);
    }
    #endregion

    #region User Actions Tests
    [Fact]
    public async Task GetUserByPost_ValidPost_ReturnsRedirect()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var post = new Post { Id = postId, UserId = userId };
        var user = new User { Id = userId, UserName = "TestUser" };

        _mockPostService.Setup(s => s.GetPostByIdAsync(postId))
            .ReturnsAsync(post);
        _mockUserService.Setup(s => s.GetUserByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.GetUserByPost(postId);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirectResult.ActionName);
        Assert.Equal("User", redirectResult.ControllerName);
        Assert.Equal(userId, redirectResult.RouteValues!["id"]);
    }

    [Fact]
    public async Task Unsubscribe_ValidRequest_ReturnsRedirect()
    {
        // Arrange
        var testTitle = "Test Post";
        var testEmail = "user@example.com";
        var testPostId = Guid.NewGuid();

        _mockPostService.Setup(s => s.UnsubscribeUserAsync(testEmail, testTitle))
            .ReturnsAsync("Success");
        _mockPostService.Setup(s => s.GetPostByTitleAsync(testTitle))
            .ReturnsAsync(new Post { Id = testPostId });

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.Name, testEmail)
        }, "test"));
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

        // Act
        var result = await _controller.Unsubscribe(testTitle);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(testPostId, redirectResult.RouteValues!["id"]);
    }
    #endregion

    #region File Handling Tests
    [Fact]
    public async Task DownloadFile_NonExistentFile_ReturnsNotFound()
    {
        // Arrange
        var postId = Guid.NewGuid();
        _mockPostService.Setup(s => s.GetFileAsync(postId))
            .ReturnsAsync((IFormFile?)null);

        // Act
        var result = await _controller.DownloadFile(postId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ViewImage_NonImagePost_ReturnsNotFound()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var post = new Post 
        { 
            Id = postId,
            FileContent = new byte[0],
            FileType = "application/pdf" // Not an image
        };
        
        _mockPostService.Setup(s => s.GetPostByIdAsync(postId))
            .ReturnsAsync(post);

        // Act
        var result = await _controller.ViewImage(postId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
    #endregion

    #region Delete Post Tests
    [Fact]
    public async Task Delete_ValidTitle_ReturnsRedirectToIndex()
    {
        // Arrange
        var testTitle = "Test Post";
        _mockPostService.Setup(s => s.DeletePostAsync(testTitle))
            .ReturnsAsync("Success");

        // Act
        var result = await _controller.Delete(testTitle);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }

    [Fact]
    public async Task Delete_InvalidTitle_ReturnsRedirectToIndex()
    {
        // Arrange
        var testTitle = "Non-existent Post";
        _mockPostService.Setup(s => s.DeletePostAsync(testTitle))
            .ReturnsAsync("Post not found");

        // Act
        var result = await _controller.Delete(testTitle);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        // In a real app, you might want to verify TempData contains an error message
    }
    #endregion

    #region Add Post Tests
    [Fact]
    public async Task Add_InvalidModelState_ReturnsViewWithModel()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testTopics = new List<Topic>
        {
            new Topic { Id = Guid.NewGuid(), Name = "Test Topic" }
        };

        _mockTopicService.Setup(x => x.GetAllTopicsAsync())
            .ReturnsAsync(testTopics);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "test"));
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

        var model = new AddPostViewModel
        {
            UserId = userId,
            Title = "", // Invalid - empty title
            Text = "Test content"
        };

        _controller.ModelState.AddModelError("Title", "Title is required");

        // Act
        var result = await _controller.Add(model, null);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var returnedModel = Assert.IsType<AddPostViewModel>(viewResult.Model);
        Assert.Equal(model.UserId, returnedModel.UserId);
        Assert.Equal(model.Text, returnedModel.Text);
        Assert.Single(returnedModel.AvailableTopics);
    }

    [Fact]
    public async Task Add_InvalidFileType_ReturnsViewWithError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testTopics = new List<Topic> { new Topic { Id = Guid.NewGuid(), Name = "Test" } };

        _mockTopicService.Setup(x => x.GetAllTopicsAsync()).ReturnsAsync(testTopics);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "test"));
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

        var model = new AddPostViewModel
        {
            UserId = userId,
            Title = "Valid Title",
            Text = "Valid Content",
            TopicId = testTopics[0].Id
        };

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.ContentType).Returns("application/exe"); // Invalid file type
        mockFile.Setup(f => f.Length).Returns(1024); // 1KB

        // Act
        var result = await _controller.Add(model, mockFile.Object);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(_controller.ModelState.IsValid);
        Assert.True(_controller.ModelState.ContainsKey("file"));
    }
    #endregion

}