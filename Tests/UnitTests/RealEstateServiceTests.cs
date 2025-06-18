using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Xml;
using FluentAssertions;
using Logic.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.UnitTests
{
    public class RealEstateServiceTests
    {
        private readonly Mock<IRealEstateRepository> _mockRepository;
        private readonly Mock<IRealEstateImageRepository> _mockImageRepository;
        private readonly Mock<IRealEstateImageService> _mockImageService;
        private readonly RealEstateService _service;

        public RealEstateServiceTests()
        {
            _mockRepository = new Mock<IRealEstateRepository>();
            _mockImageRepository = new Mock<IRealEstateImageRepository>();
            _mockImageService = new Mock<IRealEstateImageService>();
            _service = new RealEstateService(
                _mockRepository.Object,
                _mockImageService.Object,  
                _mockImageRepository.Object
            );
        }

        #region SearchRealEstateAsync Tests
        [Fact]
        public async Task SearchRealEstateAsync_ReturnsFilteredResults()
        {
            // Arrange
            var criteria = new RealEstateSearchCriteria
            {
                SearchQuery = "apartment",
                Category = RealEstateCategoryEnum.Residential
            };

            var expectedResults = new List<RealEstate>
            {
                new RealEstate { Id = Guid.NewGuid(), Title = "Luxury Apartment", Category = RealEstateCategoryEnum.Residential }
            };

            _mockRepository.Setup(x => x.SearchAsync(criteria))
                .ReturnsAsync(expectedResults);

            // Act
            var result = await _service.SearchRealEstateAsync(criteria);

            // Assert
            result.Should().HaveCount(1);
            result.First().Title.Should().Be("Luxury Apartment");
        }
        #endregion

        #region GetRealEstateByIdAsync Tests
        [Fact]
        public async Task GetRealEstateByIdAsync_ValidId_ReturnsProperty()
        {
            // Arrange
            var propertyId = Guid.NewGuid();
            var expectedProperty = new RealEstate
            {
                Id = propertyId,
                Title = "Test Property"
            };

            _mockRepository.Setup(x => x.GetByIdAsync(propertyId))
                .ReturnsAsync(expectedProperty);

            // Act
            var result = await _service.GetRealEstateByIdAsync(propertyId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(propertyId);
            result.Title.Should().Be("Test Property");
        }

        [Fact]
        public async Task GetRealEstateByIdAsync_InvalidId_ReturnsNull()
        {
            // Arrange
            var propertyId = Guid.NewGuid();
            _mockRepository.Setup(x => x.GetByIdAsync(propertyId))
                .ReturnsAsync((RealEstate?)null);

            // Act
            var result = await _service.GetRealEstateByIdAsync(propertyId);

            // Assert
            result.Should().BeNull();
        }
        #endregion

        #region CreateRealEstateAsync Tests
        [Fact]
        public async Task CreateRealEstateAsync_ValidProperty_ReturnsSuccess()
        {
            // Arrange
            var property = new RealEstate
            {
                Id = Guid.NewGuid(),
                Title = "Test Property",
                Description = "Test Description",
                Price = 100000,
                AreaTotal = 100,
                RoomCount = 3,
                Floor = 5,
                TotalFloors = 10,
                Country = "Ukraine",
                Region = "Kyiv",
                Locality = "Kyiv",
                Street = "Main Street"
            };

            _mockRepository.Setup(x => x.AddAsync(It.IsAny<RealEstate>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateRealEstateAsync(property);

            // Assert
            result.Should().Be("Success");
            _mockRepository.Verify(x => x.AddAsync(It.IsAny<RealEstate>()), Times.Once);
        }

        [Fact]
        public async Task CreateRealEstateAsync_ThrowsException_ReturnsErrorMessage()
        {
            // Arrange
            var property = new RealEstate
            {
                Id = Guid.NewGuid(),
                Title = "Test Property"
            };

            _mockRepository.Setup(x => x.AddAsync(It.IsAny<RealEstate>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _service.CreateRealEstateAsync(property);

            // Assert
            result.Should().Be("Database error");
        }
        #endregion

        #region UpdateRealEstateAsync Tests
        [Fact]
        public async Task UpdateRealEstateAsync_ValidProperty_ReturnsSuccess()
        {
            // Arrange
            var propertyId = Guid.NewGuid();
            var existingProperty = new RealEstate
            {
                Id = propertyId,
                Title = "Old Title",
                Description = "Old Description",
                Price = 100000,
                AreaTotal = 100,
                RoomCount = 3,
                Floor = 5,
                TotalFloors = 10,
                Country = "Ukraine",
                Region = "Kyiv",
                Locality = "Kyiv",
                Street = "Main Street"
            };

            var updatedProperty = new RealEstate
            {
                Id = propertyId,
                Title = "New Title",
                Description = "New Description",
                Price = 120000,
                AreaTotal = 100,
                RoomCount = 3,
                Floor = 5,
                TotalFloors = 10,
                Country = "Ukraine",
                Region = "Kyiv",
                Locality = "Kyiv",
                Street = "Main Street"
            };

            _mockRepository.Setup(x => x.GetByIdAsync(propertyId))
                .ReturnsAsync(existingProperty);
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<RealEstate>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateRealEstateAsync(updatedProperty);

            // Assert
            result.Should().Be("Success");
            _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<RealEstate>()), Times.Once);
        }

        [Fact]
        public async Task UpdateRealEstateAsync_PropertyNotFound_ReturnsError()
        {
            // Arrange
            var propertyId = Guid.NewGuid();
            var property = new RealEstate { Id = propertyId };

            _mockRepository.Setup(x => x.GetByIdAsync(propertyId))
                .ReturnsAsync((RealEstate)null);

            // Act
            var result = await _service.UpdateRealEstateAsync(property);

            // Assert
            result.Should().Be("Property not found.");
        }

        [Fact]
        public async Task UpdateRealEstateAsync_WithImages_ReturnsSuccess()
        {
            // Arrange
            var propertyId = Guid.NewGuid();
            var existingProperty = new RealEstate
            {
                Id = propertyId,
                Title = "Test Property",
                Description = "Test Description",
                Price = 100000,
                AreaTotal = 100,
                RoomCount = 3,
                Floor = 5,
                TotalFloors = 10,
                Country = "Ukraine",
                Region = "Kyiv",
                Locality = "Kyiv",
                Street = "Main Street"
            };

            var newImages = new List<IFormFile>();
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1024);
            mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
            mockFile.Setup(f => f.FileName).Returns("test.jpg");
            newImages.Add(mockFile.Object);

            var removeImageIds = new List<Guid> { Guid.NewGuid() };
            var existingImage = new RealEstateImage
            {
                Id = removeImageIds[0],
                Url = "/test/image.jpg",
                RealEstateId = propertyId
            };

            _mockRepository.Setup(x => x.GetByIdAsync(propertyId))
                .ReturnsAsync(existingProperty);
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<RealEstate>()))
                .Returns(Task.CompletedTask);
            _mockImageRepository.Setup(x => x.GetByIdAsync(removeImageIds[0]))
                .ReturnsAsync(existingImage);
            _mockImageRepository.Setup(x => x.DeleteAsync(removeImageIds[0]))
                .Returns(Task.CompletedTask);
            _mockImageRepository.Setup(x => x.GetImagesByRealEstateIdAsync(propertyId))
                .ReturnsAsync(new List<RealEstateImage>());
            _mockImageRepository.Setup(x => x.AddAsync(It.IsAny<RealEstateImage>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateRealEstateAsync(existingProperty, newImages, removeImageIds);

            // Assert
            result.Should().Be("Success");
            _mockImageRepository.Verify(x => x.DeleteAsync(removeImageIds[0]), Times.Once);
            _mockImageRepository.Verify(x => x.AddAsync(It.IsAny<RealEstateImage>()), Times.Once);
        }
        #endregion

        #region DeleteRealEstateAsync Tests
        [Fact]
        public async Task DeleteRealEstateAsync_ValidId_ReturnsSuccess()
        {
            // Arrange
            var propertyId = Guid.NewGuid();
            var property = new RealEstate
            {
                Id = propertyId,
                Title = "Test Property"
            };

            var images = new List<RealEstateImage>
            {
                new RealEstateImage { Id = Guid.NewGuid(), RealEstateId = propertyId }
            };

            _mockRepository.Setup(x => x.GetByIdAsync(propertyId))
                .ReturnsAsync(property);
            _mockRepository.Setup(x => x.DeleteAsync(propertyId))
                .Returns(Task.CompletedTask);

            // Mock image service
            var mockImageService = new Mock<IRealEstateImageService>();
            mockImageService.Setup(x => x.GetImagesByPropertyIdAsync(propertyId))
                .ReturnsAsync(images);
            mockImageService.Setup(x => x.DeleteImageAsync(It.IsAny<Guid>()))
                .ReturnsAsync("Success");

            var serviceWithImageService = new RealEstateService(_mockRepository.Object, mockImageService.Object, _mockImageRepository.Object);

            // Act
            var result = await serviceWithImageService.DeleteRealEstateAsync(propertyId);

            // Assert
            result.Should().Be("Success");
            _mockRepository.Verify(x => x.DeleteAsync(propertyId), Times.Once);
        }

        [Fact]
        public async Task DeleteRealEstateAsync_PropertyNotFound_ReturnsError()
        {
            // Arrange
            var propertyId = Guid.NewGuid();
            _mockRepository.Setup(x => x.GetByIdAsync(propertyId))
                .ReturnsAsync((RealEstate)null);

            // Act
            var result = await _service.DeleteRealEstateAsync(propertyId);

            // Assert
            result.Should().Be("Property not found.");
        }
        #endregion

        #region GetUserRealEstatesAsync Tests
        [Fact]
        public async Task GetUserRealEstatesAsync_ReturnsUserProperties()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userProperties = new List<RealEstate>
            {
                new RealEstate { Id = Guid.NewGuid(), Title = "Property 1", UserId = userId },
                new RealEstate { Id = Guid.NewGuid(), Title = "Property 2", UserId = userId }
            };

            _mockRepository.Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(userProperties);

            // Act
            var result = await _service.GetUserRealEstatesAsync(userId);

            // Assert
            result.Should().HaveCount(2);
            result.All(r => r.UserId == userId).Should().BeTrue();
        }
        #endregion

        #region ValidateRealEstate Tests
        [Theory]
        [InlineData("", "Title is required.")]
        [InlineData("   ", "Title is required.")]
        public void ValidateRealEstate_InvalidTitle_ReturnsError(string title, string expectedError)
        {
            // Arrange
            var property = new RealEstate
            {
                Title = title,
                Description = "Valid Description",
                Price = 100000,
                AreaTotal = 100,
                RoomCount = 3,
                Floor = 5,
                TotalFloors = 10,
                Country = "Ukraine",
                Region = "Kyiv",
                Locality = "Kyiv",
                Borough = "Test Borough",
                Street = "Main Street",
                StreetType = "Street",
            };

            // Act
            var result = InvokeValidateRealEstate(property);

            // Assert
            result.Should().Be(expectedError);
        }

        [Fact]
        public void ValidateRealEstate_ValidProperty_ReturnsSuccess()
        {
            // Arrange
            var property = new RealEstate
            {
                Title = "Valid Title",
                Description = "Valid Description",
                Price = 100000,
                AreaTotal = 100,
                RoomCount = 3,
                Floor = 5,
                TotalFloors = 10,
                Country = "Ukraine",
                Region = "Kyiv",
                Locality = "Kyiv",
                Borough = "Test Borough",
                Street = "Main Street",
                StreetType = "Street"
            };

            // Act
            var result = InvokeValidateRealEstate(property);

            // Assert
            result.Should().Be("Success");
        }

        private string InvokeValidateRealEstate(RealEstate realEstate)
        {
            // Якщо ValidateRealEstate - публічний статичний метод
            var method = typeof(RealEstateService).GetMethod("ValidateRealEstate",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            if (method == null)
            {
                // Якщо ValidateRealEstate - приватний статичний метод
                method = typeof(RealEstateService).GetMethod("ValidateRealEstate",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            }

            if (method == null)
            {
                throw new InvalidOperationException("ValidateRealEstate method not found");
            }

            return (string)method.Invoke(null, new object[] { realEstate })!;
        }
        #endregion
    }
}
