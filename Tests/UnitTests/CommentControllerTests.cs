using Moq;
using Microsoft.AspNetCore.Mvc;
using MVC.Controllers;
using Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Core.Entities;
using MVC.Models;
using FluentAssertions;

namespace Tests.UnitTests;

public class CommentControllerTests
{
    private readonly Mock<IPostService> _mockPostService;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly CommentController _commentController;

    public CommentControllerTests()
    {
        _mockPostService = new Mock<IPostService>();
        _mockNotificationService = new Mock<INotificationService>();
        _commentController = new CommentController(_mockPostService.Object, _mockNotificationService.Object);
    }

    [Fact]
    public async Task Add_WithValidData_RedirectsToPostDetailsAsync()
    {
        // Arrange
        var testText = "Test comment";
        var postId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var testFile = new Mock<IFormFile>().Object;

        _mockPostService.Setup(x => x.AddCommentAsync(testText, testFile, userId, postId))
            .ReturnsAsync("Success");

        _mockPostService.Setup(x => x.GetPostByIdAsync(postId))
            .ReturnsAsync(new Post { Id = postId, Title = "Test post" });

        _mockPostService.Setup(x => x.GetPostSubscribersAsync(postId))
            .ReturnsAsync(new List<User>());

        // Act
        var result = await _commentController.Add(testText, testFile, postId, userId);

        // Assert
        var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
        redirectToActionResult.ActionName.Should().Be("Details");
        redirectToActionResult.ControllerName.Should().Be("Post");
        redirectToActionResult.RouteValues!["id"].Should().Be(postId);
    }

    [Fact]
    public async Task Add_WithEmptyText_RedirectsToPostDetailsAsync()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        var result = await _commentController.Add("", null, postId, userId);

        // Assert
        var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
        redirectToActionResult.ActionName.Should().Be("Details");
        redirectToActionResult.ControllerName.Should().Be("Post");
        redirectToActionResult.RouteValues!["id"].Should().Be(postId);

        _mockPostService.Verify(x => x.AddCommentAsync(It.IsAny<string>(), It.IsAny<IFormFile>(), It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Delete_ValidCommentId_RedirectsToPostDetails()
    {
        // Arrange
        var commentId = Guid.NewGuid();
        var postId = Guid.NewGuid();

        _mockPostService.Setup(x => x.DeleteCommentAsync(commentId))
            .ReturnsAsync("Success");

        // Act
        var result = await _commentController.Delete(commentId, postId);

        // Assert
        var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
        redirectToActionResult.ActionName.Should().Be("Details");
        redirectToActionResult.ControllerName.Should().Be("Post");
        redirectToActionResult.RouteValues!["id"].Should().Be(postId);
    }

    [Fact]
    public async Task Delete_FailedDeletion_RedirectsToPostDetailsWithError()
    {
        // Arrange
        var commentId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var errorMessage = "Error deletign comment";

        _mockPostService.Setup(x => x.DeleteCommentAsync(commentId))
            .ReturnsAsync(errorMessage);

        // Act
        var result = await _commentController.Delete(commentId, postId);

        // Assert
        var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
        redirectToActionResult.ActionName.Should().Be("Details");
        redirectToActionResult.ControllerName.Should().Be("Post");
        redirectToActionResult.RouteValues!["id"].Should().Be(postId);
    }

    [Fact]
    public async Task Vote_ValidVote_ReturnsJsonSuccess()
    {
        // Arrange
        var model = new CommentVoteModel
        {
            CommentId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            VoteType = "up"
        };

        _mockPostService
            .Setup(s => s.VoteAsync(model.CommentId, model.UserId, model.VoteType))
            .ReturnsAsync(("Success", 1));

        _commentController.ModelState.Clear();

        // Act
        var result = await _commentController.Vote(model);

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var type = jsonResult.Value.GetType();
        var successProp = type.GetProperty("success");
        var newScoreProp = type.GetProperty("newScore");
        Assert.True((bool)successProp!.GetValue(jsonResult.Value)!);
        Assert.Equal(1, (int)newScoreProp!.GetValue(jsonResult.Value)!);
    }

    [Fact]
    public async Task Vote_FailedVote_ReturnsJsonFailure()
    {
        // Arrange
        var model = new CommentVoteModel
        {
            CommentId = Guid.Empty,
            UserId = Guid.NewGuid(),
            VoteType = "up"
        };

        _mockPostService
            .Setup(s => s.VoteAsync(model.CommentId, model.UserId, model.VoteType))
            .ReturnsAsync(("Comment not found", 0));

        // Act
        var result = await _commentController.Vote(model);

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);

        var type = jsonResult.Value.GetType();
        var successProp = type.GetProperty("success");
        var messageProp = type.GetProperty("message");
        Assert.False((bool)successProp!.GetValue(jsonResult.Value)!);
        Assert.Equal("Comment not found", (string)messageProp!.GetValue(jsonResult.Value)!);
    }
}
