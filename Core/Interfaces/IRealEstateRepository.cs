using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Entities;

namespace Core.Interfaces
{
    public interface IRealEstateRepository : IRepository<RealEstate>
    {
        Task<IEnumerable<RealEstate>> SearchAsync(
            string? city = null,
            string? borough = null,
            int? roomCount = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            bool? isNew = null,
            string? keyword = null
        );
    }

}
