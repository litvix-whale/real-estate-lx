using Moq;
using Microsoft.AspNetCore.Mvc;
using MVC.Controllers;
using Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Core.Entities;
using MVC.Models;
using FluentAssertions;
using System.Security.Claims;

namespace Tests.UnitTests;

public class PostCommentControllerTests
{
    private readonly Mock<IPostService> _mockPostService;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<ITopicService> _mockTopicService;
    private readonly Mock<IPostVoteRepository> _mockPostVoteRepository;
    private readonly Mock<IUserTitleRepository> _mockUserTitleRepository;
    private readonly PostController _postController;

    public PostCommentControllerTests()
    {
        _mockPostService = new Mock<IPostService>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockUserService = new Mock<IUserService>();
        _mockTopicService = new Mock<ITopicService>();
        _mockPostVoteRepository = new Mock<IPostVoteRepository>();
        _mockUserTitleRepository = new Mock<IUserTitleRepository>();
        _postController = new PostController(_mockPostService.Object, _mockUserService.Object, _mockTopicService.Object, _mockNotificationService.Object, _mockPostVoteRepository.Object, _mockUserTitleRepository.Object);

        _postController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = TestHelper.CreateClaimsPrincipal(Guid.NewGuid()) }
        };
    }

    [Fact]
public async Task Details_PostExists_ReturnsViewModel()
{
    // Arrange
    var userId = Guid.NewGuid();
    var postId = Guid.NewGuid();
    var commentUserId = Guid.NewGuid();
    var post = new Post { Id = postId, Title = "Test post", UserId = commentUserId };
    var comments = new List<Comment> { new Comment { Id = Guid.NewGuid(), PostId = postId, UserId = commentUserId } };
    
    // Create claims principal with required claims
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
        new Claim(ClaimTypes.Email, "test@example.com"),
        new Claim(ClaimTypes.Name, "test@example.com")
    };
    var identity = new ClaimsIdentity(claims, "Test");
    var principal = new ClaimsPrincipal(identity);
    
    // Update controller context to use the principal
    _postController.ControllerContext = new ControllerContext
    {
        HttpContext = new DefaultHttpContext { User = principal }
    };
    
    // Mock the GetVoteByUser method
    _mockPostVoteRepository.Setup(x => x.GetVoteByUser(postId, userId))
        .ReturnsAsync(new PostVote { RateType = "upvote" });
    
    // Mock IsUserSubscribedAsync
    _mockPostService.Setup(x => x.IsUserSubscribedAsync("test@example.com", post.Title))
        .ReturnsAsync(false);
    
    // Other mocks
    _mockPostService.Setup(x => x.GetPostByIdAsync(postId))
        .ReturnsAsync(post);
    _mockPostService.Setup(x => x.GetAllCommentsAsync(postId))
        .ReturnsAsync(comments);
    _mockUserService.Setup(x => x.GetUserByIdAsync(commentUserId))
        .ReturnsAsync(new User { UserName = "TestUser", ProfilePicture = "pfp_1.png" });
    _mockUserService.Setup(x => x.GetUserByIdAsync(userId))
        .ReturnsAsync(new User { UserName = "CurrentUser", ProfilePicture = "pfp_2.png" });
    _mockPostService.Setup(x => x.GetUserVotesForCommentsAsync(It.IsAny<Guid>(), It.IsAny<List<Guid>>()))
        .ReturnsAsync(new Dictionary<Guid, string>());
    _mockUserService.Setup(x => x.GetBannedTo(userId))
        .Returns((string?)null);
        
    // Act
    var result = await _postController.Details(postId);
    
    // Assert
    var viewResult = Assert.IsType<ViewResult>(result);
    var model = Assert.IsType<PostDetailsViewModel>(viewResult.Model);
    Assert.Equal(post.Id, model.Post.Id);
    Assert.Single(model.Comments);
    Assert.Equal("TestUser", model.UserNames[comments.First().UserId]);
    Assert.Equal("pfp_1.png", model.UserProfilePictures[comments.First().UserId]);
    Assert.Empty(model.UserVotes);
    Assert.Equal("upvote", model.UserPostVote);
    Assert.Null(_postController.ViewBag.BannedTo);
    Assert.False((bool)(_postController.ViewBag.IsSubscribed ?? false));
}

    [Fact]
    public async Task Details_PostDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var postId = Guid.NewGuid();

        _mockPostService.Setup(x => x.GetPostByIdAsync(postId))
            .ReturnsAsync((Post)null!);

        // Act
        var result = await _postController.Details(postId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Details_AuthenticatedUser_SetsViewBagValues()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var post = new Post { Id = postId, Title = "Test post" };
        var comments = new List<Comment> { new Comment { Id = Guid.NewGuid(), PostId = postId, UserId = Guid.NewGuid() } };

        _mockPostService.Setup(x => x.GetPostByIdAsync(postId))
            .ReturnsAsync(post);
        _mockPostService.Setup(x => x.GetAllCommentsAsync(postId))
            .ReturnsAsync(comments);
        _mockUserService.Setup(x => x.GetUserByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new User { UserName = "TestUser", ProfilePicture = "pfp_1.png" });
        _mockUserService.Setup(x => x.GetBannedTo(userId))
            .Returns(DateTime.UtcNow.AddDays(1).ToString("O"));
        _mockPostService.Setup(x => x.GetUserVotesForCommentsAsync(userId, It.IsAny<List<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, string> { { comments.First().Id, "up" } });
        _mockPostService.Setup(x => x.IsUserSubscribedAsync("test@example.com", post.Title))
            .ReturnsAsync(true);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Role, "User")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        _postController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        // Act
        var result = await _postController.Details(postId);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PostDetailsViewModel>(viewResult.Model);

        Assert.Equal(post.Id, model.Post.Id);
        Assert.Single(model.Comments);
        Assert.Equal("TestUser", model.UserNames[comments.First().UserId]);
        Assert.Equal("up", model.UserVotes[comments.First().Id]);
        Assert.Equal("", model.UserPostVote);
        Assert.NotNull(_postController.ViewBag.BannedTo);
        Assert.IsType<string>(_postController.ViewBag.BannedTo);
        Assert.True((bool)_postController.ViewBag.IsSubscribed);
    }

    [Fact]
    public async Task Details_NoComments_ReturnsEmptyCommentsList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var post = new Post { Id = postId, Title = "Test post", UserId = Guid.NewGuid() };
    
        // Create claims principal with required claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Name, "test@example.com")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
    
        // Update controller context to use the principal
        _postController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    
        // Mock the GetVoteByUser method
        _mockPostVoteRepository.Setup(x => x.GetVoteByUser(postId, userId))
            .ReturnsAsync((PostVote)null);
    
        // Mock IsUserSubscribedAsync
        _mockPostService.Setup(x => x.IsUserSubscribedAsync("test@example.com", post.Title))
            .ReturnsAsync(false);
    
        // Basic mocks
        _mockPostService.Setup(x => x.GetPostByIdAsync(postId))
            .ReturnsAsync(post);
        _mockPostService.Setup(x => x.GetAllCommentsAsync(postId))
            .ReturnsAsync(new List<Comment>());
        _mockPostService.Setup(x => x.GetUserVotesForCommentsAsync(It.IsAny<Guid>(), It.IsAny<List<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, string>());
        _mockUserService.Setup(x => x.GetBannedTo(userId))
            .Returns((string?)null);
        _mockUserService.Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync(new User { UserName = "CurrentUser", ProfilePicture = "pfp_2.png" });
        
        // Act
        var result = await _postController.Details(postId);
    
        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PostDetailsViewModel>(viewResult.Model);
        Assert.Equal(post.Id, model.Post.Id);
        Assert.Empty(model.Comments);
        Assert.Equal(model.UserNames, new Dictionary<Guid, string> { { post.UserId ?? Guid.Empty, "Unknown" } });
        Assert.Equal(model.UserProfilePictures, new Dictionary<Guid, string> { { post.UserId ?? Guid.Empty, "pfp_1.png" } });
    }

    [Fact]
    public async Task Details_UnauthenticatedUser_SetsDefaultViewBag()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var post = new Post { Id = postId, Title = "Test post" };

        _mockPostService.Setup(x => x.GetPostByIdAsync(postId))
            .ReturnsAsync(post);
        _mockPostService.Setup(x => x.GetAllCommentsAsync(postId))
            .ReturnsAsync(new List<Comment>());

        _postController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = null! }
        };

        // Act
        var result = await _postController.Details(postId);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);

        Assert.Null(_postController.ViewBag.BannedTo);
        Assert.Null(_postController.ViewBag.IsSubscribed);
    }
}