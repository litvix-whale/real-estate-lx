using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Entities;
using Core.Enums;
using Core.Models;

namespace Core.Interfaces
{
    public interface IRealEstateService
    {
        Task<IEnumerable<RealEstate>> SearchRealEstateAsync(RealEstateSearchCriteria criteria);
        Task<int> GetSearchCountAsync(RealEstateSearchCriteria criteria);
        Task<RealEstate?> GetRealEstateByIdAsync(Guid id);
        Task<RealEstate?> GetRealEstateWithImagesAsync(Guid id);
        Task<string> CreateRealEstateAsync(RealEstate realEstate);
        Task<string> UpdateRealEstateAsync(RealEstate realEstate);
        Task<string> DeleteRealEstateAsync(Guid id);
        Task<IEnumerable<RealEstate>> GetUserRealEstatesAsync(Guid userId);
    }
}
