using Moq;
using Microsoft.AspNetCore.Mvc;
using MVC.Controllers;
using Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Core.Entities;
using MVC.Models;
using FluentAssertions;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Diagnostics;
using Json.More;

namespace Tests.UnitTests;

public class TopicControllerTests
{
    private readonly Mock<ITopicService> _mockTopicService;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<ICategoryService> _mockCategoryService;
    private readonly Mock<ITopicCategoryRepository> _mockTopicCategoryRepository;
    private readonly Mock<IPostRepository> _mockPostRepository;
    private readonly TopicController _topicController;

    public TopicControllerTests()
    {
        _mockTopicService = new Mock<ITopicService>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockCategoryService = new Mock<ICategoryService>();
        _mockTopicCategoryRepository = new Mock<ITopicCategoryRepository>();
        _mockPostRepository = new Mock<IPostRepository>();
        _topicController = new TopicController(
            _mockTopicService.Object,
            _mockNotificationService.Object,
            _mockCategoryService.Object,
            _mockTopicCategoryRepository.Object,
            _mockPostRepository.Object
        );

        _topicController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = TestHelper.CreateClaimsPrincipal(Guid.NewGuid(), "Admin") }
        };
    }

    [Fact]
    public async Task Index_ReturnsViewWithTopics()
    {
        // Arrange
        var topics = new List<Topic>
        {
            new Topic { Id = Guid.NewGuid(), Name = "Topic 1" },
            new Topic { Id = Guid.NewGuid(), Name = "Topic 2" }
        };

        _mockTopicService.Setup(x => x.GetTopics(0, 7, ""))
            .ReturnsAsync(topics);

        var categories = new List<Category>
        {
            new Category { Id = Guid.NewGuid(), Name = "Category 1" },
            new Category { Id = Guid.NewGuid(), Name = "Category 2" }
        };

        _mockCategoryService.Setup(x => x.GetAllCategoriesAsync())
            .ReturnsAsync(categories);

        // Act
        var result = await _topicController.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<TopicsListViewModel>(viewResult.Model);

        model.Topics.Should().HaveCount(2);
    }

    [Fact]
    [Authorize(Roles = "Admin")]
    public void Add_Get_ReturnsView()
    {
        // Act
        var result = _topicController.Add();

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    [Authorize(Roles = "Admin")]
    public async Task Add_Post_ValidModel_RedirectsToIndex()
    {
        // Arrange
        var topicName = "New Topic";
        var categoryName = "Category 1";

        _mockTopicService.Setup(x => x.CreateTopicAsync(topicName, categoryName))
            .ReturnsAsync("Success");

        // Act
        var result = await _topicController.Add(topicName, categoryName);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        redirectResult.ActionName.Should().Be("Index");
    }

    [Fact]
    [Authorize(Roles = "Admin")]
    public async Task Add_Post_InvalidModel_ReturnsViewWithErrors()
    {
        // Arrange
        var topicName = "New Topic";
        var categoryName = "Category 1";

        _topicController.ModelState.AddModelError("Error", "Invalid model");

        // Act
        var result = await _topicController.Add(topicName, categoryName);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        viewResult.ViewData.ModelState.Should().ContainKey("Error");
    }

    [Fact]
    [Authorize(Roles = "Admin")]
    public void Delete_Get_ReturnsView()
    {
        // Act
        var result = _topicController.Delete();

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    [Authorize(Roles = "Admin")]
    public async Task Delete_Post_ValidModel_RedirectsToIndex()
    {
        // Arrange
        var topicName = "Topic 1";

        _mockTopicService.Setup(x => x.DeleteTopicAsync(topicName))
            .ReturnsAsync("Success");

        // Act
        var result = await _topicController.Delete(topicName);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        redirectResult.ActionName.Should().Be("Index");
    }

    [Fact]
    [Authorize(Roles = "Admin")]
    public async Task Delete_Post_InvalidModel_ReturnsViewWithErrors()
    {
        // Arrange
        var topicName = "Topic 1";

        _topicController.ModelState.AddModelError("Error", "Invalid model");

        // Act
        var result = await _topicController.Delete(topicName);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        viewResult.ViewData.ModelState.Should().ContainKey("Error");
    }

    [Fact]
    [Authorize]
    public async Task SubsribeToTopic_ValidModel_RedirectsToIndex()
    {
        // Arrange
        var topicName = "Topic 1";
        var userName = "User1";

        // set up user in controller context
        _topicController.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, userName)
        ]));

        _mockTopicService.Setup(x => x.SubscribeUserAsync(userName, topicName))
            .ReturnsAsync("Success");

        // Act
        var result = await _topicController.Subscribe(topicName);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        redirectResult.ActionName.Should().Be("Index");
    }

    [Fact]
    [Authorize]
    public async Task SubscribeToTopic_InvalidModel_ReturnsBadRequest()
    {
        // Arrange
        var topicName = "Topic 1";

        _topicController.ModelState.AddModelError("Error", "Failed to subscribe to the topic.");

        // Act
        var result = await _topicController.Subscribe(topicName);

        // Assert
        var viewResult = Assert.IsType<BadRequestObjectResult>(result);
        viewResult.Value.Should().Be("Failed to subscribe to the topic.");
    }

    [Fact]
    [Authorize]
    public async Task UnsubscribeFromTopic_ValidModel_RedirectsToIndex()
    {
        // Arrange
        var topicName = "Topic 1";
        var userName = "User1";

        // set up user in controller context
        _topicController.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, userName)
        ]));


        _mockTopicService.Setup(x => x.UnsubscribeUserAsync(userName, topicName))
            .ReturnsAsync("Success");

        // Act
        var result = await _topicController.Unsubscribe(topicName);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        redirectResult.ActionName.Should().Be("Index");
    }

    [Fact]
    [Authorize]
    public async Task UnsubscribeFromTopic_InvalidModel_ReturnsViewWithErrors()
    {
        // Arrange
        var topicName = "Topic 1";

        _topicController.ModelState.AddModelError("Error", "Failed to unsubscribe from the topic.");

        // Act
        var result = await _topicController.Unsubscribe(topicName);

        // Assert
        var viewResult = Assert.IsType<BadRequestObjectResult>(result);
        viewResult.Value.Should().Be("Failed to unsubscribe from the topic.");
    }

    [Fact]
    [Authorize]
    public async Task AskForTopic_ValidModel_RedirectsToIndex()
    {
        // Arrange
        var topicName = "Topic 1";
        var userName = "User1";

        // set up user in controller context
        _topicController.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, userName)
        ]));


        _mockNotificationService.Setup(x => x.AskAdminForTopicAsync(userName!, topicName))
            .ReturnsAsync("Success");

        // Act
        var result = await _topicController.AskForTopic(topicName);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        redirectResult.ActionName.Should().Be("Index");
    }

    [Fact]
    public async Task AskForTopic_InvalidModel_ReturnsViewWithErrors()
    {
        // Arrange
        var topicName = "Topic 1";

        _topicController.ModelState.AddModelError("Error", "Failed to ask for the topic.");

        // Act
        var result = await _topicController.AskForTopic(topicName);

        // Assert
        var viewResult = Assert.IsType<BadRequestObjectResult>(result);
        viewResult.Value.Should().Be("Failed to ask for the topic.");
    }

    [Fact]
    public async Task Posts_ReturnsViewWithPosts()
    {
        // Arrange
        var topicId = Guid.NewGuid();
        var posts = new List<Post>
        {
            new Post { Id = Guid.NewGuid(), Title = "Post 1", Text = "Content 1", CreatedAt = DateTime.UtcNow },
            new Post { Id = Guid.NewGuid(), Title = "Post 2", Text = "Content 2", CreatedAt = DateTime.UtcNow + TimeSpan.FromHours(1) }
        };

        _mockPostRepository.Setup(x => x.GetPostsByTopicAsync(topicId))
            .ReturnsAsync(posts);

        // Act
        var result = await _topicController.Posts(topicId);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PostsListViewModel>(viewResult.Model);

        model.Posts.Should().HaveCount(2);
    }
}
