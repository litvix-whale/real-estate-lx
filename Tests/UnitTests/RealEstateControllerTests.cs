using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MVC.Controllers;
using MVC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Core.Xml;
using FluentAssertions;

namespace Tests.UnitTests
{
    public class RealEstateControllerTests
    {
        private readonly Mock<IRealEstateService> _mockRealEstateService;
        private readonly Mock<IGoogleMapsService> _mockGoogleMapsService;
        private readonly Mock<IRealEstateImageRepository> _mockImageRepository;
        private readonly RealEstateController _controller;
        private readonly Guid _testUserId;

        public RealEstateControllerTests()
        {
            _mockRealEstateService = new Mock<IRealEstateService>();
            _mockGoogleMapsService = new Mock<IGoogleMapsService>();
            _mockImageRepository = new Mock<IRealEstateImageRepository>();
            _testUserId = Guid.NewGuid();

            _controller = new RealEstateController(
                _mockRealEstateService.Object,
                _mockGoogleMapsService.Object,
                _mockImageRepository.Object
            );

            var tempData = new Mock<ITempDataDictionary>();
            _controller.TempData = tempData.Object;

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = TestHelper.CreateClaimsPrincipal(_testUserId, "User")
                }
            };
        }

        #region Index Tests
        [Fact]
        public async Task Index_ReturnsViewWithSearchResults()
        {
            // Arrange
            var realEstates = new List<RealEstate>
            {
                new RealEstate { Id = Guid.NewGuid(), Title = "Test Property 1", UserId = _testUserId },
                new RealEstate { Id = Guid.NewGuid(), Title = "Test Property 2", UserId = _testUserId }
            };

            _mockRealEstateService.Setup(x => x.SearchRealEstateAsync(It.IsAny<RealEstateSearchCriteria>()))
                .ReturnsAsync(realEstates);
            _mockRealEstateService.Setup(x => x.GetSearchCountAsync(It.IsAny<RealEstateSearchCriteria>()))
                .ReturnsAsync(2);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<RealEstateSearchViewModel>(viewResult.Model);
            model.RealEstates.Should().HaveCount(2);
            model.TotalItems.Should().Be(2);
        }

        [Fact]
        public async Task Index_WithSearchQuery_ReturnsFilteredResults()
        {
            // Arrange
            var searchQuery = "Apartment";
            var realEstates = new List<RealEstate>
            {
                new RealEstate { Id = Guid.NewGuid(), Title = "Luxury Apartment", UserId = _testUserId }
            };

            _mockRealEstateService.Setup(x => x.SearchRealEstateAsync(It.Is<RealEstateSearchCriteria>(c => c.SearchQuery == searchQuery)))
                .ReturnsAsync(realEstates);
            _mockRealEstateService.Setup(x => x.GetSearchCountAsync(It.IsAny<RealEstateSearchCriteria>()))
                .ReturnsAsync(1);

            // Act
            var result = await _controller.Index(searchQuery);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<RealEstateSearchViewModel>(viewResult.Model);
            model.SearchQuery.Should().Be(searchQuery);
            model.RealEstates.Should().HaveCount(1);
        }
        #endregion

        #region Details Tests
        [Fact]
        public async Task Details_ValidId_ReturnsViewWithProperty()
        {
            // Arrange
            var propertyId = Guid.NewGuid();
            var property = new RealEstate
            {
                Id = propertyId,
                Title = "Test Property",
                UserId = _testUserId,
                Images = new List<RealEstateImage>()
            };

            _mockRealEstateService.Setup(x => x.GetRealEstateWithImagesAsync(propertyId))
                .ReturnsAsync(property);
            _mockGoogleMapsService.Setup(x => x.GetApiKey())
                .Returns("test-api-key");

            // Act
            var result = await _controller.Details(propertyId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<RealEstate>(viewResult.Model);
            model.Id.Should().Be(propertyId);
            model.Title.Should().Be("Test Property");
        }

        [Fact]
        public async Task Details_InvalidId_ReturnsNotFound()
        {
            // Arrange
            var propertyId = Guid.NewGuid();
            _mockRealEstateService.Setup(x => x.GetRealEstateWithImagesAsync(propertyId))
                .ReturnsAsync((RealEstate?)null);

            // Act
            var result = await _controller.Details(propertyId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
        #endregion

        #region Create Tests
        [Fact]
        public void Create_Get_ReturnsView()
        {
            // Act
            var result = _controller.Create();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<RealEstateViewCreateModel>(viewResult.Model);
            Assert.NotNull(model);
        }

        [Fact]
        public async Task Create_Post_ValidModel_RedirectsToDetails()
        {
            // Arrange
            var model = new RealEstateViewCreateModel
            {
                Title = "Test Property",
                Description = "Test Description",
                Category = RealEstateCategoryEnum.Residential,
                RealtyType = RealEstateTypeEnum.Apartment,
                Deal = DealTypeEnum.Sale,
                Country = "Ukraine",
                Region = "Kyiv",
                Locality = "Kyiv",
                Street = "Main Street",
                Floor = 5,
                TotalFloors = 10,
                AreaTotal = 100,
                RoomCount = 3,
                Price = 100000,
                Currency = CurrencyEnum.USD
            };

            _mockRealEstateService.Setup(x => x.CreateRealEstateAsync(It.IsAny<RealEstate>()))
                .ReturnsAsync("Success");

            // Act
            var result = await _controller.Create(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            redirectResult.ActionName.Should().Be("Details");
        }

        [Fact]
        public async Task Create_Post_InvalidModel_ReturnsViewWithError()
        {
            // Arrange
            var model = new RealEstateViewCreateModel
            {
                Title = "", // Invalid - empty title
                Description = "Test Description"
            };

            _controller.ModelState.AddModelError("Title", "Title is required");

            // Act
            var result = await _controller.Create(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var returnedModel = Assert.IsType<RealEstateViewCreateModel>(viewResult.Model);
            Assert.False(_controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Create_Post_ServiceError_ReturnsViewWithError()
        {
            // Arrange
            var model = new RealEstateViewCreateModel
            {
                Title = "Test Property",
                Description = "Test Description",
                Category = RealEstateCategoryEnum.Residential,
                RealtyType = RealEstateTypeEnum.Apartment,
                Deal = DealTypeEnum.Sale,
                Country = "Ukraine",
                Region = "Kyiv",
                Locality = "Kyiv",
                Street = "Main Street",
                Floor = 5,
                TotalFloors = 10,
                AreaTotal = 100,
                RoomCount = 3,
                Price = 100000,
                Currency = CurrencyEnum.USD
            };

            _mockRealEstateService.Setup(x => x.CreateRealEstateAsync(It.IsAny<RealEstate>()))
                .ReturnsAsync("Error creating property");

            // Act
            var result = await _controller.Create(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
        }
        #endregion

        #region Edit Tests
        [Fact]
        public async Task Edit_Get_ValidId_ReturnsViewWithModel()
        {
            // Arrange
            var propertyId = Guid.NewGuid();
            var property = new RealEstate
            {
                Id = propertyId,
                Title = "Test Property",
                UserId = _testUserId,
                Description = "Test Description",
                Category = RealEstateCategoryEnum.Residential,
                RealtyType = RealEstateTypeEnum.Apartment,
                Deal = DealTypeEnum.Sale,
                Country = "Ukraine",
                Region = "Kyiv",
                Locality = "Kyiv",
                Borough = "Shevchenkivskyi",
                Street = "Main Street",
                StreetType = "Street",
                Floor = 5,
                TotalFloors = 10,
                AreaTotal = 100,
                RoomCount = 3,
                Price = 100000,
                Currency = CurrencyEnum.USD,
                Images = new List<RealEstateImage>()
            };

            _mockRealEstateService.Setup(x => x.GetRealEstateWithImagesAsync(propertyId))
                .ReturnsAsync(property);

            // Act
            var result = await _controller.Edit(propertyId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<RealEstateViewCreateModel>(viewResult.Model);
            model.Id.Should().Be(propertyId);
        }

        [Fact]
        public async Task Edit_Get_InvalidId_ReturnsNotFound()
        {
            // Arrange
            var propertyId = Guid.NewGuid();
            _mockRealEstateService.Setup(x => x.GetRealEstateWithImagesAsync(propertyId))
                .ReturnsAsync((RealEstate)null);

            // Act
            var result = await _controller.Edit(propertyId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Get_UnauthorizedUser_ReturnsForbid()
        {
            // Arrange
            var propertyId = Guid.NewGuid();
            var differentUserId = Guid.NewGuid();
            var property = new RealEstate
            {
                Id = propertyId,
                Title = "Test Property",
                UserId = differentUserId, // Different user
                Images = new List<RealEstateImage>()
            };

            _mockRealEstateService.Setup(x => x.GetRealEstateWithImagesAsync(propertyId))
                .ReturnsAsync(property);

            // Act
            var result = await _controller.Edit(propertyId);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Edit_Post_ValidModel_RedirectsToDetails()
        {
            // Arrange
            var model = new RealEstateViewCreateModel
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                Title = "Updated Property",
                Description = "Updated Description",
                Category = RealEstateCategoryEnum.Residential,
                RealtyType = RealEstateTypeEnum.Apartment,
                Deal = DealTypeEnum.Sale,
                Country = "Ukraine",
                Region = "Kyiv",
                Locality = "Kyiv",
                Street = "Main Street",
                Floor = 5,
                TotalFloors = 10,
                AreaTotal = 100,
                RoomCount = 3,
                Price = 120000,
                Currency = CurrencyEnum.USD
            };

            _mockRealEstateService.Setup(x => x.UpdateRealEstateAsync(It.IsAny<RealEstate>(), null, null))
                .ReturnsAsync("Success");

            // Act
            var result = await _controller.Edit(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            redirectResult.ActionName.Should().Be("Details");
        }
        #endregion

        #region Delete Tests
        [Fact]
        public async Task Delete_ValidId_RedirectsToIndex()
        {
            // Arrange
            var propertyId = Guid.NewGuid();
            var property = new RealEstate
            {
                Id = propertyId,
                Title = "Test Property",
                UserId = _testUserId
            };

            _mockRealEstateService.Setup(x => x.GetRealEstateByIdAsync(propertyId))
                .ReturnsAsync(property);
            _mockRealEstateService.Setup(x => x.DeleteRealEstateAsync(propertyId))
                .ReturnsAsync("Success");

            // Act
            var result = await _controller.Delete(propertyId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            redirectResult.ActionName.Should().Be("Index");
        }

        [Fact]
        public async Task Delete_InvalidId_ReturnsNotFound()
        {
            // Arrange
            var propertyId = Guid.NewGuid();
            _mockRealEstateService.Setup(x => x.GetRealEstateByIdAsync(propertyId))
                .ReturnsAsync((RealEstate)null);

            // Act
            var result = await _controller.Delete(propertyId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_UnauthorizedUser_ReturnsForbid()
        {
            // Arrange
            var propertyId = Guid.NewGuid();
            var differentUserId = Guid.NewGuid();
            var property = new RealEstate
            {
                Id = propertyId,
                Title = "Test Property",
                UserId = differentUserId // Different user
            };

            _mockRealEstateService.Setup(x => x.GetRealEstateByIdAsync(propertyId))
                .ReturnsAsync(property);

            // Act
            var result = await _controller.Delete(propertyId);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }
        #endregion

        #region MyRealEstates Tests
        [Fact]
        public async Task MyRealEstates_ReturnsViewWithUserProperties()
        {
            // Arrange
            var userProperties = new List<RealEstate>
            {
                new RealEstate { Id = Guid.NewGuid(), Title = "User Property 1", UserId = _testUserId },
                new RealEstate { Id = Guid.NewGuid(), Title = "User Property 2", UserId = _testUserId }
            };

            _mockRealEstateService.Setup(x => x.SearchRealEstateAsync(It.Is<RealEstateSearchCriteria>(c => c.UserId == _testUserId)))
                .ReturnsAsync(userProperties);
            _mockRealEstateService.Setup(x => x.GetSearchCountAsync(It.IsAny<RealEstateSearchCriteria>()))
                .ReturnsAsync(2);

            // Act
            var result = await _controller.MyRealEstates();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<MyRealEstatesViewModel>(viewResult.Model);
            model.RealEstates.Should().HaveCount(2);
            model.UserId.Should().Be(_testUserId);
        }

        [Fact]
        public async Task MyRealEstates_InvalidUser_ReturnsForbid()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity()) // No claims
                }
            };

            // Act
            var result = await _controller.MyRealEstates();

            // Assert
            Assert.IsType<ForbidResult>(result);
        }
        #endregion

        #region Admin Tests
        [Fact]
        public async Task AdminIndex_AsAdmin_ReturnsViewWithAllProperties()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = TestHelper.CreateClaimsPrincipal(_testUserId, "Admin")
                }
            };

            var allProperties = new List<RealEstate>
            {
                new RealEstate { Id = Guid.NewGuid(), Title = "Property 1", UserId = Guid.NewGuid() },
                new RealEstate { Id = Guid.NewGuid(), Title = "Property 2", UserId = Guid.NewGuid() }
            };

            _mockRealEstateService.Setup(x => x.SearchRealEstateAsync(It.IsAny<RealEstateSearchCriteria>()))
                .ReturnsAsync(allProperties);
            _mockRealEstateService.Setup(x => x.GetSearchCountAsync(It.IsAny<RealEstateSearchCriteria>()))
                .ReturnsAsync(2);

            // Act
            var result = await _controller.AdminIndex();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<RealEstateSearchViewModel>(viewResult.Model);
            model.RealEstates.Should().HaveCount(2);
        }

        [Fact]
        public async Task AdminDelete_AsAdmin_RedirectsToAdminIndex()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = TestHelper.CreateClaimsPrincipal(_testUserId, "Admin")
                }
            };

            var propertyId = Guid.NewGuid();
            var property = new RealEstate
            {
                Id = propertyId,
                Title = "Test Property",
                UserId = Guid.NewGuid() // Different user
            };

            _mockRealEstateService.Setup(x => x.GetRealEstateByIdAsync(propertyId))
                .ReturnsAsync(property);
            _mockRealEstateService.Setup(x => x.DeleteRealEstateAsync(propertyId))
                .ReturnsAsync("Success");

            // Act
            var result = await _controller.AdminDelete(propertyId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            redirectResult.ActionName.Should().Be("AdminIndex");
        }

        [Fact]
        public async Task AdminEdit_AsAdmin_ReturnsView()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = TestHelper.CreateClaimsPrincipal(_testUserId, "Admin")
                }
            };

            var propertyId = Guid.NewGuid();
            var property = new RealEstate
            {
                Id = propertyId,
                Title = "Test Property",
                UserId = Guid.NewGuid(), // Different user
                Description = "Test Description",
                Category = RealEstateCategoryEnum.Residential,
                RealtyType = RealEstateTypeEnum.Apartment,
                Deal = DealTypeEnum.Sale,
                Country = "Ukraine",
                Region = "Kyiv",
                Locality = "Kyiv",
                Borough = "Shevchenkivskyi",
                Street = "Main Street",
                StreetType = "Street",
                Floor = 5,
                TotalFloors = 10,
                AreaTotal = 100,
                RoomCount = 3,
                Price = 100000,
                Currency = CurrencyEnum.USD,
                Images = new List<RealEstateImage>()
            };

            _mockRealEstateService.Setup(x => x.GetRealEstateWithImagesAsync(propertyId))
                .ReturnsAsync(property);

            // Act
            var result = await _controller.AdminEdit(propertyId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            viewResult.ViewName.Should().Be("Edit");
            var model = Assert.IsType<RealEstateViewCreateModel>(viewResult.Model);
            model.Id.Should().Be(propertyId);
            ((bool)_controller.ViewBag.IsAdminEdit).Should().BeTrue();
        }
        #endregion
    }
}
