using Core.Entities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IRealEstateImageService
    {
        Task<string> AddImagesAsync(Guid realEstateId, List<IFormFile> images);
        Task<string> DeleteImageAsync(Guid imageId);
        Task<string> UpdateImagePriorityAsync(Guid imageId, int newPriority);
        Task<IEnumerable<RealEstateImage>> GetImagesByPropertyIdAsync(Guid realEstateId);
        Task<byte[]?> GetImageContentAsync(Guid imageId);
    }
}
