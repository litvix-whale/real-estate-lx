using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities
{
    public class RealEstateImage:EntityBase
    {
        public string Url { get; set; } = default!;
        public int UiPriority { get; set; }

        public Guid RealEstateId { get; set; }
        public RealEstate RealEstate { get; set; } = default!;
    }
}
