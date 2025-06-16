using Moq;
using Microsoft.AspNetCore.Mvc;
using MVC.Controllers;
using Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Core.Entities;
using MVC.Models;
using FluentAssertions;

namespace Tests.UnitTests;

public class CategoryControllerTests
{
    private readonly Mock<ICategoryService> _mockCategoryService;
    private readonly CategoryController _categoryController;

    public CategoryControllerTests()
    {
        _mockCategoryService = new Mock<ICategoryService>();
        _categoryController = new CategoryController(_mockCategoryService.Object);

        _categoryController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = TestHelper.CreateClaimsPrincipal(Guid.NewGuid(), "Admin") }
        };
    }

    [Fact]
    public async Task Index_ReturnsViewWithCategories()
    {
        // Arrange
        var categories = new List<Category>
        {
            new Category { Id = Guid.NewGuid(), Name = "Category 1" },
            new Category { Id = Guid.NewGuid(), Name = "Category 2" }
        };

        // Change this to mock GetCategories instead of GetAllCategoriesAsync
        _mockCategoryService.Setup(x => x.GetCategories(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(categories);

        // Act
        var result = await _categoryController.Index(1, 7, string.Empty);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<CategoriesListViewModel>(viewResult.Model);

        model.Categories.Should().HaveCount(2);
    }
    
    [Fact]
    public void Add_Get_ReturnsView()
    {
        // Act
        var result = _categoryController.Add();

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Delete_Get_ReturnsView()
    {
        // Act
        var result = _categoryController.Delete();

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Add_Post_ValidInput_RedirectsToIndex()
    {
        // Arrange
        var name = "Category 1";

        _mockCategoryService.Setup(x => x.CreateCategoryAsync(name))
            .ReturnsAsync("Success");

        // Act
        var result = await _categoryController.Add(name);

        // Assert
        var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
        redirectToActionResult.ActionName.Should().Be("Index");
        redirectToActionResult.ControllerName.Should().Be("Category");
    }

    [Fact]
    public async Task Add_Post_InvalidInput_ReturnsViewWithError()
    {
        // Arrange
        var name = "Invcalid category";

        _mockCategoryService.Setup(x => x.CreateCategoryAsync(name))
            .ReturnsAsync("Category already exists");

        _categoryController.ModelState.AddModelError("Name", "Invalid name");

        // Act
        var result = await _categoryController.Add(name);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        viewResult.ViewData.ModelState.ErrorCount.Should().Be(1);
        Assert.Null(viewResult.Model);
        Assert.False(viewResult.ViewData.ModelState.IsValid);
    }

    [Fact]
    public async Task Delete_Post_ValidInput_RedirectsToIndex()
    {
        // Arrange
        var name = "Old category";

        _mockCategoryService.Setup(x => x.DeleteCategoryAsync(name))
            .ReturnsAsync("Success");

        // Act
        var result = await _categoryController.Delete(name);

        // Assert
        var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
        redirectToActionResult.ActionName.Should().Be("Index");
        redirectToActionResult.ControllerName.Should().Be("Category");
    }

    [Fact]
    public async Task Delete_Post_InvalidInput_ReturnsViewWithError()
    {
        // Arrange
        var name = "Invalid category";

        _mockCategoryService.Setup(x => x.DeleteCategoryAsync(name))
            .ReturnsAsync("Category not found");

        _categoryController.ModelState.AddModelError("Name", "Invalid name");

        // Act
        var result = await _categoryController.Delete(name);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        viewResult.ViewData.ModelState.ErrorCount.Should().Be(1);
        Assert.Null(viewResult.Model);
        Assert.False(viewResult.ViewData.ModelState.IsValid);
    }
}