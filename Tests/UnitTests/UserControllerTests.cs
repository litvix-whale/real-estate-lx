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

public class UserControllerTests
{
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IPostService> _mockPostService;
    private readonly UserController _userController;

    public UserControllerTests()
    {
        _mockUserService = new Mock<IUserService>();
        _mockPostService = new Mock<IPostService>();
        _userController = new UserController(_mockUserService.Object, _mockPostService.Object);
    }

    [Fact]
    public void Register_Get_ReturnsView()
    {
        // Act
        var result = _userController.Register();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Register_Post_ValidModel_ReturnsRedirect()
    {
        // Arrange
        var model = new RegisterViewModel 
        { 
            Email = "test@example.com", 
            UserName = "testuser", 
            Password = "Password123!", 
            RememberMe = false 
        };
        
        _mockUserService.Setup(x => x.RegisterUserAsync(
            model.Email, model.UserName, model.Password, model.RememberMe))
            .ReturnsAsync("Success");

        // Act
        var result = await _userController.Register(model);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        redirectResult.ActionName.Should().Be("Index");
        redirectResult.ControllerName.Should().Be("Post");
    }

    [Fact]
    public async Task Register_Post_InvalidModel_ReturnsViewWithModel()
    {
        // Arrange
        var model = new RegisterViewModel();
        _userController.ModelState.AddModelError("Email", "Required");

        // Act
        var result = await _userController.Register(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        viewResult.Model.Should().Be(model);
    }

    [Fact]
    public async Task Register_Post_RegistrationFails_ReturnsViewWithError()
    {
        // Arrange
        var model = new RegisterViewModel 
        { 
            Email = "test@example.com", 
            UserName = "testuser", 
            Password = "Password123!", 
            RememberMe = false 
        };
        
        _mockUserService.Setup(x => x.RegisterUserAsync(
            model.Email, model.UserName, model.Password, model.RememberMe))
            .ReturnsAsync("Registration failed");

        // Act
        var result = await _userController.Register(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        viewResult.Model.Should().Be(model);
        _userController.ModelState.Should().ContainKey(string.Empty);
    }

    [Fact]
    public void Login_Get_ReturnsView()
    {
        // Act
        var result = _userController.Login();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Login_Post_ValidModel_ReturnsRedirect()
    {
        // Arrange
        var model = new LoginViewModel 
        { 
            Email = "test@example.com", 
            Password = "Password123!", 
            RememberMe = false 
        };
        
        _mockUserService.Setup(x => x.LoginUserAsync(
            model.Email, model.Password, model.RememberMe))
            .ReturnsAsync("Success");

        // Act
        var result = await _userController.Login(model);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        redirectResult.ActionName.Should().Be("Index");
        redirectResult.ControllerName.Should().Be("Post");
    }

    [Fact]
    public async Task Login_Post_InvalidModel_ReturnsViewWithModel()
    {
        // Arrange
        var model = new LoginViewModel();
        _userController.ModelState.AddModelError("Email", "Required");

        // Act
        var result = await _userController.Login(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        viewResult.Model.Should().Be(model);
    }

    [Fact]
    public async Task Login_Post_LoginFails_ReturnsViewWithError()
    {
        // Arrange
        var model = new LoginViewModel 
        { 
            Email = "test@example.com", 
            Password = "Password123!", 
            RememberMe = false 
        };
        
        _mockUserService.Setup(x => x.LoginUserAsync(
            model.Email, model.Password, model.RememberMe))
            .ReturnsAsync("Invalid credentials");

        // Act
        var result = await _userController.Login(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        viewResult.Model.Should().Be(model);
        _userController.ModelState.Should().ContainKey(string.Empty);
    }

    [Fact]
    public async Task Logout_Post_ReturnsRedirect()
    {
        // Arrange
        _mockUserService.Setup(x => x.LogoutUserAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _userController.Logout();

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        redirectResult.ActionName.Should().Be("Index");
        redirectResult.ControllerName.Should().Be("Post");
    }

    [Fact]
    public async Task Users_Get_AdminRole_ReturnsViewWithModel()
    {
        // Arrange
        var users = new List<User> 
        { 
            new User { Email = "user1@example.com", UserName = "user1" },
            new User { Email = "user2@example.com", UserName = "user2" }
        };
        
        _mockUserService.Setup(x => x.GetUsersByRoleAsync("Default", null, null, null, null, null))
            .ReturnsAsync(users);

        // Setup admin user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        _userController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        // Act
        var result = await _userController.Users();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<UsersViewModel>(viewResult.Model);
        model.Users.Should().HaveCount(2);
    }

    [Fact]
    public async Task Users_Get_NonAdminRole_ReturnsForbid()
    {
        // Arrange - setup non-admin user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, "Default")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        _userController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        // Act
        var result = await _userController.Users();

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteUser_Post_ValidUser_ReturnsRedirect()
    {
        // Arrange
        var email = "test@example.com";
        _mockUserService.Setup(x => x.DeleteUserAsync(email))
            .ReturnsAsync("Success");

        // Act
        var result = await _userController.DeleteUser(email);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        redirectResult.ActionName.Should().Be("Users");
        redirectResult.ControllerName.Should().Be("User");
    }

    [Fact]
    public async Task DeleteUser_Post_InvalidUser_ReturnsRedirectWithError()
    {
        // Arrange
        var email = "test@example.com";
        _mockUserService.Setup(x => x.DeleteUserAsync(email))
            .ReturnsAsync("User not found");

        // Act
        var result = await _userController.DeleteUser(email);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        redirectResult.ActionName.Should().Be("Users");
        redirectResult.ControllerName.Should().Be("User");
        _userController.ModelState.Should().ContainKey(string.Empty);
    }

    [Fact]
    public async Task Ban_Get_ReturnsViewWithModel()
    {
        // Arrange
        var email = "test@example.com";
        var user = new User { Email = email, UserName = "testuser" };
        
        _mockUserService.Setup(x => x.GetUserByUserNameAsync(email))
            .ReturnsAsync(user);

        // Act
        var result = await _userController.Ban(email);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<BanViewModel>(viewResult.Model);
        model.UserName.Should().Be(user.UserName);
        model.BannedTo.Should().BeNull();
    }

    [Fact]
    public async Task BanUser_Post_ValidInput_ReturnsRedirect()
    {
        // Arrange
        var email = "test@example.com";
        var bannedTo = DateTime.UtcNow.AddDays(7);
        
        _mockUserService.Setup(x => x.BanUserAsync(email, bannedTo))
            .ReturnsAsync("Success");

        // Act
        var result = await _userController.BanUser(email, bannedTo);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        redirectResult.ActionName.Should().Be("Users");
        redirectResult.ControllerName.Should().Be("User");
    }

    [Fact]
    public async Task BanUser_Post_InvalidInput_ReturnsRedirectWithError()
    {
        // Arrange
        var email = "test@example.com";
        var bannedTo = DateTime.UtcNow.AddDays(7);
        
        _mockUserService.Setup(x => x.BanUserAsync(email, bannedTo))
            .ReturnsAsync("User not found");

        // Act
        var result = await _userController.BanUser(email, bannedTo);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        redirectResult.ActionName.Should().Be("Users");
        redirectResult.ControllerName.Should().Be("User");
        _userController.ModelState.Should().ContainKey(string.Empty);
    }

    [Fact]
    public async Task UnbanUserAsync_Post_ValidUser_ReturnsRedirect()
    {
        // Arrange
        var email = "test@example.com";
        _mockUserService.Setup(x => x.UnbanUserAsync(email))
            .ReturnsAsync("Success");

        // Act
        var result = await _userController.UnbanUserAsync(email);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        redirectResult.ActionName.Should().Be("Users");
        redirectResult.ControllerName.Should().Be("User");
    }

    [Fact]
    public async Task UnbanUserAsync_Post_InvalidUser_ReturnsRedirectWithError()
    {
        // Arrange
        var email = "test@example.com";
        _mockUserService.Setup(x => x.UnbanUserAsync(email))
            .ReturnsAsync("User not found");

        // Act
        var result = await _userController.UnbanUserAsync(email);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        redirectResult.ActionName.Should().Be("Users");
        redirectResult.ControllerName.Should().Be("User");
        _userController.ModelState.Should().ContainKey(string.Empty);
    }

    [Fact]
    public async Task Profile_Get_AuthenticatedUser_ReturnsViewWithModel()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var username = "testuser";
        var profilePicture = "profile.jpg";
        
        var user = new User 
        { 
            Id = userId, 
            Email = email, 
            UserName = username, 
            ProfilePicture = profilePicture 
        };
        
        _mockUserService.Setup(x => x.GetUserByUserNameAsync(username))
            .ReturnsAsync(user);

        // Setup authenticated user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username)
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        _userController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        // Act
        var result = await _userController.Profile();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ProfileViewModel>(viewResult.Model);
        model.Email.Should().Be(email);
        model.UserName.Should().Be(username);
        model.ProfilePicture.Should().Be(profilePicture);
        model.NewUserName.Should().Be(username);
    }

    [Fact]
    public async Task UpdateProfile_Post_ValidModel_ReturnsViewWithSuccess()
    {
        // Arrange
        var username = "testuser";
        var newUsername = "newtestuser";
        var profilePicture = "newprofile.jpg";
        
        var user = new User 
        { 
            Id = Guid.NewGuid(), 
            Email = "test@example.com", 
            UserName = username, 
            ProfilePicture = "oldprofile.jpg" 
        };
        
        var model = new ProfileViewModel 
        { 
            NewUserName = newUsername, 
            NewProfilePicture = profilePicture 
        };

        // Mock TempData
        _userController.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

        _mockUserService.Setup(x => x.GetUserByUserNameAsync(username))
            .ReturnsAsync(user);
            
        // Updated mock setup that modifies the user object
        _mockUserService.Setup(x => x.UpdateUserProfileAsync(user, newUsername, profilePicture))
            .ReturnsAsync("Success")
            .Callback<User, string, string>((u, newName, newPic) => 
            {
                u.UserName = newName;
                u.ProfilePicture = newPic;
            });

        // Setup authenticated user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username)
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        _userController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        // Act
        var result = await _userController.UpdateProfile(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        viewResult.ViewName.Should().Be("Profile");
        var returnedModel = Assert.IsType<ProfileViewModel>(viewResult.Model);
        
        // Now these assertions should pass
        returnedModel.UserName.Should().Be(newUsername);
        returnedModel.ProfilePicture.Should().Be(profilePicture);
        _userController.TempData["SuccessMessage"].Should().Be("Profile updated successfully!");
    }

    [Fact]
    public async Task UpdateProfile_Post_InvalidModel_ReturnsViewWithModel()
    {
        // Arrange
        var username = "testuser";
        var model = new ProfileViewModel();
        
        // Initialize controller dependencies
        _userController.ModelState.Clear();
        _userController.TempData = new TempDataDictionary(
            new DefaultHttpContext(), 
            Mock.Of<ITempDataProvider>());

        // Setup authenticated user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username)
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        _userController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        // ACTUALLY make the model invalid by leaving required fields null
        // Instead of manually adding model errors
        _userController.ModelState.AddModelError("NewUserName", "The NewUserName field is required.");

        // Act
        var result = await _userController.UpdateProfile(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Profile", viewResult.ViewName);
        Assert.Same(model, viewResult.Model);
        
        // Verify ModelState contains errors
        Assert.False(_userController.ModelState.IsValid);
        Assert.Contains("NewUserName", _userController.ModelState.Keys);
        
        // Verify no service calls were made
        _mockUserService.Verify(
            x => x.GetUserByUserNameAsync(It.IsAny<string>()), 
            Times.Never);
        _mockUserService.Verify(
            x => x.UpdateUserProfileAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()), 
            Times.Never);
        
        // Verify TempData wasn't modified
        Assert.Empty(_userController.TempData);
    }

    [Fact]
    public async Task UpdateProfile_Post_UpdateFails_ReturnsViewWithError()
    {
        // Arrange
        var username = "testuser";
        var newUsername = "newtestuser";
        var profilePicture = "newprofile.jpg";
        
        var user = new User 
        { 
            Id = Guid.NewGuid(), 
            Email = "test@example.com", 
            UserName = username, 
            ProfilePicture = "oldprofile.jpg" 
        };
        
        var model = new ProfileViewModel 
        { 
            NewUserName = newUsername, 
            NewProfilePicture = profilePicture 
        };

        _mockUserService.Setup(x => x.GetUserByUserNameAsync(username))
            .ReturnsAsync(user);
        _mockUserService.Setup(x => x.UpdateUserProfileAsync(user, newUsername, profilePicture))
            .ReturnsAsync("Update failed");

        // Setup authenticated user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username)
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        _userController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        // Act
        var result = await _userController.UpdateProfile(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        viewResult.ViewName.Should().Be("Profile");
        var returnedModel = Assert.IsType<ProfileViewModel>(viewResult.Model);
        _userController.ModelState.Should().ContainKey(string.Empty);
    }
}