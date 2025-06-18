using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class RealEstateImageRepository(AppDbContext context) : RepositoryBase<RealEstateImage>(context), IRealEstateImageRepository
    {
        public async Task<IEnumerable<RealEstateImage>> GetImagesByRealEstateIdAsync(Guid realEstateId)
        {
            return await _context.RealEstateImages
                .Where(image => image.RealEstateId == realEstateId)
                .OrderBy(image => image.UiPriority)
                .ToListAsync();
        }

        public async Task<RealEstateImage?> GetByRealEstateIdAndPriorityAsync(Guid realEstateId, int priority)
        {
            return await _context.RealEstateImages
                .FirstOrDefaultAsync(i => i.RealEstateId == realEstateId && i.UiPriority == priority);
        }

        public async Task<int> GetMaxPriorityAsync(Guid realEstateId)
        {
            var images = await _context.RealEstateImages
                .Where(i => i.RealEstateId == realEstateId)
                .ToListAsync();

            return images.Any() ? images.Max(i => i.UiPriority) : 0;
        }

        public async Task DeleteByRealEstateIdAsync(Guid realEstateId)
        {
            var images = await _context.RealEstateImages
                .Where(i => i.RealEstateId == realEstateId)
                .ToListAsync();

            _context.RealEstateImages.RemoveRange(images);
            await _context.SaveChangesAsync();
        }
    }
}
