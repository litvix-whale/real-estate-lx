using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Entities;
using Core.Xml;

namespace Core.Interfaces
{
    public interface IRealEstateRepository : IRepository<RealEstate>
    {
        Task<IEnumerable<RealEstate>> SearchAsync(RealEstateSearchCriteria criteria);
        Task<int> GetSearchCountAsync(RealEstateSearchCriteria criteria);
        Task<IEnumerable<RealEstate>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<RealEstate>> GetByLocationAsync(string locality, string? region = null);
        Task<RealEstate?> GetByIdWithImagesAsync(Guid id);
    }

}
