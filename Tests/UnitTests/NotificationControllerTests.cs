using Moq;
using Microsoft.AspNetCore.Mvc;
using MVC.Controllers;
using Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Core.Entities;
using MVC.Models;
using FluentAssertions;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Tests.UnitTests;

public class NotificationControllerTests
{
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<IPostService> _mockPostService;
    private readonly Mock<ITopicService> _mockTopicService;
    private readonly NotificationController _notificationController;

    public NotificationControllerTests()
    {
        _mockNotificationService = new Mock<INotificationService>();
        _mockPostService = new Mock<IPostService>();
        _mockTopicService = new Mock<ITopicService>();
        
        _notificationController = new NotificationController(
            _mockNotificationService.Object,
            _mockPostService.Object,
            _mockTopicService.Object);
    }

    [Fact]
    public async Task Index_Get_AuthenticatedUser_ReturnsViewWithModel()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notifications = new List<Notification>
        {
            new Notification { Id = Guid.NewGuid(), Message = "Test 1", IsRead = false },
            new Notification { Id = Guid.NewGuid(), Message = "Test 2", IsRead = true }
        };
        
        _mockNotificationService.Setup(x => x.GetNotificationsForUserAsync(userId))
            .ReturnsAsync(notifications);
        _mockNotificationService.Setup(x => x.GetUnreadCountAsync(userId))
            .ReturnsAsync(1);

        // Setup authenticated user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        _notificationController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        // Act
        var result = await _notificationController.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<NotificationsViewModel>(viewResult.Model);
        model.Notifications.Should().HaveCount(2);
        model.UnreadCount.Should().Be(1);
    }

    [Fact]
    public async Task Index_Get_UnauthenticatedUser_ReturnsChallenge()
    {
        // Arrange - no user claims
        _notificationController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        // Act
        var result = await _notificationController.Index();

        // Assert
        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task MarkAsRead_Post_ValidId_ReturnsRedirect()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        _mockNotificationService.Setup(x => x.MarkAsReadAsync(notificationId))
            .ReturnsAsync("Success");

        // Act
        var result = await _notificationController.MarkAsRead(notificationId);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        redirectResult.ActionName.Should().Be("Index");
    }

    [Fact]
    public async Task MarkAsRead_Post_WithReturnUrl_ReturnsRedirectToUrl()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        var returnUrl = "/some/url";
        _mockNotificationService.Setup(x => x.MarkAsReadAsync(notificationId))
            .ReturnsAsync("Success");

        // Act
        var result = await _notificationController.MarkAsRead(notificationId, returnUrl);

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        redirectResult.Url.Should().Be(returnUrl);
    }

    [Fact]
    public async Task MarkAsRead_Post_Failure_SetsTempData()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        _mockNotificationService.Setup(x => x.MarkAsReadAsync(notificationId))
            .ReturnsAsync("Failed to mark as read");

        // Mock TempData
        _notificationController.TempData = new TempDataDictionary(
            new DefaultHttpContext(), 
            Mock.Of<ITempDataProvider>());

        // Act
        var result = await _notificationController.MarkAsRead(notificationId);

        // Assert
        Assert.IsType<RedirectToActionResult>(result);
        _notificationController.TempData["ErrorMessage"].Should().Be("Failed to mark as read");
    }

    [Fact]
    public async Task MarkAllAsRead_Post_ValidUser_ReturnsRedirect()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockNotificationService.Setup(x => x.MarkAllAsReadAsync(userId))
            .ReturnsAsync("Success");

        // Setup authenticated user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        _notificationController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        // Act
        var result = await _notificationController.MarkAllAsRead();

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        redirectResult.ActionName.Should().Be("Index");
    }

    [Fact]
    public async Task MarkAllAsRead_Post_Failure_SetsTempData()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockNotificationService.Setup(x => x.MarkAllAsReadAsync(userId))
            .ReturnsAsync("Failed to mark all as read");

        // Mock TempData
        _notificationController.TempData = new TempDataDictionary(
            new DefaultHttpContext(), 
            Mock.Of<ITempDataProvider>());

        // Setup authenticated user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        _notificationController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        // Act
        var result = await _notificationController.MarkAllAsRead();

        // Assert
        Assert.IsType<RedirectToActionResult>(result);
        _notificationController.TempData["ErrorMessage"].Should().Be("Failed to mark all as read");
    }

    [Fact]
    public async Task Delete_Post_ValidId_ReturnsRedirect()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        _mockNotificationService.Setup(x => x.DeleteAsync(notificationId))
            .ReturnsAsync("Success");

        // Act
        var result = await _notificationController.Delete(notificationId);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        redirectResult.ActionName.Should().Be("Index");
    }

    [Fact]
    public async Task Delete_Post_Failure_SetsTempData()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        _mockNotificationService.Setup(x => x.DeleteAsync(notificationId))
            .ReturnsAsync("Failed to delete");

        // Mock TempData
        _notificationController.TempData = new TempDataDictionary(
            new DefaultHttpContext(), 
            Mock.Of<ITempDataProvider>());

        // Act
        var result = await _notificationController.Delete(notificationId);

        // Assert
        Assert.IsType<RedirectToActionResult>(result);
        _notificationController.TempData["ErrorMessage"].Should().Be("Failed to delete");
    }

    [Fact]
    public async Task GetUnreadCount_Get_AuthenticatedUser_ReturnsJson()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var unreadCount = 5;
        
        _mockNotificationService.Setup(x => x.GetUnreadCountAsync(userId))
            .ReturnsAsync(unreadCount);

        // Setup authenticated user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        _notificationController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        // Act
        var result = await _notificationController.GetUnreadCount();

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        var value = jsonResult.Value as dynamic;
        Assert.Equal(unreadCount, value!.GetType().GetProperty("count").GetValue(value, null));
    }

    [Fact]
    public async Task GetUnreadCount_Get_UnauthenticatedUser_ReturnsZeroCount()
    {
        // Arrange - no user claims
        _notificationController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        // Act
        var result = await _notificationController.GetUnreadCount();

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        var value = jsonResult.Value as dynamic;
        Assert.Equal(0, value!.GetType().GetProperty("count").GetValue(value, null));
    }

    [Fact]
    public async Task NavigateToPost_Post_ValidIds_ReturnsRedirectToPost()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        
        _mockNotificationService.Setup(x => x.MarkAsReadAsync(notificationId))
            .ReturnsAsync("Success");

        // Act
        var result = await _notificationController.NavigateToPost(notificationId, postId);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        redirectResult.ActionName.Should().Be("Details");
        redirectResult.ControllerName.Should().Be("Post");
        redirectResult.RouteValues!["id"].Should().Be(postId);
    }
}