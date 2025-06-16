using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class RealEstateRepository(AppDbContext context)
    : RepositoryBase<RealEstate>(context), IRealEstateRepository
{
    public async Task<IEnumerable<RealEstate>> SearchAsync(
        string? city = null,
        string? borough = null,
        int? roomCount = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool? isNew = null,
        string? keyword = null)
    {
        var query = _context.RealEstates.AsQueryable();

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(r => r.Locality.ToLower().Contains(city.ToLower()));

        if (!string.IsNullOrWhiteSpace(borough))
            query = query.Where(r => r.Borough.ToLower().Contains(borough.ToLower()));

        if (roomCount is not null)
            query = query.Where(r => r.RoomCount == roomCount);

        if (minPrice is not null)
            query = query.Where(r => r.Price >= minPrice);

        if (maxPrice is not null)
            query = query.Where(r => r.Price <= maxPrice);

        if (isNew is not null)
            query = query.Where(r => r.IsNewBuilding == isNew);

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(r =>
                r.Title.ToLower().Contains(keyword.ToLower()) ||
                r.Description.ToLower().Contains(keyword.ToLower()));

        return await query.ToListAsync();
    }
}