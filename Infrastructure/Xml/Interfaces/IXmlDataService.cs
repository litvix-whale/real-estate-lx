using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Infrastructure.Data;

namespace Infrastructure.Xml.Interfaces
{
    public interface IXmlDataService
    {
        Task<List<XmlRealEstateItem>> LoadRealEstateDataAsync();
        Task SeedRealEstateInBatchesAsync(AppDbContext context, Guid userId);
    }
}
