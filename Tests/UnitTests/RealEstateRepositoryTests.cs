using Core.Entities;
using Core.Enums;
using Core.Xml;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.InMemory;

namespace Tests.UnitTests
{
    public class RealEstateRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly RealEstateRepository _repository;

        public RealEstateRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _repository = new RealEstateRepository(_context);
        }

        [Fact]
        public async Task AddAsync_ValidProperty_AddsToDatabase()
        {
            // Arrange
            var property = new RealEstate
            {
                Id = Guid.NewGuid(),
                Title = "Test Property",
                Description = "Test Description",
                Category = RealEstateCategoryEnum.Residential,
                RealtyType = RealEstateTypeEnum.Apartment,
                Deal = DealTypeEnum.Sale,
                Price = 100000,
                Currency = CurrencyEnum.USD,
                AreaTotal = 100,
                RoomCount = 3,
                Floor = 5,
                TotalFloors = 10,
                Country = "Ukraine",
                Region = "Kyiv",
                Locality = "Kyiv",
                Street = "Main Street",
                UserId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };

            // Act
            await _repository.AddAsync(property);

            // Assert
            var savedProperty = await _context.RealEstates.FindAsync(property.Id);
            savedProperty.Should().NotBeNull();
            savedProperty.Title.Should().Be("Test Property");
        }

        [Fact]
        public async Task GetByIdAsync_ExistingProperty_ReturnsProperty()
        {
            // Arrange
            var property = new RealEstate
            {
                Id = Guid.NewGuid(),
                Title = "Test Property",
                Description = "Test Description",
                Category = RealEstateCategoryEnum.Residential,
                RealtyType = RealEstateTypeEnum.Apartment,
                Deal = DealTypeEnum.Sale,
                Price = 100000,
                Currency = CurrencyEnum.USD,
                AreaTotal = 100,
                RoomCount = 3,
                Floor = 5,
                TotalFloors = 10,
                Country = "Ukraine",
                Region = "Kyiv",
                Locality = "Kyiv",
                Street = "Main Street",
                UserId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };

            await _context.RealEstates.AddAsync(property);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(property.Id);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(property.Id);
            result.Title.Should().Be("Test Property");
        }

        [Fact]
        public async Task SearchAsync_WithCriteria_ReturnsFilteredResults()
        {
            // Arrange
            var properties = new List<RealEstate>
            {
                new RealEstate
                {
                    Id = Guid.NewGuid(),
                    Title = "Apartment in Kyiv",
                    Description = "Modern apartment",
                    Category = RealEstateCategoryEnum.Residential,
                    RealtyType = RealEstateTypeEnum.Apartment,
                    Deal = DealTypeEnum.Sale,
                    Price = 100000,
                    Currency = CurrencyEnum.USD,
                    AreaTotal = 100,
                    RoomCount = 3,
                    Floor = 5,
                    TotalFloors = 10,
                    Country = "Ukraine",
                    Region = "Kyiv",
                    Locality = "Kyiv",
                    Street = "Main Street",
                    UserId = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow
                },
                new RealEstate
                {
                    Id = Guid.NewGuid(),
                    Title = "House in Lviv",
                    Description = "Beautiful house",
                    Category = RealEstateCategoryEnum.Residential,
                    RealtyType = RealEstateTypeEnum.House,
                    Deal = DealTypeEnum.Rent,
                    Price = 50000,
                    Currency = CurrencyEnum.USD,
                    AreaTotal = 200,
                    RoomCount = 5,
                    Floor = 1,
                    TotalFloors = 2,
                    Country = "Ukraine",
                    Region = "Lviv",
                    Locality = "Lviv",
                    Street = "Central Street",
                    UserId = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow
                }
            };

            await _context.RealEstates.AddRangeAsync(properties);
            await _context.SaveChangesAsync();

            var criteria = new RealEstateSearchCriteria
            {
                SearchQuery = "Kyiv",
                Deal = DealTypeEnum.Sale
            };

            // Act
            var result = await _repository.SearchAsync(criteria);

            // Assert
            result.Should().HaveCount(1);
            result.First().Title.Should().Be("Apartment in Kyiv");
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
