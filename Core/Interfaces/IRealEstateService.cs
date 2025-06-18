using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Entities;
using Core.Enums;
using Core.Xml;
using Microsoft.AspNetCore.Http;

namespace Core.Interfaces
{
    public interface IRealEstateService
    {
        Task<IEnumerable<RealEstate>> SearchRealEstateAsync(RealEstateSearchCriteria criteria);
        Task<int> GetSearchCountAsync(RealEstateSearchCriteria criteria);
        Task<RealEstate?> GetRealEstateByIdAsync(Guid id);
        Task<RealEstate?> GetRealEstateWithImagesAsync(Guid id);

        // Оновлені методи з обробкою файлів
        Task<string> CreateRealEstateAsync(RealEstate realEstate);

        Task<string> UpdateRealEstateAsync(RealEstate realEstate, List<IFormFile>? newImages, List<Guid>? removeImageIds);
        Task<string> DeleteRealEstateAsync(Guid id);

        Task<IEnumerable<RealEstate>> GetUserRealEstatesAsync(Guid userId);

        // Методи для роботи з зображеннями
        Task<string> AddImagesToPropertyAsync(Guid realEstateId, List<IFormFile> images);
        Task<string> RemoveImageAsync(Guid imageId);
        Task<byte[]?> GetImageContentAsync(Guid imageId);
    }
}
