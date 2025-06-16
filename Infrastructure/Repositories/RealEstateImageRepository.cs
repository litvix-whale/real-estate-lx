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
                .ToListAsync();
        }
    }
}
