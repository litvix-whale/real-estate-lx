using Core.Entities;
using Core.Interfaces;
using Core.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class RealEstateRepository(AppDbContext context) : RepositoryBase<RealEstate>(context), IRealEstateRepository
    {
        public async Task<IEnumerable<RealEstate>> SearchAsync(RealEstateSearchCriteria criteria)
        {
            var query = _context.RealEstates
                .Include(r => r.Images)
                .Include(r => r.User)
                .AsQueryable();

            // Застосування фільтрів
            query = ApplyFilters(query, criteria);

            // Сортування
            query = ApplySorting(query, criteria);

            // Пагінація
            return await query
                .Skip(criteria.Skip)
                .Take(criteria.Take)
                .ToListAsync();
        }

        public async Task<int> GetSearchCountAsync(RealEstateSearchCriteria criteria)
        {
            var query = _context.RealEstates.AsQueryable();
            query = ApplyFilters(query, criteria);
            return await query.CountAsync();
        }

        public async Task<IEnumerable<RealEstate>> GetByUserIdAsync(Guid userId)
        {
            return await _context.RealEstates
                .Include(r => r.Images)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<RealEstate>> GetByLocationAsync(string locality, string? region = null)
        {
            var query = _context.RealEstates
                .Include(r => r.Images)
                .Where(r => r.Locality.Contains(locality));

            if (!string.IsNullOrEmpty(region))
            {
                query = query.Where(r => r.Region.Contains(region));
            }

            return await query.ToListAsync();
        }

        public async Task<RealEstate?> GetByIdWithImagesAsync(Guid id)
        {
            return await _context.RealEstates
                .Include(r => r.Images.OrderBy(i => i.UiPriority))
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        private IQueryable<RealEstate> ApplyFilters(IQueryable<RealEstate> query, RealEstateSearchCriteria criteria)
        {
            // Текстовий пошук
            if (!string.IsNullOrEmpty(criteria.SearchQuery))
            {
                var searchQuery = criteria.SearchQuery.ToLower();
                query = query.Where(r =>
                    r.Title.ToLower().Contains(searchQuery) ||
                    r.Description.ToLower().Contains(searchQuery) ||
                    r.Locality.ToLower().Contains(searchQuery) ||
                    r.Street.ToLower().Contains(searchQuery) ||
                    (r.NewBuildingName != null && r.NewBuildingName.ToLower().Contains(searchQuery)));
            }

            // Фільтри за категорією та типом
            if (criteria.Category.HasValue)
                query = query.Where(r => r.Category == criteria.Category.Value);

            if (criteria.RealtyType.HasValue)
                query = query.Where(r => r.RealtyType == criteria.RealtyType.Value);

            if (criteria.Deal.HasValue)
                query = query.Where(r => r.Deal == criteria.Deal.Value);

            if (criteria.IsNewBuilding.HasValue)
                query = query.Where(r => r.IsNewBuilding == criteria.IsNewBuilding.Value);

            // Фільтри за локацією
            if (!string.IsNullOrEmpty(criteria.Country))
                query = query.Where(r => r.Country.Contains(criteria.Country));

            if (!string.IsNullOrEmpty(criteria.Region))
                query = query.Where(r => r.Region.Contains(criteria.Region));

            if (!string.IsNullOrEmpty(criteria.Locality))
                query = query.Where(r => r.Locality.Contains(criteria.Locality));

            if (!string.IsNullOrEmpty(criteria.Borough))
                query = query.Where(r => r.Borough.Contains(criteria.Borough));

            // Фільтри за характеристиками
            if (criteria.MinFloor.HasValue)
                query = query.Where(r => r.Floor >= criteria.MinFloor.Value);

            if (criteria.MaxFloor.HasValue)
                query = query.Where(r => r.Floor <= criteria.MaxFloor.Value);

            if (criteria.MinAreaTotal.HasValue)
                query = query.Where(r => r.AreaTotal >= criteria.MinAreaTotal.Value);

            if (criteria.MaxAreaTotal.HasValue)
                query = query.Where(r => r.AreaTotal <= criteria.MaxAreaTotal.Value);

            if (criteria.MinRoomCount.HasValue)
                query = query.Where(r => r.RoomCount >= criteria.MinRoomCount.Value);

            if (criteria.MaxRoomCount.HasValue)
                query = query.Where(r => r.RoomCount <= criteria.MaxRoomCount.Value);

            // Фільтри за ціною
            if (criteria.MinPrice.HasValue)
                query = query.Where(r => r.Price >= criteria.MinPrice.Value);

            if (criteria.MaxPrice.HasValue)
                query = query.Where(r => r.Price <= criteria.MaxPrice.Value);

            if (criteria.Currency.HasValue)
                query = query.Where(r => r.Currency == criteria.Currency.Value);

            return query;
        }

        private IQueryable<RealEstate> ApplySorting(IQueryable<RealEstate> query, RealEstateSearchCriteria criteria)
        {
            return criteria.SortBy?.ToLower() switch
            {
                "price" => criteria.SortDescending
                    ? query.OrderByDescending(r => r.Price)
                    : query.OrderBy(r => r.Price),
                "areatotal" => criteria.SortDescending
                    ? query.OrderByDescending(r => r.AreaTotal)
                    : query.OrderBy(r => r.AreaTotal),
                "roomcount" => criteria.SortDescending
                    ? query.OrderByDescending(r => r.RoomCount)
                    : query.OrderBy(r => r.RoomCount),
                "floor" => criteria.SortDescending
                    ? query.OrderByDescending(r => r.Floor)
                    : query.OrderBy(r => r.Floor),
                _ => criteria.SortDescending
                    ? query.OrderByDescending(r => r.CreatedAt)
                    : query.OrderBy(r => r.CreatedAt)
            };
        }
    }
}